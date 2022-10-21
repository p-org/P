// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Coyote.SystematicTesting
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
            get { return this.Steps.Count; }
        }

        /// <summary>
        /// Index for the schedule trace.
        /// </summary>
        internal ScheduleStep this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleTrace"/> class.
        /// </summary>
        internal ScheduleTrace()
        {
            this.Steps = new List<ScheduleStep>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleTrace"/> class.
        /// </summary>
        internal ScheduleTrace(string[] traceDump)
        {
            this.Steps = new List<ScheduleStep>();

            foreach (var step in traceDump)
            {
                if (step.StartsWith("--") || step.Length == 0)
                {
                    continue;
                }
                else if (step.Equals("True"))
                {
                    this.AddNondeterministicBooleanChoice(true);
                }
                else if (step.Equals("False"))
                {
                    this.AddNondeterministicBooleanChoice(false);
                }
                else if (int.TryParse(step, out int intChoice))
                {
                    this.AddNondeterministicIntegerChoice(intChoice);
                }
                else
                {
                    string id = step.TrimStart('(').TrimEnd(')');
                    this.AddSchedulingChoice(ulong.Parse(id));
                }
            }
        }

        /// <summary>
        /// Adds a scheduling choice.
        /// </summary>
        internal void AddSchedulingChoice(ulong scheduledActorId)
        {
            var scheduleStep = ScheduleStep.CreateSchedulingChoice(this.Count, scheduledActorId);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic boolean choice.
        /// </summary>
        internal void AddNondeterministicBooleanChoice(bool choice)
        {
            var scheduleStep = ScheduleStep.CreateNondeterministicBooleanChoice(
                this.Count, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic integer choice.
        /// </summary>
        internal void AddNondeterministicIntegerChoice(int choice)
        {
            var scheduleStep = ScheduleStep.CreateNondeterministicIntegerChoice(
                this.Count, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Returns the latest schedule step and removes
        /// it from the trace.
        /// </summary>
        internal ScheduleStep Pop()
        {
            if (this.Count > 0)
            {
                this.Steps[this.Count - 1].Next = null;
            }

            var step = this.Steps[this.Count - 1];
            this.Steps.RemoveAt(this.Count - 1);

            return step;
        }

        /// <summary>
        /// Returns the latest schedule step without removing it.
        /// </summary>
        internal ScheduleStep Peek()
        {
            ScheduleStep step = null;

            if (this.Steps.Count > 0)
            {
                step = this.Steps[this.Count - 1];
            }

            return step;
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        IEnumerator<ScheduleStep> IEnumerable<ScheduleStep>.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Pushes a new step to the trace.
        /// </summary>
        private void Push(ScheduleStep step)
        {
            if (this.Count > 0)
            {
                this.Steps[this.Count - 1].Next = step;
                step.Previous = this.Steps[this.Count - 1];
            }

            this.Steps.Add(step);
        }
    }
}
