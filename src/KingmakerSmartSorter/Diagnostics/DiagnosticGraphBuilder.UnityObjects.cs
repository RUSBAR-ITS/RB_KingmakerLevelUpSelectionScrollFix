using System;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
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
                ["SourcePath"] = CompactDiagnosticPath(path),
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
                ["SourcePath"] = CompactDiagnosticPath(path)
            };
        }

        private JToken SerializeElement(Element element, string path, int depth)
        {
            string id;
            JToken existingReference;
            if (!TryBeginReference(
                element,
                path,
                "element",
                out id,
                out existingReference))
            {
                return existingReference;
            }

            Type type = element.GetType();
            TrackExpandedElement(type);
            return new JObject
            {
                ["$id"] = id,
                ["Kind"] = "Element",
                ["Type"] = type.FullName,
                ["ShortType"] = type.Name,
                ["Name"] = SafeUnityName(element),
                ["SourcePath"] = CompactDiagnosticPath(path),
                ["Fields"] = SerializeFields(element, path + "/fields", depth),
                ["Properties"] = SerializeProperties(
                    element,
                    path + "/properties",
                    depth)
            };
        }

        private JToken SerializeLocalizedAsset(
            SharedStringAsset asset,
            string path,
            int depth)
        {
            string id;
            JToken existingReference;
            if (!TryBeginReference(
                asset,
                path,
                "localized-asset",
                out id,
                out existingReference))
            {
                return existingReference;
            }

            Type type = asset.GetType();
            TrackExpandedLocalizedAsset();
            return new JObject
            {
                ["$id"] = id,
                ["Kind"] = "LocalizedAsset",
                ["Type"] = type.FullName,
                ["ShortType"] = type.Name,
                ["Name"] = SafeUnityName(asset),
                ["SourcePath"] = CompactDiagnosticPath(path),
                ["Fields"] = SerializeFields(asset, path + "/fields", depth),
                ["Properties"] = SerializeProperties(
                    asset,
                    path + "/properties",
                    depth)
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

            if (field.Name.StartsWith("m_Cached", StringComparison.Ordinal)
                || string.Equals(
                    field.Name,
                    "m_EnchantmentsCollected",
                    StringComparison.Ordinal))
            {
                reason = "DerivedRuntimeCache";
                return true;
            }

            if (field.Name.StartsWith("<Fact>", StringComparison.Ordinal)
                || field.Name.StartsWith("<Owner>", StringComparison.Ordinal)
                || field.Name.StartsWith("<IsListeningEvents>", StringComparison.Ordinal)
                || string.Equals(field.Name, "m_Modifier", StringComparison.Ordinal))
            {
                reason = "RuntimeComponentState";
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
                && !fullName.StartsWith(
                    "Kingmaker.AreaLogic.Cutscenes.Commands.",
                    StringComparison.Ordinal)
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
