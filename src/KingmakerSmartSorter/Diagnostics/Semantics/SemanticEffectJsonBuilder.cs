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
            SemanticSourceContext source,
            SemanticComponentClassification classification)
        {
            JObject parameters = m_Normalizer.NormalizeFields(mechanical);
            string componentType = (string)mechanical["ShortType"]
                ?? GetShortType((string)mechanical["Type"]);
            return new JObject
            {
                ["Category"] = classification.Category,
                ["CategoryDisplay"] = SemanticLocalization.Category(classification.Category),
                ["ComponentType"] = componentType,
                ["IsPresenceBased"] = SemanticEffectSummaryBuilder.IsPresenceBased(componentType),
                ["Summary"] = SemanticEffectSummaryBuilder.Build(
                    componentType,
                    classification.Category,
                    parameters),
                ["Parameters"] = parameters,
                ["Source"] = BuildSource(sourceBlueprint, source, mechanical, false)
            };
        }

        internal static JObject BuildDirectAbility(
            JObject blueprint,
            SemanticSourceContext source)
        {
            string name = (string)blueprint["ResolvedName"];
            if (string.IsNullOrEmpty(name))
            {
                name = (string)blueprint["InternalName"] ?? string.Empty;
            }

            bool usesInternalName = string.IsNullOrEmpty((string)blueprint["ResolvedName"]);

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
                        ["NameSource"] = usesInternalName
                            ? "InternalNameFallback"
                            : "GameLocalization",
                        ["Description"] = (string)blueprint["ResolvedDescription"] ?? string.Empty
                    }
                },
                ["Source"] = new JObject
                {
                    ["BlueprintGuid"] = (string)blueprint["Guid"] ?? string.Empty,
                    ["BlueprintType"] = (string)blueprint["ShortType"] ?? string.Empty,
                    ["InternalName"] = (string)blueprint["InternalName"] ?? string.Empty,
                    ["Name"] = name,
                    ["Relationship"] = source == null
                        ? string.Empty
                        : source.Relationship,
                    ["Origin"] = source == null ? string.Empty : source.Origin,
                    ["Depth"] = source == null ? 0 : source.Depth,
                    ["Scope"] = "Direct",
                    ["ScopeDisplay"] = SemanticLocalization.Scope("Direct"),
                    ["Chain"] = source == null || source.Chain == null
                        ? new JArray()
                        : source.Chain.DeepClone(),
                    ["Path"] = (string)blueprint["FirstSourcePath"] ?? string.Empty
                }
            };
        }

        private static JObject BuildSource(
            JObject sourceBlueprint,
            SemanticSourceContext source,
            JObject mechanical,
            bool forceDirect)
        {
            string scope = source == null ? "Direct" : source.GetScope(forceDirect);
            return new JObject
            {
                ["BlueprintGuid"] = (string)sourceBlueprint["Guid"] ?? string.Empty,
                ["BlueprintType"] = (string)sourceBlueprint["ShortType"] ?? string.Empty,
                ["InternalName"] = (string)sourceBlueprint["InternalName"] ?? string.Empty,
                ["Name"] = (string)sourceBlueprint["ResolvedName"] ?? string.Empty,
                ["Relationship"] = source == null
                    ? string.Empty
                    : source.Relationship,
                ["Origin"] = source == null ? string.Empty : source.Origin,
                ["Depth"] = source == null ? 0 : source.Depth,
                ["Scope"] = scope,
                ["ScopeDisplay"] = SemanticLocalization.Scope(scope),
                ["Chain"] = source == null || source.Chain == null
                    ? new JArray()
                    : source.Chain.DeepClone(),
                ["Path"] = (string)mechanical["SourcePath"] ?? string.Empty
            };
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
