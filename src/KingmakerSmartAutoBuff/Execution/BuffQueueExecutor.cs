using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;

namespace KingmakerSmartAutoBuff
{
    internal sealed class BuffQueueExecutor
    {
        private const float InitialBuffVerificationDelay = 0.35f;
        private const float BuffVerificationRetryDelay = 0.25f;
        private const float BuffVerificationTimeout = 1.5f;

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

            if (m_State.CurrentGather != null)
            {
                UpdateCurrentGather(deltaTime);
                return;
            }

            if (m_State.CurrentCommand != null)
            {
                UpdateCurrentCommand(deltaTime);
                return;
            }

            if (m_State.PendingVerificationEntry != null)
            {
                UpdatePendingVerification(deltaTime);
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
                string reason;
                if (!ResolveEntry(task, out entry, out reason))
                {
                    RegisterSkip(task, reason);
                    continue;
                }

                AbilityBuffProfile profile = entry.BuffProfile ?? AbilityBuffProfileReader.Read(entry.Ability);
                DumpProfileIfNeeded(entry, profile);

                if (!IsAbilityCurrentlyAvailable(entry.Ability, out reason))
                {
                    RegisterSkip(task, reason);
                    continue;
                }

                if (profile.IsFriendlyBuff && (profile.IsAreaBuff || profile.DeliveryKind == BuffDeliveryKind.WholeParty))
                {
                    if (StartAreaTask(task, entry, profile))
                    {
                        return;
                    }

                    continue;
                }

                if (StartDirectTask(task, entry, profile))
                {
                    return;
                }
            }

            Complete();
        }

        private bool StartDirectTask(ResolvedCastTask task, SpellCatalogEntry entry, AbilityBuffProfile profile)
        {
            UnitEntityData targetUnit = ResolveDirectTarget(task, entry, profile);
            if (targetUnit == null)
            {
                RegisterSkip(task, ModLocalization.T("Execution.Skip.TargetUnavailable"));
                return false;
            }

            if (m_State.Mode == QueueExecutionMode.Smart)
            {
                ActiveBuffInfo matchedBuff;
                if (ActiveBuffHelper.HasAnyProfileBuff(targetUnit, profile, entry, out matchedBuff))
                {
                    RegisterSkip(
                        task,
                        string.Format(
                            ModLocalization.T("Execution.Skip.BuffAlreadyActive"),
                            matchedBuff != null ? matchedBuff.DisplayName : SpellCatalog.SafeUnitName(targetUnit)));
                    return false;
                }
            }

            TargetWrapper target = targetUnit;
            string reason;
            if (ShouldCheckCanTarget(entry.TargetKind) && !CanTarget(entry.Ability, target, out reason))
            {
                RegisterSkip(task, reason);
                return false;
            }

            UnitUseAbility command;
            if (!CastCommandRunner.TryRun(entry, target, out command, out reason))
            {
                RegisterFailure(task, reason);
                return false;
            }

            SetCurrentCommand(task, entry, command, new List<UnitEntityData> { targetUnit }, ResolveDisplayTarget(task, entry));
            return true;
        }

        private bool StartAreaTask(ResolvedCastTask task, SpellCatalogEntry entry, AbilityBuffProfile profile)
        {
            List<UnitEntityData> selectedRecipients = QueueActionResolver.ResolveRecipients(task.Action);
            if (selectedRecipients.Count == 0)
            {
                RegisterSkip(task, ModLocalization.T("Execution.Skip.TargetUnavailable"));
                return false;
            }

            BuffRecipientPlan plan = BuffRecipientPlanner.Plan(entry, profile, selectedRecipients, m_State.Mode);
            if (plan.RecipientsNeedingBuff.Count == 0)
            {
                RegisterSkip(task, ModLocalization.T("Execution.Skip.AllRecipientsAlreadyBuffed"));
                return false;
            }

            if (profile.DeliveryKind == BuffDeliveryKind.WholeParty)
            {
                return RunAreaCastNow(task, entry, profile, plan.RecipientsNeedingBuff);
            }

            if (profile.RadiusMeters <= 0.01f)
            {
                Logger.Warning("Area buff has no detected radius. spell=" + entry.SpellName + ".");
                return RunAreaCastNow(task, entry, profile, plan.RecipientsNeedingBuff);
            }

            m_State.CurrentGather = new PartyGatherController(entry.Caster, plan.RecipientsNeedingBuff, profile.RadiusMeters);
            m_State.PendingGatherTask = task;
            m_State.PendingGatherEntry = entry;
            m_State.PendingGatherRecipients = plan.RecipientsNeedingBuff;
            m_State.LastMessage = string.Format(
                ModLocalization.T("Execution.Status.Gathering"),
                entry.SpellName,
                plan.RecipientsNeedingBuff.Count);

            if (m_State.CurrentGather.IsFinished)
            {
                UpdateCurrentGather(0f);
            }

            return true;
        }

