// Test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <stdio.h>

extern "C" {
#include "PingPongSimple.h"
#include "Prt.h"
}

#include <string>

/* Global variables */
PRT_PROCESS* ContainerProcess;
PRT_INT64 sendMessageSeqNumber = 0;

/* the Stubs */

typedef struct ClientContext
{
	PRT_VALUE* client;
} ClientContext;

typedef struct ServerContext
{
	PRT_VALUE* client;
} ServerContext;

std::wstring ConvertToUnicode(const char* str)
{
	std::string temp(str == nullptr ? "" : str);
	return std::wstring(temp.begin(), temp.end());
}

static void LogHandler(PRT_STEP step, PRT_MACHINESTATE* state, PRT_MACHINEINST* receiver, PRT_VALUE* event,
	PRT_VALUE* payload)
{
	PRT_MACHINEINST_PRIV* c = (PRT_MACHINEINST_PRIV *)receiver;

	std::wstring machineName = ConvertToUnicode((const char*)program->machines[c->instanceOf]->name);
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
	std::wstring senderMachineName;
	std::wstring senderStateName;
	std::wstring senderStateId;
	std::wstring senderStateLabel;

	if (state != nullptr)
	{
		_itoa(state->machineId, number, 16);
		std::wstring senderMachineInstance = ConvertToUnicode(number);
		senderMachineName = ConvertToUnicode((const char*)state->machineName);
		senderStateName = ConvertToUnicode((const char*)state->stateName);
		senderStateId = senderMachineName + L"(0x" + senderMachineInstance + L")." + senderStateName;
		senderStateLabel = senderMachineName + L"\n" + senderStateName;
	}

	if (event != nullptr)
	{
		//find out what state the sender machine is in so we can also log that information.
		PRT_MACHINEINST_PRIV* s = (PRT_MACHINEINST_PRIV *)receiver;
		eventName = ConvertToUnicode((const char*)program->events[PrtPrimGetEvent(event)]->name);
	}

	switch (step)
	{
	case PRT_STEP_HALT:
		break;
	case PRT_STEP_ENQUEUE:
		break;
	case PRT_STEP_DEQUEUE:
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

	SleepEx(1, PRT_TRUE); // SleepEx allows the Win32 Timer to execute.
}

void
PrtDistSMExceptionHandler(
	__in PRT_STATUS exception,
	__in PRT_MACHINEINST* vcontext
)
{
	int log_size = 1000;
	PRT_STRING MachineName = program->machines[vcontext->instanceOf]->name;
	PRT_UINT32 MachineId = vcontext->id->valueUnion.mid->machineId;

	PRT_MACHINEINST_PRIV* c = (PRT_MACHINEINST_PRIV*)vcontext;

	PRT_CHAR log[1000];

	switch (exception)
	{
	case PRT_STATUS_EVENT_UNHANDLED:
		sprintf_s(log,
			log_size,
			"<EXCEPTION> Machine %s(%d) : Unhandled Event Exception\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_EVENT_OVERFLOW:
		sprintf_s(log,
			log_size,
			"<EXCEPTION> Machine %s(%d) : MaxInstance of Event Exceeded Exception\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_QUEUE_OVERFLOW:
		sprintf_s(log,
			log_size,
			"<EXCEPTION> Queue Size Exceeded Max Limits in Machine %s(%d)\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_ILLEGAL_SEND:
		sprintf_s(log,
			log_size,
			"<EXCEPTION> Machine %s(%d) : Illegal use of send for sending message across process (source and target machines are in different process) ",
			MachineName,
			MachineId);
		break;
	default:
		sprintf_s(log,
			log_size,
			"<EXCEPTION> Machine %s(%d) : Unknown Exception\n",
			MachineName,
			MachineId);
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

int main(int argc, char* argv[])
{
	bool cooperative = false;
	for (int i = 0; i < argc; i++)
	{
		char* arg = argv[i];
		if (arg[0] == '/' || arg[0] == '-')
		{
			char* name = arg + 1;
			if (strcmp(name, "cooperative") == 0)
			{
				cooperative = true;
			}
		}
	}

	PRT_GUID processGuid;
	processGuid.data1 = 1;
	processGuid.data2 = 1; //nodeId
	processGuid.data3 = 0;
	processGuid.data4 = 0;
	ContainerProcess = PrtStartProcess(processGuid, &P_GEND_IMPL_DefaultImpl, PrtDistSMExceptionHandler, LogHandler);

	if (cooperative)
	{
		PrtSetSchedulingPolicy(ContainerProcess, PRT_SCHEDULINGPOLICY_COOPERATIVE);
	}

	//create main machine
	PRT_VALUE* payload = PrtMkNullValue();
	PRT_UINT32 machineId;
	PRT_BOOLEAN foundMainMachine = PrtLookupMachineByName("Client", &machineId);
	if (foundMainMachine == PRT_FALSE)
	{
		printf("%s\n", "FAILED TO FIND TestMachine");
		exit(1);
	}
	PrtMkMachine(ContainerProcess, machineId, 1, &payload);
	PrtFreeValue(payload);

	// Wait for the timer.
	int iterations = 10;
	while (iterations--)
	{
		if (cooperative)
		{
			PrtRunProcess(ContainerProcess);
		}
		else
		{
			SleepEx(1000, PRT_TRUE); // SleepEx allows the Win32 Timer to execute.
		}
	}

	return 0;
}