#include "program.h"

typedef struct TimerContext {
	PRT_VALUE *client;
} TimerContext;

void P_DTOR_Timer_IMPL(PRT_MACHINEINST *context)
{
	printf("Entering P_DTOR_Timer_IMPL\n");
	TimerContext *timerContext;
	timerContext = (TimerContext *)context->extContext;
	PrtFreeValue(timerContext->client);
	free(timerContext);
}

void P_CTOR_Timer_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value)
{
	printf("Entering P_CTOR_Timer_IMPL\n");
	TimerContext *timerContext = (TimerContext *)malloc(sizeof(TimerContext));
	timerContext->client = PrtCloneValue(value);
	context->extContext = timerContext;
}

void P_SEND_Timer_IMPL(PRT_MACHINEINST *context, PRT_VALUE *evnt, PRT_VALUE *payload)
{
	printf("Entering P_SEND_Timer_IMPL\n");
	PRT_VALUE *ev;
	BOOL success;
	TimerContext *timerContext = (TimerContext *)context->extContext;
	LARGE_INTEGER liDueTime;
	liDueTime.QuadPart = -10000000 * payload->valueUnion.nt;
	if (evnt->valueUnion.ev == P_EVENT_START) {
		printf("Timer received START\n");
		//send time out
		TimerContext *timerContext = (TimerContext *)context->extContext;
		PRT_VALUE *ev = PrtMkEventValue(P_EVENT_TIMEOUT);
		PrtSend(PrtGetMachine(context->process, timerContext->client), ev, context->id);
		PrtFreeValue(ev);
	}
	else {
		PrtAssert(FALSE, "Illegal event");
	}
}