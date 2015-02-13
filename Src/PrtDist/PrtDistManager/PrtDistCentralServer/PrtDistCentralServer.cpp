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
		CurrentNodeID = 0;
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
}

void PrtDistCentralServerCloseLogFile()
{
	fputs("Done with Central Server ...... \n", logFile);
	fflush(logFile);
	fclose(logFile);
}

void PrtDistCentralServerLog(char* log)
{
	fputs(log, logFile);
	fputs("\n", logFile);
	fflush(logFile);
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
	*amAlive = !(*amAlive);
	PrtDistCentralServerLog(_CONCAT("Pinged by External Server :", AZUREMACHINE_NAMES[server]));
}

void s_PrtDistCentralServerGetNodeId(handle_t handle, int server, int *nodeId)
{
	*nodeId = PrtDistCentralServerGetNextID();
	PrtDistCentralServerLog(_CONCAT("Received Request for a Node Id from ", AZUREMACHINE_NAMES[server]));
	PrtDistCentralServerLog(_CONCAT("and returned node ID : ", AZUREMACHINE_NAMES[*nodeId]));
}


int main()
{
	//create the log file
	PrtDistCentralServerCreateLogFile();
	//initialize the total number of nodes in the system
	totalNodes = PrtDistConfigGetTotalNodes(configurationFile);

	//get my server ID
	myServerID = PrtDistConfigGetCentralServerNode(configurationFile);
	PrtDistCentralServerLog(_CONCAT("Started The Central Server on ", AZUREMACHINE_NAMES[myServerID]));
	PrtDistCentralServerLog("Setting up RPC connection .... \n");


}