using System;
using UnityModManagerNet;

namespace KingmakerSmartAutoBuff
{
    public static class Main
    {
        internal const string ModId = "KingmakerSmartAutoBuff";

        internal static Settings Settings;
        internal static QueueRepository QueueRepository;
        internal static SmartAutoBuffUi Ui;
        internal static BuffQueueExecutor Executor;
        internal static string ModPath;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger.Initialize(modEntry.Logger);
            Logger.Info("Loading mod.");

            try
            {
                ModPath = modEntry.Path;
                Settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
                Settings.Normalize();

                ModLocalization.Initialize(modEntry.Path);
                QueueRepository = new QueueRepository(modEntry.Path);
                QueueRepository.LoadAll();
                Executor = new BuffQueueExecutor();
                Ui = new SmartAutoBuffUi(QueueRepository);

                modEntry.OnGUI = OnGUI;
                modEntry.OnUpdate = OnUpdate;
                modEntry.OnSaveGUI = OnSaveGUI;
                modEntry.OnUnload = OnUnload;

                Logger.Info("Loaded.");
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
                if (QueueRepository != null)
                {
                    QueueRepository.SaveAll();
                }

                if (Executor != null && Executor.IsRunning)
                {
                    Executor.Stop(ModLocalization.T("Execution.Status.Stopped"));
                }

                Logger.Info("Unloaded.");
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

            if (Ui == null)
            {
                if (QueueRepository == null)
                {
                    QueueRepository = new QueueRepository(modEntry.Path);
                    QueueRepository.LoadAll();
                }

                if (Executor == null)
                {
                    Executor = new BuffQueueExecutor();
                }

                Ui = new SmartAutoBuffUi(QueueRepository);
            }

            Ui.Draw();
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            try
            {
                if (Executor != null)
                {
                    Executor.Update(deltaTime);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed during executor update.", ex);
                if (Executor != null)
                {
                    Executor.Stop(ModLocalization.T("Execution.Status.FailedGeneric"));
                }
            }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            try
            {
                if (Settings != null)
                {
                    Settings.Save(modEntry);
                }

                if (QueueRepository != null)
                {
                    QueueRepository.SaveAll();
                }

                Logger.Info("Settings and queues saved.");
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to save settings or queues.", ex);
            }
        }
    }
}
