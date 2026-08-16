using System;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.UI;
using TerraStorage.Content.UI.Elements;
using TerraStorageOverflow.Content.UI.Tabs;

namespace TerraStorageOverflow.Content.UI.TabSystem
{
    public class ExtrasTabSystem : ModSystem
    {
        private delegate void orig_VoidNoArgs(object self);
        private delegate void Hook_VoidNoArgs(orig_VoidNoArgs orig, object self);

        private delegate void orig_SwitchTab(object self, int activeTab);
        private delegate void Hook_SwitchTabDelegate(orig_SwitchTab orig, object self, int activeTab);

        private static Type _terminalUIStateType;
        private static FieldInfo _mainPanelField;
        private static FieldInfo _activeTabField;
        private static FieldInfo _storageTabField;
        private static FieldInfo _craftingTabField;
        private static FieldInfo _disksTabField;

        // Native panel & bar elements
        private static FieldInfo _itemGridField;
        private static FieldInfo _scrollbarField;
        private static FieldInfo _depositAllBtnField;
        private static FieldInfo _craftingPanelField;
        private static FieldInfo _diskPanelField;
        private static FieldInfo _searchBarField;
        private static FieldInfo _filterBarField;
        private static FieldInfo _sortBarField;

        const float tabsY = 0f;
        const float tabsHeight = 25f;

        private static TSTab _extrasTab;
        private static ExtrasPanel _extrasPanel;
        private static bool _isExtrasActive = false;

        public override void Load()
        {
            if (!ModLoader.TryGetMod("TerraStorage", out Mod terraStorage))
                return;

            _terminalUIStateType = terraStorage.Code.GetType("TerraStorage.Content.UI.TerminalUIState");
            if (_terminalUIStateType == null) return;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            _mainPanelField = _terminalUIStateType.GetField("_mainPanel", flags);
            _activeTabField = _terminalUIStateType.GetField("_activeTab", flags);
            _storageTabField = _terminalUIStateType.GetField("_storageTab", flags);
            _craftingTabField = _terminalUIStateType.GetField("_craftingTab", flags);
            _disksTabField = _terminalUIStateType.GetField("_disksTab", flags);

            // Refs to some native UI elements on _mainPanel
            _itemGridField = _terminalUIStateType.GetField("_itemGrid", flags);
            _scrollbarField = _terminalUIStateType.GetField("_scrollbar", flags);
            _depositAllBtnField = _terminalUIStateType.GetField("_depositAllBtn", flags);
            _craftingPanelField = _terminalUIStateType.GetField("_craftingPanel", flags);
            _diskPanelField = _terminalUIStateType.GetField("_diskPanel", flags);
            _searchBarField = _terminalUIStateType.GetField("_searchBar", flags);
            _filterBarField = _terminalUIStateType.GetField("_filterBar", flags);
            _sortBarField = _terminalUIStateType.GetField("_sortBar", flags);

            MethodInfo visualMethod = _terminalUIStateType.GetMethod("UpdateTabVisuals", flags);
            MethodInfo switchMethod = _terminalUIStateType.GetMethod("SwitchTab", flags);
            MethodInfo activateMethod = _terminalUIStateType.GetMethod("OnActivate", flags)
                                     ?? typeof(UIElement).GetMethod("OnActivate", flags);

            if (activateMethod != null) MonoModHooks.Add(activateMethod, (Hook_VoidNoArgs)Hook_OnActivate);
            if (visualMethod != null) MonoModHooks.Add(visualMethod, (Hook_VoidNoArgs)Hook_UpdateTabVisuals);
            if (switchMethod != null) MonoModHooks.Add(switchMethod, (Hook_SwitchTabDelegate)Hook_SwitchTab);
        }

        public override void Unload()
        {
            _terminalUIStateType = null;
            _extrasTab = null;
            _extrasPanel = null;
            _isExtrasActive = false;
        }

        private static void ShowExtrasContent(object self)
        {
            if (_mainPanelField?.GetValue(self) is not UIElement mainPanel) return;

            // Strip ALL native tab elements from mainPanel
            RemoveIfPresent(mainPanel, _itemGridField?.GetValue(self));
            RemoveIfPresent(mainPanel, _scrollbarField?.GetValue(self));
            RemoveIfPresent(mainPanel, _depositAllBtnField?.GetValue(self));
            RemoveIfPresent(mainPanel, _craftingPanelField?.GetValue(self));
            RemoveIfPresent(mainPanel, _diskPanelField?.GetValue(self));
            RemoveIfPresent(mainPanel, _searchBarField?.GetValue(self));
            RemoveIfPresent(mainPanel, _filterBarField?.GetValue(self));
            RemoveIfPresent(mainPanel, _sortBarField?.GetValue(self));

            // Show Extras Panel
            if (_extrasPanel == null)
            {
                _extrasPanel = new ExtrasPanel(self);
                _extrasPanel.Activate();
            }

            if (_extrasPanel.Parent != mainPanel)
            {
                mainPanel.Append(_extrasPanel);
            }
        }

