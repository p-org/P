using System;
using System.Linq;
using PChecker.StateMachines;
using PChecker.StateMachines.Events;
using PChecker.StateMachines.Logging;
using PChecker.PRuntime.Exceptions;

namespace PChecker.PRuntime
{
    /// <summary>
    ///     Formatter for the runtime log.
    /// </summary>
    public class PLogFormatter : PCheckerLogTextFormatter
    {
        public PLogFormatter() : base()
        {
        }

        private string GetShortName(string name)
        {
            return name.Split('.').Last();
        }

        private string GetEventNameWithPayload(Event e)
        {
            if (e.GetType().Name.Contains("GotoStateEvent"))
            {
                return e.GetType().Name;
            }
            var pe = (PEvent)(e);
            var payload = pe.Payload == null ? "null" : pe.Payload.ToEscapedString();
            var msg = pe.Payload == null ? "" : $" with payload ({payload})";
            return $"{GetShortName(e.GetType().Name)}{msg}";
        }

        public override void OnStateTransition(StateMachineId id, string stateName, bool isEntry)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnStateTransition(id, GetShortName(stateName), isEntry);
        }

        public override void OnPopStateUnhandledEvent(StateMachineId id, string stateName, Event e)
        {
            base.OnPopStateUnhandledEvent(id, GetShortName(stateName), e);
        }

        public override void OnDefaultEventHandler(StateMachineId id, string stateName)
        {
            base.OnDefaultEventHandler(id, GetShortName(stateName));
        }

        public override void OnWaitEvent(StateMachineId id, string stateName, params Type[] eventTypes)
        {
            base.OnWaitEvent(id, GetShortName(stateName), eventTypes);
        }

        public override void OnWaitEvent(StateMachineId id, string stateName, Type eventType)
        {
            base.OnWaitEvent(id, GetShortName(stateName), eventType);
        }

        public override void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            if (stateName.Contains("__InitState__"))
            {
                return;
            }

            base.OnMonitorStateTransition(monitorType: GetShortName(monitorType), stateName: GetShortName(stateName), isEntry: isEntry, isInHotState: isInHotState);
        }
        
        public override void OnMonitorProcessEvent(string monitorType, string stateName, string senderName, string senderType,
            string senderStateName, Event e)
        {
            var text = $"<MonitorLog> {GetShortName(monitorType)} is processing event '{GetEventNameWithPayload(e)}' in state '{stateName}'.";
            Logger.WriteLine(text);
        }

        public override void OnDequeueEvent(StateMachineId id, string stateName, Event e)
        {
            if (stateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            stateName = GetShortName(stateName);
            var eventName = GetEventNameWithPayload(e);
            string text = null;
            if (stateName is null)
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}'.";
            }
            else
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}' in state '{stateName}'.";
            }

            Logger.WriteLine(text);
        }

        public override void OnRaiseEvent(StateMachineId id, string stateName, Event e)
        {
            stateName = GetShortName(stateName);
            var eventName = GetEventNameWithPayload(e);
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

            Logger.WriteLine(text);
        }

        public override void OnEnqueueEvent(StateMachineId id, Event e) {   }

        public override void OnReceiveEvent(StateMachineId id, string stateName, Event e, bool wasBlocked)
        {
            stateName = GetShortName(stateName);
            var eventName = GetEventNameWithPayload(e);
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

            Logger.WriteLine(text);
        }

        public override void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            stateName = GetShortName(stateName);
            var eventName = GetEventNameWithPayload(e);
            var text = $"<MonitorLog> Monitor '{GetShortName(monitorType)}' raised event '{eventName}' in state '{stateName}'.";
            Logger.WriteLine(text);
        }

        public override void OnSendEvent(StateMachineId targetStateMachineId, string senderName, string senderType, string senderStateName, Event e, Guid opGroupId, bool isTargetHalted)
        {
            senderStateName = GetShortName(senderStateName);
            var eventName = GetEventNameWithPayload(e);
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = !string.IsNullOrEmpty(senderName) ? $"'{senderName}' in state '{senderStateName}'" : $"The runtime";
            var text = $"<SendLog> {sender} sent event '{eventName}' to '{targetStateMachineId}'{isHalted}{opGroupIdMsg}.";
            Logger.WriteLine(text);
        }

        public override void OnGotoState(StateMachineId id, string currStateName, string newStateName)
        {
            if (currStateName.Contains("__InitState__") || id.Name.Contains("GodMachine"))
            {
                return;
            }

            base.OnGotoState(id, GetShortName(currStateName), GetShortName(newStateName));
        }

        public override void OnExecuteAction(StateMachineId id, string handlingStateName, string currentStateName, string actionName)
        {
        }

        public override void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
        }

        public override void OnExceptionHandled(StateMachineId id, string stateName, string actionName, Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                return;
            }
            base.OnExceptionHandled(id: id, stateName: GetShortName(stateName), actionName: actionName, ex: ex);
        }

        public override void OnExceptionThrown(StateMachineId id, string stateName, string actionName, Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                return;
            }
            base.OnExceptionThrown(id: id, stateName: GetShortName(stateName), actionName: actionName, ex: ex);
        }

        public override void OnCreateMonitor(string monitorType)
        {
            base.OnCreateMonitor(GetShortName(monitorType));
        }

        public override void OnHandleRaisedEvent(StateMachineId id, string stateName, Event e)
        {
        }

        public override void OnRandom(object result, string callerName, string callerType)
        {
        }
    }
}