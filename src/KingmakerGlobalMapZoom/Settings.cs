using System.Globalization;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerGlobalMapZoom
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public ModLanguage Language = ModLanguage.Russian;
        public bool EnablePatch = true;
        public float GlobalMapFov = 28.3f;
        public bool EnableLocalMapZoom = true;
        public float LocalMapFovMin = 10f;
        public float LocalMapFovMax = 22f;
        public bool ApplyWhenEnteringGlobalMap = true;
        public bool ApplyOnSettingsSave = true;
        public bool LogDiagnostics = true;
        public int MaxDiagnosticLogs = 80;

        private string m_GlobalMapFovText;
        private string m_LocalMapFovMinText;
        private string m_LocalMapFovMaxText;
        private string m_MaxDiagnosticLogsText;

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

            GUILayout.Space(8f);
            GUILayout.Label(ModLocalization.T("Settings.GlobalMapSection"));
            ApplyWhenEnteringGlobalMap = GUILayout.Toggle(
                ApplyWhenEnteringGlobalMap,
                ModLocalization.T("Settings.ApplyWhenEnteringGlobalMap"));
            GlobalMapFov = DrawFloatField(
                ModLocalization.T("Settings.GlobalMapFov"),
                GlobalMapFov,
                ref m_GlobalMapFovText,
                10f,
                170f);

            GUILayout.Space(8f);
            GUILayout.Label(ModLocalization.T("Settings.LocalMapSection"));
            EnableLocalMapZoom = GUILayout.Toggle(EnableLocalMapZoom, ModLocalization.T("Settings.EnableLocalMapZoom"));
            LocalMapFovMin = DrawFloatField(
                ModLocalization.T("Settings.LocalMapFovMin"),
                LocalMapFovMin,
                ref m_LocalMapFovMinText,
                1f,
                170f);
            LocalMapFovMax = DrawFloatField(
                ModLocalization.T("Settings.LocalMapFovMax"),
                LocalMapFovMax,
                ref m_LocalMapFovMaxText,
                1f,
                170f);
            GUILayout.Label(ModLocalization.T("Settings.BagOfTricksHint"));

            GUILayout.Space(8f);
            ApplyOnSettingsSave = GUILayout.Toggle(
                ApplyOnSettingsSave,
                ModLocalization.T("Settings.ApplyOnSettingsSave"));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(ModLocalization.T("Settings.ApplyNow"), GUILayout.Width(220f)))
            {
                ApplyTextFields();
                GlobalMapZoomController.ApplyNow("settings button");
            }

            GUILayout.Label(ModLocalization.T("Settings.ApplyNowHint"));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");

            GUILayout.Label(ModLocalization.T("Settings.Diagnostics"));
            LogDiagnostics = GUILayout.Toggle(LogDiagnostics, ModLocalization.T("Settings.LogDiagnostics"));
            if (LogDiagnostics)
            {
                MaxDiagnosticLogs = DrawIntField(
                    ModLocalization.T("Settings.MaxDiagnosticLogs"),
                    MaxDiagnosticLogs,
                    ref m_MaxDiagnosticLogsText,
                    1,
                    5000);
            }

            GUILayout.EndVertical();
        }

        internal void Normalize()
        {
            if (!System.Enum.IsDefined(typeof(ModLanguage), Language))
            {
                Language = ModLanguage.Russian;
            }

            GlobalMapFov = Mathf.Clamp(GlobalMapFov, 10f, 170f);
            LocalMapFovMin = Mathf.Clamp(LocalMapFovMin, 1f, 170f);
            LocalMapFovMax = Mathf.Clamp(LocalMapFovMax, 1f, 170f);
            if (LocalMapFovMin > LocalMapFovMax)
            {
                float swap = LocalMapFovMin;
                LocalMapFovMin = LocalMapFovMax;
                LocalMapFovMax = swap;
            }

            MaxDiagnosticLogs = Mathf.Clamp(MaxDiagnosticLogs, 1, 5000);
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
            }
        }

        private void ApplyTextFields()
        {
            EnsureTextFieldsInitialized();

            GlobalMapFov = ParseOrCurrent(m_GlobalMapFovText, GlobalMapFov, 10f, 170f);
            LocalMapFovMin = ParseOrCurrent(m_LocalMapFovMinText, LocalMapFovMin, 1f, 170f);
            LocalMapFovMax = ParseOrCurrent(m_LocalMapFovMaxText, LocalMapFovMax, 1f, 170f);
            MaxDiagnosticLogs = ParseOrCurrent(m_MaxDiagnosticLogsText, MaxDiagnosticLogs, 1, 5000);

            Normalize();
            ResetTextFieldsFromValues();
        }

        private void EnsureTextFieldsInitialized()
        {
            if (m_GlobalMapFovText != null)
            {
                return;
            }

            ResetTextFieldsFromValues();
        }

        private void ResetTextFieldsFromValues()
        {
            m_GlobalMapFovText = GlobalMapFov.ToString(CultureInfo.InvariantCulture);
            m_LocalMapFovMinText = LocalMapFovMin.ToString(CultureInfo.InvariantCulture);
            m_LocalMapFovMaxText = LocalMapFovMax.ToString(CultureInfo.InvariantCulture);
            m_MaxDiagnosticLogsText = MaxDiagnosticLogs.ToString(CultureInfo.InvariantCulture);
        }

        private static int DrawIntField(string label, int value, ref string text, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(280f));
            string nextText = GUILayout.TextField(text ?? value.ToString(CultureInfo.InvariantCulture), GUILayout.Width(90f));
            GUILayout.Label(ModLocalization.T("Settings.ActiveValue") + ": " + value, GUILayout.Width(140f));
            GUILayout.EndHorizontal();

            text = nextText;

            int parsed;
            if (int.TryParse((text ?? string.Empty).Trim(), out parsed))
            {
                return Mathf.Clamp(parsed, min, max);
            }

            return Mathf.Clamp(value, min, max);
        }

        private static float DrawFloatField(string label, float value, ref string text, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(280f));
            string nextText = GUILayout.TextField(text ?? value.ToString(CultureInfo.InvariantCulture), GUILayout.Width(90f));
            GUILayout.Label(
                ModLocalization.T("Settings.ActiveValue") + ": " + value.ToString("0.###", CultureInfo.InvariantCulture),
                GUILayout.Width(140f));
            GUILayout.EndHorizontal();

            text = nextText;

            float parsed;
            if (TryParseFloat(text, out parsed))
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

        private static float ParseOrCurrent(string text, float current, float min, float max)
        {
            float parsed;
            if (!TryParseFloat(text, out parsed))
            {
                return Mathf.Clamp(current, min, max);
            }

            return Mathf.Clamp(parsed, min, max);
        }

        private static bool TryParseFloat(string text, out float value)
        {
            string normalized = (text ?? string.Empty).Trim().Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
