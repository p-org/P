#include "NodeManager.h"

//
// Helper Functions
//
int counter = 0;

void PrtDistNodeManagerCreateLogFile()
{
	g_lock.lock();
	fopen_s(&logFile, logFileName, "w+");
	fputs("Starting NodeManager Service ..... \n", logFile);
	fflush(logFile);
	fclose(logFile);
	g_lock.unlock();
}


void PrtDistNodeManagerLog(char* log)
{
	g_lock.lock();
	fopen_s(&logFile, logFileName, "a+");
	fputs(log, logFile);
	fputs("\n", logFile);
	fflush(logFile);
	fclose(logFile);
	g_lock.unlock();
}

int PrtDistNodeManagerNextContainerId()
{
	g_lock.lock();
	counter = counter + 1;
	g_lock.unlock();
	return (counter);
}

DWORD WINAPI PrtDistNodeManagerCreateRPCServerAndWait(
	LPVOID portNumber
)
{
	PrtDistNodeManagerLog("Creating RPC server for NodeManager ....");
	RPC_STATUS status;

	status = RpcServerUseProtseqEp(
		reinterpret_cast<unsigned char*>("ncacn_ip_tcp"), // Use TCP/IP protocol.
		RPC_C_PROTSEQ_MAX_REQS_DEFAULT, // Backlog queue length for TCP/IP.
		(RPC_CSTR)ClusterConfiguration.NodeManagerPort, // TCP/IP port to use.
		NULL);

	if (status)
	{
		PrtDistNodeManagerLog("Runtime reported exception in RpcServerUseProtseqEp");
		exit(status);
	}

	status = RpcServerRegisterIf2(
		s_PrtDistNodeManager_v1_0_s_ifspec, // Interface to register.
		NULL, // Use the MIDL generated entry-point vector.
		NULL, // Use the MIDL generated entry-point vector.
		RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, // Forces use of security callback.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Use default number of concurrent calls.
		(unsigned)-1, // Infinite max size of incoming data blocks.
		NULL); // Naive security callback.

	if (status)
	{
		PrtDistNodeManagerLog("Runtime reported exception in RpcServerRegisterIf2");
		exit(status);
	}

	PrtDistNodeManagerLog("Node Manager is listening ...");
	// Start to listen for remote procedure calls for all registered interfaces.
	// This call will not return until RpcMgmtStopServerListening is called.
	status = RpcServerListen(
		1, // Recommended minimum number of threads.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Recommended maximum number of threads.
		0);

	if (status)
	{
		PrtDistNodeManagerLog("Runtime reported exception in RpcServerListen");
		exit(status);
	}

	return NULL;

}


//
// RPC Service
//

// Ping service
void s_PrtDistNMPing(handle_t mHandle, int server,  boolean* amAlive)
{
	char log[1000] = "";
	_CONCAT(log, "Pinged by External Server :", ClusterConfiguration.ClusterMachines[server]);
	*amAlive = !(*amAlive);
	PrtDistNodeManagerLog(log);
}

// Create Container.
void s_PrtDistNMCreateContainer(handle_t mHandle, int* containerId, boolean *status)
{
	//get the exe name
	string exeName = ClusterConfiguration.MainExe;
	*containerId = PrtDistNodeManagerNextContainerId();
	char commandLine[1000];
	sprintf_s(commandLine, 1000, "%s %s %d %d %d", exeName.c_str(),ClusterConfiguration.configFileName,  0, *containerId, myNodeId);
	//create the node manager process
	STARTUPINFO si;
	PROCESS_INFORMATION pi;

	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	ZeroMemory(&pi, sizeof(pi));
	
	// Start the child process. 
	if (!CreateProcess(NULL,   // No module name (use command line)
		const_cast<LPSTR>(commandLine),        // Command line
		NULL,           // Process handle not inheritable
		NULL,           // Thread handle not inheritable
		FALSE,          // Set handle inheritance to FALSE
		0,              // No creation flags
		NULL,           // Use parent's environment block
		NULL,           // Use parent's starting directory 
		&si,            // Pointer to STARTUPINFO structure
		&pi)           // Pointer to PROCESS_INFORMATION structure
		)
	{
		PrtDistNodeManagerLog("CreateProcess for Node Manager failed\n");
		*status = false;
		return;
	}

	*status = true;
}

