using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerraStorage.Content.Items;
using TerraStorage.Content.Tiles;
using TerraStorage.Helpers;
using TerraStorage.Systems;
using TerraStorageOverflow.Common.Networking;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Common.ModPlayers
{
    public class TerraStorageOverflow : ModPlayer
    {
        public static bool NetworkDirty = true;

        private List<List<Guid>> _activeNetworks = [];
        private bool _isHandlingPickup;
        private long _lastFullMessageFrame = -5400;

        public bool HasActiveStorage => _activeNetworks.Count > 0;

        private int _remotesFoundThisFrame;
        private int _remotesFoundLastFrame;

        public void ReportRemoteFound()
        {
            _remotesFoundThisFrame++;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (_remotesFoundThisFrame != _remotesFoundLastFrame)
            {
                NetworkDirty = true;
                _remotesFoundLastFrame = _remotesFoundThisFrame;
                Loggers.Log("Remote count changed, cache marked dirty.");
            }

            _remotesFoundThisFrame = 0;
        }

        public override void OnEnterWorld()
        {
            NetworkDirty = true;
        }

        private void EnsureCacheFresh()
        {
            if (NetworkDirty || _activeNetworks.Count == 0)
            {
                RefreshAllStorageCaches();
                NetworkDirty = false;
            }
        }

        private void RefreshAllStorageCaches()
        {
            _activeNetworks.Clear();
            HashSet<int> seenEntities = [];

            for (int i = 0; i < 50; i++)
            {
                Item item = Player.inventory[i];
                if (item.ModItem is RemoteTerminal rt && rt.BoundEntityId != -1)
                {
                    if (seenEntities.Contains(rt.BoundEntityId))
                        continue;

                    if (
                        TileEntity.ByID.TryGetValue(rt.BoundEntityId, out var te)
                        && te is TerminalEntity terminal
                    )
                    {
                        var diskIds = StorageNetwork.GetAllConnectedDiskIds(terminal.Position);
                        if (diskIds != null && diskIds.Count > 0)
                        {
                            _activeNetworks.Add(diskIds);
                            seenEntities.Add(rt.BoundEntityId);
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
            EnsureCacheFresh();
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
                //Multiplayer
                else
                {
                    Guid networkKey = networkIds[0];
                    StorageBufferSystem.AddToBuffer(networkKey, item);
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

        public override bool OnPickup(Item item)
        {
            if (item.IsAir || InventoryUtils.IsInstantPickup(item) || _isHandlingPickup)
                return true;

            EnsureCacheFresh();
            if (!HasActiveStorage)
                return true;

            _isHandlingPickup = true;
            try
            {
                Item leftover = Player.GetItem(
                    Player.whoAmI,
                    item,
                    GetItemSettings.PickupItemFromWorld
                );

                if (leftover.stack > 0)
                {
                    bool fullyStored = DepositIntoAllNetworks(leftover);

                    if (!fullyStored && Main.GameUpdateCount - _lastFullMessageFrame > 5400)
                    {
                        Loggers.Log("All connected networks are full!", Color.OrangeRed);
                        _lastFullMessageFrame = Main.GameUpdateCount;
                    }
                }

                if (leftover.stack <= 0)
                {
                    leftover.TurnToAir();
                    return false;
                }

                return true;
            }
            finally
            {
                _isHandlingPickup = false;
            }
        }
    }
}
