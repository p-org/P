#include "PrtInterface.h"

struct PRT_STATEMACHINE {
	PRT_MACHINE_HANDLE handle; /* Pointer to State Machine */

};

void PrtSend(__in PRT_STATEMACHINE machine, __in PRT_VALUE *event, __in PRT_VALUE *payload)
{

	PrtEnqueueEvent(machine.handle, event, payload);
}

PRT_STATUS PrtCreateMachine(__in PRT_PPROCESS *process, __in PRT_INT32 instanceOfMachine, __in PRT_VALUE *payload, __out PRT_STATEMACHINE *pSM)
{
	return PrtCreate(process, instanceOfMachine, payload, &pSM->handle);
}