#include "Timer.h"

static PRT_UINT32 numTimerInstances = 0;
typedef struct TimerContext {
	PRT_UINT32 refCount;
	PRT_UINT32 timerInstance;
	PRT_MACHINEINST *clientContext;
	HANDLE timer;
} TimerContext;

void PRT_FOREIGN_FREE_TimerPtr_IMPL(PRT_UINT64 frgnVal)
{
	if (frgnVal == 0) return;
	TimerContext *timerContext = (TimerContext *)frgnVal;
	timerContext->refCount--;
	if (timerContext->refCount == 0)
	{
		CloseHandle(timerContext->timer);
		PrtFree(timerContext);
	}
}

PRT_BOOLEAN PRT_FOREIGN_ISEQUAL_TimerPtr_IMPL(PRT_UINT64 frgnVal1, PRT_UINT64 frgnVal2)
{
	return frgnVal1 == frgnVal2;
}

PRT_STRING PRT_FOREIGN_TOSTRING_TimerPtr_IMPL(PRT_UINT64 frgnVal)
{
	if (frgnVal == 0) return "";
	TimerContext *timerContext = (TimerContext *)frgnVal;
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	sprintf_s(str, 100, "Timer : %d", timerContext->timerInstance);
	return str;
}

PRT_UINT32 PRT_FOREIGN_GETHASHCODE_TimerPtr_IMPL(PRT_UINT64 frgnVal)
{
	return (PRT_UINT32)frgnVal;
}

PRT_UINT64 PRT_FOREIGN_MKDEF_TimerPtr_IMPL(void)
{
	return 0;
}

PRT_UINT64 PRT_FOREIGN_CLONE_TimerPtr_IMPL(PRT_UINT64 frgnVal)
{
	if (frgnVal == 0) return 0;
	TimerContext *timerContext = (TimerContext *)frgnVal;
	timerContext->refCount++;
	return frgnVal;
}

VOID CALLBACK Callback(PTP_CALLBACK_INSTANCE Instance, PVOID Context, PTP_TIMER Timer)
{
	//printf("Entering Timer Callback\n");	
	TimerContext *timerContext = (TimerContext *)Context;
	PRT_MACHINEINST *context = timerContext->clientContext;
    PRT_VALUE *ev = &P_EVENT_TIMEOUT_STRUCT.value;
	PRT_MACHINEINST* clientMachine = PrtGetMachine(context->process, context->id);
	PRT_VALUE *timerId = PrtMkForeignValue((PRT_UINT64)timerContext, &P_GEND_TYPE_TimerPtr);
	PRT_MACHINESTATE state;
	state.machineId = timerContext->timerInstance;
	state.machineName = "Timer";
	state.stateId = 1;
	state.stateName = "Tick";
	PrtSend(&state, clientMachine, ev, 1, PRT_FUN_PARAM_MOVE, &timerId);
}

PRT_VALUE *P_FUN_CreateTimer_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **owner)
{
	TimerContext *timerContext = (TimerContext *)PrtMalloc(sizeof(TimerContext));
	timerContext->refCount = 1;
	timerContext->clientContext = PrtGetMachine(context->process, *owner);
	timerContext->timer = CreateThreadpoolTimer(Callback, (PVOID) timerContext, NULL);
	timerContext->timerInstance = numTimerInstances;
	numTimerInstances++;
	PrtAssert(timerContext->timer != NULL, "CreateWaitableTimer failed");
	return PrtMkForeignValue((PRT_UINT64)timerContext, &P_GEND_TYPE_TimerPtr);
}

PRT_VALUE *P_FUN_StartTimer_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **timer, PRT_VALUE **time)
{
	LARGE_INTEGER liDueTime;
	PRT_INT timeout_value = (*time)->valueUnion.nt;
	liDueTime.QuadPart = -10000 * timeout_value;
	
	FILETIME ftDueTime;
	ftDueTime.dwLowDateTime = liDueTime.LowPart;
	ftDueTime.dwHighDateTime = liDueTime.HighPart;
	
	TimerContext *timerContext = (TimerContext *)PrtGetForeignValue(*timer);
	SetThreadpoolTimer(timerContext->timer, &ftDueTime, 0, 0);
	return NULL;
}