// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using PChecker.Actors.Events;
using PChecker.Actors.Timers;
using System.Collections.Generic;
using Plang.CSharpRuntime;
using Plang.CSharpRuntime.Exceptions;

namespace PChecker.Actors.Logging
{
    /// <summary>
    /// This class implements IActorRuntimeLog and generates log output in an XML format.
    /// </summary>
    internal class ActorRuntimeLogJsonFormatter : IActorRuntimeLog
    {
        private JsonWriter Writer;

        public ActorRuntimeLogJsonFormatter(JsonWriter writer)
        {
            Writer = writer;
        }

        /// <summary>
        /// Removes the '<' and '>' tags for a log text.
        /// I.e., '<ErrorLog> Some error log...' becomes 'Some error log...' 
        /// </summary>
        /// <param name="log">The text log</param>
        /// <returns>New string with the tag removed or just the string itself if there is no tag.</returns>
        private static string RemoveLogTag(string log)
        {
            var openingTagIndex = log.IndexOf("<");
            var closingTagIndex = log.IndexOf(">");
            var potentialTagExists = openingTagIndex != -1 && closingTagIndex != -1;
            var validOpeningTag = openingTagIndex == 0 && closingTagIndex > openingTagIndex;

            if (potentialTagExists && validOpeningTag)
            {
                return log[(closingTagIndex + 1)..].Trim();
            }

            return log;
        }

        /// <summary>
        /// Parse payload input to dictionary format.
        /// I.e.
        /// Input: (<source:Client(4), accountId:0, amount:8, rId:19, >)
        /// Parsed Output: {
        ///     "source": "Client(4)",
        ///     "accountId": "0",
        ///     "amount": "8",
        ///     "rId": "19",
        /// }
        /// </summary>
        /// <param name="payload">String representing the payload.</param>
        /// <returns>The dictionary object representation of the payload.</returns>
        private static Dictionary<string, string> ParsePayloadToDictionary(string payload)
        {
            var parsedPayload = new Dictionary<string, string>();
            var trimmedPayload = payload.Trim('(', ')', '<', '>');
            var payloadKeyValuePairs = trimmedPayload.Split(',');

            foreach (var payloadPair in payloadKeyValuePairs)
            {
                var payloadKeyValue = payloadPair.Split(':');

                if (payloadKeyValue.Length != 2) continue;
                var key = payloadKeyValue[0].Trim();
                var value = payloadKeyValue[1].Trim();

                parsedPayload[key] = value;
            }

            return parsedPayload;
        }

        /// <summary>
        /// Method taken from PLogFormatter.cs file. Takes in a string and only get the
        /// last element of the string separated by a period.
        /// I.e.
        /// Input: PImplementation.TestWithSingleClient(2)
        /// Output: TestWithSingleClient(2)
        /// </summary>
        /// <param name="name">String representing the name to be parsed.</param>
        /// <returns>The split string.</returns>
        private static string GetShortName(string name) => name?.Split('.').Last();

        /// TODO: What other forms can the payload be in? Have the method implemented in IPrtValue?
        /// <summary>
        /// Cleans the payload string to remove "<" and "/>" and replacing ':' with '='
        /// I.e.
        /// Input: (<source:Client(4), accountId:0, amount:4, rId:18, >)
        /// Output: (source=Client(4), accountId=0, amount=4, rId=18)
        /// </summary>
        /// <param name="payload">String representation of the payload.</param>
        /// <returns>The cleaned payload.</returns>
        private static string CleanPayloadString(string payload)
        {
            var output = payload.Replace("<", string.Empty);
            output = output.Replace(':', '=');
            output = output.Replace(", >", string.Empty);
            return output;
        }

        /// <summary>
        /// Method taken from PLogFormatter.cs file. Takes in Event e and returns string
        /// with details about the event such as event name and its payload. Slightly modified
        /// from the method in PLogFormatter.cs in that payload is parsed with the CleanPayloadString
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

