using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Runtime.Events;
using PChecker.Runtime.Logging;
using PChecker.Runtime.StateMachines;

namespace PChecker.Feedback;

internal class TimelineObserver : IControlledRuntimeLog
{
    private HashSet<(string, string, string)> _timelines = new();
    private Dictionary<string, HashSet<string>> _allEvents = new();
    private Dictionary<string, List<string>> _orderedEvents = new();

    public static readonly List<(int, int)> Coefficients = new();
    public static int NumOfCoefficients = 50;

    static TimelineObserver()
    {
        // Fix seed to generate same random numbers across runs.
        var rand = new System.Random(0);

        for (int i = 0; i < NumOfCoefficients; i++)
        {
            Coefficients.Add((rand.Next(), rand.Next()));
        }
    }

    public string GetAbstractTimeline()
    {
        var tls = _timelines.Select(it => $"<{it.Item1}, {it.Item2}, {it.Item3}>").ToList();
        tls.Sort();
        return string.Join(";", tls);
    }

    public string GetTimeline()
    {
        return string.Join(";", _orderedEvents.Select(it =>
        {
            var events = string.Join(",", it.Value);
            return $"{it.Key}: {events}";
        }));
    }

    public List<int> GetTimelineMinhash()
    {
        List<int> minHash = new();
        // Use a deterministic, process-independent hash of each timeline tuple.
        // Tuple/string GetHashCode is randomized per process in modern .NET, which
        // would make the minhash — and thus ComputeDiversity / generator
        // prioritization — non-reproducible under a fixed --seed.
        var timelineHash = _timelines.Select(it => StableHash(it.Item1, it.Item2, it.Item3)).ToList();
        foreach (var (a, b) in Coefficients)
        {
            int minValue = Int32.MaxValue;
            foreach (var value in timelineHash)
            {
                int hash = a * value + b;
                minValue = Math.Min(minValue, hash);
            }
            minHash.Add(minValue);
        }
        return minHash;
    }

    /// <summary>
    /// Deterministic 32-bit FNV-1a hash of a timeline tuple, stable across processes
    /// (unlike string.GetHashCode). Fields are separated so distinct tuples do not alias.
    /// </summary>
    private static int StableHash(string a, string b, string c)
    {
        unchecked
        {
            const uint prime = 16777619;
            uint hash = 2166136261;
            foreach (var s in new[] { a, b, c })
            {
                foreach (var by in System.Text.Encoding.UTF8.GetBytes(s))
                {
                    hash = (hash ^ by) * prime;
                }
                hash = (hash ^ 0x1f) * prime; // field separator
            }
            return (int)hash;
        }
    }

    public void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType)
    {
    }

    public void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName, string actionName)
    {
    }

    public void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName,
        Event e, bool isTargetHalted)
    {
    }

    public void OnRaiseEvent(StateMachineId id, string stateName, Event e)
    {
    }

    public void OnEnqueueEvent(StateMachineId id, Event e)
    {
    }

    public void OnDequeueEvent(StateMachineId id, string stateName, Event e)
    {
        string actor = id.Type;
        
        _allEvents.TryAdd(actor, new());
        _orderedEvents.TryAdd(actor, new());

        string name = e.GetType().Name;
        foreach (var ev in _allEvents[actor])
        {
            _timelines.Add((actor, ev, name));
        }
        _allEvents[actor].Add(name);
        _orderedEvents[actor].Add(name);
    }

    public void OnReceiveEvent(StateMachineId id, string stateName, Event e, bool wasBlocked)
    {
    }

    public void OnWaitEvent(StateMachineId id, string stateName, Type eventType)
    {
    }

    public void OnWaitEvent(StateMachineId id, string stateName, params Type[] eventTypes)
    {
    }

    public void OnStateTransition(StateMachineId id, string stateName, bool isEntry)
    {
    }

    public void OnGotoState(StateMachineId id, string currentStateName, string newStateName)
    {
    }

    public void OnDefaultEventHandler(StateMachineId id, string stateName)
    {
    }

    public void OnHalt(StateMachineId id, int inboxSize)
    {
    }

    public void OnHandleRaisedEvent(StateMachineId id, string stateName, Event e)
    {
    }

    public void OnPopStateUnhandledEvent(StateMachineId id, string stateName, Event e)
    {
    }

    public void OnExceptionThrown(StateMachineId id, string stateName, string actionName, Exception ex)
    {
    }

    public void OnExceptionHandled(StateMachineId id, string stateName, string actionName, Exception ex)
    {
    }

    public void OnCreateMonitor(string monitorType)
    {
    }

    public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
    {
    }

    public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName, string senderType,
        string senderStateName, Event e)
    {
    }

    public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
    {
    }

    public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
    {
    }

    public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
    {
    }

    public void OnRandom(object result, string callerName, string callerType)
    {
    }

    public void OnAssertionFailure(string error)
    {
    }

    public void OnStrategyDescription(string strategyName, string description)
    {
    }

    public void OnCompleted()
    {
    }
}
