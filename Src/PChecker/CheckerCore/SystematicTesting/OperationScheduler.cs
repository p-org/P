// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using PChecker.Exceptions;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies;
using PChecker.SystematicTesting.Traces;
using Debug = PChecker.IO.Debugging.Debug;

namespace PChecker.SystematicTesting
{
    /// <summary>
    /// Implements a scheduler that serializes and schedules controlled operations.
    /// </summary>
    /// <remarks>
    /// All operations run cooperatively on the thread that calls <see cref="Run"/>.
    /// A scheduling decision that picks a different operation parks the continuation of
    /// the current one (see <see cref="SchedulingPoint"/>) and control unwinds back to
    /// the run loop, which resumes the chosen operation.
    /// </remarks>
    internal sealed class OperationScheduler
    {
        /// <summary>
        /// The checkerConfiguration used by the scheduler.
        /// </summary>
        private readonly CheckerConfiguration CheckerConfiguration;

        /// <summary>
        /// The controlled runtime.
        /// </summary>
        private readonly ControlledRuntime Runtime;

        /// <summary>
        /// The scheduling strategy used for program exploration.
        /// </summary>
        private readonly ISchedulingStrategy Strategy;

        /// <summary>
        /// Map from unique ids to asynchronous operations.
        /// </summary>
        private readonly Dictionary<ulong, AsyncOperation> OperationMap;

        /// <summary>
        /// Registered operations ordered by id, as expected by the scheduling strategies.
        /// </summary>
        private readonly List<AsyncOperation> Operations;

        /// <summary>
        /// The program schedule trace.
        /// </summary>
        internal ScheduleTrace ScheduleTrace;

        /// <summary>
        /// The thread executing the run loop.
        /// </summary>
        private int LoopThreadId;

        /// <summary>
        /// Checks if the scheduler is running.
        /// </summary>
        internal bool IsRunning { get; private set; }

        /// <summary>
        /// The currently scheduled asynchronous operation.
        /// </summary>
        internal AsyncOperation ScheduledOperation { get; private set; }

        /// <summary>
        /// Number of scheduled steps.
        /// </summary>
        internal int ScheduledSteps => Strategy.GetScheduledSteps();

        /// <summary>
        /// Checks if the schedule has been fully explored.
        /// </summary>
        internal bool HasFullyExploredSchedule { get; private set; }

        /// <summary>
        /// True if a bug was found.
        /// </summary>
        internal bool BugFound { get; private set; }

        /// <summary>
        /// Bug report.
        /// </summary>
        internal string BugReport { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationScheduler"/> class.
        /// </summary>
        internal OperationScheduler(ControlledRuntime runtime, ISchedulingStrategy strategy,
            ScheduleTrace trace, CheckerConfiguration checkerConfiguration)
        {
            CheckerConfiguration = checkerConfiguration;
            Runtime = runtime;
            Strategy = strategy;
            OperationMap = new Dictionary<ulong, AsyncOperation>();
            Operations = new List<AsyncOperation>();
            ScheduleTrace = trace;
            IsRunning = true;
            BugFound = false;
            HasFullyExploredSchedule = false;
        }

        /// <summary>
        /// Runs scheduled operations until the schedule terminates.
        /// </summary>
        internal void Run()
        {
            LoopThreadId = Environment.CurrentManagedThreadId;
            while (IsRunning)
            {
                var op = ScheduledOperation;
                var continuation = op.Continuation;
                if (continuation is null)
                {
                    NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture,
                        "Operation '{0}' was scheduled, but it is not waiting at a scheduling point. Please make " +
                        "sure to avoid awaiting anything but P runtime operations inside state machine handlers.",
                        op.Name), cancelExecution: false);
                    break;
                }

                op.Continuation = null;
                continuation();
            }
        }

