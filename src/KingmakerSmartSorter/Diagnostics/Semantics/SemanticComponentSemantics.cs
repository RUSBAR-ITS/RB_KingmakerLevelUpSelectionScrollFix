using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal static class SemanticComponentSemantics
    {
        private static readonly HashSet<string> s_PresenceBasedTypes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "AddImmunityToAbilityScoreDamage",
                "AddImmunityToCriticalHits",
                "AddImmunityToEnergyDrain",
                "AddImmunityToPrecisionDamage",
                "ChirurgeonSpell",
                "ContextActionKillTarget",
                "EquipmentRestrictionMainPlayer",
                "FullSpeedInStealth",
                "IgnoreConcealment",
                "RemoveBuffOnAttack",
                "UniqueBuff"
            };

        internal static bool IsPresenceBased(string componentType)
        {
            return s_PresenceBasedTypes.Contains(componentType ?? string.Empty);
        }

        internal static bool IsInactive(string componentType, JObject parameters)
        {
            return componentType == "AddUnitFeatureEquipment"
                && (parameters == null || parameters.Count == 0);
        }
    }
}
