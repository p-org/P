using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker.Feedback;
using PChecker.Generator.Object;
using PChecker.SystematicTesting.Strategies.Probabilistic;
using AsyncOperation = PChecker.SystematicTesting.Operations.AsyncOperation;

namespace PChecker.SystematicTesting.Strategies.Feedback;


internal class FeedbackGuidedStrategy : IFeedbackGuidedStrategy
{
    public record StrategyGenerator(ControlledRandom InputGenerator, IScheduler Scheduler);

    // Priority (ordering score) is decoupled from MutationBudget: Priority ranks saved
    // generators for exploration order, while MutationBudget is how many mutations to
    // spend on this parent. Both derive from the normalized diversity, but keeping them
    // separate stops the [0,1] diversity metric from doubling as an integer budget.
    public record GeneratorRecord(double Priority, int MutationBudget, StrategyGenerator Generator, List<int> MinHash);

    // Default mutation budget for generators with no diversity-derived budget yet
    // (the first parent's exploration and freshly-explored inputs).
    private const int DefaultMutationBudget = 50;

    internal StrategyGenerator Generator;

    private readonly int _maxScheduledSteps;

    protected int ScheduledSteps;

    private readonly HashSet<string> _visitedTimelines = new();

    // Suite-level scenario-coverage state for steering, accumulated across schedules. Only
    // advanced for KEPT generators (see ObserveRunningResults) so a discarded schedule can
    // never "use up" a scenario's novelty and starve a later kept schedule of the boost.
    private readonly Dictionary<string, int> _scenarioSuiteBestReached = new();
    private readonly HashSet<string> _scenarioSuiteSatisfied = new();

    private List<GeneratorRecord> _savedGenerators = new ();
    private int _pendingMutations;
    private bool _shouldExploreNew;
    private HashSet<GeneratorRecord> _visitedGenerators = new HashSet<GeneratorRecord>();
    private GeneratorRecord _currentParent;

    private readonly System.Random _rnd;



    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackGuidedStrategy"/> class.
    /// </summary>
    public FeedbackGuidedStrategy(CheckerConfiguration checkerConfiguration, ControlledRandom inputGenerator, IScheduler scheduler)
    {
        _maxScheduledSteps = checkerConfiguration.MaxUnfairSchedulingSteps;
        // Seed the explore/exploit RNG from the configured seed so feedback-guided
        // runs are reproducible under a fixed --seed (mirrors ControlledRandom).
        _rnd = new System.Random((int?)checkerConfiguration.RandomGeneratorSeed ?? Guid.NewGuid().GetHashCode());
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

    /// <summary>
    /// Diversity of a timeline as 1 - Jaccard(new, closest prior), in [0,1] (the Jaccard
    /// index is estimated from the MinHash signatures). Returns 0 for a timeline already
    /// seen exactly (a duplicate to discard); 1 for the first timeline (maximally novel).
    /// </summary>
    private double ComputeDiversity(string timeline, List<int> hash)
    {
        if (!_visitedTimelines.Add(timeline))
        {
            return 0;
        }

        if (_savedGenerators.Count == 0 || hash.Count == 0)
        {
            return 1.0;
        }

        var maxSim = 0;
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

        // 1 - estimated Jaccard similarity to the most-similar prior timeline.
        return 1.0 - (double)maxSim / hash.Count;
    }

    /// <summary>
    /// This method observes the results of previous run and prepare for the next run.
    /// </summary>
    public virtual void ObserveRunningResults(TimelineObserver timelineObserver,
        IReadOnlyDictionary<string, int> scenarioReached, IReadOnlyCollection<string> scenarioSatisfied)
    {
        var timelineId = timelineObserver.GetAbstractTimeline();
        var timelineMinhash = timelineObserver.GetTimelineMinhash();

        double diversity = ComputeDiversity(timelineId, timelineMinhash);

        if (diversity <= 0)
        {
            // Timeline-redundant schedule is discarded; do NOT fold its scenario progress
            // into the suite state, so it cannot rob a later kept schedule of the novelty.
            return;
        }

        // Steer the search toward under-covered scenarios. Compliance (a coverage-NOVELTY
        // signal, computed only now that the generator is being kept) is 1.0 iff this
        // schedule made new scenario progress; else 0.0 -> priority == diversity (unchanged).
        // A boost (rather than the paper's strict diversity x compliance product) keeps
        // diverse-but-scenario-irrelevant schedules from being discarded, since scenarios
        // are always auto-attached here.
        double scenarioCompliance = ScenarioSteering.NoveltyCompliance(
            scenarioReached, scenarioSatisfied, _scenarioSuiteBestReached, _scenarioSuiteSatisfied);
        double priority = diversity * (1.0 + scenarioCompliance);
        // Mutation budget is proportional to novelty (diversity) but kept as a separate
        // integer so the [0,1] priority metric never doubles as the budget.
        int mutationBudget = Math.Max(1, (int)Math.Round(diversity * DefaultMutationBudget));

        {
            var record = new GeneratorRecord(priority, mutationBudget, Generator, timelineMinhash);
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
                _pendingMutations = _currentParent.MutationBudget;
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
                    _pendingMutations = generator.MutationBudget;
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
                        _pendingMutations = _currentParent.MutationBudget;
                    }
                    else
                    {
                        _shouldExploreNew = true;
                        _currentParent = null;
                        _pendingMutations = DefaultMutationBudget;
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
