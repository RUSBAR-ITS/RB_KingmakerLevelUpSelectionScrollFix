using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed class SemanticBlueprintLink
    {
        internal string FieldName { get; set; }

        internal JObject Reference { get; set; }
    }
}