        /// <summary>
        /// Schedules the next enabled operation.
        /// </summary>
        /// <returns>
        /// An awaitable that completes immediately if the current operation keeps running,
        /// otherwise it suspends the current operation until it is scheduled again.
        /// </returns>
        internal SchedulingPoint ScheduleNextEnabledOperation(AsyncOperationType type)
        {
            var current = ScheduledOperation;
            if (!IsRunning)
            {
                return Suspend(current);
            }

            CheckNoExternalConcurrencyUsed();

            if (CheckIfSchedulingStepsBoundIsReached())
            {
                return Suspend(current);
            }

            current.Type = type;

            if (CheckerConfiguration.IsProgramStateHashingEnabled)
            {
                // Update the current operation with the hashed program state.
                current.HashedProgramState = Runtime.GetHashedProgramState();
            }

            if (!Strategy.GetNextOperation(current, Operations, out var next))
            {
                CheckIfProgramHasDeadlocked(Operations);

                Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                HasFullyExploredSchedule = true;
                Stop();
                return Suspend(current);
            }

            ScheduledOperation = next;
            ScheduleTrace.AddSchedulingChoice(next.Id);

            if (current == next)
            {
                return SchedulingPoint.Continue;
            }

            Debug.WriteLine("<ScheduleDebug> Scheduling the next operation of '{0}'.", next.Name);
            return Suspend(current);
        }

        /// <summary>
        /// Suspends the operation, unless it has already completed and there is nothing left to resume.
        /// </summary>
        private static SchedulingPoint Suspend(AsyncOperation op) =>
            op.Status is AsyncOperationStatus.Completed ? SchedulingPoint.Continue : new SchedulingPoint(op);

        /// <summary>
        /// Returns the next nondeterministic boolean choice.
        /// </summary>
        internal bool GetNextNondeterministicBooleanChoice(int maxValue)
        {
            if (!IsRunning)
            {
                // If scheduler is not running, throw exception to force terminate the caller.
                throw new ExecutionCanceledException();
            }

            CheckNoExternalConcurrencyUsed();

            if (CheckIfSchedulingStepsBoundIsReached())
            {
                // A choice cannot suspend its caller, so unwind it instead. This happens at
                // most once per schedule.
                throw new ExecutionCanceledException();
            }

            if (!Strategy.GetNextBooleanChoice(ScheduledOperation, maxValue, out var choice))
            {
                Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                Stop();
                throw new ExecutionCanceledException();
            }

            ScheduleTrace.AddNondeterministicBooleanChoice(choice);
            return choice;
        }

        /// <summary>
        /// Returns the next nondeterministic integer choice.
        /// </summary>
        internal int GetNextNondeterministicIntegerChoice(int maxValue)
        {
            if (!IsRunning)
            {
                // If scheduler is not running, throw exception to force terminate the caller.
                throw new ExecutionCanceledException();
            }

            CheckNoExternalConcurrencyUsed();

            if (CheckIfSchedulingStepsBoundIsReached())
            {
                // A choice cannot suspend its caller, so unwind it instead. This happens at
                // most once per schedule.
                throw new ExecutionCanceledException();
            }

            if (!Strategy.GetNextIntegerChoice(ScheduledOperation, maxValue, out var choice))
            {
                Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                Stop();
                throw new ExecutionCanceledException();
            }

            ScheduleTrace.AddNondeterministicIntegerChoice(choice);
            return choice;
        }

        /// <summary>
        /// Registers the specified asynchronous operation.
        /// </summary>
        /// <returns>True if the operation was successfully registered, else false if it already exists.</returns>
        internal bool RegisterOperation(AsyncOperation op)
        {
            if (!OperationMap.TryAdd(op.Id, op))
            {
                return false;
            }

            if (Operations.Count == 0)
            {
                ScheduledOperation = op;
            }

            var index = Operations.Count;
            while (index > 0 && Operations[index - 1].Id > op.Id)
            {
                index--;
            }

            Operations.Insert(index, op);
            return true;
        }

        /// <summary>
        /// Gets the <see cref="IAsyncOperation"/> associated with the specified
        /// unique id, or null if no such operation exists.
        /// </summary>
        [DebuggerStepThrough]
        internal TAsyncOperation GetOperationWithId<TAsyncOperation>(ulong id)
            where TAsyncOperation : IAsyncOperation
        {
            if (OperationMap.TryGetValue(id, out var op) &&
                op is TAsyncOperation expected)
            {
                return expected;
            }

            return default;
        }

