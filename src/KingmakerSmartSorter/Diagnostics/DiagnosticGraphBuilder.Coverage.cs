using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed partial class DiagnosticGraphBuilder
    {
        private readonly Dictionary<string, int> m_ExpandedComponentTypes =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> m_TerminalUnityTypes =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> m_PotentialMechanicalTerminalTypes =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> m_IgnoredFields =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> m_Truncations =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, EnumCoverageEntry> m_EnumEntries =
            new Dictionary<string, EnumCoverageEntry>(StringComparer.Ordinal);

        private int m_BlueprintReferenceCount;
        private int m_ObjectReferenceCount;
        private int m_ExpandedComponentCount;
        private int m_TerminalUnityObjectCount;
        private int m_PotentialMechanicalTerminalCount;
        private int m_IgnoredFieldCount;
        private int m_LocalizedResolvedCount;
        private int m_LocalizedEmptyKeyCount;
        private int m_LocalizedUnresolvedKeyCount;
        private int m_EnumResolvedCount;
        private int m_EnumUnresolvedCount;
        private int m_SerializedPropertyCount;
        private int m_PropertyReadErrorCount;

        internal int ExpandedComponentCount
        {
            get { return m_ExpandedComponentCount; }
        }

        internal int PotentialMechanicalTerminalCount
        {
            get { return m_PotentialMechanicalTerminalCount; }
        }

        internal int TruncationCount
        {
            get
            {
                int total = 0;
                foreach (int count in m_Truncations.Values)
                {
                    total += count;
                }

                return total;
            }
        }

        internal JObject BuildCoverage()
        {
            return new JObject
            {
                ["BlueprintReferences"] = m_BlueprintReferenceCount,
                ["ObjectReferences"] = m_ObjectReferenceCount,
                ["ExpandedBlueprintComponents"] = m_ExpandedComponentCount,
                ["ExpandedBlueprintComponentTypes"] = BuildCountArray(
                    m_ExpandedComponentTypes,
                    "Type"),
                ["TerminalUnityObjects"] = m_TerminalUnityObjectCount,
                ["TerminalUnityObjectTypes"] = BuildCountArray(
                    m_TerminalUnityTypes,
                    "Type"),
                ["PotentiallyMechanicalTerminalUnityObjects"] =
                    m_PotentialMechanicalTerminalCount,
                ["PotentiallyMechanicalTerminalUnityObjectTypes"] = BuildCountArray(
                    m_PotentialMechanicalTerminalTypes,
                    "Type"),
                ["IgnoredRuntimeFields"] = m_IgnoredFieldCount,
                ["IgnoredRuntimeFieldTypes"] = BuildCountArray(
                    m_IgnoredFields,
                    "Field"),
                ["SerializedPublicProperties"] = m_SerializedPropertyCount,
                ["PropertyReadErrors"] = m_PropertyReadErrorCount,
                ["LocalizedValues"] = new JObject
                {
                    ["Resolved"] = m_LocalizedResolvedCount,
                    ["EmptyOptionalKey"] = m_LocalizedEmptyKeyCount,
                    ["UnresolvedNonEmptyKey"] = m_LocalizedUnresolvedKeyCount
                },
                ["Enums"] = new JObject
                {
                    ["Resolved"] = m_EnumResolvedCount,
                    ["Unresolved"] = m_EnumUnresolvedCount
                },
                ["Truncations"] = BuildCountArray(m_Truncations, "Reason"),
                ["ErrorCount"] = m_Errors.Count,
                ["SuppressedErrorsAfterLimit"] = m_SuppressedErrorCount,
                ["CompletenessRule"] =
                    "PotentiallyMechanicalTerminalUnityObjects and Truncations should both be zero before semantic tables are generated."
            };
        }

        internal JArray BuildEnumIndex()
        {
            List<EnumCoverageEntry> entries =
                new List<EnumCoverageEntry>(m_EnumEntries.Values);
            entries.Sort(delegate(EnumCoverageEntry left, EnumCoverageEntry right)
            {
                int type = string.Compare(left.Type, right.Type, StringComparison.Ordinal);
                if (type != 0)
                {
                    return type;
                }

                int numeric = left.Numeric.CompareTo(right.Numeric);
                return numeric != 0
                    ? numeric
                    : string.Compare(left.Raw, right.Raw, StringComparison.Ordinal);
            });

            JArray result = new JArray();
            for (int i = 0; i < entries.Count; i++)
            {
                EnumCoverageEntry entry = entries[i];
                List<string> paths = new List<string>(entry.Paths);
                paths.Sort(StringComparer.Ordinal);
                int sampleCount = Math.Min(paths.Count, 50);
                JArray samplePaths = new JArray();
                for (int j = 0; j < sampleCount; j++)
                {
                    samplePaths.Add(paths[j]);
                }

                result.Add(new JObject
                {
                    ["Type"] = entry.Type,
                    ["Raw"] = entry.Raw,
                    ["Numeric"] = entry.Numeric,
                    ["Display"] = entry.Display,
                    ["Localized"] = entry.Localized,
                    ["ResolutionSource"] = entry.ResolutionSource,
                    ["ResolutionStatus"] = entry.Resolved ? "Resolved" : "Unresolved",
                    ["Occurrences"] = entry.Occurrences,
                    ["PathCount"] = paths.Count,
                    ["SamplePaths"] = samplePaths
                });
            }

            return result;
        }

        private void TrackBlueprintReference()
        {
            m_BlueprintReferenceCount++;
        }

        private void TrackObjectReference()
        {
            m_ObjectReferenceCount++;
        }

        private void TrackExpandedComponent(Type type)
        {
            m_ExpandedComponentCount++;
            Increment(m_ExpandedComponentTypes, SafeTypeName(type));
        }

        private void TrackTerminalUnityObject(Type type, bool potentiallyMechanical)
        {
            m_TerminalUnityObjectCount++;
            string typeName = SafeTypeName(type);
            Increment(m_TerminalUnityTypes, typeName);
            if (potentiallyMechanical)
            {
                m_PotentialMechanicalTerminalCount++;
                Increment(m_PotentialMechanicalTerminalTypes, typeName);
            }
        }

        private void TrackIgnoredField(FieldInfo field, string reason)
        {
            m_IgnoredFieldCount++;
            string key = SafeTypeName(field == null ? null : field.DeclaringType)
                + "."
                + (field == null ? string.Empty : field.Name)
                + " | "
                + (reason ?? string.Empty);
            Increment(m_IgnoredFields, key);
        }

        private void TrackLocalizedValue(string status, string key)
        {
            if (string.Equals(status, "Resolved", StringComparison.Ordinal))
            {
                m_LocalizedResolvedCount++;
            }
            else if (string.IsNullOrEmpty(key))
            {
                m_LocalizedEmptyKeyCount++;
            }
            else
            {
                m_LocalizedUnresolvedKeyCount++;
            }
        }

        private void TrackEnum(
            Type type,
            string raw,
            long numeric,
            string display,
            string localized,
            string resolutionSource,
            bool resolved,
            string path)
        {
            if (resolved)
            {
                m_EnumResolvedCount++;
            }
            else
            {
                m_EnumUnresolvedCount++;
            }

            string typeName = SafeTypeName(type);
            string key = typeName
                + "|"
                + numeric
                + "|"
                + (raw ?? string.Empty);
            EnumCoverageEntry entry;
            if (!m_EnumEntries.TryGetValue(key, out entry))
            {
                entry = new EnumCoverageEntry
                {
                    Type = typeName,
                    Raw = raw ?? string.Empty,
                    Numeric = numeric,
                    Display = display ?? string.Empty,
                    Localized = localized ?? string.Empty,
                    ResolutionSource = resolutionSource ?? string.Empty,
                    Resolved = resolved
                };
                m_EnumEntries.Add(key, entry);
            }

            entry.Occurrences++;
            if (!string.IsNullOrEmpty(path))
            {
                entry.Paths.Add(path);
            }
        }

        private void TrackTruncation(string reason)
        {
            Increment(m_Truncations, reason ?? string.Empty);
        }

        private void TrackSerializedProperty()
        {
            m_SerializedPropertyCount++;
        }

        private void TrackPropertyReadError()
        {
            m_PropertyReadErrorCount++;
        }

        private static JArray BuildCountArray(
            Dictionary<string, int> values,
            string keyName)
        {
            List<KeyValuePair<string, int>> entries =
                new List<KeyValuePair<string, int>>(values);
            entries.Sort(delegate(
                KeyValuePair<string, int> left,
                KeyValuePair<string, int> right)
            {
                int count = right.Value.CompareTo(left.Value);
                return count != 0
                    ? count
                    : string.Compare(left.Key, right.Key, StringComparison.Ordinal);
            });

            JArray result = new JArray();
            for (int i = 0; i < entries.Count; i++)
            {
                result.Add(new JObject
                {
                    [keyName] = entries[i].Key,
                    ["Count"] = entries[i].Value
                });
            }

            return result;
        }

        private static void Increment(Dictionary<string, int> values, string key)
        {
            int count;
            values.TryGetValue(key ?? string.Empty, out count);
            values[key ?? string.Empty] = count + 1;
        }

        private static string SafeTypeName(Type type)
        {
            return type == null ? string.Empty : type.FullName ?? type.Name;
        }

        private sealed class EnumCoverageEntry
        {
            internal EnumCoverageEntry()
            {
                Paths = new HashSet<string>(StringComparer.Ordinal);
            }

            internal string Type;
            internal string Raw;
            internal long Numeric;
            internal string Display;
            internal string Localized;
            internal string ResolutionSource;
            internal bool Resolved;
            internal int Occurrences;
            internal HashSet<string> Paths;
        }
    }
}
