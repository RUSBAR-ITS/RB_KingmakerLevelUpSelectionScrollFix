using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed partial class DiagnosticGraphBuilder
    {
        private JToken SerializeEnum(Enum value, string path)
        {
            Type type = value.GetType();
            string raw = value.ToString();
            long numeric = GetEnumNumericValue(value);
            string localized;
            string source;
            bool resolved = m_Localization.TryResolveEnum(value, out localized, out source);
            bool isFlags = type.IsDefined(typeof(FlagsAttribute), false);
            JArray members = isFlags ? BuildFlagMembers(value) : new JArray();

            if (!resolved && members.Count > 0)
            {
                resolved = TryBuildLocalizedFlags(members, out localized, out source);
            }

            string display = resolved && !string.IsNullOrEmpty(localized)
                ? localized
                : HumanizeIdentifier(raw);
            TrackEnum(
                type,
                raw,
                numeric,
                display,
                localized,
                source,
                resolved,
                path);

            return new JObject
            {
                ["Kind"] = "Enum",
                ["Type"] = type.FullName,
                ["Raw"] = raw,
                ["Numeric"] = numeric,
                ["IsFlags"] = isFlags,
                ["Members"] = members,
                ["Display"] = display,
                ["Localized"] = resolved ? localized : string.Empty,
                ["ResolutionSource"] = resolved ? source : string.Empty,
                ["ResolutionStatus"] = resolved ? "Resolved" : "Unresolved"
            };
        }

        private JArray BuildFlagMembers(Enum value)
        {
            Type type = value.GetType();
            string[] names = value.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            JArray result = new JArray();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i].Trim();
                Enum member;
                try
                {
                    member = (Enum)Enum.Parse(type, name, false);
                }
                catch
                {
                    continue;
                }

                string localized;
                string source;
                bool resolved = m_Localization.TryResolveEnum(member, out localized, out source);
                result.Add(new JObject
                {
                    ["Raw"] = name,
                    ["Numeric"] = GetEnumNumericValue(member),
                    ["Display"] = resolved ? localized : HumanizeIdentifier(name),
                    ["Localized"] = resolved ? localized : string.Empty,
                    ["ResolutionSource"] = resolved ? source : string.Empty,
                    ["ResolutionStatus"] = resolved ? "Resolved" : "Unresolved"
                });
            }

            return result;
        }

        private static bool TryBuildLocalizedFlags(
            JArray members,
            out string localized,
            out string source)
        {
            List<string> values = new List<string>();
            List<string> sources = new List<string>();
            for (int i = 0; i < members.Count; i++)
            {
                JObject member = members[i] as JObject;
                if (member == null
                    || !string.Equals(
                        (string)member["ResolutionStatus"],
                        "Resolved",
                        StringComparison.Ordinal))
                {
                    localized = string.Empty;
                    source = string.Empty;
                    return false;
                }

                values.Add((string)member["Localized"] ?? string.Empty);
                string memberSource = (string)member["ResolutionSource"] ?? string.Empty;
                if (!sources.Contains(memberSource))
                {
                    sources.Add(memberSource);
                }
            }

            localized = string.Join(", ", values.ToArray());
            source = string.Join("; ", sources.ToArray());
            return values.Count > 0 && !string.IsNullOrEmpty(localized);
        }

        private static string HumanizeIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(value.Length + 8);
            char previous = '\0';
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (current == '_')
                {
                    result.Append(' ');
                    previous = current;
                    continue;
                }

                if (i > 0
                    && char.IsUpper(current)
                    && (char.IsLower(previous) || char.IsDigit(previous)))
                {
                    result.Append(' ');
                }

                result.Append(current);
                previous = current;
            }

            return result.ToString().Trim();
        }
    }
}
