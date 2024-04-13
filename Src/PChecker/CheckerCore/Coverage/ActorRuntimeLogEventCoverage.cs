// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using PChecker.Actors;
using PChecker.Actors.Events;
using PChecker.Actors.Logging;

namespace PChecker.Coverage
{
    /// <summary>
    /// This class maintains information about events received and sent from each state of each actor.
    /// </summary>
    [DataContract]
    public class EventCoverage
    {
        /// <summary>
        /// Map from states to the list of events received by that state.  The state id is fully qualified by
        /// the actor id it belongs to.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, HashSet<string>> EventsReceived = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Map from states to the list of events sent by that state.  The state id is fully qualified by
        /// the actor id it belongs to.
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
        /// <param name="stateId">The actor qualified state name</param>
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
        /// <param name="stateId">The actor qualified state name</param>
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

    internal class ActorRuntimeLogEventCoverage : IActorRuntimeLog
    {
        private readonly EventCoverage InternalEventCoverage = new EventCoverage();
        private Event Dequeued;

        public ActorRuntimeLogEventCoverage()
        {
        }

        public EventCoverage EventCoverage => InternalEventCoverage;

        public void OnAssertionFailure(string error)
        {
        }

        public void OnCompleted()
        {
        }

        public void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
        }

        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
        }

        public void OnCreateMonitor(string monitorType)
        {
        }

        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
            Dequeued = DefaultEvent.Instance;
        }

        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            Dequeued = e;
        }

        public void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            OnEventHandled(id, handlingStateName);
        }

        private void OnEventHandled(ActorId id, string stateName)
        {
            if (Dequeued != null)
            {
                EventCoverage.AddEventReceived(GetStateId(id.Type, stateName), Dequeued.GetType().FullName);
                Dequeued = null;
            }
        }

        public void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
            OnEventHandled(id, currentStateName);
        }

        public void OnHalt(ActorId id, int inboxSize)
        {
        }

        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
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

        public void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
        }

        public void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
        }

        public void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
            OnEventHandled(id, currentStateName);
        }

        public void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            var eventName = e.GetType().FullName;
            EventCoverage.AddEventSent(GetStateId(id.Type, stateName), eventName);
        }

        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            var eventName = e.GetType().FullName;
            EventCoverage.AddEventReceived(GetStateId(id.Type, stateName), eventName);
        }

        public void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
            var eventName = e.GetType().FullName;
            EventCoverage.AddEventSent(GetStateId(senderType, senderStateName), eventName);
        }

        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
        }

        public void OnStrategyDescription(string strategyName, string description)
        {
        }

        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
        }

        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
        }

        private static string GetStateId(string actorType, string stateName)
        {
            var id = ResolveActorTypeName(actorType);
            if (string.IsNullOrEmpty(stateName))
            {
                if (actorType == null)
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

        private static string ResolveActorTypeName(string actorType)
        {
            if (actorType == null)
            {
                // The sender id can be null if an event is fired from non-actor code.
                return "ExternalCode";
            }

            return actorType;
        }

        private static string GetLabel(string actorId, string fullyQualifiedName)
        {
            if (fullyQualifiedName == null)
            {
                // then this is probably an Actor, not a StateMachine.  For Actors we can invent a state
                // name equal to the short name of the class, this then looks like a Constructor which is fine.
                var pos = actorId.LastIndexOf(".");
                if (pos > 0)
                {
                    return actorId.Substring(pos + 1);
                }

                return actorId;
            }

            if (fullyQualifiedName.StartsWith(actorId))
            {
                fullyQualifiedName = fullyQualifiedName.Substring(actorId.Length + 1).Trim('+');
            }

            return fullyQualifiedName;
        }
    }
}