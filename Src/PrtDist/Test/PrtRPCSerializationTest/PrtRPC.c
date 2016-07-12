#include<process.h>
#include "PrtDist.h"
#include "PrtExecution.h"
#include "PrtRPCTest_h.h"
#include "PrtRPCTest_s.c"

PRT_VALUE *ReceivedValue;


DWORD WINAPI WaitThreadForRPCListen(LPVOID lpParam)
{
	printf("Thread - WaitThreadForRPCListen listening ...\n");
	RPC_STATUS status;
	status = RpcMgmtWaitServerListen();
	if (status)
		exit(status);
	printf("In WaitThreadForRPCListen - Wait Server Listen Stopped");
	ExitThread(0);
	return 0;
}


void s_PrtRPCTestSend(
	handle_t handleM,
	PRT_VALUE* value
)
{
	printf("\n");
	PRT_STRING log;
	PRT_VALUE* x = PrtDistDeserializeValue(value);
	ReceivedValue = PrtCloneValue(x);
	
	printf("In Send Value Received :\n");
	printf("Value :");
	log = NULL;
	log = PrtToStringValue(ReceivedValue);
	printf(log);
	printf("\n");
}

void CreateRPCTestServer()
{
	printf("Creating RPC server for PService ....\n");

	RPC_STATUS status;

	status = RpcServerUseProtseqEp(
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP protocol.
		RPC_C_PROTSEQ_MAX_REQS_DEFAULT, // Backlog queue length for TCP/IP.
		(unsigned char*)"4748", // TCP/IP port to use.
		NULL);

	if (status)
	{
		printf(stderr, "Runtime reported exception in RpcServerUseProtseqEp"); 
		exit(status);
	}

	status = RpcServerRegisterIf2(
		s_PrtRPCSerializationTest_v1_0_s_ifspec, // Interface to register.
		NULL, // Use the MIDL generated entry-point vector.
		NULL, // Use the MIDL generated entry-point vector.
		RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, // Forces use of security callback.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Use default number of concurrent calls.
		(unsigned)-1, // Infinite max size of incoming data blocks.
		NULL); // Naive security callback.

	if (status)
	{
		printf(stderr, "Runtime reported exception in RpcServerRegisterIf2");
		exit(status);
	}

	// Start to listen for remote procedure calls for all registered interfaces.
	// This call will not return until RpcMgmtStopServerListening is called.
	status = RpcServerListen(
		1, // Recommended minimum number of threads.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Recommended maximum number of threads.
		1);
	DWORD hThreadId;
	HANDLE workerThread = CreateThread(NULL, 0, WaitThreadForRPCListen, NULL, 0, &hThreadId);
	if (workerThread == NULL)
	{
		printf(stderr, "Runtime reported exception in pthread_create");
		exit(status);
	}

}

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

