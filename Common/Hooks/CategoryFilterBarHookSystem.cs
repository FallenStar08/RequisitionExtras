using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TerraStorage.Content.UI.Elements;
using TerraStorageOverflow.Common.Utils;
using TerraStorageOverflow.Common.Utils.Items;

namespace TerraStorageOverflow.Common.Hooks
{
    /// <summary>
    /// Creates a new custom category for the item filter bar.
    /// </summary>
    public class CustomCategory(
        Func<string> tooltipGetter,
        ItemCategory? categoryId = null,
        string modName = null,
        string[] damageClassNames = null,
        string iconItemName = null,
        int fallbackIconId = ItemID.Cherry,
        ItemCategory targetAnchorCategory = ItemCategory.OtherWeapons,
        bool insertAfter = false,
        Func<Item, bool> customMatcher = null
    )
    {
        private static int _nextAutoId = 100;

        private readonly Func<string> _tooltipGetter = tooltipGetter;

        public ItemCategory CategoryId { get; } = categoryId ?? (ItemCategory)_nextAutoId++;
        public string ModName { get; } = modName;
        public string[] DamageClassNames { get; } = damageClassNames ?? [];
        public string IconItemName { get; } = iconItemName;
        public int FallbackIconId { get; } = fallbackIconId;

        /// <summary>
        /// Evaluates the tooltip lazily so localization keys resolve properly at UI build time.
        /// </summary>
        public string Tooltip => _tooltipGetter?.Invoke() ?? string.Empty;

        public ItemCategory TargetAnchorCategory { get; set; } = targetAnchorCategory;
        public bool InsertAfter { get; set; } = insertAfter;
        public Func<Item, bool> CustomMatcher { get; set; } = customMatcher;

        public List<DamageClass> ResolvedClasses { get; } = [];
        public int ResolvedIconId { get; set; }
        public bool IsLoaded { get; private set; }

        // Convenience overload accepting plain string
        public CustomCategory(
            string tooltip,
            ItemCategory? categoryId = null,
            string modName = null,
            string[] damageClassNames = null,
            string iconItemName = null,
            int fallbackIconId = ItemID.Cherry,
            ItemCategory targetAnchorCategory = ItemCategory.OtherWeapons,
            bool insertAfter = false,
            Func<Item, bool> customMatcher = null
        )
            : this(
                () => tooltip,
                categoryId,
                modName,
                damageClassNames,
                iconItemName,
                fallbackIconId,
                targetAnchorCategory,
                insertAfter,
                customMatcher
            ) { }

        public void Resolve(Mod hostMod)
        {
            if (IsLoaded)
                return;

            ResolvedClasses.Clear();

            if (string.IsNullOrEmpty(ModName))
            {
                IsLoaded = true;
                ResolvedIconId = FallbackIconId;
                return;
            }

            if (!ModLoader.TryGetMod(ModName, out Mod targetMod))
            {
                Loggers.Log($"[CustomCategory] Mod '{ModName}' was not found/loaded.");
                return;
            }

            foreach (string className in DamageClassNames)
            {
                if (targetMod.TryFind<DamageClass>(className, out var dc))
                {
                    ResolvedClasses.Add(dc);
                    Loggers.Log(
                        $"[CustomCategory] Successfully resolved DamageClass '{className}' from {ModName}."
                    );
                }
                else
                {
                    Loggers.Log(
                        $"[CustomCategory] Could not find DamageClass '{className}' in {ModName}."
                    );
                }
            }

            ResolvedIconId =
                !string.IsNullOrEmpty(IconItemName)
                && targetMod.TryFind<ModItem>(IconItemName, out var item)
                    ? item.Type
                    : FallbackIconId;

            if (ResolvedClasses.Count > 0 || CustomMatcher != null)
            {
                IsLoaded = true;
                Loggers.Log(
                    $"[CustomCategory] Category '{Tooltip}' resolved with IconItemID: {ResolvedIconId}."
                );
            }
        }
    }

    [ExtendsFromMod("TerraStorage")]
    public class CategoryHookSystem : ModSystem
    {
        private static readonly List<CustomCategory> Registry = [];

        private static readonly HashSet<int> FishingAccessoryIds = [];

        private delegate void orig_InitActiveCategories();
        private delegate ItemCategory orig_ClassifyItemInstance(Item item);
        private delegate bool orig_PassesFilter(UICategoryFilterBar self, int itemType);

