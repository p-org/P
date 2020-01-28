using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Logging;
using Plang.PrtSharp.Exceptions;
using System;
using System.Linq;

namespace Plang.PrtSharp
{
    /// <summary>
    ///     Formatter for the Coyote runtime log.
    /// </summary>
    public class PLogFormatter : ActorRuntimeLogTextFormatter
    {
        public PLogFormatter() : base()
        {
        }

        public override void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnStateTransition(id, stateName.Split('.').Last(), isEntry);
        }

        public override void OnDefaultEventHandler(ActorId id, string stateName)
        {
            base.OnDefaultEventHandler(id, stateName.Split('.').Last());
        }

        public override void OnPopState(ActorId id, string currStateName, string restoredStateName)
        {
            base.OnPopState(id, currStateName.Split('.').Last(), restoredStateName.Split('.').Last());
        }

        public override void OnPopUnhandledEvent(ActorId id, string stateName, string eventName)
        {
            base.OnPopUnhandledEvent(id, stateName.Split('.').Last(), eventName);
        }

        public override void OnPushState(ActorId id, string currStateName, string newStateName)
        {
            base.OnPushState(id, currStateName.Split('.').Last(), newStateName.Split('.').Last());
        }

        public override void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            base.OnWaitEvent(id, stateName.Split('.').Last(), eventTypes);
        }

        public override void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            base.OnWaitEvent(id, stateName.Split('.').Last(), eventType);
        }

        public override void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName, bool isEntry, bool? isInHotState)
        {
            if (stateName.Contains("__InitState__"))
            {
                return;
            }

            base.OnMonitorStateTransition(monitorTypeName, id, stateName.Split('.').Last(), isEntry, isInHotState);
        }

        public override void OnCreateActor(ActorId id, ActorId creator)
        {
            if (id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnCreateActor(id, creator);
        }

        public override void OnDequeueEvent(ActorId id, string stateName, string eventName)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnDequeueEvent(id, stateName.Split('.').Last(), eventName.Split('.').Last());
        }

        public override void OnRaiseEvent(ActorId id, string stateName, string eventName)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine") || eventName.Contains("GotoStateEvent"))
            {
                return;
            }

            base.OnRaiseEvent(id, stateName.Split('.').Last(), eventName.Split('.').Last());
        }

        public override void OnEnqueueEvent(ActorId id, string eventName)
        {
            base.OnEnqueueEvent(id, eventName.Split('.').Last());
        }

        public override void OnReceiveEvent(ActorId id, string stateName, string eventName, bool wasBlocked)
        {
            base.OnReceiveEvent(id, stateName.Split('.').Last(), eventName.Split('.').Last(), wasBlocked);
        }

        public override void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, string eventName)
        {
            base.OnMonitorRaiseEvent(monitorTypeName, id, stateName.Split('.').Last(), eventName.Split('.').Last());
        }

        public override void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName, Guid opGroupId, bool isTargetHalted)
        {
            base.OnSendEvent(targetActorId, senderId, senderStateName.Split('.').Last(), eventName.Split('.').Last(), opGroupId, isTargetHalted);
        }

        public override void OnGotoState(ActorId id, string currStateName, string newStateName)
        {
            if (currStateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnGotoState(id, currStateName.Split('.').Last(), newStateName.Split('.').Last());
        }

        public override void OnExecuteAction(ActorId id, string stateName, string actionName)
        {
        }

        public override void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName)
        {
        }

        public override void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            base.OnExceptionHandled(id, stateName.Split('.').Last(), actionName, ex);
        }

        public override void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            base.OnExceptionThrown(id, stateName.Split('.').Last(), actionName, ex);
        }
    }
}