using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;
using TerraStorage.Systems;
using TerraStorageOverflow.Common.Utils;

namespace TerraStorageOverflow.Common.Hooks
{
    internal class DefragHook : ModSystem
    {
        private delegate List<Guid> orig_Defragment(StorageWorldSystem self, List<Guid> _diskIds);
        private delegate void orig_SendDefragRequest(Mod mod, List<Guid> _diskIds);

        public override void Load()
        {
            Type storageWorldSystem = typeof(StorageWorldSystem);
            Type networkHandler = typeof(NetworkHandler);
            MethodInfo SendDefragRequestMethod = Reflect.Method(
                networkHandler,
                "SendDefragRequest"
            );
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

            if (SendDefragRequestMethod != null)
            {
                MonoModHooks.Add(SendDefragRequestMethod, Detour_SendDefragRequest);
            }
            else
            {
                Loggers.Error(
                    new MissingMethodException(
                        "SendDefragRequest method not found in NetworkHandler."
                    ),
                    "Failed to hook SendDefragRequest method."
                );
            }
        }

        private void Detour_SendDefragRequest(
            orig_SendDefragRequest orig,
            Mod mod,
            List<Guid> _diskIds
        )
        {
            StorageNetworkHelper.ConsolidateStacks(_diskIds);
            orig(mod, _diskIds);
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
