using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Common.Commands
{
    /// <summary>
    /// A command that spawns a batch of random-prefixed copies of specified items into the player's inventory for testing purposes.
    /// </summary>
    public class TestItemsCommand : ModCommand
    {
        private readonly int minSpawnAmount = 2;
        private readonly int maxSpawnAmount = 100;
        public override CommandType Type => CommandType.Chat;
        public override string Command => "spawntestitems";
        public override string Description => $"Spawns {minSpawnAmount} to {maxSpawnAmount} random-prefixed copies of specified items into your inventory.";
        public override string Usage => "/spawntestitems [optional item 1], [item 2] ... (leave blank for default batch)";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (ModSettingsUtils.GetBoolSetting("DebugText") == false)
            {
                caller.Reply("[TestItems] DebugText setting is disabled. Enable it in the mod settings to use this command.", Color.Red);
                return;
            }
            List<string> itemNames = [];

            if (args.Length > 0)
            {
                // Join arguments and split by comma to support names with spaces
                string fullArgs = string.Join(" ", args);
                string[] split = fullArgs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                itemNames.AddRange(split);
            }
            else
            {
                // Default test batch of popular unstackables (weapons & accessories)
                itemNames.AddRange(
                [
                    "Terra Blade",
                    "Megashark",
                    "Hermes Boots",
                    "Ankh Shield",
                    "Iron Broadsword",
                    "Zenith",
                    "Excalibur",
                    "Magic Dagger",
                    "Copper Shortsword",
                ]);
            }

            int totalSpawned = 0;

            foreach (string rawName in itemNames)
            {
                int itemType = ResolveItemType(rawName);
                if (itemType <= 0)
                {
                    caller.Reply($"[TestItems] Could not find item matching '{rawName}'. Skipping.", Color.Orange);
                    continue;
                }

                int amountToSpawn = Main.rand.Next(minSpawnAmount, maxSpawnAmount);

                for (int i = 0; i < amountToSpawn; i++)
                {
                    Item item = new();
                    item.SetDefaults(itemType);

                    item.Prefix(-1);

                    Item leftover = caller.Player.GetItem(caller.Player.whoAmI, item, GetItemSettings.PickupItemFromWorld);
                    if (!leftover.IsAir)
                    {
                        caller.Player.QuickSpawnItem(caller.Player.GetSource_Misc("TestItemCommand"), leftover, leftover.stack);
                    }

                    totalSpawned++;
                }

                caller.Reply($"[TestItems] Spawned {amountToSpawn}x '{Lang.GetItemNameValue(itemType)}' with random prefixes.", Color.LightGreen);
            }

            caller.Reply($"[TestItems] Done! Generated {totalSpawned} total test items.", Color.Gold);
        }

        private static int ResolveItemType(string name)
        {
            // Direct ID match
            if (int.TryParse(name, out int id) && id > 0 && id < ItemLoader.ItemCount)
                return id;

            // Search by name (case-insensitive, works for both vanilla & modded, can't be fucked remembering any ID)
            for (int i = 1; i < ItemLoader.ItemCount; i++)
            {
                string displayName = Lang.GetItemNameValue(i);
                if (string.Equals(displayName, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }
    }
}