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

            if (IsRecipientSelectionMode(entry) && entry.BuffProfile.DeliveryKind == BuffDeliveryKind.WholeParty)
            {
                foreach (TargetOption target in State.TargetOptions)
                {
                    State.SelectedTargetIds.Add(target.Id);
                }

                return;
            }

            if (entry.BuffProfile != null && entry.BuffProfile.DeliveryKind == BuffDeliveryKind.Personal && entry.Caster != null)
            {
                State.SelectedTargetIds.Add(SpellCatalog.GetUnitId(entry.Caster));
            }
        }

        internal bool CanAddSelectedSpell()
        {
            SpellCatalogEntry entry = CurrentSpellEntry();
            if (entry == null)
            {
                return false;
            }

            if (entry.BuffProfile != null && entry.BuffProfile.DeliveryKind == BuffDeliveryKind.Unsupported)
            {
                return false;
            }

            if (entry.BuffProfile != null && entry.BuffProfile.DeliveryKind == BuffDeliveryKind.Personal)
            {
                return true;
            }

            if (entry.TargetKind == TargetKind.NoTarget)
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

            int added = 0;
            if (IsRecipientSelectionMode(entry))
            {
                BuffQueueAction action = CreateBaseAction(entry);
                action.CastTargetId = entry.CasterId;
                action.CastTargetName = entry.CasterName;

                foreach (TargetOption target in SelectedTargetsForEntry(entry))
                {
                    action.RecipientIds.Add(target.Id);
                    action.RecipientNames.Add(target.Name);
                }

                file.Queue.Actions.Add(action);
                added++;
            }
            else if (entry.BuffProfile != null && entry.BuffProfile.DeliveryKind == BuffDeliveryKind.Personal)
            {
                BuffQueueAction action = CreateBaseAction(entry);
                action.CastTargetId = entry.CasterId;
                action.CastTargetName = entry.CasterName;
                action.RecipientIds.Add(entry.CasterId);
                action.RecipientNames.Add(entry.CasterName);
                file.Queue.Actions.Add(action);
                added++;
            }
            else if (entry.TargetKind == TargetKind.NoTarget)
            {
                BuffQueueAction action = CreateBaseAction(entry);
                action.CastTargetId = entry.CasterId;
                action.CastTargetName = entry.CasterName;
                file.Queue.Actions.Add(action);
                added++;
            }
            else
            {
                foreach (TargetOption target in SelectedTargetsForEntry(entry))
                {
                    BuffQueueAction action = CreateBaseAction(entry);
                    action.CastTargetId = target.Id;
                    action.CastTargetName = target.Name;
                    action.RecipientIds.Add(target.Id);
                    action.RecipientNames.Add(target.Name);
                    file.Queue.Actions.Add(action);
                    added++;
                }
            }

            if (added == 0)
            {
                return;
            }

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
            if (entry.BuffProfile != null && entry.BuffProfile.DeliveryKind == BuffDeliveryKind.Personal && entry.Caster != null)
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

        private static BuffQueueAction CreateBaseAction(SpellCatalogEntry entry)
        {
            BuffQueueAction action = new BuffQueueAction();
            action.CasterId = entry.CasterId;
            action.CasterName = entry.CasterName;
            action.SpellbookId = entry.SpellbookId;
            action.SpellbookName = entry.SpellbookName;
            action.SpellBlueprintId = entry.SpellBlueprintId;
            action.SpellLevel = entry.SpellLevel;
            action.SpellName = entry.SpellName;
            action.Metamagic = new List<string>(entry.MetamagicNames);
            action.DeliveryKind = entry.BuffProfile != null ? entry.BuffProfile.DeliveryKind : BuffDeliveryKind.Unknown;
            return action;
        }

        private static bool IsRecipientSelectionMode(SpellCatalogEntry entry)
        {
            AbilityBuffProfile profile = entry != null ? entry.BuffProfile : null;
            return profile != null
                && profile.IsFriendlyBuff
                && (profile.IsAreaBuff || profile.DeliveryKind == BuffDeliveryKind.WholeParty);
        }
    }
}
