using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class SpellCatalogPanel
    {
        internal static void Draw(SmartAutoBuffUi ui)
        {
            UiState state = ui.State;

            GUILayout.BeginVertical("box");
            GUILayout.Label(ModLocalization.T("Catalog.Title"));

            if (state.Casters.Count == 0)
            {
                GUILayout.Label(ModLocalization.T("Status.NoCasters"));
                GUILayout.EndVertical();
                return;
            }

            DrawCasterFilter(ui);
            DrawLevelFilter(ui);
            DrawSpellTable(ui);

            GUILayout.EndVertical();
        }

        private static void DrawCasterFilter(SmartAutoBuffUi ui)
        {
            UiState state = ui.State;

            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("Filter.Caster") + ":", GUILayout.Width(UiLayout.CasterFilterLabelWidth));
            for (int i = 0; i < state.Casters.Count; i++)
            {
                bool selected = i == state.SelectedCasterIndex;
                bool nextSelected = GUILayout.Toggle(selected, state.Casters[i].Name, "Button", GUILayout.Width(UiLayout.CasterButtonWidth));
                if (nextSelected && !selected)
                {
                    state.SelectedCasterIndex = i;
                    state.LevelFilter = -1;
                    state.SelectedSpellIndex = -1;
                    state.SelectedTargetIds.Clear();
                    ui.RefreshSpellEntries();
                }
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawLevelFilter(SmartAutoBuffUi ui)
        {
            CasterOption caster = ui.CurrentCaster();
            int maxLevel = caster != null ? caster.MaxSpellLevel : 0;

            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("Filter.Level") + ":", GUILayout.Width(UiLayout.LevelFilterLabelWidth));
            DrawLevelButton(ui, -1, ModLocalization.T("Filter.Level.All"));
            for (int level = 0; level <= maxLevel; level++)
            {
                DrawLevelButton(ui, level, level.ToString());
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawLevelButton(SmartAutoBuffUi ui, int level, string label)
        {
            UiState state = ui.State;
            bool selected = state.LevelFilter == level;
            bool nextSelected = GUILayout.Toggle(selected, label, "Button", GUILayout.Width(UiLayout.LevelButtonWidth));
            if (nextSelected && !selected)
            {
                state.LevelFilter = level;
                state.SelectedSpellIndex = -1;
                state.SelectedTargetIds.Clear();
                ui.RefreshSpellEntries();
            }
        }

        private static void DrawSpellTable(SmartAutoBuffUi ui)
        {
            UiState state = ui.State;

            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("Column.Name"), GUILayout.Width(UiLayout.SpellNameColumnWidth));
            GUILayout.Space(UiLayout.ColumnGap);
            GUILayout.Label(ModLocalization.T("Column.Metamagic"), GUILayout.Width(UiLayout.MetamagicColumnWidth));
            GUILayout.Space(UiLayout.ColumnGap);
            GUILayout.Label(ModLocalization.T("Column.Target"), GUILayout.Width(UiLayout.TargetColumnWidth));
            GUILayout.Space(UiLayout.ColumnGap);
            GUILayout.Label(ModLocalization.T("Column.Description"));
            GUILayout.EndHorizontal();

            state.SpellScroll = GUILayout.BeginScrollView(state.SpellScroll, GUILayout.Height(330f));

            for (int i = 0; i < state.SpellEntries.Count; i++)
            {
                SpellCatalogEntry entry = state.SpellEntries[i];
                GUILayout.BeginHorizontal();

                bool selected = i == state.SelectedSpellIndex;
                bool nextSelected = GUILayout.Toggle(selected, entry.SpellName, "Button", GUILayout.Width(UiLayout.SpellNameColumnWidth));
                if (nextSelected && !selected)
                {
                    state.SelectedSpellIndex = i;
                    state.SelectedTargetIds.Clear();
                    ui.RefreshTargetOptions();
                    ui.SelectDefaultTargets();
                }

                GUILayout.Space(UiLayout.ColumnGap);
                GUILayout.Label(entry.MetamagicText, GUILayout.Width(UiLayout.MetamagicColumnWidth));
                GUILayout.Space(UiLayout.ColumnGap);
                GUILayout.Label(entry.TargetSummary, GUILayout.Width(UiLayout.TargetColumnWidth));
                GUILayout.Space(UiLayout.ColumnGap);
                GUILayout.Label(entry.Description, UiHelpers.WrappedLabel, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            SpellCatalogEntry selectedEntry = ui.CurrentSpellEntry();
            if (selectedEntry != null)
            {
                GUILayout.Label(ModLocalization.T("Catalog.Selected") + ": " + selectedEntry.SpellName);
                state.DescriptionScroll = GUILayout.BeginScrollView(state.DescriptionScroll, GUILayout.Height(70f));
                GUILayout.Label(selectedEntry.Description);
                GUILayout.EndScrollView();
            }
        }
    }
}
