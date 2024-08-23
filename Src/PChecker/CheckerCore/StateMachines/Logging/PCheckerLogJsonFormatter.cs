// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using PChecker.StateMachines;
using PChecker.StateMachines.Events;
using PChecker.StateMachines.Logging;
using PChecker.PRuntime.Exceptions;

namespace PChecker.PRuntime
{
    /// <summary>
    /// This class implements IStateMachineRuntimeLog and generates log output in an XML format.
    /// </summary>
    public class PCheckerLogJsonFormatter : IStateMachineRuntimeLog
    {
        /// <summary>
        /// Get or set the JsonWriter to write to.
        /// </summary>
        public JsonWriter Writer { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PCheckerLogJsonFormatter"/> class.
        /// </summary>
        protected PCheckerLogJsonFormatter()
        {
            Writer = new JsonWriter();
        }

        /// <summary>
        /// Removes the '<' and '>' tags for a log text.
        /// </summary>
        /// <param name="log">The text log</param>
        /// <returns>New string with the tag removed or just the string itself if there is no tag.</returns>
        private static string RemoveLogTag(string log)
        {
            var openingTagIndex = log.IndexOf("<", StringComparison.Ordinal);
            var closingTagIndex = log.IndexOf(">", StringComparison.Ordinal);
            var potentialTagExists = openingTagIndex != -1 && closingTagIndex != -1;
            var validOpeningTag = openingTagIndex == 0 && closingTagIndex > openingTagIndex;

            if (potentialTagExists && validOpeningTag)
            {
                return log[(closingTagIndex + 1)..].Trim();
            }

            return log;
        }

        /// <summary>
        /// Method taken from PCheckerLogTextFormatter.cs file. Takes in a string and only get the
        /// last element of the string separated by a period.
        /// I.e.
        /// Input: PImplementation.TestWithSingleClient(2)
        /// Output: TestWithSingleClient(2)
        /// </summary>
        /// <param name="name">String representing the name to be parsed.</param>
        /// <returns>The split string.</returns>
        private static string GetShortName(string name) => name?.Split('.').Last();

        /// <summary>
        /// Method taken from PCheckerLogTextFormatter.cs file. Takes in Event e and returns string
        /// with details about the event such as event name and its payload. Slightly modified
        /// from the method in PCheckerLogTextFormatter.cs in that payload is parsed with the CleanPayloadString
        /// method right above.
        /// </summary>
        /// <param name="e">Event input.</param>
        /// <returns>String with the event description.</returns>
        private static string GetEventNameWithPayload(Event e)
        {
            if (e.GetType().Name.Contains("GotoStateEvent"))
            {
                return e.GetType().Name;
            }

            var pe = (Event)(e);
            var payload = pe.Payload == null ? "null" : pe.Payload.ToEscapedString();
            var msg = pe.Payload == null ? "" : $" with payload ({payload})";
            return $"{GetShortName(e.GetType().Name)}{msg}";
        }

        /// <summary>
        /// Takes in Event e and returns the dictionary representation of the payload of the event.
        /// </summary>
        /// <param name="e">Event input.</param>
        /// <returns>Dictionary representation of the payload for the event, if any.</returns>
        private static object GetEventPayloadInJson(Event e)
        {
            if (e.GetType().Name.Contains("GotoStateEvent"))
            {
                return null;
            }

            var pe = (Event)(e);
            return pe.Payload?.ToDict();
        }

        public void OnCompleted()
        {
        }

        public void OnAssertionFailure(string error)
        {
            error = RemoveLogTag(error);

            Writer.AddLogType(JsonWriter.LogType.AssertionFailure);
            Writer.LogDetails.Error = error;
            Writer.AddLog(error);
            Writer.AddToLogs();
        }
        
        /// <inheritdoc/>
        public void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType)
        {
            if (id.Name.Contains("GodMachine") || creatorName.Contains("GodMachine"))
            {
                return;
            }

            var source = creatorName ?? $"task '{Task.CurrentId}'";
            var log = $"{id} was created by {source}.";

            Writer.AddLogType(JsonWriter.LogType.CreateStateMachine);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.CreatorName = creatorName;
            Writer.LogDetails.CreatorType = GetShortName(creatorType);
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        public void OnDefaultEventHandler(StateMachineId id, string stateName)
        {
            stateName = GetShortName(stateName);

            var log = stateName is null
                ? $"{id} is executing the default handler."
                : $"{id} is executing the default handler in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogType.DefaultEventHandler);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.State = stateName;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        public void OnDequeueEvent(StateMachineId id, string stateName, Event e)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            var eventName = GetEventNameWithPayload(e);
            var log = stateName is null
                ? $"'{id}' dequeued event '{eventName}'."
                : $"'{id}' dequeued event '{eventName}' in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogType.DequeueEvent);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.Event = GetShortName(e.GetType().Name);
            Writer.LogDetails.State = GetShortName(stateName);
            Writer.LogDetails.Payload = GetEventPayloadInJson(e);
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnEnqueueEvent(StateMachineId id, Event e)
        {
        }

        /// <inheritdoc />
        public void OnExceptionHandled(StateMachineId id, string stateName, string actionName, Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                return;
            }

            var log = stateName is null
                ? $"{id} running action '{actionName}' chose to handle exception '{ex.GetType().Name}'."
                : $"{id} running action '{actionName}' in state '{stateName}' chose to handle exception '{ex.GetType().Name}'.";

            Writer.AddLogType(JsonWriter.LogType.ExceptionHandled);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.State = GetShortName(stateName);
            Writer.LogDetails.Action = actionName;
            Writer.LogDetails.Exception = ex.GetType().Name;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnExceptionThrown(StateMachineId id, string stateName, string actionName, Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                return;
            }

            var log = stateName is null
                ? $"{id} running action '{actionName}' chose to handle exception '{ex.GetType().Name}'."
                : $"{id} running action '{actionName}' in state '{stateName}' threw exception '{ex.GetType().Name}'.";

            Writer.AddLogType(JsonWriter.LogType.ExceptionThrown);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.State = GetShortName(stateName);
            Writer.LogDetails.Action = actionName;
            Writer.LogDetails.Exception = ex.GetType().Name;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }
        
        /// <inheritdoc />
        public void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName, string actionName)
        {
        }

