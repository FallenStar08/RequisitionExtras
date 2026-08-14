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
                if (Main.mouseRight && Main.mouseRightRelease && !Main.LocalPlayer.mouseInterface)
                {
                    if (rt.BoundEntityId != -1 && TileEntity.ByID.TryGetValue(rt.BoundEntityId, out var te))
                    {
                        if (te is TerminalEntity terminal)
                        {

                            var disks = terminal.GetConnectedDiskIds();
                            for (int i = 10; i < 50; i++)
                            {
                                if (!player.inventory[i].IsAir && !player.inventory[i].favorited && player.inventory[i].ModItem is not RemoteTerminal)
                                {
                                    var storagePlayer = Main.LocalPlayer.GetModPlayer<TerraStorageOverflow.Common.ModPlayers.TerraStorageOverflow>();
                                    bool success = storagePlayer.DepositIntoAllNetworks(player.inventory[i]);
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
}
