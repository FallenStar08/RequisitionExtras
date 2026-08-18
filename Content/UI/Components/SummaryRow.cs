using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Content.UI.Components
{
    /// <summary>
    /// cute lil summary row at the bottom of the report panel
    /// </summary>
    public class SummaryRow : UIElement
    {
        private string _text = EasyLoca.ReportNoItems;
        private Color _color = Color.LightGray;

        public void SetText(string text, Color color)
        {
            _text = text;
            _color = color;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            Vector2 pos = GetDimensions().Position();
            UIUtils.DrawTextWithTags(spriteBatch, _text, pos, _color, 0.75f);
        }
    }
}
