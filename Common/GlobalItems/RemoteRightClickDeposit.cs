using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerraStorage.Content.Items;
using TerraStorage.Content.Tiles;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Common.GlobalItems
{
    public class RemoteRightClickDeposit : GlobalItem
    {
        public override void HoldItem(Item item, Player player)
        {
            if (player.whoAmI != Main.myPlayer) return;

            if (item.ModItem is RemoteTerminal rt)
            {
                if (Main.mouseRight && Main.mouseRightRelease && !Main.LocalPlayer.mouseInterface)
                {
                    if (IsHoveringInteractable(player)) return;

                    if (!GetBoolSetting("EnableRightClickDeposit")) return;

                    if (rt.BoundEntityId != -1 && TileEntity.ByID.TryGetValue(rt.BoundEntityId, out var te))
                    {
                        if (te is TerminalEntity terminal)
                        {
                            var storagePlayer = Main.LocalPlayer.GetModPlayer<ModPlayers.TerraStorageOverflow>();

                            for (int i = 10; i < 50; i++)
                            {
                                Item invItem = player.inventory[i];
                                if (!invItem.IsAir && !invItem.favorited && invItem.ModItem is not RemoteTerminal)
                                {
                                    _ = storagePlayer.DepositIntoAllNetworks(invItem, false);
                                }
                            }
                            SoundEngine.PlaySound(SoundID.Grab, null, null);
                            Loggers.Log("Deposited all items via right click", Color.MediumPurple);
                        }
                    }
                    else
                    {
                        if (Main.GameUpdateCount % 60 == 0)
                            Loggers.Log("Remote is not bound.", Color.Red);
                    }
                }
            }
        }

        private static bool IsHoveringInteractable(Player player)
        {
            if (player.talkNPC >= 0 || player.chest >= 0 || player.sign >= 0)
                return true;

            if (player.cursorItemIconEnabled)
                return true;

            Vector2 mouseWorld = Main.MouseWorld;

            Rectangle mouseRect = new((int)mouseWorld.X, (int)mouseWorld.Y, 1, 1);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.Hitbox.Intersects(mouseRect) && (npc.townNPC || npc.isLikeATownNPC))
                {
                    return true;
                }
            }

            int tileX = (int)(mouseWorld.X / 16f);
            int tileY = (int)(mouseWorld.Y / 16f);

            if (WorldGen.InWorld(tileX, tileY, 10))
            {
                Tile tile = Main.tile[tileX, tileY];
                if (tile.HasTile && player.IsInTileInteractionRange(tileX, tileY, TileReachCheckSettings.Simple))
                {
                    ushort type = tile.TileType;

                    if (Main.tileContainer[type] ||
                        Main.tileSign[type] ||
                        TileID.Sets.IsAContainer[type] ||
                        TileID.Sets.InteractibleByNPCs[type] ||
                        TileID.Sets.AvoidedByNPCs[type])
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}