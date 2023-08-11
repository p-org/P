using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker.Generator;
using PChecker.Feedback;
using AsyncOperation = PChecker.SystematicTesting.Operations.AsyncOperation;
using Debug = System.Diagnostics.Debug;

namespace PChecker.SystematicTesting.Strategies.Feedback;


internal class FeedbackGuidedStrategy<TInput, TSchedule> : IFeedbackGuidedStrategy
    where TInput: IInputGenerator<TInput>
    where TSchedule: IScheduleGenerator<TSchedule>
{
    public record StrategyGenerator(TInput InputGenerator, TSchedule ScheduleGenerator);

    public record GeneratorRecord(int Priority, StrategyGenerator Generator, List<int> Hash, int Coverage)
    {
        public int Priority { set; get; } = Priority;
    }

    protected StrategyGenerator Generator;

    private readonly int _maxScheduledSteps;

    protected int ScheduledSteps;
    private int _visitedStates = 0;

    private readonly HashSet<int> _visitedTimelines = new();

    protected MaxHeap<GeneratorRecord> SavedGenerators = new(Comparer<GeneratorRecord>.Create((l, r) =>
    {
        return l.Priority - r.Priority;
    }));

    private bool _matched = false;
    private readonly bool _savePartialMatch;
    private readonly bool _discardLowerCoverage;
    private readonly bool _diversityBasedPriority;
    private int _pendingMutations = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackGuidedStrategy"/> class.
    /// </summary>
    public FeedbackGuidedStrategy(CheckerConfiguration checkerConfiguration, TInput input, TSchedule schedule)
    {
        if (schedule is PctScheduleGenerator)
        {
            _maxScheduledSteps = checkerConfiguration.MaxUnfairSchedulingSteps;
        }
        else
        {
            _maxScheduledSteps = checkerConfiguration.MaxFairSchedulingSteps;
        }
        Generator = new StrategyGenerator(input, schedule);
        _savePartialMatch = checkerConfiguration.SavePartialMatch;
        _discardLowerCoverage = checkerConfiguration.DiscardLowerCoverage;
        _diversityBasedPriority = checkerConfiguration.DiversityBasedPriority;
    }

    /// <inheritdoc/>
    public virtual bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        // var enabledOperations = _nfa != null? _nfa.FindHighPriorityOperations(ops) : ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
        next = Generator.ScheduleGenerator.NextRandomOperation(ops.ToList(), current);
        ScheduledSteps++;
        return next != null;
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

        if (SavedGenerators.Elements.Count == 0 || !_diversityBasedPriority)
        {
            return 20;
        }

        var maxSim = int.MinValue;
        foreach (var record in SavedGenerators.Elements)
        {
            var timelineHash = record.Hash;
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


        return (hash.Count - maxSim) * 5 + 10;
    }

    /// <summary>
    /// This method observes the results of previous run and prepare for the next run.
    /// </summary>
    /// <param name="runtime">The ControlledRuntime of previous run.</param>
    public virtual void ObserveRunningResults(EventPatternObserver patternObserver, ControlledRuntime runtime)
    {
        var timelineHash = runtime.TimelineObserver.GetTimelineHash();
        var timelineMinhash = runtime.TimelineObserver.GetTimelineMinhash();
        int diversityScore = ComputeDiversity(timelineHash, timelineMinhash);

        if (diversityScore == 0)
        {
            return;
        }

        if (patternObserver == null)
        {
            if (diversityScore > 0)
            {
                _pendingMutations += diversityScore;
                SavedGenerators.Add(new(diversityScore, Generator, timelineMinhash, 1));
            }
        }
        else
        {
            int coverageResult = patternObserver.ShouldSave();


            if (coverageResult == 1 || _savePartialMatch)
            {

                int priority = 0;
                if (!_discardLowerCoverage)
                {
                    double coverageScore = 1.0 / coverageResult;
                    priority = (int) (diversityScore * coverageScore);
                }
                else
                {
                    if (SavedGenerators.Elements.Count == 0 || coverageResult == SavedGenerators.Peek().Coverage)
                    {
                        priority = diversityScore;
                    }
                    else if (coverageResult < SavedGenerators.Peek().Coverage)
                    {
                        // We remove all saved generators if a new generator with higher coverage is found.
                        SavedGenerators.Elements.Clear();
                        _pendingMutations = 0;
                        priority = diversityScore;
                    }
                }
                if (priority > 0)
                {
                    SavedGenerators.Add(new(priority, Generator, timelineMinhash, coverageResult));
                    _pendingMutations += priority;
                }
            }
        }
    }

    public int TotalSavedInputs()
    {
        return SavedGenerators.Elements.Count;
    }

    private void PrepareNextInput()
    {
        Generator.ScheduleGenerator.PrepareForNextInput();
        if (SavedGenerators.Elements.Count == 0)
        {
            // Mutate current input if no input is saved.
            Generator = NewGenerator();
        }
        else
        {
            var record = SavedGenerators.Pop();
            Generator = MutateGenerator(record.Generator);
            record.Priority -= 1;
            _pendingMutations -= 1;
            SavedGenerators.Add(record);
        }
    }


    protected virtual StrategyGenerator MutateGenerator(StrategyGenerator prev)
    {
        return new StrategyGenerator(prev.InputGenerator.Mutate(), prev.ScheduleGenerator.Mutate());
    }

    protected virtual StrategyGenerator NewGenerator()
    {
        return new StrategyGenerator(Generator.InputGenerator.New(), Generator.ScheduleGenerator.New());
    }

    public void DumpStats(TextWriter writer)
    {
        Debug.Assert(
            SavedGenerators.Elements.Select(it => it.Priority).Sum() == _pendingMutations);
        writer.WriteLine($"..... Total saved: {TotalSavedInputs()}, pending mutations: {_pendingMutations}");
    }
}