        private static void HideExtrasContent(object self)
        {
            if (_mainPanelField?.GetValue(self) is not UIElement mainPanel) return;

            if (_extrasPanel != null)
            {
                _extrasPanel.ResetReport();

                if (_extrasPanel.Parent == mainPanel)
                {
                    mainPanel.RemoveChild(_extrasPanel);
                }
            }

            // Restore static native UI elements that native SwitchTab expects to exist on mainPanel
            RestoreIfMissing(mainPanel, _searchBarField?.GetValue(self));
            RestoreIfMissing(mainPanel, _filterBarField?.GetValue(self));
            RestoreIfMissing(mainPanel, _sortBarField?.GetValue(self));
            RestoreIfMissing(mainPanel, _depositAllBtnField?.GetValue(self));
            RestoreIfMissing(mainPanel, _scrollbarField?.GetValue(self));
        }

        private static void RemoveIfPresent(UIElement parent, object childObj)
        {
            if (childObj is UIElement child && child.Parent == parent)
            {
                parent.RemoveChild(child);
            }
        }

        private static void RestoreIfMissing(UIElement parent, object childObj)
        {
            if (childObj is UIElement child && child.Parent != parent)
            {
                parent.Append(child);
            }
        }

        private static void Hook_OnActivate(orig_VoidNoArgs orig, object self)
        {
            orig(self);
            if (_terminalUIStateType == null || !_terminalUIStateType.IsInstanceOfType(self)) return;

            EnsureTabAttached(self);
            MethodInfo updateVisuals = self.GetType().GetMethod("UpdateTabVisuals", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            updateVisuals?.Invoke(self, null);
        }

        private static void Hook_UpdateTabVisuals(orig_VoidNoArgs orig, object self)
        {
            if (_terminalUIStateType == null || !_terminalUIStateType.IsInstanceOfType(self))
            {
                orig(self);
                return;
            }

            EnsureTabAttached(self);

            orig(self);

            // Reset native tabs to unselected visual state when Extras is active
            if (_isExtrasActive)
            {
                static void ResetTab(object tabObj)
                {
                    if (tabObj is TSTab tab)
                    {
                        tab.Active = false;
                        tab.Top.Set(tabsY, 0f);
                        tab.Height.Set(tabsHeight, 0f);
                        tab.Recalculate();
                    }
                }

                ResetTab(_storageTabField?.GetValue(self));
                ResetTab(_craftingTabField?.GetValue(self));
                ResetTab(_disksTabField?.GetValue(self));
            }

            // Highlight our custom tab
            if (_extrasTab != null)
            {
                _extrasTab.Active = _isExtrasActive;
                _extrasTab.Top.Set(_isExtrasActive ? tabsY - 3 : tabsY, 0f);
                _extrasTab.Height.Set(_isExtrasActive ? tabsHeight + 3 : tabsHeight, 0f);
                _extrasTab.Recalculate();
            }
        }

        private static void EnsureTabAttached(object self)
        {
            if (_terminalUIStateType == null || !_terminalUIStateType.IsInstanceOfType(self)) return;
            if (_mainPanelField?.GetValue(self) is not TSWindowElement mainPanel) return;

            if (_extrasTab == null || _extrasTab.Parent != mainPanel)
            {
                _extrasTab = new TSTab("Extras");
                _extrasTab.Width.Set(105, 0f);
                _extrasTab.Height.Set(tabsHeight, 0f);
                _extrasTab.Left.Set(346, 0f);
                _extrasTab.Top.Set(tabsY, 0f);

                _extrasTab.OnLeftClick += (evt, el) =>
                {
                    _isExtrasActive = true;

                    // Invalidate native _activeTab so switching back to native tabs triggers full re-render
                    _activeTabField?.SetValue(self, -1);

                    ShowExtrasContent(self);

                    MethodInfo updateVisuals = self.GetType().GetMethod("UpdateTabVisuals", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    updateVisuals?.Invoke(self, null);
                };

                var origDrag = mainPanel.GetDragZone;
                if (origDrag != null)
                {
                    mainPanel.GetDragZone = mouse =>
                    {
                        var id = mainPanel.GetInnerDimensions();
                        return (mouse.Y < id.Y || mouse.Y > id.Y + 31f || mouse.X < id.X + 346f || mouse.X > id.X + 451f) && origDrag(mouse);
                    };
                }

                mainPanel.Append(_extrasTab);
                _extrasTab.Activate();
                mainPanel.Recalculate();
            }
        }

        private static void Hook_SwitchTab(orig_SwitchTab orig, object self, int activeTab)
        {
            _isExtrasActive = false;
            HideExtrasContent(self);
            orig(self, activeTab);
        }
    }
}