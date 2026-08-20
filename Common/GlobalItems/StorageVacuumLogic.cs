using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Common.GlobalItems
{
    public class StorageVacuumLogic : GlobalItem
    {
        public override bool ItemSpace(Item item, Player player)
        {
            return InventoryUtils.IsInstantPickup(item)
                ? base.ItemSpace(item, player)
                : player.GetModPlayer<ModPlayers.TerraStorageOverflowPlayer>().HasActiveStorage
                    || base.ItemSpace(item, player);
        }

        public override bool GrabStyle(Item item, Player player)
        {
            var modPlayer = player.GetModPlayer<ModPlayers.TerraStorageOverflowPlayer>();
            if (player.whoAmI == Main.myPlayer && modPlayer.HasActiveStorage)
            {
                if (!InventoryUtils.HasRoomForItem(item) && !InventoryUtils.IsInstantPickup(item))
                {
                    Vector2 toPlayer = player.Center - item.Center;
                    float distSq = toPlayer.LengthSquared();

                    int grabRange = player.GetItemGrabRange(item);
                    float rangeSq = (float)grabRange * grabRange;

                    if (distSq < rangeSq)
                    {
                        toPlayer.Normalize();
                        item.velocity = toPlayer * (12f + player.moveSpeed);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
