using System;

namespace KingmakerSmartSorter
{
    internal static class SemanticTraversalPolicy
    {
        internal static bool ShouldFollowComponentLink(
            string componentType,
            string fieldName)
        {
            string type = (componentType ?? string.Empty).ToLowerInvariant();
            string field = (fieldName ?? string.Empty).ToLowerInvariant();

            if (type.Contains("ifhasfact")
                || type.Contains("condition")
                || type.Contains("prerequisite")
                || type == "addfactcontextactions")
            {
                return false;
            }

            if (type.Contains("addunitfeature")
                || type.Contains("addunitfact")
                || type.StartsWith("addfeature", StringComparison.Ordinal)
                || type == "addfacts"
                || type.Contains("grantfeature"))
            {
                return field == "feature" || field == "fact" || field == "facts";
            }

            if (type.Contains("addability") || type.Contains("grantability"))
            {
                return field.Contains("ability");
            }

            if (type.Contains("applybuff") || type.Contains("addbuff"))
            {
                return field == "buff";
            }

            if (type.Contains("castspell"))
            {
                return field == "spell" || field == "ability";
            }

            if (type.Contains("stickytouch"))
            {
                return field.Contains("ability");
            }

            if (type == "abilityvariants")
            {
                return field == "variants" || field.Contains("ability");
            }

            return type.Contains("addenchantment") && field.Contains("enchantment");
        }

        internal static bool ShouldFollowBlueprintField(
            string blueprintType,
            string fieldName)
        {
            string type = (blueprintType ?? string.Empty).ToLowerInvariant();
            string field = (fieldName ?? string.Empty).ToLowerInvariant();

            if (type.Contains("blueprintitemequipment"))
            {
                return field.Contains("ability");
            }

            return type.Contains("blueprintactivatableability") && field == "buff";
        }

        internal static bool IsDirectItemAbility(
            string relationship,
            string blueprintType)
        {
            string relation = relationship ?? string.Empty;
            string type = blueprintType ?? string.Empty;
            return relation.StartsWith("BlueprintItemEquipment", StringComparison.Ordinal)
                && type.IndexOf("Ability", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
