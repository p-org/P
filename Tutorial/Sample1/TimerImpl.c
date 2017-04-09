#include "CoffeeMachine.h"

static PRT_UINT32 numTimerInstances = 0;
typedef struct TimerContext {
	PRT_UINT32 refCount;
	PRT_UINT32 timerInstance;
	PRT_MACHINEINST *clientContext;
	HANDLE timer;
	BOOL started;
} TimerContext;

void PRT_FORGN_FREE_TimerPtr_IMPL(PRT_UINT64 frgnVal)
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

PRT_BOOLEAN PRT_FORGN_ISEQUAL_TimerPtr_IMPL(PRT_UINT64 frgnVal1, PRT_UINT64 frgnVal2)
{
	return frgnVal1 == frgnVal2;
}

PRT_STRING PRT_FORGN_TOSTRING_TimerPtr_IMPL(PRT_UINT64 frgnVal)
{
	if (frgnVal == 0) return "";
	TimerContext *timerContext = (TimerContext *)frgnVal;
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	sprintf_s(str, 100, "Timer : %d", timerContext->timerInstance);
	return str;
}

PRT_UINT32 PRT_FORGN_GETHASHCODE_TimerPtr_IMPL(PRT_UINT64 frgnVal)
{
	return (PRT_UINT32)frgnVal;
}

PRT_UINT64 PRT_FORGN_MKDEF_TimerPtr_IMPL(void)
{
	return 0;
}

PRT_UINT64 PRT_FORGN_CLONE_TimerPtr_IMPL(PRT_UINT64 frgnVal)
{
	if (frgnVal == 0) return 0;
	TimerContext *timerContext = (TimerContext *)frgnVal;
	timerContext->refCount++;
	return frgnVal;
}

VOID CALLBACK Callback(LPVOID arg, DWORD dwTimerLowValue, DWORD dwTimerHighValue)
{
	//printf("Entering Timer Callback\n");	
	TimerContext *timerContext = (TimerContext *)arg;
	PRT_MACHINEINST *context = timerContext->clientContext;
	PRT_VALUE *ev = PrtMkEventValue(P_EVENT_TIMEOUT);
	PRT_MACHINEINST* clientMachine = PrtGetMachine(context->process, context->id);
	PRT_VALUE *timerId = PrtMkForeignValue((PRT_UINT64)timerContext, &P_GEND_TYPE_TimerPtr);
	PRT_MACHINESTATE state;
	state.machineId = timerContext->timerInstance;
	state.machineName = "Timer";
	state.stateId = 1;
	state.stateName = "Tick";
	PrtSend(&state, clientMachine, ev, 1, PRT_FUN_PARAM_MOVE, &timerId);
	PrtFreeValue(ev);
}

PRT_VALUE *P_FUN_CreateTimer_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **owner)
{
	TimerContext *timerContext = (TimerContext *)PrtMalloc(sizeof(TimerContext));
	timerContext->refCount = 1;
	timerContext->clientContext = PrtGetMachine(context->process, *owner);
	timerContext->started = FALSE;
	timerContext->timer = CreateWaitableTimer(NULL, TRUE, NULL);
	timerContext->timerInstance = numTimerInstances;
	numTimerInstances++;

	PrtAssert(timerContext->timer != NULL, "CreateWaitableTimer failed");
	return PrtMkForeignValue((PRT_UINT64)timerContext, &P_GEND_TYPE_TimerPtr);
}

PRT_VALUE *P_FUN_StartTimer_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **timer, PRT_VALUE **time)
{
	LARGE_INTEGER liDueTime;
	BOOL success;

	TimerContext *timerContext = (TimerContext *)PrtGetForeignValue(*timer);
	int timeout_value = (*time)->valueUnion.nt;
	liDueTime.QuadPart = -10000 * timeout_value;
	success = SetWaitableTimer(timerContext->timer, &liDueTime, 0, Callback, timerContext, FALSE);
	timerContext->started = success;
	PrtAssert(success, "SetWaitableTimer failed");

	return NULL;
}

PRT_VALUE *P_FUN_CancelTimer_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE **timer)
{
	BOOL success;
	PRT_VALUE *ev;
	PRT_MACHINESTATE state;
	TimerContext *timerContext = (TimerContext *)PrtGetForeignValue(*timer);
	state.machineId = timerContext->timerInstance;
	state.machineName = "Timer";
	state.stateId = 1;
	state.stateName = "Cancel";
	
	PrtAssert(timerContext->started, "Trying to cancel a timer without starting it");

	timerContext->started = FALSE;
	success = CancelWaitableTimer(timerContext->timer);
	if (success) {
		ev = PrtMkEventValue(P_EVENT_CANCEL_SUCCESS);
		PrtSend(&state, timerContext->clientContext, ev, 1, PRT_FUN_PARAM_CLONE, *timer);
	}
	else {
		ev = PrtMkEventValue(P_EVENT_CANCEL_FAILURE);
		PrtSend(&state, timerContext->clientContext, ev, 1, PRT_FUN_PARAM_CLONE, *timer);
	}
	PrtFreeValue(ev);

	return NULL;
}

