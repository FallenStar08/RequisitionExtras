using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using TerraStorage.Common;
using TerraStorage.Content.Tiles;
using TerraStorage.Helpers;
using TerraStorage.Systems;

namespace TerraStorageOverflow.Common.Utils
{
    public static class StorageNetworkHelper
    {
        public static List<DiskData> GetConnectedDisks(object target)
        {
            if (target == null)
                return null;

            // 1. Resolve CraftingCoreEntity directly or from container properties/fields
            CraftingCoreEntity coreEntity =
                target as CraftingCoreEntity
                ?? TryGetValue<CraftingCoreEntity>(
                    target,
                    "OpenEntity",
                    "Entity",
                    "_entity",
                    "entity"
                );

            if (coreEntity != null)
            {
                return GetDisksFromCraftingCore(coreEntity);
            }

            // 2. Resolve Terminal entity from Terminal UI State
            object terminal = TryGetValue<object>(target, "_terminal", "terminal", "Terminal");
            if (terminal != null)
            {
                try
                {
                    if (
                        Reflect.Invoke(terminal, "GetConnectedDiskIds") is List<Guid> diskIds
                        && diskIds.Count > 0
                    )
                    {
                        StorageWorldSystem storageWorld =
                            ModContent.GetInstance<StorageWorldSystem>();
                        return diskIds
                            .Select(id => storageWorld.GetDiskData(id))
                            .Where(d => d != null)
                            .ToList();
                    }

                    Loggers.Warn("No connected disks found on terminal.", Color.Yellow);
                    return null;
                }
                catch (Exception ex)
                {
                    Loggers.Warn($"Failed to invoke GetConnectedDiskIds: {ex.Message}", Color.Red);
                    return null;
                }
            }

            Loggers.Warn(
                $"Target of type '{target.GetType().Name}' is neither a valid Terminal UI state nor a Crafting Core.",
                Color.Red
            );
            return null;
        }

        private static List<DiskData> GetDisksFromCraftingCore(CraftingCoreEntity coreEntity)
        {
            var diskIds = new HashSet<Guid>();

            foreach (TileEntity te in TileEntity.ByID.Values)
            {
                if (te is TerminalEntity terminal)
                {
                    List<CraftingCoreEntity> connectedCores =
                        StorageNetwork.FindConnectedCraftingCores(terminal.Position);
                    if (connectedCores.Contains(coreEntity))
                    {
                        List<Guid> terminalDiskIds = StorageNetwork.GetAllConnectedDiskIds(
                            terminal.Position
                        );
                        foreach (Guid id in terminalDiskIds)
                        {
                            diskIds.Add(id);
                        }
                    }
                }
            }

            if (diskIds.Count == 0)
            {
                Loggers.Warn(
                    "No connected terminal/storage network found within range of this Crafting Core.",
                    Color.Yellow
                );
                return null;
            }

            StorageWorldSystem storageWorld = ModContent.GetInstance<StorageWorldSystem>();
            return diskIds
                .Select(id => storageWorld.GetDiskData(id))
                .Where(d => d != null)
                .ToList();
        }

        public static void RefreshTerminalUI(object terminalUIState)
        {
            if (terminalUIState == null)
                return;

            try
            {
                Reflect.Invoke(terminalUIState, "RefreshItems");
            }
            catch (Exception ex)
            {
                Loggers.Warn($"Failed to invoke RefreshItems: {ex.Message}", Color.Yellow);
            }
        }

        public static bool IsValidForDuplicateCheck(Item item)
        {
            return item != null
                && !item.IsAir
                && item.maxStack == 1
                && !item.favorited
                && item.ModItem is not UnloadedItem;
        }

        private static T TryGetValue<T>(object target, params string[] memberNames)
        {
            if (target == null)
                return default;

            Type type = target is Type t ? t : target.GetType();

            foreach (string name in memberNames)
            {
                if (Reflect.Field(type, name) != null || Reflect.Property(type, name) != null)
                {
                    try
                    {
                        return Reflect.GetValue<T>(target, name);
                    }
                    catch
                    {
                        // Safe ignore on failed cast or access issue
                    }
                }
            }

            return default;
        }
    }
}
