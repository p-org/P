// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.SystematicTesting.Strategies
{
    /// <summary>
    /// Abstract strategy for detecting liveness property violations. It
    /// contains a nested <see cref="ISchedulingStrategy"/> that is used
    /// for scheduling decisions.
    /// </summary>
    internal abstract class LivenessCheckingStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        protected List<Monitor> Monitors;

        /// <summary>
        /// Strategy used for scheduling decisions.
        /// </summary>
        protected ISchedulingStrategy SchedulingStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="LivenessCheckingStrategy"/> class.
        /// </summary>
        internal LivenessCheckingStrategy(Configuration configuration, List<Monitor> monitors, ISchedulingStrategy strategy)
        {
            this.Configuration = configuration;
            this.Monitors = monitors;
            this.SchedulingStrategy = strategy;
        }

        /// <inheritdoc/>
        public abstract bool GetNextOperation(IAsyncOperation current, IEnumerable<IAsyncOperation> ops, out IAsyncOperation next);

        /// <inheritdoc/>
        public abstract bool GetNextBooleanChoice(IAsyncOperation current, int maxValue, out bool next);

        /// <inheritdoc/>
        public abstract bool GetNextIntegerChoice(IAsyncOperation current, int maxValue, out int next);

        /// <inheritdoc/>
        public virtual bool PrepareForNextIteration()
        {
            return this.SchedulingStrategy.PrepareForNextIteration();
        }

        /// <inheritdoc/>
        public virtual int GetScheduledSteps()
        {
            return this.SchedulingStrategy.GetScheduledSteps();
        }

        /// <inheritdoc/>
        public virtual bool HasReachedMaxSchedulingSteps()
        {
            return this.SchedulingStrategy.HasReachedMaxSchedulingSteps();
        }

        /// <inheritdoc/>
        public virtual bool IsFair()
        {
            return this.SchedulingStrategy.IsFair();
        }

        /// <inheritdoc/>
        public virtual string GetDescription()
        {
            return this.SchedulingStrategy.GetDescription();
        }

        /// <inheritdoc/>
        public virtual void Reset()
        {
            this.SchedulingStrategy.Reset();
        }
    }
}
