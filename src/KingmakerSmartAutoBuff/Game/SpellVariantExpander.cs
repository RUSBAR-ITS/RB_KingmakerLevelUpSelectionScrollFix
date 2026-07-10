using System;
using System.Collections.Generic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace KingmakerSmartAutoBuff
{
    internal static class SpellVariantExpander
    {
        internal static List<AbilityData> GetCastableVariants(AbilityData ability)
        {
            List<AbilityData> variants = new List<AbilityData>();
            if (ability == null || ability.Blueprint == null)
            {
                return variants;
            }

            if (!SafeHasVariants(ability.Blueprint))
            {
                variants.Add(ability);
                return variants;
            }

            AddAbilityDataVariants(variants, ability);
            if (variants.Count == 0)
            {
                AddBlueprintVariants(variants, ability);
            }

            return variants.Count > 0 ? variants : new List<AbilityData> { ability };
        }

        private static void AddAbilityDataVariants(List<AbilityData> variants, AbilityData ability)
        {
            try
            {
                IList<AbilityData> abilityVariants = ability.Variants;
                if (abilityVariants == null)
                {
                    return;
                }

                foreach (AbilityData variant in abilityVariants)
                {
                    if (variant != null && variant.Blueprint != null)
                    {
                        variants.Add(variant);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to read ability data variants for " + SafeAbilityName(ability) + ".", ex);
            }
        }

        private static void AddBlueprintVariants(List<AbilityData> variants, AbilityData ability)
        {
            try
            {
                BlueprintAbility[] blueprints = ability.Blueprint.Variants;
                if (blueprints == null)
                {
                    return;
                }

                foreach (BlueprintAbility blueprint in blueprints)
                {
                    if (blueprint == null)
                    {
                        continue;
                    }

                    AbilityData variant = new AbilityData(ability, blueprint);
                    if (ability.MetamagicData != null)
                    {
                        variant.MetamagicData = ability.MetamagicData;
                    }

                    variants.Add(variant);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to create blueprint variants for " + SafeAbilityName(ability) + ".", ex);
            }
        }

        private static bool SafeHasVariants(BlueprintAbility blueprint)
        {
            try
            {
                return blueprint != null && blueprint.HasVariants;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeAbilityName(AbilityData ability)
        {
            try
            {
                return ability != null && !string.IsNullOrEmpty(ability.Name) ? ability.Name : "<spell>";
            }
            catch
            {
                return "<spell>";
            }
        }
    }
}
