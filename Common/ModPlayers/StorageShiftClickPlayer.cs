using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using TerraStorageOverflow.Common.Utils;
using TerraStorageOverflow.Common.Utils.Players;

namespace TerraStorageOverflow.Common.ModPlayers
{
    public class StorageShiftClick : ModPlayer
    {
        public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
        {
            //should probably check for other inv slots or maybe just trigger the logic for chest slots
            //Idk it's fine I think.
            if (context == ItemSlot.Context.InventoryItem)
                return false;
            Player player = Main.LocalPlayer;
            Item item = inventory[slot];

            if (item.IsAir || item.favorited)
                return false;

            var modPlayer = player.GetModPlayer<TerraStorageOverflowPlayer>();

            Loggers.Log(
                $"HasActiveStorage: {modPlayer.HasActiveStorage} | HasRoom: {InventoryUtils.HasRoomForItem(item)}"
            );
            if (modPlayer.HasActiveStorage && !InventoryUtils.HasRoomForItem(item))
            {
                Loggers.Log(
                    $"Inventory full, shift-clicking {item.Name} to storage.",
                    Color.Orange
                );
                if (modPlayer.RemoteCache.DepositIntoAllNetworks(item))
                {
                    inventory[slot] = new Item();
                    SoundEngine.PlaySound(SoundID.Grab);
                    Recipe.FindRecipes();
                    return true;
                }
            }

            return base.ShiftClickSlot(inventory, context, slot);
        }
    }
}
