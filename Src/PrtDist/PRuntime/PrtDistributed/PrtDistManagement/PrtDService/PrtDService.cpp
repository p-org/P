#include "PrtDService.h"

/* GLobal Variables */
string configurationFile = "../PrtDistManagement/Config/PrtDMConfiguration.xml";

string PrtDGetDeploymentFolder() {
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string DeploymentFolder;
	strcpy_s(DM, 200, "DeploymentFolder");
	listofNodes = XMLDOMParsingNodes(configurationFile.c_str());
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
	PrtDLog(PRTD_SERVICE, (char*)("Deployment Folder = " + DeploymentFolder).c_str());
	return DeploymentFolder;
}

void s_PrtDPingService(handle_t mHandle, boolean* amAlive)
{
	*amAlive = !(*amAlive);
	PrtDLog(PRTD_SERVICE, "Pinged by External Server");
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
		PrtDLog(PRTD_SERVICE, (char*)("Robocopy Successful from " + source + " to " + dest).c_str());

	return true;
}

void s_PrtDCreateNodeManagerForJob(handle_t mHandle, unsigned char* jobName, boolean *status)
{
	string remoteDeploymentFolder = PrtDGetDeploymentFolder();
	string jobS(reinterpret_cast<char*>(jobName));
	string jobFolder = remoteDeploymentFolder + jobS;
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
	string nextport = (PrtDNextNodeManagerPort());
	PrtDLog(PRTD_SERVICE, (char*)("New Node Manager created listening at Port : " + nextport).c_str());
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
		PrtDLog(PRTD_SERVICE, "CreateProcess for Node Manager failed\n");
		*status = false;
		return;
	}

	*status = true;
}
void PrtDGetJobNameAndJobFolder(string* jobName, string* jobFolder)
{
	ifstream read;
	read.open("job.txt");
	read >> *jobName;
	read >> *jobFolder;

	PrtDLog(PRTD_SERVICE, (char*)("Job Folder : " + *jobFolder).c_str());
	PrtDLog(PRTD_SERVICE, (char*)("Job Name : " + *jobName).c_str());
}


string PrtDNextNodeManagerPort()
{
	static int counter;
	int nextPort = START_NODEMANAGER_TCPPORT + counter;
	counter = counter + 1;
	return to_string(nextPort);
}

int main(int argc, char* argv[])
{

	string remoteDeploymentFolder = PrtDGetDeploymentFolder();
	PrtDCreateLogFile(PRTD_SERVICE);
	PrtDCreatePServiceRPCServer();
	PrtDCloseLogFile();
}

void PrtDCreatePServiceRPCServer()
{
	PrtDLog(PRTD_SERVICE, "Creating RPC server for PrtDService ....");
	RPC_STATUS status;

	status = RpcServerUseProtseqEp(
		reinterpret_cast<unsigned char*>("ncacn_ip_tcp"), // Use TCP/IP protocol.
		RPC_C_PROTSEQ_MAX_REQS_DEFAULT, // Backlog queue length for TCP/IP.
		(unsigned char*)PRTD_SERVICE, // TCP/IP port to use.
		NULL);

	if (status)
	{
		std::cerr << "Runtime reported exception in RpcServerUseProtseqEp" << std::endl;
		exit(status);
	}

	status = RpcServerRegisterIf2(
		s_PrtDService_v1_0_s_ifspec, // Interface to register.
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

	PrtDLog(PRTD_SERVICE, "PrtDService listening ...");
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
