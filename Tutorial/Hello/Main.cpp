#include <stdio.h>
extern "C" {
#include "PrtDist.h"
#include "hello.h"
#include "Prt.h"
}
#include <string>

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

std::wstring ConvertToUnicode(const char* str)
{
	std::string temp(str == NULL ? "" : str);
	return std::wstring(temp.begin(), temp.end());
}

static void LogHandler(PRT_STEP step, PRT_MACHINESTATE* state, PRT_MACHINEINST *receiver, PRT_VALUE* event, PRT_VALUE* payload)
{
	PRT_MACHINEINST_PRIV * c = (PRT_MACHINEINST_PRIV *)receiver;

	std::wstring machineName = ConvertToUnicode((const char*)c->process->program->machines[c->instanceOf]->name);
	PRT_UINT32 machineId = c->id->valueUnion.mid->machineId;
	char number[20]; // longest 32 bit integer in base 10 is 10 digits, plus room for null terminator.
	_itoa(machineId, number, 16);
	std::wstring machineInstance = ConvertToUnicode(number);
	std::wstring stateName;
	stateName = ConvertToUnicode((const char*)PrtGetCurrentStateDecl(c)->name);
	
	std::wstring eventName;
	std::wstring stateId = machineName + L"." + stateName;

	// optional sender information.
	std::wstring senderMachineName	;
	std::wstring senderStateName	;
	std::wstring senderStateId		;

	if (state != NULL)
	{
		_itoa(state->machineId, number, 16);
		std::wstring senderMachineInstance = ConvertToUnicode(number);
		senderMachineName = ConvertToUnicode((const char*)state->machineName);
		senderStateName = ConvertToUnicode((const char*)state->stateName);
		senderStateId = senderMachineName + L"." + senderStateName;
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
		printf("HALT at %S\n", stateId.c_str());
		break;
	case PRT_STEP_ENQUEUE:
		break;
	case PRT_STEP_DEQUEUE:
		printf("DEQUEUE event %S from %S at %S\n", eventName.c_str(), 
			senderStateId.c_str(), stateId.c_str());
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


void
ExceptionHandler(
	__in PRT_STATUS exception,
	__in PRT_MACHINEINST* vcontext
)
{
	PRT_STRING MachineName = vcontext->process->program->machines[vcontext->instanceOf]->name;
	PRT_UINT32 MachineId = vcontext->id->valueUnion.mid->machineId;
	PRT_MACHINEINST_PRIV *c = (PRT_MACHINEINST_PRIV*)vcontext;

	switch (exception)
	{
	case PRT_STATUS_EVENT_UNHANDLED:
		printf(
			"<EXCEPTION> Machine %s(%d) : Unhandled Event Exception\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_EVENT_OVERFLOW:
		printf(
			"<EXCEPTION> Machine %s(%d) : MaxInstance of Event Exceeded Exception\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_QUEUE_OVERFLOW:
		printf(
			"<EXCEPTION> Queue Size Exceeded Max Limits in Machine %s(%d)\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_ILLEGAL_SEND:
		printf(
			"<EXCEPTION> Machine %s(%d) : Illegal use of send for sending message across process (source and target machines are in different process) ",
			MachineName,
			MachineId);
		break;
	default:
		printf(
			"<EXCEPTION> Machine %s(%d) : Unknown Exception\n",
			MachineName,
			MachineId);
		break;
	}

	exit(-1);

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
	PrtInitialize(&P_GEND_PROGRAM);

    PRT_GUID processGuid;
    processGuid.data1 = 1;
    processGuid.data2 = 1; //nodeId
    processGuid.data3 = 0;
    processGuid.data4 = 0;
    ContainerProcess = PrtStartProcess(processGuid, &P_GEND_PROGRAM, ExceptionHandler, LogHandler);

    //create main machine 
	PRT_VALUE* payload = PrtMkNullValue();
    PRT_MACHINEINST* machine = PrtMkMachine(ContainerProcess, P_MACHINE_Main, 1, PRT_FUN_PARAM_CLONE, payload);
	PrtFreeValue(payload);
	
	//Sleep(INFINITE);

	
    // Wait for the timer.
	int iterations = 10;
    while (iterations--) {
		SleepEx(1000, TRUE); // SleepEx allows the Win32 Timer to execute.
    }

	PrtHaltMachine((PRT_MACHINEINST_PRIV*)machine);
	PrtStopProcess(ContainerProcess);
	
    return 0;

}
