#include"PrtSMTrace.h"
#include "PrtSMProtected.h"

VOID
SmfTraceExit(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		StateExited
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<ExitLog> Machine %ws(0x%lu) exited state %ws and executing its exit function\n",
		MachineName, MachineId, StateExited);
#else
	printf(
		"<ExitLog> Machine %ws(0x%lu) exited state %ws and executing its exit function\n",
		MachineName, MachineId, StateExited);
#endif
}

VOID SmfTraceQueueResize(
	__in PCWSTR		MachineName,
	__in ULONG_PTR	MachineId,
	__in UCHAR newQueueSize)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<QueueSizeLog> Machine %ws(0x%lu) did Queue Resize (New Queue Size - %d) \n",
		MachineName,
		MachineId,
		newQueueSize);
#else
	printf(
		"<QueueSizeLog> Machine %ws(0x%lu) did Queue Resize (New Queue Size - %d) \n",
		MachineName,
		MachineId,
		newQueueSize);
#endif
}

VOID
SmfTraceActions(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		ActionName
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<ActionLog> Machine %ws(0x%lu) Executed Action - %ws \n",
		MachineName,
		MachineId,
		ActionName);
#else
	printf(
		"<ActionLog> Machine %ws(0x%lu) Executed Action - %ws \n",
		MachineName,
		MachineId,
		ActionName);
#endif
}

VOID
SmfTraceDelete(
__in PCWSTR DeletedMachineName,
__in PCWSTR InState
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<DeleteLog> Machine %ws Deleted in State %ws \n",
		DeletedMachineName,
		InState);
#else
	printf(
		"<DeleteLog> Machine %ws Deleted in State %ws \n",
		DeletedMachineName,
		InState);
#endif
}

VOID
SmfTraceEnqueue(
__in PCWSTR		DesMachineName,
__in ULONG_PTR	DesMachineId,
__in PCWSTR		EventName,
__in ULONG_PTR	Payload
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<EnqueueLog> Enqueued Event < %ws, %lu > on Machine %ws(0x%lu) \n",
		EventName,
		Payload,
		DesMachineName, DesMachineId);
#else
	printf(
		"<EnqueueLog> Enqueued Event < %ws, %lu > on Machine %ws(0x%lu) \n",
		EventName,
		Payload,
		DesMachineName, DesMachineId);
#endif
}

VOID
SmfTraceDequeue(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		EventName,
__in ULONG_PTR	Payload
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<DequeueLog> Dequeued Event < %ws, %lu > by Machine %ws(0x%lu) \n",
		EventName,
		Payload,
		MachineName, MachineId);
#else
	printf(
		"<DequeueLog> Dequeued Event < %ws, %lu > by Machine %ws(0x%lu) \n",
		EventName,
		Payload,
		MachineName, MachineId);
#endif
}

VOID
SmfTraceStateChange(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		NewStateEntered
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<StateLog> Machine %ws(0x%lu) entered state %ws\n",
		MachineName, MachineId, NewStateEntered);
#else
	printf(
		"<StateLog> Machine %ws(0x%lu) entered state %ws\n",
		MachineName, MachineId, NewStateEntered);
#endif
}

VOID
SmfTraceRaiseEvent(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		EventRaised,
__in ULONG_PTR	Payload
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<RaiseLog> Machine %ws(0x%lu) raised event < %ws, %lu >\n",
		MachineName, MachineId, EventRaised, Payload);
#else
	printf(
		"<RaiseLog> Machine %ws(0x%lu) raised event < %ws, %lu >\n",
		MachineName, MachineId, EventRaised, Payload);
#endif
}

VOID
SmfTraceCreateMachine(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<CreateLog> Machine %ws(0x%lu) is created\n",
		MachineName, MachineId);
#else
	printf(
		"<CreateLog> Machine %ws(0x%lu) is created\n",
		MachineName, MachineId);
#endif
}

VOID
SmfTracePop(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		EnteredState
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<PopLog> Machine %ws(0x%lu) executed POP and entered state %ws\n",
		MachineName, MachineId, EnteredState);
#else
	printf(
		"<PopLog> Machine %ws(0x%lu) executed POP and entered state %ws\n",
		MachineName, MachineId, EnteredState);
#endif
}

VOID
SmfTraceCallStatement(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		CallState
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<CallLog> Machine %ws(0x%lu) executed Call and entered state %ws\n",
		MachineName, MachineId, CallState);
#else
	printf(
		"<CallLog> Machine %ws(0x%lu) executed Call and entered state %ws\n",
		MachineName, MachineId, CallState);
#endif
}


VOID
SmfTraceCallTransition(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		CallState
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<CallLog> Machine %ws(0x%lu) took Call transition and entered state %ws\n",
		MachineName, MachineId, CallState);
#else
	printf(
		"<CallLog> Machine %ws(0x%lu) took Call transition and entered state %ws\n",
		MachineName, MachineId, CallState);
#endif
}

