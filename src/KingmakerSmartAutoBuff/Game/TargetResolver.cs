using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.Utility;

namespace KingmakerSmartAutoBuff
{
    internal static class TargetResolver
    {
        internal static List<TargetOption> BuildTargetOptions(SpellCatalogEntry entry)
        {
            List<TargetOption> result = new List<TargetOption>();
            if (entry == null || entry.Ability == null)
            {
                return result;
            }

            if (entry.TargetKind == TargetKind.Self)
            {
                if (entry.Caster != null)
                {
                    result.Add(CreateTargetOption(entry.Caster));
                }

                return result;
            }

            if (entry.TargetKind == TargetKind.NoTarget || entry.TargetKind == TargetKind.Unsupported)
            {
                return result;
            }

            foreach (UnitEntityData unit in PartyProvider.GetActiveParty())
            {
                if (unit == null)
                {
                    continue;
                }

                if (CanUsePartyUnitAsTarget(entry, unit))
                {
                    result.Add(CreateTargetOption(unit));
                }
            }

            return result
                .GroupBy(target => target.Id)
                .Select(group => group.First())
                .OrderBy(target => target.Name)
                .ToList();
        }

        internal static TargetKind DetermineTargetKind(AbilityData ability)
        {
            BlueprintAbility blueprint = ability != null ? ability.Blueprint : null;
            if (blueprint == null)
            {
                return TargetKind.Unknown;
            }

            AbilityRange range = blueprint.Range;
            bool self = blueprint.CanTargetSelf;
            bool friends = blueprint.CanTargetFriends;
            bool enemies = blueprint.CanTargetEnemies;
            bool point = blueprint.CanTargetPoint;
            bool harmfulOnAlly = blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful;

            if (range == AbilityRange.Personal)
            {
                return TargetKind.Self;
            }

            if (!self && !friends && !enemies && !point)
            {
                return TargetKind.NoTarget;
            }

            if (self && !friends && !enemies && !point)
            {
                return TargetKind.Self;
            }

            if (point)
            {
                return DeterminePointTargetKind(blueprint);
            }

            if ((friends || self) && enemies)
            {
                return TargetKind.SelectedAny;
            }

            if (enemies)
            {
                return TargetKind.SelectedEnemy;
            }

            if ((friends || self) && !harmfulOnAlly)
            {
                return self && friends ? TargetKind.SelectedAllyOrSelf : TargetKind.SelectedAlly;
            }

            return TargetKind.Unsupported;
        }

        internal static string LocalizeTargetKind(TargetKind targetKind)
        {
            switch (targetKind)
            {
                case TargetKind.Self:
                    return ModLocalization.T("Target.Self");
                case TargetKind.SelectedAlly:
                    return ModLocalization.T("Target.Ally");
                case TargetKind.SelectedAllyOrSelf:
                    return ModLocalization.T("Target.AllyOrSelf");
                case TargetKind.SelectedEnemy:
                    return ModLocalization.T("Target.Enemy");
                case TargetKind.SelectedAny:
                    return ModLocalization.T("Target.Any");
                case TargetKind.AreaAlly:
                    return ModLocalization.T("Target.AreaAlly");
                case TargetKind.AreaEnemy:
                    return ModLocalization.T("Target.AreaEnemy");
                case TargetKind.AreaAny:
                    return ModLocalization.T("Target.AreaAny");
                case TargetKind.Point:
                    return ModLocalization.T("Target.Point");
                case TargetKind.NoTarget:
                    return ModLocalization.T("Target.NoTarget");
                case TargetKind.Unsupported:
                    return ModLocalization.T("Target.Unsupported");
                default:
                    return ModLocalization.T("Target.Unknown");
            }
        }

        private static TargetKind DeterminePointTargetKind(BlueprintAbility blueprint)
        {
            try
            {
                switch (blueprint.AoETargets)
                {
                    case TargetType.Ally:
                        return TargetKind.AreaAlly;
                    case TargetType.Enemy:
                        return TargetKind.AreaEnemy;
                    case TargetType.Any:
                        return TargetKind.AreaAny;
                    default:
                        return TargetKind.Point;
                }
            }
            catch
            {
                return TargetKind.Point;
            }
        }

        private static bool IsAreaTarget(TargetKind targetKind)
        {
            return targetKind == TargetKind.AreaAlly
                || targetKind == TargetKind.AreaEnemy
                || targetKind == TargetKind.AreaAny;
        }

        private static bool CanUsePartyUnitAsTarget(SpellCatalogEntry entry, UnitEntityData unit)
        {
            if (entry.TargetKind == TargetKind.SelectedEnemy || entry.TargetKind == TargetKind.AreaEnemy)
            {
                return false;
            }

            if (entry.TargetKind == TargetKind.AreaAlly || entry.TargetKind == TargetKind.AreaAny)
            {
                return true;
            }

            return entry.TargetKind != TargetKind.Point && CanTargetUnit(entry.Ability, unit);
        }

        private static TargetOption CreateTargetOption(UnitEntityData unit)
        {
            TargetOption option = new TargetOption();
            option.Unit = unit;
            option.Id = PartyProvider.GetUnitId(unit);
            option.Name = PartyProvider.SafeUnitName(unit);
            return option;
        }

        private static bool CanTargetUnit(AbilityData ability, UnitEntityData unit)
        {
            try
            {
                TargetWrapper target = unit;
                return ability.CanTarget(target);
            }
            catch
            {
                return false;
            }
        }
    }
}
