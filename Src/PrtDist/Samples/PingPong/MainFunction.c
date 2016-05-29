#include "PrtDist.h"
#include "PingPong.h"
#include "Prt.h"
#include <stdio.h>

/* Global variables */
PRT_PROCESS* ContainerProcess;
struct ClusterConfig ClusterConfiguration;
PRT_INT64 sendMessageSeqNumber = 0;


/**
* The main function performs the following steps
* 1) If the createMain option is true then it create the main machine.
* 2) If the createMain option is false then it creates the Container machine.
* 3) It creates a RPC server to listen for messages.

Also note that the machine hosting the main machine does not host container machine.

**/

static char* FirstArgument = NULL;

char* GetApplicationName()
{
    char* last = strrchr(FirstArgument, '/');
    char* last2 = strrchr(FirstArgument, '\\');
    if (last2 > last)
    {
        last = last2;
    }
    return last + 1;
}

void PrintUsage() {

    printf("Usage: %s clusterConfigFile createMain processId nodeId\n", GetApplicationName());
    printf("Where:\n");
    printf("  clusterConfigFile points to the cluser config xml file.\n");
    printf("  createMain (true if 1 and false if 0)\n");
    printf("  processId the id of the process we are talking to\n");
    printf("  nodeid (0 is localhost)\n");
}

int main(int argc, char *argv[])
{
	//The commandline arguments 
    FirstArgument = argv[0];

    if (argc != 5)
    {
        PrintUsage();
        return;
    }

	int createMain = atoi(argv[2]);
	PrtAssert(createMain == 0 || createMain == 1, "CreateMain should be either 0 or 1");
	int processId = atoi(argv[3]);
	PrtAssert(processId >= 0, "Process Id should be positive");
	int nodeId = atoi(argv[4]);
	
	PRT_DBG_START_MEM_BALANCED_REGION
	{
		//Initialize the cluster configuration.
		PrtDistClusterConfigInitialize(argv[1]);
		SetCurrentDirectory(ClusterConfiguration.LocalFolder);
		PRT_GUID processGuid;
		processGuid.data1 = processId;
		processGuid.data2 = nodeId; //nodeId
		processGuid.data3 = 0;
		processGuid.data4 = 0;
		ContainerProcess = PrtStartProcess(processGuid, &P_GEND_PROGRAM, PrtDistSMExceptionHandler, PrtDistSMLogHandler);
		HANDLE listener = NULL;
		PRT_INT32 portNumber = atoi(ClusterConfiguration.ContainerPortStart) + processId;
		listener = CreateThread(NULL, 0, PrtDistCreateRPCServerForEnqueueAndWait, &portNumber, 0, NULL);
		if (listener == NULL)
		{
			PrtDistLog("Error Creating RPC server in PrtDistStartNodeManagerMachine");
		}
		else
		{
			DWORD status;
			//Sleep(3000);
			//check if the thread is all ok
			GetExitCodeThread(listener, &status);
			if (status != STILL_ACTIVE)
				PrtDistLog("ERROR : Thread terminated");

		}

		if (createMain)
		{
			//create main machine 
			PRT_VALUE* payload = PrtMkNullValue();
			PrtMkMachine(ContainerProcess, _P_MACHINE_MAIN, payload);
			PrtFreeValue(payload);
		}
		else
		{
			//create container machine
			PrtDistLog("Creating Container Machine");
			PRT_VALUE* payload = PrtMkNullValue();
			PrtMkMachine(ContainerProcess, P_MACHINE_Container, payload);
			PrtFreeValue(payload);

		}

		//after performing all operations block and wait
		WaitForSingleObject(listener, INFINITE);

		PrtStopProcess(ContainerProcess);
	}
	PRT_DBG_END_MEM_BALANCED_REGION
}

