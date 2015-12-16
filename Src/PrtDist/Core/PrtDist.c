#include "PrtDist.h"
#include ".\PrtDistIDL_s.c"
#include "PrtDistInternals.h"


/***********************************************************************************************************/
//Create remote machine 
PRT_MACHINEINST * PRT_CALL_CONV PrtMkMachineRemote(
	_Inout_ PRT_PROCESS *process,
	_In_ PRT_UINT32 instanceOf,
	_In_ PRT_VALUE *payload,
	_In_ PRT_VALUE* container)
{
	PRT_VALUE* serial_params = PrtDistSerializeValue(payload);
	PRT_VALUE* retVal = PrtMkNullValue();

	handle_t handle;
	handle = PrtDistCreateRPCClient(container);

	RpcTryExcept
	{

		c_PrtDistMkMachine(handle, instanceOf, serial_params, &retVal);
		//c_PrtDistSendEx(handle, serial_target, serial_event, serial_payload);
	}
	RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		char log[MAX_LOG_SIZE];
		sprintf_s(log, MAX_LOG_SIZE, "Runtime reported RPC exception 0x%lx = %ld\n when executing function PrtDistMkMachine", ulCode, ulCode);
		PrtDistLog(log);
		sprintf_s(log, MAX_LOG_SIZE, "Terminated the Process as -new- operation failed");
		PrtDistLog(log);
		exit(1);
	}
	RpcEndExcept
	
	PRT_MACHINEINST_PRIV *context;
	context = (PRT_MACHINEINST_PRIV*)PrtMalloc(sizeof(PRT_MACHINEINST_PRIV));
	context->id = PrtDistDeserializeValue(retVal);
	return (PRT_MACHINEINST*)context;
}

void s_PrtDistMkMachine(
	handle_t handle,
	PRT_INT32 instanceOf,
	PRT_VALUE* params,
	PRT_VALUE** retVal
)
{
	PRT_VALUE* deserial_params = PrtDistDeserializeValue(params);
	PRT_MACHINEINST* newContext = PrtMkMachine(ContainerProcess, instanceOf, deserial_params);
	*retVal = PrtDistSerializeValue(newContext->id);
}

/***********************************************************************************************************/
// Function for enqueueing message into the remote machine
void s_PrtDistSendEx(
	PRPC_ASYNC_STATE asyncState,
	handle_t handle,
	PRT_VALUE* source,
	PRT_INT64 seqNum,
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload
	)
{
	//get the context handle for this function
	PRT_VALUE* deserial_target = PrtDistDeserializeValue(target);
	PRT_VALUE* deserial_event = PrtDistDeserializeValue(event);
	PRT_VALUE* deserial_payload = PrtDistDeserializeValue(payload);
	PRT_VALUE* deserial_source = PrtDistDeserializeValue(source);
	PRT_MACHINEINST* context = PrtGetMachine(ContainerProcess, deserial_target);
	PrtEnqueueInOrder(source, seqNum, (PRT_MACHINEINST_PRIV*)context, deserial_event, deserial_payload);
}

/***********************************************************************************************************
* Functions for creation of RPC client and Server
*/

handle_t
PrtDistCreateRPCClient(
PRT_VALUE* target
)
{
	PRT_INT32 nodeId = target->valueUnion.mid->processId.data2;
	PRT_INT32 portNumber = atoi(ClusterConfiguration.ContainerPortStart) + target->valueUnion.mid->processId.data1;

	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;
	handle_t handle;

	char buffPort[100];
	_itoa(portNumber, buffPort, 10);

	// Connection is not done here.
	status = RpcStringBindingCompose(
		NULL, // UUID to bind to.
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP
		// protocol.
		(unsigned char*)(ClusterConfiguration.ClusterMachines[nodeId]), // TCP/IP network
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
		PrtAssert(PRT_FALSE, "Failed to create an RPC Client (function : PrtDistCreateRPCClient)");
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
		PrtDistLog("Runtime reported exception in RpcServerUseProtseqEp (function : PrtDistCreateRPCServerForEnqueueAndWait)");
		exit(status);
	}

	status = RpcServerRegisterIf2(
		s_PrtDist_v1_0_s_ifspec, // Interface to register.
		NULL, // Use the MIDL generated entry-point vector.
		NULL, // Use the MIDL generated entry-point vector.
		RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, // Forces use of security callback.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // sequential RPC 
		(unsigned)-1, // Infinite max size of incoming data blocks.
		NULL); // Naive security callback.

	if (status)
	{
		PrtDistLog("Runtime reported exception in RpcServerRegisterIf2 (function : PrtDistCreateRPCServerForEnqueueAndWait)");
		exit(status);
	}

	PrtDistLog("RPC Receiver listening ...");
	// Start to listen for remote procedure calls for all registered interfaces.
	// This call will not return until RpcMgmtStopServerListening is called.
	status = RpcServerListen(
		1, // Recommended minimum number of threads.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Recommended maximum number of threads.
		0);

	if (status)
	{
		PrtDistLog("Runtime reported exception in RpcServerListen (function : PrtDistCreateRPCServerForEnqueueAndWait)");
		exit(status);
	}

	return -1;

}

/***********************************************************************************************************/



/***********************************************************************************************************
* Implementation of all the model functions
**/

PRT_VALUE *P_FUN__SEND_IMPL(PRT_MACHINEINST *context)
{
	PRT_FUNSTACK_INFO frame;
	PrtPopFrame((PRT_MACHINEINST_PRIV*)context, &frame);
	PRT_VALUE* target = frame.locals[2U];
	PrtDistSend(context->id, target, frame.locals[1U], frame.locals[0U]);
	PrtFreeLocals((PRT_MACHINEINST_PRIV*)context, &frame);
	return PrtMkBoolValue(PRT_TRUE);
}

PRT_VALUE *P_FUN__CREATECONTAINER_IMPL(PRT_MACHINEINST *context)
{
	PRT_FUNSTACK_INFO frame;
	PrtPopFrame((PRT_MACHINEINST_PRIV*) context, &frame);
	//first step is to get the nodeId from central node.
	int newNodeId;
	while (TRUE != PrtDistGetNextNodeId(&newNodeId));

	//send message to the node manager on the new node to create container.

	int newContainerId;
	while(TRUE != PrtDistCreateContainer(newNodeId, &newContainerId));

	PRT_VALUE* containerMachine = PrtMkDefaultValue(PrtMkPrimitiveType(PRT_KIND_MACHINE));
	containerMachine->valueUnion.mid->machineId = 1; //the first machine.
	containerMachine->valueUnion.mid->processId.data1 = newContainerId;
	containerMachine->valueUnion.mid->processId.data2 = newNodeId;


	PrtFreeLocals((PRT_MACHINEINST_PRIV*)context, &frame);
	return containerMachine;
}

/***********************************************************************************************************/


/***********************************************************************************************************
* RPC Related Functions
*/

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

/************************************************************************************************************/