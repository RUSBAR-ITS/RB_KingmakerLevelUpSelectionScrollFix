using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed class SemanticSourceContext
    {
        internal string Origin { get; private set; }

        internal string Relationship { get; private set; }

        internal int Depth { get; private set; }

        internal JArray Chain { get; private set; }

        internal string Scope { get; private set; }

        internal static SemanticSourceContext Root(
            JObject reference,
            string origin)
        {
            JArray chain = new JArray();
            chain.Add(BuildStep(reference, origin));
            return new SemanticSourceContext
            {
                Origin = origin ?? string.Empty,
                Relationship = origin ?? string.Empty,
                Depth = 0,
                Chain = chain,
                Scope = "Direct"
            };
        }

        internal SemanticSourceContext Child(
            JObject reference,
            string relationship)
        {
            JArray chain = Chain == null
                ? new JArray()
                : (JArray)Chain.DeepClone();
            chain.Add(BuildStep(reference, relationship));
            return new SemanticSourceContext
            {
                Origin = Origin,
                Relationship = relationship ?? string.Empty,
                Depth = Depth + 1,
                Chain = chain,
                Scope = Depth == 0 && IsGrantedRelationship(relationship)
                    ? "Granted"
                    : "Nested"
            };
        }

        internal string GetScope(bool forceDirect)
        {
            return forceDirect ? "Direct" : Scope ?? "Nested";
        }

        private static bool IsGrantedRelationship(string relationship)
        {
            string value = (relationship ?? string.Empty).ToLowerInvariant();
            return value.StartsWith("addunitfeature", System.StringComparison.Ordinal)
                || value.StartsWith("addunitfact", System.StringComparison.Ordinal)
                || value.StartsWith("addfeature", System.StringComparison.Ordinal)
                || value.StartsWith("addfacts", System.StringComparison.Ordinal)
                || value.StartsWith("grantfeature", System.StringComparison.Ordinal);
        }

        private static JObject BuildStep(JObject reference, string relationship)
        {
            return new JObject
            {
                ["Relationship"] = relationship ?? string.Empty,
                ["NodeId"] = reference == null
                    ? string.Empty
                    : (string)reference["NodeId"]
                        ?? (string)reference["$ref"]
                        ?? string.Empty,
                ["Guid"] = reference == null
                    ? string.Empty
                    : (string)reference["Guid"] ?? string.Empty,
                ["Type"] = reference == null
                    ? string.Empty
                    : (string)reference["ShortType"] ?? string.Empty,
                ["InternalName"] = reference == null
                    ? string.Empty
                    : (string)reference["InternalName"] ?? string.Empty,
                ["Name"] = reference == null
                    ? string.Empty
                    : (string)reference["ResolvedName"] ?? string.Empty
            };
        }
    }
}
