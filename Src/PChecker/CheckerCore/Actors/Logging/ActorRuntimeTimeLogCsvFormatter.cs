using System;
using System.IO;
using PChecker.Actors.Events;
using PChecker.Actors.Timers;
using PChecker.IO.Logging;
using PChecker.SystematicTesting;

namespace PChecker.Actors.Logging;

/// <summary>
/// This class implements IActorRuntimeLog and generates log output in a CSV format with time information included.
/// To be able to access the payload of events, PTimeLogger in CSharpRuntime inherits from this class and implements
/// the logging methods with payload information included in the log.
/// </summary>
public class ActorRuntimeTimeLogCsvFormatter : IActorRuntimeLog
{

    /// <summary>
    /// Underlying thread-safe in-memory logger.
    /// </summary>
    protected InMemoryLogger InMemoryLogger;

    /// <summary>
    /// Current iteration number.
    /// </summary>
    private static int CurrentIteration = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorRuntimeTimeLogCsvFormatter"/> class.
    /// </summary>
    public ActorRuntimeTimeLogCsvFormatter()
    {
        InMemoryLogger = new InMemoryLogger();
        InMemoryLogger.WriteLine("Time,Operation,Event,Payload,Source,State,Target");
    }

    /// <inheritdoc />
    public virtual void OnCreateActor(ActorId id, string creatorName, string creatorType)
    {
    }

    /// <inheritdoc />
    public virtual void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
    {
    }

    /// <inheritdoc />
    public virtual void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
    {
    }

    /// <inheritdoc />
    public virtual void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName, Event e,
        Guid opGroupId, bool isTargetHalted)
    {
    }

    /// <inheritdoc />
    public virtual void OnRaiseEvent(ActorId id, string stateName, Event e)
    {
    }

    /// <inheritdoc />
    public virtual void OnEnqueueEvent(ActorId id, Event e)
    {
    }

    /// <inheritdoc />
    public virtual void OnDequeueEvent(ActorId id, string stateName, Event e)
    {
    }

    /// <inheritdoc />
    public virtual void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
    {
    }

    /// <inheritdoc />
    public virtual void OnWaitEvent(ActorId id, string stateName, Type eventType)
    {
    }

    /// <inheritdoc />
    public virtual void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
    {
    }

    /// <inheritdoc />
    public virtual void OnStateTransition(ActorId id, string stateName, bool isEntry)
    {
    }

    /// <inheritdoc />
    public virtual void OnGotoState(ActorId id, string currentStateName, string newStateName)
    {
    }

    /// <inheritdoc />
    public virtual void OnPushState(ActorId id, string currentStateName, string newStateName)
    {
    }

    /// <inheritdoc />
    public virtual void OnPopState(ActorId id, string currentStateName, string restoredStateName)
    {
    }

    /// <inheritdoc />
    public virtual void OnDefaultEventHandler(ActorId id, string stateName)
    {
    }

    /// <inheritdoc />
    public virtual void OnHalt(ActorId id, int inboxSize)
    {
    }

    /// <inheritdoc />
    public virtual void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
    {
    }

    /// <inheritdoc />
    public virtual void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
    {
    }

    /// <inheritdoc />
    public virtual void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
    {
    }

    /// <inheritdoc />
    public virtual void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
    {
    }

    /// <inheritdoc />
    public virtual void OnCreateTimer(TimerInfo info)
    {
    }

    /// <inheritdoc />
    public virtual void OnStopTimer(TimerInfo info)
    {
    }

    /// <inheritdoc />
    public virtual void OnCreateMonitor(string monitorType)
    {
    }

    /// <inheritdoc />
    public virtual void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
    {
    }

    /// <inheritdoc />
    public virtual void OnMonitorProcessEvent(string monitorType, string stateName, string senderName, string senderType,
        string senderStateName, Event e)
    {
    }

    /// <inheritdoc />
    public virtual void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
    {
    }

    /// <inheritdoc />
    public virtual void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
    {
    }

    /// <inheritdoc />
    public virtual void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
    {
    }

    /// <inheritdoc />
    public virtual void OnRandom(object result, string callerName, string callerType)
    {
    }

    /// <inheritdoc />
    public virtual void OnAssertionFailure(string error)
    {
    }

    /// <inheritdoc />
    public virtual void OnStrategyDescription(string strategyName, string description)
    {
    }

    /// <inheritdoc />
    public void OnCompleted()
    {
        CurrentIteration++;
        Directory.CreateDirectory("PTimeLogs");
        var LogFilePath = "PTimeLogs/Log" + CurrentIteration + ".csv";
        InMemoryLogger.WriteLine(ControlledRuntime.GlobalTime.GetTime() + ", Completed, null, null, null, null");
        File.WriteAllText(LogFilePath, InMemoryLogger.ToString());
        InMemoryLogger.Dispose();
        InMemoryLogger = new InMemoryLogger();
        InMemoryLogger.WriteLine("Time,Operation,Event,Source,State,Target");
    }
}