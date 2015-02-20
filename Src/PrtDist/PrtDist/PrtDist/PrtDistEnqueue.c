#include "PrtDist.h"
#include "../PrtDistIDL/PrtDist.h"
#include "../PrtDistIDL/PrtDist_s.c"

//A global structure to store mapping from the port to the receiver state machine id listening on that port.
typedef struct TUPLE_PORT_MACHINEINST {
	PRT_INT32 portNumber;
	PRT_MACHINEINST* machineInst;
} TUPLE_PORT_MACHINEINST;

TUPLE_PORT_MACHINEINST MAP_PORT_TO_MACHINEINST[100] = { {-1, 0} };

//Function to add an element into the map
void PrtDistAddElementToPortMap(PRT_INT32 portNumber, PRT_MACHINEINST* machineInst)
{
	int index = 0;
	while (MAP_PORT_TO_MACHINEINST[index].portNumber != -1 && index < 100)
	{
		index++;
	}
	if (index >= 99)
	{
		PrtAssert(PRT_FALSE, "Max limit for Machines on a node");
	}
	MAP_PORT_TO_MACHINEINST[index].portNumber = portNumber;
	MAP_PORT_TO_MACHINEINST[index].machineInst = machineInst;
	MAP_PORT_TO_MACHINEINST[index+1].portNumber = -1;
	MAP_PORT_TO_MACHINEINST[index+1].machineInst = NULL;
	
}


// Function to loopup the machine instance
PRT_MACHINEINST* PrtDistGetMachineInstForPort(PRT_INT32 portNumber)
{
	int index = 0;
	while (MAP_PORT_TO_MACHINEINST[index].portNumber != -1 && index < 100)
	{
		if (MAP_PORT_TO_MACHINEINST[index].portNumber == portNumber)
			return MAP_PORT_TO_MACHINEINST[index].machineInst;
	}
	if (index >= 99)
	{
		PrtAssert(PRT_FALSE, "Port Not Registered");
	}

}

// Function for enqueueing message into the remote machine
void s_PrtDistEnqueue(
	handle_t handle,
	PRT_INT32 portNumber,
	PRT_VALUE* event,
	PRT_VALUE* payload,
	PRT_BOOLEAN* status
	)
{
	//get the context handle for this function
	PRT_MACHINEINST* context = PrtDistGetMachineInstForPort(portNumber);

	PrtSend(context, event, payload);
	*status = PRT_TRUE;
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