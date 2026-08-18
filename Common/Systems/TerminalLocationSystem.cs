using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace TerraStorageOverflow.Common.Systems
{
    public class TerminalLocationSystem : ModSystem
    {
        public static Point16? TargetTerminal;

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (TargetTerminal == null)
                return;

            Point16 pos = TargetTerminal.Value;
            if (!WorldGen.InWorld(pos.X, pos.Y))
                return;

            Tile tile = Main.tile[pos.X, pos.Y];
            if (!tile.HasTile)
                return;

            int width = 1;
            int height = 1;
            TileObjectData data = TileObjectData.GetTileData(tile.TileType, 0);
            if (data != null)
            {
                width = data.Width;
                height = data.Height;
            }

            Vector2 worldPos = pos.ToVector2() * 16f;
            Vector2 screenPos = Vector2.Transform(
                worldPos - Main.screenPosition,
                Main.GameViewMatrix.ZoomMatrix
            );

            float uiScale = Main.UIScale;
            screenPos /= uiScale;

            float zoom = Main.GameViewMatrix.Zoom.X;
            int pixelWidth = (int)(width * 16 * zoom / uiScale);
            int pixelHeight = (int)(height * 16 * zoom / uiScale);

            Rectangle rect = new((int)screenPos.X, (int)screenPos.Y, pixelWidth, pixelHeight);

            int inflation = (int)(4 * zoom / uiScale);
            rect.Inflate(inflation, inflation);

            Color boxColor = Color.Cyan * 0.8f;
            int thickness = (int)Math.Max(2, 2 * zoom / uiScale);
            DrawBorderedRect(spriteBatch, rect, boxColor, thickness);

            TargetTerminal = null;
        }

        private static void DrawBorderedRect(
            SpriteBatch sb,
            Rectangle rect,
            Color color,
            int thickness
        )
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            sb.Draw(
                pixel,
                new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness),
                color
            );
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            sb.Draw(
                pixel,
                new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height),
                color
            );
        }
    }
}
