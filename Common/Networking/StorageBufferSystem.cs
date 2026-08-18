using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerraStorage.Systems;

namespace TerraStorageOverflow.Common.Networking
{
    public class StorageBufferSystem : ModSystem
    {
        private static Dictionary<Guid, Dictionary<int, int>> _buffers = [];
        private int _timer;

        public static void AddToBuffer(Guid networkId, Item item)
        {
            if (!_buffers.ContainsKey(networkId))
                _buffers[networkId] = [];
            if (!_buffers[networkId].ContainsKey(item.type))
                _buffers[networkId][item.type] = 0;

            _buffers[networkId][item.type] += item.stack;
        }

        public override void PostUpdateWorld()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || _buffers.Count == 0)
                return;

            _timer++;
            if (_timer >= 30)
            {
                Flush();
                _timer = 0;
            }
        }

        private static void Flush()
        {
            foreach (var networkEntry in _buffers)
            {
                Guid networkId = networkEntry.Key;
                var itemBuffer = networkEntry.Value;

                foreach (var itemEntry in itemBuffer)
                {
                    int itemType = itemEntry.Key;
                    int remaining = itemEntry.Value;

                    while (remaining > 0)
                    {
                        Item dummy = new();
                        dummy.SetDefaults(itemType);

                        int toSend = Math.Min(remaining, dummy.maxStack);
                        dummy.stack = toSend;
                        remaining -= toSend;

                        var targetNetwork = new List<Guid> { networkId };
                        NetworkHandler.SendDepositItem(
                            ModLoader.GetMod("TerraStorage"),
                            targetNetwork,
                            dummy
                        );
                    }
                }

                itemBuffer.Clear();
            }
            _buffers.Clear();
        }
    }
}
