using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;

namespace KingmakerCategorizedFeatMultiplier
{
    [HarmonyPatch(typeof(LevelUpHelper), "AddFeatures")]
    internal static class LevelUpHelperAddFeaturesPatch
    {
        private static int s_CallCount;
        private static int s_DetailedSelectionLogCount;
        private static bool s_CheckedRuntimeCompatibility;
        private static readonly Dictionary<LevelUpState, List<GeneratedSelectionRecord>> s_GeneratedSelectionsByState =
            new Dictionary<LevelUpState, List<GeneratedSelectionRecord>>();

        internal static void ResetDiagnosticsCounters()
        {
            s_DetailedSelectionLogCount = 0;
        }

        internal static void NotifySettingsChanged()
        {
        }

        [HarmonyPriority(Priority.First)]
        private static bool Prefix(
            LevelUpState state,
            UnitDescriptor unit,
            IList<BlueprintFeatureBase> features,
            BlueprintScriptableObject source,
            int level)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.EnablePatch)
            {
                return true;
            }

            if (!s_CheckedRuntimeCompatibility)
            {
                s_CheckedRuntimeCompatibility = true;
                if (settings.WarnAboutBagOfTricks)
                {
                    Compatibility.CheckBagOfTricks("first AddFeatures call");
                }
            }

            bool skipOriginal;
            TryApplyOrFallback(
                state,
                unit,
                features,
                source,
                level,
                "LevelUpHelper prefix",
                out skipOriginal);

