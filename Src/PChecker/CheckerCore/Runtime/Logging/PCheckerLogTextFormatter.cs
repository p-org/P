// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PChecker.IO.Logging;
using PChecker.Runtime.Events;
using PChecker.Runtime.Exceptions;
using PChecker.Runtime.StateMachines;

namespace PChecker.Runtime.Logging
{
    /// <summary>
    ///     Formatter for the runtime log.
    /// </summary>
    public class PCheckerLogTextFormatter : IControlledRuntimeLog
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

        private string GetShortName(string name)
        {
            return name.Split('.').Last();
        }

        private string GetEventNameWithPayload(Event e)
        {
            if (e.GetType().Name.Contains("GotoStateEvent"))
            {
                return e.GetType().Name;
            }
            var pe = e;
            var payload = pe.Payload == null ? "null" : pe.Payload.ToEscapedString();
            var msg = pe.Payload == null ? "" : $" with payload ({payload})";
            return $"{GetShortName(e.GetType().Name)}{msg}";
        }
        
        /// <inheritdoc/>
        public void OnAssertionFailure(string error)
        {
            Logger.WriteLine(error);
        }

        /// <inheritdoc/>
        public void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType)
        {
            var source = creatorName ?? $"task '{Task.CurrentId}'";
            var text = $"<CreateLog> {id} was created by {source}.";
            Logger.WriteLine(text);
        }
        
        /// <inheritdoc/>
        public void OnStateTransition(StateMachineId id, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            var text = $"<StateLog> {id} {direction} state '{stateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnDefaultEventHandler(StateMachineId id, string stateName)
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
        public void OnWaitEvent(StateMachineId id, string stateName, params Type[] eventTypes)
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
        public void OnWaitEvent(StateMachineId id, string stateName, Type eventType)
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
        public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            if (stateName.Contains("__InitState__"))
            {
                return;
            }

            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "hot " : "cold ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            var text = $"<MonitorLog> {monitorType} {direction} {liveness}state '{stateName}'.";
            Logger.WriteLine(text);
        }
        
        /// <inheritdoc/>
        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName, string senderType,
            string senderStateName, Event e)
        {
            var text = $"<MonitorLog> {GetShortName(monitorType)} is processing event '{GetEventNameWithPayload(e)}' in state '{stateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnDequeueEvent(StateMachineId id, string stateName, Event e)
        {
            stateName = GetShortName(stateName);
            var eventName = GetEventNameWithPayload(e);
            string text = null;
            if (stateName is null)
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}'.";
            }
            else
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}' in state '{stateName}'.";
            }

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnRaiseEvent(StateMachineId id, string stateName, Event e)
        {
            stateName = GetShortName(stateName);
            var eventName = GetEventNameWithPayload(e);
            if (eventName.Contains("GotoStateEvent"))
            {
                return;
            }

            string text = null;
            if (stateName is null)
            {
                text = $"<RaiseLog> '{id}' raised event '{eventName}'.";
            }
            else
            {
                text = $"<RaiseLog> '{id}' raised event '{eventName}' in state '{stateName}'.";
            }

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnEnqueueEvent(StateMachineId id, Event e) {   }

        /// <inheritdoc/>
        public void OnReceiveEvent(StateMachineId id, string stateName, Event e, bool wasBlocked)
        {
            stateName = GetShortName(stateName);
            var eventName = GetEventNameWithPayload(e);
            string text = null;
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            if (stateName is null)
            {
                text = $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked}.";
            }
            else
            {
                text = $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked} in state '{stateName}'.";
            }

            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            stateName = GetShortName(stateName);
            var eventName = GetEventNameWithPayload(e);
            var text = $"<MonitorLog> Monitor '{GetShortName(monitorType)}' raised event '{eventName}' in state '{stateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName, Event e, bool isTargetHalted)
        {
            senderStateName = GetShortName(senderStateName);
            var eventName = GetEventNameWithPayload(e);
            var isHalted = isTargetHalted ? " which has halted" : string.Empty;
            var sender = !string.IsNullOrEmpty(senderName) ? $"'{senderName}' in state '{senderStateName}'" : "The runtime";
            var text = $"<SendLog> {sender} sent event '{eventName}' to '{targetStateMachineId}'{isHalted}.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnGotoState(StateMachineId id, string currStateName, string newStateName)
        {
            var text = $"<GotoLog> {id} is transitioning from state '{currStateName}' to state '{newStateName}'.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName, string actionName)
        {
        }

        /// <inheritdoc/>
        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
        }

        /// <inheritdoc/>
        public void OnExceptionHandled(StateMachineId id, string stateName, string actionName, Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                return;
            }
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
        public void OnExceptionThrown(StateMachineId id, string stateName, string actionName, Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                return;
            }
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
        public void OnCreateMonitor(string monitorType)
        {
            var text = $"<CreateLog> {monitorType} was created.";
            Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnHandleRaisedEvent(StateMachineId id, string stateName, Event e)
        {
        }

        /// <inheritdoc/>
        public void OnRandom(object result, string callerName, string callerType)
        {
            var source = callerName ?? $"Task '{Task.CurrentId}'";
            var text = $"<RandomLog> {source} nondeterministically chose '{result}'.";
            Logger.WriteLine(text);
        }
        
        /// <inheritdoc/>
        public void OnHalt(StateMachineId id, int inboxSize)
        {
            var text = $"<HaltLog> {id} halted with {inboxSize} events in its inbox.";
            Logger.WriteLine(text);
        }
        
        /// <inheritdoc/>
        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }

        /// <inheritdoc/>
        public void OnCompleted()
        {
        }
        
        /// <inheritdoc/>
        public void OnStrategyDescription(string strategyName, string description)
        {
            var desc = string.IsNullOrEmpty(description) ? $" Description: {description}" : string.Empty;
            var text = $"<StrategyLog> Found bug using '{strategyName}' strategy.{desc}";
            Logger.WriteLine(text);
        }
    }
}