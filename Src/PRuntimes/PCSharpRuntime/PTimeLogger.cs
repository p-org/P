// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using PChecker.Actors;
using PChecker.Actors.Events;
using PChecker.Actors.Logging;
using PChecker.Actors.Timers;
using PChecker.IO.Logging;

namespace Plang.CSharpRuntime
{
    public class PTimeLogger : ActorRuntimeTimeLogCsvFormatter
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="PTimeLogger"/> class.
        /// </summary>
        public PTimeLogger() : base()
        {
        }

        /// <inheritdoc />
        public override void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
        }

        /// <inheritdoc />
        public override void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
        }

        /// <inheritdoc />
        public override void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
        }

        /// <inheritdoc />
        public override void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName, Event e,
            Guid opGroupId, bool isTargetHalted)
        {
            var pe = (PEvent)e;
            var payload = pe.Payload == null ? "null" : pe.Payload.ToEscapedString().Replace(" ", "").Replace(",", "|");
            InMemoryLogger.WriteLine(e.EnqueueTime.GetTime() + ",Send," + e + "," + payload + "," + senderName + "," + senderStateName + "," + targetActorId);
        }

        /// <inheritdoc />
        public override void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public override void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        /// <inheritdoc />
        public override void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            var pe = (PEvent)e;
            var payload = pe.Payload == null ? "null" : pe.Payload.ToEscapedString().Replace(" ", "").Replace(",", "|");
            InMemoryLogger.WriteLine(e.DequeueTime.GetTime() + ",Dequeue," + e + "," + payload + "," + id + "," + stateName + ",null");
        }

        /// <inheritdoc />
        public override void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
        }

        /// <inheritdoc />
        public override void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
        }

        /// <inheritdoc />
        public override void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
        }

        /// <inheritdoc />
        public override void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
        }

        /// <inheritdoc />
        public override void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
        }

        /// <inheritdoc />
        public override void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
        }

        /// <inheritdoc />
        public override void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
        }

        /// <inheritdoc />
        public override void OnDefaultEventHandler(ActorId id, string stateName)
        {
        }

        /// <inheritdoc />
        public override void OnHalt(ActorId id, int inboxSize)
        {
        }

        /// <inheritdoc />
        public override void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public override void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public override void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc />
        public override void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc />
        public override void OnCreateTimer(TimerInfo info)
        {
        }

        /// <inheritdoc />
        public override void OnStopTimer(TimerInfo info)
        {
        }

        /// <inheritdoc />
        public override void OnCreateMonitor(string monitorType)
        {
        }

        /// <inheritdoc />
        public override void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
        }

        /// <inheritdoc />
        public override void OnMonitorProcessEvent(string monitorType, string stateName, string senderName, string senderType,
            string senderStateName, Event e)
        {
        }

        /// <inheritdoc />
        public override void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
        }

        /// <inheritdoc />
        public override void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
        }

        /// <inheritdoc />
        public override void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }

        /// <inheritdoc />
        public override void OnRandom(object result, string callerName, string callerType)
        {
        }

        /// <inheritdoc />
        public override void OnAssertionFailure(string error)
        {
        }

        /// <inheritdoc />
        public override void OnStrategyDescription(string strategyName, string description)
        {
        }
    }
}
