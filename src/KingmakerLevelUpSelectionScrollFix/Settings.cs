using UnityEngine;
using UnityModManagerNet;

namespace KingmakerLevelUpSelectionScrollFix
{
    public sealed class Settings : UnityModManager.ModSettings
    {
        public bool EnablePatch = true;
        public int MaxSelectorHeight = 220;
        public float ScrollSensitivity = 35f;
        public bool ShowScrollbar = true;
        public bool DumpUiHierarchy = false;
        public int DumpUiHierarchyMaxRuns = 2;
        public int DumpUiHierarchyMaxDepth = 10;
        public int DumpUiHierarchyMaxNodes = 2500;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        internal void Draw()
        {
            GUILayout.BeginVertical("box");

            GUILayout.Label("Level-up feature selection scroll fix");
            EnablePatch = GUILayout.Toggle(EnablePatch, "Enable patch (requires mod reload if changed)");
            ShowScrollbar = GUILayout.Toggle(ShowScrollbar, "Show vertical scrollbar");

            MaxSelectorHeight = DrawIntSlider("Max selector height", MaxSelectorHeight, 80, 420);
            ScrollSensitivity = DrawFloatSlider("Scroll sensitivity", ScrollSensitivity, 5f, 100f);

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");

            GUILayout.Label("Diagnostics");
            DumpUiHierarchy = GUILayout.Toggle(DumpUiHierarchy, "Dump relevant UI hierarchy when the patch runs");
            if (DumpUiHierarchy)
            {
                DumpUiHierarchyMaxRuns = DrawIntSlider("Dump max runs", DumpUiHierarchyMaxRuns, 1, 5);
                DumpUiHierarchyMaxDepth = DrawIntSlider("Dump max depth", DumpUiHierarchyMaxDepth, 4, 16);
                DumpUiHierarchyMaxNodes = DrawIntSlider("Dump max nodes", DumpUiHierarchyMaxNodes, 200, 5000);
            }

            GUILayout.EndVertical();
        }

        private static int DrawIntSlider(string label, int value, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ": " + value, GUILayout.Width(260f));
            float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(260f));
            GUILayout.EndHorizontal();
            return Mathf.RoundToInt(sliderValue);
        }

        private static float DrawFloatSlider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ": " + value.ToString("0"), GUILayout.Width(260f));
            float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(260f));
            GUILayout.EndHorizontal();
            return Mathf.Round(sliderValue);
        }
    }
}
