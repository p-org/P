// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Feedback;
using PChecker.IO.Debugging;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Probabilistic
{
    /// <summary>
    /// A priority-based probabilistic scheduling strategy.
    /// </summary>
    /// <remarks>
    /// This strategy is described in the following paper:
    /// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/asplos277-pct.pdf
    /// </remarks>
    internal class PrioritizedSchedulingStrategy: ISchedulingStrategy
    {

        /// <summary>
        /// Random value generator.
        /// </summary>
        private readonly IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        private readonly int MaxScheduledSteps;

        private int _scheduledSteps;

        internal readonly PrioritizedScheduler Scheduler;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrioritizedSchedulingStrategy"/> class.
        /// </summary>
        public PrioritizedSchedulingStrategy(int maxSteps, IRandomValueGenerator random, PrioritizedScheduler scheduler)
        {
            RandomValueGenerator = random;
            MaxScheduledSteps = maxSteps;
            Scheduler = scheduler;
        }

        /// <inheritdoc/>
        public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
        {
            _scheduledSteps++;
            return Scheduler.GetNextOperation(current, ops, out next);
        }

        /// <inheritdoc/>
        public bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            next = false;
            if (RandomValueGenerator.Next(maxValue) == 0)
            {
                next = true;
            }

            _scheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            next = RandomValueGenerator.Next(maxValue);
            _scheduledSteps++;
            return true;
        }

        /// <inheritdoc/>
        public int GetScheduledSteps() => _scheduledSteps;

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (MaxScheduledSteps == 0)
            {
                return false;
            }

            return _scheduledSteps >= MaxScheduledSteps;
        }

        /// <inheritdoc/>
        public bool IsFair() => false;

        /// <inheritdoc/>
        public string GetDescription()
        {
            var text = $"pct[seed '" + RandomValueGenerator.Seed + "']";
            return text;
        }

        public bool PrepareForNextIteration() {
            _scheduledSteps = 0;
            return Scheduler.PrepareForNextIteration();
        }


        /// <inheritdoc/>
        public void Reset()
        {
            _scheduledSteps = 0;
            Scheduler.Reset();
        }
    }
}
