using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using TerraStorageOverflow.Common.Systems;

namespace TerraStorageOverflow.Common.Utils
{
    /// <summary>
    /// This is because I'm lazy
    /// </summary>
    public static class ModSettingsUtils
    {
        private static readonly ModSettings _modSettings = ModContent.GetInstance<ModSettings>();

        private static readonly Dictionary<string, Func<bool>> _boolGetters = new()
        {
            [nameof(ModSettings.DebugText)] = () => _modSettings.DebugText,
            [nameof(ModSettings.EnableRightClickDeposit)] = () =>
                _modSettings.EnableRightClickDeposit,
        };

        public static bool GetBoolSetting(string settingName)
        {
            return _boolGetters.TryGetValue(settingName, out var getter)
                ? getter()
                : throw new ArgumentException(
                    $"Setting '{settingName}' not found or is not a boolean."
                );
        }
    }
}
