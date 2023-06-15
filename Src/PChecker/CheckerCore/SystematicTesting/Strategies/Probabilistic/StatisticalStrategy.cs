using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Probabilistic
{
    /// <summary>
    /// A simple (but effective) randomized scheduling strategy.
    /// </summary>
    internal class StatisticalStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        protected int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        protected int ScheduledSteps;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatisticalStrategy"/> class.
        /// </summary>
        public StatisticalStrategy(int maxSteps, IRandomValueGenerator random)
        {
            RandomValueGenerator = random;
            MaxScheduledSteps = maxSteps;
            ScheduledSteps = 0;
        }
        
        /// <inheritdoc/>
        public virtual bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
        {
            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
                next = null;
                return false;
            }

            var idx = RandomValueGenerator.Next(enabledOperations.Count);
            next = enabledOperations[idx];

            ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public virtual bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            next = false;
            if (RandomValueGenerator.Next(maxValue) == 0)
            {
                next = true;
            }

            ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public virtual bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            next = RandomValueGenerator.Next(maxValue);
            ScheduledSteps++;
            return true;
        }
        
        /// <inheritdoc/>
        public bool GetSampleFromDistribution(string dist, out int sample)
        {
            // dist should be of the following form:
            //      - Uniform(lowerBound, upperBound)
            //      - Normal(lowerBound, upperBound)
            sample = RandomValueGenerator.Next(1, 10);
            if (dist.Contains("Uniform"))
            {
                var bounds = dist.Replace(" ", "").Replace("(", "").Replace(")", "").Substring(7).Split(",");
                if (int.TryParse(bounds[0], out var lowerBound) && int.TryParse(bounds[1], out var upperBound))
                {
                    sample = RandomValueGenerator.Next(lowerBound, upperBound);
                    return true;
                }
            } else if (dist.Contains("Normal"))
            {
                var bounds = dist.Replace(" ", "").Replace("(", "").Replace(")", "").Substring(6).Split(",");
                if (int.TryParse(bounds[0], out var mean) && int.TryParse(bounds[1], out var variance))
                {
                    double u1 = 1.0 - RandomValueGenerator.NextDouble();
                    double u2 = 1.0 - RandomValueGenerator.NextDouble();
                    double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                    double randNormal = mean + Math.Sqrt(variance) * randStdNormal; //random normal(mean,stdDev^2)
                    sample = (int)randNormal;
                    return true;
                }   
            }

            return false;
        }

        /// <inheritdoc/>
        public virtual bool PrepareForNextIteration()
        {
            ScheduledSteps = 0;
            return true;
        }

        /// <inheritdoc/>
        public int GetScheduledSteps() => ScheduledSteps;

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (MaxScheduledSteps == 0)
            {
                return false;
            }

            return ScheduledSteps >= MaxScheduledSteps;
        }

        /// <inheritdoc/>
        public bool IsFair() => true;

        /// <inheritdoc/>
        public virtual string GetDescription() => $"random[seed '{RandomValueGenerator.Seed}']";

        /// <inheritdoc/>
        public virtual void Reset()
        {
            ScheduledSteps = 0;
        }
    }
}
