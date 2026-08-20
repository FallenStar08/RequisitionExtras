using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TerraStorage.Content.Items;
using TerraStorage.Content.Tiles;
using TerraStorage.Helpers;
using TerraStorage.Systems;
using TerraStorageOverflow.Common.Networking;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Common.Systems
{
    public class RemoteCache
    {
        private List<List<Guid>> _activeNetworks = [];

        // Reusable lists to avoid Garbage Collection (GC) allocations every frame
        private readonly List<int> _currentRemotes = [];
        private readonly List<int> _lastRemotes = [];

        public bool IsDirty { get; private set; } = true;
        public bool HasActiveStorage => _activeNetworks.Count > 0;

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void Update(Player player)
        {
            bool inventoryChanged = false;

            // 1. Lightweight, allocation-free check
            if (!IsDirty)
            {
                _currentRemotes.Clear();
                PopulateRemotesList(player, _currentRemotes);

                if (DidInventoryRemotesChange())
                {
                    inventoryChanged = true;
                }
            }

            // 2. Only do the heavy lifting if marked dirty externally OR if inventory changed
            if (IsDirty || inventoryChanged)
            {
                RefreshCache(player);
                IsDirty = false;

                // Sync our tracking list so we don't trigger false positives next frame
                _lastRemotes.Clear();
                PopulateRemotesList(player, _lastRemotes);
            }
        }

        private void PopulateRemotesList(Player player, List<int> list)
        {
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i].ModItem is RemoteTerminal rt && rt.BoundEntityId != -1)
                {
                    list.Add(rt.BoundEntityId);
                }
            }

            // Catch edge cases like holding the item or dropping it in the trash
            if (Main.mouseItem?.ModItem is RemoteTerminal mouseRt && mouseRt.BoundEntityId != -1)
                list.Add(mouseRt.BoundEntityId);
            if (player.trashItem?.ModItem is RemoteTerminal trashRt && trashRt.BoundEntityId != -1)
                list.Add(trashRt.BoundEntityId);
        }

        private bool DidInventoryRemotesChange()
        {
            if (_currentRemotes.Count != _lastRemotes.Count)
                return true;

            // Sort both lists to ensure order doesn't cause false positives
            // (Sorting 1-5 integers takes less than a nanosecond)
            _currentRemotes.Sort();
            _lastRemotes.Sort();

            for (int i = 0; i < _currentRemotes.Count; i++)
            {
                if (_currentRemotes[i] != _lastRemotes[i])
                    return true;
            }

            return false;
        }

        private void RefreshCache(Player player)
        {
            _activeNetworks.Clear();

            // Allocating here is perfectly fine because this only runs when IsDirty is true!
            var seenEntities = new HashSet<int>();

            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i].ModItem is RemoteTerminal rt && rt.BoundEntityId != -1)
                {
                    if (!seenEntities.Add(rt.BoundEntityId))
                        continue;

                    if (
                        TileEntity.ByID.TryGetValue(rt.BoundEntityId, out var te)
                        && te is TerminalEntity terminal
                    )
                    {
                        var diskIds = StorageNetwork.GetAllConnectedDiskIds(terminal.Position);
                        if (diskIds is { Count: > 0 })
                        {
                            _activeNetworks.Add(diskIds);
                        }
                    }
                }
            }

            Loggers.Log(
                $"Multi-Cache Refreshed: {_activeNetworks.Count} unique networks found.",
                Color.Cyan
            );
        }

        public bool DepositIntoAllNetworks(Item item, bool showPopupText = true)
        {
            if (!HasActiveStorage || item.IsAir)
                return false;

            int startStack = item.stack;

            foreach (var networkIds in _activeNetworks)
            {
                if (item.stack <= 0)
                    break;

                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    item.stack = StorageWorldSystem.Instance.InsertItem(networkIds, item);
                }
                else // Multiplayer
                {
                    StorageBufferSystem.AddToBuffer(networkIds, item);
                    item.stack = 0;
                }
            }

            if (item.stack < startStack)
            {
                int amountStored = startStack - item.stack;
                if (showPopupText)
                {
                    PopupText.NewText(
                        PopupTextContext.ItemPickupToVoidContainer,
                        item,
                        amountStored
                    );
                }
                return item.stack <= 0;
            }

            return false;
        }

        public bool HasItemInNetworks(int itemType, bool consumeIfPossible = false)
        {
            if (!HasActiveStorage)
                return false;

            foreach (var networkIds in _activeNetworks)
            {
                if (StorageWorldSystem.Instance.CountItem(networkIds, itemType) > 0)
                {
                    Item dummy = new(itemType);
                    Loggers.Log(
                        $"Found itemType [i:{dummy}] in network {string.Join(", ", networkIds)}"
                    );

                    if (consumeIfPossible && dummy.consumable)
                    {
                        _ = StorageWorldSystem.Instance.ExtractItem(networkIds, itemType, 1);
                        Loggers.Log(
                            $"Consumed 1 of itemType [i:{dummy}] from network {string.Join(", ", networkIds)}"
                        );
                    }

                    return true;
                }
            }
            return false;
        }
    }
}