        public override void Load()
        {
            // CLICKER CLASS CATEGORY
            RegisterCategory(
                new CustomCategory(
                    tooltipGetter: () => EasyLoca.CategoryClicker,
                    modName: "ClickerClass",
                    damageClassNames: ["ClickerDamage"],
                    iconItemName: "TheClicker",
                    fallbackIconId: ItemID.GoldPickaxe,
                    targetAnchorCategory: ItemCategory.OtherWeapons,
                    insertAfter: false
                )
            );

            // CAPTURE DISC CLASS CATEGORY
            RegisterCategory(
                new CustomCategory(
                    tooltipGetter: () => EasyLoca.CategoryCaptureDisc,
                    modName: "CaptureDiscClass",
                    damageClassNames: ["CaptureDamage"],
                    iconItemName: "HighTest",
                    fallbackIconId: ItemID.DiscWall,
                    targetAnchorCategory: ItemCategory.OtherWeapons,
                    insertAfter: false
                )
            );

            if (GetBoolSetting("EnableFishingCategory"))
            {
                // FISHING CATEGORY (Vanilla + Modded rods & bait)
                RegisterCategory(
                    new CustomCategory(
                        tooltipGetter: () => EasyLoca.CategoryFishing,
                        fallbackIconId: ItemID.ReinforcedFishingPole,
                        targetAnchorCategory: ItemCategory.Tools,
                        insertAfter: true,
                        customMatcher: item =>
                            item.fishingPole > 0
                            || item.bait > 0
                            || item.type == ItemID.AnglerPants
                            || item.type == ItemID.AnglerVest
                            || item.type == ItemID.AnglerHat
                            || FishingAccessoryIds.Contains(item.type)
                    )
                );
            }

            if (GetBoolSetting("EnablePetsCategory"))
            {
                // PETS & MOUNTS CATEGORY
                RegisterCategory(
                    new CustomCategory(
                        tooltipGetter: () => EasyLoca.CategoryPets,
                        fallbackIconId: ItemID.CatMask,
                        targetAnchorCategory: ItemCategory.Miscellaneous,
                        insertAfter: false,
                        customMatcher: item =>
                        {
                            // sanity check to ignore summoner weapons
                            return item.damage <= 0
                                && item.DamageType == DamageClass.Default
                                && item.ammo == ItemID.None
                                && (
                                    item.mountType != -1
                                    || (
                                        item.buffType > 0
                                        && (
                                            Main.vanityPet[item.buffType]
                                            || Main.lightPet[item.buffType]
                                        )
                                    )
                                );
                        }
                    )
                );
            }

            Type targetType = typeof(UICategoryFilterBar);

            MethodInfo initMethod = Reflect.Method(targetType, "InitActiveCategories");
            MethodInfo classifyMethod = Reflect.Method(targetType, "ClassifyItemInstance");
            MethodInfo passesFilterMethod = Reflect.Method(targetType, "PassesFilter");

            if (initMethod == null)
            {
                Loggers.Error(
                    "[CategoryHookSystem] Failed to find InitActiveCategories method via Reflection!"
                );
            }
            else
            {
                MonoModHooks.Add(initMethod, Detour_InitActiveCategories);
                Mod.Logger.Info("[CategoryHookSystem] Hooked InitActiveCategories successfully.");
            }

            if (classifyMethod == null)
            {
                Loggers.Error(
                    "[CategoryHookSystem] Failed to find ClassifyItemInstance method via Reflection!"
                );
            }
            else
            {
                MonoModHooks.Add(classifyMethod, Detour_ClassifyItemInstance);
                Mod.Logger.Info("[CategoryHookSystem] Hooked ClassifyItemInstance successfully.");
            }

            if (passesFilterMethod == null)
            {
                Loggers.Error(
                    "[CategoryHookSystem] Failed to find PassesFilter method via Reflection!"
                );
            }
            else
            {
                MonoModHooks.Add(passesFilterMethod, Detour_PassesFilter);
                Mod.Logger.Info("[CategoryHookSystem] Hooked PassesFilter successfully.");
            }
        }

        public override void PostSetupContent()
        {
            if (GetBoolSetting("EnableFishingCategory"))
            {
                ScanFishingAccessories();
            }
            InjectCategoriesDirectly();
        }

        public override void Unload()
        {
            Registry.Clear();
            if (GetBoolSetting("EnableFishingCategory"))
            {
                FishingAccessoryIds.Clear();
            }
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

        private bool Detour_PassesFilter(
            orig_PassesFilter orig,
            UICategoryFilterBar self,
            int itemType
        )
        {
            EnsureEnabledArraySize(self);
            return orig(self, itemType);
        }

        private void InjectCategoriesDirectly()
        {
            Type type = typeof(UICategoryFilterBar);
            var activeCategories = Reflect.GetValue<List<ItemCategory>>(type, "_activeCategories");
            var activeCategoryIcons = Reflect.GetValue<List<int>>(type, "_activeCategoryIcons");
            var activeCategoryTooltips = Reflect.GetValue<List<string>>(
                type,
                "_activeCategoryTooltips"
            );

            if (
                activeCategories == null
                || activeCategoryIcons == null
                || activeCategoryTooltips == null
            )
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

                int targetIndex = activeCategories.IndexOf(cat.TargetAnchorCategory);
                int insertIndex =
                    targetIndex != -1
                        ? cat.InsertAfter
                            ? targetIndex + 1
                            : targetIndex
                        : activeCategories.Count;

                activeCategories.Insert(insertIndex, cat.CategoryId);
                activeCategoryIcons.Insert(insertIndex, cat.ResolvedIconId);

                activeCategoryTooltips.Insert(insertIndex, cat.Tooltip);
            }
        }

