using System;
using System.Collections;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerraStorage.Common;
using TerraStorageOverflow.Common.Utils.Reflection;

namespace TerraStorageOverflow.Common.Hooks
{
    internal class DiskDataHook : ModSystem
    {
        public override void Load()
        {
            DetourHelpers.Detour<DiskData>(
                "TagValueEquals",
                (Func<Func<object, object, bool>, object, object, bool>)(
                    (orig, a, b) => LocalTagValueEquals(a, b)
                )
            );

            DetourHelpers.Detour<DiskData>(
                "PerInstanceDataMatches",
                (Func<
                    Func<TagCompound, TagCompound, TagCompound, TagCompound, bool>,
                    TagCompound,
                    TagCompound,
                    TagCompound,
                    TagCompound,
                    bool
                >)(
                    (orig, storedFullTag, incomingFullTag, storedModData, incomingModData) =>
                        LocalPerInstanceDataMatches(
                            storedFullTag,
                            incomingFullTag,
                            storedModData,
                            incomingModData
                        )
                )
            );

            DetourHelpers.Detour<DiskData>(
                "InsertItem",
                (Func<
                    Func<DiskData, Item, long, TagCompound, int>,
                    DiskData,
                    Item,
                    long,
                    TagCompound,
                    int
                >)(
                    (orig, self, item, insertionOrder, preSerializedTag) =>
                        LocalInsertItem(self, item, insertionOrder, preSerializedTag)
                )
            );
        }

        private static int LocalInsertItem(
            DiskData disk,
            Item item,
            long insertionOrder,
            TagCompound preSerializedTag
        )
        {
            if (item == null || item.IsAir)
                return 0;

            int remaining = item.stack;
            var items = disk.Items;
            int count = items.Count;

            // STACKABLE ITEMS (Sand, Ores, Potions, Materials, etc.)
            // Pure type + prefix matching. Bypasses NBT entirely for zero lag and clean merging.
            if (item.maxStack > 1)
            {
                for (int i = 0; i < count; i++)
                {
                    var stored = items[i];

                    if (
                        stored.ItemType == item.type
                        && stored.PrefixId == item.prefix
                        && stored.Stack < item.maxStack
                    )
                    {
                        int canAdd = Math.Min(remaining, item.maxStack - stored.Stack);
                        stored.Stack += canAdd;
                        if (insertionOrder > 0)
                            stored.InsertionOrder = insertionOrder;

                        remaining -= canAdd;
                        if (remaining <= 0)
                            return 0;
                    }
                }

                while (remaining > 0 && !disk.IsFull)
                {
                    int stackSize = Math.Min(remaining, item.maxStack);
                    items.Add(
                        new StoredItemStack
                        {
                            ItemType = item.type,
                            Stack = stackSize,
                            PrefixId = item.prefix,
                            InsertionOrder = insertionOrder,
                            ModData = null,
                            FullItemTag = null,
                        }
                    );
                    remaining -= stackSize;
                }

                return remaining;
            }

            // UNSTACKABLE ITEMS / GEAR (Weapons, Armor, Tools, Accessories)
            // Preserves 100% of instance NBT (TerraCards slots, GlobalItem data, ModItem data).
            // We don't care about them not stacking since, well, they don't stack. We just want to preserve all of their data and store them in the disk.
            if (!disk.IsFull)
            {
                TagCompound modData = null;
                if (item.ModItem != null)
                {
                    var tempTag = new TagCompound();
                    item.ModItem.SaveData(tempTag);
                    if (tempTag.Count > 0)
                        modData = tempTag;
                }

                TagCompound fullSave = preSerializedTag ?? ItemIO.Save(item);

                items.Add(
                    new StoredItemStack
                    {
                        ItemType = item.type,
                        Stack = 1,
                        PrefixId = item.prefix,
                        InsertionOrder = insertionOrder,
                        ModData = modData,
                        FullItemTag = fullSave,
                    }
                );

                return 0;
            }

            return remaining;
        }

        private static bool LocalPerInstanceDataMatches(
            TagCompound storedFullTag,
            TagCompound incomingFullTag,
            TagCompound storedModData,
            TagCompound incomingModData
        )
        {
            return (storedModData == null && incomingModData == null)
                || (
                    storedModData == null == (incomingModData == null)
                    && LocalTagCompoundEquals(storedModData, incomingModData)
                );
        }

        private static bool LocalTagCompoundEquals(TagCompound a, TagCompound b)
        {
            if (a.Count != b.Count)
                return false;

            foreach (var kv in a)
            {
                if (!b.ContainsKey(kv.Key))
                    return false;
                if (!LocalTagValueEquals(kv.Value, b[kv.Key]))
                    return false;
            }

            return true;
        }

        private static bool LocalTagValueEquals(object a, object b)
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return false;
            if (a.GetType() != b.GetType())
                return false;

            if (a is TagCompound ta && b is TagCompound tb)
                return LocalTagCompoundEquals(ta, tb);

            if (a is byte[] ba && b is byte[] bb)
                return ba.SequenceEqual(bb);

            if (a is int[] ia && b is int[] ib)
                return ia.SequenceEqual(ib);

            if (a is IList la && b is IList lb)
            {
                if (la.Count != lb.Count)
                    return false;
                for (int i = 0; i < la.Count; i++)
                {
                    if (!LocalTagValueEquals(la[i], lb[i]))
                        return false;
                }
                return true;
            }

            return a.Equals(b);
        }
    }
}
