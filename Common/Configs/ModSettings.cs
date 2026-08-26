using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace TerraStorageOverflow.Common.Configs
{
    public class ModSettings : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(false)]
        public bool DebugText;

        [DefaultValue(true)]
        public bool EnableRightClickDeposit;

        [DefaultValue(true)]
        public bool EnableChestUnlockMessages;

        [DefaultValue(true)]
        [ReloadRequired]
        public bool EnableFishingCategory;

        [DefaultValue(true)]
        [ReloadRequired]
        public bool EnablePetsCategory;
    }
}
