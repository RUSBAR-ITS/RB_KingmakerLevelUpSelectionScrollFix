using System;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
using Newtonsoft.Json.Linq;

namespace KingmakerSmartSorter
{
    internal sealed partial class DiagnosticGraphBuilder
    {
        private JArray SerializeProperties(object owner, string path, int depth)
        {
            PropertyInfo[] properties = GetDiagnosticProperties(owner.GetType());
            JArray result = new JArray();
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                string propertyPath = path
                    + "/"
                    + property.DeclaringType.FullName
                    + "."
                    + property.Name;
                JObject entry = new JObject
                {
                    ["Name"] = property.Name,
                    ["DeclaringType"] = property.DeclaringType.FullName,
                    ["ValueType"] = property.PropertyType.FullName
                };
                try
                {
                    entry["Value"] = SerializeValue(
                        property.GetValue(owner, null),
                        propertyPath,
                        depth + 1);
                    TrackSerializedProperty();
                }
                catch (Exception ex)
                {
                    Exception effective = ex is TargetInvocationException
                        && ex.InnerException != null
                        ? ex.InnerException
                        : ex;
                    TrackPropertyReadError();
                    RecordError(propertyPath, "ReadProperty", effective);
                    entry["Value"] = new JObject
                    {
                        ["$error"] = effective.Message
                    };
                }

                result.Add(entry);
            }

            return result;
        }

        private static PropertyInfo[] GetDiagnosticProperties(Type type)
        {
            PropertyInfo[] cached;
            if (s_PropertyCache.TryGetValue(type, out cached))
            {
                return cached;
            }

            List<PropertyInfo> result = new List<PropertyInfo>();
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance
                | BindingFlags.Public);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                MethodInfo getter = property.GetGetMethod(false);
                string declaringType = property.DeclaringType == null
                    ? string.Empty
                    : property.DeclaringType.FullName ?? string.Empty;
                if (getter == null
                    || getter.IsStatic
                    || property.GetIndexParameters().Length != 0
                    || property.PropertyType == typeof(IntPtr)
                    || property.PropertyType == typeof(UIntPtr)
                    || typeof(Delegate).IsAssignableFrom(property.PropertyType)
                    || !declaringType.StartsWith("Kingmaker.", StringComparison.Ordinal)
                    || IsRuntimeOnlyProperty(property)
                    || !IsSupportedPropertyType(property.PropertyType))
                {
                    continue;
                }

                result.Add(property);
            }

            result.Sort(delegate(PropertyInfo left, PropertyInfo right)
            {
                int declaring = string.Compare(
                    left.DeclaringType.FullName,
                    right.DeclaringType.FullName,
                    StringComparison.Ordinal);
                return declaring != 0
                    ? declaring
                    : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
            });
            cached = result.ToArray();
            s_PropertyCache[type] = cached;
            return cached;
        }

        private static bool IsRuntimeOnlyProperty(PropertyInfo property)
        {
            string name = property == null ? string.Empty : property.Name;
            return string.Equals(name, "Context", StringComparison.Ordinal)
                || string.Equals(name, "Fact", StringComparison.Ordinal)
                || string.Equals(name, "Owner", StringComparison.Ordinal)
                || string.Equals(name, "IsListeningEvents", StringComparison.Ordinal)
                || string.Equals(name, "IsReapplying", StringComparison.Ordinal)
                || string.Equals(name, "IsAvailable", StringComparison.Ordinal);
        }

        private static bool IsSupportedPropertyType(Type type)
        {
            return IsScalar(type)
                || type.IsEnum
                || type.IsValueType
                || type == typeof(LocalizedString)
                || typeof(BlueprintScriptableObject).IsAssignableFrom(type)
                || typeof(BlueprintComponent).IsAssignableFrom(type)
                || typeof(Element).IsAssignableFrom(type);
        }
    }
}
