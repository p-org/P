
#pragma once
#include "Config\PrtConfig.h"
#include "PrtSMPublic.h"


VOID
PrtTraceExit(
__in PRT_STRING		MachineName,
__in ULONG_PTR		MachineId,
__in PRT_STRING		StateExited
);

VOID PrtTraceQueueResize(
	__in PRT_STRING		MachineName,
	__in ULONG_PTR		MachineId,
	__in UCHAR newQueueSize);

VOID
PrtTraceEnqueue(
__in PRT_STRING		DesMachineName,
__in ULONG_PTR	DesMachineId,
__in PRT_STRING		EventName,
__in ULONG_PTR	Payload
);

VOID
PrtTraceDelete(
__in PRT_STRING DeletedMachineName,
__in PRT_STRING InState
);

VOID
PrtTraceDequeue(
__in PRT_STRING		MachineName,
__in ULONG_PTR	MachineId,
__in PRT_STRING		EventName,
__in ULONG_PTR	Payload
);

VOID
PrtTraceStateChange(
__in PRT_STRING		MachineName,
__in ULONG_PTR	MachineId,
__in PRT_STRING		NewStateEntered
);

VOID
PrtTraceRaiseEvent(
__in PRT_STRING		MachineName,
__in ULONG_PTR	MachineId,
__in PRT_STRING		EventRaised,
__in ULONG_PTR	Payload
);

VOID
PrtTraceCreateMachine(
__in PRT_STRING		MachineName,
__in ULONG_PTR	MachineId
);

VOID
PrtTracePop(
__in PRT_STRING		MachineName,
__in ULONG_PTR	MachineId,
__in PRT_STRING		EnteredState
);

VOID
PrtTraceCallStatement(
__in PRT_STRING		MachineName,
__in ULONG_PTR	MachineId,
__in PRT_STRING		CallState
);

VOID
PrtTraceCallTransition(
__in PRT_STRING		MachineName,
__in ULONG_PTR	MachineId,
__in PRT_STRING		CallState
);

VOID
PrtTraceActions(
__in PRT_STRING		MachineName,
__in ULONG_PTR	MachineId,
__in PRT_STRING		ActionName
);

VOID
PrtTraceUnhandledEvent(
__in PRT_STRING		DesMachineName,
__in ULONG_PTR	DesMachineId,
__in PRT_STRING		EventName,
__in PRT_STRING	EnteredState
);

VOID
PrtTraceReportException(
__in PRT_STRING		MachineName,
__in ULONG_PTR	MachineId,
__in PRT_EXCEPTIONS		Exception
);

VOID
PrtTraceAssertionFailure(
__in const char*	MachineName,
__in ULONG		MachineId,
__in const char*	Msg
);

