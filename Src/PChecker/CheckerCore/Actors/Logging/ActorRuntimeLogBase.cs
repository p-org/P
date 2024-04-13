using System;
using PChecker.Actors.Events;

namespace PChecker.Actors.Logging;

internal abstract class ActorRuntimeLogBase : IActorRuntimeLog
{
    public virtual void OnCreateActor(ActorId id, string creatorName, string creatorType)
    {
    }

    public virtual void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
    {
    }

    public virtual void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName,
        string actionName)
    {
    }

    public virtual void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
        Event e,
        Guid opGroupId, bool isTargetHalted)
    {
    }

    public virtual void OnRaiseEvent(ActorId id, string stateName, Event e)
    {
    }

    public virtual void OnEnqueueEvent(ActorId id, Event e)
    {
    }

    public virtual void OnDequeueEvent(ActorId id, string stateName, Event e)
    {
    }

    public virtual void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
    {
    }

    public virtual void OnWaitEvent(ActorId id, string stateName, Type eventType)
    {
    }

    public virtual void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
    {
    }

    public virtual void OnStateTransition(ActorId id, string stateName, bool isEntry)
    {
    }

    public virtual void OnGotoState(ActorId id, string currentStateName, string newStateName)
    {
    }

    public virtual void OnDefaultEventHandler(ActorId id, string stateName)
    {
    }

    public virtual void OnHalt(ActorId id, int inboxSize)
    {
    }

    public virtual void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
    {
    }

    public virtual void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
    {
    }

    public virtual void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
    {
    }

    public virtual void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
    {
    }

    public virtual void OnCreateMonitor(string monitorType)
    {
    }

    public virtual void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
    {
    }

    public virtual void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
        string senderType,
        string senderStateName, Event e)
    {
    }

    public virtual void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
    {
    }

    public virtual void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
    {
    }

    public virtual void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
    {
    }

    public virtual void OnRandom(object result, string callerName, string callerType)
    {
    }

    public virtual void OnAssertionFailure(string error)
    {
    }

    public virtual void OnStrategyDescription(string strategyName, string description)
    {
    }

    public virtual void OnCompleted()
    {
    }
}