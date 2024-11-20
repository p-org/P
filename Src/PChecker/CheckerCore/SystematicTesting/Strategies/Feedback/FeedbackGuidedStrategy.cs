using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker.Generator;
using PChecker.Feedback;
using PChecker.Generator.Object;
using PChecker.SystematicTesting.Strategies.Probabilistic;
using AsyncOperation = PChecker.SystematicTesting.Operations.AsyncOperation;
using Debug = System.Diagnostics.Debug;

namespace PChecker.SystematicTesting.Strategies.Feedback;


internal class FeedbackGuidedStrategy : IFeedbackGuidedStrategy
{
    public record StrategyGenerator(ControlledRandom InputGenerator, IScheduler Scheduler);

    public record GeneratorRecord(int Priority, StrategyGenerator Generator, List<int> MinHash);

    internal StrategyGenerator Generator;

    private readonly int _maxScheduledSteps;

    protected int ScheduledSteps;

    private readonly HashSet<int> _visitedTimelines = new();

    private List<GeneratorRecord> _savedGenerators = new ();
    private int _pendingMutations = 0;
    private bool _shouldExploreNew = false;
    private HashSet<GeneratorRecord> _visitedGenerators = new HashSet<GeneratorRecord>();
    private GeneratorRecord? _currentParent = null;

    private System.Random _rnd = new System.Random();



    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackGuidedStrategy"/> class.
    /// </summary>
    public FeedbackGuidedStrategy(CheckerConfiguration checkerConfiguration, ControlledRandom inputGenerator, IScheduler scheduler)
    {
        _maxScheduledSteps = checkerConfiguration.MaxUnfairSchedulingSteps;
        Generator = new StrategyGenerator(inputGenerator, scheduler);
    }

    /// <inheritdoc/>
    public virtual bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        var result = Generator.Scheduler.GetNextOperation(current, ops, out next);
        ScheduledSteps++;
        return result;
    }

    /// <inheritdoc/>
    public bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
    {
        next = Generator.InputGenerator.Next(maxValue) == 0;
        ScheduledSteps++;
        return true;
    }

    /// <inheritdoc/>
    public bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
    {
        next = Generator.InputGenerator.Next(maxValue);
        ScheduledSteps++;
        return true;
    }

    /// <inheritdoc/>
    public virtual bool PrepareForNextIteration()
    {
        ScheduledSteps = 0;
        PrepareNextInput();
        return true;
    }

    /// <inheritdoc/>
    public int GetScheduledSteps()
    {
        return ScheduledSteps;
    }

    /// <inheritdoc/>
    public bool HasReachedMaxSchedulingSteps()
    {
        if (_maxScheduledSteps == 0)
        {
            return false;
        }

        return ScheduledSteps >= _maxScheduledSteps;
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
        ScheduledSteps = 0;
    }

    private int ComputeDiversity(int timeline, List<int> hash)
    {
        if (!_visitedTimelines.Add(timeline))
        {
            return 0;
        }

        if (_savedGenerators.Count == 0)
        {
            return 20;
        }

        var maxSim = int.MinValue;
        foreach (var record in _savedGenerators)
        {
            var timelineHash = record.MinHash;
            var similarity = 0;
            for (int i = 0; i < hash.Count; i++)
            {
                if (hash[i] == timelineHash[i])
                {
                    similarity += 1;
                }
            }

            maxSim = Math.Max(maxSim, similarity);
        }


        return (hash.Count - maxSim) + 20;
    }

    /// <summary>
    /// This method observes the results of previous run and prepare for the next run.
    /// </summary>
    public virtual void ObserveRunningResults(TimelineObserver timelineObserver)
    {
        var timelineHash = timelineObserver.GetTimelineHash();
        var timelineMinhash = timelineObserver.GetTimelineMinhash();

        int diversityScore = ComputeDiversity(timelineHash, timelineMinhash);

        if (diversityScore == 0)
        {
            return;
        }

        int priority = diversityScore;
        
        if (priority > 0)
        {
            var record = new GeneratorRecord(priority, Generator, timelineMinhash);
            if (_savedGenerators.Count == 0)
            {
                _savedGenerators.Add(record);
                return;
            }

            // Maybe use binary search to speed up in the future.
            var index = 0;
            while (index < _savedGenerators.Count && priority < _savedGenerators[index].Priority)
            {
                index += 1;
            }
            if (index >= _savedGenerators.Count)
            {
                _savedGenerators.Add(record);
            }
            else
            {
                _savedGenerators.Insert(index, record);
            }
        }
    }

    public int TotalSavedInputs()
    {
        return _savedGenerators.Count;
    }

    private void PrepareNextInput()
    {
        Generator.Scheduler.PrepareForNextIteration();
        if (_savedGenerators.Count == 0)
        {
            // Mutate current input if no input is saved.
            Generator = NewGenerator();
        }
        else
        {
            if (_currentParent == null && !_shouldExploreNew)
            {
                _currentParent = _savedGenerators.First();
                _visitedGenerators.Add(_currentParent);
                _pendingMutations = 50;
            }

            if (_pendingMutations == 0)
            {
                _shouldExploreNew = false;
                bool found = false;
                foreach (var generator in _savedGenerators)
                {
                    if (_visitedGenerators.Contains(generator)) continue;
                    _currentParent = generator;
                    _visitedGenerators.Add(generator);
                    _pendingMutations = generator.Priority;
                    found = true;
                    break;
                }

                if (!found)
                {
                    if (_rnd.NextDouble() < 0.5)
                    {
                        _visitedGenerators.Clear();
                        _currentParent = _savedGenerators.First();
                        _visitedGenerators.Add(_currentParent);
                        _pendingMutations = _currentParent.Priority;
                    }
                    else
                    {
                        _shouldExploreNew = true;
                        _currentParent = null;
                        _pendingMutations = 50;
                    }
                }
            }

            Generator = _shouldExploreNew ? NewGenerator() : MutateGenerator(_currentParent.Generator);
            _pendingMutations -= 1;
        }
    }


    protected virtual StrategyGenerator MutateGenerator(StrategyGenerator prev)
    {
        return new StrategyGenerator(prev.InputGenerator.Mutate(), prev.Scheduler.Mutate());
    }

    protected virtual StrategyGenerator NewGenerator()
    {
        return new StrategyGenerator(Generator.InputGenerator.New(), Generator.Scheduler.New());
    }

    public void DumpStats(TextWriter writer)
    {
        writer.WriteLine($"..... Total saved: {TotalSavedInputs()}, pending mutations: {_pendingMutations}, visited generators: {_visitedGenerators.Count}");
    }
}
