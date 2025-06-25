// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using PChecker.Coverage.Common;
using PChecker.Runtime.Events;
using PChecker.Runtime.Logging;
using PChecker.Runtime.StateMachines;

namespace PChecker.Coverage.Event
{
    /// <summary>
    /// Implementation of IControlledRuntimeLog that tracks event coverage.
    /// </summary>
    internal class ControlledRuntimeLogEventCoverage : IControlledRuntimeLog
    {
        private readonly EventCoverage InternalEventCoverage = new EventCoverage();
        private Runtime.Events.Event Dequeued;

        /// <summary>
        /// Gets the event coverage data collected by this logger.
        /// </summary>
        public EventCoverage EventCoverage => InternalEventCoverage;

        public void OnAssertionFailure(string error)
        {
        }

        public void OnCompleted()
        {
        }

        public void OnAnnouceEvent(string machineName, Runtime.Events.Event @event)
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

        public void OnDequeueEvent(StateMachineId id, string stateName, Runtime.Events.Event e)
        {
            Dequeued = e;
        }

        public void OnEnqueueEvent(StateMachineId id, Runtime.Events.Event e)
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

        public void OnHandleRaisedEvent(StateMachineId id, string stateName, Runtime.Events.Event e)
        {
            Dequeued = e;
        }

        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
        }

        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Runtime.Events.Event e)
        {
            EventCoverage.AddEventReceived(GetStateId(monitorType, stateName), e.GetType().FullName);
        }

        public void OnMonitorRaiseEvent(string monitorType, string stateName, Runtime.Events.Event e)
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

        public void OnRaiseEvent(StateMachineId id, string stateName, Runtime.Events.Event e)
        {
            var eventName = e.GetType().FullName;
            EventCoverage.AddEventSent(GetStateId(id.Type, stateName), eventName);
        }

        public void OnReceiveEvent(StateMachineId id, string stateName, Runtime.Events.Event e, bool wasBlocked)
        {
            var eventName = e.GetType().FullName;
            EventCoverage.AddEventReceived(GetStateId(id.Type, stateName), eventName);
        }

        public void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName,
            Runtime.Events.Event e, bool isTargetHalted)
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
            return CoverageUtilities.GetStateId(stateMachineType, stateName);
        }
    }
}
