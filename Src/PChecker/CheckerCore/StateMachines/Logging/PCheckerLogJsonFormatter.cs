// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using PChecker.StateMachines.Events;

namespace PChecker.StateMachines.Logging
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

        /// <inheritdoc />
        public virtual void OnCompleted()
        {
        }

        /// <inheritdoc />
        public virtual void OnAssertionFailure(string error)
        {
        }
        
        /// <inheritdoc />
        public virtual void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType)
        {
        }

        /// <inheritdoc />
        public virtual void OnDefaultEventHandler(StateMachineId id, string stateName)
        {
        }

        /// <inheritdoc />
        public virtual void OnDequeueEvent(StateMachineId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnEnqueueEvent(StateMachineId id, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnExceptionHandled(StateMachineId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc />
        public virtual void OnExceptionThrown(StateMachineId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc />
        public virtual void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName,
            string actionName)
        {
        }

        /// <inheritdoc />
        public virtual void OnGotoState(StateMachineId id, string currentStateName, string newStateName)
        {
        }

        /// <inheritdoc />
        public virtual void OnHalt(StateMachineId id, int inboxSize)
        {
        }

        /// <inheritdoc />
        public virtual void OnHandleRaisedEvent(StateMachineId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnPopState(StateMachineId id, string currentStateName, string restoredStateName)
        {
        }

        /// <inheritdoc />
        public virtual void OnPopStateUnhandledEvent(StateMachineId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnPushState(StateMachineId id, string currentStateName, string newStateName)
        {
        }

        /// <inheritdoc />
        public virtual void OnRaiseEvent(StateMachineId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnReceiveEvent(StateMachineId id, string stateName, Event e, bool wasBlocked)
        {
        }

        /// <inheritdoc />
        public virtual void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType,
            string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
        }

        /// <inheritdoc />
        public virtual void OnStateTransition(StateMachineId id, string stateName, bool isEntry)
        {
        }

        /// <inheritdoc />
        public virtual void OnWaitEvent(StateMachineId id, string stateName, Type eventType)
        {
        }

        /// <inheritdoc />
        public virtual void OnWaitEvent(StateMachineId id, string stateName, params Type[] eventTypes)
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
        public virtual void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry,
            bool? isInHotState)
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
        public virtual void OnStrategyDescription(string strategyName, string description)
        {
        }
    }
}