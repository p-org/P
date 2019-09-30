using System;
using System.Linq;
using Microsoft.PSharp;
using Microsoft.PSharp.IO;
using Plang.PrtSharp.Exceptions;

namespace Plang.PrtSharp
{
    /// <summary>
    ///     Logger that writes text to the console.
    /// </summary>
    public class PLogger : RuntimeLogWriter
    {
        public PLogger() : base()
        {

        }
        public override void OnMachineState(MachineId machineId, string stateName, bool isEntry)
        {
            if (stateName.Contains("__InitState__") || machineId.Name.Contains("GodMachine"))
                return;
            base.OnMachineState(machineId, stateName.Split('.').Last(), isEntry);
        }
        public override void OnDefault(MachineId machineId, string currStateName)
        {
            base.OnDefault(machineId, currStateName.Split('.').Last());
        }
        public override void OnPop(MachineId machineId, string currStateName, string restoredStateName)
        {
            base.OnPop(machineId, currStateName.Split('.').Last(), restoredStateName.Split('.').Last());
        }
        public override void OnPopUnhandledEvent(MachineId machineId, string currStateName, string eventName)
        {
            base.OnPopUnhandledEvent(machineId, currStateName.Split('.').Last(), eventName);
        }

        public override void OnPush(MachineId machineId, string currStateName, string newStateName)
        {
            base.OnPush(machineId, currStateName.Split('.').Last(), newStateName.Split('.').Last());
        }

        public override void OnWait(MachineId machineId, string currStateName, params Type[] eventTypes)
        {
            base.OnWait(machineId, currStateName.Split('.').Last(), eventTypes);
        }
        public override void OnWait(MachineId machineId, string currStateName, Type eventType)
        {
            base.OnWait(machineId, currStateName.Split('.').Last(), eventType);
        }
        public override void OnMonitorState(string monitorTypeName, MachineId monitorId, string stateName, bool isEntry, bool? isInHotState)
        {
            if (stateName.Contains("__InitState__"))
                return;

            base.OnMonitorState(monitorTypeName, monitorId, stateName.Split('.').Last(), isEntry, isInHotState);
        }
        public override void OnCreateMachine(MachineId machineId, MachineId creator)
        {
            if (machineId.Name.Contains("GodMachine"))
                return;

            base.OnCreateMachine(machineId, creator);
        }

        public override void OnDequeue(MachineId machineId, string currStateName, string eventName)
        {
            if (currStateName.Contains("__InitState__") || machineId.Name.Contains("GodMachine"))
                return;
            base.OnDequeue(machineId, currStateName.Split('.').Last(), eventName.Split('.').Last());
        }

        public override void OnMachineEvent(MachineId machineId, string currStateName, string eventName)
        {
            if (currStateName.Contains("__InitState__") || machineId.Name.Contains("GodMachine") || eventName.Contains("GotoStateEvent"))
                return;
            base.OnMachineEvent(machineId, currStateName.Split('.').Last(), eventName.Split('.').Last());
        }

        public override void OnEnqueue(MachineId machineId, string eventName)
        {
            base.OnEnqueue(machineId, eventName.Split('.').Last());
        }
        public override void OnReceive(MachineId machineId, string currStateName, string eventName, bool wasBlocked)
        {
            base.OnReceive(machineId, currStateName.Split('.').Last(), eventName.Split('.').Last(), wasBlocked);
        }
        public override void OnMonitorEvent(string monitorTypeName, MachineId monitorId, string currStateName, string eventName, bool isProcessing)
        {
            base.OnMonitorEvent(monitorTypeName, monitorId, currStateName.Split('.').Last(), eventName.Split('.').Last(), isProcessing);
        }
        public override void OnSend(MachineId targetMachineId, MachineId senderId, string senderStateName, string eventName, Guid opGroupId, bool isTargetHalted)
        {
            base.OnSend(targetMachineId, senderId, senderStateName.Split('.').Last(), eventName.Split('.').Last(), opGroupId, isTargetHalted);
        }
        public override void OnGoto(MachineId machineId, string currStateName, string newStateName)
        {
            if (currStateName.Contains("__InitState__") || machineId.Name.Contains("GodMachine"))
                return;

            base.OnGoto(machineId, currStateName.Split('.').Last(), newStateName.Split('.').Last());
        }

        public override void OnMachineAction(MachineId machineId, string currStateName, string actionName)
        {
        }

        public override void OnMonitorAction(string monitorTypeName, MachineId monitorId, string currStateName, string actionName)
        {
           
        }
        public override void OnMachineExceptionHandled(MachineId machineId, string currentStateName, string actionName,
            Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                //ignore logging
            }
            else
            {
                base.OnMachineExceptionThrown(machineId, currentStateName.Split('.').Last(), actionName, ex);
            }
        }

        public override void OnMachineExceptionThrown(MachineId machineId, string currentStateName, string actionName,
            Exception ex)
        {
            if (ex is PNonStandardReturnException)
            {
                //ignore logging
            }
            else
            {
                base.OnMachineExceptionThrown(machineId, currentStateName.Split('.').Last(), actionName, ex);
            }
        }
    }
}