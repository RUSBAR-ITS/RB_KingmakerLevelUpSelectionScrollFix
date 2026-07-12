using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Kingmaker.UI.Common;
using Kingmaker.UI.Vendor;
using TMPro;

namespace KingmakerSmartSorter
{
    internal static class SmartSortController
    {
        internal const int SmartSorterValue = 1000;

        private static readonly ConditionalWeakTable<FilterController, SmartSortControllerState> s_States =
            new ConditionalWeakTable<FilterController, SmartSortControllerState>();

        private static readonly List<WeakReference> s_TrackedControllers = new List<WeakReference>();

        internal static bool IsSmartSorter(ItemsFilter.SorterType sorterType)
        {
            return (int)sorterType == SmartSorterValue;
        }

        internal static void AfterInitialize(FilterController controller)
        {
            if (!IsSupportedController(controller))
            {
                return;
            }

            try
            {
                SmartSortControllerState state = GetState(controller);
                EnsureOption(controller, state);
                RestoreSmartSelection(controller, state);
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to initialize the smart sorting menu option.", ex);
            }
        }

        internal static void BeforeLoadStates(FilterController controller)
        {
            if (!IsSupportedController(controller)
                || Main.Settings == null
                || !Main.Settings.SmartSortingSelected)
            {
                return;
            }

            GetState(controller).IsRestoring = true;
        }

        internal static void AfterLoadStates(FilterController controller)
        {
            if (!IsSupportedController(controller))
            {
                return;
            }

            SmartSortControllerState state = GetState(controller);
            try
            {
                EnsureOption(controller, state);
                RestoreSmartSelection(controller, state);
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to restore the smart sorting selection.", ex);
            }
            finally
            {
                state.IsRestoring = false;
            }
        }

        internal static bool HandleSorterSelection(FilterController controller, int selectedIndex)
        {
            if (!IsSupportedController(controller))
            {
                return true;
            }

            SmartSortControllerState state = GetState(controller);
            EnsureOption(controller, state);

            if (selectedIndex == state.OptionIndex)
            {
                state.CustomSelectionHandled = true;
                Activate(controller, state, !state.IsRestoring);
                return false;
            }

            if (state.IsRestoring)
            {
                return true;
            }

            if (state.IsActive || (Main.Settings != null && Main.Settings.SmartSortingSelected))
            {
                state.IsActive = false;
                Main.SetSmartSortingSelected(false);
                Logger.Info("Smart sorting disabled by selecting vanilla sorter index " + selectedIndex + ".");
            }

            return true;
        }

        internal static void RefreshOptionLabels()
        {
            string label = ModLocalization.T("SortMenu.Smart");
            VisitTrackedControllers(
                delegate(FilterController controller, SmartSortControllerState state)
                {
                    if (state.OptionData == null)
                    {
                        return;
                    }

                    state.OptionData.text = label;
                    if (controller.DropdownMenu != null)
                    {
                        controller.DropdownMenu.RefreshShownValue();
                    }
                });
        }

        internal static void RemoveInjectedOptions()
        {
            VisitTrackedControllers(
                delegate(FilterController controller, SmartSortControllerState state)
                {
                    try
                    {
                        state.IsRestoring = true;
                        bool needsVanillaSort = IsSmartSorter(controller.CurrentSorter);

                        TMP_Dropdown dropdown = controller.DropdownMenu;
                        if (dropdown != null)
                        {
                            if (dropdown.value == state.OptionIndex)
                            {
                                dropdown.value = (int)ItemsFilter.SorterType.TypeUp;
                            }

                            if (state.OptionData != null && dropdown.options.Contains(state.OptionData))
                            {
                                dropdown.options.Remove(state.OptionData);
                            }

                            dropdown.RefreshShownValue();
                        }

                        if (needsVanillaSort && IsSmartSorter(controller.CurrentSorter))
                        {
                            controller.CurrentSorter = ItemsFilter.SorterType.TypeUp;
                            controller.ApplySortAndFilters();
                        }

                        state.IsActive = false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception("Failed to remove a smart sorting menu option.", ex);
                    }
                    finally
                    {
                        state.IsRestoring = false;
                    }
                });

            s_TrackedControllers.Clear();
        }

        private static bool IsSupportedController(FilterController controller)
        {
            return controller != null && controller.GetType() == typeof(FilterController);
        }

        private static SmartSortControllerState GetState(FilterController controller)
        {
            SmartSortControllerState state;
            if (s_States.TryGetValue(controller, out state))
            {
                return state;
            }

            state = new SmartSortControllerState();
            s_States.Add(controller, state);
            s_TrackedControllers.Add(new WeakReference(controller));
            return state;
        }

        private static void EnsureOption(FilterController controller, SmartSortControllerState state)
        {
            TMP_Dropdown dropdown = controller.DropdownMenu;
            if (dropdown == null || dropdown.options == null)
            {
                state.OptionIndex = -1;
                return;
            }

            string label = ModLocalization.T("SortMenu.Smart");
            if (state.OptionData == null || !dropdown.options.Contains(state.OptionData))
            {
                state.OptionData = new TMP_Dropdown.OptionData(label);
                dropdown.options.Add(state.OptionData);
                Logger.Info(
                    "Added smart sorting option to "
                    + controller.GetType().FullName
                    + ". vanillaOptionCount="
                    + (dropdown.options.Count - 1)
                    + ".");
            }
            else
            {
                state.OptionData.text = label;
            }

            state.OptionIndex = dropdown.options.IndexOf(state.OptionData);
            dropdown.RefreshShownValue();
        }

        private static void RestoreSmartSelection(
            FilterController controller,
            SmartSortControllerState state)
        {
            if (Main.Settings == null || !Main.Settings.SmartSortingSelected || state.OptionIndex < 0)
            {
                state.IsActive = false;
                return;
            }

            bool wasRestoring = state.IsRestoring;
            state.IsRestoring = true;

            try
            {
                state.CustomSelectionHandled = false;
                if (controller.DropdownMenu != null && controller.DropdownMenu.value != state.OptionIndex)
                {
                    controller.DropdownMenu.value = state.OptionIndex;
                }

                if (!state.CustomSelectionHandled)
                {
                    Activate(controller, state, false);
                }

                if (controller.DropdownMenu != null)
                {
                    controller.DropdownMenu.RefreshShownValue();
                }
            }
            finally
            {
                state.IsRestoring = wasRestoring;
            }
        }

        private static void Activate(
            FilterController controller,
            SmartSortControllerState state,
            bool persistSelection)
        {
            bool wasActive = state.IsActive && IsSmartSorter(controller.CurrentSorter);

            state.IsActive = true;
            controller.CurrentSorter = (ItemsFilter.SorterType)SmartSorterValue;

            if (persistSelection)
            {
                Main.SetSmartSortingSelected(true);
            }

            controller.ApplySortAndFilters();

            if (!wasActive)
            {
                Logger.Info("Smart sorting activated.");
            }
        }

        private static void VisitTrackedControllers(Action<FilterController, SmartSortControllerState> action)
        {
            for (int i = s_TrackedControllers.Count - 1; i >= 0; i--)
            {
                FilterController controller = s_TrackedControllers[i].Target as FilterController;
                if (controller == null)
                {
                    s_TrackedControllers.RemoveAt(i);
                    continue;
                }

                SmartSortControllerState state;
                if (!s_States.TryGetValue(controller, out state))
                {
                    s_TrackedControllers.RemoveAt(i);
                    continue;
                }

                action(controller, state);
            }
        }
    }
}
