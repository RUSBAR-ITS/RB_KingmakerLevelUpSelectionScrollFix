using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;

namespace KingmakerSmartAutoBuff
{
    internal sealed class BuffQueueExecutor
    {
        private QueueExecutionState m_State;

        internal bool IsRunning
        {
            get { return m_State != null; }
        }

        internal string StatusText
        {
            get
            {
                if (m_State == null)
                {
                    return ModLocalization.T("Execution.Status.Idle");
                }

                return m_State.LastMessage;
            }
        }

        internal void Start(QueueFile file, QueueExecutionMode mode)
        {
            if (file == null || file.Queue == null)
            {
                SetIdleMessage(ModLocalization.T("Execution.Start.NoQueue"));
                return;
            }

            if (Main.Settings != null && Main.Settings.OnlyOutOfCombat && IsInCombat())
            {
                SetIdleMessage(ModLocalization.T("Execution.Start.InCombat"));
                return;
            }

            List<ResolvedCastTask> tasks = QueueExecutionPlanner.BuildTasks(file);
            if (tasks.Count == 0)
            {
                SetIdleMessage(ModLocalization.T("Execution.Start.EmptyQueue"));
                return;
            }

            m_State = new QueueExecutionState();
            m_State.QueueName = file.Queue.Name;
            m_State.Mode = mode;
            m_State.Tasks = tasks;
            m_State.LastMessage = string.Format(
                ModLocalization.T("Execution.Status.Started"),
                m_State.QueueName,
                m_State.TotalTasks);

            Logger.Info(
                "Execution started. queue="
                + m_State.QueueName
                + ", mode="
                + m_State.Mode
                + ", tasks="
                + m_State.TotalTasks
                + ".");
        }

        internal void Stop(string reason)
        {
            if (m_State == null)
            {
                return;
            }

            TryInterruptCurrentCommand();
            string message = string.IsNullOrEmpty(reason)
                ? ModLocalization.T("Execution.Status.Stopped")
                : reason;

            Logger.Info("Execution stopped. queue=" + m_State.QueueName + ", reason=" + message + ".");
            SetIdleMessage(message);
        }

        internal void Update(float deltaTime)
        {
            if (m_State == null)
            {
                return;
            }

            if (Main.Settings != null && Main.Settings.StopOnCombatStart && IsInCombat())
            {
                Stop(ModLocalization.T("Execution.Stop.Combat"));
                return;
            }

            if (m_State.CurrentCommand != null)
            {
                UpdateCurrentCommand(deltaTime);
                return;
            }

            if (m_State.DelayRemaining > 0f)
            {
                m_State.DelayRemaining -= deltaTime;
                return;
            }

            StartNextAvailableTask();
        }

        private void StartNextAvailableTask()
        {
            while (m_State != null && m_State.NextTaskIndex < m_State.TotalTasks)
            {
                ResolvedCastTask task = m_State.Tasks[m_State.NextTaskIndex];
                m_State.NextTaskIndex++;

                SpellCatalogEntry entry;
                Kingmaker.EntitySystem.Entities.UnitEntityData targetUnit;
                TargetWrapper target;
                string reason;
                if (!CastAvailabilityChecker.TryResolve(task, out entry, out targetUnit, out target, out reason))
                {
                    RegisterSkip(task, reason);
                    continue;
                }

                if (m_State.Mode == QueueExecutionMode.Smart && ShouldSkipAlreadyActiveBuff(task, entry, targetUnit, out reason))
                {
                    RegisterSkip(task, reason);
                    continue;
                }

                UnitUseAbility command;
                if (!CastCommandRunner.TryRun(entry, target, out command, out reason))
                {
                    RegisterFailure(task, reason);
                    continue;
                }

                m_State.CurrentTask = task;
                m_State.CurrentEntry = entry;
                m_State.CurrentCommand = command;
                m_State.CurrentCommandTime = 0f;
                m_State.LastMessage = string.Format(
                    ModLocalization.T("Execution.Status.Casting"),
                    m_State.CompletedTasks,
                    m_State.TotalTasks,
                    entry.CasterName,
                    entry.SpellName,
                    ResolveDisplayTarget(task, entry));

                Logger.Info(
                    "Execution casting. caster="
                    + entry.CasterName
                    + ", spell="
                    + entry.SpellName
                    + ", target="
                    + ResolveDisplayTarget(task, entry)
                    + ".");
                return;
            }

            Complete();
        }

        private static bool ShouldSkipAlreadyActiveBuff(
            ResolvedCastTask task,
            SpellCatalogEntry entry,
            Kingmaker.EntitySystem.Entities.UnitEntityData targetUnit,
            out string reason)
        {
            reason = string.Empty;
            ActiveBuffInfo matchedBuff;
            if (!ActiveBuffHelper.HasBuffFromAbility(targetUnit, entry, out matchedBuff))
            {
                return false;
            }

            reason = string.Format(
                ModLocalization.T("Execution.Skip.BuffAlreadyActive"),
                matchedBuff != null ? matchedBuff.DisplayName : ResolveDisplayTarget(task, entry));
            return true;
        }

