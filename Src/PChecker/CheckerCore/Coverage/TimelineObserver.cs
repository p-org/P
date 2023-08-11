using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Actors;
using PChecker.Actors.Events;
using PChecker.Actors.Logging;

namespace PChecker.Feedback;

public class TimelineObserver: IActorRuntimeLog
{

    private HashSet<(string, string, string)> _timelines = new();
    private Dictionary<string, HashSet<string>> _allEvents = new();

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

    public void OnCreateActor(ActorId id, string creatorName, string creatorType)
    {

    }

    public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
    {
    }

    public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
    {

    }

    public void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName, Event e,
        Guid opGroupId, bool isTargetHalted)
    {
    }

    public void OnRaiseEvent(ActorId id, string stateName, Event e)
    {

    }

    public void OnEnqueueEvent(ActorId id, Event e)
    {

    }

    public void OnDequeueEvent(ActorId id, string stateName, Event e)
    {
        string actor = id.Type;
        
        _allEvents.TryAdd(actor, new());

        string name = e.GetType().Name;
        foreach (var ev in _allEvents[actor])
        {
            _timelines.Add((actor, ev, name));
        }
        _allEvents[actor].Add(name);
    }

    public int GetTimelineHash()
    {
        return GetTimeline().GetHashCode();
    }

    public string GetTimeline()
    {
        var tls = _timelines.Select(it => $"<{it.Item1}, {it.Item2}, {it.Item3}>").ToList();
        tls.Sort();
        return string.Join(";", tls);
    }

    public List<int> GetTimelineMinhash()
    {
        List<int> minHash = new();
        var timelineHash = _timelines.Select(it => it.GetHashCode());
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

    public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
    {

    }

    public void OnWaitEvent(ActorId id, string stateName, Type eventType)
    {

    }

    public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
    {

    }

    public void OnStateTransition(ActorId id, string stateName, bool isEntry)
    {

    }

    public void OnGotoState(ActorId id, string currentStateName, string newStateName)
    {
    }

    public void OnDefaultEventHandler(ActorId id, string stateName)
    {

    }

    public void OnHalt(ActorId id, int inboxSize)
    {

    }

    public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
    {

    }

    public void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
    {

    }

    public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
    {

    }

    public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
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