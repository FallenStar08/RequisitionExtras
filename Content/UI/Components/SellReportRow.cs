using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Chat;
using Terraria.UI;
using TerraStorageOverflow.Common.Utils.UI;
using TerraStorageOverflow.Content.UI.Services;

namespace TerraStorageOverflow.Content.UI.Components
{
    /// <summary>
    /// my cute report row for each item sold, with item icon and text :3
    /// </summary>
    internal class SellReportRow : UIElement
    {
        private SellReportEntry _entry;

        public SellReportRow(SellReportEntry entry)
        {
            _entry = entry;
            Width.Set(0f, 1f);
            Height.Set(16f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Vector2 pos = GetDimensions().Position();

            string itemTag = ItemTagHandler.GenerateTag(_entry.ItemSample);
            string lineText =
                $"{itemTag} x{_entry.Count} - {UIUtils.FormatCoinString(_entry.TotalValue)}";

            UIUtils.DrawTextWithTags(
                spriteBatch,
                lineText,
                pos + new Vector2(10f, 0f),
                Color.White,
                0.75f
            );
        }
    }
}
