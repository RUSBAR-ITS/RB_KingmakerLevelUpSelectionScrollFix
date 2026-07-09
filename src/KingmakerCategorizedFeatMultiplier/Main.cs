using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace KingmakerCategorizedFeatMultiplier
{
    public static class Main
    {
        internal const string ModId = "KingmakerCategorizedFeatMultiplier";

        internal static Settings Settings;

        private static Harmony s_Harmony;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger.Initialize(modEntry.Logger);
            Logger.Info("Loading mod.");

            try
            {
                Settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
                Settings.MigrateIfNeeded();
                Settings.Normalize();
                ModLocalization.Initialize(modEntry.Path);

                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                modEntry.OnUnload = OnUnload;

                s_Harmony = new Harmony(ModId);
                s_Harmony.PatchAll(Assembly.GetExecutingAssembly());
                Logger.Info("Harmony patches applied.");

                Compatibility.PatchBagOfTricksMultiplier(s_Harmony);

                if (Settings.WarnAboutBagOfTricks)
                {
                    Compatibility.CheckBagOfTricks("load");
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
                LevelUpHelperAddFeaturesPatch.ResetDiagnosticsCounters();
                SelectionSwitchOrderPatch.ResetDiagnosticsCounters();
                Logger.Info("Settings saved.");
            }
        }
    }
}
