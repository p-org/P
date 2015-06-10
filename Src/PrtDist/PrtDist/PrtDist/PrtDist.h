#ifndef PRTDIST_H
#define PRTDIST_H

#include "PrtDistSerializer.h"
#include "..\CommonFiles\PrtDistGlobalInfo.h"
#include "..\PrtDistIDL\PrtDistIDL.h"


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
#endif