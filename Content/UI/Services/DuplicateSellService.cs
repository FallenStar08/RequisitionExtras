using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using TerraStorage.Common;
using TerraStorage.Systems;
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

            FieldInfo terminalField = terminalUIState
                .GetType()
                .GetField(
                    "_terminal",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
            object terminal = terminalField?.GetValue(terminalUIState);
            if (terminal == null)
            {
                Loggers.Warn("Terminal entity not found.", Color.Red);
                return null;
            }

            MethodInfo getDiskIdsMethod = terminal
                .GetType()
                .GetMethod("GetConnectedDiskIds", BindingFlags.Instance | BindingFlags.Public);
            if (
                getDiskIdsMethod?.Invoke(terminal, null) is not List<Guid> diskIds
                || diskIds.Count == 0
            )
            {
                Loggers.Warn("No connected disks found.", Color.Yellow);
                return null;
            }

            StorageWorldSystem storageWorld = ModContent.GetInstance<StorageWorldSystem>();
            List<DiskData> connectedDisks = diskIds
                .Select(id => storageWorld.GetDiskData(id))
                .Where(d => d != null)
                .ToList();

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

                MethodInfo refreshMethod = terminalUIState
                    .GetType()
                    .GetMethod(
                        "RefreshItems",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                    );
                refreshMethod?.Invoke(terminalUIState, null);

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
                        Item item = CreateItemFromStored(stored);
                        if (!IsValidForDuplicateCheck(item))
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
                        Item item = CreateItemFromStored(stored);
                        if (!IsValidForDuplicateCheck(item))
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

        private static bool IsValidForDuplicateCheck(Item item)
        {
            return item != null
                && !item.IsAir
                && item.maxStack == 1
                && !item.favorited
                && item.ModItem is not UnloadedItem;
        }

        private static Item CreateItemFromStored(object stored)
        {
            if (stored == null)
                return null;

            if (stored is Item directItem)
            {
                return directItem.Clone();
            }

            Type t = stored.GetType();

            // Direct object wrapper check (e.g. stored.Item)
            PropertyInfo itemProp = GetPropertySafe(t, "Item", "StoredItem");
            if (itemProp != null && itemProp.GetValue(stored) is Item propItem)
                return propItem.Clone();

            FieldInfo itemField = GetFieldSafe(t, "Item", "storedItem");
            if (itemField != null && itemField.GetValue(stored) is Item fieldItem)
                return fieldItem.Clone();

            // TagCompound Load check
            TagCompound fullTag = GetTagValue(
                stored,
                "FullItemTag",
                "fullItemTag",
                "Tag",
                "itemTag"
            );
            int stackVal = GetIntValue(
                stored,
                "Stack",
                "stack",
                "Count",
                "count",
                "Amount",
                "amount"
            );

            if (fullTag != null)
            {
                try
                {
                    Item loaded = ItemIO.Load(fullTag);
                    if (loaded != null && !loaded.IsAir)
                    {
                        if (stackVal > 0)
                            loaded.stack = stackVal;
                        return loaded;
                    }
                }
                catch { }
            }

            // Fallback to Item ID
            int itemTypeId = GetIntValue(
                stored,
                "Type",
                "type",
                "Id",
                "id",
                "ItemID",
                "itemId",
                "netID",
                "ItemType"
            );
            if (itemTypeId <= 0)
                return null;

            Item item = new();
            item.SetDefaults(itemTypeId);
            if (stackVal > 0)
                item.stack = stackVal;

            int prefixId = GetIntValue(stored, "Prefix", "prefix", "PrefixId", "prefixId");
            if (prefixId > 0)
            {
                item.Prefix(prefixId);
            }

            TagCompound modData = GetTagValue(stored, "ModData", "modData", "Data", "data");
            if (modData != null && item.ModItem != null)
            {
                try
                {
                    item.ModItem.LoadData(modData);
                }
                catch { }
            }

            return item;
        }

        private static int GetIntValue(object obj, params string[] names)
        {
            if (obj == null)
                return 0;
            Type t = obj.GetType();
            BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var properties = t.GetProperties(flags);
            foreach (string name in names)
            {
                foreach (var prop in properties)
                {
                    if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            object val = prop.GetValue(obj);
                            if (val != null && IsNumeric(val))
                                return Convert.ToInt32(val);
                        }
                        catch { }
                    }
                }
            }

            var fields = t.GetFields(flags);
            foreach (string name in names)
            {
                foreach (var field in fields)
                {
                    if (string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            object val = field.GetValue(obj);
                            if (val != null && IsNumeric(val))
                                return Convert.ToInt32(val);
                        }
                        catch { }
                    }
                }
            }

            return 0;
        }

        private static TagCompound GetTagValue(object obj, params string[] names)
        {
            if (obj == null)
                return null;
            Type t = obj.GetType();
            BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var properties = t.GetProperties(flags);
            foreach (string name in names)
            {
                foreach (var prop in properties)
                {
                    if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (prop.GetValue(obj) is TagCompound pTag)
                                return pTag;
                        }
                        catch { }
                    }
                }
            }

            var fields = t.GetFields(flags);
            foreach (string name in names)
            {
                foreach (var field in fields)
                {
                    if (string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (field.GetValue(obj) is TagCompound fTag)
                                return fTag;
                        }
                        catch { }
                    }
                }
            }

            return null;
        }

        private static PropertyInfo GetPropertySafe(Type type, params string[] names)
        {
            var props = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            foreach (string name in names)
            {
                foreach (var p in props)
                {
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                        return p;
                }
            }
            return null;
        }

        private static FieldInfo GetFieldSafe(Type type, params string[] names)
        {
            var fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            foreach (string name in names)
            {
                foreach (var f in fields)
                {
                    if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            return null;
        }

        private static bool IsNumeric(object value)
        {
            return value
                is sbyte
                    or byte
                    or short
                    or ushort
                    or int
                    or uint
                    or long
                    or ulong
                    or float
                    or double
                    or decimal;
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
                    player.QuickSpawnItem(
                        player.GetSource_Misc("Requisition_SellDuplicates"),
                        coin,
                        coin.stack
                    );
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
