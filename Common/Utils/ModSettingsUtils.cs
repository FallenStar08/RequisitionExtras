using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using TerraStorageOverflow.Common.Configs;

namespace TerraStorageOverflow.Common.Utils
{
    /// <summary>
    /// This is because I'm lazy
    /// </summary>
    internal static class ModSettingsUtils
    {
        private static readonly ModSettings _modSettings = ModContent.GetInstance<ModSettings>();

        private static readonly Dictionary<string, Func<bool>> _boolGetters = new()
        {
            [nameof(ModSettings.DebugText)] = () => _modSettings.DebugText,
            [nameof(ModSettings.EnableRightClickDeposit)] = () =>
                _modSettings.EnableRightClickDeposit,
            [nameof(ModSettings.EnableChestUnlockMessages)] = () =>
                _modSettings.EnableChestUnlockMessages,
            [nameof(ModSettings.EnableFishingCategory)] = () => _modSettings.EnableFishingCategory,
            [nameof(ModSettings.EnablePetsCategory)] = () => _modSettings.EnablePetsCategory,
            [nameof(ModSettings.EnableCustomDiskStacking)] = () =>
                _modSettings.EnableCustomDiskStacking,
        };

        /// <summary>
        /// Get the value of a boolean setting from the ModSettings class by its name.
        /// </summary>
        /// <param name="settingName">The name of the boolean setting.</param>
        /// <returns>The value of the boolean setting.</returns>
        /// <exception cref="ArgumentException">Thrown if the setting is not found or is not a boolean.</exception>
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
