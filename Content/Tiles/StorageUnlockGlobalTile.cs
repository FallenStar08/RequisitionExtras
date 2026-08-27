using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Content.Tiles
{
    internal class StorageUnlockGlobalTile : GlobalTile
    {
        private static readonly HashSet<int> ReusableKeys =
        [
            ItemID.ShadowKey,
            //Other keys?
        ];

        public static bool IsKeyConsumed(int keyType)
        {
            return !ReusableKeys.Contains(keyType);
        }

        public override void RightClick(int x, int y, int type)
        {
            bool isContainer =
                Main.tileContainer[type]
                || TileID.Sets.BasicChest[type]
                || TileID.Sets.IsAContainer[type];

            bool isLocked = Chest.IsLocked(x, y) || TileLoader.IsLockedChest(x, y, type);

            if (!isContainer)
            {
                Loggers.Log($"Tile {type} is not a container");
                return;
            }

            if (!isLocked)
            {
                Loggers.Log($"Chest at ({x}, {y}) is not locked");
                return;
            }

            Tile tile = Main.tile[x, y];
            Player player = Main.LocalPlayer;
            int requiredKeyType = GetRequiredKey(tile, type, player);

            if (requiredKeyType <= ItemID.None)
            {
                Loggers.Log(
                    $"Could not determine required key for chest style {tile.TileFrameX / 36}"
                );
                return;
            }

            if (player.HasItem(requiredKeyType))
            {
                Loggers.Log(
                    $"Player already has key {requiredKeyType} in inventory - skipping network check"
                );
                return;
            }

            int chestItemId = GetChestItemId(x, y, tile);
            string chestIcon = chestItemId > ItemID.None ? $"[i:{chestItemId}] " : "";
            string chestName =
                chestItemId > ItemID.None ? Lang.GetItemNameValue(chestItemId) : "Chest";
            string keyIcon = $"[i:{requiredKeyType}] ";
            string keyName = Lang.GetItemNameValue(requiredKeyType);

            Loggers.Log(
                $"RightClicked {chestIcon}{chestName} at ({x}, {y}) requiring {keyIcon}{keyName}"
            );
            if (
                player
                    .GetModPlayer<Common.ModPlayers.TerraStorageOverflowPlayer>()
                    .RemoteCache.HasItemInNetworks(requiredKeyType, false)
            )
            {
                Loggers.Log($"Player is using {keyIcon}{keyName} from remote networks");

                //We need to call unlock on the top left part of the chest otherwise visuals break
                Tile chestTile = Main.tile[x, y];
                int left = x - (chestTile.TileFrameX % 36 / 18);
                int top = y - (chestTile.TileFrameY % 36 / 18);

                bool unlocked = Chest.Unlock(left, top);

                if (unlocked && GetBoolSetting("EnableChestUnlockMessages"))
                {
                    player
                        .GetModPlayer<Common.ModPlayers.TerraStorageOverflowPlayer>()
                        .RemoteCache.HasItemInNetworks(
                            requiredKeyType,
                            IsKeyConsumed(requiredKeyType)
                        );
                    Loggers.ToChat(
                        EasyLoca.ChestUnlockMessage(chestIcon, chestName, keyIcon, keyName)
                    );
                }
                else
                {
                    Loggers.ToChat(
                        EasyLoca.ChestUnlockFailedMessage(chestIcon, chestName, keyIcon, keyName)
                    );
                }
            }
        }

        private static int GetRequiredKey(Tile tile, int tileType, Player player)
        {
            int style = tile.TileFrameX / 36;

            if (tileType == TileID.Containers && style < Chest.chestTypeToIcon.Length)
            {
                int key = Chest.chestTypeToIcon[style];
                if (key > ItemID.None)
                {
                    Loggers.Log("Got key from chestTypeToIcon");
                    return key;
                }
            }

            if (tileType == TileID.Containers2 && style < Chest.chestTypeToIcon2.Length)
            {
                int key = Chest.chestTypeToIcon2[style];
                if (key > ItemID.None)
                {
                    Loggers.Log("Got key from chestTypeToIcon2");
                    return key;
                }
            }
            if (player.cursorItemIconEnabled && player.cursorItemIconID > ItemID.None)
            {
                Loggers.Log("Got key from cursorItemIconID");
                return player.cursorItemIconID;
            }
            else
            {
                Loggers.Log("Didn't find a key for this chest style, returning ItemID.None");
                return ItemID.None;
            }
        }

        private static int GetChestItemId(int i, int j, Tile tile)
        {
            ushort tileType = tile.TileType;
            int style = tile.TileFrameX / 36;

            if (tileType == TileID.Containers && style < Chest.chestItemSpawn.Length)
            {
                return Chest.chestItemSpawn[style];
            }

            if (tileType == TileID.Containers2 && style < Chest.chestItemSpawn2.Length)
            {
                return Chest.chestItemSpawn2[style];
            }

            ModTile modTile = TileLoader.GetTile(tileType);
            if (modTile != null)
            {
                var drops = modTile.GetItemDrops(i, j);
                if (drops != null)
                {
                    foreach (Item item in drops)
                    {
                        if (item.type > ItemID.None)
                            return item.type;
                    }
                }
            }

            return ItemID.Chest;
        }
    }
}
