#include "PrtDist.h"
#include "PrtDistGlobals.h"
#include "../PrtDistIDL/PrtDist.h"
#include "../PrtDistIDL/PrtDist_c.c"
// Get a new receiver port 
PRT_INT32 ReceiverPort = PRTD_RECV_PORT;
PRT_RECURSIVE_MUTEX* recPortLock = NULL;

PRT_INT32 PrtDistGetRecvPortNumber()
{
	PRT_INT32 ret;
	ReceiverPort++;
	ret = ReceiverPort;
	return ret;
}


//Function to create a RPC server and wait, should be called using a worker thread.
DWORD WINAPI PrtDistCreateRPCServerForEnqueueAndWait(LPVOID portNumber)
{
	PrtDistLog("Creating RPC server for Enqueue at Port ....");
	RPC_STATUS status;
	char buffPort[100];
	_itoa((PRT_INT32*)portNumber, buffPort, 10);
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

// Foreign function, Used to registering the receiver machine and waiting for messages.
void PrtDistRegisterReceiverService(PRT_MACHINEINST* machineInst)
{
	if (recPortLock == NULL)
	{
		recPortLock = PrtCreateMutex();
	}
	PrtLockMutex(recPortLock);
	//get a receiver port
	PRT_INT32 portNumber = PrtDistGetRecvPortNumber();
	//update the GUID information with the port number
	machineInst->process->guid.data3 = portNumber;
	//register the receiver machine
	PrtDistAddElementToPortMap(portNumber, machineInst);
	PrtUnlockMutex(recPortLock);
	//create a thread that waits on the RPC server

	HANDLE handleToServer = NULL;
	handleToServer = CreateThread(NULL, 0, PrtDistCreateRPCServerForEnqueueAndWait, &portNumber, 0, NULL);
	if (handleToServer == NULL)
	{
		PrtDistLog("Error Creating RPC server in PrtDistRegisterReceiverService");
	}
	else
	{
		PrtDistLog("Receiver listening at port ");
		char log[10];
		_itoa(portNumber, log, 10);
		PrtDistLog(log);
	}
}

// function to send the event
PRT_BOOLEAN PrtDistSend(
	handle_t*	handle,
	PRT_INT32 portNumber,
	PRT_VALUE* event,
	PRT_VALUE* payload
)
{
	PRT_BOOLEAN status;
	RpcTryExcept
	{
		c_PrtDistEnqueue(*handle, portNumber, event, payload, &status);

	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		PrtDistLog("Runtime reported exception in RPC");
		char log[10];
		_itoa(ulCode, log, 10);
		PrtDistLog(log);
		return PRT_FALSE;
	}
	RpcEndExcept

	return PRT_TRUE;
}


handle_t
PrtDistCreateRPCClient(
PRT_INT32 nodeId,
PRT_INT32 portNumber
)
{

}