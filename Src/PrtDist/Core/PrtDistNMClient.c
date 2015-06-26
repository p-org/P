#include "NodeManager_h.h"
#include "NodeManager_c.c"
#include "PrtDist.h"


/******************************************************************************************
* Functions that interact with the NodeManager for creation of container
*/
boolean PrtDistGetNextNodeId(int *nextNodeId)
{
	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;
	handle_t handle;
<<<<<<< HEAD
	char log[MAX_LOG_SIZE];

	sprintf_s(log, MAX_LOG_SIZE, "Trying to connect to central server at %s\n", ClusterConfiguration.CentralServer);
=======
	char log[1000];

	sprintf_s(log, 1000, "Trying to connect to central server on %s\n", ClusterConfiguration.CentralServer);
>>>>>>> 0ef7ecabc5d783e44c1c6801e28aee620826023d
	PrtDistLog(log);

	// Creates a string binding handle.
	// This function is nothing more than a printf.
	// Connection is not done here.
	status = RpcStringBindingCompose(
		NULL, // UUID to bind to.
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP
		// protocol.
		(unsigned char*)ClusterConfiguration.CentralServer, // TCP/IP network
		// address to use.
		(unsigned char*)ClusterConfiguration.NodeManagerPort, // TCP/IP port to use.
		NULL, // Protocol dependent network options to use.
		&szStringBinding); // String binding output.

	if (status)
	{
		PrtDistLog("Failed to RpcStringBindingCompose in PrtDistGetNextNodeId");
		return FALSE;
	}



	// Validates the format of the string binding handle and converts
	// it to a binding handle.
	// Connection is not done here either.
	status = RpcBindingFromStringBinding(
		szStringBinding, // The string binding to validate.
		&handle); // Put the result in the implicit binding
	// handle defined in the IDL file.

	if (status)
	{
		PrtDistLog("Failed to RpcBindingFromStringBinding in PrtDistGetNextNodeId");
		return FALSE;
	}

	int nodeId = -1;
	RpcTryExcept
	{
<<<<<<< HEAD
		c_PrtDistCentralServerGetNodeId(handle, ContainerProcess->guid.data2, &nodeId);
=======
		c_PrtDistCentralServerGetNodeId(handle, ContainerProcess->guid.data2, &nextNodeId);
		sprintf_s(log, 1000, "Central Server Returned Node %d\n", nextNodeId);
		PrtDistLog(log);
>>>>>>> 0ef7ecabc5d783e44c1c6801e28aee620826023d

	}
	RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
<<<<<<< HEAD
		sprintf_s(log, MAX_LOG_SIZE, "Runtime reported exception 0x%lx = %ld\n when executing c_PrtDistCentralServerGetNodeId", ulCode, ulCode);
=======
		sprintf_s(log, 1000, "Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
>>>>>>> 0ef7ecabc5d783e44c1c6801e28aee620826023d
		PrtDistLog(log);
	}
	RpcEndExcept
	
<<<<<<< HEAD
		if (nodeId != -1)
		{
			sprintf_s(log, MAX_LOG_SIZE, "Central Server Returned Node %s\n", ClusterConfiguration.ClusterMachines[nodeId]);
			PrtDistLog(log);
			*nextNodeId = nodeId;
			return TRUE;
		}
			
=======
	sprintf_s(log, 1000, "Central Server Returned Node %d\n",nextNodeId);
	PrtDistLog(log);
>>>>>>> 0ef7ecabc5d783e44c1c6801e28aee620826023d

	return FALSE;

}

boolean PrtDistCreateContainer(int nodeId, int* newContainerId)
{
	RPC_STATUS status;
	int id_param;
	unsigned char* szStringBinding = NULL;
	handle_t handle;

	// Creates a string binding handle.
	// This function is nothing more than a printf.
	// Connection is not done here.
	status = RpcStringBindingCompose(
		NULL, // UUID to bind to.
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP
		// protocol.
		(unsigned char*)(ClusterConfiguration.ClusterMachines[nodeId]), // TCP/IP network
		// address to use.
		(unsigned char*)ClusterConfiguration.NodeManagerPort, // TCP/IP port to use.
		NULL, // Protocol dependent network options to use.
		&szStringBinding); // String binding output.

	if (status)
	{
		PrtDistLog("Failed to RpcStringBindingCompose in PrtDistCreateContainer");
		return FALSE;
	}



	// Validates the format of the string binding handle and converts
	// it to a binding handle.
	// Connection is not done here either.
	status = RpcBindingFromStringBinding(
		szStringBinding, // The string binding to validate.
		&handle); // Put the result in the implicit binding
	// handle defined in the IDL file.

	if (status)
	{
		PrtDistLog("Failed to RpcBindingFromStringBinding in PrtDistCreateContainer");
		return FALSE;
	}

	char log[MAX_LOG_SIZE];
	sprintf_s(log, MAX_LOG_SIZE, "Creating container on %s", ClusterConfiguration.ClusterMachines[nodeId]);
	PrtDistLog(log);

	boolean statusCC = FALSE;
	RpcTryExcept
	{
		c_PrtDistNMCreateContainer(handle, &id_param, &statusCC);

	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		sprintf_s(log, MAX_LOG_SIZE, "Runtime reported exception 0x%lx = %ld\n when executing c_PrtDistNMCreateContainer", ulCode, ulCode);
		PrtDistLog(log);
	}
	RpcEndExcept

	if (statusCC)
	{
		PrtDistLog("Successfully created the Container");
		*newContainerId = id_param;
		return TRUE;
	}
	else
	{ 
		PrtDistLog("Failed to Create Container");
		return FALSE;
	}

}

/****************************************************************************************************************/
