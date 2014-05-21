/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

    TraceSteps.h

Abstract:
    This header file contains declarations of all the trace statements which can be 
	used inside driver code.
	

***********************************************************************************/

#define TRACE_DELETE(DeletedMachineName, InState) SmfTraceDelete(DeletedMachineName, InState)

#define TRACE_ENQUEUE(DesMachineName, DesMachineId, EventName, Payload) SmfTraceEnqueue(DesMachineName, DesMachineId, EventName, Payload)

#define TRACE_DEQUEUE(Machinename, MachineId, EventName, Payload) SmfTraceDequeue(Machinename, MachineId, EventName, Payload)

#define TRACE_STATECHANGE(MachineName, MachineId, NewStateEntered) SmfTraceStateChange(MachineName, MachineId, NewStateEntered)

#define TRACE_CREATEMACHINE(MachineName, MachineId) SmfTraceCreateMachine(MachineName, MachineId)

#define TRACE_RAISEEVENT(MachineName, MachineId, EventRaised, Payload) SmfTraceRaiseEvent(MachineName, MachineId, EventRaised, Payload)

#define TRACE_POP(MachineName, MachineId, EnteredState) SmfTracePop(MachineName, MachineId, EnteredState)

#define TRACE_CALLSTATEMENT(MachineName, MachineId, CallState) SmfTraceCallStatement(MachineName, MachineId, CallState)

#define TRACE_CALLTRANSITION(MachineName, MachineId, CallState) SmfTraceCallTransition(MachineName, MachineId, CallState)

#define TRACE_UNHANDLEDEVENT(MachineName, MachineId, EventName, EnteredState) SmfTraceUnhandledEvent(MachineName, MachineId, EventName, EnteredState)

#define TRACE_ACTIONS(MachineName, MachineId, ActionName) SmfTraceActions(MachineName, MachineId, ActionName)

#define TRACE_QUEUERESIZE(MachineName, MachineId, newQueueSize) SmfTraceQueueResize(MachineName, MachineId, newQueueSize)

#define TRACE_EXIT(MachineName, MachineId, StateExited) SmfTraceExit(MachineName, MachineId, StateExited)
//
// Exceptions in P Model
//

#define TRACE_REPORTEXCEPTION(MachineName, MachineId, SmfException) SmfTraceReportException(MachineName, MachineId, SmfException)

#define TRACE_ASSERTIONFAILURE(File, Line, Msg) SmfTraceAssertionFailure(File, Line, Msg)