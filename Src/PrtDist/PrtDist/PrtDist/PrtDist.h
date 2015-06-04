#pragma once
#include "PrtWinUser.h"
#include "PrtExecution.h"



//serialization
PRT_TYPE*
PrtDistDeserializeType(
__in PRT_TYPE* type
);

PRT_TYPE*
PrtDistSerializeType(
__in PRT_TYPE* type
);

PRT_VALUE*
PrtDistDeserializeValue(
__in PRT_VALUE* value
);

PRT_VALUE*
PrtDistSerializeValue(
__in PRT_VALUE* value
);

//logging

void
PrtDistSMExceptionHandler(
__in PRT_STATUS exception,
__in void* vcontext
);

void
PrtDistSMLogHandler(
__in PRT_STEP step,
__in void* vcontext
);

void
PrtDistLog(
char* log
);

//RPC helpers

handle_t
PrtDistCreateRPCClient(
PRT_VALUE* target
);

DWORD WINAPI PrtDistCreateRPCServerForEnqueueAndWait(LPVOID portNumber
);

PRT_INT32 
PrtDistGetRecvPortNumber();

PRT_BOOLEAN PrtDistSend(
	handle_t  handle,
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload
);

extern PRT_PROCESS* NodeProcess;


