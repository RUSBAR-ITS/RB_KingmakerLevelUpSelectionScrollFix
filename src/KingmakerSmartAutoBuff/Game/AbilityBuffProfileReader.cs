using System;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;

namespace KingmakerSmartAutoBuff
{
    internal static class AbilityBuffProfileReader
    {
        internal static AbilityBuffProfile Read(AbilityData ability)
        {
            AbilityBuffProfile profile = new AbilityBuffProfile();
            BlueprintAbility blueprint = ability != null ? ability.Blueprint : null;
            if (blueprint == null)
            {
                profile.DeliveryKind = BuffDeliveryKind.Unsupported;
                return profile;
            }

            bool hasTargetsAround = false;
            bool hasAoERadius = false;
            bool targetsAllies = false;
            bool targetsEnemies = false;
            float radiusMeters = SafeMeters(blueprint.AoERadius);

            BlueprintComponent[] components = SafeComponents(blueprint);
            foreach (BlueprintComponent component in components)
            {
                AbilityTargetsAround targetsAround = component as AbilityTargetsAround;
                if (targetsAround != null)
                {
                    hasTargetsAround = true;
                    radiusMeters = Math.Max(radiusMeters, SafeMeters(targetsAround.AoERadius));
                    UpdateTargetFlags(targetsAround.TargetType, ref targetsAllies, ref targetsEnemies);
                    profile.Diagnostics.Add("AbilityTargetsAround radius=" + radiusMeters.ToString("0.##") + " targetType=" + targetsAround.TargetType);
                    continue;
                }

                AbilityAoERadius aoeRadius = component as AbilityAoERadius;
                if (aoeRadius != null)
                {
                    hasAoERadius = true;
                    radiusMeters = Math.Max(radiusMeters, SafeMeters(aoeRadius.AoERadius));
                    UpdateTargetFlags(aoeRadius.Targets, ref targetsAllies, ref targetsEnemies);
                    profile.Diagnostics.Add("AbilityAoERadius radius=" + radiusMeters.ToString("0.##") + " targets=" + aoeRadius.Targets);
                    continue;
                }

                AbilityEffectRunAction runAction = component as AbilityEffectRunAction;
                if (runAction != null)
                {
                    AbilityActionScanResult scan = AbilityActionScanner.Scan(runAction.Actions);
                    MergeScan(profile, scan);
                }
            }

            if (!targetsAllies && !targetsEnemies)
            {
                UpdateTargetFlags(SafeAoETargets(blueprint), ref targetsAllies, ref targetsEnemies);
            }

            bool harmfulOnAlly = blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful;
            bool friendlyByTargeting = targetsAllies
                || blueprint.CanTargetFriends
                || blueprint.CanTargetSelf
                || blueprint.Range == AbilityRange.Personal
                || profile.DeliveryKind == BuffDeliveryKind.WholeParty;

            profile.RadiusMeters = radiusMeters;
            profile.IsFriendlyBuff = profile.HasAppliedBuffs && friendlyByTargeting && !harmfulOnAlly;
            profile.IsAreaBuff = profile.DeliveryKind == BuffDeliveryKind.WholeParty
                || hasTargetsAround
                || hasAoERadius
                || radiusMeters > 0.01f;

            profile.DeliveryKind = DetermineDeliveryKind(blueprint, profile, hasTargetsAround, hasAoERadius);
            profile.CanUseCasterAsAnchor = profile.IsFriendlyBuff
                && (profile.DeliveryKind == BuffDeliveryKind.CasterCenteredArea
                    || profile.DeliveryKind == BuffDeliveryKind.PointCenteredArea
                    || profile.DeliveryKind == BuffDeliveryKind.SelectedUnitCenteredArea
                    || profile.DeliveryKind == BuffDeliveryKind.WholeParty);

            if (profile.DeliveryKind == BuffDeliveryKind.Unsupported)
            {
                profile.IsAreaBuff = false;
            }

            return profile;
        }

