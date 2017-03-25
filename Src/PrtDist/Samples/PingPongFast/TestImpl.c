// Test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "PrtDist.h"
#include "PingPongFast.h"
#include "Prt.h"
#include <stdio.h>

/* Global variables */
PRT_PROCESS* ContainerProcess;
struct ClusterConfig ClusterConfiguration;
PRT_INT64 sendMessageSeqNumber = 0;

/* the Stubs */


typedef struct ClientContext {
    PRT_VALUE *client;
} ClientContext;

typedef struct ServerContext {
    PRT_VALUE *client;
} ServerContext;

/**
* The main function performs the following steps
* 1) If the createMain option is true then it create the main machine.
* 2) If the createMain option is false then it creates the Container machine.
* 3) It creates a RPC server to listen for messages.

Also note that the machine hosting the main machine does not host container machine.

**/

long steps = 0;
DWORD startTime = 0;
DWORD runTime = 10000; // 10 seconds.
BOOL stopping = FALSE;

static void LogHandler(PRT_STEP step, PRT_MACHINESTATE* senderState, PRT_MACHINEINST *receiver, PRT_VALUE* eventId, PRT_VALUE* payload)
{
    steps++;
    DWORD now = GetTickCount();
    if (!stopping && now - startTime > runTime)
    {
        stopping = TRUE;
        printf("Ran %d steps in 10 seconds\n", steps);
		PRT_VALUE *haltEvent = PrtMkEventValue(_P_EVENT_HALT);
		PRT_VALUE *nullValue = PrtMkNullValue();
		PRT_MACHINESTATE state;
		state.machineId = 0;
		state.machineName = "App";
		state.stateId = 0;
		state.stateName = "LogHandler";
		PrtSend(&state, receiver, haltEvent, 1, PRT_FUN_PARAM_CLONE, nullValue);
		PrtFreeValue(haltEvent);
		PrtFreeValue(nullValue);
    }
}


int main(int argc, char *argv[])
{
    startTime = GetTickCount();

    PRT_GUID processGuid;
    processGuid.data1 = 1;
    processGuid.data2 = 1; //nodeId
    processGuid.data3 = 0;
    processGuid.data4 = 0;
    ContainerProcess = PrtStartProcess(processGuid, &P_GEND_PROGRAM, PrtDistSMExceptionHandler, LogHandler);

    //create main machine 
    PRT_VALUE* payload = PrtMkNullValue();
    PrtMkMachine(ContainerProcess, P_MACHINE_Client, 1, PRT_FUN_PARAM_CLONE, payload);
    PrtFreeValue(payload);

    return 0;
}
