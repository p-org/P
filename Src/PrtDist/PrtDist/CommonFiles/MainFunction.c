#include "PrtDist.h"

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
	PrtAssert(argc != 4, "Number of Parameters passed to Container is Incorrect");
	int createMain = atoi(argv[1]);
	PrtAssert(createMain != 0 && createMain != 1, "CreateMain should be either 0 or 1");
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
		PrtDistStartNodeManagerMachine(ContainerProcess, PRTD_RECV_PORT + 1, PRT_TRUE);
		PrtStopProcess(ContainerProcess);
	}
	PRT_DBG_END_MEM_BALANCED_REGION
}