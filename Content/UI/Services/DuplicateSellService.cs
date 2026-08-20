using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TerraStorage.Common;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Content.UI.Services
{
    public enum SellMode
    {
        KeepBestPrefix = 0, // Keeps highest value item, sells worse duplicates
        KeepFirstFound = 1, // Keeps first item found, sells any duplicate regardless of prefix
        ExactMatchesOnly = 2, // Only sells items that have the EXACT same type and prefix
    }

    public class SellReportData
    {
        public List<SellReportEntry> Entries { get; } = [];
        public int TotalItemsSold { get; set; }
        public long TotalEarnedCopper { get; set; }
    }

    public class SellReportEntry
    {
        public Item ItemSample { get; set; }
        public int Count { get; set; }
        public long TotalValue { get; set; }
    }

    public static class DuplicateSellService
    {
        private class DuplicateEntry
        {
            public DiskData Disk;
            public int ItemType;
            public int PrefixId;
            public Item ItemInstance;
        }

        public static SellReportData ExecuteSell(object terminalUIState, SellMode mode)
        {
            if (terminalUIState == null)
                return null;

            Loggers.Log($"Starting duplicate sell scan with mode: {mode}");

            List<DiskData> connectedDisks = StorageNetworkHelper.GetConnectedDisks(terminalUIState);
            if (connectedDisks == null || connectedDisks.Count == 0)
                return null;

            Loggers.Log($"Found {connectedDisks.Count} connected disk(s).");

            List<DuplicateEntry> toSell = FindDuplicates(connectedDisks, mode);
            if (toSell.Count == 0)
            {
                Loggers.Log("No unstackable duplicates found.", Color.LightGreen);
                return new SellReportData();
            }

            Loggers.Log($"Identified {toSell.Count} duplicate item(s) to sell.");

            SellReportData report = new();
            Dictionary<(int type, int prefix), SellReportEntry> groupedEntries = [];

            foreach (var entry in toSell)
            {
                Item extracted = entry.Disk.ExtractItem(entry.ItemType, 1, entry.PrefixId);
                if (!extracted.IsAir)
                {
                    long sellValue = Math.Max(1, (long)(extracted.value * 0.20f));
                    report.TotalEarnedCopper += sellValue;
                    report.TotalItemsSold++;

                    var key = (extracted.type, extracted.prefix);
                    if (!groupedEntries.TryGetValue(key, out var reportEntry))
                    {
                        reportEntry = new SellReportEntry
                        {
                            ItemSample = extracted,
                            Count = 0,
                            TotalValue = 0,
                        };
                        groupedEntries[key] = reportEntry;
                        report.Entries.Add(reportEntry);
                    }

                    reportEntry.Count++;
                    reportEntry.TotalValue += sellValue;
                }
            }

            if (report.TotalItemsSold > 0)
            {
                GiveCoinsToPlayer(Main.LocalPlayer, report.TotalEarnedCopper);
                SoundEngine.PlaySound(SoundID.Coins);

                StorageNetworkHelper.RefreshTerminalUI(terminalUIState);

                Loggers.Log(
                    $"Sold {report.TotalItemsSold} duplicate(s) for {FormatCoinString(report.TotalEarnedCopper)}!",
                    Color.Gold
                );
            }

            return report;
        }

        private static List<DuplicateEntry> FindDuplicates(List<DiskData> disks, SellMode mode)
        {
            List<DuplicateEntry> duplicatesToSell = [];
            int totalScannedItems = 0;
            int validUnstackableItems = 0;

            if (mode == SellMode.ExactMatchesOnly)
            {
                Dictionary<
                    (int type, int prefix),
                    (Item keepItem, DuplicateEntry keepEntry)
                > exactTrackers = [];

                foreach (var disk in disks)
                {
                    foreach (var stored in disk.Items.ToList())
                    {
                        totalScannedItems++;
                        Item item = stored.ToItem();
                        if (!StorageNetworkHelper.IsValidForDuplicateCheck(item))
                            continue;

                        validUnstackableItems++;
                        var key = (item.type, item.prefix);
                        int countInStack = Math.Max(1, item.stack);

                        if (!exactTrackers.ContainsKey(key))
                        {
                            exactTrackers[key] = (
                                item,
                                new DuplicateEntry
                                {
                                    Disk = disk,
                                    ItemType = item.type,
                                    PrefixId = item.prefix,
                                    ItemInstance = item,
                                }
                            );

                            for (int i = 1; i < countInStack; i++)
                            {
                                duplicatesToSell.Add(
                                    new DuplicateEntry
                                    {
                                        Disk = disk,
                                        ItemType = item.type,
                                        PrefixId = item.prefix,
                                        ItemInstance = item,
                                    }
                                );
                            }
                        }
                        else
                        {
                            for (int i = 0; i < countInStack; i++)
                            {
                                duplicatesToSell.Add(
                                    new DuplicateEntry
                                    {
                                        Disk = disk,
                                        ItemType = item.type,
                                        PrefixId = item.prefix,
                                        ItemInstance = item,
                                    }
                                );
                            }
                        }
                    }
                }
            }
            else
            {
                Dictionary<int, (Item keepItem, DuplicateEntry keepEntry)> typeTrackers = [];

                foreach (var disk in disks)
                {
                    foreach (var stored in disk.Items.ToList())
                    {
                        totalScannedItems++;
                        Item item = stored.ToItem();
                        if (!StorageNetworkHelper.IsValidForDuplicateCheck(item))
                            continue;

                        validUnstackableItems++;
                        var currentEntry = new DuplicateEntry
                        {
                            Disk = disk,
                            ItemType = item.type,
                            PrefixId = item.prefix,
                            ItemInstance = item,
                        };
                        int countInStack = Math.Max(1, item.stack);

                        if (!typeTrackers.TryGetValue(item.type, out var currentKeep))
                        {
                            typeTrackers[item.type] = (item, currentEntry);

                            for (int i = 1; i < countInStack; i++)
                            {
                                duplicatesToSell.Add(currentEntry);
                            }
                        }
                        else
                        {
                            if (
                                mode == SellMode.KeepBestPrefix
                                && item.value > currentKeep.keepItem.value
                            )
                            {
                                duplicatesToSell.Add(currentKeep.keepEntry);
                                typeTrackers[item.type] = (item, currentEntry);

                                for (int i = 1; i < countInStack; i++)
                                {
                                    duplicatesToSell.Add(currentEntry);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < countInStack; i++)
                                {
                                    duplicatesToSell.Add(currentEntry);
                                }
                            }
                        }
                    }
                }
            }

            Loggers.Log(
                $"Scanned total {totalScannedItems} item entries across disks ({validUnstackableItems} were valid unstackable items)."
            );
            return duplicatesToSell;
        }

        private static void GiveCoinsToPlayer(Player player, long totalCopper)
        {
            int platinum = (int)(totalCopper / 1000000);
            totalCopper %= 1000000;
            int gold = (int)(totalCopper / 10000);
            totalCopper %= 10000;
            int silver = (int)(totalCopper / 100);
            int copper = (int)(totalCopper % 100);

            DepositCoin(player, ItemID.PlatinumCoin, platinum);
            DepositCoin(player, ItemID.GoldCoin, gold);
            DepositCoin(player, ItemID.SilverCoin, silver);
            DepositCoin(player, ItemID.CopperCoin, copper);
        }

        private static void DepositCoin(Player player, int coinType, int count)
        {
            while (count > 0)
            {
                int stack = Math.Min(count, 999);
                Item coin = new();
                coin.SetDefaults(coinType);
                coin.stack = stack;
                coin = player.GetItem(player.whoAmI, coin, GetItemSettings.PickupItemFromWorld);
                if (!coin.IsAir)
                {
                    player.QuickSpawnItem(
                        player.GetSource_Misc("Requisition_SellDuplicates"),
                        coin,
                        coin.stack
                    );
                }
                count -= stack;
            }
        }

        private static string FormatCoinString(long copper)
        {
            List<string> parts = [];
            long plat = copper / 1000000;
            copper %= 1000000;
            long gold = copper / 10000;
            copper %= 10000;
            long silver = copper / 100;
            long cop = copper % 100;

            if (plat > 0)
                parts.Add($"{plat} Platinum");
            if (gold > 0)
                parts.Add($"{gold} Gold");
            if (silver > 0)
                parts.Add($"{silver} Silver");
            if (cop > 0 || parts.Count == 0)
                parts.Add($"{cop} Copper");

            return string.Join(", ", parts);
        }
    }
}
