#include "PrtDist.h"
#include "../PrtDistIDL/PrtDistIDL_s.c"




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
	printf("Ping Successful");
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
		(unsigned char*)(AZUREMACHINEREF[nodeId]), // TCP/IP network
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


// function to send the event
PRT_BOOLEAN PrtDistSend(
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload
	)
{
	handle_t handle;
	handle = PrtDistCreateRPCClient(target);
	PRT_VALUE* temp = PrtMkNullValue();
	PRT_VALUE* serial_target, *serial_event, *serial_payload, *deserial_payload;
	serial_target = PrtDistSerializeValue(target);
	serial_event = PrtDistSerializeValue(event);
	//PrtPrintValue(payload);
	serial_payload = PrtDistSerializeValue(payload);

	//initialize the asynchronous rpc
	RPC_ASYNC_STATE Async;
	RPC_STATUS status;

	// Initialize the handle.
	status = RpcAsyncInitializeHandle(&Async, sizeof(RPC_ASYNC_STATE));
	if (status)
	{
		// Code to handle the error goes here.
	}

	Async.UserInfo = NULL;
	Async.NotificationType = RpcNotificationTypeEvent;

	Async.u.hEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
	if (Async.u.hEvent == 0)
	{
		// Code to handle the error goes here.
	}

	RpcTryExcept
	{
		
		c_PrtDistEnqueue(handle, serial_target, serial_event, serial_payload);

	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		PrtDistLog("Runtime reported exception in RPC");
		printf("Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
		char log[100];
		_itoa(ulCode, log, 10);
		PrtDistLog(log);
		return PRT_FALSE;
	}
	RpcEndExcept

		return PRT_TRUE;
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