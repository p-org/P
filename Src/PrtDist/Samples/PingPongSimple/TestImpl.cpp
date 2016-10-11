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

void P_CTOR_Safety_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value) {}

void P_DTOR_Safety_IMPL(PRT_MACHINEINST *context) {}

std::wstring ConvertToUnicode(const char* str)
{
	std::string temp(str == NULL ? "" : str);
	return std::wstring(temp.begin(), temp.end());
}

static void LogHandler(PRT_STEP step, PRT_MACHINEINST *sender, PRT_MACHINEINST *receiver, PRT_VALUE* event, PRT_VALUE* payload)
{
    // This LogHandler shows how to use the dgmlMonitor to create a DGML graph of the state machine transitions that
	// were recorded by this LogHandler.  The DGML identifiers computed below are designed to ensure the correct DGML graph is built.
	PRT_MACHINEINST_PRIV * c = (PRT_MACHINEINST_PRIV *)receiver;
	std::wstring machineName = ConvertToUnicode((const char*)c->process->program->machines[c->instanceOf]->name);
	PRT_UINT32 machineId = c->id->valueUnion.mid->machineId;
	std::wstring stateName;
	stateName = ConvertToUnicode((const char*)PrtGetCurrentStateDecl(c)->name);
	
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
		senderMachineName = ConvertToUnicode((const char*)s->process->program->machines[s->instanceOf]->name);
		senderStateName = ConvertToUnicode((const char*)PrtGetCurrentStateDecl(s)->name);
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

	SleepEx(1, TRUE); // SleepEx allows the Win32 Timer to execute.
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
	bool dgml = false;
	bool cooperative = false;
	for (int i = 0; i < argc; i++)
	{
		char* arg = argv[i];
		if (arg[0] == '/' || arg[0] == '-')
		{
			char* name = arg + 1;
			if (strcmp(name, "dgml") == 0)
			{
				dgml = true;
			}
			else if (strcmp(name, "cooperative") == 0)
			{
				cooperative = true;
			}
		}
	}

	if (dgml) {

		// Attempt to connect to Visual Studio running on some machine.  This instance of VS 2015 needs to have the DgmlTestMonitor VSIX extension
		// installed, and the DgmlTestMonitor window needs to be open.  Then you will see the state machine building & animating in real time.
		// dgmlMonitor.Connect("10.137.62.126");

		// Either way you need to also start a new graph file on disk. If you have not connected to VS then this file will be written
		// at the time you call dgmlMonitor.Close(), otherwise VS will maintain the graph inside VS.
		dgmlMonitor.NewGraph(L"d:\\temp\\trace.dgml");
	}


    PRT_GUID processGuid;
    processGuid.data1 = 1;
    processGuid.data2 = 1; //nodeId
    processGuid.data3 = 0;
    processGuid.data4 = 0;
    ContainerProcess = PrtStartProcess(processGuid, &P_GEND_PROGRAM, PrtDistSMExceptionHandler, LogHandler);

	if (cooperative)
	{
		PrtSetSchedulingPolicy(ContainerProcess, PRT_SCHEDULINGPOLICY_COOPERATIVE);
	}

    //create main machine 
    PRT_VALUE* payload = PrtMkNullValue();
    PrtMkMachine(ContainerProcess, P_MACHINE_Client, 1, PRT_FUN_PARAM_CLONE, payload);
    PrtFreeValue(payload);

    // Wait for the timer.
	int iterations = 10;
    while (iterations--) {

		if (cooperative)
		{
			PrtRunProcess(ContainerProcess);
		}
		else {
			SleepEx(1000, TRUE); // SleepEx allows the Win32 Timer to execute.
		}
    }

	if (dgml) {
		dgmlMonitor.Close();
	}

    return 0;

}
