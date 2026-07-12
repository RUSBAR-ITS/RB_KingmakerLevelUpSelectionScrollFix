using System;
using System.Globalization;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerSmartSorter
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public ModLanguage Language = ModLanguage.Russian;
        public bool EnablePatch = true;
        public bool SmartSortingSelected;
        public bool LogDiagnostics = true;
        public int MaxDiagnosticSortRuns = 10;
        public int MaxDiagnosticItemsPerRun = 80;

        private string m_MaxDiagnosticSortRunsText;
        private string m_MaxDiagnosticItemsPerRunText;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            ApplyTextFields();
            Save(this, modEntry);
        }

        internal void Draw()
        {
            Normalize();
            EnsureTextFieldsInitialized();

            GUILayout.BeginVertical("box");

            GUILayout.Label(ModLocalization.T("Settings.Title"));
            DrawLanguageSelector();
            EnablePatch = GUILayout.Toggle(EnablePatch, ModLocalization.T("Settings.EnablePatch"));
            GUILayout.Label(ModLocalization.T("Settings.MenuHint"));
            GUILayout.Label(
                ModLocalization.T("Settings.ActiveMode")
                + ": "
                + ModLocalization.T(SmartSortingSelected ? "Common.Yes" : "Common.No"));

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");

            GUILayout.Label(ModLocalization.T("Settings.Diagnostics"));
            LogDiagnostics = GUILayout.Toggle(LogDiagnostics, ModLocalization.T("Settings.LogDiagnostics"));
            if (LogDiagnostics)
            {
                MaxDiagnosticSortRuns = DrawIntField(
                    ModLocalization.T("Settings.MaxDiagnosticSortRuns"),
                    MaxDiagnosticSortRuns,
                    ref m_MaxDiagnosticSortRunsText,
                    1,
                    1000);
                MaxDiagnosticItemsPerRun = DrawIntField(
                    ModLocalization.T("Settings.MaxDiagnosticItemsPerRun"),
                    MaxDiagnosticItemsPerRun,
                    ref m_MaxDiagnosticItemsPerRunText,
                    1,
                    1000);
            }

            GUILayout.EndVertical();
        }

        internal void Normalize()
        {
            if (!Enum.IsDefined(typeof(ModLanguage), Language))
            {
                Language = ModLanguage.Russian;
            }

            MaxDiagnosticSortRuns = Mathf.Clamp(MaxDiagnosticSortRuns, 1, 1000);
            MaxDiagnosticItemsPerRun = Mathf.Clamp(MaxDiagnosticItemsPerRun, 1, 1000);
        }

        private void DrawLanguageSelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("Settings.Language") + ":", GUILayout.Width(280f));
            DrawLanguageButton(ModLanguage.Russian, ModLocalization.T("Settings.Language.Russian"));
            DrawLanguageButton(ModLanguage.English, ModLocalization.T("Settings.Language.English"));
            GUILayout.EndHorizontal();
        }

        private void DrawLanguageButton(ModLanguage language, string label)
        {
            bool selected = Language == language;
            bool nextSelected = GUILayout.Toggle(selected, label, "Button", GUILayout.Width(120f));
            if (nextSelected && !selected)
            {
                Language = language;
                ModLocalization.Reload();
                SmartSortController.RefreshOptionLabels();
            }
        }

        private void ApplyTextFields()
        {
            EnsureTextFieldsInitialized();

            MaxDiagnosticSortRuns = ParseOrCurrent(
                m_MaxDiagnosticSortRunsText,
                MaxDiagnosticSortRuns,
                1,
                1000);
            MaxDiagnosticItemsPerRun = ParseOrCurrent(
                m_MaxDiagnosticItemsPerRunText,
                MaxDiagnosticItemsPerRun,
                1,
                1000);

            Normalize();
            ResetTextFieldsFromValues();
        }

        private void EnsureTextFieldsInitialized()
        {
            if (m_MaxDiagnosticSortRunsText == null)
            {
                ResetTextFieldsFromValues();
            }
        }

        private void ResetTextFieldsFromValues()
        {
            m_MaxDiagnosticSortRunsText = MaxDiagnosticSortRuns.ToString(CultureInfo.InvariantCulture);
            m_MaxDiagnosticItemsPerRunText = MaxDiagnosticItemsPerRun.ToString(CultureInfo.InvariantCulture);
        }

        private static int DrawIntField(string label, int value, ref string text, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(330f));
            string nextText = GUILayout.TextField(text ?? value.ToString(CultureInfo.InvariantCulture), GUILayout.Width(80f));
            GUILayout.Label(ModLocalization.T("Settings.ActiveValue") + ": " + value, GUILayout.Width(130f));
            GUILayout.EndHorizontal();

            text = nextText;

            int parsed;
            if (int.TryParse((text ?? string.Empty).Trim(), out parsed))
            {
                return Mathf.Clamp(parsed, min, max);
            }

            return Mathf.Clamp(value, min, max);
        }

        private static int ParseOrCurrent(string text, int current, int min, int max)
        {
            int parsed;
            if (!int.TryParse((text ?? string.Empty).Trim(), out parsed))
            {
                return Mathf.Clamp(current, min, max);
            }

            return Mathf.Clamp(parsed, min, max);
        }
    }
}
