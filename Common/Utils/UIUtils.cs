using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace TerraStorageOverflow.Common.Utils
{
    public static class UIUtils
    {
        /// <summary>
        /// Uses Terraria's ChatManager to draw text with color tags and item icons.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="scale"></param>
        public static void DrawTextWithTags(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 0.75f)
        {
            List<TextSnippet> snippets = ChatManager.ParseMessage(text, color);

            // Native ChatManager draw call (baseScale must be Vector2)
            ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch,
                FontAssets.MouseText.Value,
                snippets.ToArray(),
                position,
                0f, // rota
                color,
                Vector2.Zero,        // origin
                new Vector2(scale),
                out int hoveredSnippet
            );

            // Displays vanilla item hover tooltip when mouse is over an item icon
            if (hoveredSnippet >= 0 && hoveredSnippet < snippets.Count)
            {
                snippets[hoveredSnippet].OnHover();
            }
        }
    }
}