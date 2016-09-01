#include "driver.h"

BOOL inStart = FALSE;

typedef struct TimerContext {
  PRT_VALUE *client;
  HANDLE timer;
} TimerContext;

VOID CALLBACK Callback(LPVOID arg, DWORD dwTimerLowValue, DWORD dwTimerHighValue)
{
	inStart = FALSE;
  PRT_MACHINEINST *context = (PRT_MACHINEINST *) arg;
  TimerContext *timerContext = (TimerContext *) context->extContext;
  PRT_VALUE *ev = PrtMkEventValue(P_EVENT_TIMEOUT);
  PRT_MACHINEINST* clientMachine = PrtGetMachine(context->process, timerContext->client);
  PrtSend(context, clientMachine, ev, context->id, PRT_FALSE);
  PrtFreeValue(ev);
}

PRT_VALUE *P_FUN_CreateTimer_IMPL(PRT_MACHINEINST *context)
{
	PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
	TimerContext *timerContext = (TimerContext *)PrtMalloc(sizeof(TimerContext));
	PRT_VALUE *p_tmp_ret = NULL;
	PRT_FUNSTACK_INFO p_tmp_frame;
	//remm to pop frame
	PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
	printf("Creating Timer\n");
	timerContext->client = PrtCloneValue(p_tmp_frame.locals[0]);
	timerContext->timer = CreateWaitableTimer(NULL, TRUE, NULL);
	PrtAssert(timerContext->timer != NULL, "CreateWaitableTimer failed");
	context->extContext = timerContext;
	//remm to free the frame
	PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);

	return context->id;
}

PRT_VALUE *P_FUN_StartTimer_IMPL(PRT_MACHINEINST *context)
{
	PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
	TimerContext *timerContext = (TimerContext *)context->extContext;
	LARGE_INTEGER liDueTime;
	PRT_VALUE *p_tmp_ret = NULL;
	BOOL success;
	PRT_FUNSTACK_INFO p_tmp_frame;
	//remm to pop frame
	PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);

	liDueTime.QuadPart = -10000 * p_tmp_frame.locals[1]->valueUnion.nt;
	printf("Timer received START\n");
	success = SetWaitableTimer(timerContext->timer, &liDueTime, 0, Callback, context, FALSE);
	PrtAssert(success, "SetWaitableTimer failed");

	//remm to free the frame
	PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
	return NULL;
}

PRT_VALUE *P_FUN_CancelTimer_IMPL(PRT_MACHINEINST *context)
{ 
	PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
	TimerContext *timerContext = (TimerContext *)context->extContext;
	LARGE_INTEGER liDueTime;
	PRT_VALUE *p_tmp_ret = NULL;
	BOOL success;
	PRT_VALUE *ev;
	PRT_FUNSTACK_INFO p_tmp_frame;
	//remm to pop frame
	PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);

    printf("Timer received CANCEL\n");
	inStart = FALSE;
    success = CancelWaitableTimer(timerContext->timer);
    if (success) {
      ev = PrtMkEventValue(P_EVENT_CANCEL_SUCCESS);
      PrtSend(context, PrtGetMachine(context->process, timerContext->client), ev, context->id, PRT_FALSE);
    } else {
      ev = PrtMkEventValue(P_EVENT_CANCEL_FAILURE);
      PrtSend(context, PrtGetMachine(context->process, timerContext->client), ev, context->id, PRT_FALSE);
    }
    PrtFreeValue(ev);
	//remm to free the frame
	PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);

	return NULL;
}
