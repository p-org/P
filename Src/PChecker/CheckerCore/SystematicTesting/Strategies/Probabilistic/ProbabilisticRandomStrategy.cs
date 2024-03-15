// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Probabilistic
{
    /// <summary>
    /// A randomized scheduling strategy with increased probability
    /// to remain in the same scheduling choice.
    /// </summary>
    internal sealed class ProbabilisticRandomStrategy : RandomStrategy
    {
        /// <summary>
        /// Number of coin flips.
        /// </summary>
        private readonly int NumberOfCoinFlips;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProbabilisticRandomStrategy"/> class.
        /// </summary>
        public ProbabilisticRandomStrategy(int maxSteps, int numberOfCoinFlips, IRandomValueGenerator random)
            : base(maxSteps, random)
        {
            NumberOfCoinFlips = numberOfCoinFlips;
        }

        /// <inheritdoc/>
        public override bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
        {
            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
                next = null;
                return false;
            }

            ScheduledSteps++;

            if (enabledOperations.Count > 1)
            {
                if (!ShouldCurrentMachineChange() && current.Status is AsyncOperationStatus.Enabled)
                {
                    next = current;
                    return true;
                }
            }

            var idx = RandomValueGenerator.Next(enabledOperations.Count);
            next = enabledOperations[idx];

            return true;
        }

        /// <inheritdoc/>
        public override string GetDescription() =>
            $"probabilistic[seed '{RandomValueGenerator.Seed}', coin flips '{NumberOfCoinFlips}']";

        /// <summary>
        /// Flip the coin a specified number of times.
        /// </summary>
        private bool ShouldCurrentMachineChange()
        {
            for (var idx = 0; idx < NumberOfCoinFlips; idx++)
            {
                if (RandomValueGenerator.Next(2) == 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}