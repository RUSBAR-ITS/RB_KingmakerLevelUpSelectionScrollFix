using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Kingmaker.UI.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace KingmakerCategorizedFeatMultiplier
{
    [HarmonyPatch(typeof(CharBSelectionSwitchFeatures), "SetupFeatureSelection")]
    internal static class SelectionSwitchOrderPatch
    {
        private static readonly FieldInfo ShowedFeatureCollectionsField =
            AccessTools.Field(typeof(CharBSelectionSwitchFeatures), "m_ShowedFeatureCollections");

        private static bool s_MissingFieldWarningLogged;
        private static int s_OrderLogCount;

        internal static void ResetDiagnosticsCounters()
        {
            s_OrderLogCount = 0;
        }

        [HarmonyPriority(Priority.First)]
        private static void Prefix(CharBSelectionSwitchFeatures __instance)
        {
            Settings settings = Main.Settings;
            if (settings == null || !settings.EnablePatch)
            {
                return;
            }

            try
            {
                NormalizeOrder(__instance, settings);
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to normalize level-up selection switch order.", ex);
            }
        }

        private static void NormalizeOrder(CharBSelectionSwitchFeatures instance, Settings settings)
        {
            if (ShowedFeatureCollectionsField == null)
            {
                if (!s_MissingFieldWarningLogged)
                {
                    s_MissingFieldWarningLogged = true;
                    Logger.Warning("Could not find CharBSelectionSwitchFeatures.m_ShowedFeatureCollections.");
                }

                return;
            }

            IList<FeatureSelectionState> showedFeatureCollections =
                ShowedFeatureCollectionsField.GetValue(instance) as IList<FeatureSelectionState>;
            if (showedFeatureCollections == null || showedFeatureCollections.Count < 2)
            {
                return;
            }

            List<FeatureSelectionState> original = Copy(showedFeatureCollections);
            List<FeatureSelectionState> ordered = BuildParentFirstOrder(original);
            if (HasSameOrder(original, ordered))
            {
                return;
            }

            showedFeatureCollections.Clear();
            for (int i = 0; i < ordered.Count; i++)
            {
                showedFeatureCollections.Add(ordered[i]);
            }

            if (settings.LogSelectionDetails && s_OrderLogCount < settings.MaxDetailedSelectionLogs)
            {
                s_OrderLogCount++;
                Logger.Info(
                    "Selection switch order normalized. before="
                    + DescribeOrder(original)
                    + ", after="
                    + DescribeOrder(ordered));
            }
        }

        private static List<FeatureSelectionState> BuildParentFirstOrder(List<FeatureSelectionState> original)
        {
            HashSet<FeatureSelectionState> visibleStates = new HashSet<FeatureSelectionState>(original);
            HashSet<FeatureSelectionState> addedStates = new HashSet<FeatureSelectionState>();
            List<FeatureSelectionState> ordered = new List<FeatureSelectionState>(original.Count);

            for (int i = 0; i < original.Count; i++)
            {
                FeatureSelectionState state = original[i];
                if (IsVisibleRoot(state, visibleStates))
                {
                    AddStateAndVisibleChildren(state, original, visibleStates, addedStates, ordered);
                }
            }

            for (int i = 0; i < original.Count; i++)
            {
                AddStateAndVisibleChildren(original[i], original, visibleStates, addedStates, ordered);
            }

            return ordered;
        }

        private static bool IsVisibleRoot(
            FeatureSelectionState state,
            HashSet<FeatureSelectionState> visibleStates)
        {
            return state == null
                || state.Parent == null
                || !visibleStates.Contains(state.Parent);
        }

        private static void AddStateAndVisibleChildren(
            FeatureSelectionState state,
            List<FeatureSelectionState> original,
            HashSet<FeatureSelectionState> visibleStates,
            HashSet<FeatureSelectionState> addedStates,
            List<FeatureSelectionState> ordered)
        {
            if (!addedStates.Add(state))
            {
                return;
            }

            ordered.Add(state);
            if (state == null)
            {
                return;
            }

            FeatureSelectionState next = state.Next;
            if (next != null && visibleStates.Contains(next))
            {
                AddStateAndVisibleChildren(next, original, visibleStates, addedStates, ordered);
            }

            for (int i = 0; i < original.Count; i++)
            {
                FeatureSelectionState candidate = original[i];
                if (candidate != null && ReferenceEquals(candidate.Parent, state))
                {
                    AddStateAndVisibleChildren(candidate, original, visibleStates, addedStates, ordered);
                }
            }
        }

        private static bool HasSameOrder(
            List<FeatureSelectionState> first,
            List<FeatureSelectionState> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (int i = 0; i < first.Count; i++)
            {
                if (!ReferenceEquals(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<FeatureSelectionState> Copy(IList<FeatureSelectionState> source)
        {
            List<FeatureSelectionState> copy = new List<FeatureSelectionState>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                copy.Add(source[i]);
            }

            return copy;
        }

        private static string DescribeOrder(List<FeatureSelectionState> states)
        {
            const int maxItems = 30;
            List<string> parts = new List<string>();
            int count = Math.Min(states.Count, maxItems);
            for (int i = 0; i < count; i++)
            {
                parts.Add(i + ":" + DescribeState(states[i]));
            }

            if (states.Count > maxItems)
            {
                parts.Add("... +" + (states.Count - maxItems) + " more");
            }

            return "[" + string.Join("; ", parts.ToArray()) + "]";
        }

        private static string DescribeState(FeatureSelectionState state)
        {
            if (state == null)
            {
                return "<null>";
            }

            return DescribeSelectionKey(state)
                + ", parent=" + DescribeSelectionKey(state.Parent)
                + ", next=" + DescribeSelectionKey(state.Next);
        }

        private static string DescribeSelectionKey(FeatureSelectionState state)
        {
            if (state == null)
            {
                return "<none>";
            }

            object selection = state.Selection;
            string selectionName = selection != null ? selection.GetType().Name : "<null selection>";

            UnityEngine.Object selectionObject = selection as UnityEngine.Object;
            if (selectionObject != null)
            {
                selectionName = selectionObject.name;
            }

            return selectionName + "#" + state.Index;
        }
    }
}