            return !skipOriginal;
        }

        internal static bool TryApplyOrFallback(
            LevelUpState state,
            UnitDescriptor unit,
            IList<BlueprintFeatureBase> features,
            BlueprintScriptableObject source,
            int level,
            string context,
            out bool skipOriginal)
        {
            skipOriginal = false;

            Settings settings = Main.Settings;
            if (settings == null || !settings.EnablePatch)
            {
                return false;
            }

            if (state == null || unit == null || features == null)
            {
                Logger.Warning(
                    "AddFeatures prefix received invalid arguments. Falling back to the original method. "
                    + "state=" + DescribeNull(state)
                    + ", unit=" + DescribeNull(unit)
                    + ", features=" + DescribeNull(features)
                    + ", context=" + context);
                return false;
            }

            bool mutated = false;
            try
            {
                s_CallCount++;
                Apply(state, unit, features, source, level, settings, context, ref mutated);
                skipOriginal = true;
                return true;
            }
            catch (Exception ex)
            {
                if (mutated)
                {
                    skipOriginal = true;
                    Logger.Exception(
                        "AddFeatures prefix failed after changing level-up state. Suppressing original method to avoid duplicate grants.",
                        ex);
                    return false;
                }

                Logger.Exception("AddFeatures prefix failed before changing level-up state. Falling back to the original method.", ex);
                return false;
            }
        }

        private static void Apply(
            LevelUpState state,
            UnitDescriptor unit,
            IList<BlueprintFeatureBase> features,
            BlueprintScriptableObject source,
            int level,
            Settings settings,
            string context,
            ref bool mutated)
        {
            List<SelectionPlan> selectionPlans = new List<SelectionPlan>();
            List<BlueprintFeature> directFeatures = new List<BlueprintFeature>();

            int maxMultiplier = 1;
            int requestedSelectionAdds = 0;

            for (int i = 0; i < features.Count; i++)
            {
                BlueprintFeatureBase featureBase = features[i];
                if (featureBase == null)
                {
                    continue;
                }

                BlueprintFeatureSelection selection = featureBase as BlueprintFeatureSelection;
                if (selection != null)
                {
                    FeatureGroup primaryGroup;
                    FeatureGroup secondaryGroup;
                    string evidence;
                    FeatureSelectionCategory category = FeatureSelectionClassifier.Classify(
                        selection,
                        source,
                        out primaryGroup,
                        out secondaryGroup,
                        out evidence);

                    int multiplier = settings.GetMultiplier(category);
                    selectionPlans.Add(new SelectionPlan(
                        selection,
                        category,
                        primaryGroup,
                        secondaryGroup,
                        evidence,
                        multiplier));

                    maxMultiplier = Math.Max(maxMultiplier, multiplier);
                    requestedSelectionAdds += multiplier;
                    LogSelectionPlan(selection, source, category, primaryGroup, secondaryGroup, evidence, multiplier, settings);
                }

                BlueprintFeature directFeature = featureBase as BlueprintFeature;
                if (directFeature != null)
                {
                    directFeatures.Add(directFeature);
                }
            }

            int actualSelectionAdds = 0;
            int reusedGeneratedSelections = 0;
            int removedGeneratedSelections = 0;
            int maxAddsRequired = 0;

            for (int i = 0; i < selectionPlans.Count; i++)
            {
                SelectionPlan plan = selectionPlans[i];
                int existingGenerated = CountGeneratedSelections(state, source, plan.Selection, level);

                if (existingGenerated > 0 && existingGenerated != plan.Multiplier)
                {
                    int removed = RemoveGeneratedSelections(state, source, plan.Selection, level);
                    removedGeneratedSelections += removed;
                    existingGenerated = 0;
                    if (removed > 0)
                    {
                        mutated = true;
                    }
                }

                if (existingGenerated > 0)
                {
                    reusedGeneratedSelections += existingGenerated;
                    selectionPlans[i] = plan.WithAddsRequired(0);
                    continue;
                }

                actualSelectionAdds += plan.Multiplier;
                maxAddsRequired = Math.Max(maxAddsRequired, plan.Multiplier);
                selectionPlans[i] = plan.WithAddsRequired(plan.Multiplier);
            }

            for (int round = 0; round < maxAddsRequired; round++)
            {
                for (int i = 0; i < selectionPlans.Count; i++)
                {
                    SelectionPlan plan = selectionPlans[i];
                    if (round >= plan.AddsRequired)
                    {
                        continue;
                    }

                    FeatureSelectionState addedSelection = state.AddSelection(null, source, plan.Selection, level);
                    TrackGeneratedSelection(state, source, plan.Selection, level, addedSelection, plan.Category);
                    mutated = true;
                }
            }

            for (int i = 0; i < directFeatures.Count; i++)
            {
                AddDirectFeature(state, unit, directFeatures[i], source);
                mutated = true;
            }

            if (settings.LogAddFeaturesCalls || maxMultiplier > 1)
            {
                Logger.Info(
                    "AddFeatures intercepted. call=" + s_CallCount
                    + ", source=" + DescribeBlueprint(source)
                    + ", level=" + level
                    + ", inputFeatures=" + features.Count
                    + ", selections=" + selectionPlans.Count
                    + ", requestedSelectionAdds=" + requestedSelectionAdds
                    + ", actualSelectionAdds=" + actualSelectionAdds
                    + ", reusedGeneratedSelections=" + reusedGeneratedSelections
                    + ", removedGeneratedSelections=" + removedGeneratedSelections
                    + ", directFeatureGrants=" + directFeatures.Count
                    + ", maxMultiplier=" + maxMultiplier
                    + ", settingsGeneration=" + settings.SettingsGeneration
                    + ", context=" + context);
            }
        }

        private static int CountGeneratedSelections(
            LevelUpState state,
            BlueprintScriptableObject source,
            BlueprintFeatureSelection selection,
            int level)
        {
            List<GeneratedSelectionRecord> records;
            if (!s_GeneratedSelectionsByState.TryGetValue(state, out records))
            {
                return 0;
            }

            int count = 0;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                GeneratedSelectionRecord record = records[i];
                if (!IsSelectionRecordAlive(state, record))
                {
                    records.RemoveAt(i);
                    continue;
                }

                if (IsSameGeneratedSelectionKey(record, source, selection, level))
                {
                    count++;
                }
            }

            if (records.Count == 0)
            {
                s_GeneratedSelectionsByState.Remove(state);
            }

            return count;
        }

        private static int RemoveGeneratedSelections(
            LevelUpState state,
            BlueprintScriptableObject source,
            BlueprintFeatureSelection selection,
            int level)
        {
            List<GeneratedSelectionRecord> records;
            if (!s_GeneratedSelectionsByState.TryGetValue(state, out records))
            {
                return 0;
            }

            int removed = 0;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                GeneratedSelectionRecord record = records[i];
                if (!IsSameGeneratedSelectionKey(record, source, selection, level))
                {
                    continue;
                }

                if (state.Selections != null && record.SelectionState != null)
                {
                    state.Selections.Remove(record.SelectionState);
                }

                records.RemoveAt(i);
                removed++;
            }

            if (records.Count == 0)
            {
                s_GeneratedSelectionsByState.Remove(state);
            }

            return removed;
        }

        private static void TrackGeneratedSelection(
            LevelUpState state,
            BlueprintScriptableObject source,
            BlueprintFeatureSelection selection,
            int level,
            FeatureSelectionState selectionState,
            FeatureSelectionCategory category)
        {
            if (selectionState == null)
            {
                Logger.Warning("LevelUpState.AddSelection returned null for " + DescribeBlueprint(selection));
                return;
            }

            List<GeneratedSelectionRecord> records;
            if (!s_GeneratedSelectionsByState.TryGetValue(state, out records))
            {
                records = new List<GeneratedSelectionRecord>();
                s_GeneratedSelectionsByState[state] = records;
            }

            records.Add(new GeneratedSelectionRecord(source, selection, level, selectionState, category));
        }

        private static bool IsSelectionRecordAlive(LevelUpState state, GeneratedSelectionRecord record)
        {
            return record.SelectionState != null
                && state.Selections != null
                && state.Selections.Contains(record.SelectionState);
        }

        private static bool IsSameGeneratedSelectionKey(
            GeneratedSelectionRecord record,
            BlueprintScriptableObject source,
            BlueprintFeatureSelection selection,
            int level)
        {
            return record.Level == level
                && ReferenceEquals(record.Source, source)
                && ReferenceEquals(record.Selection, selection);
        }

        private static void AddDirectFeature(
            LevelUpState state,
            UnitDescriptor unit,
            BlueprintFeature feature,
            BlueprintScriptableObject source)
        {
            Feature fact = UnitHelper.AddFact(unit, feature, null, null) as Feature;

            BlueprintProgression progression = feature as BlueprintProgression;
            if (progression != null)
            {
                LevelUpHelper.UpdateProgression(state, unit, progression);
            }

            if (fact != null)
            {
                fact.Source = source;
            }
            else
            {
                Logger.Warning("UnitHelper.AddFact did not return a Feature for " + DescribeBlueprint(feature));
            }
        }

        private static void LogSelectionPlan(
            BlueprintFeatureSelection selection,
            BlueprintScriptableObject source,
            FeatureSelectionCategory category,
            FeatureGroup primaryGroup,
            FeatureGroup secondaryGroup,
            string evidence,
            int multiplier,
            Settings settings)
        {
            if (!settings.LogSelectionDetails)
            {
                return;
            }

            if (!MatchesDiagnosticsFilters(settings, source, selection))
            {
                return;
            }

            if (s_DetailedSelectionLogCount >= settings.MaxDetailedSelectionLogs)
            {
                return;
            }

            s_DetailedSelectionLogCount++;
            Logger.Info(
                "Selection category detected. source=" + DescribeBlueprint(source)
                + DescribeLocalizedPart("sourceName", source, settings)
                + ", selection=" + DescribeBlueprint(selection)
                + DescribeLocalizedPart("selectionName", selection, settings)
                + ", category=" + category
                + ", group=" + primaryGroup
                + ", group2=" + secondaryGroup
                + ", multiplier=" + multiplier
                + ", evidence=" + evidence);

            if (settings.LogAllFeatures)
            {
                LogSelectionFeatures(selection, settings);
            }
        }

        private static string DescribeBlueprint(BlueprintScriptableObject blueprint)
        {
            if (blueprint == null)
            {
                return "<null>";
            }

            return blueprint.GetType().Name + ":" + blueprint.name;
        }

        private static string DescribeLocalizedPart(string label, BlueprintScriptableObject blueprint, Settings settings)
        {
            if (!settings.LogLocalizedSelectionNames)
            {
                return string.Empty;
            }

            string displayName = GetDisplayName(blueprint);
            return string.IsNullOrEmpty(displayName) ? string.Empty : ", " + label + "=\"" + displayName + "\"";
        }

        private static string GetDisplayName(BlueprintScriptableObject blueprint)
        {
            BlueprintUnitFact fact = blueprint as BlueprintUnitFact;
            return fact != null ? fact.Name : string.Empty;
        }

        private static bool MatchesDiagnosticsFilters(
            Settings settings,
            BlueprintScriptableObject source,
            BlueprintFeatureSelection selection)
        {
            return MatchesFilter(settings.DiagnosticSourceNameFilter, source)
                && MatchesFilter(settings.DiagnosticSelectionNameFilter, selection);
        }

        private static bool MatchesFilter(string filter, BlueprintScriptableObject blueprint)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }

            string internalName = blueprint != null ? blueprint.name ?? string.Empty : string.Empty;
            string displayName = GetDisplayName(blueprint);

            return internalName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || (!string.IsNullOrEmpty(displayName)
                    && displayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void LogSelectionFeatures(BlueprintFeatureSelection selection, Settings settings)
        {
            if (selection == null)
            {
                return;
            }

            Logger.Info(
                "Selection feature lists. selection=" + DescribeBlueprint(selection)
                + DescribeLocalizedPart("selectionName", selection, settings)
                + ", Features=" + DescribeFeatureArray(selection.Features, settings)
                + ", AllFeatures=" + DescribeFeatureArray(selection.AllFeatures, settings));
        }

        private static string DescribeFeatureArray(BlueprintFeature[] features, Settings settings)
        {
            if (features == null)
            {
                return "<null>";
            }

            const int maxItems = 40;
            List<string> parts = new List<string>();
            int count = Math.Min(features.Length, maxItems);
            for (int i = 0; i < count; i++)
            {
                BlueprintFeature feature = features[i];
                parts.Add(DescribeBlueprint(feature) + DescribeLocalizedPart("name", feature, settings));
            }

            if (features.Length > maxItems)
            {
                parts.Add("... +" + (features.Length - maxItems) + " more");
            }

            return "[" + string.Join("; ", parts.ToArray()) + "]";
        }

        private static string DescribeNull(object value)
        {
            return value == null ? "<null>" : "ok";
        }

        private struct SelectionPlan
        {
            public readonly BlueprintFeatureSelection Selection;
            public readonly FeatureSelectionCategory Category;
            public readonly FeatureGroup PrimaryGroup;
            public readonly FeatureGroup SecondaryGroup;
            public readonly string Evidence;
            public readonly int Multiplier;
            public readonly int AddsRequired;

            public SelectionPlan(
                BlueprintFeatureSelection selection,
                FeatureSelectionCategory category,
                FeatureGroup primaryGroup,
                FeatureGroup secondaryGroup,
                string evidence,
                int multiplier)
                : this(selection, category, primaryGroup, secondaryGroup, evidence, multiplier, multiplier)
            {
            }

            private SelectionPlan(
                BlueprintFeatureSelection selection,
                FeatureSelectionCategory category,
                FeatureGroup primaryGroup,
                FeatureGroup secondaryGroup,
                string evidence,
                int multiplier,
                int addsRequired)
            {
                Selection = selection;
                Category = category;
                PrimaryGroup = primaryGroup;
                SecondaryGroup = secondaryGroup;
                Evidence = evidence;
                Multiplier = multiplier;
                AddsRequired = addsRequired;
            }

            public SelectionPlan WithAddsRequired(int addsRequired)
            {
                return new SelectionPlan(
                    Selection,
                    Category,
                    PrimaryGroup,
                    SecondaryGroup,
                    Evidence,
                    Multiplier,
                    addsRequired);
            }
        }

        private struct GeneratedSelectionRecord
        {
            public readonly BlueprintScriptableObject Source;
            public readonly BlueprintFeatureSelection Selection;
            public readonly int Level;
            public readonly FeatureSelectionState SelectionState;
            public readonly FeatureSelectionCategory Category;

            public GeneratedSelectionRecord(
                BlueprintScriptableObject source,
                BlueprintFeatureSelection selection,
                int level,
                FeatureSelectionState selectionState,
                FeatureSelectionCategory category)
            {
                Source = source;
                Selection = selection;
                Level = level;
                SelectionState = selectionState;
                Category = category;
            }
        }
    }
}
