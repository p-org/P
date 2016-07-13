#include "TestPrtNodeManager.h"

handle_t testme_handle = NULL;

void callPingFunction(int ServerID)
{
	boolean amIAlive = false;
	RpcTryExcept
	{
		c_PrtDistNMPing(testme_handle, 0, &amIAlive);

	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept

		if (amIAlive)
			cout << "NodeManager is Alive at " << ClusterConfiguration.ClusterMachines[ServerID] << endl;
}

void Test_PServicePing()
{
	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;

	//get centralserverID
	int numOfNodes = ClusterConfiguration.TotalNodes;

	for (int i = 0; i < numOfNodes; i++)
	{
		// Creates a string binding handle.
		// This function is nothing more than a printf.
		// Connection is not done here.
		status = RpcStringBindingCompose(
			NULL, // UUID to bind to.
			(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP
			// protocol.
			(unsigned char*)(ClusterConfiguration.ClusterMachines[i]), // TCP/IP network
			// address to use.
			(unsigned char*)ClusterConfiguration.NodeManagerPort, // TCP/IP port to use.
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

		callPingFunction(i);
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


int main()
{
	cout << "Testing Ping Operation" << endl;
	Test_PServicePing();

	cout << "Press any key to continue" << endl;

	getchar();
}