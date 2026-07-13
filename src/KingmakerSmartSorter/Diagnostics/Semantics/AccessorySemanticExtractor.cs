using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed class AccessorySemanticExtractor
    {
        private const int MaxBlueprintDepth = 10;

        private readonly SemanticGraphIndex m_Graph;
        private readonly SemanticEffectJsonBuilder m_EffectBuilder;
        private readonly SemanticReportCoverage m_Coverage;

        internal AccessorySemanticExtractor(
            SemanticGraphIndex graph,
            SemanticEnumResolver enums,
            SemanticReportCoverage coverage)
        {
            m_Graph = graph;
            m_EffectBuilder = new SemanticEffectJsonBuilder(
                new SemanticValueNormalizer(graph, enums));
            m_Coverage = coverage;
        }

        internal JObject Extract(JObject uniqueItem, JObject entity)
        {
            JArray effects = new JArray();
            Dictionary<string, int> unhandled =
                new Dictionary<string, int>(StringComparer.Ordinal);
            HashSet<string> effectIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> visitedBlueprints = new HashSet<string>(StringComparer.Ordinal);
            Queue<BlueprintWorkItem> queue = new Queue<BlueprintWorkItem>();

            AddRoot(queue, entity == null ? null : entity["Blueprint"] as JObject, "Item");
            JArray enchantments = entity == null ? null : entity["Enchantments"] as JArray;
            if (enchantments != null)
            {
                for (int i = 0; i < enchantments.Count; i++)
                {
                    JObject enchantment = enchantments[i] as JObject;
                    AddRoot(
                        queue,
                        enchantment == null ? null : enchantment["Blueprint"] as JObject,
                        "Enchantment");
                }
            }

            while (queue.Count > 0)
            {
                BlueprintWorkItem work = queue.Dequeue();
                if (work.Source.Depth > MaxBlueprintDepth)
                {
                    m_Coverage.TraversalLimitCount++;
                    continue;
                }

                JObject blueprint = m_Graph.ResolveBlueprint(work.Reference);
                if (blueprint == null)
                {
                    m_Coverage.MissingBlueprintCount++;
                    continue;
                }

                string nodeId = GetBlueprintNodeId(work.Reference, blueprint);

                if (!visitedBlueprints.Add(nodeId))
                {
                    continue;
                }

                m_Coverage.BlueprintVisitCount++;
                string blueprintType = (string)blueprint["ShortType"] ?? string.Empty;
                if (SemanticTraversalPolicy.IsDirectItemAbility(
                    work.Source.Relationship,
                    blueprintType))
                {
                    string syntheticId = nodeId + "|DirectItemAbility";
                    if (effectIds.Add(syntheticId))
                    {
                        effects.Add(m_EffectBuilder.BuildDirectAbility(
                            blueprint,
                            work.Source));
                        m_Coverage.AddRecognizedEffect(
                            work.Source,
                            "GrantedAbility",
                            true);
                    }
                }

                List<JObject> mechanicalObjects = m_Graph.CollectMechanicalObjects(blueprint);
                for (int i = 0; i < mechanicalObjects.Count; i++)
                {
                    JObject mechanical = mechanicalObjects[i];
                    m_Coverage.MechanicalObjectCount++;

                    SemanticComponentClassification classification =
                        SemanticComponentClassifier.Classify(mechanical);
                    string shortType = (string)mechanical["ShortType"]
                        ?? GetShortType((string)mechanical["Type"]);
                    string objectId = (string)mechanical["$id"]
                        ?? ((string)mechanical["SourcePath"] ?? shortType);

                    if (classification.Recognized)
                    {
                        string effectId = nodeId + "|" + objectId;
                        if (effectIds.Add(effectId))
                        {
                            effects.Add(m_EffectBuilder.Build(
                                mechanical,
                                blueprint,
                                work.Source,
                                classification));
                            m_Coverage.AddRecognizedEffect(
                                work.Source,
                                classification.Category,
                                false);
                        }
                    }
                    else if (classification.Structural)
                    {
                        m_Coverage.StructuralObjectCount++;
                    }
                    else
                    {
                        int count;
                        unhandled.TryGetValue(shortType, out count);
                        unhandled[shortType] = count + 1;
                        m_Coverage.UnhandledObjectCount++;
                        m_Coverage.AddUnhandled(
                            shortType,
                            (string)mechanical["SourcePath"] ?? string.Empty);
                    }

                    List<SemanticBlueprintLink> links =
                        m_Graph.CollectBlueprintLinks(mechanical);
                    for (int linkIndex = 0; linkIndex < links.Count; linkIndex++)
                    {
                        SemanticBlueprintLink link = links[linkIndex];
                        if (!SemanticTraversalPolicy.ShouldFollowComponentLink(
                            shortType,
                            link.FieldName))
                        {
                            continue;
                        }

                        queue.Enqueue(new BlueprintWorkItem
                        {
                            Reference = link.Reference,
                            Source = work.Source.Child(
                                link.Reference,
                                shortType + "." + link.FieldName)
                        });
                    }
                }

                List<SemanticBlueprintLink> directLinks =
                    m_Graph.CollectDirectBlueprintLinks(blueprint);
                for (int linkIndex = 0; linkIndex < directLinks.Count; linkIndex++)
                {
                    SemanticBlueprintLink link = directLinks[linkIndex];
                    if (!SemanticTraversalPolicy.ShouldFollowBlueprintField(
                        blueprintType,
                        link.FieldName))
                    {
                        continue;
                    }

                    queue.Enqueue(new BlueprintWorkItem
                    {
                        Reference = link.Reference,
                        Source = work.Source.Child(
                            link.Reference,
                            blueprintType + "." + link.FieldName)
                    });
                }
            }

            List<string> categories = CollectCategories(effects);
            string internalName = ReadInternalName(uniqueItem);
            string localizedName = ReadLocalized(uniqueItem["Name"]);
            string nameSource = "GameLocalization";
            string displayName = localizedName;
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = SemanticReferenceNameBuilder.BuildFallback(
                    internalName,
                    "BlueprintItem",
                    out nameSource);
            }
            int activeEffectCount = CountActiveEffects(effects);
            return new JObject
            {
                ["BlueprintGuid"] = (string)uniqueItem["BlueprintGuid"] ?? string.Empty,
                ["InternalName"] = internalName,
                ["Name"] = displayName,
                ["NameSource"] = nameSource,
                ["Description"] = ReadLocalized(uniqueItem["Description"]),
                ["Slot"] = NormalizeSlot(entity == null ? null : entity["FilterItemType"] as JObject),
                ["Cost"] = uniqueItem["Cost"] == null
                    ? 0
                    : uniqueItem["Cost"].DeepClone(),
                ["EffectCategories"] = new JArray(categories),
                ["Effects"] = effects,
                ["UnhandledComponents"] = BuildUnhandled(unhandled),
                ["HasRecognizedEffects"] = activeEffectCount > 0,
                ["ActiveEffectCount"] = activeEffectCount,
                ["InactiveComponentCount"] = CountInactiveEffects(effects),
                ["DirectEffectCount"] = CountEffectsByScope(effects, "Direct"),
                ["GrantedEffectCount"] = CountEffectsByScope(effects, "Granted"),
                ["NestedEffectCount"] = CountEffectsByScope(effects, "Nested"),
                ["VisitedBlueprintCount"] = visitedBlueprints.Count
            };
        }

        private static int CountActiveEffects(JArray effects)
        {
            return effects.Count - CountInactiveEffects(effects);
        }

        private static int CountInactiveEffects(JArray effects)
        {
            int result = 0;
            for (int i = 0; i < effects.Count; i++)
            {
                if ((bool?)effects[i]["IsInactive"] == true)
                {
                    result++;
                }
            }

            return result;
        }

        private static int CountEffectsByScope(JArray effects, string scope)
        {
            int result = 0;
            for (int i = 0; i < effects.Count; i++)
            {
                if ((bool?)effects[i]["IsInactive"] == true)
                {
                    continue;
                }

                JObject source = effects[i]["Source"] as JObject;
                if (source != null && (string)source["Scope"] == scope)
                {
                    result++;
                }
            }

            return result;
        }

        private static List<string> CollectCategories(JArray effects)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            List<string> result = new List<string>();
            for (int i = 0; i < effects.Count; i++)
            {
                if ((bool?)effects[i]["IsInactive"] == true)
                {
                    continue;
                }

                string category = (string)effects[i]["Category"] ?? string.Empty;
                if (!string.IsNullOrEmpty(category) && seen.Add(category))
                {
                    result.Add(category);
                }
            }

            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static JArray BuildUnhandled(Dictionary<string, int> values)
        {
            List<string> keys = new List<string>(values.Keys);
            keys.Sort(StringComparer.Ordinal);
            JArray result = new JArray();
            for (int i = 0; i < keys.Count; i++)
            {
                result.Add(new JObject
                {
                    ["ComponentType"] = keys[i],
                    ["Count"] = values[keys[i]]
                });
            }

            return result;
        }

        private static JObject NormalizeSlot(JObject slot)
        {
            if (slot == null)
            {
                return new JObject();
            }

            string raw = (string)slot["Raw"] ?? string.Empty;
            string display = (string)slot["Localized"];
            if (string.IsNullOrEmpty(display))
            {
                display = (string)slot["Display"] ?? raw;
            }

            return new JObject
            {
                ["Raw"] = raw,
                ["Display"] = display
            };
        }

        private static string ReadLocalized(JToken value)
        {
            JObject localized = value as JObject;
            if (localized == null)
            {
                return string.Empty;
            }

            return (string)localized["Localized"]
                ?? (string)localized["Raw"]
                ?? string.Empty;
        }

        private static string ReadInternalName(JObject uniqueItem)
        {
            JObject reference = uniqueItem["Blueprint"] as JObject;
            return reference == null
                ? string.Empty
                : (string)reference["InternalName"] ?? string.Empty;
        }

        private static string GetBlueprintNodeId(JObject reference, JObject blueprint)
        {
            return (string)reference["$ref"]
                ?? (string)reference["NodeId"]
                ?? (string)blueprint["$id"]
                ?? (string)blueprint["Guid"]
                ?? "unknown:" + ((string)blueprint["InternalName"] ?? string.Empty);
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

        private static void AddRoot(
            Queue<BlueprintWorkItem> queue,
            JObject reference,
            string relationship)
        {
            if (reference == null)
            {
                return;
            }

            queue.Enqueue(new BlueprintWorkItem
            {
                Reference = reference,
                Source = SemanticSourceContext.Root(reference, relationship)
            });
        }

        private sealed class BlueprintWorkItem
        {
            internal JObject Reference;
            internal SemanticSourceContext Source;
        }
    }
}
