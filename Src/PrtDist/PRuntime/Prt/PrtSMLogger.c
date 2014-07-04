/*********************************************************************************

Copyright (c) Microsoft Corporation

Module Name:

PrtLogger.c

Abstract:
This module contains functions used by P runtime for logging runtime information

Environment:

Kernel mode only.

***********************************************************************************/

#include "PrtSMPublic.h"
#include "PrtSMProtected.h"
#include "PrtSMPrivate.h"
#include "PrtSMLogger.h"
#include "PrtSMTraceMacros.h"
#include "PrtSMTrace.h"

VOID
PrtTraceStep(
__in PPRT_SMCONTEXT Context,
__in PRT_TRACE_STEP TStep,
...
)
/*++

Routine Description:

P runtime code calls this routine to log P steps.


Arguments:

Context --
Pointer to the state-machine context

TStep --
P Step executed

Return Value:

NONE (VOID)

--*/
{

	switch (TStep)
	{
	case traceDelete:
		TRACE_DELETE(Context->Program->Machines[Context->InstanceOf].Name, PrtGetCurrentStateDecl(Context).Name);
		break;
	case traceEnqueue:
		TRACE_ENQUEUE(Context->Program->Machines[Context->InstanceOf].Name, Context->This, Context->Program->Events[Context->EventQueue.Events[Context->EventQueue.Tail == 0 ? (Context->CurrentLengthOfEventQueue - 1) : (Context->EventQueue.Tail - 1)].Event].Name, Context->EventQueue.Events[Context->EventQueue.Tail == 0 ? (Context->CurrentLengthOfEventQueue - 1) : (Context->EventQueue.Tail - 1)].Arg.Value);
		break;
	case traceDequeue:
		TRACE_DEQUEUE(Context->Program->Machines[Context->InstanceOf].Name, Context->This, Context->Program->Events[Context->Trigger.Event].Name, Context->Trigger.Arg.Value);
		break;
	case traceStateChange:
		TRACE_STATECHANGE(Context->Program->Machines[Context->InstanceOf].Name, Context->This, PrtGetCurrentStateDecl(Context).Name);
		break;
	case traceCreateMachine:
		TRACE_CREATEMACHINE(Context->Program->Machines[Context->InstanceOf].Name, Context->This);
		break;
	case traceRaiseEvent:
		TRACE_RAISEEVENT(Context->Program->Machines[Context->InstanceOf].Name, Context->This, Context->Program->Events[Context->Trigger.Event].Name, Context->Trigger.Arg.Value);
		break;
	case tracePop:
		TRACE_POP(Context->Program->Machines[Context->InstanceOf].Name, Context->This, PrtGetCurrentStateDecl(Context).Name);
		break;
	case traceCallStatement:
		TRACE_CALLSTATEMENT(Context->Program->Machines[Context->InstanceOf].Name, Context->This, PrtGetCurrentStateDecl(Context).Name);
		break;
	case traceCallEdge:
		TRACE_CALLTRANSITION(Context->Program->Machines[Context->InstanceOf].Name, Context->This, PrtGetCurrentStateDecl(Context).Name);
		break;
	case traceUnhandledEvent:
		TRACE_UNHANDLEDEVENT(Context->Program->Machines[Context->InstanceOf].Name, Context->This, Context->Program->Events[Context->Trigger.Event].Name, PrtGetCurrentStateDecl(Context).Name);
		break;
	case traceActions:
		TRACE_ACTIONS(Context->Program->Machines[Context->InstanceOf].Name, Context->This, (PWSTR)(*(&TStep + 1)));
		break;
	case traceQueueResize:
		TRACE_QUEUERESIZE(Context->Program->Machines[Context->InstanceOf].Name, Context->This, Context->CurrentLengthOfEventQueue);
		break;
	case traceExit:
		TRACE_EXIT(Context->Program->Machines[Context->InstanceOf].Name, Context->This, PrtGetCurrentStateDecl(Context).Name);
		break;
	default:

		break;
	}
}


VOID PrtReportException(
	__in PRT_EXCEPTIONS		Exception,
	__in PPRT_SMCONTEXT		Machine
	)
	/*++

	Routine Description:

	P runtime code calls this routine to log Runtime Exception encountered
	and throw an similar assertion failure.


	Arguments:

	Exception - Exception thrown by runtime

	Machine - Statemachine that raised an exception.

	Return Value:

	NONE (VOID)

	--*/
{
	TRACE_REPORTEXCEPTION(Machine->Program->Machines[Machine->InstanceOf].Name, Machine->This, Exception);
}

VOID PrtLogAssertionFailure(
	const char* File,
	ULONG Line,
	const char* Msg
	)
	/*++

	Routine Description:

	P runtime code calls this routine to log an assertion failure.


	Arguments:

	File - name of C file in which assertion is
	Line - line of assertion
	Msg - string message associated with exception

	Return Value:

	NONE (VOID)

	--*/
{
	TRACE_ASSERTIONFAILURE(File, Line, Msg);
}