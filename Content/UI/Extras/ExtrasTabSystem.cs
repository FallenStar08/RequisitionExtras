using System;
using Terraria.ModLoader;
using Terraria.UI;
using TerraStorage.Content.UI.Elements;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Content.UI.Extras
{
    public class ExtrasTabSystem : ModSystem
    {
        private delegate void orig_VoidNoArgs(object self);
        private delegate void Hook_VoidNoArgs(orig_VoidNoArgs orig, object self);

        private delegate void orig_SwitchTab(object self, int activeTab);
        private delegate void Hook_SwitchTabDelegate(
            orig_SwitchTab orig,
            object self,
            int activeTab
        );

        private static Type _terminalUIStateType;
        private static TSTab _extrasTab;
        private static ExtrasPanel _extrasPanel;
        private static bool _isExtrasActive;

        const float TabsY = 0f;
        const float TabsHeight = 25f;

        // Native UI fields to hide when Extras tab is open
        private static readonly string[] NativeElements =
        {
            "_itemGrid",
            "_scrollbar",
            "_depositAllBtn",
            "_craftingPanel",
            "_diskPanel",
            "_searchBar",
            "_filterBar",
            "_sortBar",
        };

        // Native UI fields to restore when leaving Extras tab
        private static readonly string[] NativeRestoreElements =
        {
            "_searchBar",
            "_filterBar",
            "_sortBar",
            "_depositAllBtn",
            "_scrollbar",
        };

        public override void Load()
        {
            if (!ModLoader.TryGetMod("TerraStorage", out Mod terraStorage))
                return;

            _terminalUIStateType = terraStorage.Code.GetType(
                "TerraStorage.Content.UI.TerminalUIState"
            );
            if (_terminalUIStateType == null)
                return;

            var visualMethod = Reflect.Method(_terminalUIStateType, "UpdateTabVisuals");
            var switchMethod = Reflect.Method(_terminalUIStateType, "SwitchTab");
            var activateMethod =
                Reflect.Method(_terminalUIStateType, "OnActivate")
                ?? Reflect.Method<UIElement>("OnActivate");

            if (activateMethod != null)
                MonoModHooks.Add(activateMethod, (Hook_VoidNoArgs)Hook_OnActivate);
            if (visualMethod != null)
                MonoModHooks.Add(visualMethod, (Hook_VoidNoArgs)Hook_UpdateTabVisuals);
            if (switchMethod != null)
                MonoModHooks.Add(switchMethod, (Hook_SwitchTabDelegate)Hook_SwitchTab);
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
            var mainPanel = Reflect.GetValue<UIElement>(self, "_mainPanel");
            if (mainPanel == null)
                return;

            // Strip ALL native tab elements from mainPanel
            foreach (var field in NativeElements)
            {
                RemoveIfPresent(mainPanel, Reflect.GetValue<object>(self, field));
            }

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
            var mainPanel = Reflect.GetValue<UIElement>(self, "_mainPanel");
            if (mainPanel == null)
                return;

            if (_extrasPanel != null)
            {
                _extrasPanel.ResetReport();

                if (_extrasPanel.Parent == mainPanel)
                {
                    mainPanel.RemoveChild(_extrasPanel);
                }
            }

            // Restore native UI elements required by SwitchTab
            foreach (var field in NativeRestoreElements)
            {
                RestoreIfMissing(mainPanel, Reflect.GetValue<object>(self, field));
            }
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
            if (_terminalUIStateType == null || !_terminalUIStateType.IsInstanceOfType(self))
                return;

            EnsureTabAttached(self);
            Reflect.Invoke(self, "UpdateTabVisuals");
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
                ResetTab(Reflect.GetValue<object>(self, "_storageTab"));
                ResetTab(Reflect.GetValue<object>(self, "_craftingTab"));
                ResetTab(Reflect.GetValue<object>(self, "_disksTab"));
            }

            // Highlight custom tab
            if (_extrasTab != null)
            {
                _extrasTab.Active = _isExtrasActive;
                _extrasTab.Top.Set(_isExtrasActive ? TabsY - 3 : TabsY, 0f);
                _extrasTab.Height.Set(_isExtrasActive ? TabsHeight + 3 : TabsHeight, 0f);
                _extrasTab.Recalculate();
            }
        }

        private static void ResetTab(object tabObj)
        {
            if (tabObj is TSTab tab)
            {
                tab.Active = false;
                tab.Top.Set(TabsY, 0f);
                tab.Height.Set(TabsHeight, 0f);
                tab.Recalculate();
            }
        }

        private static void EnsureTabAttached(object self)
        {
            if (_terminalUIStateType == null || !_terminalUIStateType.IsInstanceOfType(self))
                return;

            var mainPanel = Reflect.GetValue<TSWindowElement>(self, "_mainPanel");
            if (mainPanel == null)
                return;

            if (_extrasTab == null || _extrasTab.Parent != mainPanel)
            {
                _extrasTab = new TSTab(EasyLoca.ExtrasTabName);
                _extrasTab.Width.Set(105, 0f);
                _extrasTab.Height.Set(TabsHeight, 0f);
                _extrasTab.Left.Set(346, 0f);
                _extrasTab.Top.Set(TabsY, 0f);

                _extrasTab.OnLeftClick += (evt, el) =>
                {
                    _isExtrasActive = true;

                    // Invalidate native _activeTab so switching back triggers full re-render
                    Reflect.SetValue(self, "_activeTab", -1);

                    ShowExtrasContent(self);
                    Reflect.Invoke(self, "UpdateTabVisuals");
                };

                var origDrag = mainPanel.GetDragZone;
                if (origDrag != null)
                {
                    mainPanel.GetDragZone = mouse =>
                    {
                        var id = mainPanel.GetInnerDimensions();
                        return (
                                mouse.Y < id.Y
                                || mouse.Y > id.Y + 31f
                                || mouse.X < id.X + 346f
                                || mouse.X > id.X + 451f
                            ) && origDrag(mouse);
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
