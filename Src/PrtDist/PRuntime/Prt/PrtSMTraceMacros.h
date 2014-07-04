/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

TraceSteps.h

Abstract:
This header file contains declarations of all the trace statements which can be
used inside driver code.


***********************************************************************************/

#define TRACE_DELETE(DeletedMachineName, InState) PrtTraceDelete(DeletedMachineName, InState)

#define TRACE_ENQUEUE(DesMachineName, DesMachineId, EventName, Payload) PrtTraceEnqueue(DesMachineName, DesMachineId, EventName, Payload)

#define TRACE_DEQUEUE(Machinename, MachineId, EventName, Payload) PrtTraceDequeue(Machinename, MachineId, EventName, Payload)

#define TRACE_STATECHANGE(MachineName, MachineId, NewStateEntered) PrtTraceStateChange(MachineName, MachineId, NewStateEntered)

#define TRACE_CREATEMACHINE(MachineName, MachineId) PrtTraceCreateMachine(MachineName, MachineId)

#define TRACE_RAISEEVENT(MachineName, MachineId, EventRaised, Payload) PrtTraceRaiseEvent(MachineName, MachineId, EventRaised, Payload)

#define TRACE_POP(MachineName, MachineId, EnteredState) PrtTracePop(MachineName, MachineId, EnteredState)

#define TRACE_CALLSTATEMENT(MachineName, MachineId, CallState) PrtTraceCallStatement(MachineName, MachineId, CallState)

#define TRACE_CALLTRANSITION(MachineName, MachineId, CallState) PrtTraceCallTransition(MachineName, MachineId, CallState)

#define TRACE_UNHANDLEDEVENT(MachineName, MachineId, EventName, EnteredState) PrtTraceUnhandledEvent(MachineName, MachineId, EventName, EnteredState)

#define TRACE_ACTIONS(MachineName, MachineId, ActionName) PrtTraceActions(MachineName, MachineId, ActionName)

#define TRACE_QUEUERESIZE(MachineName, MachineId, newQueueSize) PrtTraceQueueResize(MachineName, MachineId, newQueueSize)

#define TRACE_EXIT(MachineName, MachineId, StateExited) PrtTraceExit(MachineName, MachineId, StateExited)
//
// Exceptions in P Model
//

#define TRACE_REPORTEXCEPTION(MachineName, MachineId, PrtException) PrtTraceReportException(MachineName, MachineId, PrtException)

#define TRACE_ASSERTIONFAILURE(File, Line, Msg) PrtTraceAssertionFailure(File, Line, Msg)