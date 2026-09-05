using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerraStorage.Content.Items;

namespace TerraStorageOverflow.Common.GlobalItems
{
    internal class RemoteTerminalBindingFix : GlobalItem
    {
        public Point16 SavedPosition = new(-1, -1);

        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type == ModContent.ItemType<RemoteTerminal>();
        }

        public override void SaveData(Item item, TagCompound tag)
        {
            if (item.ModItem is RemoteTerminal remote && remote.BoundEntityId >= 0)
            {
                if (TileEntity.ByID.TryGetValue(remote.BoundEntityId, out var te))
                {
                    tag["REQX_boundX"] = te.Position.X;
                    tag["REQX_boundY"] = te.Position.Y;
                }
            }
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            if (tag.ContainsKey("REQX_boundX") && tag.ContainsKey("REQX_boundY"))
            {
                SavedPosition = new Point16(
                    tag.GetShort("REQX_boundX"),
                    tag.GetShort("REQX_boundY")
                );
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (
                item.ModItem is RemoteTerminal remote
                && SavedPosition.X >= 0
                && SavedPosition.Y >= 0
            )
            {
                if (TileEntity.ByPosition.TryGetValue(SavedPosition, out var te))
                {
                    remote.BoundEntityId = te.ID;
                }
            }
            return base.CanUseItem(item, player);
        }

        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(SavedPosition.X);
            writer.Write(SavedPosition.Y);
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            SavedPosition = new Point16(reader.ReadInt16(), reader.ReadInt16());
        }
    }
}
