using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KingmakerSmartAutoBuff
{
    internal static class CasterPriorityResolver
    {
        internal static List<CasterCandidate> SortCandidates(IEnumerable<CasterCandidate> candidates)
        {
            return (candidates ?? new List<CasterCandidate>())
                .OrderByDescending(candidate => candidate.IsAvailable)
                .ThenByDescending(candidate => candidate.CasterLevel)
                .ThenByDescending(candidate => candidate.CastingAttributeValue)
                .ThenBy(candidate => candidate.DisplayName ?? string.Empty, StringComparer.Ordinal)
                .ToList();
        }

        internal static void FillPriority(CasterCandidate candidate)
        {
            if (candidate == null || candidate.Entry == null)
            {
                return;
            }

            candidate.CasterLevel = ReadIntMember(candidate.Entry.Spellbook, "CasterLevel");
            candidate.CastingAttributeValue = ReadCastingAttributeValue(candidate.Entry);
        }

        private static int ReadCastingAttributeValue(SpellCatalogEntry entry)
        {
            try
            {
                object spellbook = entry.Spellbook;
                object blueprint = ReadMember(spellbook, "Blueprint");
                object attribute = ReadMember(blueprint, "CastingAttribute");
                if (attribute == null || entry.Caster == null)
                {
                    return 0;
                }

                object descriptor = ReadMember(entry.Caster, "Descriptor");
                object stats = ReadMember(descriptor, "Stats");
                if (stats == null)
                {
                    return 0;
                }

                MethodInfo getStat = stats.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(method => method.Name == "GetStat"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == attribute.GetType());

                if (getStat == null)
                {
                    return 0;
                }

                object stat = getStat.Invoke(stats, new[] { attribute });
                return ReadIntMember(stat, "ModifiedValue");
            }
            catch
            {
                return 0;
            }
        }

        private static int ReadIntMember(object owner, string name)
        {
            object value = ReadMember(owner, name);
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private static object ReadMember(object owner, string name)
        {
            if (owner == null)
            {
                return null;
            }

            Type type = owner.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
            {
                return property.GetValue(owner, null);
            }

            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field != null ? field.GetValue(owner) : null;
        }
    }
}
