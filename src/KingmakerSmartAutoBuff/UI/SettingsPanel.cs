using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class SettingsPanel
    {
        internal static void Draw(SmartAutoBuffUi ui, Settings settings)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(ModLocalization.T("Settings.Title"));

            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("Settings.Language") + ":", GUILayout.Width(UiLayout.LanguageLabelWidth));
            DrawLanguageButton(ModLanguage.Russian, ModLocalization.T("Settings.Language.Russian"));
            DrawLanguageButton(ModLanguage.English, ModLocalization.T("Settings.Language.English"));
            GUILayout.EndHorizontal();

            settings.EnableMod = GUILayout.Toggle(settings.EnableMod, ModLocalization.T("Settings.EnableMod"));
            settings.LogDiagnostics = GUILayout.Toggle(settings.LogDiagnostics, ModLocalization.T("Settings.LogDiagnostics"));
            settings.OnlyOutOfCombat = GUILayout.Toggle(settings.OnlyOutOfCombat, ModLocalization.T("Settings.OnlyOutOfCombat"));
            settings.StopOnCombatStart = GUILayout.Toggle(settings.StopOnCombatStart, ModLocalization.T("Settings.StopOnCombatStart"));

            DrawFloatSlider(
                ModLocalization.T("Settings.DelayBetweenCasts"),
                ref settings.DelayBetweenCasts,
                0f,
                3f,
                "0.0");

            DrawFloatSlider(
                ModLocalization.T("Settings.CastTimeoutSeconds"),
                ref settings.CastTimeoutSeconds,
                5f,
                60f,
                "0");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(ModLocalization.T("Button.RefreshData"), GUILayout.Width(UiLayout.SettingsButtonWidth)))
            {
                ui.RefreshGameData();
            }

            if (GUILayout.Button(ModLocalization.T("Button.SaveQueues"), GUILayout.Width(UiLayout.SettingsButtonWidth)))
            {
                ui.QueueRepository.SaveAll();
                ui.State.Status = ModLocalization.T("Status.QueuesSaved");
            }

            if (GUILayout.Button(ModLocalization.T("Button.TestRun"), GUILayout.Width(UiLayout.SettingsButtonWidth)))
            {
                ui.TestRunSelectedQueue();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = Main.Executor == null || !Main.Executor.IsRunning;
            if (GUILayout.Button(ModLocalization.T("Button.RunFullQueue"), GUILayout.Width(UiLayout.SettingsButtonWidth)))
            {
                ui.RunSelectedQueue(QueueExecutionMode.Full);
            }

            if (GUILayout.Button(ModLocalization.T("Button.RunSmartQueue"), GUILayout.Width(UiLayout.SettingsButtonWidth)))
            {
                ui.RunSelectedQueue(QueueExecutionMode.Smart);
            }

            GUI.enabled = Main.Executor != null && Main.Executor.IsRunning;
            if (GUILayout.Button(ModLocalization.T("Button.StopQueue"), GUILayout.Width(UiLayout.SettingsButtonWidth)))
            {
                ui.StopQueueExecution();
            }

            GUI.enabled = true;
            GUILayout.Label(ui.ExecutionStatusText);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private static void DrawLanguageButton(ModLanguage language, string label)
        {
            bool selected = Main.Settings.Language == language;
            bool nextSelected = GUILayout.Toggle(selected, label, "Button", GUILayout.Width(UiLayout.LanguageButtonWidth));
            if (nextSelected && !selected)
            {
                Main.Settings.Language = language;
                ModLocalization.Reload();
            }
        }

        private static void DrawFloatSlider(string label, ref float value, float min, float max, string format)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(UiLayout.LanguageLabelWidth));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(260f));
            GUILayout.Label(value.ToString(format), GUILayout.Width(60f));
            GUILayout.EndHorizontal();
        }
    }
}
