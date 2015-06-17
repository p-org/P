#include"TestCentralServer.h"

string configurationFile = "PrtDistClusterConfiguration.xml";
handle_t testme_handle = NULL;


void InitializeHandle()
{
	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;

	//get centralserverID
	int centralServerID = atoi(PrtDistClusterConfigGet(CentralServer));
	char buffPort[100];
	_itoa_s(PRTD_SERVICE_PORT, buffPort, 10);
	// Creates a string binding handle.
	// This function is nothing more than a printf.
	// Connection is not done here.
	status = RpcStringBindingCompose(
		NULL, // UUID to bind to.
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP
		// protocol.
		(unsigned char*)(PRTD_CLUSTERMACHINES[centralServerID]), // TCP/IP network
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
		&testme_handle); // Put the result in the implicit binding
	// handle defined in the IDL file.

	if (status)
		exit(status);

}

void CallGetNodeIDFuntion()
{
	int nextNodeId = 0;
	RpcTryExcept
	{
		c_PrtDistCentralServerGetNodeId(testme_handle, 0, &nextNodeId);

	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept
		cout << "Central Server Returned Node " << PRTD_CLUSTERMACHINES[nextNodeId] << endl;
}

void Test_CentralServerGetNodeId()
{
	unsigned char* szStringBinding = NULL;

	//get centralserverID
	int centralServerID = atoi(PrtDistClusterConfigGet(CentralServer));
	for (int i = 0; i < 3; i++)
	{
		CallGetNodeIDFuntion();
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
	InitializeHandle();

	cout << "Testing the Get Node Id Fucntion " << endl;
	Test_CentralServerGetNodeId();

	cout << "Press any key to continue" << endl;
	getchar();
}