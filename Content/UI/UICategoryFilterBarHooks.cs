using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerraStorage.Content.UI.Elements;

namespace TerraStorageOverflow.Content.UI
{
    public class CustomCategory(ItemCategory categoryId, string modName, string[] damageClassNames, string iconItemName, string tooltip, int fallbackIconId = 506)
    {
        public string ModName { get; } = modName;
        public string[] DamageClassNames { get; } = damageClassNames;
        public string IconItemName { get; } = iconItemName;
        public int FallbackIconId { get; } = fallbackIconId;
        public string Tooltip { get; } = tooltip;
        public ItemCategory CategoryId { get; } = categoryId;

        public List<DamageClass> ResolvedClasses { get; } = [];
        public int ResolvedIconId { get; set; }
        public bool IsLoaded { get; private set; }

        public void Resolve(Mod hostMod)
        {
            if (IsLoaded)
                return;

            ResolvedClasses.Clear();

            if (!ModLoader.TryGetMod(ModName, out Mod targetMod))
            {
                hostMod.Logger.Warn($"[CustomCategory] Mod '{ModName}' was not found/loaded.");
                return;
            }

            foreach (string className in DamageClassNames)
            {
                if (targetMod.TryFind<DamageClass>(className, out var dc))
                {
                    ResolvedClasses.Add(dc);
                    hostMod.Logger.Info($"[CustomCategory] Successfully resolved DamageClass '{className}' from {ModName}.");
                }
                else
                {
                    hostMod.Logger.Warn($"[CustomCategory] Could not find DamageClass '{className}' in {ModName}.");
                }
            }

            if (ResolvedClasses.Count > 0)
            {
                IsLoaded = true;
                ResolvedIconId = targetMod.TryFind<ModItem>(IconItemName, out var item) ? item.Type : FallbackIconId;
                hostMod.Logger.Info($"[CustomCategory] Category for '{ModName}' resolved with IconItemID: {ResolvedIconId}.");
            }
        }
    }

    [ExtendsFromMod("TerraStorage")]
    public class CategoryHookSystem : ModSystem
    {
        private static readonly List<CustomCategory> Registry = [];

        private delegate void orig_InitActiveCategories();
        private delegate ItemCategory orig_ClassifyItemInstance(Item item);
        private delegate bool orig_PassesFilter(UICategoryFilterBar self, int itemType);

