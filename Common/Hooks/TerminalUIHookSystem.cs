using Terraria.ModLoader;
using TerraStorage.Content.Tiles;
using TerraStorage.Content.UI;
using TerraStorageOverflow.Common.Utils;
using static TerraStorageOverflow.Common.Utils.Reflection.DetourHelpers;

namespace TerraStorageOverflow.Common.Hooks
{
    internal class TerminalUIHookSystem : ModSystem
    {
        public override void Load()
        {
            Detour<TerminalUISystem, TerminalEntity>(
                "OpenTerminal",
                (orig, self, entity) =>
                {
                    if (GetBoolSetting("EnableAutoRestackOnTerminalOpen"))
                    {
                        var connectedDiskIds = entity.GetConnectedDiskIds();
                        if (connectedDiskIds.Count > 0)
                        {
                            StorageNetworkHelper.ConsolidateStacks(connectedDiskIds);
                        }
                    }
                    orig(self, entity);
                }
            );
        }
    }
}
