using System.Runtime.CompilerServices; // Required for CallerMemberName
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TerraStorageOverflow.Common.Systems;

namespace TerraStorageOverflow.Common.Utils
{
    public class Loggers
    {
        private static ModSettings _modSetting => ModContent.GetInstance<ModSettings>();
        private static Mod _mod => ModContent.GetInstance<ModSettings>().Mod;

        //I should be the only one to see these anyway
        public static void Log(string message, Color? color = null, [CallerMemberName] string caller = "")
        {
            if (_modSetting.DebugText)
            {
                string chatPrefix = $"[i:Actuator] ";
                string prefix = $"[RE] {caller}(): ";
                Main.NewText(chatPrefix + prefix + message, color ?? Color.White);
                _mod.Logger.Info(prefix + message);
            }
        }
    }
}