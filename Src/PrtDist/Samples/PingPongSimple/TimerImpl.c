#include "test.h"

typedef struct TimerContext {
	PRT_VALUE *client;
	HANDLE timer;
	BOOL started;
} TimerContext;

char* GetMachineName(PRT_MACHINEINST *context)
{
	char* name = context->process->program->machines[context->instanceOf]->name;
	return name;
}

VOID CALLBACK Callback(LPVOID arg, DWORD dwTimerLowValue, DWORD dwTimerHighValue)
{
	//printf("Entering Timer Callback\n");	
	PRT_MACHINEINST *context = (PRT_MACHINEINST *)arg;
	TimerContext *timerContext = (TimerContext *)context->extContext;

	PRT_VALUE *ev = PrtMkEventValue(P_EVENT_TIMEOUT);
	PRT_MACHINEINST* clientMachine = PrtGetMachine(context->process, timerContext->client);
	PrtSend(context, clientMachine, ev, 1, PRT_FUN_PARAM_CLONE, context->id);
	PrtFreeValue(ev);
}


PRT_VALUE *P_FUN_StartTimer_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE *timerMachineId, PRT_VALUE *timeout)
{
	LARGE_INTEGER liDueTime;
	PRT_VALUE *p_tmp_ret = NULL;
	BOOL success;

	PRT_MACHINEINST* timerMachine = PrtGetMachine(context->process, timerMachineId);
	TimerContext *timerContext = (TimerContext *)timerMachine->extContext;

	int timeout_value = timeout->valueUnion.nt;
	liDueTime.QuadPart = -10000 * timeout_value;
	success = SetWaitableTimer(timerContext->timer, &liDueTime, 0, Callback, timerMachine, FALSE);
	timerContext->started = success;
	PrtAssert(success, "SetWaitableTimer failed");

	return NULL;
}

PRT_VALUE *P_FUN_CancelTimer_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE *timerMachineId)
{
	LARGE_INTEGER liDueTime;
	PRT_VALUE *p_tmp_ret = NULL;
	BOOL success;
	PRT_VALUE *ev;

	PRT_MACHINEINST* timerMachine = PrtGetMachine(context->process, timerMachineId);
	TimerContext *timerContext = (TimerContext *)timerMachine->extContext;

	timerContext->started = FALSE;
	success = CancelWaitableTimer(timerContext->timer);
	if (success) {
		ev = PrtMkEventValue(P_EVENT_CANCEL_SUCCESS);
		PrtSend(context, PrtGetMachine(context->process, timerContext->client), ev, 1, PRT_FUN_PARAM_CLONE, timerMachine->id);
	}
	else {
		ev = PrtMkEventValue(P_EVENT_CANCEL_FAILURE);
		PrtSend(context, PrtGetMachine(context->process, timerContext->client), ev, 1, PRT_FUN_PARAM_CLONE, timerMachine->id);
	}
	PrtFreeValue(ev);

	return NULL;
}

void P_DTOR_Timer_IMPL(PRT_MACHINEINST *context)
{
	TimerContext *timerContext;
	timerContext = (TimerContext *)context->extContext;

	PrtFreeValue(timerContext->client);
	CloseHandle(timerContext->timer);
	PrtFree(timerContext);
}

void P_CTOR_Timer_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value)
{
	TimerContext *timerContext = (TimerContext *)PrtMalloc(sizeof(TimerContext));
	timerContext->client = PrtCloneValue(value);
	timerContext->started = FALSE;
	timerContext->timer = CreateWaitableTimer(NULL, TRUE, NULL);

	PrtAssert(timerContext->timer != NULL, "CreateWaitableTimer failed");
	context->extContext = timerContext;
}
