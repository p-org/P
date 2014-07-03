
#pragma once
#include "Config\PrtConfig.h"
#include "PrtSMPublic.h"


VOID
SmfTraceExit(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		StateExited
);

VOID SmfTraceQueueResize(
	__in PCWSTR		MachineName,
	__in ULONG_PTR	MachineId,
	__in UCHAR newQueueSize);

VOID
SmfTraceEnqueue(
__in PCWSTR		DesMachineName,
__in ULONG_PTR	DesMachineId,
__in PCWSTR		EventName,
__in ULONG_PTR	Payload
);

VOID
SmfTraceDelete(
__in PCWSTR DeletedMachineName,
__in PCWSTR InState
);

VOID
SmfTraceDequeue(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		EventName,
__in ULONG_PTR	Payload
);

VOID
SmfTraceStateChange(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		NewStateEntered
);

VOID
SmfTraceRaiseEvent(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		EventRaised,
__in ULONG_PTR	Payload
);

VOID
SmfTraceCreateMachine(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId
);

VOID
SmfTracePop(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		EnteredState
);

VOID
SmfTraceCallStatement(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		CallState
);

VOID
SmfTraceCallTransition(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		CallState
);

VOID
SmfTraceActions(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in PCWSTR		ActionName
);

VOID
SmfTraceUnhandledEvent(
__in PCWSTR		DesMachineName,
__in ULONG_PTR	DesMachineId,
__in PCWSTR		EventName,
__in PCWSTR	EnteredState
);

VOID
SmfTraceReportException(
__in PCWSTR		MachineName,
__in ULONG_PTR	MachineId,
__in SMF_EXCEPTIONS		Exception
);

VOID
SmfTraceAssertionFailure(
__in const char*	MachineName,
__in ULONG		MachineId,
__in const char*	Msg
);

