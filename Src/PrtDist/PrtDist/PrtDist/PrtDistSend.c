#include "PrtWinUser.h"
#include "PrtExecution.h"
#include "../PrtDistIDL/PrtDist.h"
#include "../PrtDistIDL/PrtDist_s.c"

//A global process
PRT_PROCESS* NodeProcess;

// Function for enqueueing message into the remote machine
void s_PrtDistSendEx(
	handle_t handle,
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload
	)
{
	//get the context handle for this function

	PRT_VALUE* deserial_target = PrtDistDeserializeValue(target);
	PRT_VALUE* deserial_event = PrtDistDeserializeValue(event);
	PRT_VALUE* deserial_payload = PrtDistDeserializeValue(payload);

	PRT_MACHINEINST* context = PrtGetMachine(NodeProcess, deserial_target);
	PrtSend(context, deserial_event, deserial_payload);


}

void s_PrtDistPing(
	handle_t handle
)
{
	printf("Ping Successful");
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