#include<iostream>
#include<process.h>
#include<stdlib.h>
#include"../InterfaceIDL/Interface_h.h"
#include"../InterfaceIDL/Interface_c.c"
#include<time.h>

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

handle_t CreateRPCClient()
{
	handle_t testme_handle = NULL;
	//create RPC client
	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;

	// Creates a string binding handle.
	// This function is nothing more than a printf.
	// Connection is not done here.
	status = RpcStringBindingCompose(
		NULL, // UUID to bind to.
		(unsigned char*)"ncacn_ip_tcp", // Use TCP/IP
		// protocol.
		(unsigned char*)"localhost", // TCP/IP network
		// address to use.
		(unsigned char*)"4748", // TCP/IP port to use.
		NULL, // Protocol dependent network options to use.
		&szStringBinding); // String binding output.

	if (status)
		exit(status);

	// Validates the format of the string binding handle and converts
	// it to a binding handle.
	// Connection is not done here either.
	status = RpcBindingFromStringBinding(
		szStringBinding, // The string binding to validate.
		&testme_handle); // Put the result in the implicit binding
	// handle defined in the IDL file.

	if (status)
		exit(status);

	return testme_handle;
}

void Experiment1()
{
	//call the sync ping message with the payload 100 times
	int numberOfOperations = 0;
	int maxSeconds = 5;
	handle_t handle = CreateRPCClient();
	//create the payload 
	unsigned char arr[100];
	for (int i = 0; i < 100; i++)
	{
		arr[i] = 'a' + i;
	}
	time_t start = time(0);
	
	while (true)
	{
		double seconds_since_start = difftime(time(0), start);
		if (seconds_since_start >= maxSeconds) // check if secs passed
		{
			break;
		}
		else
		{
			RpcTryExcept
			{
				c_SyncPingServer(handle, arr);
				numberOfOperations++;
			}
				RpcExcept(1)
			{
				unsigned long ulCode;
				ulCode = RpcExceptionCode();
				printf("Runtime reported exception in SendValue 0x%lx = %ld\n", ulCode, ulCode);
			}
			RpcEndExcept
		}
	}

	cout << "Number of Operations performed in " << maxSeconds << " " << numberOfOperations << endl;


}

void Experiment2()
{
	//call the sync ping message with the payload 100 times
	int numberOfOperations = 0;
	int maxSeconds = 5;

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

	handle_t handle = CreateRPCClient();
	//create the payload 
	unsigned char arr[100];
	for (int i = 0; i < 100; i++)
	{
		arr[i] = 'a' + i;
	}
	time_t start = time(0);

	while (true)
	{
		double seconds_since_start = difftime(time(0), start);
		if (seconds_since_start >= maxSeconds) // check if secs passed
		{
			break;
		}
		else
		{
			RpcTryExcept
			{
				c_AsyncPingServer(&Async, handle, arr);
				numberOfOperations++;
			}
				RpcExcept(1)
			{
				unsigned long ulCode;
				ulCode = RpcExceptionCode();
				printf("Runtime reported exception in SendValue 0x%lx = %ld\n", ulCode, ulCode);
			}
			RpcEndExcept
		}
	}

	cout << "Number of Operations performed in " << maxSeconds << " " << numberOfOperations << endl;
}

int main()
{
	cout << "Running experiment 1 ..." << endl;
	
	
	int i = 0;
	//perform the test with synchronous rpc
	while (i < 5)
	{
		Experiment1();
		i++;
	}

	Sleep(1000);

	cout << "Running experiment 2 ..." << endl;
	//perform the test with asynchronous rpc
	i = 0;
	while (i < 5)
	{
		Experiment2();
		i++;
	}
	getchar();
	cout << "Press any key to continue" << endl;
	getchar();
}
