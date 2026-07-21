using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Runtime;
using PChecker.Runtime.Events;
using PChecker.Runtime.Logging;
using PChecker.Runtime.StateMachines;

namespace PChecker.Feedback;

/// <summary>
/// Per-iteration observer that detects when a scenario (coverage) monitor is satisfied.
/// A scenario is satisfied when its monitor enters an accepting (cold) state: the runtime
/// surfaces that as <see cref="OnMonitorStateTransition"/> with <c>isInHotState == false</c>.
/// The set of coverage monitors is provided by <see cref="PModule.coverageMonitors"/> at
/// generation time. Aggregation across iterations lives in <c>TestReport</c>.
/// </summary>
internal class ScenarioComplianceObserver : IControlledRuntimeLog
{
    // Coverage-monitor metadata, snapshotted from PModule. PModule.coverageMonitors /
    // scenarioStateCounts are populated by the generated InitializeMonitorMap during test
    // setup — i.e. AFTER this observer is constructed — so we must NOT snapshot them in the
    // ctor (that would make the first iteration, and any 1-schedule run, see an empty set and
    // silently drop all scenario coverage). Instead Refresh() re-reads PModule whenever it has
    // grown, so every transition — including the monitor's initial (start) state-entry logged
    // during RegisterMonitor — is classified.
    private HashSet<string> _coverageMonitorNames = new();
    private List<string> _allScenarioNames = new();
    private int _snapshotCount = -1;

    // Short names (P scenario names) of scenarios satisfied at least once this iteration.
    private readonly HashSet<string> _satisfied = new();

    // Scenarios whose initial (start) state-entry has already been seen. A monitor's very
    // first state-entry is its start state, logged during RegisterMonitor before any observed
    // behavior; a cold start state must NOT count as satisfied (see ScenarioSteering.IsSatisfyingEntry).
    private readonly HashSet<string> _sawStartEntry = new();

    // Short name -> distinct states entered this iteration (partial-progress signal).
    private readonly Dictionary<string, HashSet<string>> _statesReached = new();

    // Short name -> total number of states in that scenario.
    private readonly Dictionary<string, int> _totalStates = new();

    private void Refresh()
    {
        if (PModule.coverageMonitors.Count == _snapshotCount)
        {
            return; // unchanged since the last read
        }
        _snapshotCount = PModule.coverageMonitors.Count;
        _coverageMonitorNames = PModule.coverageMonitors.Select(t => t.FullName).ToHashSet();
        _allScenarioNames = _coverageMonitorNames.Select(ShortName).ToList();
        foreach (var kv in PModule.scenarioStateCounts)
        {
            _totalStates[ShortName(kv.Key.FullName)] = kv.Value;
        }
    }

    /// <summary>Scenarios (by short name) satisfied at least once during this iteration.</summary>
    public IReadOnlyCollection<string> SatisfiedScenarios => _satisfied;

    /// <summary>Short names of all declared scenarios (so 0-coverage ones are reported too).</summary>
    public IReadOnlyCollection<string> AllScenarioNames
    {
        get { Refresh(); return _allScenarioNames; }
    }

    /// <summary>True if any scenario monitors are active for this run.</summary>
    public bool HasScenarios
    {
        get { Refresh(); return _coverageMonitorNames.Count > 0; }
    }

    /// <summary>Distinct states <paramref name="scenario"/> entered this iteration.</summary>
    public int StatesReached(string scenario) => _statesReached.TryGetValue(scenario, out var s) ? s.Count : 0;

    /// <summary>Total states declared in <paramref name="scenario"/> (0 if unknown).</summary>
    public int TotalStates(string scenario) => _totalStates.TryGetValue(scenario, out var n) ? n : 0;

    /// <summary>
    /// Snapshot of distinct states reached this iteration, per scenario. Fed (with
    /// <see cref="SatisfiedScenarios"/> and the suite-so-far state) to
    /// <see cref="ScenarioSteering.NoveltyCompliance"/> to produce the coverage-novelty
    /// steering signal for the feedback-guided search.
    /// </summary>
    public IReadOnlyDictionary<string, int> ReachedSnapshot()
    {
        var snapshot = new Dictionary<string, int>();
        foreach (var scenario in AllScenarioNames)
        {
            snapshot[scenario] = StatesReached(scenario);
        }
        return snapshot;
    }

    private static string ShortName(string fullName)
    {
        var idx = fullName.LastIndexOf('.');
        return idx >= 0 ? fullName.Substring(idx + 1) : fullName;
    }

    public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
    {
        // Pick up coverage monitors registered since construction (see Refresh) so early
        // transitions — including the initial state-entry during RegisterMonitor — are counted.
        Refresh();
        if (!isEntry || !_coverageMonitorNames.Contains(monitorType))
        {
            return;
        }
        var scenario = ShortName(monitorType);

        // Track partial progress: distinct states this scenario has entered.
        if (!_statesReached.TryGetValue(scenario, out var states))
        {
            states = new HashSet<string>();
            _statesReached[scenario] = states;
        }
        states.Add(stateName);

        // Entering a cold (accepting) state == scenario satisfied, EXCEPT the monitor's very
        // first (start) state entry (logged during RegisterMonitor, before any observed event):
        // a cold start state is not "covered" until real behavior re-enters an accepting state.
        var isStartEntry = _sawStartEntry.Add(scenario); // true only on this scenario's first entry
        if (ScenarioSteering.IsSatisfyingEntry(isInHotState, isStartEntry))
        {
            _satisfied.Add(scenario);
        }
    }

    // ── Remaining IControlledRuntimeLog hooks are not needed here ──
    public void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType) { }
    public void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName, string actionName) { }
    public void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName, Event e, bool isTargetHalted) { }
    public void OnRaiseEvent(StateMachineId id, string stateName, Event e) { }
    public void OnEnqueueEvent(StateMachineId id, Event e) { }
    public void OnDequeueEvent(StateMachineId id, string stateName, Event e, StateMachineId senderId, VectorTime deliveryTime) { }
    public void OnReceiveEvent(StateMachineId id, string stateName, Event e, bool wasBlocked) { }
    public void OnWaitEvent(StateMachineId id, string stateName, Type eventType) { }
    public void OnWaitEvent(StateMachineId id, string stateName, params Type[] eventTypes) { }
    public void OnStateTransition(StateMachineId id, string stateName, bool isEntry) { }
    public void OnGotoState(StateMachineId id, string currentStateName, string newStateName) { }
    public void OnDefaultEventHandler(StateMachineId id, string stateName) { }
    public void OnHalt(StateMachineId id, int inboxSize) { }
    public void OnHandleRaisedEvent(StateMachineId id, string stateName, Event e) { }
    public void OnPopStateUnhandledEvent(StateMachineId id, string stateName, Event e) { }
    public void OnExceptionThrown(StateMachineId id, string stateName, string actionName, Exception ex) { }
    public void OnExceptionHandled(StateMachineId id, string stateName, string actionName, Exception ex) { }
    public void OnCreateMonitor(string monitorType) { }
    public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName) { }
    public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName, string senderType, string senderStateName, Event e) { }
    public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e) { }
    public void OnMonitorError(string monitorType, string stateName, bool? isInHotState) { }
    public void OnRandom(object result, string callerName, string callerType) { }
    public void OnAssertionFailure(string error) { }
    public void OnStrategyDescription(string strategyName, string description) { }
    public void OnCompleted() { }
}