            var pe = (PEvent)(e);
            var payload = pe.Payload == null ? "null" : pe.Payload.ToEscapedString();
            var msg = pe.Payload == null ? "" : $" with payload ({CleanPayloadString(payload)})";
            return $"{GetShortName(e.GetType().Name)}{msg}";
        }

        /// <summary>
        /// Takes in Event e and returns the dictionary representation of the payload of the event.
        /// </summary>
        /// <param name="e">Event input.</param>
        /// <returns>Dictionary representation of the payload for the event, if any.</returns>
        private static Dictionary<string, string> GetEventPayloadInJson(Event e)
        {
            if (e.GetType().Name.Contains("GotoStateEvent"))
            {
                return null;
            }

            var pe = (PEvent)(e);
            return pe.Payload != null ? ParsePayloadToDictionary(pe.Payload.ToEscapedString()) : null;
        }

        public void OnCompleted()
        {
        }

        public void OnAssertionFailure(string error)
        {
            error = RemoveLogTag(error);
            Writer.AddLogType(JsonWriter.LogTypes.AssertionFailure);
            Writer.AddDetail(JsonWriter.DetailAttr.error, error);
            Writer.AddLog(error);
            Writer.AddToLogs();
        }

        public void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            if (id.Name.Contains("GodMachine") || creatorName.Contains("GodMachine"))
            {
                return;
            }

            var source = creatorName ?? $"task '{Task.CurrentId}'";
            var log = $"{id} was created by {source}.";

