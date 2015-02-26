#include "PrtDist.h"
#include "../PrtDistIDL/PrtDist.h"
#include "../PrtDistIDL/PrtDist_s.c"

//A global process
PRT_PROCESS* NodeProcess;

// Function for enqueueing message into the remote machine
void s_PrtDistEnqueue(
	handle_t handle,
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload,
	PRT_BOOLEAN* status
	)
{
	//get the context handle for this function
	//PRT_MACHINEINST* context = PrtGetMachine(NodeProcess, target);
	//PrtSend(context, event, payload);
	//*status = PRT_TRUE;
}

//rpc related functions

void* __RPC_API
MIDL_user_allocate(size_t size)
{
	unsigned char* ptr;
	ptr = (unsigned char*)malloc(size);
	return (void*)ptr;
}

void __RPC_API
MIDL_user_free(void* object)

{
	free(object);
}