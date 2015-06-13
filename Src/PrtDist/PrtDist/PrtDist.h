#ifndef PRTDIST_H
#define PRTDIST_H

#include "PrtDistSerializer.h"
#include "PrtDistIDL\PrtDistIDL.h"

extern char* AZUREMACHINEREF[];

//pointer to the container process
extern PRT_PROCESS* ContainerProcess;
//pointer to the P Program
extern PRT_PROGRAMDECL P_GEND_PROGRAM;

//Functions to help logging
void
PrtDistSMExceptionHandler(
__in PRT_STATUS exception,
__in void* vcontext
);

void PrtDistSMLogHandler(PRT_STEP step, void *vcontext);


//external function to send messages over RPC

PRT_BOOLEAN PrtDistSend(
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload
	);

//logging function
void PrtDistLog(PRT_STRING log);

handle_t
PrtDistCreateRPCClient(
PRT_VALUE* target
);

DWORD WINAPI PrtDistCreateRPCServerForEnqueueAndWait(
LPVOID portNumber
);

void PrtDistStartContainerListerner(PRT_PROCESS* process, PRT_INT32 portNumber, HANDLE* listener);
#endif