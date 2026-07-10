using System;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class QueueEditorPanel
    {
        internal static void Draw(SmartAutoBuffUi ui)
        {
            UiState state = ui.State;

            GUILayout.BeginVertical("box");

            QueueFile file = ui.CurrentQueueFile();
            BuffQueueDefinition queue = file != null ? file.Queue : null;
            GUILayout.Label(ModLocalization.T("Editor.Title") + ": " + (queue != null ? queue.Name : "<queue>"));

            if (queue == null)
            {
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("Column.Order"), GUILayout.Width(UiLayout.OrderColumnWidth));
            GUILayout.Space(UiLayout.ColumnGap);
            GUILayout.Label(ModLocalization.T("Column.Caster"), GUILayout.Width(UiLayout.CasterColumnWidth));
            GUILayout.Space(UiLayout.ColumnGap);
            GUILayout.Label(ModLocalization.T("Column.Name"), GUILayout.Width(UiLayout.SpellNameColumnWidth));
            GUILayout.Space(UiLayout.ColumnGap);
            GUILayout.Label(ModLocalization.T("Column.Metamagic"), GUILayout.Width(UiLayout.MetamagicColumnWidth));
            GUILayout.Space(UiLayout.ColumnGap);
            GUILayout.Label(ModLocalization.T("Column.Target"), GUILayout.Width(UiLayout.QueueTargetColumnWidth));
            GUILayout.EndHorizontal();

            state.QueueScroll = GUILayout.BeginScrollView(state.QueueScroll, GUILayout.Height(220f));
            for (int i = 0; i < queue.Actions.Count; i++)
            {
                BuffQueueAction action = queue.Actions[i];
                GUILayout.BeginHorizontal();
                bool selected = i == state.SelectedActionIndex;
                bool nextSelected = GUILayout.Toggle(selected, (i + 1).ToString(), "Button", GUILayout.Width(UiLayout.OrderColumnWidth));
                if (nextSelected && !selected)
                {
                    state.SelectedActionIndex = i;
                }

                GUILayout.Space(UiLayout.ColumnGap);
                GUILayout.Label(action.CasterName, GUILayout.Width(UiLayout.CasterColumnWidth));
                GUILayout.Space(UiLayout.ColumnGap);
                GUILayout.Label(action.SpellName, GUILayout.Width(UiLayout.SpellNameColumnWidth));
                GUILayout.Space(UiLayout.ColumnGap);
                GUILayout.Label(MetamagicLocalization.ListOrNone(action.Metamagic), GUILayout.Width(UiLayout.MetamagicColumnWidth));
                GUILayout.Space(UiLayout.ColumnGap);
                GUILayout.Label(FormatActionTarget(action), GUILayout.Width(UiLayout.QueueTargetColumnWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUI.enabled = state.SelectedActionIndex > 0;
            if (GUILayout.Button(ModLocalization.T("Button.MoveUp"), GUILayout.Width(UiLayout.CompactButtonWidth)))
            {
                ui.MoveSelectedAction(-1);
            }

            GUI.enabled = queue.Actions.Count > 0
                && state.SelectedActionIndex >= 0
                && state.SelectedActionIndex < queue.Actions.Count - 1;
            if (GUILayout.Button(ModLocalization.T("Button.MoveDown"), GUILayout.Width(UiLayout.CompactButtonWidth)))
            {
                ui.MoveSelectedAction(1);
            }

            GUI.enabled = queue.Actions.Count > 0
                && state.SelectedActionIndex >= 0
                && state.SelectedActionIndex < queue.Actions.Count;
            if (GUILayout.Button(ModLocalization.T("Button.DeleteAction"), GUILayout.Width(UiLayout.WideButtonWidth)))
            {
                queue.Actions.RemoveAt(state.SelectedActionIndex);
                state.SelectedActionIndex = Math.Min(state.SelectedActionIndex, queue.Actions.Count - 1);
                ui.QueueRepository.Save(file);
            }

            GUI.enabled = queue.Actions.Count > 0;
            if (GUILayout.Button(ModLocalization.T("Button.ClearQueue"), GUILayout.Width(UiLayout.MediumButtonWidth)))
            {
                queue.Actions.Clear();
                state.SelectedActionIndex = -1;
                ui.QueueRepository.Save(file);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private static string FormatActionTarget(BuffQueueAction action)
        {
            if (action == null)
            {
                return ModLocalization.T("Common.None");
            }

            if (action.DeliveryKind == BuffDeliveryKind.CasterCenteredArea
                || action.DeliveryKind == BuffDeliveryKind.PointCenteredArea
                || action.DeliveryKind == BuffDeliveryKind.SelectedUnitCenteredArea
                || action.DeliveryKind == BuffDeliveryKind.WholeParty)
            {
                return ModLocalization.T("Column.Recipients") + ": " + UiHelpers.ListOrNone(action.RecipientNames);
            }

            if (!string.IsNullOrEmpty(action.CastTargetName))
            {
                return ModLocalization.T("Column.Target") + ": " + action.CastTargetName;
            }

            return UiHelpers.ListOrNone(action.RecipientNames);
        }
    }
}
