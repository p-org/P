// Test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "PingPongFast.h"
#include "Prt.h"

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

static void LogHandler(PRT_STEP step, PRT_MACHINESTATE* senderState, PRT_MACHINEINST* receiver, PRT_VALUE* eventId,
	PRT_VALUE* payload)
{
	steps++;
	DWORD now = GetTickCount();
	if (!stopping && now - startTime > runTime)
	{
		stopping = TRUE;
		printf("Ran %d steps in 10 seconds\n", steps);
		PRT_VALUE* haltEvent = &_P_EVENT_HALT_STRUCT.value;
		PRT_VALUE* nullValue = PrtMkNullValue();
		PRT_MACHINESTATE state;
		state.machineId = 0;
		state.machineName = "App";
		state.stateId = 0;
		state.stateName = "LogHandler";
		PrtSend(&state, receiver, haltEvent, 1, &nullValue);
		PrtFreeValue(nullValue);
	}
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

int main(int argc, char* argv[])
{
	startTime = GetTickCount();

	PRT_GUID processGuid;
	processGuid.data1 = 1;
	processGuid.data2 = 1; //nodeId
	processGuid.data3 = 0;
	processGuid.data4 = 0;
	ContainerProcess = PrtStartProcess(processGuid, &P_GEND_IMPL_DefaultImpl, PrtDistSMExceptionHandler, LogHandler);

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

	return 0;
}