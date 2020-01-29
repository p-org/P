using Microsoft.Coyote;
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

            base.OnStateTransition(id, stateName, isEntry);
        }

        public override void OnDefaultEventHandler(ActorId id, string stateName)
        {
            base.OnDefaultEventHandler(id, stateName);
        }

        public override void OnPopState(ActorId id, string currStateName, string restoredStateName)
        {
            base.OnPopState(id, currStateName, restoredStateName);
        }

        public override void OnPopUnhandledEvent(ActorId id, string stateName, Event e)
        {
            base.OnPopUnhandledEvent(id, stateName, e);
        }

        public override void OnPushState(ActorId id, string currStateName, string newStateName)
        {
            base.OnPushState(id, currStateName, newStateName);
        }

        public override void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            base.OnWaitEvent(id, stateName, eventTypes);
        }

        public override void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            base.OnWaitEvent(id, stateName, eventType);
        }

        public override void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName, bool isEntry, bool? isInHotState)
        {
            if (stateName.Contains("__InitState__"))
            {
                return;
            }

            base.OnMonitorStateTransition(monitorTypeName, id, stateName, isEntry, isInHotState);
        }

        public override void OnCreateActor(ActorId id, ActorId creator)
        {
            if (id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnCreateActor(id, creator);
        }

        public override void OnDequeueEvent(ActorId id, string stateName, Event e)
        {

            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnDequeueEvent(id, stateName, e);
        }

        public override void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine") || eventName.Contains("GotoStateEvent"))
            {
                return;
            }

            base.OnRaiseEvent(id, stateName, e);
        }

        public override void OnEnqueueEvent(ActorId id, Event e)
        {
            base.OnEnqueueEvent(id, e);
        }

        public override void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            base.OnReceiveEvent(id, stateName, e, wasBlocked);
        }

        public override void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, Event e)
        {
            base.OnMonitorRaiseEvent(monitorTypeName, id, stateName, e);
        }

        public override void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, Event e, Guid opGroupId, bool isTargetHalted)
        {
            base.OnSendEvent(targetActorId, senderId, senderStateName, e, opGroupId, isTargetHalted);
        }

        public override void OnGotoState(ActorId id, string currStateName, string newStateName)
        {
            if (currStateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnGotoState(id, currStateName, newStateName);
        }

        public override void OnExecuteAction(ActorId id, string stateName, string actionName)
        {
        }

        public override void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName)
        {
        }

        public override void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            base.OnExceptionHandled(id, stateName, actionName, ex);
        }

        public override void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            base.OnExceptionThrown(id, stateName, actionName, ex);
        }
    }
}