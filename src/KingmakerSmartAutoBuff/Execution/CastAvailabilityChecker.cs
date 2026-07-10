using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;

namespace KingmakerSmartAutoBuff
{
    internal static class CastAvailabilityChecker
    {
        internal static bool TryResolve(
            ResolvedCastTask task,
            out SpellCatalogEntry entry,
            out UnitEntityData targetUnit,
            out TargetWrapper target,
            out string reason)
        {
            entry = null;
            targetUnit = null;
            target = null;
            reason = string.Empty;

            if (task == null || task.Action == null)
            {
                reason = ModLocalization.T("Execution.Skip.EmptyAction");
                return false;
            }

            entry = SpellCatalog.FindCurrentEntry(task.Action);
            if (entry == null)
            {
                reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                return false;
            }

            if (!IsSupportedTargetKind(entry.TargetKind))
            {
                reason = ModLocalization.T("Execution.Skip.UnsupportedTarget");
                return false;
            }

            if (!IsAbilityCurrentlyAvailable(entry.Ability, out reason))
            {
                return false;
            }

            targetUnit = ResolveTargetUnit(task, entry);
            if (RequiresTargetUnit(entry.TargetKind) && targetUnit == null)
            {
                reason = ModLocalization.T("Execution.Skip.TargetUnavailable");
                return false;
            }

            if (targetUnit == null)
            {
                targetUnit = entry.Caster;
            }

            if (targetUnit == null)
            {
                reason = ModLocalization.T("Execution.Skip.TargetUnavailable");
                return false;
            }

            target = targetUnit;

            if (ShouldCheckCanTarget(entry.TargetKind) && !CanTarget(entry.Ability, target, out reason))
            {
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool IsSupportedTargetKind(TargetKind targetKind)
        {
            return targetKind == TargetKind.Self
                || targetKind == TargetKind.SelectedAlly
                || targetKind == TargetKind.SelectedAllyOrSelf
                || targetKind == TargetKind.SelectedAny
                || targetKind == TargetKind.AreaAlly
                || targetKind == TargetKind.AreaAny
                || targetKind == TargetKind.NoTarget;
        }

        private static bool RequiresTargetUnit(TargetKind targetKind)
        {
            return targetKind == TargetKind.SelectedAlly
                || targetKind == TargetKind.SelectedAllyOrSelf
                || targetKind == TargetKind.SelectedAny
                || targetKind == TargetKind.AreaAlly
                || targetKind == TargetKind.AreaAny;
        }

        private static bool ShouldCheckCanTarget(TargetKind targetKind)
        {
            return targetKind == TargetKind.Self
                || targetKind == TargetKind.SelectedAlly
                || targetKind == TargetKind.SelectedAllyOrSelf
                || targetKind == TargetKind.SelectedAny;
        }

        private static UnitEntityData ResolveTargetUnit(ResolvedCastTask task, SpellCatalogEntry entry)
        {
            if (entry.TargetKind == TargetKind.Self || entry.TargetKind == TargetKind.NoTarget)
            {
                return entry.Caster;
            }

            return SpellCatalog.FindPartyUnit(task.TargetId, task.TargetName);
        }

        private static bool IsAbilityCurrentlyAvailable(AbilityData ability, out string reason)
        {
            reason = string.Empty;
            if (ability == null)
            {
                reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                return false;
            }

            try
            {
                if (!ability.IsAvailable || !ability.IsAvailableForCast)
                {
                    reason = SafeUnavailableReason(ability);
                    if (string.IsNullOrEmpty(reason))
                    {
                        reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to check ability availability.", ex);
                reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                return false;
            }

            return true;
        }

        private static bool CanTarget(AbilityData ability, TargetWrapper target, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (ability.CanTarget(target))
                {
                    return true;
                }

                reason = ModLocalization.T("Execution.Skip.BadTarget");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to check ability target.", ex);
                reason = ModLocalization.T("Execution.Skip.BadTarget");
                return false;
            }
        }

        private static string SafeUnavailableReason(AbilityData ability)
        {
            try
            {
                return ability.GetUnavailableReason();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
