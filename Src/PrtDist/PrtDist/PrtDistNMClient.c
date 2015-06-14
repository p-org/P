#include "..\PrtDistClusterManagement\PrtDistNodeManager\PrtDistNodeManagerIDL\PrtDistNodeManager_h.h"
#include "..\PrtDistClusterManagement\PrtDistNodeManager\PrtDistNodeManagerIDL\PrtDistNodeManager_c.c"
#include "PrtDist.h"
#include "CommonFiles/PrtDistMachines.h"
#include "CommonFiles/PrtDistPorts.h"

//central server interaction.
int PrtDistGetNextNodeId()
{
	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;
	handle_t handle;

	//get centralserverID
	int centralServerID = PRTD_CENTRALSERVER_NODEID;

	char buffPort[100];
	_itoa_s(PRTD_SERVICE_PORT, buffPort, 100, 10);
	// Creates a string binding handle.
	// This function is nothing more than a printf.
	// Connection is not done here.
	status = RpcStringBindingCompose(
		NULL, // UUID to bind to.
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP
		// protocol.
		(unsigned char*)(AZUREMACHINEREF[centralServerID]), // TCP/IP network
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
		exit(status);

	int nextNodeId = 0;
	RpcTryExcept
	{
		c_PrtDistCentralServerGetNodeId(handle, ContainerProcess->guid.data2, &nextNodeId);

	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept
		printf("Central Server Returned Node %s\n", AZUREMACHINEREF[nextNodeId]);

	return nextNodeId;

}

int PrtDistCreateContainer(int nodeId)
{
	RPC_STATUS status;
	int newContainerId;
	unsigned char* szStringBinding = NULL;
	handle_t handle;

	char buffPort[100];

	_itoa_s(PRTD_SERVICE_PORT, buffPort, 100, 10);


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
		exit(status);

	char log[100];
	sprintf_s(log, 100, "Creating container on %s", AZUREMACHINEREF[nodeId]);
	PrtDistLog(log);

	RpcTryExcept
	{
		c_PrtDistNMCreateContainer(handle, 0, &newContainerId, &status);

	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		sprintf_s(log, 100, "Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
		PrtDistLog(log);
	}
	RpcEndExcept
		if (status)
			PrtDistLog("Successfully created the Container");

	return newContainerId;

}
