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

std::wstring ConvertToUnicode(const char* str)
{
	std::string temp(str == NULL ? "" : str);
	return std::wstring(temp.begin(), temp.end());
}

static void LogHandler(PRT_STEP step, PRT_MACHINESTATE* state, PRT_MACHINEINST *receiver, PRT_VALUE* event, PRT_VALUE* payload)
{
    // This LogHandler shows how to use the dgmlMonitor to create a DGML graph of the state machine transitions that
	// were recorded by this LogHandler.  The DGML identifiers computed below are designed to ensure the correct DGML graph is built.
	PRT_MACHINEINST_PRIV * c = (PRT_MACHINEINST_PRIV *)receiver;

	std::wstring machineName = ConvertToUnicode((const char*)c->process->program->machines[c->instanceOf]->name);
	PRT_UINT32 machineId = c->id->valueUnion.mid->machineId;
	char number[20]; // longest 32 bit integer in base 10 is 10 digits, plus room for null terminator.
	_itoa(machineId, number, 16);
	std::wstring machineInstance = ConvertToUnicode(number);
	std::wstring stateName;
	stateName = ConvertToUnicode((const char*)PrtGetCurrentStateDecl(c)->name);
	
	std::wstring eventName;
	std::wstring stateId = machineName + L"(0x" + machineInstance + L")." + stateName;
	std::wstring stateLabel = machineName + L"\n" + stateName;

	// optional sender information.
	std::wstring senderMachineName	;
	std::wstring senderStateName	;
	std::wstring senderStateId		;
	std::wstring senderStateLabel	;

	if (state != NULL)
	{
		_itoa(state->machineId, number, 16);
		std::wstring senderMachineInstance = ConvertToUnicode(number);
		senderMachineName = ConvertToUnicode((const char*)state->machineName);
		senderStateName = ConvertToUnicode((const char*)state->stateName);
		senderStateId = senderMachineName + L"(0x" + senderMachineInstance + L")." + senderStateName;
		senderStateLabel = senderMachineName + L"\n" + senderStateName;
	}

	if (event != NULL)
	{
		//find out what state the sender machine is in so we can also log that information.
		PRT_MACHINEINST_PRIV * s = (PRT_MACHINEINST_PRIV *)receiver;
		eventName = ConvertToUnicode((const char*)s->process->program->events[PrtPrimGetEvent(event)]->name);
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
		dgmlMonitor.Connect("10.137.62.126");

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
