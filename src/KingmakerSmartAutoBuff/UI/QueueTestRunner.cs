using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerSmartAutoBuff
{
    internal static class QueueTestRunner
    {
        internal static void TestRun(QueueFile file)
        {
            if (file == null || file.Queue == null)
            {
                return;
            }

            Logger.Info("Test run started. queue=" + file.Queue.Name + ", actions=" + file.Queue.Actions.Count + ".");

            foreach (BuffQueueAction action in file.Queue.Actions)
            {
                SpellCatalogEntry currentEntry = SpellCatalog.FindCurrentEntry(action);
                if (currentEntry == null)
                {
                    Logger.Info(
                        "Would skip: spell is no longer available. caster="
                        + action.CasterName
                        + ", spell="
                        + action.SpellName
                        + ", metamagic="
                        + MetamagicLocalization.ListOrNone(action.Metamagic)
                        + ".");
                    continue;
                }

                List<string> currentTargets = ResolveCurrentTargetNames(action, currentEntry);
                if (currentEntry.TargetKind != TargetKind.NoTarget && currentTargets.Count == 0)
                {
                    Logger.Info(
                        "Would skip: no selected target is currently available. caster="
                        + action.CasterName
                        + ", spell="
                        + action.SpellName
                        + ".");
                    continue;
                }

                Logger.Info(
                    "Would cast: caster="
                    + currentEntry.CasterName
                    + ", spell="
                    + currentEntry.SpellName
                    + ", metamagic="
                    + currentEntry.MetamagicText
                    + ", targets="
                    + UiHelpers.ListOrNone(currentTargets)
                    + ".");
            }
        }

        private static List<string> ResolveCurrentTargetNames(BuffQueueAction action, SpellCatalogEntry entry)
        {
            if (entry.TargetKind == TargetKind.NoTarget)
            {
                return new List<string>();
            }

            if (entry.TargetKind == TargetKind.Self)
            {
                return new List<string> { entry.CasterName };
            }

            List<TargetOption> currentOptions = SpellCatalog.BuildTargetOptions(entry);
            List<string> result = new List<string>();

            for (int i = 0; i < action.TargetIds.Count; i++)
            {
                string targetId = action.TargetIds[i];
                string targetName = i < action.TargetNames.Count ? action.TargetNames[i] : string.Empty;
                TargetOption current = currentOptions.FirstOrDefault(target =>
                    string.Equals(target.Id, targetId, StringComparison.Ordinal)
                    || string.Equals(target.Name, targetName, StringComparison.Ordinal));

                if (current != null)
                {
                    result.Add(current.Name);
                }
                else
                {
                    Logger.Info(
                        "Would skip target: target is no longer available. caster="
                        + action.CasterName
                        + ", spell="
                        + action.SpellName
                        + ", target="
                        + targetName
                        + ".");
                }
            }

            return result;
        }
    }
}