        private void UpdateCurrentGather(float deltaTime)
        {
            m_State.CurrentGather.Update(deltaTime);
            if (!m_State.CurrentGather.IsFinished)
            {
                return;
            }

            PartyGatherController gather = m_State.CurrentGather;
            ResolvedCastTask task = m_State.PendingGatherTask;
            SpellCatalogEntry entry = m_State.PendingGatherEntry;
            List<UnitEntityData> recipients = gather.ArrivedRecipients;

            m_State.CurrentGather = null;
            m_State.PendingGatherTask = null;
            m_State.PendingGatherEntry = null;
            m_State.PendingGatherRecipients = null;

            Logger.Info("Gather finished. spell=" + (entry != null ? entry.SpellName : "<spell>") + ", " + gather.Summary() + ".");

            if (recipients.Count == 0)
            {
                RegisterSkip(task, ModLocalization.T("Execution.Skip.NoRecipientsGathered"));
                return;
            }

            RunAreaCastNow(task, entry, entry.BuffProfile, recipients);
        }

        private bool RunAreaCastNow(
            ResolvedCastTask task,
            SpellCatalogEntry entry,
            AbilityBuffProfile profile,
            List<UnitEntityData> expectedRecipients)
        {
            string reason;
            UnitUseAbility command;
            bool started;

            if (profile != null && profile.DeliveryKind == BuffDeliveryKind.PointCenteredArea)
            {
                started = CastCommandRunner.TryRunAtPoint(entry, entry.Caster.Position, out command, out reason);
            }
            else
            {
                TargetWrapper target = entry.Caster;
                if (CanTarget(entry.Ability, target, out reason))
                {
                    started = CastCommandRunner.TryRun(entry, target, out command, out reason);
                }
                else
                {
                    started = CastCommandRunner.TryRunAtPoint(entry, entry.Caster.Position, out command, out reason);
                }
            }

            if (!started)
            {
                RegisterFailure(task, reason);
                return false;
            }

            SetCurrentCommand(task, entry, command, expectedRecipients, QueueActionResolver.FormatUnitList(expectedRecipients));
            return true;
        }

        private void SetCurrentCommand(
            ResolvedCastTask task,
            SpellCatalogEntry entry,
            UnitUseAbility command,
            List<UnitEntityData> expectedRecipients,
            string displayTarget)
        {
            m_State.CurrentTask = task;
            m_State.CurrentEntry = entry;
            m_State.CurrentCommand = command;
            m_State.CurrentExpectedRecipients = expectedRecipients;
            m_State.CurrentCommandTime = 0f;
            m_State.LastMessage = string.Format(
                ModLocalization.T("Execution.Status.Casting"),
                m_State.CompletedTasks,
                m_State.TotalTasks,
                entry.CasterName,
                entry.SpellName,
                displayTarget);

            Logger.Info(
                "Execution casting. caster="
                + entry.CasterName
                + ", spell="
                + entry.SpellName
                + ", recipients="
                + displayTarget
                + ".");
        }