        private void UpdateCurrentCommand(float deltaTime)
        {
            m_State.CurrentCommandTime += deltaTime;

            if (m_State.CurrentCommand.IsFinished)
            {
                UnitUseAbility command = m_State.CurrentCommand;
                ResolvedCastTask task = m_State.CurrentTask;
                SpellCatalogEntry entry = m_State.CurrentEntry;

                m_State.CurrentCommand = null;
                m_State.CurrentTask = null;
                m_State.CurrentEntry = null;
                m_State.CurrentCommandTime = 0f;

                if (command.Result == Kingmaker.UnitLogic.Commands.Base.UnitCommand.ResultType.Success)
                {
                    m_State.CastCount++;
                    Logger.Info(
                        "Execution cast finished. caster="
                        + entry.CasterName
                        + ", spell="
                        + entry.SpellName
                        + ", target="
                        + ResolveDisplayTarget(task, entry)
                        + ".");
                }
                else
                {
                    m_State.FailCount++;
                    Logger.Info(
                        "Execution command finished without success. result="
                        + command.Result
                        + ", caster="
                        + entry.CasterName
                        + ", spell="
                        + entry.SpellName
                        + ".");
                }

                m_State.DelayRemaining = Main.Settings != null ? Main.Settings.DelayBetweenCasts : 0.2f;
                return;
            }

            float timeout = Main.Settings != null ? Main.Settings.CastTimeoutSeconds : 20f;
            if (m_State.CurrentCommandTime >= timeout)
            {
                Logger.Warning(
                    "Execution command timed out. caster="
                    + (m_State.CurrentEntry != null ? m_State.CurrentEntry.CasterName : "<caster>")
                    + ", spell="
                    + (m_State.CurrentEntry != null ? m_State.CurrentEntry.SpellName : "<spell>")
                    + ".");
                TryInterruptCurrentCommand();
                m_State.FailCount++;
                m_State.CurrentCommand = null;
                m_State.CurrentTask = null;
                m_State.CurrentEntry = null;
                m_State.CurrentCommandTime = 0f;
                m_State.DelayRemaining = Main.Settings != null ? Main.Settings.DelayBetweenCasts : 0.2f;
            }
        }

        private void RegisterSkip(ResolvedCastTask task, string reason)
        {
            m_State.SkipCount++;
            m_State.LastMessage = string.Format(
                ModLocalization.T("Execution.Status.Skipped"),
                m_State.CompletedTasks,
                m_State.TotalTasks,
                task.Action.SpellName,
                reason);

            Logger.Info(
                "Execution skipped. caster="
                + task.Action.CasterName
                + ", spell="
                + task.Action.SpellName
                + ", target="
                + ResolveDisplayTarget(task, null)
                + ", reason="
                + reason
                + ".");
        }

        private void RegisterFailure(ResolvedCastTask task, string reason)
        {
            m_State.FailCount++;
            m_State.LastMessage = string.Format(
                ModLocalization.T("Execution.Status.Failed"),
                m_State.CompletedTasks,
                m_State.TotalTasks,
                task.Action.SpellName,
                reason);

            Logger.Info(
                "Execution failed. caster="
                + task.Action.CasterName
                + ", spell="
                + task.Action.SpellName
                + ", target="
                + ResolveDisplayTarget(task, null)
                + ", reason="
                + reason
                + ".");
        }

        private void Complete()
        {
            string message = string.Format(
                ModLocalization.T("Execution.Status.Completed"),
                m_State.QueueName,
                m_State.CastCount,
                m_State.SkipCount,
                m_State.FailCount);

            Logger.Info(
                "Execution completed. queue="
                + m_State.QueueName
                + ", mode="
                + m_State.Mode
                + ", cast="
                + m_State.CastCount
                + ", skipped="
                + m_State.SkipCount
                + ", failed="
                + m_State.FailCount
                + ".");

            SetIdleMessage(message);
        }

        private void TryInterruptCurrentCommand()
        {
            try
            {
                if (m_State != null && m_State.CurrentCommand != null && !m_State.CurrentCommand.IsFinished)
                {
                    m_State.CurrentCommand.Interrupt(true);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to interrupt current command.", ex);
            }
        }

        private void SetIdleMessage(string message)
        {
            m_State = null;
            if (Main.Ui != null)
            {
                Main.Ui.State.ExecutionStatus = message;
            }
        }

        private static string ResolveDisplayTarget(ResolvedCastTask task, SpellCatalogEntry entry)
        {
            if (entry != null && entry.TargetKind == TargetKind.Self)
            {
                return entry.CasterName;
            }

            if (task == null)
            {
                return ModLocalization.T("Common.None");
            }

            if (!string.IsNullOrEmpty(task.TargetName))
            {
                return task.TargetName;
            }

            return ModLocalization.T("Common.None");
        }

        private static bool IsInCombat()
        {
            try
            {
                return Game.Instance != null && Game.Instance.Player != null && Game.Instance.Player.IsInCombat;
            }
            catch
            {
                return false;
            }
        }
    }
}
