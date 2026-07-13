using System;
using System.Reflection;
using Kingmaker.Blueprints;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed partial class DiagnosticGraphBuilder
    {
        private JToken SerializeBlueprintComponent(
            BlueprintComponent component,
            string path,
            int depth)
        {
            string id;
            JToken existingReference;
            if (!TryBeginReference(
                component,
                path,
                "blueprint-component",
                out id,
                out existingReference))
            {
                return existingReference;
            }

            Type type = component.GetType();
            TrackExpandedComponent(type);
            return new JObject
            {
                ["$id"] = id,
                ["Kind"] = "BlueprintComponent",
                ["Type"] = type.FullName,
                ["ShortType"] = type.Name,
                ["Name"] = SafeUnityName(component),
                ["SourcePath"] = path ?? string.Empty,
                ["Fields"] = SerializeFields(component, path + "/fields", depth),
                ["Properties"] = SerializeProperties(
                    component,
                    path + "/properties",
                    depth)
            };
        }

        private JToken SerializeTerminalUnityObject(
            UnityEngine.Object value,
            string path)
        {
            Type type = value.GetType();
            bool potentiallyMechanical = IsPotentiallyMechanicalUnityObject(type);
            TrackTerminalUnityObject(type, potentiallyMechanical);
            return new JObject
            {
                ["Kind"] = "UnityObjectTerminal",
                ["Type"] = type.FullName,
                ["ShortType"] = type.Name,
                ["Name"] = SafeUnityName(value),
                ["Category"] = GetUnityObjectCategory(type),
                ["PotentiallyMechanical"] = potentiallyMechanical,
                ["SourcePath"] = path ?? string.Empty
            };
        }

        private static bool TryGetIgnoredFieldReason(
            FieldInfo field,
            out string reason)
        {
            reason = string.Empty;
            if (field == null)
            {
                return false;
            }

            if (field.FieldType == typeof(IntPtr)
                || field.FieldType == typeof(UIntPtr)
                || string.Equals(field.Name, "m_CachedPtr", StringComparison.Ordinal))
            {
                reason = "NativeRuntimePointer";
                return true;
            }

            return false;
        }

        private static bool IsPotentiallyMechanicalUnityObject(Type type)
        {
            string fullName = SafeTypeName(type);
            if (!fullName.StartsWith("Kingmaker.", StringComparison.Ordinal))
            {
                return false;
            }

            return !fullName.StartsWith("Kingmaker.Visual.", StringComparison.Ordinal)
                && !fullName.StartsWith("Kingmaker.View.", StringComparison.Ordinal)
                && !fullName.StartsWith("Kingmaker.UI.", StringComparison.Ordinal)
                && !fullName.StartsWith("Kingmaker.ResourceLinks.", StringComparison.Ordinal)
                && fullName.IndexOf("Sound", StringComparison.OrdinalIgnoreCase) < 0
                && fullName.IndexOf("EquipmentEntity", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static string GetUnityObjectCategory(Type type)
        {
            string fullName = SafeTypeName(type);
            if (fullName.StartsWith("UnityEngine.Sprite", StringComparison.Ordinal)
                || fullName.StartsWith("UnityEngine.Texture", StringComparison.Ordinal)
                || fullName.StartsWith("UnityEngine.Material", StringComparison.Ordinal)
                || fullName.StartsWith("UnityEngine.GameObject", StringComparison.Ordinal)
                || fullName.StartsWith("UnityEngine.Animation", StringComparison.Ordinal)
                || fullName.StartsWith("UnityEngine.Audio", StringComparison.Ordinal)
                || fullName.StartsWith("Kingmaker.Visual.", StringComparison.Ordinal)
                || fullName.IndexOf("EquipmentEntity", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "VisualOrAudioAsset";
            }

            if (fullName.StartsWith("Kingmaker.ResourceLinks.", StringComparison.Ordinal))
            {
                return "ResourceLink";
            }

            return "OtherUnityAsset";
        }
    }
}
