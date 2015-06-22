#ifndef PRTDIST_H
#define PRTDIST_H

#include "PrtDistSerializer.h"
#include "PrtDistIDL_h.h"
#include "ConfigParser.h"

ClusterConfig ClusterConfiguration;
//pointer to the container process
extern PRT_PROCESS* ContainerProcess;

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

DWORD WINAPI PrtDistCreateRPCServerForEnqueueAndWait(
LPVOID portNumber
);


PRT_VALUE *P_FUN__CREATENODE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

PRT_VALUE *P_FUN__SENDRELIABLE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

PRT_VALUE *P_FUN__SEND_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);
#endif