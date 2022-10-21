// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// This class implements IActorRuntimeLog and generates output in a a human readable text format.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/core/logging" >Logging</see> for more information.
    /// </remarks>
    public class ActorRuntimeLogTextFormatter : IActorRuntimeLog
    {
        /// <summary>
        /// Get or set the TextWriter to write to.
        /// </summary>
        public TextWriter Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntimeLogTextFormatter"/> class.
        /// </summary>
        public ActorRuntimeLogTextFormatter()
        {
            this.Logger = new ConsoleLogger();
        }

        /// <inheritdoc/>
        public virtual void OnAssertionFailure(string error)
        {
            this.Logger.WriteLine(error);
        }

        /// <inheritdoc/>
        public virtual void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            var source = creatorName ?? $"task '{Task.CurrentId}'";
            var text = $"<CreateLog> {id} was created by {source}.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            var source = creatorName ?? $"task '{Task.CurrentId}'";
            var text = $"<CreateLog> {id} was created by {source}.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnCreateMonitor(string monitorType)
        {
            var text = $"<CreateLog> {monitorType} was created.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnCreateTimer(TimerInfo info)
        {
            string text;
            var source = info.OwnerId?.Name ?? $"task '{Task.CurrentId}'";
            if (info.Period.TotalMilliseconds >= 0)
            {
                text = $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms; " +
                    $"period :{info.Period.TotalMilliseconds}ms) was created by {source}.";
            }
            else
            {
                text = $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms) was created by {source}.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnDefaultEventHandler(ActorId id, string stateName)
        {
            string text;
            if (stateName is null)
            {
                text = $"<DefaultLog> {id} is executing the default handler.";
            }
            else
            {
                text = $"<DefaultLog> {id} is executing the default handler in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text;
            if (stateName is null)
            {
                text = $"<DequeueLog> {id} dequeued event '{eventName}'.";
            }
            else
            {
                text = $"<DequeueLog> {id} dequeued event '{eventName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnEnqueueEvent(ActorId id, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = $"<EnqueueLog> {id} enqueued event '{eventName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            string text;
            if (stateName is null)
            {
                text = $"<ExceptionLog> {id} running action '{actionName}' chose to handle exception '{ex.GetType().Name}'.";
            }
            else
            {
                text = $"<ExceptionLog> {id} running action '{actionName}' in state '{stateName}' chose to handle exception '{ex.GetType().Name}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            string text;
            if (stateName is null)
            {
                text = $"<ExceptionLog> {id} running action '{actionName}' threw exception '{ex.GetType().Name}'.";
            }
            else
            {
                text = $"<ExceptionLog> {id} running action '{actionName}' in state '{stateName}' threw exception '{ex.GetType().Name}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            string text;
            if (currentStateName is null)
            {
                text = $"<ActionLog> {id} invoked action '{actionName}'.";
            }
            else if (handlingStateName != currentStateName)
            {
                text = $"<ActionLog> {id} invoked action '{actionName}' in state '{currentStateName}' where action was declared by state '{handlingStateName}'.";
            }
            else
            {
                text = $"<ActionLog> {id} invoked action '{actionName}' in state '{currentStateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
            string text = $"<GotoLog> {id} is transitioning from state '{currentStateName}' to state '{newStateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnHalt(ActorId id, int inboxSize)
        {
            string text = $"<HaltLog> {id} halted with {inboxSize} events in its inbox.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
        }

        /// <inheritdoc/>
        public virtual void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
            string text = $"<MonitorLog> {monitorType} executed action '{actionName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = $"<MonitorLog> {monitorType} is processing event '{eventName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = $"<MonitorLog> {monitorType} raised event '{eventName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "hot " : "cold ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            var text = $"<MonitorLog> {monitorType} {direction} {liveness}state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }

        /// <inheritdoc/>
        public virtual void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
            currentStateName = string.IsNullOrEmpty(currentStateName) ? "[not recorded]" : currentStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            var text = $"<PopLog> {id} popped state '{currentStateName}' and reentered state '{reenteredStateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            var text = $"<PopLog> {id} popped state {stateName} due to unhandled event '{eventName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
            string text = $"<PushLog> {id} pushed from state '{currentStateName}' to state '{newStateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text;
            if (stateName is null)
            {
                text = $"<RaiseLog> {id} raised event '{eventName}'.";
            }
            else
            {
                text = $"<RaiseLog> {id} raised event '{eventName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            string eventName = e.GetType().FullName;
            string text;
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            if (stateName is null)
            {
                text = $"<ReceiveLog> {id} dequeued event '{eventName}'{unblocked}.";
            }
            else
            {
                text = $"<ReceiveLog> {id} dequeued event '{eventName}'{unblocked} in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = senderName != null ? $"{senderName} in state '{senderStateName}'" : $"task '{Task.CurrentId}'";
            var eventName = e.GetType().FullName;
            var text = $"<SendLog> {sender} sent event '{eventName}' to {targetActorId}{isHalted}{opGroupIdMsg}.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            var text = $"<StateLog> {id} {direction} state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnStopTimer(TimerInfo info)
        {
            var source = info.OwnerId?.Name ?? $"task '{Task.CurrentId}'";
            var text = $"<TimerLog> Timer '{info}' was stopped and disposed by {source}.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnStrategyDescription(string strategyName, string description)
        {
            var desc = string.IsNullOrEmpty(description) ? $" Description: {description}" : string.Empty;
            var text = $"<StrategyLog> Found bug using '{strategyName}' strategy.{desc}";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            string text;
            if (stateName is null)
            {
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type '{eventType.FullName}'.";
            }
            else
            {
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type '{eventType.FullName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            string text;
            string eventNames;
            if (eventTypes.Length == 0)
            {
                eventNames = "'<missing>'";
            }
            else if (eventTypes.Length == 1)
            {
                eventNames = "'" + eventTypes[0].FullName + "'";
            }
            else if (eventTypes.Length == 2)
            {
                eventNames = "'" + eventTypes[0].FullName + "' or '" + eventTypes[1].FullName + "'";
            }
            else if (eventTypes.Length == 3)
            {
                eventNames = "'" + eventTypes[0].FullName + "', '" + eventTypes[1].FullName + "' or '" + eventTypes[2].FullName + "'";
            }
            else
            {
                string[] eventNameArray = new string[eventTypes.Length - 1];
                for (int i = 0; i < eventTypes.Length - 2; i++)
                {
                    eventNameArray[i] = eventTypes[i].FullName;
                }

                eventNames = "'" + string.Join("', '", eventNameArray) + "' or '" + eventTypes[eventTypes.Length - 1].FullName + "'";
            }

            if (stateName is null)
            {
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type {eventNames}.";
            }
            else
            {
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type {eventNames} in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnRandom(object result, string callerName, string callerType)
        {
            var source = callerName ?? $"Task '{Task.CurrentId}'";
            var text = $"<RandomLog> {source} nondeterministically chose '{result}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnCompleted()
        {
        }
    }
}
