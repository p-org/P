// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.IO.Debugging;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Traces;

namespace PChecker.SystematicTesting.Strategies.Special
{
    /// <summary>
    /// Class representing a replaying scheduling strategy.
    /// </summary>
    internal sealed class ReplayStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The checkerConfiguration.
        /// </summary>
        private readonly CheckerConfiguration _checkerConfiguration;

        /// <summary>
        /// The Coyote program schedule trace.
        /// </summary>
        private readonly ScheduleTrace ScheduleTrace;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private readonly ISchedulingStrategy SuffixStrategy;

        /// <summary>
        /// Is the scheduler that produced the
        /// schedule trace fair?
        /// </summary>
        private readonly bool IsSchedulerFair;

        /// <summary>
        /// Is the scheduler replaying the trace?
        /// </summary>
        private bool IsReplaying;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps;

        /// <summary>
        /// Text describing a replay error.
        /// </summary>
        internal string ErrorText { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayStrategy"/> class.
        /// </summary>
        public ReplayStrategy(CheckerConfiguration checkerConfiguration, ScheduleTrace trace, bool isFair)
            : this(checkerConfiguration, trace, isFair, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayStrategy"/> class.
        /// </summary>
        public ReplayStrategy(CheckerConfiguration checkerConfiguration, ScheduleTrace trace, bool isFair, ISchedulingStrategy suffixStrategy)
        {
            _checkerConfiguration = checkerConfiguration;
            ScheduleTrace = trace;
            ScheduledSteps = 0;
            IsSchedulerFair = isFair;
            IsReplaying = true;
            SuffixStrategy = suffixStrategy;
            ErrorText = string.Empty;
        }

        /// <inheritdoc/>
        public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
        {
            if (IsReplaying)
            {
                var asyncOperations = ops.ToList();
                var enabledOperations = asyncOperations.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
                if (enabledOperations.Count == 0)
                {
                    next = null;
                    return false;
                }

                try
                {
                    if (ScheduledSteps >= ScheduleTrace.Count)
                    {
                        ErrorText = "Trace is not reproducible: execution is longer than trace.";
                        throw new InvalidOperationException(ErrorText);
                    }

                    var nextStep = ScheduleTrace[ScheduledSteps];
                    if (nextStep.Type != ScheduleStepType.SchedulingChoice)
                    {
                        ErrorText = "Trace is not reproducible: next step is not a scheduling choice.";
                        throw new InvalidOperationException(ErrorText);
                    }

                    next = enabledOperations.FirstOrDefault(op => op.Id == nextStep.ScheduledOperationId);
                    if (next is null)
                    {
                        ErrorText = $"Trace is not reproducible: cannot detect id '{nextStep.ScheduledOperationId}'.";
                        throw new InvalidOperationException(ErrorText);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    if (SuffixStrategy is null)
                    {
                        if (!_checkerConfiguration.DisableEnvironmentExit)
                        {
                            Error.ReportAndExit(ex.Message);
                        }

                        next = null;
                        return false;
                    }
                    else
                    {
                        IsReplaying = false;
                        return SuffixStrategy.GetNextOperation(current, asyncOperations, out next);
                    }
                }

                ScheduledSteps++;
                return true;
            }

            return SuffixStrategy.GetNextOperation(current, ops, out next);
        }

        /// <inheritdoc/>
        public bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            if (IsReplaying)
            {
                ScheduleStep nextStep;

                try
                {
                    if (ScheduledSteps >= ScheduleTrace.Count)
                    {
                        ErrorText = "Trace is not reproducible: execution is longer than trace.";
                        throw new InvalidOperationException(ErrorText);
                    }

                    nextStep = ScheduleTrace[ScheduledSteps];
                    if (nextStep.Type != ScheduleStepType.NondeterministicChoice)
                    {
                        ErrorText = "Trace is not reproducible: next step is not a nondeterministic choice.";
                        throw new InvalidOperationException(ErrorText);
                    }

                    if (nextStep.BooleanChoice is null)
                    {
                        ErrorText = "Trace is not reproducible: next step is not a nondeterministic boolean choice.";
                        throw new InvalidOperationException(ErrorText);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    if (SuffixStrategy is null)
                    {
                        if (!_checkerConfiguration.DisableEnvironmentExit)
                        {
                            Error.ReportAndExit(ex.Message);
                        }

                        next = false;
                        return false;
                    }
                    else
                    {
                        IsReplaying = false;
                        return SuffixStrategy.GetNextBooleanChoice(current, maxValue, out next);
                    }
                }

                next = nextStep.BooleanChoice.Value;
                ScheduledSteps++;
                return true;
            }

            return SuffixStrategy.GetNextBooleanChoice(current, maxValue, out next);
        }

        /// <inheritdoc/>
        public bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            if (IsReplaying)
            {
                ScheduleStep nextStep;

                try
                {
                    if (ScheduledSteps >= ScheduleTrace.Count)
                    {
                        ErrorText = "Trace is not reproducible: execution is longer than trace.";
                        throw new InvalidOperationException(ErrorText);
                    }

                    nextStep = ScheduleTrace[ScheduledSteps];
                    if (nextStep.Type != ScheduleStepType.NondeterministicChoice)
                    {
                        ErrorText = "Trace is not reproducible: next step is not a nondeterministic choice.";
                        throw new InvalidOperationException(ErrorText);
                    }

                    if (nextStep.IntegerChoice is null)
                    {
                        ErrorText = "Trace is not reproducible: next step is not a nondeterministic integer choice.";
                        throw new InvalidOperationException(ErrorText);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    if (SuffixStrategy is null)
                    {
                        if (!_checkerConfiguration.DisableEnvironmentExit)
                        {
                            Error.ReportAndExit(ex.Message);
                        }

                        next = 0;
                        return false;
                    }
                    else
                    {
                        IsReplaying = false;
                        return SuffixStrategy.GetNextIntegerChoice(current, maxValue, out next);
                    }
                }

                next = nextStep.IntegerChoice.Value;
                ScheduledSteps++;
                return true;
            }

            return SuffixStrategy.GetNextIntegerChoice(current, maxValue, out next);
        }

        /// <inheritdoc/>
        public bool PrepareForNextIteration()
        {
            ScheduledSteps = 0;
            if (SuffixStrategy != null)
            {
                return SuffixStrategy.PrepareForNextIteration();
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public int GetScheduledSteps()
        {
            if (SuffixStrategy != null)
            {
                return ScheduledSteps + SuffixStrategy.GetScheduledSteps();
            }
            else
            {
                return ScheduledSteps;
            }
        }

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (SuffixStrategy != null)
            {
                return SuffixStrategy.HasReachedMaxSchedulingSteps();
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public bool IsFair()
        {
            if (SuffixStrategy != null)
            {
                return SuffixStrategy.IsFair();
            }
            else
            {
                return IsSchedulerFair;
            }
        }

        /// <inheritdoc/>
        public string GetDescription()
        {
            if (SuffixStrategy != null)
            {
                return "replay(" + SuffixStrategy.GetDescription() + ")";
            }
            else
            {
                return "replay";
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            ScheduledSteps = 0;
            SuffixStrategy?.Reset();
        }
    }
}