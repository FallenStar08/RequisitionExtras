using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Common.ModPlayers
{
    public class StorageShiftClick : ModPlayer
    {
        public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
        {
            if (context == ItemSlot.Context.InventoryItem)
                return false;
            Player player = Main.LocalPlayer;
            Item item = inventory[slot];

            if (item.IsAir || item.favorited)
                return false;

            var modPlayer = player.GetModPlayer<TerraStorageOverflow>();

            Loggers.Log(
                $"HasActiveStorage: {modPlayer.HasActiveStorage} | HasRoom: {InventoryUtils.HasRoomForItem(item)}"
            );
            if (modPlayer.HasActiveStorage && !InventoryUtils.HasRoomForItem(item))
            {
                Loggers.Log(
                    $"Inventory full, shift-clicking {item.Name} to storage.",
                    Color.Orange
                );
                if (modPlayer.DepositIntoAllNetworks(item))
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
