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

    public record GeneratorRecord(int Priority, StrategyGenerator Generator, List<int> MinHash);

    protected StrategyGenerator Generator;

    private readonly int _maxScheduledSteps;

    protected int ScheduledSteps;

    private readonly HashSet<int> _visitedTimelines = new();

    private LinkedList<GeneratorRecord> _savedGenerators = new LinkedList<GeneratorRecord>();
    private int _pendingMutations = 0;
    private HashSet<GeneratorRecord> _visitedGenerators = new HashSet<GeneratorRecord>();
    private GeneratorRecord? _currentParent = null;

    private readonly bool _savePartialMatch;
    private readonly bool _diversityBasedPriority;
    private readonly bool _ignorePatternFeedback;
    private readonly int _discardAfter;



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
        _diversityBasedPriority = checkerConfiguration.DiversityBasedPriority;
        _discardAfter = checkerConfiguration.DiscardAfter;
        _ignorePatternFeedback = checkerConfiguration.IgnorePatternFeedback;

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

        if (_savedGenerators.Count == 0 || !_diversityBasedPriority)
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


        return (hash.Count - maxSim) * 10 + 20;
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

        int priority = 0;
        if (patternObserver == null || _ignorePatternFeedback)
        {
            priority = diversityScore;
        }
        else
        {
            int coverageResult = patternObserver.ShouldSave();
            if (coverageResult == 1 || _savePartialMatch)
            {
                double coverageScore = 1.0 / coverageResult;
                priority = (int)(diversityScore * coverageScore);
            }
        }

        if (priority > 0)
        {
            var record = new GeneratorRecord(priority, Generator, timelineMinhash);
            if (_savedGenerators.Count == 0)
            {
                _savedGenerators.AddLast(record);
                return;
            }

            if (priority <= _savedGenerators.Last.Value.Priority)
            {
                return;
            }

            // Maybe use binary search to speed up in the future.
            var cur = _savedGenerators.First;
            while (cur != null && priority < cur.Value.Priority)
            {
                cur = cur.Next;
            }

            if (cur == null)
            {
                _savedGenerators.AddLast(record);
            }
            else
            {
                _savedGenerators.AddBefore(cur, record);
            }

            if (_savedGenerators.Count > _discardAfter)
            {
                var last = _savedGenerators.Last.Value;
                _visitedGenerators.Remove(last);
                _savedGenerators.RemoveLast();
            }
        }
    }

    public int TotalSavedInputs()
    {
        return _savedGenerators.Count;
    }

    private void PrepareNextInput()
    {
        Generator.ScheduleGenerator.PrepareForNextInput();
        if (_savedGenerators.Count == 0)
        {
            // Mutate current input if no input is saved.
            Generator = NewGenerator();
        }
        else
        {
            if (_currentParent == null)
            {
                _currentParent = _savedGenerators.First!.Value;
                _visitedGenerators.Add(_currentParent);
                _pendingMutations = _currentParent.Priority;
            }

            if (_pendingMutations == 0)
            {
                bool found = false;
                foreach (var generator in _savedGenerators)
                {
                    if (_visitedGenerators.Contains(generator)) continue;
                    _currentParent = generator;
                    _visitedGenerators.Add(generator);
                    _pendingMutations = generator.Priority;
                    found = true;
                }

                if (!found)
                {
                    _visitedGenerators.Clear();
                    _currentParent = _savedGenerators.First!.Value;
                    _visitedGenerators.Add(_currentParent);
                    _pendingMutations = _currentParent.Priority;
                }
            }

            Generator = MutateGenerator(_currentParent.Generator);
            _pendingMutations -= 1;
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
        writer.WriteLine($"..... Total saved: {TotalSavedInputs()}, pending mutations: {_pendingMutations}, visited generators: {_visitedGenerators.Count}");
    }
}