        internal static string LocalizeDeliveryKind(BuffDeliveryKind kind)
        {
            switch (kind)
            {
                case BuffDeliveryKind.DirectUnit:
                    return ModLocalization.T("Delivery.DirectUnit");
                case BuffDeliveryKind.Personal:
                    return ModLocalization.T("Delivery.Personal");
                case BuffDeliveryKind.CasterCenteredArea:
                    return ModLocalization.T("Delivery.CasterCenteredArea");
                case BuffDeliveryKind.PointCenteredArea:
                    return ModLocalization.T("Delivery.PointCenteredArea");
                case BuffDeliveryKind.SelectedUnitCenteredArea:
                    return ModLocalization.T("Delivery.SelectedUnitCenteredArea");
                case BuffDeliveryKind.WholeParty:
                    return ModLocalization.T("Delivery.WholeParty");
                case BuffDeliveryKind.Unsupported:
                    return ModLocalization.T("Target.Unsupported");
                default:
                    return ModLocalization.T("Target.Unknown");
            }
        }

        private static BuffDeliveryKind DetermineDeliveryKind(
            BlueprintAbility blueprint,
            AbilityBuffProfile profile,
            bool hasTargetsAround,
            bool hasAoERadius)
        {
            if (!profile.HasAppliedBuffs)
            {
                return BuffDeliveryKind.Unsupported;
            }

            if (profile.DeliveryKind == BuffDeliveryKind.WholeParty)
            {
                return BuffDeliveryKind.WholeParty;
            }

            bool hasArea = hasTargetsAround || hasAoERadius || profile.RadiusMeters > 0.01f;
            if (hasArea)
            {
                if (blueprint.CanTargetPoint)
                {
                    return BuffDeliveryKind.PointCenteredArea;
                }

                if (blueprint.CanTargetFriends && !blueprint.CanTargetSelf)
                {
                    return BuffDeliveryKind.SelectedUnitCenteredArea;
                }

                return BuffDeliveryKind.CasterCenteredArea;
            }

            if (blueprint.Range == AbilityRange.Personal || (blueprint.CanTargetSelf && !blueprint.CanTargetFriends && !blueprint.CanTargetPoint))
            {
                return BuffDeliveryKind.Personal;
            }

            if (blueprint.CanTargetFriends || blueprint.CanTargetSelf)
            {
                return BuffDeliveryKind.DirectUnit;
            }

            return BuffDeliveryKind.Unsupported;
        }

        private static void MergeScan(AbilityBuffProfile profile, AbilityActionScanResult scan)
        {
            if (scan == null)
            {
                return;
            }

            if (scan.HasPartyMembersAction)
            {
                profile.DeliveryKind = BuffDeliveryKind.WholeParty;
            }

            foreach (string id in scan.AppliedBuffBlueprintIds)
            {
                if (!profile.AppliedBuffBlueprintIds.Contains(id))
                {
                    profile.AppliedBuffBlueprintIds.Add(id);
                }
            }

            foreach (string name in scan.AppliedBuffNames)
            {
                if (!profile.AppliedBuffNames.Contains(name))
                {
                    profile.AppliedBuffNames.Add(name);
                }
            }

            foreach (string diagnostic in scan.Diagnostics)
            {
                profile.Diagnostics.Add(diagnostic);
            }
        }

        private static BlueprintComponent[] SafeComponents(BlueprintAbility blueprint)
        {
            try
            {
                return blueprint.ComponentsArray ?? new BlueprintComponent[0];
            }
            catch
            {
                return new BlueprintComponent[0];
            }
        }

        private static TargetType SafeAoETargets(BlueprintAbility blueprint)
        {
            try
            {
                return blueprint.AoETargets;
            }
            catch
            {
                return TargetType.Any;
            }
        }

        private static void UpdateTargetFlags(TargetType targetType, ref bool targetsAllies, ref bool targetsEnemies)
        {
            switch (targetType)
            {
                case TargetType.Ally:
                    targetsAllies = true;
                    break;
                case TargetType.Enemy:
                    targetsEnemies = true;
                    break;
                case TargetType.Any:
                    targetsAllies = true;
                    targetsEnemies = true;
                    break;
            }
        }

        private static float SafeMeters(Kingmaker.Utility.Feet feet)
        {
            try
            {
                return feet.Meters;
            }
            catch
            {
                return 0f;
            }
        }
    }
}