        /// <summary>
        /// Gets the <see cref="IAsyncOperation"/> that is currently executing,
        /// or null if no such operation is executing.
        /// </summary>
        [DebuggerStepThrough]
        internal TAsyncOperation GetExecutingOperation<TAsyncOperation>()
            where TAsyncOperation : IAsyncOperation =>
            ScheduledOperation is TAsyncOperation expected ? expected : default;

        /// <summary>
        /// Returns all registered operations.
        /// </summary>
        internal IEnumerable<IAsyncOperation> GetRegisteredOperations() => Operations;

        /// <summary>
        /// Returns the enabled operation ids.
        /// </summary>
        internal HashSet<ulong> GetEnabledOperationIds()
        {
            var enabledSchedulableIds = new HashSet<ulong>();
            foreach (var op in Operations)
            {
                if (op.Status is AsyncOperationStatus.Enabled)
                {
                    enabledSchedulableIds.Add(op.Id);
                }
            }

            return enabledSchedulableIds;
        }

        /// <summary>
        /// Returns a test report with the scheduling statistics.
        /// </summary>
        internal TestReport GetReport()
        {
            var report = new TestReport(CheckerConfiguration);

            if (BugFound)
            {
                report.NumOfFoundBugs++;
                report.BugReports.Add(BugReport);
            }

            if (Strategy.IsFair())
            {
                report.NumOfExploredFairSchedules++;
                report.TotalExploredFairSteps += ScheduledSteps;

                if (report.MinExploredFairSteps < 0 ||
                    report.MinExploredFairSteps > ScheduledSteps)
                {
                    report.MinExploredFairSteps = ScheduledSteps;
                }

                if (report.MaxExploredFairSteps < ScheduledSteps)
                {
                    report.MaxExploredFairSteps = ScheduledSteps;
                }

                if (Strategy.HasReachedMaxSchedulingSteps())
                {
                    report.MaxFairStepsHitInFairTests++;
                }

                if (ScheduledSteps >= report.CheckerConfiguration.MaxUnfairSchedulingSteps)
                {
                    report.MaxUnfairStepsHitInFairTests++;
                }
            }
            else
            {
                report.NumOfExploredUnfairSchedules++;

                if (Strategy.HasReachedMaxSchedulingSteps())
                {
                    report.MaxUnfairStepsHitInUnfairTests++;
                }
            }

            return report;
        }

