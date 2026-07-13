using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed class SemanticReportCoverage
    {
        private readonly Dictionary<string, UnhandledTypeInfo> m_Unhandled =
            new Dictionary<string, UnhandledTypeInfo>(StringComparer.Ordinal);

        internal int ItemCount { get; set; }

        internal int BlueprintVisitCount { get; set; }

        internal int MechanicalObjectCount { get; set; }

        internal int RecognizedEffectCount { get; set; }

        internal int RecognizedMechanicalEffectCount { get; private set; }

        internal int SyntheticAbilityCount { get; private set; }

        internal int DirectEffectCount { get; private set; }

        internal int NestedEffectCount { get; private set; }

        internal int GrantedEffectCount { get; private set; }

        internal int ConditionEffectCount { get; private set; }

        internal int StructuralObjectCount { get; set; }

        internal int UnhandledObjectCount { get; set; }

        internal int MissingBlueprintCount { get; set; }

        internal int TraversalLimitCount { get; set; }

        internal void AddRecognizedEffect(
            SemanticSourceContext source,
            string category,
            bool syntheticAbility)
        {
            RecognizedEffectCount++;
            if (syntheticAbility)
            {
                SyntheticAbilityCount++;
            }
            else
            {
                RecognizedMechanicalEffectCount++;
            }

            string scope = source == null ? "Direct" : source.GetScope(syntheticAbility);
            if (scope == "Nested")
            {
                NestedEffectCount++;
            }
            else if (scope == "Granted")
            {
                GrantedEffectCount++;
            }
            else
            {
                DirectEffectCount++;
            }

            if (category == "Condition" || category == "Restriction")
            {
                ConditionEffectCount++;
            }
        }

        internal void AddUnhandled(string type, string samplePath)
        {
            string key = string.IsNullOrEmpty(type) ? "Unknown" : type;
            UnhandledTypeInfo info;
            if (!m_Unhandled.TryGetValue(key, out info))
            {
                info = new UnhandledTypeInfo { Type = key };
                m_Unhandled.Add(key, info);
            }

            info.Count++;
            if (info.SamplePaths.Count < 3
                && !string.IsNullOrEmpty(samplePath)
                && !info.SamplePaths.Contains(samplePath))
            {
                info.SamplePaths.Add(samplePath);
            }
        }

        internal JObject ToJson()
        {
            List<UnhandledTypeInfo> types = new List<UnhandledTypeInfo>(m_Unhandled.Values);
            types.Sort(delegate(UnhandledTypeInfo left, UnhandledTypeInfo right)
            {
                int count = right.Count.CompareTo(left.Count);
                return count != 0
                    ? count
                    : string.Compare(left.Type, right.Type, StringComparison.Ordinal);
            });

            JArray unhandled = new JArray();
            for (int i = 0; i < types.Count; i++)
            {
                unhandled.Add(new JObject
                {
                    ["ComponentType"] = types[i].Type,
                    ["Count"] = types[i].Count,
                    ["SamplePaths"] = new JArray(types[i].SamplePaths)
                });
            }

            return new JObject
            {
                ["ItemCount"] = ItemCount,
                ["BlueprintVisitCount"] = BlueprintVisitCount,
                ["MechanicalObjectCount"] = MechanicalObjectCount,
                ["RecognizedEffectCount"] = RecognizedEffectCount,
                ["RecognizedMechanicalEffectCount"] = RecognizedMechanicalEffectCount,
                ["SyntheticAbilityCount"] = SyntheticAbilityCount,
                ["DirectEffectCount"] = DirectEffectCount,
                ["GrantedEffectCount"] = GrantedEffectCount,
                ["NestedEffectCount"] = NestedEffectCount,
                ["ConditionOrRestrictionEffectCount"] = ConditionEffectCount,
                ["StructuralObjectCount"] = StructuralObjectCount,
                ["UnhandledObjectCount"] = UnhandledObjectCount,
                ["MissingBlueprintCount"] = MissingBlueprintCount,
                ["TraversalLimitCount"] = TraversalLimitCount,
                ["UnhandledComponentTypes"] = unhandled
            };
        }

        private sealed class UnhandledTypeInfo
        {
            internal string Type;
            internal int Count;
            internal readonly List<string> SamplePaths = new List<string>();
        }
    }
}
