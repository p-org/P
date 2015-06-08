#include<iostream>
#include<process.h>
#include<stdlib.h>
#include"../InterfaceIDL/Interface_h.h"
#include"../InterfaceIDL/Interface_s.c"
using namespace std;



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

DWORD WINAPI WaitThreadForRPCListen(LPVOID lpParam)
{
	printf("Thread - WaitThreadForRPCListen is listening ...\n");
	RPC_STATUS status;
	status = RpcMgmtWaitServerListen();
	if (status)
		exit(status);
	printf("In WaitThreadForRPCListen - Wait Server Listen Stopped");
	ExitThread(0);
	return 0;
}

void CreateRPCTestServer()
{
	printf("Creating RPC server ....\n");

	RPC_STATUS status;

	status = RpcServerUseProtseqEp(
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP protocol.
		RPC_C_PROTSEQ_MAX_REQS_DEFAULT, // Backlog queue length for TCP/IP.
		(unsigned char*)"4748", // TCP/IP port to use.
		NULL);

	if (status)
	{
		printf("Runtime reported exception in RpcServerUseProtseqEp");
		exit(status);
	}

	status = RpcServerRegisterIf2(
		s_Example1_v1_0_s_ifspec, // Interface to register.
		NULL, // Use the MIDL generated entry-point vector.
		NULL, // Use the MIDL generated entry-point vector.
		RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, // Forces use of security callback.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Use default number of concurrent calls.
		(unsigned)-1, // Infinite max size of incoming data blocks.
		NULL); // Naive security callback.

	if (status)
	{
		printf("Runtime reported exception in RpcServerRegisterIf2");
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
		printf("Runtime reported exception in pthread_create");
		exit(status);
	}

}



//the test function
void s_SyncPingServer(
	handle_t handleM,
	unsigned char* payload
)
{
	static int count = 0;
	count++;
	//print every 10000 operation
	if (count % 10000 == 0)
	{
		cout << count;
	}
}

void s_AsyncPingServer(
	PRPC_ASYNC_STATE asyncState,
	handle_t handleM,
	unsigned char* payload
	)
{
	static int count = 0;
	count++;
	//print every 10000 operation
	if (count % 10000 == 0)
	{
		cout << count;
	}
}

int main()
{
	cout << "Creating RPC Server ..." << endl;
	CreateRPCTestServer();

	getchar();
	cout << "Press any key to continue" << endl;
	getchar();
}