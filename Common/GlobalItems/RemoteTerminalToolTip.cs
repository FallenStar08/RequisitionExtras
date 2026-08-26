using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TerraStorage.Content.Items;
using TerraStorage.Helpers;
using TerraStorage.Systems;
using TerraStorageOverflow.Common.Systems;

namespace TerraStorageOverflow.Common.GlobalItems
{
    internal class RemoteTerminalToolTip : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.ModItem is RemoteTerminal rt)
            {
                if (
                    rt.BoundEntityId != -1
                    && TileEntity.ByID.TryGetValue(rt.BoundEntityId, out var te)
                )
                {
                    string posText =
                        $"[c/AAAAAA:{EasyLoca.RemoteTooltipBound} ({te.Position.X}, {te.Position.Y})]";
                    tooltips.Add(new TooltipLine(Mod, "RemotePos", posText));
                    if (Main.HoverItem == item)
                    {
                        TerminalLocationSystem.TargetTerminal = te.Position;
                    }

                    var diskIds = StorageNetwork.GetAllConnectedDiskIds(te.Position);
                    if (diskIds != null && diskIds.Count > 0)
                    {
                        long used = 0;
                        long total = 0;

                        foreach (var id in diskIds)
                        {
                            var diskData = StorageWorldSystem.Instance.GetDiskData(id);
                            if (diskData != null)
                            {
                                used += diskData.UsedStacks;
                                total += diskData.MaxStacks;
                            }
                        }

                        string capacityText =
                            $"[c/66FF66:{EasyLoca.RemoteTooltipCapacity} {used:N0} / {total:N0}]";

                        float fillRatio = total > 0 ? (float)used / total : 0;
                        if (fillRatio >= 0.9f)
                            capacityText = capacityText.Replace("66FF66", "FF4444");
                        else if (fillRatio >= 0.75f)
                            capacityText = capacityText.Replace("66FF66", "FFFF66");

                        tooltips.Add(new TooltipLine(Mod, "RemoteCapacity", capacityText));
                    }
                }
                else
                {
                    tooltips.Add(
                        new TooltipLine(
                            Mod,
                            "RemoteUnbound",
                            $"[c/FF4444:{EasyLoca.RemoteTooltipUnbound}]"
                        )
                    );
                }
            }
        }
    }
}
