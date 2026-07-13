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
            bool isPresenceBased =
                SemanticComponentSemantics.IsPresenceBased(componentType);
            bool isInactive =
                SemanticComponentSemantics.IsInactive(componentType, parameters);
            string definitionMode = isInactive
                ? "Inactive"
                : isPresenceBased ? "PresenceBased" : "Parameterized";
            return new JObject
            {
                ["Category"] = classification.Category,
                ["CategoryDisplay"] = SemanticLocalization.Category(classification.Category),
                ["ComponentType"] = componentType,
                ["ComponentDisplay"] = SemanticLocalization.Component(componentType),
                ["DefinitionMode"] = definitionMode,
                ["DefinitionModeDisplay"] = SemanticLocalization.DefinitionMode(definitionMode),
                ["IsPresenceBased"] = isPresenceBased,
                ["IsInactive"] = isInactive,
                ["Summary"] = SemanticEffectSummaryBuilder.Build(
                    componentType,
                    classification.Category,
                    parameters),
                ["Parameters"] = parameters,
                ["Source"] = BuildSource(sourceBlueprint, source, mechanical, false)
            };
        }

        internal JObject BuildDirectAbility(
            JObject blueprint,
            SemanticSourceContext source)
        {
            string nameSource;
            string name = m_Normalizer.ResolveBlueprintName(blueprint, out nameSource);

            return new JObject
            {
                ["Category"] = "GrantedAbility",
                ["CategoryDisplay"] = SemanticLocalization.Category("GrantedAbility"),
                ["ComponentType"] = (string)blueprint["ShortType"] ?? string.Empty,
                ["ComponentDisplay"] = SemanticLocalization.Category("GrantedAbility"),
                ["DefinitionMode"] = "Parameterized",
                ["DefinitionModeDisplay"] = SemanticLocalization.DefinitionMode("Parameterized"),
                ["IsPresenceBased"] = false,
                ["IsInactive"] = false,
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
                        ["NameSource"] = nameSource,
                        ["Description"] = (string)blueprint["ResolvedDescription"] ?? string.Empty
                    }
                },
                ["Source"] = new JObject
                {
                    ["BlueprintGuid"] = (string)blueprint["Guid"] ?? string.Empty,
                    ["BlueprintType"] = (string)blueprint["ShortType"] ?? string.Empty,
                    ["InternalName"] = (string)blueprint["InternalName"] ?? string.Empty,
                    ["Name"] = name,
                    ["NameSource"] = nameSource,
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

        private JObject BuildSource(
            JObject sourceBlueprint,
            SemanticSourceContext source,
            JObject mechanical,
            bool forceDirect)
        {
            string scope = source == null ? "Direct" : source.GetScope(forceDirect);
            string nameSource;
            string sourceName = m_Normalizer.ResolveBlueprintName(
                sourceBlueprint,
                out nameSource);
            return new JObject
            {
                ["BlueprintGuid"] = (string)sourceBlueprint["Guid"] ?? string.Empty,
                ["BlueprintType"] = (string)sourceBlueprint["ShortType"] ?? string.Empty,
                ["InternalName"] = (string)sourceBlueprint["InternalName"] ?? string.Empty,
                ["Name"] = sourceName,
                ["NameSource"] = nameSource,
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
