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
            ExecutionReportPanel.Draw(this);

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
            int selectedActionIndex = -1;
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

                selectedActionIndex = AddOrMergeAction(file.Queue, action);
                added++;
            }
            else if (entry.BuffProfile != null && entry.BuffProfile.DeliveryKind == BuffDeliveryKind.Personal)
            {
                BuffQueueAction action = CreateBaseAction(entry);
                action.CastTargetId = entry.CasterId;
                action.CastTargetName = entry.CasterName;
                action.RecipientIds.Add(entry.CasterId);
                action.RecipientNames.Add(entry.CasterName);
                action.CastTargetIds.Add(entry.CasterId);
                action.CastTargetNames.Add(entry.CasterName);
                selectedActionIndex = AddOrMergeAction(file.Queue, action);
                added++;
            }
            else if (entry.TargetKind == TargetKind.NoTarget)
            {
                BuffQueueAction action = CreateBaseAction(entry);
                action.CastTargetId = entry.CasterId;
                action.CastTargetName = entry.CasterName;
                selectedActionIndex = AddOrMergeAction(file.Queue, action);
                added++;
            }
            else
            {
                BuffQueueAction action = CreateBaseAction(entry);
                foreach (TargetOption target in SelectedTargetsForEntry(entry))
                {
                    if (string.IsNullOrEmpty(action.CastTargetId))
                    {
                        action.CastTargetId = target.Id;
                        action.CastTargetName = target.Name;
                    }

                    action.CastTargetIds.Add(target.Id);
                    action.CastTargetNames.Add(target.Name);
                    action.RecipientIds.Add(target.Id);
                    action.RecipientNames.Add(target.Name);
                    added++;
                }

                if (added > 0)
                {
                    selectedActionIndex = AddOrMergeAction(file.Queue, action);
                }
            }

            if (added == 0)
            {
                return;
            }

            State.SelectedActionIndex = selectedActionIndex >= 0 ? selectedActionIndex : file.Queue.Actions.Count - 1;
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
            action.SpellVariantId = entry.SpellVariantId;
            action.SpellLevel = entry.SpellLevel;
            action.SpellName = entry.SpellName;
            action.Metamagic = new List<string>(entry.MetamagicNames);
            action.TargetKind = entry.TargetKind;
            action.DeliveryKind = entry.BuffProfile != null ? entry.BuffProfile.DeliveryKind : BuffDeliveryKind.Unknown;
            action.CandidateCasters.Add(new QueueCasterReference
            {
                CasterId = entry.CasterId,
                CasterName = entry.CasterName,
                SpellbookId = entry.SpellbookId,
                SpellbookName = entry.SpellbookName
            });
            return action;
        }

        private static int AddOrMergeAction(BuffQueueDefinition queue, BuffQueueAction action)
        {
            if (queue == null || action == null)
            {
                return -1;
            }

            int index = FindMergeIndex(queue, action);
            if (index < 0)
            {
                queue.Actions.Add(action);
                return queue.Actions.Count - 1;
            }

            BuffQueueAction existing = queue.Actions[index];
            MergeCandidates(existing, action);
            MergePairs(existing.CastTargetIds, existing.CastTargetNames, action.CastTargetIds, action.CastTargetNames);
            MergePairs(existing.RecipientIds, existing.RecipientNames, action.RecipientIds, action.RecipientNames);
            if (string.IsNullOrEmpty(existing.CastTargetId) && !string.IsNullOrEmpty(action.CastTargetId))
            {
                existing.CastTargetId = action.CastTargetId;
                existing.CastTargetName = action.CastTargetName;
            }

            return index;
        }

        private static int FindMergeIndex(BuffQueueDefinition queue, BuffQueueAction action)
        {
            for (int i = 0; i < queue.Actions.Count; i++)
            {
                if (CanMerge(queue.Actions[i], action))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool CanMerge(BuffQueueAction left, BuffQueueAction right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (!SameSpellKey(left, right))
            {
                return false;
            }

            if (IsSelfAction(left) || IsSelfAction(right))
            {
                QueueCasterReference leftCaster = FirstCandidate(left);
                QueueCasterReference rightCaster = FirstCandidate(right);
                return SameCaster(leftCaster, rightCaster);
            }

            return true;
        }

        private static bool SameSpellKey(BuffQueueAction left, BuffQueueAction right)
        {
            return string.Equals(left.SpellBlueprintId, right.SpellBlueprintId, StringComparison.Ordinal)
                && string.Equals(left.SpellVariantId ?? string.Empty, right.SpellVariantId ?? string.Empty, StringComparison.Ordinal)
                && left.SpellLevel == right.SpellLevel
                && left.TargetKind == right.TargetKind
                && left.DeliveryKind == right.DeliveryKind
                && SameMetamagic(left.Metamagic, right.Metamagic);
        }

        private static bool SameMetamagic(List<string> left, List<string> right)
        {
            left = left ?? new List<string>();
            right = right ?? new List<string>();
            if (left.Count != right.Count)
            {
                return false;
            }

            foreach (string value in left)
            {
                if (!right.Contains(value))
                {
                    return false;
                }
            }

            return true;
        }

        private static void MergeCandidates(BuffQueueAction target, BuffQueueAction source)
        {
            if (target.CandidateCasters == null)
            {
                target.CandidateCasters = new List<QueueCasterReference>();
            }

            if (source.CandidateCasters == null)
            {
                return;
            }

            foreach (QueueCasterReference candidate in source.CandidateCasters)
            {
                bool exists = false;
                foreach (QueueCasterReference current in target.CandidateCasters)
                {
                    if (SameCaster(current, candidate))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    target.CandidateCasters.Add(candidate);
                }
            }
        }

        private static void MergePairs(List<string> targetIds, List<string> targetNames, List<string> sourceIds, List<string> sourceNames)
        {
            if (sourceIds == null)
            {
                return;
            }

            for (int i = 0; i < sourceIds.Count; i++)
            {
                string id = sourceIds[i];
                string name = sourceNames != null && i < sourceNames.Count ? sourceNames[i] : string.Empty;
                if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(name))
                {
                    continue;
                }

                bool exists = false;
                for (int j = 0; j < targetIds.Count; j++)
                {
                    if (string.Equals(targetIds[j], id, StringComparison.Ordinal)
                        || (!string.IsNullOrEmpty(name) && j < targetNames.Count && string.Equals(targetNames[j], name, StringComparison.Ordinal)))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    targetIds.Add(id);
                    targetNames.Add(name);
                }
            }
        }

        private static bool SameCaster(QueueCasterReference left, QueueCasterReference right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            bool sameCaster = string.Equals(left.CasterId, right.CasterId, StringComparison.Ordinal)
                || (!string.IsNullOrEmpty(left.CasterName)
                    && string.Equals(left.CasterName, right.CasterName, StringComparison.Ordinal));
            if (!sameCaster)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(left.SpellbookId) && !string.IsNullOrEmpty(right.SpellbookId))
            {
                return string.Equals(left.SpellbookId, right.SpellbookId, StringComparison.Ordinal);
            }

            return true;
        }

        private static QueueCasterReference FirstCandidate(BuffQueueAction action)
        {
            if (action != null && action.CandidateCasters != null && action.CandidateCasters.Count > 0)
            {
                return action.CandidateCasters[0];
            }

            return action != null
                ? new QueueCasterReference
                {
                    CasterId = action.CasterId,
                    CasterName = action.CasterName,
                    SpellbookId = action.SpellbookId,
                    SpellbookName = action.SpellbookName
                }
                : null;
        }

        private static bool IsSelfAction(BuffQueueAction action)
        {
            return action != null
                && (action.TargetKind == TargetKind.Self || action.DeliveryKind == BuffDeliveryKind.Personal);
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
