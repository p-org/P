#include "PrtDist.h"
#include "program.h"
#include "CommonFiles\PrtDistPorts.h"

PRT_PROCESS* ContainerProcess;

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
	PrtAssert(argc == 4, "Number of Parameters passed to Container is Incorrect");
	int createMain = atoi(argv[1]);
	PrtAssert(createMain == 0 || createMain == 1, "CreateMain should be either 0 or 1");
	int processId = atoi(argv[2]);
	PrtAssert(processId >= 0, "Process Id should be positive");
	int nodeId = atoi(argv[3]);
	
	PRT_DBG_START_MEM_BALANCED_REGION
	{
		PRT_GUID processGuid;
		processGuid.data1 = processId;
		processGuid.data2 = nodeId; //nodeId
		processGuid.data3 = 0;
		processGuid.data4 = 0;
		ContainerProcess = PrtStartProcess(processGuid, &P_GEND_PROGRAM, PrtDistSMExceptionHandler, PrtDistSMLogHandler);
		HANDLE listener = NULL;
		PRT_INT32 portNumber = PRTD_CONTAINER_RECV_PORT + processId;
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

			PrtDistLog("Receiver listening at port ");
			char log[10];
			_itoa(portNumber, log, 10);
			PrtDistLog(log);
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
			PrtMkMachine(ContainerProcess, P_MACHINE_NodeManager, payload);
			PrtFreeValue(payload);

		}

		//after performing all operations block and wait
		WaitForSingleObject(listener, INFINITE);

		PrtStopProcess(ContainerProcess);
	}
	PRT_DBG_END_MEM_BALANCED_REGION
}