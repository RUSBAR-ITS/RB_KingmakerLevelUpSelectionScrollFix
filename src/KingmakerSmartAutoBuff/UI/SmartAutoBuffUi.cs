using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingmakerSmartAutoBuff
{
    internal sealed class SmartAutoBuffUi
    {
        internal SmartAutoBuffUi(QueueRepository queueRepository)
        {
            QueueRepository = queueRepository;
            State = new UiState();

            RefreshGameData();
            SyncRenameText();
        }

        internal QueueRepository QueueRepository { get; private set; }

        internal UiState State { get; private set; }

        internal string ExecutionStatusText
        {
            get
            {
                if (Main.Executor != null && Main.Executor.IsRunning)
                {
                    return Main.Executor.StatusText;
                }

                return string.IsNullOrEmpty(State.ExecutionStatus)
                    ? ModLocalization.T("Execution.Status.Idle")
                    : State.ExecutionStatus;
            }
        }

        internal void Draw()
        {
            Settings settings = Main.Settings;
            settings.Normalize();

            SettingsPanel.Draw(this, settings);

            if (!settings.EnableMod)
            {
                GUILayout.Label(ModLocalization.T("Status.ModDisabled"));
                return;
            }

            GUILayout.BeginHorizontal();
            QueueListPanel.Draw(this);
            SpellCatalogPanel.Draw(this);
            GUILayout.EndHorizontal();

            TargetSelectionPanel.Draw(this);
            QueueEditorPanel.Draw(this);

            if (!string.IsNullOrEmpty(State.Status))
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label(State.Status);
                GUILayout.EndVertical();
            }
        }

        internal void RefreshGameData()
        {
            State.Casters = SpellCatalog.GetCasters();
            if (State.SelectedCasterIndex >= State.Casters.Count)
            {
                State.SelectedCasterIndex = Math.Max(0, State.Casters.Count - 1);
            }

            RefreshSpellEntries();
            State.Status = ModLocalization.T("Status.DataRefreshed");
        }

        internal void RefreshSpellEntries()
        {
            CasterOption caster = CurrentCaster();
            State.SpellEntries = caster != null
                ? SpellCatalog.BuildSpellEntries(caster.Unit, State.LevelFilter)
                : new List<SpellCatalogEntry>();

            if (State.SelectedSpellIndex >= State.SpellEntries.Count)
            {
                State.SelectedSpellIndex = -1;
            }

            RefreshTargetOptions();
        }

        internal void RefreshTargetOptions()
        {
            SpellCatalogEntry entry = CurrentSpellEntry();
            State.TargetOptions = entry != null
                ? SpellCatalog.BuildTargetOptions(entry)
                : new List<TargetOption>();
        }

        internal void SelectDefaultTargets()
        {
            SpellCatalogEntry entry = CurrentSpellEntry();
            if (entry == null)
            {
                return;
            }

            if (entry.TargetKind == TargetKind.Self && entry.Caster != null)
            {
                State.SelectedTargetIds.Add(SpellCatalog.GetUnitId(entry.Caster));
            }
        }

        internal bool CanAddSelectedSpell()
        {
            SpellCatalogEntry entry = CurrentSpellEntry();
            if (entry == null || entry.TargetKind == TargetKind.Unsupported)
            {
                return false;
            }

            if (entry.TargetKind == TargetKind.NoTarget || entry.TargetKind == TargetKind.Self)
            {
                return true;
            }

            return State.SelectedTargetIds.Count > 0;
        }

        internal void AddSelectedSpellToQueue()
        {
            QueueFile file = CurrentQueueFile();
            SpellCatalogEntry entry = CurrentSpellEntry();
            if (file == null || file.Queue == null || entry == null)
            {
                return;
            }

            BuffQueueAction action = new BuffQueueAction();
            action.CasterId = entry.CasterId;
            action.CasterName = entry.CasterName;
            action.SpellbookId = entry.SpellbookId;
            action.SpellbookName = entry.SpellbookName;
            action.SpellBlueprintId = entry.SpellBlueprintId;
            action.SpellLevel = entry.SpellLevel;
            action.SpellName = entry.SpellName;
            action.Metamagic = new List<string>(entry.MetamagicNames);
            action.TargetKind = entry.TargetKind;

            foreach (TargetOption target in SelectedTargetsForEntry(entry))
            {
                action.TargetIds.Add(target.Id);
                action.TargetNames.Add(target.Name);
            }

            file.Queue.Actions.Add(action);
            State.SelectedActionIndex = file.Queue.Actions.Count - 1;
            QueueRepository.Save(file);
            State.Status = ModLocalization.T("Status.ActionAdded");
        }

        internal void MoveSelectedAction(int delta)
        {
            QueueFile file = CurrentQueueFile();
            if (file == null || file.Queue == null)
            {
                return;
            }

            int next = State.SelectedActionIndex + delta;
            if (State.SelectedActionIndex < 0 || next < 0 || next >= file.Queue.Actions.Count)
            {
                return;
            }

            BuffQueueAction action = file.Queue.Actions[State.SelectedActionIndex];
            file.Queue.Actions.RemoveAt(State.SelectedActionIndex);
            file.Queue.Actions.Insert(next, action);
            State.SelectedActionIndex = next;
            QueueRepository.Save(file);
        }

        internal void TestRunSelectedQueue()
        {
            QueueTestRunner.TestRun(CurrentQueueFile());
            State.Status = ModLocalization.T("Status.TestRunLogged");
        }

        internal void RunSelectedQueue(QueueExecutionMode mode)
        {
            if (Main.Executor == null)
            {
                Main.Executor = new BuffQueueExecutor();
            }

            Main.Executor.Start(CurrentQueueFile(), mode);
        }

        internal void StopQueueExecution()
        {
            if (Main.Executor != null)
            {
                Main.Executor.Stop(ModLocalization.T("Execution.Status.Stopped"));
            }
        }

        internal QueueFile CurrentQueueFile()
        {
            ClampQueueSelection();
            if (QueueRepository.Queues.Count == 0)
            {
                return null;
            }

            return QueueRepository.Queues[State.SelectedQueueIndex];
        }

        internal CasterOption CurrentCaster()
        {
            if (State.SelectedCasterIndex < 0 || State.SelectedCasterIndex >= State.Casters.Count)
            {
                return null;
            }

            return State.Casters[State.SelectedCasterIndex];
        }

        internal SpellCatalogEntry CurrentSpellEntry()
        {
            if (State.SelectedSpellIndex < 0 || State.SelectedSpellIndex >= State.SpellEntries.Count)
            {
                return null;
            }

            return State.SpellEntries[State.SelectedSpellIndex];
        }

        internal void ClampQueueSelection()
        {
            if (State.SelectedQueueIndex >= QueueRepository.Queues.Count)
            {
                State.SelectedQueueIndex = Math.Max(0, QueueRepository.Queues.Count - 1);
            }
        }

        internal void SyncRenameText()
        {
            QueueFile file = CurrentQueueFile();
            State.RenameQueueText = file != null && file.Queue != null ? file.Queue.Name : string.Empty;
        }

        private IEnumerable<TargetOption> SelectedTargetsForEntry(SpellCatalogEntry entry)
        {
            if (entry.TargetKind == TargetKind.Self && entry.Caster != null)
            {
                yield return new TargetOption
                {
                    Unit = entry.Caster,
                    Id = SpellCatalog.GetUnitId(entry.Caster),
                    Name = entry.CasterName
                };

                yield break;
            }

            if (entry.TargetKind == TargetKind.NoTarget)
            {
                yield break;
            }

            foreach (TargetOption target in State.TargetOptions)
            {
                if (State.SelectedTargetIds.Contains(target.Id))
                {
                    yield return target;
                }
            }
        }
    }
}
