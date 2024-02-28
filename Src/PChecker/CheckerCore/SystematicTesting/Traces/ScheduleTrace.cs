// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;

namespace PChecker.SystematicTesting.Traces
{
    /// <summary>
    /// Class implementing a program schedule trace. A trace is a series
    /// of transitions from some initial state to some end state.
    /// </summary>
    internal sealed class ScheduleTrace : IEnumerable, IEnumerable<ScheduleStep>
    {
        /// <summary>
        /// The steps of the schedule trace.
        /// </summary>
        private readonly List<ScheduleStep> Steps;

        /// <summary>
        /// The number of steps in the schedule trace.
        /// </summary>
        internal int Count
        {
            get { return Steps.Count; }
        }

        /// <summary>
        /// Index for the schedule trace.
        /// </summary>
        internal ScheduleStep this[int index]
        {
            get { return Steps[index]; }
            set { Steps[index] = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleTrace"/> class.
        /// </summary>
        internal ScheduleTrace()
        {
            Steps = new List<ScheduleStep>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleTrace"/> class.
        /// </summary>
        internal ScheduleTrace(string[] traceDump)
        {
            Steps = new List<ScheduleStep>();

            foreach (var step in traceDump)
            {
                if (step.StartsWith("--") || step.Length == 0 || step.StartsWith("//"))
                {
                    continue;
                }
                else if (step.Equals("True"))
                {
                    AddNondeterministicBooleanChoice(true);
                }
                else if (step.Equals("False"))
                {
                    AddNondeterministicBooleanChoice(false);
                }
                else if (int.TryParse(step, out var intChoice))
                {
                    AddNondeterministicIntegerChoice(intChoice);
                }
                else
                {
                    var id = step.TrimStart('(').TrimEnd(')');
                    AddSchedulingChoice(ulong.Parse(id));
                }
            }
        }

        /// <summary>
        /// Adds a scheduling choice.
        /// </summary>
        internal void AddSchedulingChoice(ulong scheduledActorId)
        {
            var scheduleStep = ScheduleStep.CreateSchedulingChoice(Count, scheduledActorId);
            Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic boolean choice.
        /// </summary>
        internal void AddNondeterministicBooleanChoice(bool choice)
        {
            var scheduleStep = ScheduleStep.CreateNondeterministicBooleanChoice(
                Count, choice);
            Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic integer choice.
        /// </summary>
        internal void AddNondeterministicIntegerChoice(int choice)
        {
            var scheduleStep = ScheduleStep.CreateNondeterministicIntegerChoice(
                Count, choice);
            Push(scheduleStep);
        }

        /// <summary>
        /// Returns the latest schedule step and removes
        /// it from the trace.
        /// </summary>
        internal ScheduleStep Pop()
        {
            if (Count > 0)
            {
                Steps[Count - 1].Next = null;
            }

            var step = Steps[Count - 1];
            Steps.RemoveAt(Count - 1);

            return step;
        }

        /// <summary>
        /// Returns the latest schedule step without removing it.
        /// </summary>
        internal ScheduleStep Peek()
        {
            ScheduleStep step = null;

            if (Steps.Count > 0)
            {
                step = Steps[Count - 1];
            }

            return step;
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Steps.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        IEnumerator<ScheduleStep> IEnumerable<ScheduleStep>.GetEnumerator()
        {
            return Steps.GetEnumerator();
        }

        /// <summary>
        /// Pushes a new step to the trace.
        /// </summary>
        private void Push(ScheduleStep step)
        {
            if (Count > 0)
            {
                Steps[Count - 1].Next = step;
                step.Previous = Steps[Count - 1];
            }

            Steps.Add(step);
        }
    }
}