        private void UpdateCurrentCommand(float deltaTime)
        {
            m_State.CurrentCommandTime += deltaTime;

            if (m_State.CurrentCommand.IsFinished)
            {
                UnitUseAbility command = m_State.CurrentCommand;
                ResolvedCastTask task = m_State.CurrentTask;
                SpellCatalogEntry entry = m_State.CurrentEntry;
                List<UnitEntityData> expectedRecipients = m_State.CurrentExpectedRecipients;

                m_State.CurrentCommand = null;
                m_State.CurrentTask = null;
                m_State.CurrentEntry = null;
                m_State.CurrentExpectedRecipients = null;
                m_State.CurrentCommandTime = 0f;

                if (command.Result == Kingmaker.UnitLogic.Commands.Base.UnitCommand.ResultType.Success)
                {
                    m_State.CastCount++;
                    Logger.Info(
                        "Execution cast finished. caster="
                        + entry.CasterName
                        + ", spell="
                        + entry.SpellName
                        + ", recipients="
                        + QueueActionResolver.FormatUnitList(expectedRecipients)
                        + ".");
                    ScheduleBuffVerification(entry, expectedRecipients);
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
                    ScheduleCastDelay();
                }

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
                m_State.CurrentExpectedRecipients = null;
                m_State.CurrentCommandTime = 0f;
                ScheduleCastDelay();
            }
        }

        private void ScheduleBuffVerification(SpellCatalogEntry entry, List<UnitEntityData> expectedRecipients)
        {
            if (entry == null || expectedRecipients == null || expectedRecipients.Count == 0)
            {
                ScheduleCastDelay();
                return;
            }

            m_State.PendingVerificationEntry = entry;
            m_State.PendingVerificationRecipients = new List<UnitEntityData>(expectedRecipients);
            m_State.PendingVerificationElapsed = 0f;
            m_State.PendingVerificationNextCheck = InitialBuffVerificationDelay;
        }

        private void UpdatePendingVerification(float deltaTime)
        {
            m_State.PendingVerificationElapsed += Math.Max(0f, deltaTime);
            m_State.PendingVerificationNextCheck -= Math.Max(0f, deltaTime);
            if (m_State.PendingVerificationNextCheck > 0f)
            {
                return;
            }

            SpellCatalogEntry entry = m_State.PendingVerificationEntry;
            List<UnitEntityData> recipients = m_State.PendingVerificationRecipients;
            bool finalAttempt = m_State.PendingVerificationElapsed >= BuffVerificationTimeout;
            if (TryVerifyCastResult(entry, recipients, finalAttempt))
            {
                ClearPendingVerification();
                ScheduleCastDelay();
                return;
            }

            if (finalAttempt)
            {
                ClearPendingVerification();
                ScheduleCastDelay();
                return;
            }

            m_State.PendingVerificationNextCheck = BuffVerificationRetryDelay;
        }

        private bool TryVerifyCastResult(
            SpellCatalogEntry entry,
            List<UnitEntityData> expectedRecipients,
            bool logMissingRecipients)
        {
            if (entry == null || expectedRecipients == null || expectedRecipients.Count == 0)
            {
                return true;
            }

            BuffApplicationResult result = BuffApplicationVerifier.VerifyRecipients(entry, entry.BuffProfile, expectedRecipients);
            if (result.Missing.Count > 0)
            {
                if (logMissingRecipients)
                {
                    Logger.Warning(
                        "Buff verification missing recipients. spell="
                        + entry.SpellName
                        + ", missing="
                        + QueueActionResolver.FormatUnitList(result.Missing)
                        + ", waited="
                        + m_State.PendingVerificationElapsed.ToString("0.##")
                        + "s.");
                }

                return false;
            }

            Logger.Info("Buff verification ok. spell=" + entry.SpellName + ", covered=" + result.Covered.Count + ".");
            return true;
        }

        private void ClearPendingVerification()
        {
            m_State.PendingVerificationEntry = null;
            m_State.PendingVerificationRecipients = null;
            m_State.PendingVerificationElapsed = 0f;
            m_State.PendingVerificationNextCheck = 0f;
        }

        private void ScheduleCastDelay()
        {
            m_State.DelayRemaining = Main.Settings != null ? Main.Settings.DelayBetweenCasts : 0.2f;
        }

        private bool ResolveEntry(ResolvedCastTask task, out SpellCatalogEntry entry, out string reason)
        {
            entry = null;
            reason = string.Empty;

            if (task == null || task.Action == null)
            {
                reason = ModLocalization.T("Execution.Skip.EmptyAction");
                return false;
            }

            entry = SpellCatalog.FindCurrentEntry(task.Action);
            if (entry == null)
            {
                reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                return false;
            }

            if (entry.BuffProfile == null || entry.BuffProfile.DeliveryKind == BuffDeliveryKind.Unsupported)
            {
                reason = ModLocalization.T("Execution.Skip.UnsupportedTarget");
                return false;
            }

            return true;
        }

