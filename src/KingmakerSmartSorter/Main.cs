using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace KingmakerSmartSorter
{
    public static class Main
    {
        internal const string ModId = "KingmakerSmartSorter";
        internal const string ModVersion = "0.7.2";

        internal static Settings Settings;

        internal static string ModPath
        {
            get { return s_ModEntry == null ? string.Empty : s_ModEntry.Path; }
        }

        private static Harmony s_Harmony;
        private static UnityModManager.ModEntry s_ModEntry;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            s_ModEntry = modEntry;
            Logger.Initialize(modEntry.Logger);
            Logger.Info("Loading mod version " + ModVersion + ".");

            try
            {
                Settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
                Settings.Normalize();
                ModLocalization.Initialize(modEntry.Path);

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
                    Logger.Warning("Patch is disabled in settings. Changing this option requires a mod reload.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to load mod.", ex);
                return false;
            }
        }

        internal static void SetSmartSortingSelected(bool selected)
        {
            if (Settings == null || Settings.SmartSortingSelected == selected)
            {
                return;
            }

            Settings.SmartSortingSelected = selected;
            SaveSettings("inventory sorter changed");
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
            SaveSettings("UMM settings saved");
            SortDiagnostics.Reset();
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            try
            {
                SmartSortController.RemoveInjectedOptions();

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

        private static void SaveSettings(string reason)
        {
            if (Settings == null || s_ModEntry == null)
            {
                return;
            }

            try
            {
                Settings.Normalize();
                Settings.Save(s_ModEntry);
                Logger.Info("Settings saved: " + reason + ".");
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to save settings.", ex);
            }
        }
    }
}
