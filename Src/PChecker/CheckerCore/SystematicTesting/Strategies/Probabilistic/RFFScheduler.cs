using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Feedback;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies.Probabilistic;

internal class ScheduleRecord
{
    public int NumVisit {get; set; }
    public int FuzzLevel {get; set; }
    public int Priority {get; set;}
    public AbstractSchedule Schedule;

    public ScheduleRecord(int numVisit, int fuzzLevel, AbstractSchedule schedule) {
        NumVisit = numVisit;
        FuzzLevel = fuzzLevel;
        Schedule = schedule;
    }

}
internal class RFFScheduler: PrioritizedScheduler
{
    // public List<(AbstractSchedule, )> savedSchedules = new();
    public Dictionary<int, ScheduleRecord> TraceRecords = new();
    public int SavedTraces = 0;
    public int TotalExec = 1;
    int Skip = 0;
    int Adj = 1;
    int SchedNonDets = 0;
    int parentIndex = 0;
    int count = 0;
    internal AbstractSchedule currentSchedule;
    internal AbstractScheduleObserver observer;
    internal List<ScheduleRecord> savedSchedules = new();
    public IRandomValueGenerator random;
    private POSScheduler _posScheduler;

    public RFFScheduler(IRandomValueGenerator random, ConflictOpMonitor monitor, AbstractScheduleObserver observer)
    {
        this.observer = observer;
        this.random = random;
        currentSchedule = new AbstractSchedule(new HashSet<Constraint>());
        observer.OnNewAbstractSchedule(currentSchedule);
        _posScheduler = new POSScheduler(new RandomPriorizationProvider(random), monitor);
    }

    public bool PrepareForNextIteration()
    {
        _posScheduler.PrepareForNextIteration();
        // We should always check novelty to update global states.
        if (observer.CheckNoveltyAndUpdate() || observer.CheckAbstractTimelineSatisfied())
        {
            int traceHash = observer.GetTraceHash();

            if (!TraceRecords.ContainsKey(traceHash))
            {
                TraceRecords[traceHash] = new ScheduleRecord(0, 0, currentSchedule);
                savedSchedules.Add(TraceRecords[traceHash]);
            }
            var record = TraceRecords[traceHash];
            record.NumVisit += 1;

            int u = TotalExec / (SavedTraces + Adj);

            int factor;
            if (record.NumVisit <= u)
            {
                if (record.FuzzLevel < 31)
                {
                    record.FuzzLevel += 1;
                }

                factor = (1 << record.FuzzLevel) / u;
                Skip = 0;
            }
            else
            {
                factor = 0;
                Skip += 1;
                if (Skip >= SchedNonDets)
                {
                    Skip = 0;
                    Adj += 1;
                }
            }
            int PerfScore = Math.Min(factor * 1, 50);
            record.Priority = PerfScore;
            SavedTraces += 1;
        }
        TotalExec += 1;


        if (parentIndex == -1 && savedSchedules.Count > 0)
        {
            parentIndex = 0;
        }

        if (parentIndex == -1)
        {
            currentSchedule = currentSchedule.Mutate(observer.allVisitedConstraints.Keys.ToList(), random);
            return true;
        }

        if (count < savedSchedules[parentIndex].Priority)
        {
            currentSchedule = savedSchedules[parentIndex].Schedule.Mutate(observer.allVisitedConstraints.Keys.ToList(), random);
            count += 1;
        } else {
            parentIndex += 1;
            if (parentIndex >= savedSchedules.Count) {
                parentIndex = 0;
            }
            count = 0;
        }

        observer.OnNewAbstractSchedule(currentSchedule);
        return true;
    }

    public void Reset()
    {
        _posScheduler.Reset();
    }

    public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
        if (enabledOperations.Count == 0)
        {
            next = null;
            return false;
        }
        if (enabledOperations.Count == 1)
        {
            next = enabledOperations[0];
            return true;
        }

        var highPrioOps = new List<AsyncOperation>();
        var normalPrioOps = new List<AsyncOperation>();
        var lowPrioOps = new List<AsyncOperation>();
        foreach (var op in enabledOperations)
        {
            var avoid = observer.ShouldAvoid(op);
            var take = observer.ShouldTake(op);

            if (avoid && !take)
            {
                lowPrioOps.Add(op);
            }
            else if (!avoid && take)
            {
                highPrioOps.Add(op);
            }
            else
            {
                normalPrioOps.Add(op);
            }

        }

        if (highPrioOps.Count > 0)
        {
            return _posScheduler.GetNextOperation(current, highPrioOps, out next);
        }
        if (normalPrioOps.Count > 0)
        {
            return _posScheduler.GetNextOperation(current, normalPrioOps, out next);
        }
        return _posScheduler.GetNextOperation(current, lowPrioOps, out next);
    }
}