// Test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <stdio.h>
extern "C" {
#include "PrtDist.h"
#include "test.h"
#include "Prt.h"
}

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

static void LogHandler(PRT_STEP step, PRT_MACHINEINST *context)
{
    PrtPrintStep(step, context);
}

/**
* The main function performs the following steps
* 1) If the createMain option is true then it create the main machine.
* 2) If the createMain option is false then it creates the Container machine.
* 3) It creates a RPC server to listen for messages.

Also note that the machine hosting the main machine does not host container machine.

**/

int main(int argc, char *argv[])
{
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

    // Wait for the timer.
    while (1) {
        SleepEx(1000, TRUE);
    }

    return 0;

}