        /// <inheritdoc />
        public void OnGotoState(StateMachineId id, string currentStateName, string newStateName)
        {
            if (currentStateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            currentStateName = GetShortName(currentStateName);
            newStateName = GetShortName(newStateName);

            var log =
                $"{id} is transitioning from state '{currentStateName}' to state '{newStateName}'.";

            Writer.AddLogType(JsonWriter.LogType.GotoState);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.StartState = currentStateName;
            Writer.LogDetails.EndState = newStateName;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnHalt(StateMachineId id, int inboxSize)
        {
            var log = $"{id} halted with {inboxSize} events in its inbox.";

            Writer.AddLogType(JsonWriter.LogType.Halt);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.HaltInboxSize = inboxSize;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }
        
        /// <inheritdoc />
        public void OnHandleRaisedEvent(StateMachineId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public void OnPopState(StateMachineId id, string currentStateName, string restoredStateName)
        {
            currentStateName = string.IsNullOrEmpty(currentStateName) ? "[not recorded]" : currentStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            var log = $"{id} popped state '{currentStateName}' and reentered state '{reenteredStateName}'.";

            Writer.AddLogType(JsonWriter.LogType.PopState);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.StartState = currentStateName;
            Writer.LogDetails.EndState = reenteredStateName;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnPopStateUnhandledEvent(StateMachineId id, string stateName, Event e)
        {
            var log = $"{id} popped state {stateName} due to unhandled event '{e.GetType().Name}'.";

            Writer.AddLogType(JsonWriter.LogType.PopStateUnhandledEvent);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.State = stateName;
            Writer.LogDetails.Event = e.GetType().Name;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnPushState(StateMachineId id, string currentStateName, string newStateName)
        {
            var log = $"{id} pushed from state '{currentStateName}' to state '{newStateName}'.";

            Writer.AddLogType(JsonWriter.LogType.PushState);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.StartState = currentStateName;
            Writer.LogDetails.EndState = newStateName;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnRaiseEvent(StateMachineId id, string stateName, Event e)
        {
            stateName = GetShortName(stateName);
            string eventName = GetEventNameWithPayload(e);

            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine") ||
                eventName.Contains("GotoStateEvent"))
            {
                return;
            }

            var log = stateName is null
                ? $"'{id}' raised event '{eventName}'."
                : $"'{id}' raised event '{eventName}' in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogType.RaiseEvent);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.Event = GetShortName(e.GetType().Name);
            Writer.LogDetails.State = stateName;
            Writer.LogDetails.Payload = GetEventPayloadInJson(e);
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnReceiveEvent(StateMachineId id, string stateName, Event e, bool wasBlocked)
        {
            stateName = GetShortName(stateName);
            string eventName = GetEventNameWithPayload(e);
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            var log = stateName is null
                ? $"'{id}' dequeued event '{eventName}'{unblocked}."
                : $"'{id}' dequeued event '{eventName}'{unblocked} in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogType.ReceiveEvent);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.State = stateName;
            Writer.LogDetails.Event = GetShortName(e.GetType().Name);
            Writer.LogDetails.WasBlocked = wasBlocked;
            Writer.LogDetails.Payload = GetEventPayloadInJson(e);
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
            senderStateName = GetShortName(senderStateName);
            string eventName = GetEventNameWithPayload(e);
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = !string.IsNullOrEmpty(senderName)
                ? $"'{senderName}' in state '{senderStateName}'"
                : $"The runtime";
            var log = $"{sender} sent event '{eventName}' to '{targetStateMachineId}'{isHalted}{opGroupIdMsg}.";

            Writer.AddLogType(JsonWriter.LogType.SendEvent);
            Writer.LogDetails.Sender = !string.IsNullOrEmpty(senderName) ? senderName : "Runtime";
            Writer.LogDetails.State = senderStateName;
            Writer.LogDetails.Event = GetShortName(e.GetType().Name);
            Writer.LogDetails.Target = targetStateMachineId.ToString();
            Writer.LogDetails.OpGroupId = opGroupId.ToString();
            Writer.LogDetails.IsTargetHalted = isTargetHalted;
            Writer.LogDetails.Payload = GetEventPayloadInJson(e);
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnStateTransition(StateMachineId id, string stateName, bool isEntry)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            stateName = GetShortName(stateName);
            var direction = isEntry ? "enters" : "exits";
            var log = $"{id} {direction} state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogType.StateTransition);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.State = stateName;
            Writer.LogDetails.IsEntry = isEntry;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnWaitEvent(StateMachineId id, string stateName, Type eventType)
        {
            stateName = GetShortName(stateName);

            var log = stateName is null
                ? $"{id} is waiting to dequeue an event of type '{eventType.FullName}'."
                : $"{id} is waiting to dequeue an event of type '{eventType.FullName}' in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogType.WaitEvent);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.EventType = eventType.FullName;
            Writer.LogDetails.State = stateName;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnWaitEvent(StateMachineId id, string stateName, params Type[] eventTypes)
        {
            stateName = GetShortName(stateName);
            string eventNames;

            switch (eventTypes.Length)
            {
                case 0:
                    eventNames = "'<missing>'";
                    break;
                case 1:
                    eventNames = "'" + eventTypes[0].Name + "'";
                    break;
                case 2:
                    eventNames = "'" + eventTypes[0].Name + "' or '" + eventTypes[1].Name + "'";
                    break;
                case 3:
                    eventNames = "'" + eventTypes[0].Name + "', '" + eventTypes[1].Name + "' or '" +
                                 eventTypes[2].Name +
                                 "'";
                    break;
                default:
                {
                    var eventNameArray = new string[eventTypes.Length - 1];
                    for (var i = 0; i < eventTypes.Length - 2; i++)
                    {
                        eventNameArray[i] = eventTypes[i].Name;
                    }

                    eventNames = "'" + string.Join("', '", eventNameArray) + "' or '" +
                                 eventTypes[^1].Name + "'";
                    break;
                }
            }

            var log = stateName is null
                ? $"{id} is waiting to dequeue an event of type {eventNames}."
                : $"{id} is waiting to dequeue an event of type {eventNames} in state '{stateName}'.";

            var eventTypesNames = eventTypes.Select(eventType => eventType.Name).ToList();

            Writer.AddLogType(JsonWriter.LogType.WaitMultipleEvents);
            Writer.LogDetails.Id = id.ToString();
            Writer.LogDetails.EventTypes = eventTypesNames;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnCreateMonitor(string monitorType)
        {
            monitorType = GetShortName(monitorType);
            var log = $"{monitorType} was created.";

            Writer.AddLogType(JsonWriter.LogType.CreateMonitor);
            Writer.LogDetails.Monitor = monitorType;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
        }

        /// <inheritdoc />
        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            monitorType = GetShortName(monitorType);
            var log = $"{monitorType} is processing event '{GetEventNameWithPayload(e)}' in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogType.MonitorProcessEvent);
            Writer.LogDetails.Monitor = monitorType;
            Writer.LogDetails.State = stateName;
            Writer.LogDetails.Sender = senderName;
            Writer.LogDetails.Event = GetShortName(e.GetType().Name);
            Writer.LogDetails.Payload = GetEventPayloadInJson(e);
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            stateName = GetShortName(stateName);
            monitorType = GetShortName(monitorType);
            string eventName = GetEventNameWithPayload(e);
            var log = $"Monitor '{monitorType}' raised event '{eventName}' in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogType.MonitorRaiseEvent);
            Writer.LogDetails.Monitor = monitorType;
            Writer.LogDetails.Event = GetShortName(e.GetType().Name);
            Writer.LogDetails.State = stateName;
            Writer.LogDetails.Payload = GetEventPayloadInJson(e);
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            monitorType = GetShortName(monitorType);

            if (stateName.Contains("__InitState__"))
            {
                return;
            }

            stateName = GetShortName(stateName);
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "hot " : "cold ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            var log = $"{monitorType} {direction} {liveness}state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogType.MonitorStateTransition);
            Writer.LogDetails.Monitor = monitorType;
            Writer.LogDetails.State = stateName;
            Writer.LogDetails.IsEntry = isEntry;
            Writer.LogDetails.IsInHotState = isInHotState;
            Writer.AddLog(log);
            Writer.AddToLogs(updateVcMap: true);
        }

        /// <inheritdoc />
        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }
        
        /// <inheritdoc />
        public void OnRandom(object result, string callerName, string callerType)
        {
        }

        /// <inheritdoc />
        public void OnStrategyDescription(string strategyName, string description)
        {
            var desc = string.IsNullOrEmpty(description) ? $" Description: {description}" : string.Empty;
            var log = $"Found bug using '{strategyName}' strategy.{desc}";

            Writer.AddLogType(JsonWriter.LogType.StrategyDescription);
            Writer.LogDetails.Strategy = strategyName;
            Writer.LogDetails.StrategyDescription = description;
            Writer.AddLog(log);
            Writer.AddToLogs();
        }
    }
}