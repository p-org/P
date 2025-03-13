// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Probabilistic
{
    /// <summary>
    /// A probabilistic scheduling strategy that uses Q-learning.
    /// </summary>
    internal class QLearningStrategy : RandomStrategy
    {
        /// <summary>
        /// Map from program states to a map from next operations to their quality values.
        /// </summary>
        private readonly Dictionary<int, Dictionary<ulong, double>> OperationQTable;

        /// <summary>
        /// The path that is being executed during the current schedule. Each
        /// step of the execution is represented by an operation and a value
        /// represented the program state after the operation executed.
        /// </summary>
        private readonly LinkedList<(ulong op, AsyncOperationType type, int state)> ExecutionPath;

        /// <summary>
        /// Map from values representing program states to their transition
        /// frequency in the current execution path.
        /// </summary>
        private readonly Dictionary<int, ulong> TransitionFrequencies;

        /// <summary>
        /// The previously chosen operation.
        /// </summary>
        private ulong PreviousOperation;

        /// <summary>
        /// The value of the learning rate.
        /// </summary>
        private readonly double LearningRate;

        /// <summary>
        /// The value of the discount factor.
        /// </summary>
        private readonly double Gamma;

        /// <summary>
        /// The op value denoting a true boolean choice.
        /// </summary>
        private readonly ulong TrueChoiceOpValue;

        /// <summary>
        /// The op value denoting a false boolean choice.
        /// </summary>
        private readonly ulong FalseChoiceOpValue;

        /// <summary>
        /// The op value denoting the min integer choice.
        /// </summary>
        private readonly ulong MinIntegerChoiceOpValue;

        /// <summary>
        /// The failure injection reward.
        /// </summary>
        private readonly double FailureInjectionReward;

        /// <summary>
        /// The basic action reward.
        /// </summary>
        private readonly double BasicActionReward;

        /// <summary>
        /// The number of explored executions.
        /// </summary>
        private int Epochs;

        /// <summary>
        /// Initializes a new instance of the <see cref="QLearningStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public QLearningStrategy(int maxSteps, IRandomValueGenerator random)
            : base(maxSteps, random)
        {
            OperationQTable = new Dictionary<int, Dictionary<ulong, double>>();
            ExecutionPath = new LinkedList<(ulong, AsyncOperationType, int)>();
            TransitionFrequencies = new Dictionary<int, ulong>();
            PreviousOperation = 0;
            LearningRate = 0.3;
            Gamma = 0.7;
            TrueChoiceOpValue = ulong.MaxValue;
            FalseChoiceOpValue = ulong.MaxValue - 1;
            MinIntegerChoiceOpValue = ulong.MaxValue - 2;
            FailureInjectionReward = -1000;
            BasicActionReward = -1;
            Epochs = 0;
        }

        /// <inheritdoc/>
        public override bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
        {
            if (!ops.Any(op => op.Status is AsyncOperationStatus.Enabled))
            {
                // Fail fast if there are no enabled operations.
                next = null;
                return false;
            }

            int state = CaptureExecutionStep(current);
            InitializeOperationQValues(state, ops);

            next = GetNextOperationByPolicy(state, ops);
            PreviousOperation = next.Id;

            ScheduledSteps++;
            return true;
        }

        /// <inheritdoc/>
        public override bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            int state = CaptureExecutionStep(current);
            InitializeBooleanChoiceQValues(state);

            next = GetNextBooleanChoiceByPolicy(state);

            PreviousOperation = next ? TrueChoiceOpValue : FalseChoiceOpValue;
            ScheduledSteps++;
            return true;
        }

        /// <inheritdoc/>
        public override bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            int state = CaptureExecutionStep(current);
            InitializeIntegerChoiceQValues(state, maxValue);

            next = GetNextIntegerChoiceByPolicy(state, maxValue);

            PreviousOperation = MinIntegerChoiceOpValue - (ulong)next;
            ScheduledSteps++;
            return true;
        }

        /// <inheritdoc/>
        public override bool PrepareForNextIteration()
        {
            LearnQValues();
            ExecutionPath.Clear();
            PreviousOperation = 0;
            Epochs++;

            return base.PrepareForNextIteration();
        }

        /// <inheritdoc/>
        public override string GetDescription() => $"RL[seed '{RandomValueGenerator.Seed}']";

        /// <summary>
        /// Returns the next operation to schedule by drawing from the probability
        /// distribution over the specified state and enabled operations.
        /// </summary>
        private AsyncOperation GetNextOperationByPolicy(int state, IEnumerable<AsyncOperation> ops)
        {
            var opIds = new List<ulong>();
            var qValues = new List<double>();
            foreach (var pair in OperationQTable[state])
            {
                // Consider only the Q values of enabled operations.
                if (ops.Any(op => op.Id == pair.Key && op.Status == AsyncOperationStatus.Enabled))
                {
                    opIds.Add(pair.Key);
                    qValues.Add(pair.Value);
                }
            }

            int idx = ChooseQValueIndexFromDistribution(qValues);
            return ops.FirstOrDefault(op => op.Id == opIds[idx]);
        }

        /// <summary>
        /// Returns the next boolean choice by drawing from the probability
        /// distribution over the specified state and boolean choices.
        /// </summary>
        private bool GetNextBooleanChoiceByPolicy(int state)
        {
            double trueQValue = OperationQTable[state][TrueChoiceOpValue];
            double falseQValue = OperationQTable[state][FalseChoiceOpValue];

            var qValues = new List<double>(2)
            {
                trueQValue,
                falseQValue
            };

            int idx = ChooseQValueIndexFromDistribution(qValues);
            return idx == 0 ? true : false;
        }

        /// <summary>
        /// Returns the next integer choice by drawing from the probability
        /// distribution over the specified state and integer choices.
        /// </summary>
        private int GetNextIntegerChoiceByPolicy(int state, int maxValue)
        {
            var qValues = new List<double>(maxValue);
            for (ulong i = 0; i < (ulong)maxValue; i++)
            {
                qValues.Add(OperationQTable[state][MinIntegerChoiceOpValue - i]);
            }

            return ChooseQValueIndexFromDistribution(qValues);
        }

        /// <summary>
        /// Returns an index of a Q value by drawing from the probability distribution
        /// over the specified Q values.
        /// </summary>
        private int ChooseQValueIndexFromDistribution(List<double> qValues)
        {
            double sum = 0;
            for (int i = 0; i < qValues.Count; i++)
            {
                qValues[i] = Math.Exp(qValues[i]);
                sum += qValues[i];
            }

            for (int i = 0; i < qValues.Count; i++)
            {
                qValues[i] /= sum;
            }

            sum = 0;

            // First, change the shape of the distribution probability array to be cumulative.
            // For example, instead of [0.1, 0.2, 0.3, 0.4], we get [0.1, 0.3, 0.6, 1.0].
            var cumulative = qValues.Select(c =>
            {
                var result = c + sum;
                sum += c;
                return result;
            }).ToList();

            // Generate a random double value between 0.0 to 1.0.
            var rvalue = RandomValueGenerator.NextDouble();

            // Find the first index in the cumulative array that is greater
            // or equal than the generated random value.
            var idx = cumulative.BinarySearch(rvalue);

            if (idx < 0)
            {
                // If an exact match is not found, List.BinarySearch will return the index
                // of the first items greater than the passed value, but in a specific form
                // (negative) we need to apply ~ to this negative value to get real index.
                idx = ~idx;
            }

            if (idx > cumulative.Count - 1)
            {
                // Very rare case when probabilities do not sum to 1 because of
                // double precision issues (so sum is 0.999943 and so on).
                idx = cumulative.Count - 1;
            }

            return idx;
        }

        /// <summary>
        /// Captures metadata related to the current execution step, and returns
        /// a value representing the current program state.
        /// </summary>
        private int CaptureExecutionStep(AsyncOperation current)
        {
            int state = current.HashedProgramState;

            // Update the execution path with the current state.
            ExecutionPath.AddLast((PreviousOperation, current.Type, state));

            if (!TransitionFrequencies.ContainsKey(state))
            {
                TransitionFrequencies.Add(state, 0);
            }

            // Increment the state transition frequency.
            TransitionFrequencies[state]++;

            return state;
        }

        /// <summary>
        /// Initializes the Q values of all enabled operations that can be chosen
        /// at the specified state that have not been previously encountered.
        /// </summary>
        private void InitializeOperationQValues(int state, IEnumerable<AsyncOperation> ops)
        {
            if (!OperationQTable.TryGetValue(state, out Dictionary<ulong, double> qValues))
            {
                qValues = new Dictionary<ulong, double>();
                OperationQTable.Add(state, qValues);
            }

            foreach (var op in ops)
            {
                // Assign the same initial probability for all new enabled operations.
                if (op.Status == AsyncOperationStatus.Enabled && !qValues.ContainsKey(op.Id))
                {
                    qValues.Add(op.Id, 0);
                }
            }
        }

        /// <summary>
        /// Initializes the Q values of all boolean choices that can be chosen
        /// at the specified state that have not been previously encountered.
        /// </summary>
        private void InitializeBooleanChoiceQValues(int state)
        {
            if (!OperationQTable.TryGetValue(state, out Dictionary<ulong, double> qValues))
            {
                qValues = new Dictionary<ulong, double>();
                OperationQTable.Add(state, qValues);
            }

            if (!qValues.ContainsKey(TrueChoiceOpValue))
            {
                qValues.Add(TrueChoiceOpValue, 0);
            }

            if (!qValues.ContainsKey(FalseChoiceOpValue))
            {
                qValues.Add(FalseChoiceOpValue, 0);
            }
        }

        /// <summary>
        /// Initializes the Q values of all integer choices that can be chosen
        /// at the specified state that have not been previously encountered.
        /// </summary>
        private void InitializeIntegerChoiceQValues(int state, int maxValue)
        {
            if (!OperationQTable.TryGetValue(state, out Dictionary<ulong, double> qValues))
            {
                qValues = new Dictionary<ulong, double>();
                OperationQTable.Add(state, qValues);
            }

            for (ulong i = 0; i < (ulong)maxValue; i++)
            {
                ulong opValue = MinIntegerChoiceOpValue - i;
                if (!qValues.ContainsKey(opValue))
                {
                    qValues.Add(opValue, 0);
                }
            }
        }

        /// <summary>
        /// Learn Q values using data from the current execution.
        /// </summary>
        private void LearnQValues()
        {
            var pathBuilder = new StringBuilder();

            int idx = 0;
            var node = ExecutionPath.First;
            while (node != null && node.Next != null)
            {
                pathBuilder.Append($"{node.Value.op},");

                var (_, _, state) = node.Value;
                var (nextOp, nextType, nextState) = node.Next.Value;

                // Compute the max Q value.
                double maxQ = double.MinValue;
                foreach (var nextOpQValuePair in OperationQTable[nextState])
                {
                    if (nextOpQValuePair.Value > maxQ)
                    {
                        maxQ = nextOpQValuePair.Value;
                    }
                }

                // Compute the reward. Program states that are visited with higher frequency result into lesser rewards.
                var freq = TransitionFrequencies[nextState];
                double reward = (nextType == AsyncOperationType.InjectFailure ?
                    FailureInjectionReward : BasicActionReward) * freq;
                if (reward > 0)
                {
                    // The reward has underflowed.
                    reward = double.MinValue;
                }

                // Get the operations that are available from the current execution step.
                var currOpQValues = OperationQTable[state];
                if (!currOpQValues.ContainsKey(nextOp))
                {
                    currOpQValues.Add(nextOp, 0);
                }

                // Update the Q value of the next operation.
                // Q = [(1-a) * Q]  +  [a * (rt + (g * maxQ))]
                currOpQValues[nextOp] = ((1 - LearningRate) * currOpQValues[nextOp]) +
                                        (LearningRate * (reward + (Gamma * maxQ)));

                node = node.Next;
                idx++;
            }
        }
    }
}