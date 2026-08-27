using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using TerraStorageOverflow.Common.Utils;
using TerraStorageOverflow.Common.Utils.Players;
using TerraStorageOverflow.Common.Utils.Reflection;

namespace TerraStorageOverflow.Common.Hooks
{
    internal class ChestUIHookSystem : ModSystem
    {
        private static bool DepositedAtLeastOneItem;

        public override void Load()
        {
            MethodInfo lootAll = Reflect.Method(typeof(ChestUI), "LootAll");
            if (lootAll != null)
            {
                MonoModHooks.Add(lootAll, Detour_LootAll);
                Loggers.Log("Hooked ChestUI.LootAll successfully.");
            }
        }

        private void Detour_LootAll(Action orig)
        {
            orig();

            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<ModPlayers.TerraStorageOverflowPlayer>();

            if (!modPlayer.HasActiveStorage)
                return;

            int chestIndex = player.chest;
            if (chestIndex <= -1)
                return;

            Item[] chestInv =
                chestIndex == -2 ? player.bank.item
                : chestIndex == -3 ? player.bank2.item
                : chestIndex == -4 ? player.bank3.item
                : chestIndex == -5 ? player.bank4.item
                : Main.chest[chestIndex].item;

            for (int i = 0; i < chestInv.Length; i++)
            {
                Item item = chestInv[i];

                if (!item.IsAir && !InventoryUtils.HasRoomForItem(item))
                {
                    Loggers.Log($"Loot All Overflow: {item.Name} -> Storage.", Color.Orange);

                    if (modPlayer.RemoteCache.DepositIntoAllNetworks(item))
                    {
                        DepositedAtLeastOneItem = true;
                        chestInv[i] = new Item();

                        if (Main.netMode == NetmodeID.MultiplayerClient && chestIndex >= 0)
                        {
                            NetMessage.SendData(
                                MessageID.SyncChestItem,
                                -1,
                                -1,
                                null,
                                chestIndex,
                                i
                            );
                        }
                    }
                }
            }

            if (DepositedAtLeastOneItem)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                DepositedAtLeastOneItem = false;
            }
        }
    }
}
