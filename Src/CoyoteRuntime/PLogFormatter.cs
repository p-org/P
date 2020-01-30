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

        private string GetShortName(string stateName)
        {
            return stateName.Split('.').Last();
        }
        private string GetEventName(Event e)
        {
            return e.GetType().Name;
        }

        public override void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }
            
            base.OnStateTransition(id, this.GetShortName(stateName), isEntry);
        }

        public override void OnDefaultEventHandler(ActorId id, string stateName)
        {
            base.OnDefaultEventHandler(id, this.GetShortName(stateName));
        }

        public override void OnPopState(ActorId id, string currStateName, string restoredStateName)
        {
            base.OnPopState(id, this.GetShortName(currStateName), this.GetShortName(restoredStateName));
        }

        public override void OnPopUnhandledEvent(ActorId id, string stateName, Event e)
        {
            stateName = this.GetShortName(stateName);
            string eventName = this.GetEventName(e);
            var reenteredStateName = string.IsNullOrEmpty(stateName)
                ? string.Empty
                : $" and reentered state '{stateName}";
            var text = $"<PopLog> '{id}' popped with unhandled event '{eventName}'{reenteredStateName}.";
            this.Logger.WriteLine(text);
        }

        public override void OnPushState(ActorId id, string currStateName, string newStateName)
        {
            base.OnPushState(id, this.GetShortName(currStateName), this.GetShortName(newStateName));
        }

        public override void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            base.OnWaitEvent(id, this.GetShortName(stateName), eventTypes);
        }

        public override void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            base.OnWaitEvent(id, this.GetShortName(stateName), eventType);
        }

        public override void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName, bool isEntry, bool? isInHotState)
        {
            if (stateName.Contains("__InitState__"))
            {
                return;
            }

            base.OnMonitorStateTransition(monitorTypeName, id, this.GetShortName(stateName), isEntry, isInHotState);
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

            stateName = this.GetShortName(stateName);
            string eventName = this.GetEventName(e);
            string text = null;
            if (stateName is null)
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}'.";
            }
            else
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        public override void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            stateName = this.GetShortName(stateName);
            string eventName = this.GetEventName(e);
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine") || eventName.Contains("GotoStateEvent"))
            {
                return;
            }

            string text = null;
            if (stateName is null)
            {
                text = $"<RaiseLog> '{id}' raised event '{eventName}'.";
            }
            else
            {
                text = $"<RaiseLog> '{id}' raised event '{eventName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        public override void OnEnqueueEvent(ActorId id, Event e)
        {
            string eventName = this.GetEventName(e);
            string text = $"<EnqueueLog> '{id}' enqueued event '{eventName}'.";
            this.Logger.WriteLine(text);
        }

        public override void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            stateName = this.GetShortName(stateName);
            string eventName = this.GetEventName(e);
            string text = null;
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            if (stateName is null)
            {
                text = $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked}.";
            }
            else
            {
                text = $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked} in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        public override void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, Event e)
        {
            stateName = this.GetShortName(stateName);
            string eventName = this.GetEventName(e);
            string text = $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' raised event '{eventName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        public override void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, Event e, Guid opGroupId, bool isTargetHalted)
        {
            senderStateName = this.GetShortName(senderStateName);
            string eventName = this.GetEventName(e);
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = senderId != null ? $"'{senderId}' in state '{senderStateName}'" : $"The runtime";
            var text = $"<SendLog> {sender} sent event '{eventName}' to '{targetActorId}'{isHalted}{opGroupIdMsg}.";
            this.Logger.WriteLine(text);
        }

        public override void OnGotoState(ActorId id, string currStateName, string newStateName)
        {
            if (currStateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnGotoState(id, this.GetShortName(currStateName), this.GetShortName(newStateName));
        }

        public override void OnExecuteAction(ActorId id, string stateName, string actionName)
        {
        }

        public override void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName)
        {
        }

        public override void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            base.OnExceptionHandled(id, this.GetShortName(stateName), actionName, ex);
        }

        public override void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            base.OnExceptionThrown(id, this.GetShortName(stateName), actionName, ex);
        }
    }
}