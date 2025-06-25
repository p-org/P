// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using PChecker.Runtime.Logging;
using PChecker.Runtime.StateMachines;
using PChecker.SystematicTesting;
using PChecker.Utilities;

namespace PChecker.Coverage.Code
{
    /// <summary>
    /// Implementation of <see cref="IControlledRuntimeLog"/> that logs code coverage information.
    /// This class receives code coverage events and aggregates them into a CodeCoverage object.
    /// </summary>
    public class ControlledRuntimeLogCodeCoverage : IControlledRuntimeLog
    {
        /// <summary>
        /// The code coverage instance being built.
        /// </summary>
        private readonly CodeCoverage _codeCoverage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledRuntimeLogCodeCoverage"/> class.
        /// </summary>
        public ControlledRuntimeLogCodeCoverage()
        {
            _codeCoverage = new CodeCoverage();
        }

        /// <summary>
        /// Logs a code coverage event.
        /// </summary>
        /// <param name="label">The coverage label.</param>
        /// <param name="codeLocation">The code location.</param>
        /// <param name="customPayload">Optional custom payload data.</param>
        public void LogCodeCoverage(string label, string codeLocation, string customPayload = null)
        {
            if (string.IsNullOrEmpty(label))
            {
                label = string.Empty;
            }

            if (string.IsNullOrEmpty(codeLocation))
            {
                codeLocation = string.Empty;
            }

            // Use the correct API to record coverage metric
            _codeCoverage.RecordCoverageMetric(label, codeLocation, customPayload);
        }

        /// <summary>
        /// Gets the code coverage information collected by this logger.
        /// </summary>
        public CodeCoverage GetCodeCoverage()
        {
            return _codeCoverage;
        }

        #region IControlledRuntimeLog implementation

        public void OnEmitCoverage(string label, string location, string payload = null)
        {
            this.LogCodeCoverage(label, location, payload);
        }

        public void OnCreateEvent(Runtime.Events.Event theEvent, string creatorName, string creatorType)
        {
            // Not used for code coverage
        }

        public void OnEnqueueEvent(StateMachineId targetMachine, Runtime.Events.Event theEvent)
        {
            // Not used for code coverage
        }

        public void OnDequeueEvent(StateMachineId machine, string state, Runtime.Events.Event theEvent)
        {
            // Not used for code coverage
        }

        public void OnReceiveEvent(StateMachineId machine, string state, Runtime.Events.Event theEvent, bool wasBlocked)
        {
            // Not used for code coverage
        }

        public void OnWaitEvent(StateMachineId machine, string state, Type eventType)
        {
            // Not used for code coverage
        }

        public void OnWaitEvent(StateMachineId machine, string state, params Type[] eventTypes)
        {
            // Not used for code coverage
        }

        public void OnDropEvent(StateMachineId machine, string state, Runtime.Events.Event theEvent)
        {
            // Not used for code coverage
        }

        public void OnMonitorRaisesEvent(string monitorType, string state, Runtime.Events.Event theEvent)
        {
            // Not used for code coverage
        }
        
        public void OnMonitorRaiseEvent(string monitorType, string state, Runtime.Events.Event theEvent)
        {
            // Not used for code coverage
        }

        public void OnCreateMonitor(string monitorType)
        {
            // Not used for code coverage
        }

        public void OnMonitorProcessEvent(string monitorType, string state, string senderName, string senderType, string senderStateName, Runtime.Events.Event theEvent)
        {
            // Not used for code coverage
        }

        // This method is already properly implemented above with the correct signature

        public void OnCreateStateMachine(StateMachineId machine, string creatorName, string creatorType)
        {
            // Not used for code coverage
        }

        public void OnStateTransition(StateMachineId machine, string stateName, bool isEntry)
        {
            // Not used for code coverage
        }

        public void OnGotoState(StateMachineId machine, string oldState, string newState)
        {
            // Not used for code coverage
        }

        public void OnMachineState(StateMachineId machine, string state, string operationName)
        {
            // Not used for code coverage
        }

        public void OnHalt(StateMachineId machine, int inboxSize)
        {
            // Not used for code coverage
        }

        public void OnHandleRaisedEvent(StateMachineId machine, string state, Runtime.Events.Event theEvent)
        {
            // Not used for code coverage
        }

        public void OnDefaultEventHandler(StateMachineId machine, string state)
        {
            // Not used for code coverage
        }

        public void OnExecuteAction(StateMachineId machine, string state, string currentState, string actionName)
        {
            // Not used for code coverage
        }

        public void OnSendEvent(StateMachineId targetMachine, string senderName, string senderType, string senderStateName, Runtime.Events.Event theEvent, bool isTargetHalted)
        {
            // Not used for code coverage
        }

        public void OnRaiseEvent(StateMachineId machine, string state, Runtime.Events.Event theEvent)
        {
            // Not used for code coverage
        }

        public void OnExceptionThrown(StateMachineId machine, string state, string actionName, Exception ex)
        {
            // Not used for code coverage
        }

        public void OnExceptionHandled(StateMachineId machine, string state, string actionName, Exception ex)
        {
            // Not used for code coverage
        }

        public void OnAssertionFailure(string text)
        {
            // Not used for code coverage
        }

        public void OnMonitorExecuteAction(string monitorType, string state, string actionName)
        {
            // Not used for code coverage
        }

        public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            // Not used for code coverage
        }

        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
            // Not used for code coverage
        }

        public void OnRandom(object result, string callerName, string callerType)
        {
            // Not used for code coverage
        }

        public void OnStrategyDescription(string strategyName, string description)
        {
            // Not used for code coverage
        }

        public void LogCompletion()
        {
            // Not used for code coverage
        }

        public void OnCompleted()
        {
            // Not used for code coverage
        }

        public void OnAnnouceEvent(string machineName, Runtime.Events.Event @event)
        {
        }

        #endregion
    }
}
