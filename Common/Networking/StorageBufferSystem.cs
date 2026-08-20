using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerraStorage.Systems;

namespace TerraStorageOverflow.Common.Networking
{
    public class StorageBufferSystem : ModSystem
    {
        private static readonly Dictionary<List<Guid>, Dictionary<int, int>> _buffers = new(
            new GuidListComparer()
        );
        private int _timer;

        public static void AddToBuffer(List<Guid> networkGuids, Item item)
        {
            if (networkGuids == null || networkGuids.Count == 0 || item == null || item.IsAir)
                return;

            if (!_buffers.TryGetValue(networkGuids, out var itemBuffer))
            {
                itemBuffer = [];
                _buffers[networkGuids.ToList()] = itemBuffer;
            }

            itemBuffer[item.type] = itemBuffer.GetValueOrDefault(item.type, 0) + item.stack;
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
            var mod = ModLoader.GetMod("TerraStorage");

            foreach (var (networkGuids, itemBuffer) in _buffers)
            {
                foreach (var (itemType, totalAmount) in itemBuffer)
                {
                    int remaining = totalAmount;

                    while (remaining > 0)
                    {
                        Item dummy = new();
                        dummy.SetDefaults(itemType);

                        int toSend = Math.Min(remaining, dummy.maxStack);
                        dummy.stack = toSend;
                        remaining -= toSend;

                        NetworkHandler.SendDepositItem(mod, networkGuids, dummy);
                    }
                }
            }

            _buffers.Clear();
        }

        private sealed class GuidListComparer : IEqualityComparer<List<Guid>>
        {
            public bool Equals(List<Guid>? x, List<Guid>? y)
            {
                return ReferenceEquals(x, y)
                    || (x is not null && y is not null && x.SequenceEqual(y));
            }

            public int GetHashCode(List<Guid> obj)
            {
                HashCode hash = new();
                foreach (var guid in obj)
                    hash.Add(guid);
                return hash.ToHashCode();
            }
        }
    }
}
