using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace TerraStorageOverflow.Common.Utils.Items
{
    internal static class RecipeUtils
    {
        public static bool IsTileUsedInAnyRecipe(int tileType)
        {
            if (tileType < TileID.Dirt)
                return false;

            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe == null || recipe.Disabled)
                    continue;

                if (recipe.requiredTile != null && recipe.requiredTile.Contains(tileType))
                    return true;
            }

            return false;
        }

        public static bool IsStationItemUsedInAnyRecipe(Item item)
        {
            if (item == null || item.IsAir)
                return false;

            // Bypass recipe scan if the item provides a special condition (Water, Lava, Honey, Graveyard, Snow)
            object condition = ItemConditionsUtils.GetItemCondition(item.type);
            if (!ItemConditionsUtils.IsNoneCondition(condition))
                return true;

            if (item.createTile < TileID.Dirt)
                return false;

            // Check ALL tiles provided/inherited by this station (handles AdjTiles & Combined Stations)
            HashSet<int> providedTiles = TileResolverUtils.GetProvidedTiles(item.createTile);
            foreach (int tile in providedTiles)
            {
                if (IsTileUsedInAnyRecipe(tile))
                    return true;
            }

            return false;
        }
    }
}
