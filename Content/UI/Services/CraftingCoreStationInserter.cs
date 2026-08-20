using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TerraStorage.Common;
using TerraStorage.Content.Tiles;
using TerraStorageOverflow.Common.Utils;
using TerraStorageOverflow.Common.Utils.Items;

// This shit is ass.

namespace TerraStorageOverflow.Content.UI.Services
{
    public static class CraftingCoreStationInserter
    {
        private static bool IsValidTile(int tile)
        {
            return tile >= 0;
        }

        private static bool HasValidCondition(object condition)
        {
            return !ItemConditionsUtils.IsNoneCondition(condition);
        }

        public static void AutoInsertMissingStations(
            CraftingCoreEntity entity,
            List<DiskData> disks
        )
        {
            if (entity == null || disks == null || disks.Count == 0)
            {
                Loggers.Log("No storage disks connected to pull stations from.", Color.Yellow);
                return;
            }

            // Clean up any stations inside the core that are already redundant
            int initialPruned = PruneRedundantStations(entity, disks);

            // Resolve currently active tiles and conditions in the Core
            HashSet<int> existingTiles = GetCurrentTiles(entity);
            HashSet<object> existingConditions = GetCurrentConditionsAsObjects(entity);

            // Gather candidate items across disks and de-duplicate by item type
            var candidateMap = new Dictionary<int, (Item item, DiskData disk)>();
            foreach (DiskData disk in disks)
            {
                if (disk?.Items == null)
                    continue;

                foreach (var stored in disk.Items.ToList())
                {
                    Item item = stored.ToItem();
                    if (item == null || item.IsAir || !CraftingCoreEntity.IsValidStation(item))
                        continue;

                    // Recipe filter: only process candidate stations used in active recipes (avoid dirt, chest etc)
                    if (!RecipeUtils.IsStationItemUsedInAnyRecipe(item))
                        continue;

                    if (!candidateMap.ContainsKey(item.type))
                    {
                        candidateMap[item.type] = (item, disk);
                    }
                }
            }

            // Pre-calculate station coverage and sort candidates (highest coverage first)
            var sortedCandidates = candidateMap
                .Values.Select(c => new
                {
                    c.item,
                    c.disk,
                    ProvidedTiles = TileResolverUtils.GetProvidedTiles(c.item.createTile),
                    Condition = ItemConditionsUtils.GetItemCondition(c.item.type),
                })
                .OrderByDescending(c => c.ProvidedTiles.Count(IsValidTile))
                .ThenByDescending(c => c.item.rare)
                .ThenByDescending(c => c.item.value)
                .ToList();

            int insertedCount = 0;

            // Process candidates in order of highest coverage to lowest
            foreach (var candidate in sortedCandidates)
            {
                Item item = candidate.item;
                DiskData disk = candidate.disk;

                bool providesNewTile = candidate.ProvidedTiles.Any(t =>
                    IsValidTile(t)
                    && !existingTiles.Contains(t)
                    && RecipeUtils.IsTileUsedInAnyRecipe(t)
                );

                bool providesNewCondition =
                    HasValidCondition(candidate.Condition)
                    && !existingConditions.Contains(candidate.Condition);

                if (!providesNewTile && !providesNewCondition)
                    continue;

                Item extracted = disk.ExtractItem(item.type, 1, item.prefix);
                if (extracted == null || extracted.IsAir)
                    continue;

                // Try inserting directly
                if (entity.InsertStation(extracted))
                {
                    insertedCount++;
                    LogInsertion(extracted.Name, candidate.ProvidedTiles, candidate.Condition);

                    UpdateCoverage(
                        existingTiles,
                        existingConditions,
                        candidate.ProvidedTiles,
                        candidate.Condition
                    );
                    PruneRedundantStations(entity, disks);
                }
                else
                {
                    // Core is full: check if candidate supersedes an installed station
                    Item installedToSwap = FindReplaceableStation(
                        entity,
                        candidate.ProvidedTiles,
                        candidate.Condition
                    );

                    if (installedToSwap != null)
                    {
                        string oldName = installedToSwap.Name;
                        if (
                            RemoveStationFromCore(entity, installedToSwap)
                            && entity.InsertStation(extracted)
                        )
                        {
                            insertedCount++;
                            Loggers.Log(
                                $"Swapped '{oldName}' with '{extracted.Name}' ({FormatDetails(candidate.ProvidedTiles, candidate.Condition)})",
                                Color.Gold
                            );

                            StationEjector.EjectStation(installedToSwap, disks);
                            UpdateCoverage(
                                existingTiles,
                                existingConditions,
                                candidate.ProvidedTiles,
                                candidate.Condition
                            );
                            PruneRedundantStations(entity, disks);
                        }
                        else
                        {
                            // Return candidate back to disk if swap failed
                            disk.InsertItem(extracted);
                        }
                    }
                    else
                    {
                        disk.InsertItem(extracted);
                        Loggers.Log("Crafting Core station slots are full.", Color.Orange);
                        break;
                    }
                }
            }

            // Compact and sort the internal array so slots have no gaps and are ordered cleanly
            if (insertedCount > 0 || initialPruned > 0)
            {
                CompactAndSortStationSlots(entity);
            }

            if (insertedCount > 0)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                Loggers.Log(
                    $"Successfully inserted/upgraded {insertedCount} crafting station(s)!",
                    Color.Gold
                );
            }
            else
            {
                Loggers.Log(
                    "No new missing or upgradeable crafting stations found in network.",
                    Color.LightGreen
                );
            }
        }