            Writer.AddLogType(JsonWriter.LogTypes.CreateActor);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.creatorName, source);
            Writer.AddDetail(JsonWriter.DetailAttr.creatorType, creatorType);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }
        
        /// <inheritdoc/>
        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            if (id.Name.Contains("GodMachine") || creatorName.Contains("GodMachine"))
            {
                return;
            }

            var source = creatorName ?? $"task '{Task.CurrentId}'";
            var log = $"{id} was created by {source}.";

            Writer.AddLogType(JsonWriter.LogTypes.CreateStateMachine);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.creatorName, source);
            Writer.AddDetail(JsonWriter.DetailAttr.creatorType, creatorType);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnCreateTimer(TimerInfo info)
        {
            var source = info.OwnerId?.Name ?? $"task '{Task.CurrentId}'";
            var log = info.Period.TotalMilliseconds >= 0
                ? $"Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms; " +
                  $"period :{info.Period.TotalMilliseconds}ms) was created by {source}."
                : $"Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms) was created by {source}.";

            Writer.AddLogType(JsonWriter.LogTypes.CreateTimer);
            Writer.AddDetail(JsonWriter.DetailAttr.timerInfo, info.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.timerDueTime, info.DueTime.TotalMilliseconds);
            Writer.AddDetail(JsonWriter.DetailAttr.timerPeriod, info.Period.TotalMilliseconds);
            Writer.AddDetail(JsonWriter.DetailAttr.source, source);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
            stateName = GetShortName(stateName);

            var log = stateName is null
                ? $"{id} is executing the default handler."
                : $"{id} is executing the default handler in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.DefaultEventHandler);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.state, stateName);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            var eventName = GetEventNameWithPayload(e);
            var log = stateName is null
                ? $"'{id}' dequeued event '{eventName}'."
                : $"'{id}' dequeued event '{eventName}' in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.DequeueEvent);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.@event, GetShortName(e.GetType().Name));
            Writer.AddDetail(JsonWriter.DetailAttr.state, GetShortName(stateName));
            Writer.AddDetail(JsonWriter.DetailAttr.payload, GetEventPayloadInJson(e));
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                return;
            }

            var log = stateName is null
                ? $"{id} running action '{actionName}' chose to handle exception '{ex.GetType().Name}'."
                : $"{id} running action '{actionName}' in state '{stateName}' chose to handle exception '{ex.GetType().Name}'.";

            Writer.AddLogType(JsonWriter.LogTypes.ExceptionHandled);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.state, GetShortName(stateName));
            Writer.AddDetail(JsonWriter.DetailAttr.action, actionName);
            Writer.AddDetail(JsonWriter.DetailAttr.exception, ex.GetType().Name);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                return;
            }

            var log = stateName is null
                ? $"{id} running action '{actionName}' chose to handle exception '{ex.GetType().Name}'."
                : $"{id} running action '{actionName}' in state '{stateName}' threw exception '{ex.GetType().Name}'.";

            Writer.AddLogType(JsonWriter.LogTypes.ExceptionThrown);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.state, GetShortName(stateName));
            Writer.AddDetail(JsonWriter.DetailAttr.action, actionName);
            Writer.AddDetail(JsonWriter.DetailAttr.exception, ex.GetType().Name);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
        }

        public void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
            if (currentStateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            currentStateName = GetShortName(currentStateName);
            newStateName = GetShortName(newStateName);

            var log =
                $"{id} is transitioning from state '{currentStateName}' to state '{newStateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.GotoState);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.startState, currentStateName);
            Writer.AddDetail(JsonWriter.DetailAttr.endState, newStateName);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnHalt(ActorId id, int inboxSize)
        {
            var log = $"{id} halted with {inboxSize} events in its inbox.";

            Writer.AddLogType(JsonWriter.LogTypes.Halt);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.haltInboxSize, inboxSize);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
        }

        public void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
            currentStateName = string.IsNullOrEmpty(currentStateName) ? "[not recorded]" : currentStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            var log = $"{id} popped state '{currentStateName}' and reentered state '{reenteredStateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.PopState);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.startState, currentStateName);
            Writer.AddDetail(JsonWriter.DetailAttr.endState, reenteredStateName);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
            var log = $"{id} popped state {stateName} due to unhandled event '{e.GetType().Name}'.";

            Writer.AddLogType(JsonWriter.LogTypes.PopStateUnhandledEvent);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.state, stateName);
            Writer.AddDetail(JsonWriter.DetailAttr.@event, e.GetType().Name);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
            var log = $"{id} pushed from state '{currentStateName}' to state '{newStateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.PushState);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.startState, currentStateName);
            Writer.AddDetail(JsonWriter.DetailAttr.endState, newStateName);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnRaiseEvent(ActorId id, string stateName, Event e)
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

            Writer.AddLogType(JsonWriter.LogTypes.RaiseEvent);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.@event, GetShortName(e.GetType().Name));
            Writer.AddDetail(JsonWriter.DetailAttr.state, stateName);
            Writer.AddDetail(JsonWriter.DetailAttr.payload, GetEventPayloadInJson(e));
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            stateName = GetShortName(stateName);
            string eventName = GetEventNameWithPayload(e);
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            var log = stateName is null
                ? $"'{id}' dequeued event '{eventName}'{unblocked}."
                : $"'{id}' dequeued event '{eventName}'{unblocked} in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.ReceiveEvent);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.state, stateName);
            Writer.AddDetail(JsonWriter.DetailAttr.@event, GetShortName(e.GetType().Name));
            Writer.AddDetail(JsonWriter.DetailAttr.wasBlocked, wasBlocked);
            Writer.AddDetail(JsonWriter.DetailAttr.payload, GetEventPayloadInJson(e));
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
            senderStateName = GetShortName(senderStateName);
            string eventName = GetEventNameWithPayload(e);
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = !string.IsNullOrEmpty(senderName)
                ? $"'{senderName}' in state '{senderStateName}'"
                : $"The runtime";
            var log = $"{sender} sent event '{eventName}' to '{targetActorId}'{isHalted}{opGroupIdMsg}.";

            Writer.AddLogType(JsonWriter.LogTypes.SendEvent);
            Writer.AddDetail(JsonWriter.DetailAttr.sender, !string.IsNullOrEmpty(senderName) ? senderName : "Runtime");
            Writer.AddDetail(JsonWriter.DetailAttr.state, senderStateName);
            Writer.AddDetail(JsonWriter.DetailAttr.@event, GetShortName(e.GetType().Name));
            Writer.AddDetail(JsonWriter.DetailAttr.target, targetActorId.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.opGroupId, opGroupId.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.isTargetHalted, isTargetHalted);
            Writer.AddDetail(JsonWriter.DetailAttr.payload, GetEventPayloadInJson(e));
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            stateName = GetShortName(stateName);
            var direction = isEntry ? "enters" : "exits";
            var log = $"{id} {direction} state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.StateTransition);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.state, stateName);
            Writer.AddDetail(JsonWriter.DetailAttr.isEntry, isEntry);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnStopTimer(TimerInfo info)
        {
            var source = info.OwnerId?.Name ?? $"task '{Task.CurrentId}'";
            var log = $"Timer '{info}' was stopped and disposed by {source}.";

            Writer.AddLogType(JsonWriter.LogTypes.StopTimer);
            Writer.AddDetail(JsonWriter.DetailAttr.timerInfo, info.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.source, info.OwnerId?.Name ?? $"{Task.CurrentId}");
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            stateName = GetShortName(stateName);

            var log = stateName is null
                ? $"{id} is waiting to dequeue an event of type '{eventType.FullName}'."
                : $"{id} is waiting to dequeue an event of type '{eventType.FullName}' in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.WaitEvent);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.eventType, eventType.FullName);
            Writer.AddDetail(JsonWriter.DetailAttr.state, stateName);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
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

            Writer.AddLogType(JsonWriter.LogTypes.WaitMultipleEvents);
            Writer.AddDetail(JsonWriter.DetailAttr.id, id.ToString());
            Writer.AddDetail(JsonWriter.DetailAttr.eventTypes, eventTypesNames);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnCreateMonitor(string monitorType)
        {
            monitorType = GetShortName(monitorType);
            var log = $"{monitorType} was created.";

            Writer.AddLogType(JsonWriter.LogTypes.CreateMonitor);
            Writer.AddDetail(JsonWriter.DetailAttr.monitor, monitorType);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
        }

        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            monitorType = GetShortName(monitorType);
            var log = $"{monitorType} is processing event '{GetEventNameWithPayload(e)}' in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.MonitorProcessEvent);
            Writer.AddDetail(JsonWriter.DetailAttr.monitor, monitorType);
            Writer.AddDetail(JsonWriter.DetailAttr.@event, GetShortName(e.GetType().Name));
            Writer.AddDetail(JsonWriter.DetailAttr.state, stateName);
            Writer.AddDetail(JsonWriter.DetailAttr.payload, GetEventPayloadInJson(e));
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            stateName = GetShortName(stateName);
            string eventName = GetEventNameWithPayload(e);
            var log = $"Monitor '{GetShortName(monitorType)}' raised event '{eventName}' in state '{stateName}'.";

            Writer.AddLogType(JsonWriter.LogTypes.MonitorRaiseEvent);
            Writer.AddDetail(JsonWriter.DetailAttr.monitor, monitorType);
            Writer.AddDetail(JsonWriter.DetailAttr.@event, GetShortName(e.GetType().Name));
            Writer.AddDetail(JsonWriter.DetailAttr.state, stateName);
            Writer.AddDetail(JsonWriter.DetailAttr.payload, GetEventPayloadInJson(e));
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

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

            Writer.AddLogType(JsonWriter.LogTypes.MonitorStateTransition);
            Writer.AddDetail(JsonWriter.DetailAttr.monitor, monitorType);
            Writer.AddDetail(JsonWriter.DetailAttr.state, stateName);
            Writer.AddDetail(JsonWriter.DetailAttr.isEntry, isEntry);
            Writer.AddDetail(JsonWriter.DetailAttr.isInHotState, isInHotState);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }

        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }

        public void OnRandom(object result, string callerName, string callerType)
        {
        }

        public void OnStrategyDescription(string strategyName, string description)
        {
            var desc = string.IsNullOrEmpty(description) ? $" Description: {description}" : string.Empty;
            var log = $"Found bug using '{strategyName}' strategy.{desc}";

            Writer.AddLogType(JsonWriter.LogTypes.StrategyDescription);
            Writer.AddDetail(JsonWriter.DetailAttr.strategy, strategyName);
            Writer.AddDetail(JsonWriter.DetailAttr.strategyDescription, description);
            Writer.AddLog(log);
            Writer.AddToLogs();
        }
    }
}