        private static UnitEntityData ResolveDirectTarget(ResolvedCastTask task, SpellCatalogEntry entry, AbilityBuffProfile profile)
        {
            if (profile != null && profile.DeliveryKind == BuffDeliveryKind.Personal)
            {
                return entry.Caster;
            }

            UnitEntityData target = QueueActionResolver.ResolveCastTarget(task.Action);
            return target ?? entry.Caster;
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
                + ", recipients="
                + UiHelpers.ListOrNone(task.Action.RecipientNames)
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
                + ", recipients="
                + UiHelpers.ListOrNone(task.Action.RecipientNames)
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
            if (task == null || task.Action == null)
            {
                return ModLocalization.T("Common.None");
            }

            if (!string.IsNullOrEmpty(task.Action.CastTargetName))
            {
                return task.Action.CastTargetName;
            }

            if (entry != null && entry.BuffProfile != null && entry.BuffProfile.DeliveryKind == BuffDeliveryKind.Personal)
            {
                return entry.CasterName;
            }

            return UiHelpers.ListOrNone(task.Action.RecipientNames);
        }

        private static bool IsAbilityCurrentlyAvailable(AbilityData ability, out string reason)
        {
            reason = string.Empty;
            if (ability == null)
            {
                reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                return false;
            }

            try
            {
                if (!ability.IsAvailable || !ability.IsAvailableForCast)
                {
                    reason = SafeUnavailableReason(ability);
                    if (string.IsNullOrEmpty(reason))
                    {
                        reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to check ability availability.", ex);
                reason = ModLocalization.T("Execution.Skip.SpellUnavailable");
                return false;
            }

            return true;
        }

        private static bool ShouldCheckCanTarget(TargetKind targetKind)
        {
            return targetKind == TargetKind.Self
                || targetKind == TargetKind.SelectedAlly
                || targetKind == TargetKind.SelectedAllyOrSelf
                || targetKind == TargetKind.SelectedAny;
        }

        private static bool CanTarget(AbilityData ability, TargetWrapper target, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (ability != null && target != null && ability.CanTarget(target))
                {
                    return true;
                }

                reason = ModLocalization.T("Execution.Skip.BadTarget");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Exception("Failed to check ability target.", ex);
                reason = ModLocalization.T("Execution.Skip.BadTarget");
                return false;
            }
        }

        private static string SafeUnavailableReason(AbilityData ability)
        {
            try
            {
                return ability.GetUnavailableReason();
            }
            catch
            {
                return string.Empty;
            }
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

        private static void DumpProfileIfNeeded(SpellCatalogEntry entry, AbilityBuffProfile profile)
        {
            if (Main.Settings == null || !Main.Settings.LogDiagnostics || entry == null || profile == null)
            {
                return;
            }

            AbilityTargetProfile target = entry.TargetProfile;
            Logger.Info(
                "Spell profile. spell="
                + entry.SpellName
                + ", blueprint="
                + entry.SpellBlueprintId
                + ", targetKind="
                + entry.TargetKind
                + ", range="
                + (target != null ? target.Range.ToString() : "<range>")
                + ", canSelf="
                + (target != null && target.CanTargetSelf)
                + ", canFriends="
                + (target != null && target.CanTargetFriends)
                + ", canEnemies="
                + (target != null && target.CanTargetEnemies)
                + ", canPoint="
                + (target != null && target.CanTargetPoint)
                + ", delivery="
                + profile.DeliveryKind
                + ", friendly="
                + profile.IsFriendlyBuff
                + ", area="
                + profile.IsAreaBuff
                + ", radius="
                + profile.RadiusMeters.ToString("0.##")
                + ", buffs="
                + string.Join(",", profile.AppliedBuffNames.ToArray())
                + ", diagnostics="
                + string.Join(" > ", profile.Diagnostics.ToArray())
                + ".");
        }
    }
}
