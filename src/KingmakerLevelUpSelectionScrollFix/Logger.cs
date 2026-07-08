using System;
using UnityModManagerNet;

namespace KingmakerLevelUpSelectionScrollFix
{
    internal static class Logger
    {
        private static UnityModManager.ModEntry.ModLogger s_Logger;

        internal static void Initialize(UnityModManager.ModEntry.ModLogger logger)
        {
            s_Logger = logger;
        }

        internal static void Info(string message)
        {
            if (s_Logger != null)
            {
                s_Logger.Log(message);
            }
        }

        internal static void Warning(string message)
        {
            if (s_Logger != null)
            {
                s_Logger.Warning(message);
            }
        }

        internal static void Error(string message)
        {
            if (s_Logger != null)
            {
                s_Logger.Error(message);
            }
        }

        internal static void Exception(string message, Exception exception)
        {
            if (s_Logger != null)
            {
                s_Logger.LogException(message, exception);
            }
        }
    }
}
