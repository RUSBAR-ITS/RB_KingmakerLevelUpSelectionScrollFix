using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace KingmakerSmartAutoBuff
{
    internal sealed class QueueJsonSerializer
    {
        private readonly JsonSerializerSettings m_Settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver(),
            Formatting = Formatting.Indented,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        internal string Serialize(BuffQueueDefinition queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            StringBuilder buffer = new StringBuilder();
            using (StringWriter textWriter = new StringWriter(buffer))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter))
            {
                JsonSerializer.Create(m_Settings).Serialize(jsonWriter, queue);
            }

            string json = buffer.ToString();
            ValidateSerializedDocument(json, queue);
            return json;
        }

        internal BuffQueueDefinition Deserialize(
            string json,
            out bool containsName,
            out bool containsActions)
        {
            JObject document = JObject.Parse(json ?? string.Empty);
            containsName = HasProperty(document, "Name");
            containsActions = HasProperty(document, "Actions");

            using (StringReader textReader = new StringReader(json))
            using (JsonTextReader jsonReader = new JsonTextReader(textReader))
            {
                return JsonSerializer.Create(m_Settings).Deserialize<BuffQueueDefinition>(jsonReader);
            }
        }

        private void ValidateSerializedDocument(string json, BuffQueueDefinition original)
        {
            JObject document = JObject.Parse(json);
            if (!HasProperty(document, "Name") || !HasProperty(document, "Actions"))
            {
                throw new JsonSerializationException("Serialized queue is missing required Name or Actions fields.");
            }

            JToken actions = GetProperty(document, "Actions");
            if (actions == null || actions.Type != JTokenType.Array)
            {
                throw new JsonSerializationException("Serialized queue Actions field is not an array.");
            }

            bool containsName;
            bool containsActions;
            BuffQueueDefinition roundTrip = Deserialize(json, out containsName, out containsActions);
            int originalActionCount = original.Actions != null ? original.Actions.Count : 0;
            int restoredActionCount = roundTrip != null && roundTrip.Actions != null ? roundTrip.Actions.Count : -1;
            if (roundTrip == null
                || !string.Equals(roundTrip.Name, original.Name, StringComparison.Ordinal)
                || restoredActionCount != originalActionCount)
            {
                throw new JsonSerializationException("Serialized queue failed round-trip validation.");
            }
        }

        private static bool HasProperty(JObject document, string name)
        {
            return GetProperty(document, name) != null;
        }

        private static JToken GetProperty(JObject document, string name)
        {
            if (document == null)
            {
                return null;
            }

            JProperty property = document.Properties().FirstOrDefault(
                item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            return property != null ? property.Value : null;
        }
    }
}
