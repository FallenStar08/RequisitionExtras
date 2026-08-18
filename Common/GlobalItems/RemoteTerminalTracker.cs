using Terraria;
using Terraria.ModLoader;
using TerraStorage.Content.Items;

namespace TerraStorageOverflow.Common.GlobalItems
{
    public class RemoteTerminalTracker : GlobalItem
    {
        public override void UpdateInventory(Item item, Player player)
        {
            if (player.whoAmI == Main.myPlayer && item.ModItem is RemoteTerminal)
            {
                player.GetModPlayer<ModPlayers.TerraStorageOverflow>().ReportRemoteFound();
            }
        }
    }
}
