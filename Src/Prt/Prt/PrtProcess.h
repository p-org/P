#ifndef PRTPROCESS_H
#define PRTPROCESS_H
#include "PrtSMTypeDefs.h"
#include "PrtConfig.h"

typedef struct _PRT_PPROCESS PRT_PPROCESS;

typedef void(*PRT_EXCEPHANDLER_FUN)(PRT_EXCEPTIONS, void*);

typedef void(*PRT_LOG_FUN)(PRT_STEP, void*);

typedef struct _PRT_LINKEDLIST PRT_LINKEDLIST;

struct _PRT_LINKEDLIST {
	void* data;
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
__in PRT_PROGRAMDECL *program,
__in PRT_EXCEPHANDLER_FUN exHandler,
__in PRT_LOG_FUN logger
);

void
PrtStopPProcess(
PRT_PPROCESS* stopProcess);

void 
PrtPProcessAddMachine(
__in void* context
);

#endif

