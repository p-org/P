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
    // FullName of every coverage (scenario) monitor for this run.
    private readonly HashSet<string> _coverageMonitorNames;

    // Short names (P scenario names) of scenarios satisfied at least once this iteration.
    private readonly HashSet<string> _satisfied = new();

    public ScenarioComplianceObserver()
    {
        _coverageMonitorNames = PModule.coverageMonitors.Select(t => t.FullName).ToHashSet();
        AllScenarioNames = _coverageMonitorNames.Select(ShortName).ToList();
    }

    /// <summary>Scenarios (by short name) satisfied at least once during this iteration.</summary>
    public IReadOnlyCollection<string> SatisfiedScenarios => _satisfied;

    /// <summary>Short names of all declared scenarios (so 0-coverage ones are reported too).</summary>
    public IReadOnlyCollection<string> AllScenarioNames { get; }

    /// <summary>True if any scenario monitors are active for this run.</summary>
    public bool HasScenarios => _coverageMonitorNames.Count > 0;

    private static string ShortName(string fullName)
    {
        var idx = fullName.LastIndexOf('.');
        return idx >= 0 ? fullName.Substring(idx + 1) : fullName;
    }

    public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
    {
        // A coverage monitor entering a cold (accepting) state == scenario satisfied.
        if (isEntry && isInHotState == false && _coverageMonitorNames.Contains(monitorType))
        {
            _satisfied.Add(ShortName(monitorType));
        }
    }

    // ── Remaining IControlledRuntimeLog hooks are not needed here ──
    public void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType) { }
    public void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName, string actionName) { }
    public void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName, Event e, bool isTargetHalted) { }
    public void OnRaiseEvent(StateMachineId id, string stateName, Event e) { }
    public void OnEnqueueEvent(StateMachineId id, Event e) { }
    public void OnDequeueEvent(StateMachineId id, string stateName, Event e) { }
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
