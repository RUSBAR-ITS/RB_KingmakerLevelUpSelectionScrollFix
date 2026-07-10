using System.Collections.Generic;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal static class QueueListPanel
    {
        internal static void Draw(SmartAutoBuffUi ui)
        {
            UiState state = ui.State;

            GUILayout.BeginVertical("box", GUILayout.Width(UiLayout.SidebarWidth));
            GUILayout.Label(ModLocalization.T("Queues.Title"));

            List<QueueFile> queues = ui.QueueRepository.Queues;
            if (queues.Count == 0)
            {
                ui.QueueRepository.CreateQueue("Daily buffs");
            }

            ui.ClampQueueSelection();

            state.QueueListScroll = GUILayout.BeginScrollView(state.QueueListScroll, GUILayout.Height(120f));
            for (int i = 0; i < queues.Count; i++)
            {
                string name = queues[i].Queue != null ? queues[i].Queue.Name : "<queue>";
                bool selected = i == state.SelectedQueueIndex;
                bool nextSelected = GUILayout.Toggle(selected, name, "Button");
                if (nextSelected && !selected)
                {
                    state.SelectedQueueIndex = i;
                    state.SelectedActionIndex = -1;
                    ui.SyncRenameText();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Label(ModLocalization.T("Queues.NewName"));
            state.NewQueueName = GUILayout.TextField(state.NewQueueName ?? string.Empty);
            if (GUILayout.Button(ModLocalization.T("Button.CreateQueue")))
            {
                QueueFile file = ui.QueueRepository.CreateQueue(state.NewQueueName);
                state.SelectedQueueIndex = ui.QueueRepository.Queues.IndexOf(file);
                ui.SyncRenameText();
            }

            GUILayout.Space(8f);
            GUILayout.Label(ModLocalization.T("Queues.Rename"));
            state.RenameQueueText = GUILayout.TextField(state.RenameQueueText ?? string.Empty);
            if (GUILayout.Button(ModLocalization.T("Button.RenameQueue")))
            {
                QueueFile current = ui.CurrentQueueFile();
                if (current != null)
                {
                    ui.QueueRepository.Rename(current, state.RenameQueueText);
                    ui.SyncRenameText();
                }
            }

            if (GUILayout.Button(ModLocalization.T("Button.DeleteQueue")))
            {
                ui.QueueRepository.DeleteQueue(state.SelectedQueueIndex);
                ui.ClampQueueSelection();
                ui.SyncRenameText();
            }

            GUILayout.EndVertical();
        }
    }
}
