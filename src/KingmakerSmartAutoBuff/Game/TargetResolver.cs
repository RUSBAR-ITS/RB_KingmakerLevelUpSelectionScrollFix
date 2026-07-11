using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
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

            AbilityBuffProfile profile = entry.BuffProfile;
            if (profile != null && profile.IsFriendlyBuff && (profile.IsAreaBuff || profile.DeliveryKind == BuffDeliveryKind.WholeParty))
            {
                return BuildPartyTargetOptions();
            }

            if (profile != null && profile.DeliveryKind == BuffDeliveryKind.Personal)
            {
                if (entry.Caster != null)
                {
                    result.Add(CreateTargetOption(entry.Caster));
                }

                return result;
            }

            if (profile != null && profile.DeliveryKind == BuffDeliveryKind.Unsupported)
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

        private static List<TargetOption> BuildPartyTargetOptions()
        {
            return PartyProvider.GetActiveParty()
                .Where(unit => unit != null)
                .Select(CreateTargetOption)
                .GroupBy(target => target.Id)
                .Select(group => group.First())
                .OrderBy(target => target.Name)
                .ToList();
        }

        internal static TargetKind DetermineTargetKind(AbilityData ability)
        {
            return ReadTargetProfile(ability).TargetKind;
        }

        internal static AbilityTargetProfile ReadTargetProfile(AbilityData ability)
        {
            BlueprintAbility blueprint = ability != null ? ability.Blueprint : null;
            AbilityTargetProfile profile = new AbilityTargetProfile();
            if (blueprint == null)
            {
                return profile;
            }

            profile.Range = blueprint.Range;
            profile.CanTargetSelf = blueprint.CanTargetSelf;
            profile.CanTargetFriends = blueprint.CanTargetFriends;
            profile.CanTargetEnemies = blueprint.CanTargetEnemies;
            profile.CanTargetPoint = blueprint.CanTargetPoint;
            profile.RadiusMeters = ReadRadiusMeters(blueprint);

            bool self = profile.CanTargetSelf;
            bool friends = profile.CanTargetFriends;
            bool enemies = profile.CanTargetEnemies;
            bool point = profile.CanTargetPoint;
            bool harmfulOnAlly = blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful;

            if (profile.Range == AbilityRange.Personal)
            {
                profile.TargetKind = TargetKind.Self;
                profile.IsFriendly = true;
                return profile;
            }

            if (!self && !friends && !enemies && !point)
            {
                profile.TargetKind = TargetKind.NoTarget;
                return profile;
            }

            if (self && !friends && !enemies && !point)
            {
                profile.TargetKind = TargetKind.Self;
                profile.IsFriendly = true;
                return profile;
            }

            if (point)
            {
                profile.TargetKind = DeterminePointTargetKind(blueprint, profile.RadiusMeters, self, friends, enemies);
                profile.IsPointTarget = profile.TargetKind == TargetKind.Point
                    || profile.TargetKind == TargetKind.AreaAlly
                    || profile.TargetKind == TargetKind.AreaEnemy
                    || profile.TargetKind == TargetKind.AreaAny;
                profile.IsAreaTarget = profile.TargetKind == TargetKind.AreaAlly
                    || profile.TargetKind == TargetKind.AreaEnemy
                    || profile.TargetKind == TargetKind.AreaAny;
                profile.IsHostile = profile.TargetKind == TargetKind.AreaEnemy
                    || (profile.TargetKind == TargetKind.AreaAny && !friends && enemies);
                profile.IsFriendly = profile.TargetKind == TargetKind.AreaAlly
                    || (profile.TargetKind == TargetKind.AreaAny && friends);
                return profile;
            }

            if ((friends || self) && enemies)
            {
                profile.TargetKind = TargetKind.SelectedAny;
                profile.IsFriendly = true;
                profile.IsHostile = true;
                return profile;
            }

            if (enemies)
            {
                profile.TargetKind = TargetKind.SelectedEnemy;
                profile.IsHostile = true;
                return profile;
            }

            if ((friends || self) && !harmfulOnAlly)
            {
                profile.TargetKind = self && friends ? TargetKind.SelectedAllyOrSelf : TargetKind.SelectedAlly;
                profile.IsFriendly = true;
                return profile;
            }

            profile.TargetKind = TargetKind.Unsupported;
            return profile;
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

        private static TargetKind DeterminePointTargetKind(
            BlueprintAbility blueprint,
            float radiusMeters,
            bool self,
            bool friends,
            bool enemies)
        {
            if (!self && !friends && !enemies && radiusMeters <= 0.01f)
            {
                return TargetKind.Point;
            }

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

        private static float ReadRadiusMeters(BlueprintAbility blueprint)
        {
            float radius = 0f;
            try
            {
                radius = blueprint.AoERadius.Meters;
            }
            catch
            {
                radius = 0f;
            }

            BlueprintComponent[] components;
            try
            {
                components = blueprint.ComponentsArray ?? new BlueprintComponent[0];
            }
            catch
            {
                return radius;
            }

            foreach (BlueprintComponent component in components)
            {
                AbilityTargetsAround targetsAround = component as AbilityTargetsAround;
                if (targetsAround != null)
                {
                    try
                    {
                        radius = System.Math.Max(radius, targetsAround.AoERadius.Meters);
                    }
                    catch
                    {
                    }

                    continue;
                }

                AbilityAoERadius aoeRadius = component as AbilityAoERadius;
                if (aoeRadius != null)
                {
                    try
                    {
                        radius = System.Math.Max(radius, aoeRadius.AoERadius.Meters);
                    }
                    catch
                    {
                    }
                }
            }

            return radius;
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
