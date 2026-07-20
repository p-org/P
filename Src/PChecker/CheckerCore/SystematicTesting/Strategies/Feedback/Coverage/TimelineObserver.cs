using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Runtime.Events;
using PChecker.Runtime.Logging;
using PChecker.Runtime.StateMachines;

namespace PChecker.Feedback;

/// <summary>
/// Observes event deliveries in one schedule and produces the "timeline" — the diversity
/// signal that drives the feedback-guided search. The actual timeline encoding is
/// delegated to a selectable <see cref="ITimelineRepresentation"/> (see --timeline-repr);
/// this class owns the shared, deterministic hashing (FNV-1a + fixed-coefficient MinHash)
/// and the canonical-string / MinHash views its consumers expect.
///
/// The default representation (pairwise) is byte-compatible with the historical behavior,
/// so with default options the abstract-timeline strings and MinHash are unchanged.
/// </summary>
internal class TimelineObserver : IControlledRuntimeLog
{
    private readonly ITimelineRepresentation _representation;
    private readonly bool _includePayload;

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

    /// <summary>Default ctor keeps the historical (pairwise) behavior.</summary>
    public TimelineObserver() : this("pairwise", 3, false)
    {
    }

    public TimelineObserver(string representation, int kgram, bool includePayload)
    {
        _representation = ITimelineRepresentation.Create(representation, kgram);
        _includePayload = includePayload;
    }

    /// <summary>
    /// Canonical timeline string: the sorted, joined token set. Used for the exact-match
    /// novelty gate and the ExploredTimelines metric. Distinct schedules under the chosen
    /// representation map to distinct strings; the internal format is not user-facing.
    /// </summary>
    public string GetAbstractTimeline()
    {
        var tokens = _representation.Tokens().ToList();
        tokens.Sort(StringComparer.Ordinal);
        return string.Join(";", tokens);
    }

    /// <summary>MinHash sketch of the token set for Jaccard-similarity diversity estimation.</summary>
    public List<int> GetTimelineMinhash()
    {
        var tokenHashes = _representation.Tokens().Select(StableHash).ToList();
        List<int> minHash = new();
        foreach (var (a, b) in Coefficients)
        {
            int minValue = Int32.MaxValue;
            foreach (var value in tokenHashes)
            {
                int hash = a * value + b;
                minValue = Math.Min(minValue, hash);
            }
            minHash.Add(minValue);
        }
        return minHash;
    }

    /// <summary>
    /// Deterministic 32-bit FNV-1a hash of a token string, stable across processes
    /// (unlike string.GetHashCode, which is randomized per process and would make the
    /// MinHash — and thus ComputeDiversity / generator prioritization — non-reproducible
    /// under a fixed --seed).
    /// </summary>
    private static int StableHash(string token)
    {
        unchecked
        {
            const uint prime = 16777619;
            uint hash = 2166136261;
            foreach (var by in System.Text.Encoding.UTF8.GetBytes(token))
            {
                hash = (hash ^ by) * prime;
            }
            return (int)hash;
        }
    }

    public void OnDequeueEvent(StateMachineId id, string stateName, Event e,
        StateMachineId senderId, VectorTime deliveryTime)
    {
        // The event label is the type name, optionally enriched with a stable hash of the
        // payload so that deliveries differing only by payload value (ballot, term, txnId,
        // status, ...) are not conflated. Payload enrichment is off by default.
        string label = e.GetType().Name;
        if (_includePayload && e.Payload != null)
        {
            label = $"{label}#{StableHash(e.Payload.ToString() ?? string.Empty)}";
        }

        _representation.RecordDelivery(id.Name, senderId?.Name ?? string.Empty, label, deliveryTime);
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