        private static void EnsureEnabledArraySize(UICategoryFilterBar instance)
        {
            var activeCategories = Reflect.GetValue<List<ItemCategory>>(
                typeof(UICategoryFilterBar),
                "_activeCategories"
            );
            if (activeCategories == null)
                return;

            FieldInfo enabledField = Reflect.Field(typeof(UICategoryFilterBar), "_enabled");
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

        private static ItemCategory Detour_ClassifyItemInstance(
            orig_ClassifyItemInstance orig,
            Item item
        )
        {
            if (item == null || item.IsAir)
                return orig(item);

            foreach (var cat in Registry)
            {
                if (!cat.IsLoaded)
                    continue;

                if (cat.CustomMatcher != null && cat.CustomMatcher(item))
                {
                    return cat.CategoryId;
                }

                if (
                    cat.ResolvedClasses.Count > 0
                    && (
                        item.damage > 0
                        || (
                            item.useStyle > ItemUseStyleID.None
                            && item.shoot > ProjectileID.None
                            && item.DamageType != DamageClass.Default
                        )
                    )
                )
                {
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

        // won't be as good in non english loca but idk how to retrive english tooltip text from other languages, so this is a best effort
        private static bool MatchesFishingRules(string tooltip)
        {
            if (string.IsNullOrWhiteSpace(tooltip))
                return false;

            // Game's official localized string for "Fishing Power"
            string localizedFishingPower = Language.GetTextValue("GameUI.FishingPower");

            bool Has(string term)
            {
                return tooltip.Contains(term, StringComparison.OrdinalIgnoreCase);
            }

            bool HasAny(params string[] terms)
            {
                return terms.Any(t => Has(t));
            }

            return Has(localizedFishingPower)
                || (Has("fish") && HasAny("power", "skill", "level", "yield", "catch", "speed"))
                || (Has("lava") && Has("fish"))
                || (Has("line") && HasAny("break", "snap"))
                || (
                    Has("bait")
                    && HasAny("chance", "consum", "save", "reduction", "power", "quality")
                )
                || Has("crate") // Any accessory mentioning crates should be fishing related (maybe?)
                || HasAny("tackle box", "angler tackle", "bobber", "sonar");
        }

        private static void AddModdedAccessory(string modName, string itemName)
        {
            if (ModContent.TryFind<ModItem>(modName, itemName, out var modItem))
            {
                FishingAccessoryIds.Add(modItem.Type);
            }
        }

        private static void ScanFishingAccessories()
        {
            FishingAccessoryIds.Clear();

            // Vanilla Fishing Accessories
            HashSet<int> baseFishingAccessories =
            [
                ItemID.FishingBobber,
                ItemID.AnglerEarring,
                ItemID.HighTestFishingLine,
                ItemID.TackleBox,
                ItemID.AnglerTackleBag,
                ItemID.LavaproofTackleBag,
                ItemID.LavaFishingHook,
                ItemID.FloatingTube,
            ];

            AddModdedAccessory("CalamityMod", "VolcanicSinker");
            AddModdedAccessory("CalamityMod", "SupremeBaitTackleBoxFishingStation");
            AddModdedAccessory("CalamityMod", "SunkenSinker");
            AddModdedAccessory("CalamityMod", "EnchantedPearl");
            AddModdedAccessory("CalamityMod", "FeralBobber");
            AddModdedAccessory("CalamityMod", "AcrobaticBobber");
            AddModdedAccessory("Clamity", "TreasureOfClamity");
            AddModdedAccessory("ImproveGame", "Autofisher");
            AddModdedAccessory("ImproveGame", "BaitSupplier");
            AddModdedAccessory("AutoFisher", "AnglerWhistle");

            foreach (int id in baseFishingAccessories)
            {
                FishingAccessoryIds.Add(id);
            }

            // Recipe Inheritance
            bool addedAny;
            do
            {
                addedAny = false;
                for (int i = 0; i < Recipe.maxRecipes; i++)
                {
                    Recipe recipe = Main.recipe[i];
                    if (recipe == null || recipe.Disabled || recipe.createItem.IsAir)
                        continue;

                    Item resultItem = recipe.createItem;
                    if (!resultItem.accessory || FishingAccessoryIds.Contains(resultItem.type))
                        continue;

                    foreach (Item ingredient in recipe.requiredItem)
                    {
                        if (
                            ingredient != null
                            && !ingredient.IsAir
                            && FishingAccessoryIds.Contains(ingredient.type)
                        )
                        {
                            if (FishingAccessoryIds.Add(resultItem.type))
                            {
                                addedAny = true;
                            }
                            break;
                        }
                    }
                }
            } while (addedAny);

            // Modded Tooltip Scanning (sadly english only)
            foreach (var (type, item) in ContentSamples.ItemsByType)
            {
                if (
                    item == null
                    || item.IsAir
                    || !item.accessory
                    || FishingAccessoryIds.Contains(type)
                )
                    continue;

                string fullTooltip = TooltipsUtils.GetRawTooltipText(item);

                if (MatchesFishingRules(fullTooltip))
                {
                    FishingAccessoryIds.Add(type);
                }
            }
        }
    }
}
