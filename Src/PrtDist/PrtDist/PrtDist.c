#include "PrtDist.h"
#include "PrtDistIDL/PrtDistIDL_s.c"

extern int PrtDistGetNextNodeId();
extern int PrtDistCreateContainer(int nodeId);

// Function for enqueueing message into the remote machine
void s_PrtDistSendEx(
	PRPC_ASYNC_STATE asyncState,
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

	PRT_MACHINEINST* context = PrtGetMachine(ContainerProcess, deserial_target);
	
	PrtSend(context, deserial_event, deserial_payload);


}

void s_PrtDistPing(
	PRPC_ASYNC_STATE asyncState,
	handle_t handle
)
{
	PrtDistLog("Ping Successful");
}

PRT_INT32 PrtDistGetRecvPortNumber(PRT_VALUE* target)
{
	return PRTD_CONTAINER_RECV_PORT + target->valueUnion.mid->processId.data1;
}

handle_t
PrtDistCreateRPCClient(
PRT_VALUE* target
)
{
	PRT_INT32 nodeId = target->valueUnion.mid->processId.data2;
	PRT_INT32 portNumber = PrtDistGetRecvPortNumber(target);
	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;
	handle_t handle;
	//get centralserverID
	char buffPort[100];
	_itoa(portNumber, buffPort, 10);
	// Creates a string binding handle.
	// This function is nothing more than a printf.
	// Connection is not done here.
	status = RpcStringBindingCompose(
		NULL, // UUID to bind to.
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP
		// protocol.
		(unsigned char*)(PRTD_CLUSTERMACHINES[nodeId]), // TCP/IP network
		// address to use.
		(unsigned char*)buffPort, // TCP/IP port to use.
		NULL, // Protocol dependent network options to use.
		&szStringBinding); // String binding output.

	if (status)
		exit(status);



	// Validates the format of the string binding handle and converts
	// it to a binding handle.
	// Connection is not done here either.
	status = RpcBindingFromStringBinding(
		szStringBinding, // The string binding to validate.
		&handle); // Put the result in the implicit binding
	// handle defined in the IDL file.

	if (status)
	{
		PrtAssert(PRT_FALSE, "Failed to create an RPC Client");
	}
	return handle;
}




//Function to create a RPC server and wait, should be called using a worker thread.
DWORD WINAPI PrtDistCreateRPCServerForEnqueueAndWait(LPVOID portNumber)
{
	PrtDistLog("Creating RPC server for Enqueue at Port :");
	RPC_STATUS status;
	char buffPort[100];
	_itoa(*((PRT_INT32*)portNumber), buffPort, 10);
	PrtDistLog(buffPort);
	status = RpcServerUseProtseqEp(
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP protocol.
		RPC_C_PROTSEQ_MAX_REQS_DEFAULT, // Backlog queue length for TCP/IP.
		(RPC_CSTR)buffPort, // TCP/IP port to use.
		NULL);

	if (status)
	{
		PrtDistLog("Runtime reported exception in RpcServerUseProtseqEp");
		exit(status);
	}

	status = RpcServerRegisterIf2(
		s_PrtDist_v1_0_s_ifspec, // Interface to register.
		NULL, // Use the MIDL generated entry-point vector.
		NULL, // Use the MIDL generated entry-point vector.
		RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, // Forces use of security callback.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Use default number of concurrent calls.
		(unsigned)-1, // Infinite max size of incoming data blocks.
		NULL); // Naive security callback.

	if (status)
	{
		PrtDistLog("Runtime reported exception in RpcServerRegisterIf2");
		exit(status);
	}

	PrtDistLog("Receiver listening ...");
	// Start to listen for remote procedure calls for all registered interfaces.
	// This call will not return until RpcMgmtStopServerListening is called.
	status = RpcServerListen(
		1, // Recommended minimum number of threads.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Recommended maximum number of threads.
		0);

	if (status)
	{
		PrtDistLog("Runtime reported exception in RpcServerListen");
		exit(status);
	}

	return -1;

}

void PrtDistStartContainerListerner(PRT_PROCESS* process, PRT_INT32 portNumber, HANDLE listener)
{

	listener = CreateThread(NULL, 0, PrtDistCreateRPCServerForEnqueueAndWait, &portNumber, 0, NULL);
	if (listener == NULL)
	{
		PrtDistLog("Error Creating RPC server in PrtDistStartNodeManagerMachine");
	}
	else
	{
		DWORD status;
		//Sleep(3000);
		//check if the thread is all ok
		GetExitCodeThread(listener, &status);
		if (status != STILL_ACTIVE)
			PrtDistLog("ERROR : Thread terminated");

		PrtDistLog("Receiver listening at port ");
		char log[10];
		_itoa(portNumber, log, 10);
		PrtDistLog(log);
	}

}

PRT_VALUE *P_FUN__SENDRELIABLE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
{
	PRT_VALUE* target = PrtTupleGet(value, 0);
	while (PRT_FALSE == PrtDistSend(target, PrtTupleGet(value, 1), PrtTupleGet(value, 2)));

	return PrtMkNullValue();
}

PRT_VALUE *P_FUN__SEND_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
{
	PRT_VALUE* target = PrtTupleGet(value, 0);
	PrtDistSend(target, PrtTupleGet(value, 1), PrtTupleGet(value, 2));
	return PrtMkBoolValue(PRT_TRUE);
}

PRT_VALUE *P_FUN__CREATECONTAINER_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
{
	//first step is to get the nodeId from central node.
	int newNodeId = PrtDistGetNextNodeId();

	//send message to the node manager on the new node to create container.
	int newContainerId = PrtDistCreateContainer(newNodeId);

	PRT_VALUE* containerMachine = PrtMkDefaultValue(PrtMkPrimitiveType(PRT_KIND_MACHINE));
	containerMachine->valueUnion.mid->machineId = 1; //the first machine.
	containerMachine->valueUnion.mid->processId.data1 = newContainerId;
	containerMachine->valueUnion.mid->processId.data2 = newNodeId;

	return containerMachine;
}


//logging function
void PrtDistLog(PRT_STRING log)
{
	((PRT_PROCESS_PRIV*)ContainerProcess)->logHandler(PRT_STEP_COUNT, log);
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