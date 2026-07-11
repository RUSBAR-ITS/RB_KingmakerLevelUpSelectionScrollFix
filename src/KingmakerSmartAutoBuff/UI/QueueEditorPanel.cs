using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
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
                QueueActionAvailability availability = QueueActionAvailabilityEvaluator.Evaluate(action);
                Color normal = GUI.color;
                Color warning = new Color(1f, 0.45f, 0.45f);
                GUILayout.BeginHorizontal();
                bool selected = i == state.SelectedActionIndex;
                bool nextSelected = GUILayout.Toggle(selected, (i + 1).ToString(), "Button", GUILayout.Width(UiLayout.OrderColumnWidth));
                if (nextSelected && !selected)
                {
                    state.SelectedActionIndex = i;
                }

                GUILayout.Space(UiLayout.ColumnGap);
                UiHelpers.ColoredLabel(FormatCasterColumn(action, availability), availability.IsSpellAvailable ? normal : warning, GUILayout.Width(UiLayout.CasterColumnWidth));
                GUILayout.Space(UiLayout.ColumnGap);
                UiHelpers.ColoredLabel(action.SpellName, availability.IsSpellAvailable ? normal : warning, GUILayout.Width(UiLayout.SpellNameColumnWidth));
                GUILayout.Space(UiLayout.ColumnGap);
                GUILayout.Label(MetamagicLocalization.ListOrNone(action.Metamagic), GUILayout.Width(UiLayout.MetamagicColumnWidth));
                GUILayout.Space(UiLayout.ColumnGap);
                UiHelpers.ColoredWrappedLabel(
                    FormatActionTarget(action),
                    HasMissingTargets(availability) ? warning : normal,
                    GUILayout.Width(UiLayout.QueueTargetColumnWidth));
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
                state.EditingActionDraft = null;
                state.EditingActionIndex = -1;
                ui.QueueRepository.Save(file);
            }

            GUI.enabled = queue.Actions.Count > 0;
            if (GUILayout.Button(ModLocalization.T("Button.ClearQueue"), GUILayout.Width(UiLayout.MediumButtonWidth)))
            {
                queue.Actions.Clear();
                state.SelectedActionIndex = -1;
                state.EditingActionDraft = null;
                state.EditingActionIndex = -1;
                ui.QueueRepository.Save(file);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            DrawEditPanel(ui, file, queue);

            GUILayout.EndVertical();
        }

        private static void DrawEditPanel(SmartAutoBuffUi ui, QueueFile file, BuffQueueDefinition queue)
        {
            UiState state = ui.State;
            if (state.SelectedActionIndex < 0 || state.SelectedActionIndex >= queue.Actions.Count)
            {
                return;
            }

            GUILayout.BeginVertical("box");
            GUILayout.Label(ModLocalization.T("Editor.Selected") + ": " + queue.Actions[state.SelectedActionIndex].SpellName);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(ModLocalization.T("Button.EditAction"), GUILayout.Width(UiLayout.MediumButtonWidth)))
            {
                state.EditingActionIndex = state.SelectedActionIndex;
                state.EditingActionDraft = QueueActionCloner.Clone(queue.Actions[state.SelectedActionIndex]);
                state.EditScroll = Vector2.zero;
                state.EditTargetsScroll = Vector2.zero;
            }

            GUI.enabled = state.EditingActionDraft != null && state.EditingActionIndex == state.SelectedActionIndex;
            if (GUILayout.Button(ModLocalization.T("Button.SaveEdit"), GUILayout.Width(UiLayout.MediumButtonWidth)))
            {
                queue.Actions[state.EditingActionIndex] = QueueActionCloner.Clone(state.EditingActionDraft);
                ui.QueueRepository.Save(file);
                state.EditingActionDraft = null;
                state.EditingActionIndex = -1;
                ui.State.Status = ModLocalization.T("Status.ActionUpdated");
            }

            if (GUILayout.Button(ModLocalization.T("Button.CancelEdit"), GUILayout.Width(UiLayout.MediumButtonWidth)))
            {
                state.EditingActionDraft = null;
                state.EditingActionIndex = -1;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (state.EditingActionDraft != null && state.EditingActionIndex == state.SelectedActionIndex)
            {
                DrawDraftEditor(ui, state.EditingActionDraft);
            }

            GUILayout.EndVertical();
        }

        private static void DrawDraftEditor(SmartAutoBuffUi ui, BuffQueueAction draft)
        {
            UiState state = ui.State;

            GUILayout.Label(ModLocalization.T("Editor.Candidates"));
            if (IsSelfAction(draft))
            {
                GUILayout.Label(ModLocalization.T("Editor.FixedSelfCaster") + ": " + FormatCandidateNames(draft.CandidateCasters));
            }
            else
            {
                state.EditScroll = GUILayout.BeginScrollView(state.EditScroll, GUILayout.Height(UiLayout.EditCandidatesHeight));
                DrawCandidateGrid(draft, BuildCandidateOptions(draft));
                GUILayout.EndScrollView();
            }

            GUILayout.Space(6f);
            GUILayout.Label(IsAreaDelivery(draft.DeliveryKind) ? ModLocalization.T("Column.Recipients") : ModLocalization.T("Column.Target"));
            if (IsSelfAction(draft))
            {
                GUILayout.Label(ModLocalization.T("Targets.FixedSelf"));
            }
            else
            {
                List<TargetOption> targetOptions = BuildEditableTargetOptions(draft);
                state.EditTargetsScroll = GUILayout.BeginScrollView(state.EditTargetsScroll, GUILayout.Height(UiLayout.EditTargetsHeight));
                DrawTargetGrid(draft, targetOptions);
                DrawMissingSavedTargets(draft, targetOptions);
                GUILayout.EndScrollView();
            }
        }

        private static void DrawCandidateGrid(BuffQueueAction draft, List<QueueCasterReference> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                GUILayout.Label(ModLocalization.T("Common.None"));
                return;
            }

            int column = 0;
            GUILayout.BeginHorizontal();
            foreach (QueueCasterReference candidate in candidates)
            {
                bool selected = HasCandidate(draft, candidate);
                bool nextSelected = GUILayout.Toggle(selected, candidate.CasterName, "Button", GUILayout.Width(UiLayout.EditTargetButtonWidth));
                if (nextSelected && !selected)
                {
                    draft.CandidateCasters.Add(candidate);
                }
                else if (!nextSelected && selected && draft.CandidateCasters.Count > 1)
                {
                    RemoveCandidate(draft, candidate);
                }

                column++;
                if (column >= UiLayout.EditGridColumns)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    column = 0;
                }
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawTargetGrid(BuffQueueAction draft, List<TargetOption> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                GUILayout.Label(ModLocalization.T("Targets.NoneAvailable"));
                return;
            }

            int column = 0;
            GUILayout.BeginHorizontal();
            foreach (TargetOption target in targets)
            {
                bool selected = IsAreaDelivery(draft.DeliveryKind)
                    ? ContainsPair(draft.RecipientIds, draft.RecipientNames, target.Id, target.Name)
                    : ContainsPair(draft.CastTargetIds, draft.CastTargetNames, target.Id, target.Name);
                bool nextSelected = GUILayout.Toggle(selected, target.Name, "Button", GUILayout.Width(UiLayout.EditTargetButtonWidth));
                if (nextSelected && !selected)
                {
                    AddTargetToDraft(draft, target.Id, target.Name);
                }
                else if (!nextSelected && selected)
                {
                    RemoveTargetFromDraft(draft, target.Id, target.Name);
                }

                column++;
                if (column >= UiLayout.EditGridColumns)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    column = 0;
                }
            }

            GUILayout.EndHorizontal();
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

            if (action.CastTargetNames != null && action.CastTargetNames.Count > 0)
            {
                string columnKey = action.CastTargetNames.Count > 1 ? "Column.Targets" : "Column.Target";
                return ModLocalization.T(columnKey) + ": " + UiHelpers.ListOrNone(action.CastTargetNames);
            }

            if (!string.IsNullOrEmpty(action.CastTargetName))
            {
                return ModLocalization.T("Column.Target") + ": " + action.CastTargetName;
            }

            return UiHelpers.ListOrNone(action.RecipientNames);
        }

        private static string FormatCasterColumn(BuffQueueAction action, QueueActionAvailability availability)
        {
            if (availability != null && !string.IsNullOrEmpty(availability.BestCasterName))
            {
                return ModLocalization.T("Column.Casts") + ": " + availability.BestCasterName;
            }

            return FormatCandidateNames(action != null ? action.CandidateCasters : null);
        }

        private static string FormatCandidateNames(List<QueueCasterReference> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return ModLocalization.T("Common.None");
            }

            return string.Join(", ", candidates.Select(candidate => candidate.CasterName).ToArray());
        }

        private static bool HasMissingTargets(QueueActionAvailability availability)
        {
            return availability != null
                && (availability.MissingTargets.Count > 0 || availability.MissingRecipients.Count > 0);
        }

        private static bool IsSelfAction(BuffQueueAction action)
        {
            return action != null && (action.TargetKind == TargetKind.Self || action.DeliveryKind == BuffDeliveryKind.Personal);
        }

        private static bool IsAreaDelivery(BuffDeliveryKind deliveryKind)
        {
            return deliveryKind == BuffDeliveryKind.CasterCenteredArea
                || deliveryKind == BuffDeliveryKind.PointCenteredArea
                || deliveryKind == BuffDeliveryKind.SelectedUnitCenteredArea
                || deliveryKind == BuffDeliveryKind.WholeParty;
        }

        private static List<QueueCasterReference> BuildCandidateOptions(BuffQueueAction action)
        {
            List<QueueCasterReference> result = new List<QueueCasterReference>();
            foreach (CasterOption caster in SpellCatalog.GetCasters())
            {
                foreach (SpellCatalogEntry entry in SpellCatalog.BuildSpellEntries(caster.Unit, -1))
                {
                    if (!MatchesAction(entry, action))
                    {
                        continue;
                    }

                    QueueCasterReference reference = new QueueCasterReference
                    {
                        CasterId = entry.CasterId,
                        CasterName = entry.CasterName,
                        SpellbookId = entry.SpellbookId,
                        SpellbookName = entry.SpellbookName
                    };

                    if (!result.Any(current => SameCandidate(current, reference)))
                    {
                        result.Add(reference);
                    }
                }
            }

            return result.OrderBy(candidate => candidate.CasterName).ToList();
        }

        private static List<TargetOption> BuildEditableTargetOptions(BuffQueueAction draft)
        {
            if (draft == null)
            {
                return new List<TargetOption>();
            }

            if (IsAreaDelivery(draft.DeliveryKind))
            {
                return BuildPartyTargetOptions();
            }

            SpellCatalogEntry entry = SpellCatalog.FindCurrentEntries(draft).FirstOrDefault();
            if (entry != null)
            {
                return SpellCatalog.BuildTargetOptions(entry);
            }

            return BuildPartyTargetOptions();
        }

        private static List<TargetOption> BuildPartyTargetOptions()
        {
            return SpellCatalog.GetActiveParty()
                .Where(unit => unit != null)
                .Select(unit => new TargetOption
                {
                    Unit = unit,
                    Id = SpellCatalog.GetUnitId(unit),
                    Name = SpellCatalog.SafeUnitName(unit)
                })
                .GroupBy(target => target.Id)
                .Select(group => group.First())
                .OrderBy(target => target.Name)
                .ToList();
        }

        private static void DrawMissingSavedTargets(BuffQueueAction draft, List<TargetOption> currentTargets)
        {
            List<TargetOption> missing = GetMissingSavedTargets(draft, currentTargets);
            if (missing.Count == 0)
            {
                return;
            }

            GUILayout.Space(6f);
            UiHelpers.ColoredLabel(ModLocalization.T("Editor.MissingTargets"), new Color(1f, 0.45f, 0.45f));

            int column = 0;
            GUILayout.BeginHorizontal();
            foreach (TargetOption target in missing)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 0.45f, 0.45f);
                bool keep = GUILayout.Toggle(true, target.Name, "Button", GUILayout.Width(UiLayout.EditTargetButtonWidth));
                GUI.color = oldColor;

                if (!keep)
                {
                    RemoveTargetFromDraft(draft, target.Id, target.Name);
                }

                column++;
                if (column >= UiLayout.EditGridColumns)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    column = 0;
                }
            }

            GUILayout.EndHorizontal();
        }

        private static List<TargetOption> GetMissingSavedTargets(BuffQueueAction draft, List<TargetOption> currentTargets)
        {
            List<TargetOption> result = new List<TargetOption>();
            List<string> ids = IsAreaDelivery(draft.DeliveryKind) ? draft.RecipientIds : draft.CastTargetIds;
            List<string> names = IsAreaDelivery(draft.DeliveryKind) ? draft.RecipientNames : draft.CastTargetNames;
            if (ids == null)
            {
                return result;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                string name = names != null && i < names.Count ? names[i] : string.Empty;
                if (ContainsTargetOption(currentTargets, id, name))
                {
                    continue;
                }

                result.Add(new TargetOption
                {
                    Id = id,
                    Name = string.IsNullOrEmpty(name) ? id : name
                });
            }

            return result;
        }

        private static bool ContainsTargetOption(List<TargetOption> targets, string id, string name)
        {
            if (targets == null)
            {
                return false;
            }

            foreach (TargetOption target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(id) && string.Equals(target.Id, id, StringComparison.Ordinal))
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(name) && string.Equals(target.Name, name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddTargetToDraft(BuffQueueAction draft, string id, string name)
        {
            if (IsAreaDelivery(draft.DeliveryKind))
            {
                AddPair(draft.RecipientIds, draft.RecipientNames, id, name);
                return;
            }

            AddPair(draft.CastTargetIds, draft.CastTargetNames, id, name);
            AddPair(draft.RecipientIds, draft.RecipientNames, id, name);
            if (string.IsNullOrEmpty(draft.CastTargetId))
            {
                draft.CastTargetId = id;
                draft.CastTargetName = name;
            }
        }

        private static void RemoveTargetFromDraft(BuffQueueAction draft, string id, string name)
        {
            if (IsAreaDelivery(draft.DeliveryKind))
            {
                RemovePair(draft.RecipientIds, draft.RecipientNames, id, name);
                return;
            }

            RemovePair(draft.CastTargetIds, draft.CastTargetNames, id, name);
            RemovePair(draft.RecipientIds, draft.RecipientNames, id, name);
            SyncPrimaryTarget(draft);
        }

        private static bool MatchesAction(SpellCatalogEntry entry, BuffQueueAction action)
        {
            return entry != null
                && action != null
                && string.Equals(entry.SpellBlueprintId, action.SpellBlueprintId, StringComparison.Ordinal)
                && entry.SpellLevel == action.SpellLevel
                && SameMetamagic(entry.MetamagicNames, action.Metamagic);
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

        private static bool HasCandidate(BuffQueueAction action, QueueCasterReference candidate)
        {
            return action.CandidateCasters != null && action.CandidateCasters.Any(current => SameCandidate(current, candidate));
        }

        private static void RemoveCandidate(BuffQueueAction action, QueueCasterReference candidate)
        {
            action.CandidateCasters.RemoveAll(current => SameCandidate(current, candidate));
        }

        private static bool SameCandidate(QueueCasterReference left, QueueCasterReference right)
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

        private static bool ContainsPair(List<string> ids, List<string> names, string id, string name)
        {
            if (ids == null)
            {
                return false;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal)
                    || (!string.IsNullOrEmpty(name) && names != null && i < names.Count && string.Equals(names[i], name, StringComparison.Ordinal)))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddPair(List<string> ids, List<string> names, string id, string name)
        {
            if (!ContainsPair(ids, names, id, name))
            {
                ids.Add(id);
                names.Add(name);
            }
        }

        private static void RemovePair(List<string> ids, List<string> names, string id, string name)
        {
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal)
                    || (!string.IsNullOrEmpty(name) && names != null && i < names.Count && string.Equals(names[i], name, StringComparison.Ordinal)))
                {
                    ids.RemoveAt(i);
                    if (names != null && i < names.Count)
                    {
                        names.RemoveAt(i);
                    }
                }
            }
        }

        private static void SyncPrimaryTarget(BuffQueueAction action)
        {
            if (action.CastTargetIds != null && action.CastTargetIds.Count > 0)
            {
                action.CastTargetId = action.CastTargetIds[0];
                action.CastTargetName = action.CastTargetNames != null && action.CastTargetNames.Count > 0
                    ? action.CastTargetNames[0]
                    : string.Empty;
                return;
            }

            action.CastTargetId = string.Empty;
            action.CastTargetName = string.Empty;
        }
    }
}