// Create Container.
void PrtDistNMCreateMain()
{
	//get the exe name
	string exeName = ClusterConfiguration.MainExe;
	int containerId = PrtDistNodeManagerNextContainerId();
	char commandLine[1000];
	sprintf_s(commandLine, 1000, "%s %s %d %d %d", exeName.c_str(), ClusterConfiguration.configFileName, 1, containerId, myNodeId);
	//create the node manager process
	STARTUPINFO si;
	PROCESS_INFORMATION pi;

	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	ZeroMemory(&pi, sizeof(pi));


	// Start the child process. 
	if (!CreateProcess(NULL,   // No module name (use command line)
		const_cast<LPSTR>(commandLine),        // Command line
		NULL,           // Process handle not inheritable
		NULL,           // Thread handle not inheritable
		FALSE,          // Set handle inheritance to FALSE
		0,              // No creation flags
		NULL,           // Use parent's environment block
		NULL,           // Use parent's starting directory 
		&si,            // Pointer to STARTUPINFO structure
		&pi)           // Pointer to PROCESS_INFORMATION structure
		)
	{
		PrtDistNodeManagerLog("CreateProcess for Node Manager failed\n");
		return;
	}
}

//Helper Functions
int PrtDistCentralServerGetNextID()
{
	int retValue = 0;
	g_lock.lock();
	if (CurrentNodeID < ClusterConfiguration.TotalNodes)
	{
		retValue = CurrentNodeID;
		CurrentNodeID++;
	}
	else
	{
		CurrentNodeID = 0;
	}
	g_lock.unlock();
	return retValue;
}

void s_PrtDistCentralServerGetNodeId(handle_t handle, int server, int *nodeId)
{
	char log[1000] = "";
	*nodeId = PrtDistCentralServerGetNextID();
	_CONCAT(log, "Received Request for a new NodeId from ", ClusterConfiguration.ClusterMachines[server]);
	_CONCAT(log, " and returned node ID : ", ClusterConfiguration.ClusterMachines[*nodeId]);
	PrtDistNodeManagerLog(log);
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


void _CONCAT(char* dest, char* string1, char* string2)
{
	strcat(dest, string1);
	strcat(dest, string2);
}


///
/// Main
///
int main(int argc, char* argv[])
{
	if (argc != 4)
	{
		PrtDistNodeManagerLog("ERROR : Wrong number of commandline arguments passed\n");
		exit(-1);
	}

	PrtDistClusterConfigInitialize(argv[1]);
	//set the local directory
	SetCurrentDirectory(ClusterConfiguration.LocalFolder);

	PrtDistNodeManagerCreateLogFile();
	int createMain = 0;
	
	myNodeId = atoi(argv[2]);
	createMain = atoi(argv[3]);
	if (myNodeId >= ClusterConfiguration.TotalNodes)
	{
		PrtDistNodeManagerLog("ERROR : Wrong nodeId passed as commandline argument\n");
		exit(1);
	}
		
	char log[1000];
	sprintf_s(log, 1000, "Started NodeManager at : %d", myNodeId);
	PrtDistNodeManagerLog(log);

	HANDLE listener = NULL;
	listener = CreateThread(NULL, 0, PrtDistNodeManagerCreateRPCServerAndWait, NULL, 0, NULL);
	if (listener == NULL)
	{
		PrtDistNodeManagerLog("Error Creating RPC server in PrtDistStartNodeManagerMachine");
	}
	else
	{
		DWORD status;
		GetExitCodeThread(listener, &status);
		if (status != STILL_ACTIVE)
			PrtDistNodeManagerLog("ERROR : Thread terminated");
	}
	if (createMain)
		PrtDistNMCreateMain();

	//after performing all operations block and wait
	WaitForSingleObject(listener, INFINITE);

	
}
