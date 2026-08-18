using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerraStorage.Content.Tiles;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Common.Hooks
{
    [ExtendsFromMod("TerraStorage")]
    public class DriveBayHookSystem : ModSystem
    {
        private delegate bool orig_InsertDisk(DriveBayEntity self, Item diskItem, int slot);
        private delegate Item orig_RemoveDisk(DriveBayEntity self, int slot);

        public override void Load()
        {
            Type driveBayType = typeof(DriveBayEntity);

            MethodInfo insertMethod = Reflect.Method(driveBayType, "InsertDisk");
            MethodInfo removeMethod = Reflect.Method(driveBayType, "RemoveDisk");

            if (insertMethod != null)
            {
                MonoModHooks.Add(insertMethod, Detour_InsertDisk);
            }

            if (removeMethod != null)
            {
                MonoModHooks.Add(removeMethod, Detour_RemoveDisk);
            }
        }

        private bool Detour_InsertDisk(
            orig_InsertDisk orig,
            DriveBayEntity self,
            Item diskItem,
            int slot
        )
        {
            bool result = orig(self, diskItem, slot);

            if (result)
            {
                ModPlayers.TerraStorageOverflow.NetworkDirty = true;
                Loggers.Log("Disk Inserted. Dirty flag set.", Color.LightPink);
            }

            return result;
        }

        private Item Detour_RemoveDisk(orig_RemoveDisk orig, DriveBayEntity self, int slot)
        {
            Item result = orig(self, slot);

            Loggers.Log(
                $"RemoveDisk called for slot {slot}. Result Type: {result.type} (Name: {result.Name})",
                Color.Gray
            );

            if (result != null && result.type != ItemID.None)
            {
                ModPlayers.TerraStorageOverflow.NetworkDirty = true;
                Loggers.Log(
                    $"Disk Removed ({result.Name}). Network marked dirty.",
                    Color.LightPink
                );
            }

            return result;
        }
    }
}
