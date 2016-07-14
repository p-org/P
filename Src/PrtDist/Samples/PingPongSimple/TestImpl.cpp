// Test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <stdio.h>
extern "C" {
#include "PrtDist.h"
#include "test.h"
#include "Prt.h"
}
#include "DgmlGraphWriter.h"
#include <string>

/* Global variables */
PRT_PROCESS* ContainerProcess;
struct ClusterConfig ClusterConfiguration;
PRT_INT64 sendMessageSeqNumber = 0;
DgmlGraphWriter dgmlMonitor;

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

std::wstring ConvertToUnicode(const char* str)
{
	std::string temp(str == NULL ? "" : str);
	return std::wstring(temp.begin(), temp.end());
}

static void LogHandler(PRT_STEP step, PRT_MACHINEINST *sender, PRT_MACHINEINST *receiver, PRT_VALUE* event, PRT_VALUE* payload)
{
    //PrtPrintStep(step, sender, receiver, eventId, payload);
	PRT_MACHINEINST_PRIV * c = (PRT_MACHINEINST_PRIV *)receiver;
	std::wstring machineName = ConvertToUnicode((const char*)c->process->program->machines[c->instanceOf].name);
	PRT_UINT32 machineId = c->id->valueUnion.mid->machineId;
	std::wstring stateName;
	if (receiver->isModel) {
		stateName = L"model";
	}
	else {
		stateName = ConvertToUnicode((const char*)PrtGetCurrentStateDecl(c)->name);
	}
	std::wstring eventName;
	std::wstring stateId = machineName + L"." + stateName;
	std::wstring stateLabel = machineName + L"\n" + stateName;
	std::wstring senderMachineName;
	std::wstring senderStateName;
	std::wstring senderStateId;
	std::wstring senderStateLabel;
	if (sender != NULL && event != NULL)
	{
		PRT_MACHINEINST_PRIV * s = (PRT_MACHINEINST_PRIV *)sender;
		eventName = ConvertToUnicode((const char*)s->process->program->events[PrtPrimGetEvent(event)].name);
		senderMachineName = ConvertToUnicode((const char*)s->process->program->machines[s->instanceOf].name);
		senderStateName = sender->isModel ? ConvertToUnicode("model") : ConvertToUnicode((const char*)PrtGetCurrentStateDecl(s)->name);
		senderStateId = senderMachineName + L"." + senderStateName;
		senderStateLabel = senderMachineName + L"\n" + senderStateName;
	}

	switch (step)
	{
	case PRT_STEP_HALT:
		dgmlMonitor.NavigateLink(stateId.c_str(), stateLabel.c_str(), stateId.c_str(), stateLabel.c_str(), L"halt", 0);
		break;
	case PRT_STEP_ENQUEUE:
		break;
	case PRT_STEP_DEQUEUE:
		dgmlMonitor.NavigateLink(senderStateId.c_str(), senderStateLabel.c_str(), stateId.c_str(), stateLabel.c_str(), eventName.c_str(), 0);
		break;
	case PRT_STEP_ENTRY:
		break;
	case PRT_STEP_CREATE:
		break;
	case PRT_STEP_RAISE:
		break;
	case PRT_STEP_POP:
		break;
	case PRT_STEP_PUSH:
		break;
	case PRT_STEP_UNHANDLED:
		break;
	case PRT_STEP_DO:
		break;
	case PRT_STEP_EXIT:
		break;
	case PRT_STEP_IGNORE:
		break;
	}
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
	dgmlMonitor.Connect("10.137.62.126");
	dgmlMonitor.NewGraph(L"d:\\temp\\trace.dgml");

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
