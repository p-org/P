#include "PrtDistService.h"

/* GLobal Variables */
string configurationFile = "PrtDMConfiguration.xml";
char* logFileName = "PRTDIST_DEPLOYER.txt";
FILE* logFile;

//
// Helper Functions
//
string PrtDistServiceGetNetworkShare() {
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string DeploymentFolder = "";
	strcpy_s(DM, 200, "NetworkShare");
	listofNodes = XMLDOMParseNodes(configurationFile.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			DeploymentFolder = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}
	PrtDistServiceLog((char*)("Network Share = " + DeploymentFolder).c_str());
	return DeploymentFolder;
}

boolean _ROBOCOPY(string source, string dest)
{
	string copycommand = "robocopy " + source + " " + dest + " > " + "ROBOCOPY_PSERVICE_LOG.txt";
	if (system(copycommand.c_str()) == -1)
	{
		cerr << "Failed to Copy Files from " << source << "in " << dest << endl;
		return false;
	}
	else
		PrtDistServiceLog((char*)("Robocopy Successful from " + source + " to " + dest).c_str());

	return true;
}

void PrtDistServiceGetJobNameAndJobFolder(string* jobName, string* jobFolder)
{
	ifstream read;
	read.open("job.txt");
	read >> *jobName;
	read >> *jobFolder;

	PrtDistServiceLog((char*)("Job Folder : " + *jobFolder).c_str());
	PrtDistServiceLog((char*)("Job Name : " + *jobName).c_str());
}


string PrtDistServiceNextNodeManagerPort()
{
	static int counter;
	int nextPort = START_NODEMANAGER_TCPPORT + counter;
	counter = counter + 1;
	return to_string(nextPort);
}

void PrtDistServiceCreateRPCServer()
{
	PrtDistServiceLog("Creating RPC server for PrtDService ....");
	RPC_STATUS status;
	char buffPort[100];
	_itoa_s(PRTD_SERVICE_PORT, buffPort, 10);
	status = RpcServerUseProtseqEp(
		reinterpret_cast<unsigned char*>("ncacn_ip_tcp"), // Use TCP/IP protocol.
		RPC_C_PROTSEQ_MAX_REQS_DEFAULT, // Backlog queue length for TCP/IP.
		(RPC_CSTR)buffPort, // TCP/IP port to use.
		NULL);

	if (status)
	{
		std::cerr << "Runtime reported exception in RpcServerUseProtseqEp" << std::endl;
		exit(status);
	}

	status = RpcServerRegisterIf2(
		s_PrtDistService_v1_0_s_ifspec, // Interface to register.
		NULL, // Use the MIDL generated entry-point vector.
		NULL, // Use the MIDL generated entry-point vector.
		RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, // Forces use of security callback.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Use default number of concurrent calls.
		(unsigned)-1, // Infinite max size of incoming data blocks.
		NULL); // Naive security callback.

	if (status)
	{
		std::cerr << "Runtime reported exception in RpcServerRegisterIf2" << std::endl;
		exit(status);
	}

	PrtDistServiceLog("PrtDService listening ...");
	// Start to listen for remote procedure calls for all registered interfaces.
	// This call will not return until RpcMgmtStopServerListening is called.
	status = RpcServerListen(
		1, // Recommended minimum number of threads.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Recommended maximum number of threads.
		0);

	if (status)
	{
		std::cerr << "Runtime reported exception in RpcServerListen" << std::endl;
		exit(status);
	}

}


//
// RPC Service
//

// Ping service
void s_PrtDistServicePing(handle_t mHandle, boolean* amAlive)
{
	*amAlive = !(*amAlive);
	PrtDistServiceLog("Pinged by External Server");
}

// Create NodeManager
void s_PrtDistServiceCreateNodeManager(handle_t mHandle, unsigned char* jobName, boolean *status)
{
	string networkShare = PrtDistServiceGetNetworkShare();
	string jobS(reinterpret_cast<char*>(jobName));
	string jobFolder = networkShare + jobS;
	string newLocalJobFolder = "F:\\PLang_Shared\\Jobs\\" + jobS;
	boolean st = _ROBOCOPY(jobFolder, newLocalJobFolder);
	if (!st)
	{
		*status = st;
		return;
	}

	//create the node manager process
	STARTUPINFO si;
	PROCESS_INFORMATION pi;

	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	ZeroMemory(&pi, sizeof(pi));

	char currDir[100];
	GetCurrentDirectory(100, currDir);
	SetCurrentDirectory(newLocalJobFolder.c_str());
	string nextport = PrtDistServiceNextNodeManagerPort();
	PrtDistServiceLog((char*)("New Node Manager created listening at Port : " + nextport).c_str());
	// Start the child process. 
	if (!CreateProcess("NodeManager.exe",   // No module name (use command line)
		(LPTSTR)nextport.c_str(),        // Command line
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
		PrtDistServiceLog("CreateProcess for Node Manager failed\n");
		*status = false;
		return;
	}

	*status = true;
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


///
///PrtDist Deployer Logging
///
void PrtDistServiceCreateLogFile()
{
	fopen_s(&logFile, logFileName, "w+");
	fputs("Starting PrtDistService ..... \n", logFile);
	fflush(logFile);
}

void PrtDistServieCloseLogFile()
{
	fputs("Done with PrtDistService ...... \n", logFile);
	fflush(logFile);
	fclose(logFile);
}

void PrtDistServiceLog(char* log)
{
	fputs(log, logFile);
	fputs("\n", logFile);
	fflush(logFile);
}

///
/// Main
///
int main(int argc, char* argv[])
{
	PrtDistServiceCreateLogFile();
	PrtDistServiceCreateRPCServer();
	PrtDistServieCloseLogFile();
}
