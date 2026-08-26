using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace TerraStorageOverflow.Common.Utils.Items
{
    internal class TooltipsUtils
    {
        public static string GetRawTooltipText(Item item)
        {
            List<string> textPieces = [];

            if (item.ModItem != null)
            {
                var mainTooltip = item.ModItem.GetLocalization("Tooltip");
                if (mainTooltip != null && !string.IsNullOrEmpty(mainTooltip.Value))
                {
                    textPieces.Add(mainTooltip.Value);
                }
            }

            return string.Join("\n", textPieces);
        }
    }
}
