// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Special
{
    /// <summary>
    /// This strategy combines two given strategies, using them to schedule
    /// the prefix and suffix of an execution.
    /// </summary>
    internal sealed class ComboStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The prefix strategy.
        /// </summary>
        private readonly ISchedulingStrategy PrefixStrategy;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private readonly ISchedulingStrategy SuffixStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboStrategy"/> class.
        /// </summary>
        public ComboStrategy(ISchedulingStrategy prefixStrategy, ISchedulingStrategy suffixStrategy)
        {
            PrefixStrategy = prefixStrategy;
            SuffixStrategy = suffixStrategy;
        }

        /// <inheritdoc/>
        public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
        {
            if (PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return SuffixStrategy.GetNextOperation(current, ops, out next);
            }
            else
            {
                return PrefixStrategy.GetNextOperation(current, ops, out next);
            }
        }

        /// <inheritdoc/>
        public bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            if (PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return SuffixStrategy.GetNextBooleanChoice(current, maxValue, out next);
            }
            else
            {
                return PrefixStrategy.GetNextBooleanChoice(current, maxValue, out next);
            }
        }

        /// <inheritdoc/>
        public bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            if (PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return SuffixStrategy.GetNextIntegerChoice(current, maxValue, out next);
            }
            else
            {
                return PrefixStrategy.GetNextIntegerChoice(current, maxValue, out next);
            }
        }

        /// <inheritdoc/>
        public bool PrepareForNextIteration()
        {
            var doNext = PrefixStrategy.PrepareForNextIteration();
            doNext |= SuffixStrategy.PrepareForNextIteration();
            return doNext;
        }

        /// <inheritdoc/>
        public int GetScheduledSteps()
        {
            if (PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return SuffixStrategy.GetScheduledSteps() + PrefixStrategy.GetScheduledSteps();
            }
            else
            {
                return PrefixStrategy.GetScheduledSteps();
            }
        }

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps() => SuffixStrategy.HasReachedMaxSchedulingSteps();

        /// <inheritdoc/>
        public bool IsFair() => SuffixStrategy.IsFair();

        /// <inheritdoc/>
        public string GetDescription() =>
            string.Format("combo[{0},{1}]", PrefixStrategy.GetDescription(), SuffixStrategy.GetDescription());

        /// <inheritdoc/>
        public void Reset()
        {
            PrefixStrategy.Reset();
            SuffixStrategy.Reset();
        }
    }
}