using System.Globalization;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerLargePartyFix
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public bool EnablePatch = true;
        public bool GenerateExtraFormationOffsets = true;
        public bool ExpandCustomFormations = true;
        public bool LogDiagnostics = true;
        public int MaxDiagnosticLogs = 80;
        public int ExtraColumns = 4;
        public float ExtraHorizontalSpacing = 1.6f;
        public float ExtraVerticalSpacing = 1.6f;

        private string m_MaxDiagnosticLogsText;
        private string m_ExtraColumnsText;
        private string m_ExtraHorizontalSpacingText;
        private string m_ExtraVerticalSpacingText;

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

            GUILayout.Label("Large party formation fix");
            EnablePatch = GUILayout.Toggle(EnablePatch, "Enable patch (requires mod reload if changed)");
            GenerateExtraFormationOffsets = GUILayout.Toggle(
                GenerateExtraFormationOffsets,
                "Generate safe formation positions for party members beyond the vanilla formation array");
            ExpandCustomFormations = GUILayout.Toggle(
                ExpandCustomFormations,
                "Expand custom formations so extra party member positions can be saved");

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");

            GUILayout.Label("Generated extra positions");
            ExtraColumns = DrawIntField("Extra grid columns", ExtraColumns, ref m_ExtraColumnsText, 1, 12);
            ExtraHorizontalSpacing = DrawFloatField("Horizontal spacing", ExtraHorizontalSpacing, ref m_ExtraHorizontalSpacingText, 0.5f, 6f);
            ExtraVerticalSpacing = DrawFloatField("Vertical spacing", ExtraVerticalSpacing, ref m_ExtraVerticalSpacingText, 0.5f, 6f);

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");

            GUILayout.Label("Diagnostics");
            LogDiagnostics = GUILayout.Toggle(LogDiagnostics, "Log formation diagnostics");
            if (LogDiagnostics)
            {
                MaxDiagnosticLogs = DrawIntField("Max diagnostic logs", MaxDiagnosticLogs, ref m_MaxDiagnosticLogsText, 1, 5000);
            }

            GUILayout.EndVertical();
        }

        internal void Normalize()
        {
            MaxDiagnosticLogs = Mathf.Clamp(MaxDiagnosticLogs, 1, 5000);
            ExtraColumns = Mathf.Clamp(ExtraColumns, 1, 12);
            ExtraHorizontalSpacing = Mathf.Clamp(ExtraHorizontalSpacing, 0.5f, 6f);
            ExtraVerticalSpacing = Mathf.Clamp(ExtraVerticalSpacing, 0.5f, 6f);
        }

        private void ApplyTextFields()
        {
            EnsureTextFieldsInitialized();

            MaxDiagnosticLogs = ParseOrCurrent(m_MaxDiagnosticLogsText, MaxDiagnosticLogs, 1, 5000);
            ExtraColumns = ParseOrCurrent(m_ExtraColumnsText, ExtraColumns, 1, 12);
            ExtraHorizontalSpacing = ParseOrCurrent(m_ExtraHorizontalSpacingText, ExtraHorizontalSpacing, 0.5f, 6f);
            ExtraVerticalSpacing = ParseOrCurrent(m_ExtraVerticalSpacingText, ExtraVerticalSpacing, 0.5f, 6f);

            Normalize();
            ResetTextFieldsFromValues();
        }

        private void EnsureTextFieldsInitialized()
        {
            if (m_MaxDiagnosticLogsText != null)
            {
                return;
            }

            ResetTextFieldsFromValues();
        }

        private void ResetTextFieldsFromValues()
        {
            m_MaxDiagnosticLogsText = MaxDiagnosticLogs.ToString(CultureInfo.InvariantCulture);
            m_ExtraColumnsText = ExtraColumns.ToString(CultureInfo.InvariantCulture);
            m_ExtraHorizontalSpacingText = ExtraHorizontalSpacing.ToString(CultureInfo.InvariantCulture);
            m_ExtraVerticalSpacingText = ExtraVerticalSpacing.ToString(CultureInfo.InvariantCulture);
        }

        private static int DrawIntField(string label, int value, ref string text, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(260f));
            string nextText = GUILayout.TextField(text ?? value.ToString(CultureInfo.InvariantCulture), GUILayout.Width(80f));
            GUILayout.Label("active: " + value, GUILayout.Width(120f));
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
            GUILayout.Label(label + ":", GUILayout.Width(260f));
            string nextText = GUILayout.TextField(text ?? value.ToString(CultureInfo.InvariantCulture), GUILayout.Width(80f));
            GUILayout.Label("active: " + value.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(120f));
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
