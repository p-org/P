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

void P_DTOR_Timer_IMPL(PRT_MACHINEINST *context)
{
  printf("Entering P_DTOR_Timer_IMPL\n");
  TimerContext *timerContext;
  timerContext = (TimerContext *) context->extContext;
  PrtFreeValue(timerContext->client);
  CloseHandle(timerContext->timer);
  PrtFree(timerContext); 
}

void P_CTOR_Timer_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value)
{
  printf("Entering P_CTOR_Timer_IMPL\n");
  TimerContext *timerContext = (TimerContext *) PrtMalloc(sizeof(TimerContext));
  timerContext->client = PrtCloneValue(value);
  timerContext->timer = CreateWaitableTimer(NULL, TRUE, NULL);
  PrtAssert(timerContext->timer != NULL, "CreateWaitableTimer failed");
  context->extContext = timerContext;
}

void P_SEND_Timer_IMPL(PRT_MACHINEINST *sender, PRT_MACHINEINST *receiver, PRT_VALUE *evnt, PRT_VALUE *payload, PRT_BOOLEAN doTransfer)
{ 
  PrtAssert(doTransfer == PRT_FALSE, "Ownership must stay with caller");
  printf("Entering P_SEND_Timer_IMPL\n");
  PRT_VALUE *ev;
  BOOL success;
  TimerContext *timerContext = (TimerContext *) receiver->extContext;
  LARGE_INTEGER liDueTime;
  liDueTime.QuadPart = -10000 * payload->valueUnion.nt;
  if (!inStart && evnt->valueUnion.ev == P_EVENT_START) {
    printf("Timer received START\n");
    success = SetWaitableTimer(timerContext->timer, &liDueTime, 0, Callback, receiver, FALSE);
	inStart = TRUE;
    PrtAssert(success, "SetWaitableTimer failed");
  } else if (evnt->valueUnion.ev == P_EVENT_CANCEL) {
    printf("Timer received CANCEL\n");
	inStart = FALSE;
    success = CancelWaitableTimer(timerContext->timer);
    if (success) {
      ev = PrtMkEventValue(P_EVENT_CANCEL_SUCCESS);
      PrtSend(sender, PrtGetMachine(receiver->process, timerContext->client), ev, receiver->id, PRT_FALSE);
    } else {
      ev = PrtMkEventValue(P_EVENT_CANCEL_FAILURE);
      PrtSend(sender, PrtGetMachine(receiver->process, timerContext->client), ev, receiver->id, PRT_FALSE);
    }
    PrtFreeValue(ev);
  } else {
    PrtAssert(FALSE, "Illegal event");
  }
}
