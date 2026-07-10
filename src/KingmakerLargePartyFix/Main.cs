using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace KingmakerLargePartyFix
{
    public static class Main
    {
        internal const string ModId = "KingmakerLargePartyFix";

        internal static Settings Settings;

        private static Harmony s_Harmony;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger.Initialize(modEntry.Logger);
            Logger.Info("Loading mod.");

            try
            {
                Settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
                Settings.Normalize();

                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                modEntry.OnUnload = OnUnload;

                if (Settings.EnablePatch)
                {
                    s_Harmony = new Harmony(ModId);
                    s_Harmony.PatchAll(Assembly.GetExecutingAssembly());
                    Logger.Info("Harmony patches applied.");
                }
                else
                {
                    Logger.Warning("Patch is disabled in settings. EnablePatch changes require a mod reload.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to load mod.", ex);
                return false;
            }
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            try
            {
                if (s_Harmony != null)
                {
                    s_Harmony.UnpatchAll(ModId);
                    s_Harmony = null;
                    Logger.Info("Harmony patches removed.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to unload mod cleanly.", ex);
                return false;
            }
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Settings == null)
            {
                Settings = new Settings();
            }

            Settings.Draw();
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            if (Settings != null)
            {
                Settings.Normalize();
                Settings.Save(modEntry);
                LargePartyFormationLogic.ResetDiagnostics();
                Logger.Info("Settings saved.");
            }
        }
    }
}
