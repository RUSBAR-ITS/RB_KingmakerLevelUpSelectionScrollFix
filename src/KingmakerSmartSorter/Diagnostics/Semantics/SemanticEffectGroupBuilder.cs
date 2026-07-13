using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal static class SemanticEffectGroupBuilder
    {
        internal static JArray Build(JArray effects)
        {
            JArray result = new JArray();
            Dictionary<string, GroupState> groups =
                new Dictionary<string, GroupState>(StringComparer.Ordinal);

            for (int i = 0; i < effects.Count; i++)
            {
                JObject effect = effects[i] as JObject;
                if (effect == null)
                {
                    continue;
                }

                string signature = BuildSignature(effect);
                GroupState state;
                if (!groups.TryGetValue(signature, out state))
                {
                    state = new GroupState(BuildGroup(effect));
                    groups.Add(signature, state);
                    result.Add(state.Group);
                }

                AddEffect(state, effect, i);
            }

            for (int i = 0; i < result.Count; i++)
            {
                JObject group = (JObject)result[i];
                int count = (int?)group["Count"] ?? 1;
                string baseSummary = (string)group["BaseSummary"] ?? string.Empty;
                group["IsRepeated"] = count > 1;
                group["Summary"] = count > 1
                    ? string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        SemanticLocalization.Template(
                            "RepeatedEffect",
                            "{0} (repeated {1} times)"),
                        baseSummary,
                        count)
                    : baseSummary;
            }

            return result;
        }

        private static JObject BuildGroup(JObject effect)
        {
            return new JObject
            {
                ["Category"] = CloneOrNull(effect["Category"]),
                ["CategoryDisplay"] = CloneOrNull(effect["CategoryDisplay"]),
                ["ComponentType"] = CloneOrNull(effect["ComponentType"]),
                ["ComponentDisplay"] = CloneOrNull(effect["ComponentDisplay"]),
                ["DefinitionMode"] = CloneOrNull(effect["DefinitionMode"]),
                ["DefinitionModeDisplay"] = CloneOrNull(effect["DefinitionModeDisplay"]),
                ["IsPresenceBased"] = CloneOrNull(effect["IsPresenceBased"]),
                ["IsInactive"] = CloneOrNull(effect["IsInactive"]),
                ["Count"] = 0,
                ["BaseSummary"] = CloneOrNull(effect["Summary"]),
                ["Summary"] = CloneOrNull(effect["Summary"]),
                ["Parameters"] = CloneOrNull(effect["Parameters"]),
                ["Scopes"] = new JArray(),
                ["SourceBlueprints"] = new JArray(),
                ["EffectIndexes"] = new JArray()
            };
        }

        private static void AddEffect(
            GroupState state,
            JObject effect,
            int effectIndex)
        {
            state.Group["Count"] = ((int?)state.Group["Count"] ?? 0) + 1;
            ((JArray)state.Group["EffectIndexes"]).Add(effectIndex);

            JObject source = effect["Source"] as JObject;
            if (source == null)
            {
                return;
            }

            string scope = (string)source["Scope"] ?? string.Empty;
            if (!string.IsNullOrEmpty(scope) && state.Scopes.Add(scope))
            {
                ((JArray)state.Group["Scopes"]).Add(new JObject
                {
                    ["Raw"] = scope,
                    ["Display"] = (string)source["ScopeDisplay"]
                        ?? SemanticLocalization.Scope(scope)
                });
            }

            string sourceKey = string.Join(
                "|",
                (string)source["BlueprintGuid"] ?? string.Empty,
                (string)source["Relationship"] ?? string.Empty,
                (string)source["Origin"] ?? string.Empty,
                Convert.ToString(
                    (int?)source["Depth"] ?? 0,
                    System.Globalization.CultureInfo.InvariantCulture));
            if (!state.Sources.Add(sourceKey))
            {
                return;
            }

            ((JArray)state.Group["SourceBlueprints"]).Add(new JObject
            {
                ["BlueprintGuid"] = CloneOrNull(source["BlueprintGuid"]),
                ["BlueprintType"] = CloneOrNull(source["BlueprintType"]),
                ["InternalName"] = CloneOrNull(source["InternalName"]),
                ["Name"] = CloneOrNull(source["Name"]),
                ["NameSource"] = CloneOrNull(source["NameSource"]),
                ["Relationship"] = CloneOrNull(source["Relationship"]),
                ["Origin"] = CloneOrNull(source["Origin"]),
                ["Depth"] = CloneOrNull(source["Depth"])
            });
        }

        private static string BuildSignature(JObject effect)
        {
            return string.Join(
                "\u001f",
                (string)effect["Category"] ?? string.Empty,
                (string)effect["ComponentType"] ?? string.Empty,
                (string)effect["DefinitionMode"] ?? string.Empty,
                effect["Parameters"] == null
                    ? string.Empty
                    : effect["Parameters"].ToString(Formatting.None));
        }

        private static JToken CloneOrNull(JToken value)
        {
            return value == null ? JValue.CreateNull() : value.DeepClone();
        }

        private sealed class GroupState
        {
            internal readonly JObject Group;
            internal readonly HashSet<string> Scopes =
                new HashSet<string>(StringComparer.Ordinal);
            internal readonly HashSet<string> Sources =
                new HashSet<string>(StringComparer.Ordinal);

            internal GroupState(JObject group)
            {
                Group = group;
            }
        }
    }
}
