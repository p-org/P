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

        private ulong? Time;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatisticalStrategy"/> class.
        /// </summary>
        public StatisticalStrategy(int maxSteps, IRandomValueGenerator random)
        {
            RandomValueGenerator = random;
            MaxScheduledSteps = maxSteps;
            ScheduledSteps = 0;
            Time = 0;
        }
        
        /// <inheritdoc/>
        public virtual bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
        {
            // Associate each event with a delay value. For now, set static values. Also for now, only send operations
            // should have this delay value set. Keep a global time here. Exhaust all non-send operations. Then get the
            // send with min delay, increment time by its delay value, and schedule it.
            // Questions for next steps:
            //      - How do we find the actual delays given in the program? For this, using a send operation, we should
            //        be able to access its payload value or a specific field defined for delay.
            //      - Also, although we delay the send operations, we should actually be delaying the enqueuing of the
            //        message. But actually delaying a message requires us to modify parts of the code outside of the
            //        strategy, which we do not want to do. This is tricky. Think about it!
            foreach (var op in ops)
            {
                op.Delay ??= (ulong)(op.Type == AsyncOperationType.Send ? RandomValueGenerator.Next(1, 10) : 0);
                op.Timestamp ??= Time + op.Delay;
                // We need the following timestamp update because some operations are enabled after they are completed
                // so their timestamps should be updated.
                if (op.Timestamp < Time)
                {
                    op.Timestamp = Time;
                }
            }
            var enabledCurrentOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled && op.Timestamp == Time).ToList();
            var enabledFutureOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled && op.Timestamp > Time).ToList();
            if (enabledCurrentOperations.Count == 0)
            {
                if (enabledFutureOperations.Count == 0)
                {
                    next = null;
                    return false;
                }

                Time = enabledFutureOperations.Min(op => op.Timestamp);
                var temp = enabledFutureOperations.Where(op => op.Timestamp == Time).ToList();

                var idx = RandomValueGenerator.Next(temp.Count);
                next = temp[idx];

                ScheduledSteps++;

                return true;
            }
            else
            {
                var idx = RandomValueGenerator.Next(enabledCurrentOperations.Count);
                next = enabledCurrentOperations[idx];

                ScheduledSteps++;

                return true;
            }
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
