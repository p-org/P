// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using PChecker.Runtime.Events;
using PChecker.Runtime.Logging;
using PChecker.Runtime.StateMachines;

namespace PChecker.Coverage
{
    /// <summary>
    /// This class maintains information about events received and sent from each state of each state machine.
    /// </summary>
    [DataContract]
    public class EventCoverage
    {
        /// <summary>
        /// Map from states to the list of events received by that state. The state id is fully qualified by
        /// the state machine id it belongs to.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, HashSet<string>> EventsReceived = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Map from states to the list of events sent by that state.  The state id is fully qualified by
        /// the state machine id it belongs to.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, HashSet<string>> EventsSent = new Dictionary<string, HashSet<string>>();

        internal void AddEventReceived(string stateId, string eventId)
        {
            if (!EventsReceived.TryGetValue(stateId, out var set))
            {
                set = new HashSet<string>();
                EventsReceived[stateId] = set;
            }

            set.Add(eventId);
        }

        /// <summary>
        /// Get list of events received by the given fully qualified state.
        /// </summary>
        /// <param name="stateId">The state machine qualified state name</param>
        public IEnumerable<string> GetEventsReceived(string stateId)
        {
            if (EventsReceived.TryGetValue(stateId, out var set))
            {
                return set;
            }

            return Array.Empty<string>();
        }

        internal void AddEventSent(string stateId, string eventId)
        {
            if (!EventsSent.TryGetValue(stateId, out var set))
            {
                set = new HashSet<string>();
                EventsSent[stateId] = set;
            }

            set.Add(eventId);
        }

        /// <summary>
        /// Get list of events sent by the given state.
        /// </summary>
        /// <param name="stateId">The state machine qualified state name</param>
        public IEnumerable<string> GetEventsSent(string stateId)
        {
            if (EventsSent.TryGetValue(stateId, out var set))
            {
                return set;
            }

            return Array.Empty<string>();
        }

        internal void Merge(EventCoverage other)
        {
            MergeHashSets(EventsReceived, other.EventsReceived);
            MergeHashSets(EventsSent, other.EventsSent);
        }

        private static void MergeHashSets(Dictionary<string, HashSet<string>> ours, Dictionary<string, HashSet<string>> theirs)
        {
            foreach (var pair in theirs)
            {
                var stateId = pair.Key;
                if (!ours.TryGetValue(stateId, out var eventSet))
                {
                    eventSet = new HashSet<string>();
                    ours[stateId] = eventSet;
                }

                eventSet.UnionWith(pair.Value);
            }
        }
    }

    internal class ControlledRuntimeLogEventCoverage : IControlledRuntimeLog
    {
        private readonly EventCoverage InternalEventCoverage = new EventCoverage();
        private Event Dequeued;

        public EventCoverage EventCoverage => InternalEventCoverage;

        public void OnAssertionFailure(string error)
        {
        }

        public void OnCompleted()
        {
        }

        public void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType)
        {
        }

        public void OnCreateMonitor(string monitorType)
        {
        }

        public void OnDefaultEventHandler(StateMachineId id, string stateName)
        {
            Dequeued = DefaultEvent.Instance;
        }

        public void OnDequeueEvent(StateMachineId id, string stateName, Event e)
        {
            Dequeued = e;
        }

        public void OnEnqueueEvent(StateMachineId id, Event e)
        {
        }

        public void OnExceptionHandled(StateMachineId id, string stateName, string actionName, Exception ex)
        {
        }

        public void OnExceptionThrown(StateMachineId id, string stateName, string actionName, Exception ex)
        {
        }

        public void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName, string actionName)
        {
            OnEventHandled(id, handlingStateName);
        }

        private void OnEventHandled(StateMachineId id, string stateName)
        {
            if (Dequeued != null)
            {
                EventCoverage.AddEventReceived(GetStateId(id.Type, stateName), Dequeued.GetType().FullName);
                Dequeued = null;
            }
        }

        public void OnGotoState(StateMachineId id, string currentStateName, string newStateName)
        {
            OnEventHandled(id, currentStateName);
        }

        public void OnHalt(StateMachineId id, int inboxSize)
        {
        }

        public void OnHandleRaisedEvent(StateMachineId id, string stateName, Event e)
        {
            Dequeued = e;
        }

        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
        }

        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            EventCoverage.AddEventReceived(GetStateId(monitorType, stateName), e.GetType().FullName);
        }

        public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            var eventName = e.GetType().FullName;
            EventCoverage.AddEventSent(GetStateId(monitorType, stateName), eventName);
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

        public void OnRaiseEvent(StateMachineId id, string stateName, Event e)
        {
            var eventName = e.GetType().FullName;
            EventCoverage.AddEventSent(GetStateId(id.Type, stateName), eventName);
        }

        public void OnReceiveEvent(StateMachineId id, string stateName, Event e, bool wasBlocked)
        {
            var eventName = e.GetType().FullName;
            EventCoverage.AddEventReceived(GetStateId(id.Type, stateName), eventName);
        }

        public void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName,
            Event e, bool isTargetHalted)
        {
            var eventName = e.GetType().FullName;
            EventCoverage.AddEventSent(GetStateId(senderType, senderStateName), eventName);
        }

        public void OnStateTransition(StateMachineId id, string stateName, bool isEntry)
        {
        }

        public void OnStrategyDescription(string strategyName, string description)
        {
        }

        public void OnWaitEvent(StateMachineId id, string stateName, Type eventType)
        {
        }

        public void OnWaitEvent(StateMachineId id, string stateName, params Type[] eventTypes)
        {
        }

        private static string GetStateId(string stateMachineType, string stateName)
        {
            var id = ResolvestateMachineTypeName(stateMachineType);
            if (string.IsNullOrEmpty(stateName))
            {
                if (stateMachineType == null)
                {
                    stateName = "ExternalState";
                }
                else
                {
                    stateName = GetLabel(id, null);
                }
            }

            return id += "." + stateName;
        }

        private static string ResolvestateMachineTypeName(string stateMachineType)
        {
            if (stateMachineType == null)
            {
                // The sender id can be null if an event is fired from non-stateMachine code.
                return "ExternalCode";
            }

            return stateMachineType;
        }

        private static string GetLabel(string stateMachineId, string fullyQualifiedName)
        {
            if (fullyQualifiedName.StartsWith(stateMachineId))
            {
                fullyQualifiedName = fullyQualifiedName.Substring(stateMachineId.Length + 1).Trim('+');
            }

            return fullyQualifiedName;
        }
    }
}