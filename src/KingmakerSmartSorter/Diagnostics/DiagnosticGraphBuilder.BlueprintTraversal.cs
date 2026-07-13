using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Properties;

namespace KingmakerSmartSorter
{
    internal sealed partial class DiagnosticGraphBuilder
    {
        private static bool ShouldExpandBlueprint(
            BlueprintScriptableObject blueprint,
            string path,
            out string shallowReason)
        {
            shallowReason = string.Empty;
            string sourcePath = path ?? string.Empty;
            if (sourcePath.IndexOf(
                    "Kingmaker.Blueprints.Classes.Spells.SpellListComponent.",
                    StringComparison.Ordinal) >= 0)
            {
                shallowReason = "SpellListMembershipMetadata";
                return false;
            }

            if (sourcePath.IndexOf(
                    "Kingmaker.Designers.Mechanics.Recommendations.",
                    StringComparison.Ordinal) >= 0)
            {
                shallowReason = "RecommendationMetadata";
                return false;
            }

            if (sourcePath.IndexOf(
                    "Kingmaker.Blueprints.Classes.Prerequisites.",
                    StringComparison.Ordinal) >= 0)
            {
                shallowReason = "PrerequisiteMetadata";
                return false;
            }

            if (sourcePath.IndexOf(".TargetCheckers.", StringComparison.Ordinal) >= 0
                || sourcePath.IndexOf(
                    ".Mechanics.Conditions.",
                    StringComparison.Ordinal) >= 0)
            {
                shallowReason = "ConditionReferenceIdentity";
                return false;
            }

            if (blueprint is BlueprintItem
                || blueprint is BlueprintItemEnchantment
                || blueprint is BlueprintAbility
                || blueprint is BlueprintBuff
                || blueprint is BlueprintActivatableAbility
                || blueprint is BlueprintAbilityAreaEffect
                || blueprint is BlueprintAbilityResource
                || blueprint is BlueprintUnitProperty)
            {
                return true;
            }

            BlueprintUnitFact unitFact = blueprint as BlueprintUnitFact;
            if (!ReferenceEquals(unitFact, null))
            {
                string fullName = blueprint.GetType().FullName ?? string.Empty;
                if (fullName.IndexOf(".Selection.", StringComparison.Ordinal) >= 0
                    || fullName.EndsWith(
                        ".BlueprintProgression",
                        StringComparison.Ordinal)
                    || fullName.EndsWith(
                        ".BlueprintFeatureReplaceSpellbook",
                        StringComparison.Ordinal))
                {
                    shallowReason = "GlobalProgressionOrSelection";
                    return false;
                }

                return true;
            }

            shallowReason = "ReferenceOnlyBlueprintType";
            return false;
        }
    }
}