        public override void Load()
        {
            //CLICKER CLASS CATEGORY
            RegisterCategory(new CustomCategory(
                categoryId: (ItemCategory)100,
                modName: "ClickerClass",
                damageClassNames: ["ClickerDamage"],
                iconItemName: "TheClicker",
                tooltip: "Clicker Weapons"
            ));
            //CAPTURE DISC CLASS CATEGORY
            RegisterCategory(new CustomCategory(
                categoryId: (ItemCategory)101,
                modName: "CaptureDiscClass",
                damageClassNames: ["CaptureDamage"],
                iconItemName: "HighTest",
                tooltip: "Capture Weapons"
            ));

            Type targetType = typeof(UICategoryFilterBar);

            MethodInfo initMethod = targetType.GetMethod("InitActiveCategories", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo classifyMethod = targetType.GetMethod("ClassifyItemInstance", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo passesFilterMethod = targetType.GetMethod("PassesFilter", BindingFlags.Public | BindingFlags.Instance);

            if (initMethod == null)
            {
                Mod.Logger.Error("[CategoryHookSystem] Failed to find InitActiveCategories method via Reflection!");
            }
            else
            {
                MonoModHooks.Add(initMethod, Detour_InitActiveCategories);
                Mod.Logger.Info("[CategoryHookSystem] Hooked InitActiveCategories successfully.");
            }

            if (classifyMethod == null)
            {
                Mod.Logger.Error("[CategoryHookSystem] Failed to find ClassifyItemInstance method via Reflection!");
            }
            else
            {
                MonoModHooks.Add(classifyMethod, Detour_ClassifyItemInstance);
                Mod.Logger.Info("[CategoryHookSystem] Hooked ClassifyItemInstance successfully.");
            }

            if (passesFilterMethod == null)
            {
                Mod.Logger.Error("[CategoryHookSystem] Failed to find PassesFilter method via Reflection!");
            }
            else
            {
                MonoModHooks.Add(passesFilterMethod, Detour_PassesFilter);
                Mod.Logger.Info("[CategoryHookSystem] Hooked PassesFilter successfully.");
            }
        }

        public override void PostSetupContent()
        {
            InjectCategoriesDirectly();
        }

        public override void Unload()
        {
            Registry.Clear();
        }

        public static void RegisterCategory(CustomCategory category)
        {
            Registry.Add(category);
        }

        private void Detour_InitActiveCategories(orig_InitActiveCategories orig)
        {
            orig();
            InjectCategoriesDirectly();
        }

        private bool Detour_PassesFilter(orig_PassesFilter orig, UICategoryFilterBar self, int itemType)
        {
            EnsureEnabledArraySize(self);
            return orig(self, itemType);
        }

        private void InjectCategoriesDirectly()
        {
            Type type = typeof(UICategoryFilterBar);
            var activeCategories = GetFieldValue<List<ItemCategory>>(type, "_activeCategories");
            var activeCategoryIcons = GetFieldValue<List<int>>(type, "_activeCategoryIcons");
            var activeCategoryTooltips = GetFieldValue<List<string>>(type, "_activeCategoryTooltips");

            if (activeCategories == null || activeCategoryIcons == null || activeCategoryTooltips == null)
                return;

            foreach (var cat in Registry)
            {
                if (!cat.IsLoaded)
                {
                    cat.Resolve(Mod);
                }

                if (!cat.IsLoaded)
                    continue;

                if (activeCategories.Contains(cat.CategoryId))
                    continue;

                int insertIndex = activeCategories.IndexOf(ItemCategory.OtherWeapons);
                if (insertIndex == -1)
                    insertIndex = activeCategories.Count;

                activeCategories.Insert(insertIndex, cat.CategoryId);
                activeCategoryIcons.Insert(insertIndex, cat.ResolvedIconId);
                activeCategoryTooltips.Insert(insertIndex, cat.Tooltip);
            }
        }

        private static void EnsureEnabledArraySize(UICategoryFilterBar instance)
        {
            var activeCategories = GetFieldValue<List<ItemCategory>>(typeof(UICategoryFilterBar), "_activeCategories");
            if (activeCategories == null)
                return;

            FieldInfo enabledField = typeof(UICategoryFilterBar).GetField("_enabled", BindingFlags.NonPublic | BindingFlags.Instance);
            if (enabledField?.GetValue(instance) is bool[] enabled)
            {
                if (enabled.Length < activeCategories.Count)
                {
                    int oldLen = enabled.Length;
                    Array.Resize(ref enabled, activeCategories.Count);
                    for (int i = oldLen; i < enabled.Length; i++)
                    {
                        enabled[i] = true;
                    }
                    enabledField.SetValue(instance, enabled);
                }
            }
        }

        private static ItemCategory Detour_ClassifyItemInstance(orig_ClassifyItemInstance orig, Item item)
        {
            if (item.damage > 0 || (item.useStyle > ItemUseStyleID.None && item.shoot > ProjectileID.None && item.DamageType != DamageClass.Default))
            {
                foreach (var cat in Registry)
                {
                    if (!cat.IsLoaded)
                        continue;

                    foreach (var dc in cat.ResolvedClasses)
                    {
                        if (item.DamageType == dc || item.DamageType.CountsAsClass(dc))
                        {
                            return cat.CategoryId;
                        }
                    }
                }
            }

            return orig(item);
        }

        private static T GetFieldValue<T>(Type type, string fieldName) where T : class
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null) as T;
        }
    }
}