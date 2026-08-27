using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;
using TerraStorage.Systems;
using TerraStorageOverflow.Common.Utils;
using TerraStorageOverflow.Common.Utils.Reflection;

namespace TerraStorageOverflow.Common.Hooks
{
    internal class DefragHook : ModSystem
    {
        private delegate List<Guid> orig_Defragment(StorageWorldSystem self, List<Guid> _diskIds);

        public override void Load()
        {
            Type storageWorldSystem = typeof(StorageWorldSystem);

            MethodInfo DefragMethod = Reflect.Method(storageWorldSystem, "Defragment");

            if (DefragMethod != null)
            {
                MonoModHooks.Add(DefragMethod, Detour_Defragment);
            }
            else
            {
                Loggers.Error(
                    new MissingMethodException(
                        "Defragment method not found in StorageWorldSystem."
                    ),
                    "Failed to hook Defragment method."
                );
            }
        }

        private List<Guid> Detour_Defragment(
            orig_Defragment orig,
            StorageWorldSystem self,
            List<Guid> _diskIds
        )
        {
            StorageNetworkHelper.ConsolidateStacks(_diskIds);
            return orig(self, _diskIds);
        }
    }
}
