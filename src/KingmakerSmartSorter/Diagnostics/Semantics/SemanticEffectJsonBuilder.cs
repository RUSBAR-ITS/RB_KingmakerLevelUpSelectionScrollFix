using System;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed class SemanticEffectJsonBuilder
    {
        private readonly SemanticValueNormalizer m_Normalizer;

        internal SemanticEffectJsonBuilder(SemanticValueNormalizer normalizer)
        {
            m_Normalizer = normalizer;
        }

        internal JObject Build(
            JObject mechanical,
            JObject sourceBlueprint,
            string relationship,
            SemanticComponentClassification classification)
        {
            JObject parameters = m_Normalizer.NormalizeFields(mechanical);
            return new JObject
            {
                ["Category"] = classification.Category,
                ["CategoryDisplay"] = SemanticLocalization.Category(classification.Category),
                ["ComponentType"] = (string)mechanical["ShortType"]
                    ?? GetShortType((string)mechanical["Type"]),
                ["Summary"] = BuildSummary(classification.Category, parameters),
                ["Parameters"] = parameters,
                ["Source"] = BuildSource(sourceBlueprint, relationship, mechanical)
            };
        }

        internal static JObject BuildDirectAbility(
            JObject blueprint,
            string relationship)
        {
            string name = (string)blueprint["ResolvedName"];
            if (string.IsNullOrEmpty(name))
            {
                name = (string)blueprint["InternalName"] ?? string.Empty;
            }

            return new JObject
            {
                ["Category"] = "GrantedAbility",
                ["CategoryDisplay"] = SemanticLocalization.Category("GrantedAbility"),
                ["ComponentType"] = (string)blueprint["ShortType"] ?? string.Empty,
                ["Summary"] = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    SemanticLocalization.Template("Target", "{0}: {1}"),
                    SemanticLocalization.Category("GrantedAbility"),
                    name),
                ["Parameters"] = new JObject
                {
                    ["Ability"] = new JObject
                    {
                        ["Guid"] = (string)blueprint["Guid"] ?? string.Empty,
                        ["InternalName"] = (string)blueprint["InternalName"] ?? string.Empty,
                        ["Name"] = name,
                        ["Description"] = (string)blueprint["ResolvedDescription"] ?? string.Empty
                    }
                },
                ["Source"] = new JObject
                {
                    ["BlueprintGuid"] = (string)blueprint["Guid"] ?? string.Empty,
                    ["BlueprintType"] = (string)blueprint["ShortType"] ?? string.Empty,
                    ["InternalName"] = (string)blueprint["InternalName"] ?? string.Empty,
                    ["Name"] = name,
                    ["Relationship"] = relationship ?? string.Empty,
                    ["Path"] = (string)blueprint["FirstSourcePath"] ?? string.Empty
                }
            };
        }

        private static JObject BuildSource(
            JObject sourceBlueprint,
            string relationship,
            JObject mechanical)
        {
            return new JObject
            {
                ["BlueprintGuid"] = (string)sourceBlueprint["Guid"] ?? string.Empty,
                ["BlueprintType"] = (string)sourceBlueprint["ShortType"] ?? string.Empty,
                ["InternalName"] = (string)sourceBlueprint["InternalName"] ?? string.Empty,
                ["Name"] = (string)sourceBlueprint["ResolvedName"] ?? string.Empty,
                ["Relationship"] = relationship ?? string.Empty,
                ["Path"] = (string)mechanical["SourcePath"] ?? string.Empty
            };
        }

        private static string BuildSummary(string category, JObject parameters)
        {
            string stat = SemanticValueNormalizer.ReadDisplay(parameters["Stat"]);
            string descriptor = SemanticValueNormalizer.ReadDisplay(parameters["Descriptor"]);
            string value = SemanticValueNormalizer.ReadDisplay(parameters["Value"]);

            if (!string.IsNullOrEmpty(stat) && !string.IsNullOrEmpty(value))
            {
                return string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    SemanticLocalization.Template("Bonus", "{2}: {1} ({0})"),
                    string.IsNullOrEmpty(descriptor)
                        ? SemanticLocalization.Category(category)
                        : descriptor,
                    FormatSigned(value),
                    stat);
            }

            string target = FirstDisplay(
                parameters,
                "Feature",
                "Fact",
                "Ability",
                "Spell",
                "Buff",
                "SpellDescriptor",
                "EnergyType",
                "DamageType");
            if (string.IsNullOrEmpty(target))
            {
                return SemanticLocalization.Category(category);
            }

            if (string.IsNullOrEmpty(value))
            {
                return string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    SemanticLocalization.Template("Target", "{0}: {1}"),
                    SemanticLocalization.Category(category),
                    target);
            }

            return string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                SemanticLocalization.Template("TargetValue", "{0}: {2} ({1})"),
                SemanticLocalization.Category(category),
                target,
                FormatSigned(value));
        }

        private static string FirstDisplay(JObject parameters, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                string value = SemanticValueNormalizer.ReadDisplay(parameters[names[i]]);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string FormatSigned(string value)
        {
            int numeric;
            return int.TryParse(value, out numeric) && numeric > 0
                ? "+" + value
                : value;
        }

        private static string GetShortType(string fullType)
        {
            if (string.IsNullOrEmpty(fullType))
            {
                return "Unknown";
            }

            int separator = fullType.LastIndexOf('.');
            return separator < 0 ? fullType : fullType.Substring(separator + 1);
        }
    }
}
