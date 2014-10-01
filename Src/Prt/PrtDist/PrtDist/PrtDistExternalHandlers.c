#include "PrtHeaders.h"
#include "PrtDistExternalHandlers.h"
#include "PrtDistSerializer.h"
#include "PrtDistToString.h"
#include <stdio.h>
#include <string.h>

VOID
PrtExternalExceptionHandler(
__in PRT_EXCEPTIONS exception,
__in PVOID vcontext
)
{
	PRT_SMCONTEXT *context = (PRT_SMCONTEXT*)vcontext;
	PRT_STRING MachineName = context->program->machines[context->instanceOf].name;
	PRT_MACHINE_HANDLE MachineId = context->thisP;

	switch (exception)
	{
	case UnhandledEvent:
		printf(
			"<EXCEPTION> Machine %s(0x%lu) : Unhandled Event Exception\n",
			MachineName,
			MachineId);
		break;
	case EnqueueOnHaltedMachine:
		printf(
			"<EXCEPTION> Machine %s(0x%lu) : Enqueued on a Halted Machine exception\n",
			MachineName,
			MachineId);
		break;
	case MaxInstanceExceeded:
		printf(
			"<EXCEPTION> Machine %s(0x%lu) : MaxInstance of Event Exceeded Exception\n",
			MachineName,
			MachineId);
		break;
	case FailedToAllocateMemory:
		printf(
			"<EXCEPTION> Failed to allocate memory Exception\n");
		break;
	case UnhandledEventInCallS:
		printf(
			"<EXCEPTION> Call Statement terminated with an unhandled event in Machine %s(0x%lu)\n",
			MachineName,
			MachineId);
		break;
	case MaxQueueSizeExceeded:
		printf(
			"<EXCEPTION> Queue Size Exceeded Max Limits in Machine %s(0x%lu)\n",
			MachineName,
			MachineId);
		break;
	default:
		printf(
			"<EXCEPTION> Machine %s(0x%lu) : Unknown Exception\n",
			MachineName,
			MachineId);
		break;
	}

	exit(-1);

}

VOID
PrtExternalLogHandler(
__in PRT_STEP step,
__in PVOID vcontext
)
{
	static FILE *logfile = NULL;
	PRT_SMCONTEXT *context = (PRT_SMCONTEXT*)vcontext;
	PRT_STRING MachineName = context->program->machines[context->instanceOf].name;
	PRT_MACHINE_HANDLE MachineId = context->thisP;
	PRT_STRING eventName = context->program->events[PrtPrimGetEvent(context->trigger.event)].name;
	PRT_STRING payloadValue = PrtValueToString(context->trigger.payload);

	char fileName[100] = "PRT_PPROCESS_LOG_";
	char processId[100]; 
	_itoa(context->parentProcess->processId, processId, 10);
	strcat_s(fileName, 100, processId);
	strcat_s(fileName, 100, ".txt");
	if (logfile == NULL)
	{
		logfile = fopen(fileName, "w+");
	}
	
	char log[1000];
	

	switch (step)
	{
		case traceHalt:
		{
			PRT_STRING stateName = PrtGetCurrentStateDecl(context).name;
			sprintf_s(log, 1000, "<DeleteLog> Machine %s(0x%lu) Deleted in State %s \n", MachineName, MachineId, stateName);
			break;
		}
		case traceEnqueue:
		{
			PRT_UINT32 eventIndex = PrtPrimGetEvent(context->eventQueue.events[context->eventQueue.tailIndex == 0 ? (context->currentLengthOfEventQueue - 1) : (context->eventQueue.tailIndex - 1)].event);
			PRT_STRING eventName = context->program->events[eventIndex].name;
			PRT_STRING payloadValue = PrtValueToString(context->eventQueue.events[context->eventQueue.tailIndex == 0 ? (context->currentLengthOfEventQueue - 1) : (context->eventQueue.tailIndex - 1)].payload);
			sprintf_s(log, 1000, "<EnqueueLog> Enqueued Event < %s, %s > on Machine %s(0x%lu) \n", eventName, payloadValue, MachineName, MachineId);
			break;
		}
		case traceDequeue:
		{
			sprintf_s(log, 1000, "<DequeueLog> Dequeued Event < %s, %s > by Machine %s(0x%lu) \n", eventName, payloadValue, MachineName, MachineId);
			break;
		}
			
		case traceStateChange:
			sprintf_s(log, 1000, "<StateLog> Machine %s(0x%lu) entered state %s\n", MachineName, MachineId, PrtGetCurrentStateDecl(context).name);
			break;
		case traceCreateMachine:
			sprintf_s(log, 1000, "<CreateLog> Machine %s(0x%lu) is created\n", MachineName, MachineId);
			break;
		case traceRaiseEvent:
			sprintf_s(log, 1000, "<RaiseLog> Machine %s(0x%lu) raised event < %s, %s >\n", MachineName, MachineId, eventName, payloadValue);
			break;
		case tracePop:
			sprintf_s(log, 1000, "<PopLog> Machine %ws(0x%lu) executed POP and entered state %ws\n", MachineName, MachineId, PrtGetCurrentStateDecl(context).name);
			break;
		case traceCallStatement:
			sprintf_s(log, 1000, "<CallLog> Machine %s(0x%lu) executed Call and entered state %s\n", MachineName, MachineId, PrtGetCurrentStateDecl(context).name);
			break;
		case traceCallEdge:
			sprintf_s(log, 1000, "<CallLog> Machine %s(0x%lu) took Call transition and entered state %s\n", MachineName, MachineId, PrtGetCurrentStateDecl(context).name);
			break;
		case traceUnhandledEvent:
			sprintf_s(log, 1000, "<PopLog> Machine %s(0x%lu) executed POP because of unhandled event %s and entered state %ws\n", MachineName, MachineId, PrtGetCurrentStateDecl(context).name);
			break;
		case traceActions:
			sprintf_s(log, 1000, "<ActionLog> Machine %s(0x%lu) Executed Action - %s \n", MachineName, MachineId, PrtGetAction(context)->name);
			break;
		case traceQueueResize:
			sprintf_s(log, 1000, "<QueueSizeLog> Machine %s(0x%lu) did Queue Resize (New Queue Size - %d) \n", MachineName, MachineId, context->currentLengthOfEventQueue);
			break;
		case traceExit:
			sprintf_s(log, 1000, "<ExitLog> Machine %s(0x%lu) exited state %ws and executing its exit function\n", MachineName, MachineId, PrtGetCurrentStateDecl(context).name);
			break;
		default:

			break;
	}

	PrtLockMutex(context->parentProcess->lock);
	fputs(log, logfile);
	fflush(logfile);
	PrtUnlockMutex(context->parentProcess->lock);
}