using System;
using System.Collections.Generic;
using System.Reflection;
using PChecker.Actors;
using PChecker.Actors.Events;
using PChecker.Actors.Logging;

namespace PChecker.Feedback;

internal class EventPatternObserver : ActorRuntimeLogBase
{
    private MethodInfo _matcher;
    private List<Event> _events = new();

    public EventPatternObserver(MethodInfo matcher)
    {
        _matcher = matcher;
    }

    public override void OnDequeueEvent(ActorId id, string stateName, Event e)
    {
        e.Index = _events.Count;
        e.State = stateName;
        _events.Add(e);
    }

    public override void OnMonitorProcessEvent(string monitorType, string stateName, string senderName, string senderType,
        string senderStateName, Event e)
    {
        e.Index = _events.Count;
        e.State = stateName;
        _events.Add(e);
    }

    public virtual int ShouldSave()
    {
        return (int) _matcher.Invoke(null, new [] { _events });
    }
    
    public virtual bool IsMatched()
    {
        int result = (int) _matcher.Invoke(null, new [] { _events });
        return result == 1;
    }
    

    public void Reset()
    {
        _events.Clear();
    }
}