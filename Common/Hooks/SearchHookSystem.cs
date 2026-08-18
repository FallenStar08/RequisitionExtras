//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Terraria;
//using Terraria.ModLoader;
//using TerraStorage.Content.UI.Elements;

//namespace TerraStorageOverflow.Common.Systems
//{
//    public class SearchHookSystem : ModSystem
//    {
//        private static readonly Dictionary<int, string> _customCache = [];

//        public override void Load()
//        {
//            var getTooltip = typeof(ItemSearchHelper).GetMethod("GetTooltip", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
//            if (getTooltip != null) MonoModHooks.Add(getTooltip, Detour_GetTooltip);
//        }

//        public override void OnModUnload()
//        {
//            _customCache.Clear();
//        }

//        private delegate string orig_GetTooltip(int itemType);

//        private static string Detour_GetTooltip(orig_GetTooltip orig, int itemType)
//        {
//            if (_customCache.TryGetValue(itemType, out string cached))
//            {
//                return cached;
//            }

//            string result = GenerateSearchString(itemType);
//            _customCache[itemType] = result;
//            return result;
//        }

//        private static string GenerateSearchString(int itemType)
//        {
//            Item item = new();
//            item.SetDefaults(itemType);

//            StringBuilder sb = new();
//            sb.Append(item.Name).Append(' ');

//            var richLines = GetItemTooltipLines(item);
//            foreach (string line in richLines)
//            {
//                sb.Append(line).Append(' ');
//            }

//            if (item.damage > 0) sb.Append(item.DamageType.Name.Replace("DamageClass", "")).Append(' ');
//            if (item.material) sb.Append("material ");

//            return sb.ToString().ToLower();
//        }

//        private static List<string> GetItemTooltipLines(Item item)
//        {
//            int yoyoLogo = -1;
//            int researchLine = -1;
//            float oldKB = item.knockBack;
//            int maxLines = 30;
//            int numLines = 1;

//            string[] toolTipLine = new string[maxLines];
//            bool[] preFixLine = new bool[maxLines];
//            bool[] badPreFixLine = new bool[maxLines];
//            string[] toolTipNames = new string[maxLines];

//            Main.MouseText_DrawItemTooltip_GetLinesInfo(item, ref yoyoLogo, ref researchLine, oldKB, ref numLines, toolTipLine, preFixLine, badPreFixLine, toolTipNames, out int prefixlineIndex);

//            List<TooltipLine> lines = ItemLoader.ModifyTooltips(item, ref numLines, toolTipNames, ref toolTipLine, ref preFixLine, ref badPreFixLine, ref yoyoLogo, out _, prefixlineIndex);

//            return [.. lines.Select(line => line.Text)];
//        }
//    }
//}
