// Test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "PrtDist.h"
#include "test.h"
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

void P_CTOR_Client_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value)
{
    printf("Entering P_CTOR_Client_IMPL\n");
    ClientContext *clientContext = (ClientContext *)PrtMalloc(sizeof(ClientContext));
    clientContext->client = PrtCloneValue(value);
    context->extContext = clientContext;
}

void P_DTOR_Client_IMPL(PRT_MACHINEINST *context)
{
    printf("Entering P_DTOR_Client_IMPL\n");
    ClientContext *clientContext = (ClientContext *)context->extContext;
    PrtFreeValue(clientContext->client);
    PrtFree(clientContext);
}

void P_CTOR_Server_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value)
{
    printf("Entering P_CTOR_Server_IMPL\n");
    ServerContext *serverContext = (ServerContext *)PrtMalloc(sizeof(ServerContext));
    serverContext->client = PrtCloneValue(value);
    context->extContext = serverContext;
}

void P_DTOR_Server_IMPL(PRT_MACHINEINST *context)
{
    printf("Entering P_DTOR_Server_IMPL\n");
    ServerContext *serverContext = (ServerContext *)context->extContext;
    PrtFreeValue(serverContext->client);
    PrtFree(serverContext);
}

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

static void LogHandler(PRT_STEP step, PRT_MACHINEINST *context)
{
    steps++;
    DWORD now = GetTickCount();
    if (!stopping && now - startTime > runTime)
    {
        stopping = TRUE;
        printf("Ran %d steps in 10 seconds", steps);
		PRT_VALUE *haltEvent = PrtMkEventValue(_P_EVENT_HALT);
		PRT_VALUE *nullValue = PrtMkNullValue();
		PrtSend(context, haltEvent, nullValue, PRT_FALSE);
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
    PrtMkMachine(ContainerProcess, _P_MACHINE_MAIN, payload);
    PrtFreeValue(payload);

    return 0;

}