VOID
SmfTraceUnhandledEvent(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		EventName,
__in PCWSTR	EnteredState
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<PopLog> Machine %ws(0x%lu) executed POP because of unhandled event %ws and entered state %ws\n",
		MachineName, MachineId, EventName, EnteredState);
#else
	printf(
		"<PopLog> Machine %ws(0x%lu) executed POP because of unhandled event %ws and entered state %ws\n",
		MachineName, MachineId, EventName, EnteredState);
#endif
}

VOID
SmfTraceReportException(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PRT_EXCEPTIONS		Exception
)
{
	//
	// Debug Print Statement
	//

#ifdef KERNEL_MODE

	switch (Exception)
	{
	case UnhandledEvent:
		DbgPrint(
			"<EXCEPTION> Machine %ws(0x%lu) : Unhandled Event Exception\n",
			MachineName,
			MachineId);
		NT_ASSERTMSG("Unhandled Event", FALSE);
		break;
	case UnfinishedEvents:
		DbgPrint(
			"<EXCEPTION> Machine %ws(0x%lu) : UnfinishedEvents Exception\n",
			MachineName,
			MachineId);
		NT_ASSERTMSG("UnfinishedEvents", FALSE);
		break;
	case IllegalAccess:
		DbgPrint(
			"<EXCEPTION> IllegalAccess Exception\n");
		NT_ASSERTMSG("IllegalAccess", FALSE);
		break;
	case MaxInstanceExceeded:
		DbgPrint(
			"<EXCEPTION> Machine %ws(0x%lu) : MaxInstance of Event Exceeded Exception\n",
			MachineName,
			MachineId);
		NT_ASSERTMSG("MaxInstanceExceeded", FALSE);

		break;
	case FailedToAllocateMemory:
		DbgPrint(
			"<EXCEPTION> Failed to allocate memory Exception\n");
		NT_ASSERTMSG("Failed to allocate memory", FALSE);
		break;
	case UnhandledEventInCallS:
		DbgPrint(
			"<EXCEPTION> Call Statement terminated with an unhandled event in Machine %ws(0x%lu)\n",
			MachineName,
			MachineId);
		NT_ASSERTMSG("Failed to allocate memory", FALSE);
		break;
	case MaxQueueSizeExceeded:
		DbgPrint(
			"<EXCEPTION> Queue Size Exceeded Max Limits in Machine %ws(0x%lu)\n",
			MachineName,
			MachineId);
		NT_ASSERTMSG("Queue Size Exceeded", FALSE);
		break;
	default:
		DbgPrint(
			"<EXCEPTION> Machine %ws(0x%lu) : Unknown Exception\n",
			MachineName,
			MachineId);
		NT_ASSERTMSG("Unknown Exception", FALSE);
		break;
	}

#else

	switch (Exception)
	{
	case UnhandledEvent:
		printf(
			"<EXCEPTION> Machine %ws(0x%lu) : Unhandled Event Exception\n",
			MachineName,
			MachineId);
		PRT_ASSERT(FALSE);
		break;
	case UnfinishedEvents:
		printf(
			"<EXCEPTION> Machine %ws(0x%lu) : UnfinishedEvents Exception\n",
			MachineName,
			MachineId);
		PRT_ASSERT(FALSE);
		break;
	case IllegalAccess:
		printf(
			"<EXCEPTION> Machine %ws(0x%lu) : IllegalAccess Exception\n",
			MachineName,
			MachineId);
		PRT_ASSERT(FALSE);
		break;
	case MaxInstanceExceeded:
		printf(
			"<EXCEPTION> Machine %ws(0x%lu) : MaxInstance of Event Exceeded Exception\n",
			MachineName,
			MachineId);
		PRT_ASSERT(FALSE);
		break;
	case FailedToAllocateMemory:
		printf(
			"<EXCEPTION> Failed to allocate memory Exception\n");
		PRT_ASSERT(FALSE);
		break;
	case UnhandledEventInCallS:
		printf(
			"<EXCEPTION> Call Statement terminated with an unhandled event in Machine %ws(0x%lu)\n",
			MachineName,
			MachineId);
		PRT_ASSERT(FALSE);
		break;
	case MaxQueueSizeExceeded:
		printf(
			"<EXCEPTION> Queue Size Exceeded Max Limits in Machine %ws(0x%lu)\n",
			MachineName,
			MachineId);
		PRT_ASSERT(FALSE);
		break;
	default:
		printf(
			"<EXCEPTION> Machine %ws(0x%lu) : Unknown Exception\n",
			MachineName,
			MachineId);
		PRT_ASSERT(FALSE);
		break;
	}

	DebugBreak();
#endif
}

VOID
SmfTraceAssertionFailure(
__in const char*	File,
__in ULONG			Line,
__in const char*	Msg
)
{
#ifdef KERNEL_MODE
	DbgPrint(
		"<AssertionLog> Failed an assertion %s:%lu: %s\n",
		File, Line, Msg);
#else
	printf(
		"<AssertionLog> Failed an assertion %s:%lu: %s\n",
		File, Line, Msg);
#endif

}