using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using PChecker.Coverage;
using PChecker.Generator;
using PChecker.Feedback;
using AsyncOperation = PChecker.SystematicTesting.Operations.AsyncOperation;

namespace PChecker.SystematicTesting.Strategies.Feedback;


internal class FeedbackGuidedStrategy<TInput, TSchedule> : IFeedbackGuidedStrategy
    where TInput: IInputGenerator<TInput>
    where TSchedule: IScheduleGenerator<TSchedule>
{
    public record StrategyGenerator(TInput InputGenerator, TSchedule ScheduleGenerator);

    protected StrategyGenerator Generator;

    private readonly int _maxScheduledSteps;

    protected int ScheduledSteps;
    private int _visitedStates = 0;

    private readonly Dictionary<int, HashSet<int>> _visitedTimelines = new();

    protected List<(int, StrategyGenerator)> SavedGenerators = new();

    private readonly int _maxMutationsForFavored = 50;
    private readonly int _maxMutationsForUnfavored = 10;
    private readonly int _nonFavoredCap = 10;
    private int _nonFavoredSaved = 0;

    private int _numMutations = 0;

    private int _currentInputIndex = 0;

    private bool _matched = false;
    private readonly bool _savePartialMatch;

    public int CurrentInputIndex()
    {
        return _currentInputIndex;
    }


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

    /// <summary>
    /// This method observes the results of previous run and prepare for the next run.
    /// </summary>
    /// <param name="runtime">The ControlledRuntime of previous run.</param>
    public virtual void ObserveRunningResults(EventPatternObserver patternObserver, ControlledRuntime runtime)
    {
        if (patternObserver == null)
        {
            _visitedTimelines.TryAdd(-1, new HashSet<int>());
            if (_visitedTimelines[-1].Add(runtime.TimelineObserver.GetTimelineHash()))
            {
                SavedGenerators.Add((-1, Generator));
            }
        }
        else
        {
            int state = patternObserver.ShouldSave();
            if (state == -1 || (_savePartialMatch && state >= _visitedStates))
            {
                if (state != -1 && state > _visitedStates)
                {
                    System.Random rng = new System.Random();
                    SavedGenerators = SavedGenerators.Where(it => it.Item1 == -1).OrderBy(_ => rng.Next()).ToList();
                    _visitedStates = state;
                    _currentInputIndex = 0;
                    _nonFavoredSaved = 0;
                    _numMutations = 0;
                }

                _visitedTimelines.TryAdd(state, new HashSet<int>());
                int timelineHash = runtime.TimelineObserver.GetTimelineHash();
                if (!_visitedTimelines[state].Contains(timelineHash))
                {
                    if (state == -1 || (_nonFavoredSaved < _nonFavoredCap))
                    {
                        _visitedTimelines[state].Add(timelineHash);
                        SavedGenerators.Add((state, Generator));
                        if (state != -1)
                        {
                            _nonFavoredSaved += 1;
                        }
                    }
                }
            }
        }
    }

    public int TotalSavedInputs()
    {
        return SavedGenerators.Count;
    }

    private void PrepareNextInput()
    {
        Generator.ScheduleGenerator.PrepareForNextInput();
        if (SavedGenerators.Count == 0)
        {
            // Mutate current input if no input is saved.
            Generator = NewGenerator();
            return;
        }

        int maxMutations = SavedGenerators[_currentInputIndex].Item1 == -1 ? _maxMutationsForFavored : _maxMutationsForUnfavored;
        if (_numMutations >= maxMutations)
        {
            MoveToNextInput();
        }
        else
        {
            _numMutations ++;
        }

        if (_currentInputIndex >= SavedGenerators.Count)
        {
            _currentInputIndex = 0;
        }

        if (SavedGenerators.Count == 0)
        {
            Generator = NewGenerator();
        }
        else
        {
            Generator = MutateGenerator(SavedGenerators[_currentInputIndex].Item2);
        }
    }

    protected virtual void MoveToNextInput()
    {
        if (SavedGenerators[_currentInputIndex].Item1 != -1)
        {
            SavedGenerators.RemoveAt(_currentInputIndex);
            _nonFavoredSaved -= 1;
        }
        else
        {
            _currentInputIndex += 1;
        }
        _numMutations = 0;
    }


    protected virtual StrategyGenerator MutateGenerator(StrategyGenerator prev)
    {
        return new StrategyGenerator(prev.InputGenerator.Mutate(), prev.ScheduleGenerator.Mutate());
    }

    protected virtual StrategyGenerator NewGenerator()
    {
        return new StrategyGenerator(Generator.InputGenerator.New(), Generator.ScheduleGenerator.New());
    }

    public int GetAllCoveredStates()
    {
        return _visitedStates;
    }
}