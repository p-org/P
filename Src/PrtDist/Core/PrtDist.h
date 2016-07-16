#ifndef PRTDIST_H
#define PRTDIST_H

#include "PrtDistInternals.h"
#include "PrtDistIDL_h.h"
#include "PrtDistConfigParser.h"

//pointer to the container process
extern PRT_PROCESS* ContainerProcess;
extern PRT_INT64 sendMessageSeqNumber;

//Functions to help logging
void
PrtDistSMExceptionHandler(
__in PRT_STATUS exception,
__in PRT_MACHINEINST* vcontext
);

// Function to open log file in a given directory
void PrtOpenLogFile(__in PRT_CHAR* logDirectory);
void PrtCloseLogFile();

void PrtDistSMLogHandler(PRT_STEP step, PRT_MACHINEINST *sender, PRT_MACHINEINST* receiver, PRT_VALUE* event, PRT_VALUE* payload);


//external function to send messages over RPC

PRT_BOOLEAN PrtDistSend(
	PRT_VALUE* source,
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload
);

//logging function
void PrtDistLog(PRT_STRING log);

DWORD WINAPI PrtDistCreateRPCServerForEnqueueAndWait(
LPVOID portNumber
);


PRT_VALUE *P_FUN__CREATENODE_IMPL(PRT_MACHINEINST *context);

PRT_VALUE *P_FUN__SEND_IMPL(PRT_MACHINEINST *context);
#endif