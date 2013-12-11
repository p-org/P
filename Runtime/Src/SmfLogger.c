/*********************************************************************************

Copyright (c) Microsoft Corporation

Module Name:

    SmfLogger.c

Abstract:
    This module contains functions used by P runtime for logging runtime information

Environment:

    Kernel mode only.		

***********************************************************************************/

#include "SmfPublic.h"
#include "SmfProtected.h"
#include "SmfPrivate.h"
#include "SmfLogger.h"
#include "TraceMacros.h"
#include "SmfTrace.h"

VOID 
SmfTraceStep(
__in PSMF_SMCONTEXT Context,
__in SMF_TRACE_STEP TStep,
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
		case traceEnqueue:
			TRACE_ENQUEUE(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, Context->Driver->Events[Context->EventQueue.Events[Context->EventQueue.Tail == 0 ? (Context->CurrentLengthOfEventQueue - 1) : (Context->EventQueue.Tail -1)].Event].Name, Context->EventQueue.Events[Context->EventQueue.Tail == 0 ? (Context->CurrentLengthOfEventQueue - 1) : (Context->EventQueue.Tail -1)].Arg.Value);
			break;
		case traceDequeue:
			TRACE_DEQUEUE(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, Context->Driver->Events[Context->Trigger.Event].Name, Context->Trigger.Arg.Value);
			break;
		case traceStateChange:
			TRACE_STATECHANGE(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, SmfGetCurrentStateDecl(Context).Name);
			break;
		case traceCreateMachine:
			TRACE_CREATEMACHINE(Context->Driver->Machines[Context->InstanceOf].Name, Context->This);
			break;
		case traceRaiseEvent:
			TRACE_RAISEEVENT(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, Context->Driver->Events[Context->Trigger.Event].Name, Context->Trigger.Arg.Value);
			break;
		case tracePop:
			TRACE_POP(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, SmfGetCurrentStateDecl(Context).Name);
			break;
		case traceCallStatement:
			TRACE_CALLSTATEMENT(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, SmfGetCurrentStateDecl(Context).Name);
			break;
		case traceCallEdge:
			TRACE_CALLTRANSITION(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, SmfGetCurrentStateDecl(Context).Name);
			break;
		case traceUnhandledEvent:
			TRACE_UNHANDLEDEVENT(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, Context->Driver->Events[Context->Trigger.Event].Name, SmfGetCurrentStateDecl(Context).Name);
			break;
		case traceActions:
			TRACE_ACTIONS(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, (PWSTR)(*(&TStep + 1)));
			break;
		case traceQueueResize:
			TRACE_QUEUERESIZE(Context->Driver->Machines[Context->InstanceOf].Name, Context->This,Context->CurrentLengthOfEventQueue);
			break;
		case traceExit:
			TRACE_EXIT(Context->Driver->Machines[Context->InstanceOf].Name, Context->This, SmfGetCurrentStateDecl(Context).Name);
			break;
		default:

			break;
	}
}


VOID SmfReportException(
__in SMF_EXCEPTIONS		Exception, 
__in PSMF_SMCONTEXT		Machine
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
	TRACE_REPORTEXCEPTION(Machine->Driver->Machines[Machine->InstanceOf].Name, Machine->This, Exception);
}

VOID SmfLogAssertionFailure(
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