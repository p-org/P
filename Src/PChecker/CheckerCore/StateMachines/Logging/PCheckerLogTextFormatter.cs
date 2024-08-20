// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using PChecker.StateMachines.Events;
using PChecker.IO.Logging;

namespace PChecker.StateMachines.Logging
{
    /// <summary>
    /// This class implements IStateMachineRuntimeLog and generates output in a a human readable text format.
    /// </summary>
    public class PCheckerLogTextFormatter : IStateMachineRuntimeLog
    {
        /// <summary>
        /// Get or set the TextWriter to write to.
        /// </summary>
        public TextWriter Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PCheckerLogTextFormatter"/> class.
        /// </summary>
        public PCheckerLogTextFormatter()
        {
            Logger = new ConsoleLogger();
        }

        /// <inheritdoc/>
        public virtual void OnAssertionFailure(string error)
        {
            Logger.WriteLine(error);
        }

        /// <inheritdoc/>
        public void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType)
        {
            if (id.Name.Contains("GodMachine") || creatorName.Contains("GodMachine"))
            {
                return;
            }
            var source = creatorName ?? $"task '{Task.CurrentId}'";
            var text = $"<CreateLog> {id} was created by {source}.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnCreateMonitor(string monitorType)
        {
            var text = $"<CreateLog> {monitorType} was created.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnDefaultEventHandler(StateMachineId id, string stateName)
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

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnDequeueEvent(StateMachineId id, string stateName, Event e)
        {
            var eventName = e.GetType().FullName;
            string text;
            if (stateName is null)
            {
                text = $"<DequeueLog> {id} dequeued event '{eventName}'.";
            }
            else
            {
                text = $"<DequeueLog> {id} dequeued event '{eventName}' in state '{stateName}'.";
            }

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnEnqueueEvent(StateMachineId id, Event e)
        {
            var eventName = e.GetType().FullName;
            var text = $"<EnqueueLog> {id} enqueued event '{eventName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnExceptionHandled(StateMachineId id, string stateName, string actionName, Exception ex)
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

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnExceptionThrown(StateMachineId id, string stateName, string actionName, Exception ex)
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

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName, string actionName)
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

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnGotoState(StateMachineId id, string currentStateName, string newStateName)
        {
            var text = $"<GotoLog> {id} is transitioning from state '{currentStateName}' to state '{newStateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnHalt(StateMachineId id, int inboxSize)
        {
            var text = $"<HaltLog> {id} halted with {inboxSize} events in its inbox.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnHandleRaisedEvent(StateMachineId id, string stateName, Event e)
        {
        }

        /// <inheritdoc/>
        public virtual void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
            var text = $"<MonitorLog> {monitorType} executed action '{actionName}' in state '{stateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            var text = $"<MonitorLog> {monitorType} is processing event '{e.GetType().Name}' in state '{stateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            var eventName = e.GetType().FullName;
            var text = $"<MonitorLog> {monitorType} raised event '{eventName}' in state '{stateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "hot " : "cold ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            var text = $"<MonitorLog> {monitorType} {direction} {liveness}state '{stateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }

        /// <inheritdoc/>
        public virtual void OnPopState(StateMachineId id, string currentStateName, string restoredStateName)
        {
            currentStateName = string.IsNullOrEmpty(currentStateName) ? "[not recorded]" : currentStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            var text = $"<PopLog> {id} popped state '{currentStateName}' and reentered state '{reenteredStateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnPopStateUnhandledEvent(StateMachineId id, string stateName, Event e)
        {
            var eventName = e.GetType().Name;
            var text = $"<PopLog> {id} popped state {stateName} due to unhandled event '{eventName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnPushState(StateMachineId id, string currentStateName, string newStateName)
        {
            var text = $"<PushLog> {id} pushed from state '{currentStateName}' to state '{newStateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnRaiseEvent(StateMachineId id, string stateName, Event e)
        {
            var eventName = e.GetType().FullName;
            string text;
            if (stateName is null)
            {
                text = $"<RaiseLog> {id} raised event '{eventName}'.";
            }
            else
            {
                text = $"<RaiseLog> {id} raised event '{eventName}' in state '{stateName}'.";
            }

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnReceiveEvent(StateMachineId id, string stateName, Event e, bool wasBlocked)
        {
            var eventName = e.GetType().FullName;
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

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = senderName != null ? $"{senderName} in state '{senderStateName}'" : $"task '{Task.CurrentId}'";
            var eventName = e.GetType().FullName;
            var text = $"<SendLog> {sender} sent event '{eventName}' to {targetStateMachineId}{isHalted}{opGroupIdMsg}.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnStateTransition(StateMachineId id, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            var text = $"<StateLog> {id} {direction} state '{stateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnStrategyDescription(string strategyName, string description)
        {
            var desc = string.IsNullOrEmpty(description) ? $" Description: {description}" : string.Empty;
            var text = $"<StrategyLog> Found bug using '{strategyName}' strategy.{desc}";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnWaitEvent(StateMachineId id, string stateName, Type eventType)
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

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnWaitEvent(StateMachineId id, string stateName, params Type[] eventTypes)
        {
            string text;
            string eventNames;
            if (eventTypes.Length == 0)
            {
                eventNames = "'<missing>'";
            }
            else if (eventTypes.Length == 1)
            {
                eventNames = "'" + eventTypes[0].Name + "'";
            }
            else if (eventTypes.Length == 2)
            {
                eventNames = "'" + eventTypes[0].Name + "' or '" + eventTypes[1].Name + "'";
            }
            else if (eventTypes.Length == 3)
            {
                eventNames = "'" + eventTypes[0].Name + "', '" + eventTypes[1].Name + "' or '" + eventTypes[2].Name + "'";
            }
            else
            {
                var eventNameArray = new string[eventTypes.Length - 1];
                for (var i = 0; i < eventTypes.Length - 2; i++)
                {
                    eventNameArray[i] = eventTypes[i].Name;
                }

                eventNames = "'" + string.Join("', '", eventNameArray) + "' or '" + eventTypes[eventTypes.Length - 1].Name + "'";
            }

            if (stateName is null)
            {
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type {eventNames}.";
            }
            else
            {
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type {eventNames} in state '{stateName}'.";
            }

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnRandom(object result, string callerName, string callerType)
        {
            var source = callerName ?? $"Task '{Task.CurrentId}'";
            var text = $"<RandomLog> {source} nondeterministically chose '{result}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnCompleted()
        {
        }
    }
}