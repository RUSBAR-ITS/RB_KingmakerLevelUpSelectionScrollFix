using System.Globalization;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerPartySelectionExpansion
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public bool EnablePatch = true;
        public int MaxActivePartySize = 14;
        public bool EnableActivePartyScroll = true;
        public bool ShowActivePartyScrollbar = true;
        public float MaxActivePartyListHeight = 520f;
        public float ScrollSensitivity = 35f;
        public bool LogDiagnostics = true;
        public int MaxDiagnosticLogs = 80;
        public bool DumpGroupManagerUiHierarchy = false;
        public int DumpUiHierarchyMaxRuns = 2;
        public int DumpUiHierarchyMaxDepth = 16;
        public int DumpUiHierarchyMaxNodes = 2000;

        private string m_MaxActivePartySizeText;
        private string m_MaxActivePartyListHeightText;
        private string m_ScrollSensitivityText;
        private string m_MaxDiagnosticLogsText;
        private string m_DumpUiHierarchyMaxRunsText;
        private string m_DumpUiHierarchyMaxDepthText;
        private string m_DumpUiHierarchyMaxNodesText;

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

            GUILayout.Label("Party selection expansion");
            EnablePatch = GUILayout.Toggle(EnablePatch, "Enable patch (requires mod reload if changed)");
            MaxActivePartySize = DrawIntField(
                "Max active party size, including main character",
                MaxActivePartySize,
                ref m_MaxActivePartySizeText,
                6,
                30);

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");

            GUILayout.Label("Active party list UI");
            EnableActivePartyScroll = GUILayout.Toggle(EnableActivePartyScroll, "Enable scrolling for active party slots");
            ShowActivePartyScrollbar = GUILayout.Toggle(ShowActivePartyScrollbar, "Show active party scrollbar");
            MaxActivePartyListHeight = DrawFloatField(
                "Max active party list height",
                MaxActivePartyListHeight,
                ref m_MaxActivePartyListHeightText,
                120f,
                2000f);
            ScrollSensitivity = DrawFloatField(
                "Scroll sensitivity",
                ScrollSensitivity,
                ref m_ScrollSensitivityText,
                1f,
                500f);

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");

            GUILayout.Label("Diagnostics");
            LogDiagnostics = GUILayout.Toggle(LogDiagnostics, "Log party selection diagnostics");
            if (LogDiagnostics)
            {
                MaxDiagnosticLogs = DrawIntField("Max diagnostic logs", MaxDiagnosticLogs, ref m_MaxDiagnosticLogsText, 1, 5000);
            }

            DumpGroupManagerUiHierarchy = GUILayout.Toggle(DumpGroupManagerUiHierarchy, "Dump group manager UI hierarchy");
            if (DumpGroupManagerUiHierarchy)
            {
                DumpUiHierarchyMaxRuns = DrawIntField("Dump max runs", DumpUiHierarchyMaxRuns, ref m_DumpUiHierarchyMaxRunsText, 1, 20);
                DumpUiHierarchyMaxDepth = DrawIntField("Dump max depth", DumpUiHierarchyMaxDepth, ref m_DumpUiHierarchyMaxDepthText, 1, 40);
                DumpUiHierarchyMaxNodes = DrawIntField("Dump max nodes", DumpUiHierarchyMaxNodes, ref m_DumpUiHierarchyMaxNodesText, 50, 20000);
            }

            GUILayout.EndVertical();
        }

        internal void Normalize()
        {
            MaxActivePartySize = Mathf.Clamp(MaxActivePartySize, 6, 30);
            MaxActivePartyListHeight = Mathf.Clamp(MaxActivePartyListHeight, 120f, 2000f);
            ScrollSensitivity = Mathf.Clamp(ScrollSensitivity, 1f, 500f);
            MaxDiagnosticLogs = Mathf.Clamp(MaxDiagnosticLogs, 1, 5000);
            DumpUiHierarchyMaxRuns = Mathf.Clamp(DumpUiHierarchyMaxRuns, 1, 20);
            DumpUiHierarchyMaxDepth = Mathf.Clamp(DumpUiHierarchyMaxDepth, 1, 40);
            DumpUiHierarchyMaxNodes = Mathf.Clamp(DumpUiHierarchyMaxNodes, 50, 20000);
        }

        private void ApplyTextFields()
        {
            EnsureTextFieldsInitialized();

            MaxActivePartySize = ParseOrCurrent(m_MaxActivePartySizeText, MaxActivePartySize, 6, 30);
            MaxActivePartyListHeight = ParseOrCurrent(m_MaxActivePartyListHeightText, MaxActivePartyListHeight, 120f, 2000f);
            ScrollSensitivity = ParseOrCurrent(m_ScrollSensitivityText, ScrollSensitivity, 1f, 500f);
            MaxDiagnosticLogs = ParseOrCurrent(m_MaxDiagnosticLogsText, MaxDiagnosticLogs, 1, 5000);
            DumpUiHierarchyMaxRuns = ParseOrCurrent(m_DumpUiHierarchyMaxRunsText, DumpUiHierarchyMaxRuns, 1, 20);
            DumpUiHierarchyMaxDepth = ParseOrCurrent(m_DumpUiHierarchyMaxDepthText, DumpUiHierarchyMaxDepth, 1, 40);
            DumpUiHierarchyMaxNodes = ParseOrCurrent(m_DumpUiHierarchyMaxNodesText, DumpUiHierarchyMaxNodes, 50, 20000);

            Normalize();
            ResetTextFieldsFromValues();
        }

        private void EnsureTextFieldsInitialized()
        {
            if (m_MaxActivePartySizeText != null)
            {
                return;
            }

            ResetTextFieldsFromValues();
        }

        private void ResetTextFieldsFromValues()
        {
            m_MaxActivePartySizeText = MaxActivePartySize.ToString(CultureInfo.InvariantCulture);
            m_MaxActivePartyListHeightText = MaxActivePartyListHeight.ToString(CultureInfo.InvariantCulture);
            m_ScrollSensitivityText = ScrollSensitivity.ToString(CultureInfo.InvariantCulture);
            m_MaxDiagnosticLogsText = MaxDiagnosticLogs.ToString(CultureInfo.InvariantCulture);
            m_DumpUiHierarchyMaxRunsText = DumpUiHierarchyMaxRuns.ToString(CultureInfo.InvariantCulture);
            m_DumpUiHierarchyMaxDepthText = DumpUiHierarchyMaxDepth.ToString(CultureInfo.InvariantCulture);
            m_DumpUiHierarchyMaxNodesText = DumpUiHierarchyMaxNodes.ToString(CultureInfo.InvariantCulture);
        }

        private static int DrawIntField(string label, int value, ref string text, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(320f));
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
            GUILayout.Label(label + ":", GUILayout.Width(320f));
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
