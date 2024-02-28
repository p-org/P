// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PChecker.SystematicTesting.Traces
{
    /// <summary>
    /// Class implementing a program schedule step.
    /// </summary>
    internal sealed class ScheduleStep
    {
        /// <summary>
        /// The unique index of this schedule step.
        /// </summary>
        internal int Index;

        /// <summary>
        /// The type of this schedule step.
        /// </summary>
        internal ScheduleStepType Type { get; private set; }

        /// <summary>
        /// The id of the scheduled operation. Only relevant if this is
        /// a regular schedule step.
        /// </summary>
        internal ulong ScheduledOperationId;

        /// <summary>
        /// The non-deterministic boolean choice value. Only relevant if
        /// this is a choice schedule step.
        /// </summary>
        internal bool? BooleanChoice;

        /// <summary>
        /// The non-deterministic integer choice value. Only relevant if
        /// this is a choice schedule step.
        /// </summary>
        internal int? IntegerChoice;

        /// <summary>
        /// Previous schedule step.
        /// </summary>
        internal ScheduleStep Previous;

        /// <summary>
        /// Next schedule step.
        /// </summary>
        internal ScheduleStep Next;

        /// <summary>
        /// Creates a schedule step.
        /// </summary>
        internal static ScheduleStep CreateSchedulingChoice(int index, ulong scheduledActorId)
        {
            var scheduleStep = new ScheduleStep();

            scheduleStep.Index = index;
            scheduleStep.Type = ScheduleStepType.SchedulingChoice;

            scheduleStep.ScheduledOperationId = scheduledActorId;

            scheduleStep.BooleanChoice = null;
            scheduleStep.IntegerChoice = null;

            scheduleStep.Previous = null;
            scheduleStep.Next = null;

            return scheduleStep;
        }

        /// <summary>
        /// Creates a nondeterministic boolean choice schedule step.
        /// </summary>
        internal static ScheduleStep CreateNondeterministicBooleanChoice(int index, bool choice)
        {
            var scheduleStep = new ScheduleStep();

            scheduleStep.Index = index;
            scheduleStep.Type = ScheduleStepType.NondeterministicChoice;

            scheduleStep.BooleanChoice = choice;
            scheduleStep.IntegerChoice = null;

            scheduleStep.Previous = null;
            scheduleStep.Next = null;

            return scheduleStep;
        }

        /// <summary>
        /// Creates a nondeterministic integer choice schedule step.
        /// </summary>
        internal static ScheduleStep CreateNondeterministicIntegerChoice(int index, int choice)
        {
            var scheduleStep = new ScheduleStep();

            scheduleStep.Index = index;
            scheduleStep.Type = ScheduleStepType.NondeterministicChoice;

            scheduleStep.BooleanChoice = null;
            scheduleStep.IntegerChoice = choice;

            scheduleStep.Previous = null;
            scheduleStep.Next = null;

            return scheduleStep;
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is ScheduleStep step)
            {
                return Index == step.Index;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => Index.GetHashCode();
    }
}