using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TerraStorageOverflow.Common.Systems;

namespace TerraStorageOverflow.Common.Utils
{
    /// <summary>
    /// A utility class for logging messages to both the mod logger and the in-game chat, with support for different log levels and colors.
    /// </summary>
    public static class Loggers
    {
        public enum LogLevel
        {
            Info,
            Warn,
            Error
        }

        private static ModSettings Settings => ModContent.GetInstance<ModSettings>();
        private static Mod Mod => Settings.Mod;

        private const string ActuatorIcon = "[i:Actuator] ";

        /// <summary>
        /// Logs a message to the mod logger and to the in-game chat if DebugText is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color to use for the in-game chat message. (default: White)</param>
        /// <param name="caller">The name of the calling method. (automatically provided by the compiler)</param>
        public static void Log(string message, Color? color = null, [CallerMemberName] string caller = "")
        {
            if (!Settings.DebugText)
            {
                return;
            }

            Write(LogLevel.Info, message, color, caller, writeToChat: true, writeToLogger: true);
        }

        /// <summary>
        /// Same behavior as Log, but clearer naming for new call sites.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color to use for the in-game chat message. (default: White)</param>
        /// <param name="caller">The name of the calling method. (automatically provided by the compiler)</param>
        public static void Info(string message, Color? color = null, [CallerMemberName] string caller = "")
        {
            Log(message, color, caller);
        }

        /// <summary>
        /// Logs a warning message to the mod logger (always) and to the in-game chat if DebugText is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color to use for the in-game chat message. (default: Orange)</param>
        /// <param name="caller">The name of the calling method. (automatically provided by the compiler)</param>
        public static void Warn(string message, Color? color = null, [CallerMemberName] string caller = "")
        {
            Write(
                LogLevel.Warn,
                message,
                color ?? Color.Orange,
                caller,
                writeToChat: Settings.DebugText,
                writeToLogger: true);
        }

        /// <summary>
        /// Logs an error message to the mod logger (always) and to the in-game chat if DebugText is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log. (optional)</param>
        /// <param name="color">The color to use for the in-game chat message. (default: Red)</param>
        /// <param name="caller">The name of the calling method. (automatically provided by the compiler)</param>
        public static void Error(string message, Exception? exception = null, Color? color = null, [CallerMemberName] string caller = "")
        {
            string finalMessage = exception is null
                ? message
                : $"{message}{Environment.NewLine}{exception}";

            Write(
                LogLevel.Error,
                finalMessage,
                color ?? Color.Red,
                caller,
                writeToChat: Settings.DebugText,
                writeToLogger: true);
        }

        /// <summary>
        /// Alternative overload for logging an exception with an optional message. If no message is provided, a default message will be used.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log. (optional)</param>
        /// <param name="color">The color to use for the in-game chat message. (default: Red)</param>
        /// <param name="caller">The name of the calling method. (automatically provided by the compiler)</param>
        public static void Error(Exception exception, string? message = null, Color? color = null, [CallerMemberName] string caller = "")
        {
            Error(message ?? "An exception occurred.", exception, color, caller);
        }

        /// <summary>
        /// Main logging method that handles writing to both the mod logger and the in-game chat based on the provided parameters.
        /// </summary>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color to use for the in-game chat message. (default: based on the log level)</param>
        /// <param name="caller">The name of the calling method. (automatically provided by the compiler)</param>
        /// <param name="writeToChat">Whether to write the message to the in-game chat.</param>
        /// <param name="writeToLogger">Whether to write the message to the mod logger.</param>
        private static void Write(LogLevel level, string message, Color? color, string caller, bool writeToChat, bool writeToLogger)
        {
            message = string.IsNullOrWhiteSpace(message) ? "<empty>" : message;

            string levelText = level.ToString().ToUpperInvariant();
            string prefix = $"[REQX][{levelText}] {caller}(): ";
            string fullMessage = prefix + message;

            if (writeToChat)
            {
                Main.NewText(ActuatorIcon + fullMessage + ActuatorIcon, color ?? GetDefaultColor(level));
            }

            if (!writeToLogger)
            {
                return;
            }

            switch (level)
            {
                case LogLevel.Info:
                    Mod.Logger.Info(fullMessage);
                    break;

                case LogLevel.Warn:
                    Mod.Logger.Warn(fullMessage);
                    break;

                case LogLevel.Error:
                    Mod.Logger.Error(fullMessage);
                    break;
            }
        }

        private static Color GetDefaultColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => Color.White,
                LogLevel.Warn => Color.Orange,
                LogLevel.Error => Color.Red,
                _ => Color.White
            };
        }
    }
}