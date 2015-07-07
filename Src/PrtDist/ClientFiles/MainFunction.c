#include "PrtDist.h"
#include "program.h"
#include "PrtExecution.h"

/* Macros */
#define MAX_THREADPOOL_SIZE 100
#define MIN_THREADPOOL_SIZE 10

/* Global variables */
PRT_PROCESS* ContainerProcess;
PTP_POOL PrtRunStateMachineThreadPool;
struct ClusterConfig ClusterConfiguration;
PRT_INT64 sendMessageSeqNumber = 0;

/**
* The main function performs the following steps
* 1) If the createMain option is true then it create the main machine.
* 2) If the createMain option is false then it creates the Container machine.
* 3) It creates a RPC server to listen for messages.

Also note that the machine hosting the main machine does not host container machine.

**/

int main(int argc, char *argv[])
{
	//The commandline arguments 
	//first: createMain (true if 1 and false if 0)
	//second: processId id
	//third: nodeid (0 is localhost)
	PrtAssert(argc == 5, "Number of Parameters passed to Container is Incorrect");
	int createMain = atoi(argv[2]);
	PrtAssert(createMain == 0 || createMain == 1, "CreateMain should be either 0 or 1");
	int processId = atoi(argv[3]);
	PrtAssert(processId >= 0, "Process Id should be positive");
	int nodeId = atoi(argv[4]);
	
	PRT_DBG_START_MEM_BALANCED_REGION
	{
		
		BOOL bRet = FALSE;

		//Create a global thread pool used by all the state-machines
		PrtRunStateMachineThreadPool = CreateThreadpool(NULL);
		if (NULL == PrtRunStateMachineThreadPool) {
			printf("CreateThreadpool failed. LastError: %u\n",
				GetLastError());
			exit(1);
		}


		SetThreadpoolThreadMaximum(PrtRunStateMachineThreadPool, MAX_THREADPOOL_SIZE);
		bRet = SetThreadpoolThreadMinimum(PrtRunStateMachineThreadPool, MIN_THREADPOOL_SIZE);

		if (FALSE == bRet)
		{
			PrtDistLog("Setting the Minimum thread pool size failed");
			exit(1);
		}

		
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