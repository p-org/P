#pragma once
#include "PrtHeaders.h"
#include "PrtSMPublicTypes.h"

typedef struct _PRT_PPROCESS PRT_PPROCESS;

typedef VOID(*PRT_EXCEPHANDLER_FUN)(PRT_EXCEPTIONS, PVOID);

typedef VOID(*PRT_LOG_FUN)(PRT_STEP, PVOID);

typedef struct _PRT_LINKEDLIST PRT_LINKEDLIST;

struct _PRT_LINKEDLIST {
	PVOID data;
	PRT_LINKEDLIST *next;
};


struct _PRT_PPROCESS {
	PRT_PROGRAMDECL			*program;
	PRT_UINT16				processId;
	PRT_EXCEPHANDLER_FUN	exceptionHandler;
	PRT_LOG_FUN				log;
	PRT_RECURSIVE_MUTEX		lock;
	PRT_LINKEDLIST			*allMachines;
};

PRT_PPROCESS*
PrtStartPProcess(
__in PRT_UINT16 processId,
__in PRT_PROGRAMDECL *program
);

VOID
PrtStopPProcess(
PRT_PPROCESS* stopProcess);

VOID 
PrtPProcessAddMachine(
__in PVOID context
);

