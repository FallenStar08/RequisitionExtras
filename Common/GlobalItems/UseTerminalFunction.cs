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
    public class UseTerminalFunction : GlobalItem
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
                            var disks = terminal.GetConnectedDiskIds();
                            for (int i = 10; i < 50; i++)
                            {
                                if (!player.inventory[i].IsAir && !player.inventory[i].favorited && player.inventory[i].ModItem is not RemoteTerminal)
                                {
                                    var storagePlayer = Main.LocalPlayer.GetModPlayer<ModPlayers.TerraStorageOverflow>();
                                    _ = storagePlayer.DepositIntoAllNetworks(player.inventory[i]);
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
            // Check if an active dialogue/chest/sign interface is already open
            if (player.talkNPC >= 0 || player.chest >= 0 || player.sign >= 0)
                return true;

            if (player.cursorItemIconEnabled)
                return true;

            Vector2 mouseWorld = Main.MouseWorld;

            // Check if hovering over an interactable NPC
            Rectangle mouseRect = new((int)mouseWorld.X, (int)mouseWorld.Y, 1, 1);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.Hitbox.Intersects(mouseRect) && (npc.townNPC || npc.isLikeATownNPC))
                {
                    return true;
                }
            }

            // Check if hovering over an interactable Tile
            int tileX = (int)(mouseWorld.X / 16f);
            int tileY = (int)(mouseWorld.Y / 16f);

            if (WorldGen.InWorld(tileX, tileY, 10))
            {
                Tile tile = Main.tile[tileX, tileY];
                if (tile.HasTile && player.IsInTileInteractionRange(tileX, tileY, TileReachCheckSettings.Simple))
                {
                    ushort type = tile.TileType;

                    if (Main.tileContainer[type] ||
                        type == TileID.PiggyBank ||
                        type == TileID.Safes ||
                        type == TileID.DefendersForge ||
                        type == TileID.VoidVault ||
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