using System;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker.Blueprints.Root;
using Kingmaker.Localization;

namespace KingmakerSmartSorter
{
    internal sealed class GameLocalizationResolver
    {
        private const int ProviderScanDepth = 4;

        private readonly Dictionary<Type, ResolverBinding> m_EnumResolvers =
            new Dictionary<Type, ResolverBinding>();
        private readonly HashSet<object> m_VisitedProviders =
            new HashSet<object>(ReferenceIdentityComparer.Instance);

        internal GameLocalizationResolver()
        {
            try
            {
                BlueprintRoot root = BlueprintRoot.Instance;
                if (root != null && root.LocalizedTexts != null)
                {
                    ScanProvider(root.LocalizedTexts, 0);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to initialize game localization resolvers.", ex);
            }
        }

        internal string CurrentLocale
        {
            get
            {
                try
                {
                    return LocalizationManager.CurrentLocale.ToString();
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        internal bool TryResolveEnum(Enum value, out string text, out string source)
        {
            text = string.Empty;
            source = string.Empty;
            if (value == null)
            {
                return false;
            }

            ResolverBinding binding;
            if (!m_EnumResolvers.TryGetValue(value.GetType(), out binding))
            {
                return false;
            }

            try
            {
                text = binding.Method.Invoke(binding.Provider, new object[] { value }) as string
                    ?? string.Empty;
                source = binding.Provider.GetType().FullName
                    + "."
                    + binding.Method.Name;
                return !string.IsNullOrEmpty(text);
            }
            catch
            {
                text = string.Empty;
                source = string.Empty;
                return false;
            }
        }

        internal static string Resolve(LocalizedString value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            try
            {
                return value.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ScanProvider(object provider, int depth)
        {
            if (provider == null
                || depth > ProviderScanDepth
                || !m_VisitedProviders.Add(provider))
            {
                return;
            }

            Type type = provider.GetType();
            RegisterResolverMethods(provider, type);

            FieldInfo[] fields = GetInstanceFields(type);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                Type fieldType = field.FieldType;
                if (fieldType == typeof(LocalizedString)
                    || fieldType.IsPrimitive
                    || fieldType.IsEnum
                    || fieldType == typeof(string)
                    || !IsLocalizationProviderType(fieldType))
                {
                    continue;
                }

                try
                {
                    ScanProvider(field.GetValue(provider), depth + 1);
                }
                catch
                {
                    // A missing optional localization provider is not fatal.
                }
            }
        }

        private void RegisterResolverMethods(object provider, Type type)
        {
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                ParameterInfo[] parameters = method.GetParameters();
                if (!string.Equals(method.Name, "GetText", StringComparison.Ordinal)
                    || method.ReturnType != typeof(string)
                    || parameters.Length != 1
                    || !parameters[0].ParameterType.IsEnum
                    || m_EnumResolvers.ContainsKey(parameters[0].ParameterType))
                {
                    continue;
                }

                m_EnumResolvers.Add(
                    parameters[0].ParameterType,
                    new ResolverBinding(provider, method));
            }
        }

        private static bool IsLocalizationProviderType(Type type)
        {
            string fullName = type == null ? string.Empty : type.FullName ?? string.Empty;
            return fullName.StartsWith(
                "Kingmaker.Blueprints.Root.Strings.",
                StringComparison.Ordinal)
                || fullName == "Kingmaker.Blueprints.Root.LocalizedTexts";
        }

        private static FieldInfo[] GetInstanceFields(Type type)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            Type current = type;
            while (current != null && current != typeof(object))
            {
                result.AddRange(current.GetFields(
                    BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly));
                current = current.BaseType;
            }

            return result.ToArray();
        }

        private sealed class ResolverBinding
        {
            internal ResolverBinding(object provider, MethodInfo method)
            {
                Provider = provider;
                Method = method;
            }

            internal object Provider { get; private set; }

            internal MethodInfo Method { get; private set; }
        }
    }
}
