using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using TerraStorage.Common;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Content.UI.Services
{
    public static class StationEjector
    {
        public static void EjectStation(Item item, List<DiskData> disks)
        {
            if (item == null || item.IsAir)
                return;

            string itemName = item.Name;

            foreach (DiskData disk in disks)
            {
                if (disk?.Items == null)
                    continue;

                try
                {
                    disk.InsertItem(item);
                    if (item == null || item.IsAir || item.stack <= 0)
                    {
                        Loggers.Log(
                            $"Returned replaced station '{itemName}' back to storage disks.",
                            Color.LightBlue
                        );
                        return;
                    }
                }
                catch { }
            }

            Player player = Main.LocalPlayer;
            if (player != null && player.active)
            {
                item = player.GetItem(
                    Main.myPlayer,
                    item,
                    GetItemSettings.InventoryEntityToPlayerInventorySettings
                );
                if (item == null || item.IsAir || item.stack <= 0)
                {
                    Loggers.Log(
                        $"Placed replaced station '{itemName}' into player inventory.",
                        Color.LightBlue
                    );
                    return;
                }

                player.QuickSpawnItem(
                    player.GetSource_Misc("CraftingCoreStationInserter"),
                    item,
                    item.stack
                );
                Loggers.Log(
                    $"Storage and inventory full: Dropped replaced station '{itemName}' on the ground.",
                    Color.Orange
                );
            }
        }
    }
}
