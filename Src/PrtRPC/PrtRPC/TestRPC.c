#include"../PrtCmdIDL/PrtRPC_Types_c.c"
#include"../PrtCmdIDL/PrtRPC_Types_h.h"
#include "PrtCmd.h"
#include"PrtTypes.h"
#include"PrtValues.h"

extern void CreateRPCTestServer();

handle_t CreateRPCClient()
{
	//create RPC client
	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;

	// Creates a string binding handle.
	// This function is nothing more than a printf.
	// Connection is not done here.
	status = RpcStringBindingCompose(
		NULL, // UUID to bind to.
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP
		// protocol.
		(unsigned char*)"localhost", // TCP/IP network
		// address to use.
		(unsigned char*)"4747", // TCP/IP port to use.
		NULL, // Protocol dependent network options to use.
		&szStringBinding); // String binding output.

	if (status)
		exit(status);

	handle_t testme_handle = NULL;

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

void TestPrimitiveType()
{
	handle_t testHandle = CreateRPCClient();

	/// Make the integer type
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);

	//// Make the bool type
	PRT_TYPE boolType = PrtMkPrimitiveType(PRT_KIND_BOOL);

	PRT_VALUE intVal = PrtMkDefaultValue(intType);
	PrtPrimSetInt(intVal, 100);
	printf("Value Sent is : \n");
	PrtCmdPrintValueAndType(intVal);
	printf("\n");
	RpcTryExcept
	{
		SendValue1(testHandle, intVal);
	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception in SendValue1 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept

		RpcTryExcept
	{
		SendValue2(testHandle, intVal);
	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception in SendValue1 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept

}



int main()
{
	//Create RPC test server
	CreateRPCTestServer();
	//Test Cases
	TestPrimitiveType();


	//wait
	getchar();

	//close RPC server
	RpcMgmtStopServerListening(NULL);
}