        private static void CompactAndSortStationSlots(CraftingCoreEntity entity)
        {
            if (entity == null)
                return;

            object slotsObj = Reflect.GetValue<object>(entity, "StationSlots");
            if (slotsObj is not Item[] array || array.Length == 0)
                return;

            // Extract all active stations
            var activeStations = array
                .Where(item => item != null && !item.IsAir)
                .Select(item => new
                {
                    Item = item,
                    ProvidedTiles = TileResolverUtils.GetProvidedTiles(item.createTile),
                    Condition = ItemConditionsUtils.GetItemCondition(item.type),
                })
                .OrderByDescending(s => s.ProvidedTiles.Count(IsValidTile))
                .ThenByDescending(s => s.Item.rare)
                .ThenByDescending(s => s.Item.value)
                .ThenBy(s => s.Item.Name)
                .Select(s => s.Item)
                .ToList();

            // Refill array: non-air sorted stations first, empty slots at the end
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i < activeStations.Count ? activeStations[i] : new Item();
            }
        }

        private static Item FindReplaceableStation(
            CraftingCoreEntity entity,
            HashSet<int> candidateTiles,
            object candidateCondition
        )
        {
            List<Item> installed = GetInstalledStations(entity);
            foreach (Item target in installed)
            {
                if (target == null || target.IsAir)
                    continue;

                HashSet<int> targetTiles = TileResolverUtils.GetProvidedTiles(target.createTile);
                object targetCond = ItemConditionsUtils.GetItemCondition(target.type);

                // Target is replaceable if candidate covers all of target's tiles & conditions
                bool tilesCovered = targetTiles.All(t =>
                    !IsValidTile(t) || candidateTiles.Contains(t)
                );
                bool condCovered =
                    !HasValidCondition(targetCond)
                    || (
                        HasValidCondition(candidateCondition)
                        && candidateCondition.Equals(targetCond)
                    );

                if (tilesCovered && condCovered)
                    return target;
            }

            return null;
        }

        private static int PruneRedundantStations(CraftingCoreEntity entity, List<DiskData> disks)
        {
            int prunedCount = 0;
            bool prunedAny;

            do
            {
                prunedAny = false;
                List<Item> installed = GetInstalledStations(entity);
                if (installed.Count <= 1)
                    break;

                foreach (Item current in installed)
                {
                    if (current == null || current.IsAir)
                        continue;

                    var otherTiles = new HashSet<int>();
                    var otherConditions = new HashSet<object>();

                    foreach (Item other in installed)
                    {
                        if (other == current || other == null || other.IsAir)
                            continue;

                        foreach (int tile in TileResolverUtils.GetProvidedTiles(other.createTile))
                            otherTiles.Add(tile);

                        object cond = ItemConditionsUtils.GetItemCondition(other.type);
                        if (HasValidCondition(cond))
                            otherConditions.Add(cond);
                    }

                    HashSet<int> currentTiles = TileResolverUtils.GetProvidedTiles(
                        current.createTile
                    );
                    bool tilesRedundant = currentTiles.All(t =>
                        !IsValidTile(t) || otherTiles.Contains(t)
                    );

                    object currentCond = ItemConditionsUtils.GetItemCondition(current.type);
                    bool condRedundant =
                        !HasValidCondition(currentCond) || otherConditions.Contains(currentCond);

                    if (tilesRedundant && condRedundant)
                    {
                        string name = current.Name;
                        if (RemoveStationFromCore(entity, current))
                        {
                            prunedCount++;
                            prunedAny = true;
                            Loggers.Log(
                                $"Removed redundant station '{name}' from Crafting Core.",
                                Color.MediumPurple
                            );
                            StationEjector.EjectStation(current, disks);
                            break;
                        }
                    }
                }
            } while (prunedAny);

            return prunedCount;
        }

        private static List<Item> GetInstalledStations(CraftingCoreEntity entity)
        {
            var list = new List<Item>();
            if (entity == null)
                return list;

            object stationsObj = Reflect.GetValue<object>(entity, "StationSlots");

            if (stationsObj is Item[] itemList)
            {
                foreach (Item item in itemList)
                {
                    if (item != null && !item.IsAir)
                        list.Add(item);
                }
            }

            return list;
        }

        private static bool RemoveStationFromCore(CraftingCoreEntity entity, Item stationItem)
        {
            if (entity == null || stationItem == null || stationItem.IsAir)
                return false;

            object slotsObj = Reflect.GetValue<object>(entity, "StationSlots");

            if (slotsObj is Item[] array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (
                        array[i] == stationItem
                        || (array[i] != null && array[i].type == stationItem.type)
                    )
                    {
                        Item removed = entity.RemoveStation(i);
                        return removed != null && !removed.IsAir;
                    }
                }
            }

            return false;
        }

        private static HashSet<int> GetCurrentTiles(CraftingCoreEntity entity)
        {
            var tiles = new HashSet<int>();
            foreach (int tile in entity.GetAvailableTileTypes())
            {
                foreach (int provided in TileResolverUtils.GetProvidedTiles(tile))
                    tiles.Add(provided);
            }
            return tiles;
        }

        private static HashSet<object> GetCurrentConditionsAsObjects(CraftingCoreEntity entity)
        {
            var results = new HashSet<object>();
            try
            {
                var conditions = entity.GetAvailableConditions();
                if (conditions != null)
                {
                    foreach (var cond in conditions)
                        results.Add(cond);
                }
            }
            catch (Exception ex)
            {
                Loggers.Warn($"Failed to read entity conditions: {ex.Message}");
            }
            return results;
        }

        private static void UpdateCoverage(
            HashSet<int> existingTiles,
            HashSet<object> existingConditions,
            HashSet<int> providedTiles,
            object condition
        )
        {
            foreach (int t in providedTiles)
            {
                if (IsValidTile(t))
                    existingTiles.Add(t);
            }

            if (HasValidCondition(condition))
            {
                existingConditions.Add(condition);
            }
        }

        private static void LogInsertion(string name, HashSet<int> tiles, object condition)
        {
            Loggers.Log(
                $"Added station '{name}' ({FormatDetails(tiles, condition)})",
                Color.LightGreen
            );
        }

        private static string FormatDetails(HashSet<int> tiles, object condition)
        {
            var tileNames = tiles
                .Where(IsValidTile)
                .Select(TileResolverUtils.GetTileName)
                .Distinct();

            string tilesStr = tileNames.Any() ? string.Join(", ", tileNames) : "None";
            string condStr = HasValidCondition(condition) ? condition.ToString() : "None";

            return $"fulfilled tiles: [{tilesStr}] | conditions: [{condStr}]";
        }
    }
}