        /// <summary>
        /// Checks that the runtime is only invoked from the run loop thread.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckNoExternalConcurrencyUsed()
        {
            if (Environment.CurrentManagedThreadId != LoopThreadId)
            {
                NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture,
                    "Thread '{0}' invoked a runtime method outside of the scheduler. Please make sure to avoid using " +
                    "concurrency APIs (e.g. 'Task.Run', 'Task.Delay' or 'Task.Yield' from the 'System.Threading.Tasks' " +
                    "namespace) inside state machine handlers. If you are using external libraries that are executing " +
                    "concurrently, you will need to mock them during testing.",
                    Environment.CurrentManagedThreadId));
            }
        }

        /// <summary>
        /// Checks for a deadlock. This happens when there are no more enabled operations,
        /// but there is one or more blocked operations that are waiting to receive an event.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void CheckIfProgramHasDeadlocked(IEnumerable<AsyncOperation> ops)
        {
            var blockedOnReceiveOperations = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnReceive).ToList();
            var blockedOnWaitOperations = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnWaitAll ||
                                                          op.Status is AsyncOperationStatus.BlockedOnWaitAny).ToList();
            var blockedOnResourceSynchronization = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnResource).ToList();
            if (blockedOnReceiveOperations.Count == 0 &&
                blockedOnWaitOperations.Count == 0 &&
                blockedOnResourceSynchronization.Count == 0)
            {
                return;
            }

            var message = "Deadlock detected.";
            if (blockedOnReceiveOperations.Count > 0)
            {
                for (var i = 0; i < blockedOnReceiveOperations.Count; i++)
                {
                    message += string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnReceiveOperations[i].Name);
                    if (i == blockedOnReceiveOperations.Count - 2)
                    {
                        message += " and";
                    }
                    else if (i < blockedOnReceiveOperations.Count - 1)
                    {
                        message += ",";
                    }
                }

                message += blockedOnReceiveOperations.Count == 1 ? " is " : " are ";
                message += "waiting to receive an event, but no other controlled tasks are enabled.";
            }

            if (blockedOnWaitOperations.Count > 0)
            {
                for (var i = 0; i < blockedOnWaitOperations.Count; i++)
                {
                    message += string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnWaitOperations[i].Name);
                    if (i == blockedOnWaitOperations.Count - 2)
                    {
                        message += " and";
                    }
                    else if (i < blockedOnWaitOperations.Count - 1)
                    {
                        message += ",";
                    }
                }

                message += blockedOnWaitOperations.Count == 1 ? " is " : " are ";
                message += "waiting for a task to complete, but no other controlled tasks are enabled.";
            }

            if (blockedOnResourceSynchronization.Count > 0)
            {
                for (var i = 0; i < blockedOnResourceSynchronization.Count; i++)
                {
                    message += string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnResourceSynchronization[i].Name);
                    if (i == blockedOnResourceSynchronization.Count - 2)
                    {
                        message += " and";
                    }
                    else if (i < blockedOnResourceSynchronization.Count - 1)
                    {
                        message += ",";
                    }
                }

                message += blockedOnResourceSynchronization.Count == 1 ? " is " : " are ";
                message += "waiting to acquire a resource that is already acquired, ";
                message += "but no other controlled tasks are enabled.";
            }

            NotifyAssertionFailure(message, cancelExecution: false);
        }

        /// <summary>
        /// Checks if the scheduling steps bound has been reached. If yes, it stops the scheduler.
        /// </summary>
        /// <returns>True if the bound was reached and the schedule must end.</returns>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal bool CheckIfSchedulingStepsBoundIsReached()
        {
            if (!Strategy.HasReachedMaxSchedulingSteps())
            {
                return false;
            }

            var bound = Strategy.IsFair() ? CheckerConfiguration.MaxFairSchedulingSteps :
                CheckerConfiguration.MaxUnfairSchedulingSteps;
            var message = $"Scheduling steps bound of {bound} reached.";

            if (CheckerConfiguration.ConsiderDepthBoundHitAsBug)
            {
                NotifyAssertionFailure(message, cancelExecution: false);
            }
            else
            {
                Debug.WriteLine($"<ScheduleDebug> {message}");
                Stop();
            }

            return true;
        }

        /// <summary>
        /// Notify that an assertion has failed.
        /// </summary>
        /// <param name="text">Bug report.</param>
        /// <param name="killTasks">Kill tasks.</param>
        /// <param name="cancelExecution">
        /// Unwind the failing operation with an <see cref="ExecutionCanceledException"/>.
        /// </param>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void NotifyAssertionFailure(string text, bool killTasks = true, bool cancelExecution = true)
        {
            if (!BugFound)
            {
                BugReport = text;

                Runtime.LogWriter.LogAssertionFailure($"<ErrorLog> {text}");
                Runtime.RaiseOnFailureEvent(new AssertionFailureException(text));
                Runtime.LogWriter.LogStrategyDescription(CheckerConfiguration.SchedulingStrategy,
                    Strategy.GetDescription());

                BugFound = true;
            }

            if (killTasks)
            {
                Stop();
            }

            if (cancelExecution)
            {
                throw new ExecutionCanceledException();
            }
        }

        /// <summary>
        /// Stops the scheduler. Suspended operations are never resumed.
        /// </summary>
        private void Stop()
        {
            IsRunning = false;
            foreach (var op in Operations)
            {
                op.Status = AsyncOperationStatus.Canceled;
                op.Continuation = null;
            }
        }
    }
}
