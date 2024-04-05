// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using PChecker.Actors.Events;

namespace PChecker.Actors.Logging
{
    /// <summary>
    /// This class implements IActorRuntimeLog and generates log output in an XML format.
    /// </summary>
    public class ActorRuntimeLogJsonFormatter : IActorRuntimeLog
    {
        /// <summary>
        /// Get or set the JsonWriter to write to.
        /// </summary>
        public JsonWriter Writer { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntimeLogJsonFormatter"/> class.
        /// </summary>
        protected ActorRuntimeLogJsonFormatter()
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
        public virtual void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
        }

        /// <inheritdoc />
        public virtual void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
        }

        /// <inheritdoc />
        public virtual void OnDefaultEventHandler(ActorId id, string stateName)
        {
        }

        /// <inheritdoc />
        public virtual void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc />
        public virtual void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc />
        public virtual void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName,
            string actionName)
        {
        }

        /// <inheritdoc />
        public virtual void OnGotoState(ActorId id, string currentStateName, string newStateName)
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
        public virtual void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
        }

        /// <inheritdoc />
        public virtual void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
        }

        /// <inheritdoc />
        public virtual void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public virtual void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
        }

        /// <inheritdoc />
        public virtual void OnSendEvent(ActorId targetActorId, string senderName, string senderType,
            string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
        }

        /// <inheritdoc />
        public virtual void OnStateTransition(ActorId id, string stateName, bool isEntry)
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