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
                        return
                        [
                            .. diskIds
                                .Select(id => storageWorld.GetDiskData(id))
                                .Where(d => d != null),
                        ];
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

        /// <summary>
        /// Consolidates partial stacks across disks using a list of disk GUIDs in priority sequence.
        /// </summary>
        /// <param name="orderedDiskIds">The list of disk GUIDs representing the network in priority order.</param>
        /// <returns>A list of GUIDs for disks that were modified.</returns>
        public static List<Guid> ConsolidateStacks(List<Guid> orderedDiskIds)
        {
            if (orderedDiskIds == null || orderedDiskIds.Count == 0)
                return [];

            StorageWorldSystem storageWorld = ModContent.GetInstance<StorageWorldSystem>();
            var disks = orderedDiskIds
                .Select(id => storageWorld.GetDiskData(id))
                .Where(d => d != null)
                .ToList();

            return ConsolidateStacks(disks);
        }

        /// <summary>
        /// Consolidates partial stacks across the provided list of DiskData instances.
        /// Merges identical items (matching ItemType, PrefixId, and null ModData) into full stacks.
        /// </summary>
        /// <param name="disks">The list of DiskData instances to consolidate.</param>
        /// <returns>A list of GUIDs for disks that were modified.</returns>
        public static List<Guid> ConsolidateStacks(List<DiskData> disks)
        {
            if (disks == null || disks.Count == 0)
                return [];

            var modified = new HashSet<Guid>();

            for (int ti = 0; ti < disks.Count; ti++)
            {
                var targetDisk = disks[ti];
                if (targetDisk?.Items == null)
                    continue;

                for (int si = 0; si < targetDisk.Items.Count; si++)
                {
                    var targetStack = targetDisk.Items[si];

                    // Ignore unique items with custom mod data
                    if (targetStack.ModData != null)
                        continue;

                    // Fetch item max stack
                    var tempItem = new Item();
                    tempItem.SetDefaults(targetStack.ItemType);
                    int maxStack = tempItem.maxStack;

                    // Skip unstackable items or already full stacks
                    if (maxStack <= 1 || targetStack.Stack >= maxStack)
                        continue;

                    // Search for donor stacks starting from the last disk down to current target disk
                    bool targetFilled = false;
                    for (int di = disks.Count - 1; di >= ti && !targetFilled; di--)
                    {
                        var donorDisk = disks[di];
                        if (donorDisk?.Items == null)
                            continue;

                        // If searching on the same disk, only look at slots AFTER the current target slot
                        int startSlot = donorDisk.Items.Count - 1;
                        int endSlot = (di == ti) ? si + 1 : 0;

                        for (int dsi = startSlot; dsi >= endSlot; dsi--)
                        {
                            var donorStack = donorDisk.Items[dsi];

                            // Match item type, prefix, and ensure donor has no custom ModData
                            if (
                                donorStack.ItemType == targetStack.ItemType
                                && donorStack.PrefixId == targetStack.PrefixId
                                && donorStack.ModData == null
                            )
                            {
                                int spaceLeft = maxStack - targetStack.Stack;
                                int toTransfer = Math.Min(spaceLeft, donorStack.Stack);

                                targetStack.Stack += toTransfer;
                                donorStack.Stack -= toTransfer;

                                modified.Add(targetDisk.DiskId);
                                modified.Add(donorDisk.DiskId);

                                // If the donor stack was completely drained, remove its slot
                                if (donorStack.Stack == 0)
                                {
                                    donorDisk.Items.RemoveAt(dsi);
                                }

                                // Target stack is fully filled
                                if (targetStack.Stack >= maxStack)
                                {
                                    targetFilled = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (modified.Count > 0)
            {
                // Sync storage version and backup state if needed
                try
                {
                    StorageWorldSystem storageWorld = ModContent.GetInstance<StorageWorldSystem>();
                    if (storageWorld != null)
                    {
                        int currentVersion = TryGetValue<int>(storageWorld, "StorageVersion");
                        Reflect.SetValue(storageWorld, "StorageVersion", currentVersion + 1);
                    }

                    Type backupType = typeof(StorageWorldSystem).Assembly.GetType(
                        "TerraStorage.Systems.BackupSystem"
                    );
                    if (backupType != null)
                    {
                        Reflect.Invoke(backupType, "MarkDirty");
                    }
                }
                catch (Exception ex)
                {
                    Loggers.Warn(
                        $"Failed to update StorageVersion or MarkDirty: {ex.Message}",
                        Color.Yellow
                    );
                }
            }

            return [.. modified];
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
