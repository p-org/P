// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PChecker.SystematicTesting.Traces
{
    /// <summary>
    /// Class implementing a program schedule step.
    /// </summary>
    internal readonly struct ScheduleStep
    {
        /// <summary>
        /// The unique index of this schedule step.
        /// </summary>
        internal readonly int Index;

        /// <summary>
        /// The type of this schedule step.
        /// </summary>
        internal readonly ScheduleStepType Type;

        /// <summary>
        /// The id of the scheduled operation. Only relevant if this is
        /// a regular schedule step.
        /// </summary>
        internal readonly ulong ScheduledOperationId;

        /// <summary>
        /// The non-deterministic boolean choice value. Only relevant if
        /// this is a choice schedule step.
        /// </summary>
        internal readonly bool? BooleanChoice;

        /// <summary>
        /// The non-deterministic integer choice value. Only relevant if
        /// this is a choice schedule step.
        /// </summary>
        internal readonly int? IntegerChoice;


        private ScheduleStep(int index, ScheduleStepType type, ulong scheduledOperationId, bool? booleanChoice,
            int? integerChoice)
        {
            Index = index;
            Type = type;
            ScheduledOperationId = scheduledOperationId;
            BooleanChoice = booleanChoice;
            IntegerChoice = integerChoice;
        }

        /// <summary>
        /// Creates a schedule step with a scheduling choice.
        /// </summary>
        internal static ScheduleStep CreateSchedulingChoice(int index, ulong scheduledStateMachineId) =>
            new ScheduleStep(index, ScheduleStepType.SchedulingChoice, scheduledStateMachineId, null, null);

        /// <summary>
        /// Creates a nondeterministic boolean choice schedule step.
        /// </summary>
        internal static ScheduleStep CreateNondeterministicBooleanChoice(int index, bool choice) =>
            new ScheduleStep(index, ScheduleStepType.NondeterministicChoice, 0, choice, null);

        /// <summary>
        /// Creates a nondeterministic integer choice schedule step.
        /// </summary>
        internal static ScheduleStep CreateNondeterministicIntegerChoice(int index, int choice) =>
            new ScheduleStep(index, ScheduleStepType.NondeterministicChoice, 0, null, choice);

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