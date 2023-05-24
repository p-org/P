using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Actors;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Feedback;

record StrategyInputGenerator(IRandomValueGenerator InputGenerator, IRandomValueGenerator ScheduleGenerator);
internal class FeedbackGuidedStrategy : ISchedulingStrategy
{
    private StrategyInputGenerator _generator;

    private readonly int _maxScheduledSteps;

    private int _scheduledSteps;

    private readonly CheckerConfiguration _checkerConfiguration;

    private readonly HashSet<int> _visitedStates = new();

    private readonly LinkedList<StrategyInputGenerator> _savedGenerators = new();

    private readonly int _maxMutations = 50;

    private int _numMutations = 0;

    private LinkedListNode<StrategyInputGenerator>? _currentNode = null;


    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackGuidedStrategy"/> class.
    /// </summary>
    public FeedbackGuidedStrategy(CheckerConfiguration checkerConfiguration)
    {
        _maxScheduledSteps = checkerConfiguration.MaxFairSchedulingSteps;
        _checkerConfiguration = checkerConfiguration;
        _generator = new StrategyInputGenerator(new RandomValueGenerator(_checkerConfiguration),
            new RandomValueGenerator(_checkerConfiguration));
    }

    /// <inheritdoc/>
    public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
        if (enabledOperations.Count == 0)
        {
            next = null;
            return false;
        }

        var idx = _generator.ScheduleGenerator.Next(enabledOperations.Count);
        next = enabledOperations[idx];

        _scheduledSteps++;
        return true;
    }

    /// <inheritdoc/>
    public bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
    {
        next = _generator.InputGenerator.Next(maxValue) == 0;

        _scheduledSteps++;

        return true;
    }

    /// <inheritdoc/>
    public bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
    {
        next = _generator.InputGenerator.Next(maxValue);
        _scheduledSteps++;
        return true;
    }

    /// <inheritdoc/>
    public bool PrepareForNextIteration()
    {
        // Noop
        _scheduledSteps = 0;
        return true;
    }

    /// <inheritdoc/>
    public int GetScheduledSteps()
    {
        return _scheduledSteps;
    }

    /// <inheritdoc/>
    public bool HasReachedMaxSchedulingSteps()
    {
        if (_maxScheduledSteps == 0)
        {
            return false;
        }

        return _scheduledSteps >= _maxScheduledSteps;
    }

    /// <inheritdoc/>
    public bool IsFair()
    {
        return true;
    }

    /// <inheritdoc/>
    public string GetDescription()
    {
        return "feedback";
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _scheduledSteps = 0;
    }

    /// <summary>
    /// This method observes the results of previous run and prepare for the next run.
    /// </summary>
    /// <param name="runtime">The ControlledRuntime of previous run.</param>
    public void ObserveRunningResults(ControlledRuntime runtime)
    {
        // TODO: implement real feedback.
        int stateHash = runtime.GetCoverageInfo().EventInfo.GetHashCode();
        if (!_visitedStates.Contains(stateHash))
        {
            _savedGenerators.AddLast(_generator);
        }
        PrepareNextInput();
    }

    private void PrepareNextInput()
    {
        if (_savedGenerators.Count == 0)
        {
            // Create a new input if no input is saved.
            _generator = new StrategyInputGenerator(new RandomValueGenerator(_checkerConfiguration),
                new RandomValueGenerator(_checkerConfiguration));
            return;
        }
        if (_numMutations == _maxMutations)
        {
            _currentNode = _currentNode?.Next;
        }
        _currentNode ??= _savedGenerators.First;
        _generator = MutateGenerator(_currentNode!.Value);
    }

    private StrategyInputGenerator MutateGenerator(StrategyInputGenerator prev)
    {
        // TODO: implement real mutation strategies.
        return new StrategyInputGenerator(new RandomValueGenerator(_checkerConfiguration),
            new RandomValueGenerator(_checkerConfiguration));
    }
}