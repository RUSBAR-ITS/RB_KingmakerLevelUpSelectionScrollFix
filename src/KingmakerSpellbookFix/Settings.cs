using UnityEngine;
using UnityModManagerNet;

namespace KingmakerSpellbookFix
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public bool EnablePatch = true;
        public bool FixMetamagicBookmarkPages = true;
        public bool RecalculateUndercountedRegularPages = false;
        public bool LogPageCalculations = true;
        public int MaxPageCalculationLogs = 80;

        private string m_MaxPageCalculationLogsText;

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

            GUILayout.Label("Spellbook page fix");
            EnablePatch = GUILayout.Toggle(EnablePatch, "Enable patch (requires mod reload if changed)");
            FixMetamagicBookmarkPages = GUILayout.Toggle(
                FixMetamagicBookmarkPages,
                "Fix pages on the metamagic/custom spells bookmark");
            RecalculateUndercountedRegularPages = GUILayout.Toggle(
                RecalculateUndercountedRegularPages,
                "Also correct regular spell levels if the current visible list has more pages than vanilla calculated");

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");

            GUILayout.Label("Diagnostics");
            LogPageCalculations = GUILayout.Toggle(LogPageCalculations, "Log page calculations");
            if (LogPageCalculations)
            {
                MaxPageCalculationLogs = DrawIntField("Max page calculation logs", MaxPageCalculationLogs, ref m_MaxPageCalculationLogsText, 1, 5000);
            }

            GUILayout.EndVertical();
        }

        internal void Normalize()
        {
            MaxPageCalculationLogs = Mathf.Clamp(MaxPageCalculationLogs, 1, 5000);
        }

        private void ApplyTextFields()
        {
            EnsureTextFieldsInitialized();
            MaxPageCalculationLogs = ParseOrCurrent(m_MaxPageCalculationLogsText, MaxPageCalculationLogs, 1, 5000);
            Normalize();
            ResetTextFieldsFromValues();
        }

        private void EnsureTextFieldsInitialized()
        {
            if (m_MaxPageCalculationLogsText != null)
            {
                return;
            }

            ResetTextFieldsFromValues();
        }

        private void ResetTextFieldsFromValues()
        {
            m_MaxPageCalculationLogsText = MaxPageCalculationLogs.ToString();
        }

        private static int DrawIntField(string label, int value, ref string text, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(280f));
            string nextText = GUILayout.TextField(text ?? value.ToString(), GUILayout.Width(80f));
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
