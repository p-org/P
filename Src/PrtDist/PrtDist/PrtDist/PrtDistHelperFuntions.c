#include "PrtDist.h"
#include "PrtDistGlobals.h"
#include "../PrtDistIDL/PrtDist.h"
#include "../PrtDistIDL/PrtDist_c.c"


PRT_INT32 PrtDistGetRecvPortNumber(PRT_VALUE* target)
{
	return PRTD_RECV_PORT + target->valueUnion.mid->processId.data1;
}


//Function to create a RPC server and wait, should be called using a worker thread.
DWORD WINAPI PrtDistCreateRPCServerForEnqueueAndWait(LPVOID portNumber)
{
	PrtDistLog("Creating RPC server for Enqueue at Port ....");
	RPC_STATUS status;
	char buffPort[100];
	_itoa(*((PRT_INT32*)portNumber), buffPort, 10);
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
		PrtDistLog("Runtime reported exception in RpcServerListen" );
		exit(status);
	}

}

// function to send the event
PRT_BOOLEAN PrtDistSend(
	handle_t  handle,
	PRT_VALUE* target,
	PRT_VALUE* event,
	PRT_VALUE* payload
)
{
	PRT_BOOLEAN status;
	RpcTryExcept
	{
		c_PrtDistEnqueue(handle, target, event, payload, &status);

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
		(unsigned char*)(AZUREMACHINE_NAMES[nodeId]), // TCP/IP network
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