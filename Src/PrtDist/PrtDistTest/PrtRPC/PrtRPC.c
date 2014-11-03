#include<process.h>
#include "PrtDist.h"
#include"PrtDist_s.c"

PRT_VALUE *ReceivedValue;


/*
int main(int argc, char *argv[])
{
	PRT_DBG_START_MEM_BALANCED_REGION
	{
		BinaryBoolFunTest();
	}
	PRT_DBG_END_MEM_BALANCED_REGION

	/*
	TupleTest();
	NamedTupleTest();
	SeqAppendTest();
	SeqPrependTest();
	SeqAppendRemoveTest();
	SeqNestedTest();
	MapTest1();
	MapTest2();
	

	return 0;
}
*/
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


PRT_BOOLEAN s_PrtDistSend(
	handle_t handleM,
	PRT_VALUE* event,
	PRT_VALUE* payload
	)
{
	printf("\n");
	PRT_VALUE* x = PrtDistDeserializeValue(payload);
	ReceivedValue = PrtCloneValue(x);
	printf("In SendValue1 Value Received :");
	PrtPrintValue(ReceivedValue);
	printf("\n");

	return PRT_TRUE;
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
		s_PrtDist_v1_0_s_ifspec, // Interface to register.
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

