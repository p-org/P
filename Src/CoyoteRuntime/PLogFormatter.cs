using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Plang.PrtSharp.Exceptions;
using System;
using System.Linq;

namespace Plang.PrtSharp
{
    /// <summary>
    ///     Formatter for the Coyote runtime log.
    /// </summary>
    public class PLogFormatter : ActorRuntimeLogFormatter
    {
        public PLogFormatter() : base()
        {
        }

        public override bool GetStateTransitionLog(ActorId id, string stateName, bool isEntry, out string text)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                text = string.Empty;
                return false;
            }

            return base.GetStateTransitionLog(id, stateName.Split('.').Last(), isEntry, out text);
        }

        public override bool GetDefaultEventHandlerLog(ActorId id, string stateName, out string text)
        {
            return base.GetDefaultEventHandlerLog(id, stateName.Split('.').Last(), out text);
        }

        public override bool GetPopStateLog(ActorId id, string currStateName, string restoredStateName, out string text)
        {
            return base.GetPopStateLog(id, currStateName.Split('.').Last(), restoredStateName.Split('.').Last(), out text);
        }

        public override bool GetPopUnhandledEventLog(ActorId id, string stateName, string eventName, out string text)
        {
            return base.GetPopUnhandledEventLog(id, stateName.Split('.').Last(), eventName, out text);
        }

        public override bool GetPushStateLog(ActorId id, string currStateName, string newStateName, out string text)
        {
            return base.GetPushStateLog(id, currStateName.Split('.').Last(), newStateName.Split('.').Last(), out text);
        }

        public override bool GetWaitEventLog(ActorId id, string stateName, Type[] eventTypes, out string text)
        {
            return base.GetWaitEventLog(id, stateName.Split('.').Last(), eventTypes, out text);
        }

        public override bool GetWaitEventLog(ActorId id, string stateName, Type eventType, out string text)
        {
            return base.GetWaitEventLog(id, stateName.Split('.').Last(), eventType, out text);
        }

        public override bool GetMonitorStateTransitionLog(string monitorTypeName, ActorId id, string stateName,
            bool isEntry, bool? isInHotState, out string text)
        {
            if (stateName.Contains("__InitState__"))
            {
                text = string.Empty;
                return false;
            }

            return base.GetMonitorStateTransitionLog(monitorTypeName, id, stateName.Split('.').Last(), isEntry, isInHotState, out text);
        }

        public override bool GetCreateActorLog(ActorId id, ActorId creator, out string text)
        {
            if (id.Name.Contains("GodMachine"))
            {
                text = string.Empty;
                return false;
            }

            return base.GetCreateActorLog(id, creator, out text);
        }

        public override bool GetDequeueEventLog(ActorId id, string stateName, string eventName, out string text)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                text = string.Empty;
                return false;
            }

            return base.GetDequeueEventLog(id, stateName.Split('.').Last(), eventName.Split('.').Last(), out text);
        }

        public override bool GetRaiseEventLog(ActorId id, string stateName, string eventName, out string text)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine") || eventName.Contains("GotoStateEvent"))
            {
                text = string.Empty;
                return false;
            }

            return base.GetRaiseEventLog(id, stateName.Split('.').Last(), eventName.Split('.').Last(), out text);
        }

        public override bool GetEnqueueEventLog(ActorId id, string eventName, out string text)
        {
            return base.GetEnqueueEventLog(id, eventName.Split('.').Last(), out text);
        }

        public override bool GetReceiveEventLog(ActorId id, string stateName, string eventName, bool wasBlocked, out string text)
        {
            return base.GetReceiveEventLog(id, stateName.Split('.').Last(), eventName.Split('.').Last(), wasBlocked, out text);
        }

        public override bool GetMonitorRaiseEventLog(string monitorTypeName, ActorId id, string stateName, string eventName, out string text)
        {
            return base.GetMonitorRaiseEventLog(monitorTypeName, id, stateName.Split('.').Last(), eventName.Split('.').Last(), out text);
        }

        public override bool GetSendEventLog(ActorId targetActorId, ActorId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted, out string text)
        {
            return base.GetSendEventLog(targetActorId, senderId, senderStateName.Split('.').Last(), eventName.Split('.').Last(), opGroupId, isTargetHalted, out text);
        }

        public override bool GetGotoStateLog(ActorId id, string currStateName, string newStateName, out string text)
        {
            if (currStateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                text = string.Empty;
                return false;
            }

            return base.GetGotoStateLog(id, currStateName.Split('.').Last(), newStateName.Split('.').Last(), out text);
        }

        public override bool GetExecuteActionLog(ActorId id, string stateName, string actionName, out string text)
        {
            text = string.Empty;
            return false;
        }

        public override bool GetMonitorExecuteActionLog(string monitorTypeName, ActorId id,
            string stateName, string actionName, out string text)
        {
            text = string.Empty;
            return false;
        }

        public override bool GetExceptionHandledLog(ActorId id, string stateName, string actionName, Exception ex, out string text)
        {
            if (ex is PNonStandardReturnException)
            {
                text = string.Empty;
                return false;
            }

            return base.GetExceptionHandledLog(id, stateName.Split('.').Last(), actionName, ex, out text);
        }

        public override bool GetExceptionThrownLog(ActorId id, string stateName, string actionName, Exception ex, out string text)
        {
            if (ex is PNonStandardReturnException)
            {
                text = string.Empty;
                return false;
            }

            return base.GetExceptionThrownLog(id, stateName.Split('.').Last(), actionName, ex, out text);
        }
    }
}