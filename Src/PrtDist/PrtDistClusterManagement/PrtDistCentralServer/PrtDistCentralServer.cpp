#include "PrtDistCentralServer.h"

/* GLobal Variables */
string configurationFile = "PrtDistManConfiguration.xml";
int myServerID = 0;
char* logFileName = "PRTDIST_CENTRALSERVER.txt";
FILE* logFile;
int CurrentNodeID = 1;
int totalNodes = 3;

//Helper Functions
int PrtDistCentralServerGetNextID()
{
	int retValue = CurrentNodeID;
	if (totalNodes == 0)
	{
		//local node execution
		retValue = 0;

	}
	else
	{
		if (CurrentNodeID == totalNodes)
			CurrentNodeID = 1;
		else
			CurrentNodeID = CurrentNodeID + 1;
	}
	return retValue;
}

///
///PrtDist Central Server Logging
///
void PrtDistCentralServerCreateLogFile()
{
	fopen_s(&logFile, logFileName, "w+");
	fputs("Starting Central Server ..... \n", logFile);
	fflush(logFile);
	fclose(logFile);
}

void PrtDistCentralServerLog(char* log)
{
	fopen_s(&logFile, logFileName, "a+");
	fputs(log, logFile);
	fputs("\n", logFile);
	fflush(logFile);
	fclose(logFile);
}


//rpc related functions

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


void s_PrtDistCentralServerPing(handle_t handle, int server, boolean * amAlive)
{
	char log[100] = "";
	_CONCAT(log, "Pinged by External Server :", AZUREMACHINEREF[server]);
	*amAlive = !(*amAlive);
	PrtDistCentralServerLog(log);
}

void s_PrtDistCentralServerGetNodeId(handle_t handle, int server, int *nodeId)
{
	char log[100] = "";
	*nodeId = PrtDistCentralServerGetNextID();
	_CONCAT(log, "Received Request for a Node Id from ", AZUREMACHINEREF[server]);
	_CONCAT(log, " and returned node ID : ", AZUREMACHINEREF[*nodeId]);
	PrtDistCentralServerLog(log);
}

void PrtDistCentralServerCreateRPCServer()
{
	PrtDistCentralServerLog("Creating RPC server for PrtDistCentralServer ....");
	RPC_STATUS status;
	char buffPort[100];
	_itoa_s(PRTD_CENTRALSERVER_PORT, buffPort, 10);
	status = RpcServerUseProtseqEp(
		reinterpret_cast<unsigned char*>("ncacn_ip_tcp"), // Use TCP/IP protocol.
		RPC_C_PROTSEQ_MAX_REQS_DEFAULT, // Backlog queue length for TCP/IP.
		(RPC_CSTR)buffPort, // TCP/IP port to use.
		NULL);

	if (status)
	{
		std::cerr << "Runtime reported exception in RpcServerUseProtseqEp" << std::endl;
		PrtDistCentralServerLog("Runtime reported exception in RpcServerUseProtseqEp");
		exit(status);
	}

	status = RpcServerRegisterIf2(
		s_PrtDistCentralServer_v1_0_s_ifspec, // Interface to register.
		NULL, // Use the MIDL generated entry-point vector.
		NULL, // Use the MIDL generated entry-point vector.
		RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, // Forces use of security callback.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Use default number of concurrent calls.
		(unsigned)-1, // Infinite max size of incoming data blocks.
		NULL); // Naive security callback.

	if (status)
	{
		std::cerr << "Runtime reported exception in RpcServerRegisterIf2" << std::endl;
		PrtDistCentralServerLog("Runtime reported exception in RpcServerRegisterIf2");
		exit(status);
	}

	PrtDistCentralServerLog("PrtDistCentralServer listening ...");
	// Start to listen for remote procedure calls for all registered interfaces.
	// This call will not return until RpcMgmtStopServerListening is called.
	status = RpcServerListen(
		1, // Recommended minimum number of threads.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Recommended maximum number of threads.
		0);

	if (status)
	{
		PrtDistCentralServerLog("Runtime reported exception in RpcServerListen");
		std::cerr << "Runtime reported exception in RpcServerListen" << std::endl;
		exit(status);
	}

}

int main()
{
	char log[100];

	//create the log file
	PrtDistCentralServerCreateLogFile();
	//initialize the total number of nodes in the system
	totalNodes = PrtDistConfigGetTotalNodes(configurationFile);

	//get my server ID
	myServerID = PrtDistConfigGetCentralServerNode(configurationFile);
	
	log[0] = '\0';
	_CONCAT(log, "Started The Central Server on ", AZUREMACHINEREF[myServerID]);
	PrtDistCentralServerLog(log);
	PrtDistCentralServerLog("Setting up RPC connection .... \n");

	//set up the RPC connection
	PrtDistCentralServerCreateRPCServer();

}