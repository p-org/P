#pragma once
#include "Config\PrtConfig.h"
#include "PrtSMPublicTypes.h"

typedef struct _PRT_PROCESS PRT_PROCESS;

typedef VOID(*PRT_EXCEPHANDLER_FUN)(PRT_EXCEPTIONS, PVOID);

typedef VOID(*PRT_LOG_FUN)(PRT_STEP, PVOID);

struct _PRT_PROCESS {
	PRT_UINT16				processId;
	PRT_EXCEPHANDLER_FUN	exceptionHandler;
	PRT_LOG_FUN				log;
	PRT_RECURSIVE_MUTEX		lock;
	PVOID					allMachines[20];
	PRT_UINT8				nextMachine;
};

PRT_PROCESS*
PrtStartPProcess();

VOID
PrtStopPProcess(
PRT_PROCESS* stopProcess);

VOID 
PrtProcessAddMachine(
__in PVOID context
);

