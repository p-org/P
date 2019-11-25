#include "PrtExecution.h"
#include "libhandler.h"

// Can only run one P program at a time
PRT_PROGRAMDECL* program;

PRT_TYPE NullType =
{
	PRT_KIND_NULL,
	{(struct PRT_MAPTYPE *)NULL}
};

PRT_TYPE AnyType =
{
	PRT_KIND_ANY,
	{(struct PRT_MAPTYPE *)NULL}
};

PRT_EVENTDECL _P_EVENT_NULL_STRUCT =
{
	{
		PRT_VALUE_KIND_NULL,
		{.ev = PRT_SPECIAL_EVENT_NULL}
	},
	"null",
	0,
	&NullType
};

PRT_EVENTDECL _P_EVENT_HALT_STRUCT =
{
	{
		PRT_VALUE_KIND_EVENT,
		{.ev = PRT_SPECIAL_EVENT_HALT}
	},
	"halt",
	4294967295U,
	&AnyType
};

PRT_VALUE* PRT_CALL_CONV _P_NO_OP_IMPL(_Inout_ struct PRT_MACHINEINST* context, _Inout_ PRT_VALUE*** refLocals)
{
	return NULL;
}

PRT_FUNDECL _P_NO_OP =
{
	"_P_NO_OP",
	&_P_NO_OP_IMPL,
	NULL
};

/* Initialize the function to default assert function */
PRT_ASSERT_FUN _PrtAssert = &PrtAssertDefaultFn;

/* Initialize the function to default print fucntion*/
PRT_PRINT_FUN PrtPrintf = &PrtPrintfDefaultFn;

void PrtFreeTriggerPayload(_In_ PRT_MACHINEINST_PRIV* context)
{
	PrtFreeValue(context->currentTrigger);
	context->currentTrigger = NULL;
	PrtFreeValue(context->currentPayload);
	context->currentPayload = NULL;
}

void PrtSetTriggerPayload(_Inout_ PRT_MACHINEINST_PRIV* context, PRT_VALUE* trigger, PRT_VALUE* payload)
{
	PrtFreeTriggerPayload(context);
	context->currentTrigger = trigger;
	context->currentPayload = payload;
}

void PRT_CALL_CONV PrtSetGlobalVarEx(_Inout_ PRT_MACHINEINST_PRIV* context, _In_ PRT_UINT32 varIndex,
	_In_                                             PRT_VALUE* value,
	_In_                                             PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(value), "value is not valid");
	PRT_VALUE* oldValue = context->varValues[varIndex];
	context->varValues[varIndex] = cloneValue ? PrtCloneValue(value) : value;
	if (oldValue != NULL)
	{
		PrtAssert(PrtIsValidValue(oldValue), "Variable must contain a valid value");
		PrtFreeValue(oldValue);
	}
}

void PRT_CALL_CONV PrtSetGlobalVar(_Inout_ PRT_MACHINEINST_PRIV* context, _In_ PRT_UINT32 varIndex,
	_In_                                           PRT_VALUE* value)
{
	PrtSetGlobalVarEx(context, varIndex, value, PRT_TRUE);
}

void PRT_CALL_CONV PrtSetLocalVarEx(_Inout_ PRT_VALUE** locals, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE* value,
	_In_                                            PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(value), "value is not valid");
	PRT_VALUE* oldValue = locals[varIndex];
	locals[varIndex] = cloneValue ? PrtCloneValue(value) : value;
	if (oldValue != NULL)
	{
		PrtAssert(PrtIsValidValue(oldValue), "Variable must contain a valid value");
		PrtFreeValue(oldValue);
	}
}

// This internal function is how we implement two different scheduling policies.
// The original version is called PRT_SCHEDULINGPOLICY_TASKNEUTRAL which means the advancement of the state
// machine is done on the callers thread (whichever thread), and it is done during PrtMkMachinePrivate
// and PrtSendPrivate by calling this method.  The new scheduling policy allows cooperative multitasking
// on a realtime OS (like NuttX) where the caller creates a special Thread for running all the state machines
// in a given process which goes to sleep when there is nothing to do.  This thread is automatically
// woken up using a semaphore when new work is created via PrtMkMachinePrivate or PrtSendPrivate using this method.
static void PrtScheduleWork(PRT_MACHINEINST_PRIV* context)
{
	PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)context->process;
	switch (privateProcess->schedulingPolicy)
	{
	case PRT_SCHEDULINGPOLICY_TASKNEUTRAL:
	{
		PrtRunStateMachine(context);
	}
	break;
	case PRT_SCHEDULINGPOLICY_COOPERATIVE:
	{
		PRT_COOPERATIVE_SCHEDULER* info = (PRT_COOPERATIVE_SCHEDULER*)privateProcess->schedulerInfo;
		if (info->threadsWaiting > 0)
		{
			// signal the PrtRunProcess method that there is work to do.
			PrtReleaseSemaphore(info->workAvailable);
		}
	}
	break;
	default:
		PrtAssert(PRT_FALSE, "Invalid schedulingPolicy");
		break;
	}
}

PRT_MACHINEINST_PRIV*
PrtMkMachinePrivate(
	_Inout_	         PRT_PROCESS_PRIV* process,
	_In_	         PRT_UINT32 interfaceName,
	_In_	         PRT_UINT32 instanceOf,
	_In_	         PRT_VALUE* payload
)
{
	PrtLockMutex(process->processLock);

	PRT_UINT32 i;

	PRT_UINT8 eQSize = PRT_QUEUE_LEN_DEFAULT;

	// Make space in process list for new machine
	PRT_UINT32 numMachines = process->numMachines;
	PRT_UINT32 machineCount = process->machineCount;
	PRT_MACHINEINST** machines = process->machines;
	if (machineCount == 0)
	{
		machines = (PRT_MACHINEINST **)PrtCalloc(1, sizeof(PRT_MACHINEINST *));
		process->machines = machines;
		process->machineCount = 1;
	}
	else if (machineCount == numMachines)
	{
		PRT_MACHINEINST** newMachines = (PRT_MACHINEINST **)PrtCalloc(2 * machineCount, sizeof(PRT_MACHINEINST *));
		for (i = 0; i < machineCount; i++)
		{
			newMachines[i] = machines[i];
		}
		PrtFree(machines);
		machines = newMachines;
		process->machines = newMachines;
		process->machineCount = 2 * machineCount;
	}

	// Allocate memory for state machine context
	PRT_MACHINEINST_PRIV* context = (PRT_MACHINEINST_PRIV*)PrtCalloc(1, sizeof(PRT_MACHINEINST_PRIV));

	// Assign the interface name
	context->interfaceBound = interfaceName;

	// Add it to the process list
	machines[numMachines] = (PRT_MACHINEINST *)context;
	process->numMachines++;

	//
	// Initialize Machine Identity
	//
	context->process = (PRT_PROCESS *)process;
	context->instanceOf = instanceOf;
	PRT_MACHINEID id;
	id.machineId = process->numMachines; // index begins with 1 since 0 is reserved
	id.processId = process->guid;
	context->id = PrtMkMachineValue(id);

	//
	// Initialize the map used in PrtDist, map from sender to the last seqnumber received
	//
	PRT_TYPE* domType = PrtMkPrimitiveType(PRT_KIND_MACHINE);
	PRT_TYPE* codType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE* recvMapType = PrtMkMapType(domType, codType);
	context->recvMap = PrtMkDefaultValue(recvMapType);
	PrtFreeType(domType);
	PrtFreeType(codType);
	PrtFreeType(recvMapType);

	// Initialize Machine Internal Variables
	//
	context->isRunning = PRT_FALSE;
	context->isHalted = PRT_FALSE;
	context->operation = StateEntry;
	context->returnKind = ReturnStatement;

	context->destStateIndex = 0;

	PrtSetTriggerPayload(context, NULL, PrtCloneValue(payload));
	context->handlerArguments = &context->currentPayload;

	// Initialize machine-dependent per-instance state
	PRT_MACHINEDECL* curMachineDecl = program->machines[instanceOf];
	PRT_UINT32 nVars = curMachineDecl->nVars;

	context->currentState = curMachineDecl->initStateIndex;
	context->varValues = NULL;
	if (nVars > 0)
	{
		context->varValues = PrtCalloc(nVars, sizeof(PRT_VALUE*));
		for (i = 0; i < nVars; i++)
		{
			context->varValues[i] = PrtMkDefaultValue(curMachineDecl->vars[i].type);
		}
	}

	//
	// Initialize various stacks
	//
	context->callStack.length = 0;

	//
	// Initialize event queue
	//
	context->eventQueue.eventsSize = eQSize;
	context->eventQueue.events = (PRT_EVENT*)PrtCalloc(eQSize, sizeof(PRT_EVENT));
	context->eventQueue.headIndex = 0;
	context->eventQueue.tailIndex = 0;
	context->eventQueue.size = 0;

	PRT_UINT32 packSize = PrtGetPackSize(context);

	//
	// Initialize Inherited Deferred Set
	//
	context->inheritedDeferredSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));

	//
	// Initialize the current deferred set
	//
	context->currentDeferredSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));

	//
	// Initialize actions
	//
	context->inheritedActionSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));
	context->currentActionSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));

	//
	//Initialize state machine lock
	//
	context->stateMachineLock = PrtCreateMutex();

	//
	//Log
	//
	PrtLog(PRT_STEP_CREATE, NULL, context, NULL, NULL);

	PrtUnlockMutex(process->processLock);

	//
	// Run the state machine according to the scheduling policy.
	//
	PrtScheduleWork(context);

	return context;
}

PRT_VALUE*
PrtGetCurrentTrigger(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	return context->currentTrigger;
}

PRT_VALUE*
PrtGetCurrentPayload(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	return context->currentPayload;
}

void
PrtSendPrivate(
	_In_	     PRT_MACHINESTATE* state,
	_Inout_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_VALUE* event,
	_In_	     PRT_VALUE* payload
)
{
	PrtAssert(!PrtIsSpecialEvent(event), "Enqueued event must not be null");
	PrtAssert(PrtInhabitsType(payload, PrtGetPayloadType(context, event)),
		"Payload must be member of event payload type");

	PrtLockMutex(context->stateMachineLock);

	if (context->isHalted)
	{
		// drop the event silently
		PrtUnlockMutex(context->stateMachineLock);
		// which means we must free the payload and event now, since we are not storing them in the queue.
		PrtFreeValue(event);
		PrtFreeValue(payload);
		return;
	}

	const PRT_UINT32 eventIndex = PrtPrimGetEvent(event);
	const PRT_UINT32 eventMaxInstances = program->events[eventIndex]->eventMaxInstances;
	const PRT_UINT32 maxQueueSize = program->machines[context->instanceOf]->maxQueueSize;

	PRT_EVENTQUEUE* queue = &context->eventQueue;

	// check if maximum allowed instances of event are already present in queue
	if (eventMaxInstances != 0xffffffff && PrtIsEventMaxInstanceExceeded(queue, eventIndex, eventMaxInstances))
	{
		PrtUnlockMutex(context->stateMachineLock);
		PrtHandleError(PRT_STATUS_EVENT_OVERFLOW, context);
		return;
	}

	// if queue is full, resize the queue if possible
	if (queue->eventsSize == queue->size)
	{
		if (maxQueueSize != 0xffffffff && queue->size == maxQueueSize)
		{
			PrtUnlockMutex(context->stateMachineLock);
			PrtHandleError(PRT_STATUS_QUEUE_OVERFLOW, context);
			return;
		}
		PrtResizeEventQueue(context);
	}

	const PRT_UINT32 tail = queue->tailIndex;

	//
	// Add event to the queue
	//
	queue->events[tail].trigger = event;
	queue->events[tail].payload = payload;
	if (state != NULL)
	{
		queue->events[tail].state = *state;
	}
	else
	{
		queue->events[tail].state.machineId = 0;
		queue->events[tail].state.machineName = NULL;
		queue->events[tail].state.stateId = 0;
		queue->events[tail].state.stateName = NULL;
	}
	queue->size++;
	queue->tailIndex = (tail + 1) % queue->eventsSize;

	//
	//Log
	//
	PrtLog(PRT_STEP_ENQUEUE, state, context, event, payload);
	PrtUnlockMutex(context->stateMachineLock);
	PrtScheduleWork(context);
}

void
PrtEnqueueInOrder(
	_In_	     PRT_VALUE* source,
	_In_	     PRT_INT64 seqNum,
	_Inout_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_VALUE* event,
	_In_	     PRT_VALUE* payload
)
{
	// Check if the enqueued event is in order
	PrtLockMutex(context->stateMachineLock);

	if (context->isHalted)
	{
		PrtUnlockMutex(context->stateMachineLock);
		return;
	}

	if (PrtMapExists(context->recvMap, source) && PrtMapGet(context->recvMap, source)->valueUnion.nt >= seqNum)
	{
		PrtUnlockMutex(context->stateMachineLock);
		// Drop the event
		return;
	}
	PrtMapUpdate(context->recvMap, source, PrtMkIntValue((PRT_INT32)seqNum));
	PrtUnlockMutex(context->stateMachineLock);

	// get the name of the sender machine.
	PRT_MACHINEINST_PRIV* senderMachine = (PRT_MACHINEINST_PRIV*)PrtGetMachine(context->process, source);
	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)senderMachine, &state);
	PrtSendPrivate(&state, context, event, payload);
}

PRT_VALUE* MakeTupleFromArray(_In_ PRT_TYPE* tupleType, _In_ PRT_VALUE** elems)
{
	PRT_UINT32 arity = tupleType->typeUnion.tuple->arity;
	PRT_VALUE* payload = PrtMkDefaultValue(tupleType);
	for (PRT_UINT32 i = 0; i < arity; i++)
	{
		PrtTupleSetEx(payload, i, elems[i], PRT_FALSE);
	}
	return payload;
}

PRT_VALUE* PrtMkTuple(_In_ PRT_TYPE* tupleType, ...)
{
	PRT_UINT32 arity = tupleType->typeUnion.tuple->arity;
	PRT_VALUE* tup = PrtMkDefaultValue(tupleType);

	va_list argp;
	va_start(argp, tupleType);
	for (PRT_UINT32 i = 0; i < arity; i++)
	{
		PRT_VALUE** argPtr = va_arg(argp, PRT_VALUE **);
		PrtTupleSetEx(tup, i, *argPtr, PRT_FALSE);
		*argPtr = NULL;
	}
	va_end(argp);

	return tup;
}

void
PrtGoto(
	_Inout_	        PRT_MACHINEINST_PRIV* context,
	_In_	        PRT_UINT32 destStateIndex,
	_In_	        PRT_UINT32 numArgs,
	...
)
{
	context->returnKind = GotoStatement;
	context->destStateIndex = destStateIndex;
	PRT_VALUE* payload;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE** args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			PRT_VALUE** argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_FUNDECL* entryFun = program->machines[context->instanceOf]->states[destStateIndex].entryFun;
			PRT_TYPE* payloadType = entryFun->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}

	PrtSetTriggerPayload(context, PrtMkEventValue(PRT_SPECIAL_EVENT_NULL), payload);

	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PrtLog(PRT_STEP_GOTO, &state, context, NULL, payload);
}

void
PrtRaise(
	_Inout_	        PRT_MACHINEINST_PRIV* context,
	_In_	        PRT_VALUE* event,
	_In_	        PRT_UINT32 numArgs,

	...
)
{
	PrtAssert(!PrtIsSpecialEvent(event), "Raised event must not be null");
	context->returnKind = RaiseStatement;

	PRT_VALUE* payload;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE** args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			PRT_VALUE** argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_TYPE* payloadType = PrtGetPayloadType(context, event);
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PrtAssert(PrtInhabitsType(payload, PrtGetPayloadType(context, event)),
		"Payload must be member of event payload type");

	PrtSetTriggerPayload(context, event, payload);

	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PrtLog(PRT_STEP_RAISE, &state, context, event, payload);
}

#pragma region Receive Implementation

typedef struct _receive_result_t
{
	int eventId;
	PRT_VALUE* payload;
} receive_result_t;

typedef receive_result_t* receive_result_ptr;
typedef PRT_UINT32* event_id_list_ptr;

#define lh_receive_result_ptr_value(v)      ((receive_result_ptr)lh_ptr_value(v))
#define lh_value_receive_result_ptr(r)      lh_value_ptr(r)

#define lh_event_id_list_ptr_value(v)      ((event_id_list_ptr)lh_ptr_value(v))
#define lh_value_event_id_list_ptr(r)      lh_value_ptr(r)

LH_DEFINE_EFFECT1(prt, receive);
LH_DEFINE_OP1(prt, receive, receive_result_ptr, event_id_list_ptr);

PRT_UINT32 PrtReceiveAsync(
	_In_	        PRT_UINT32 nHandledEvents,
	_In_	        PRT_UINT32* handledEvents,
	_Out_	        PRT_VALUE** payload)
{
	PRT_UINT32* receive_allowed_events = PrtMalloc((nHandledEvents + 1) * sizeof(*receive_allowed_events));
	memcpy(receive_allowed_events, handledEvents, nHandledEvents * sizeof(*receive_allowed_events));
	receive_allowed_events[nHandledEvents] = (PRT_UINT32)(-1);

	receive_result_t* res = prt_receive(receive_allowed_events);
	*payload = res->payload;
	const int event_id = res->eventId;
	PrtFree(res);
	return event_id;
}

// Await an asynchronous request
static lh_value _prt_receive(lh_resume r, lh_value local, lh_value arg)
{
	PRT_MACHINEINST_PRIV* context = (PRT_MACHINEINST_PRIV*)lh_ptr_value(local);
	context->receiveResumption = r;
	context->receiveAllowedEvents = lh_event_id_list_ptr_value(arg);
	context->returnKind = ReceiveStatement;

	// this exits our receive handler to the main event loop
	return lh_value_null;
}

// The main async handler
static const lh_operation _prt_ops[] = {
	{LH_OP_GENERAL, LH_OPTAG(prt, receive), &_prt_receive},
	{LH_OP_NULL, lh_op_null, NULL}
};

static const lh_handlerdef _prt_handler_def = {
	LH_EFFECT(prt), NULL, NULL, NULL, _prt_ops
};

typedef struct receive_handler_args_t
{
	PRT_MACHINEINST* context;
	PRT_VALUE*** args;
	PRT_SM_FUN fun;
} receive_handler_args_t;

lh_value receive_handler_action(lh_value rargsv)
{
	receive_handler_args_t* rargs = (receive_handler_args_t*)lh_value_any_ptr(rargsv);
	return lh_value_ptr(rargs->fun(rargs->context, rargs->args));
}

void* prt_receive_handler(PRT_MACHINEINST_PRIV* context, PRT_SM_FUN action, PRT_VALUE*** args)
{
	receive_handler_args_t rargs = { (PRT_MACHINEINST*)context, args, action };
	return lh_ptr_value(lh_handle(&_prt_handler_def, lh_value_ptr(context), &receive_handler_action, lh_value_any_ptr(&
		rargs)));
}

#pragma endregion

void
PrtPushState(
	_Inout_	        PRT_MACHINEINST_PRIV* context,
	_In_	        PRT_UINT32 stateIndex
)
{
	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);

	PRT_UINT16 packSize = PrtGetPackSize(context);
	PRT_UINT32* currDef = PrtGetDeferredPacked(context, context->currentState);
	PRT_UINT32* currActions = PrtGetActionsPacked(context, context->currentState);
	PRT_UINT32* currTransitions = PrtGetTransitionsPacked(context, context->currentState);

	{
		const PRT_UINT16 length = context->callStack.length;
		PrtAssert(length < PRT_MAX_STATESTACK_DEPTH, "State stack overflow");
		context->callStack.stateStack[length].stateIndex = context->currentState;
		context->callStack.stateStack[length].inheritedDeferredSetCompact = PrtClonePackedSet(
			context->inheritedDeferredSetCompact, packSize);
		context->callStack.stateStack[length].inheritedActionSetCompact = PrtClonePackedSet(
			context->inheritedActionSetCompact, packSize);
		context->callStack.length = length + 1;
	}

	// Update the defered set inherited by state-machine
	// D = (D + d) - a - e
	for (PRT_UINT16 i = 0; i < packSize; ++i)
	{
		// Update the actions set inherited by state-machine
		// A = (A - d) + a - e
		context->inheritedActionSetCompact[i] &= ~currDef[i]; // A - d
		context->inheritedActionSetCompact[i] |= currActions[i]; // + a
		context->inheritedActionSetCompact[i] &= ~currTransitions[i]; // -e

		context->inheritedDeferredSetCompact[i] |= currDef[i]; // D + d
		context->inheritedDeferredSetCompact[i] &= ~currActions[i]; // - a
		context->inheritedDeferredSetCompact[i] &= ~currTransitions[i]; // - e
	}

	context->currentState = stateIndex;

	PrtLog(PRT_STEP_PUSH, &state, context, NULL, NULL);
}

void
PrtPop(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	context->returnKind = PopStatement;
	//PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
	//PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
	// Actual pop happens in PrtPopState; the exit function must be executed first.
}

PRT_BOOLEAN
PrtPopState(
	_Inout_	        PRT_MACHINEINST_PRIV* context,
	_In_	        PRT_BOOLEAN isPopStatement
)
{
	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);

	const PRT_UINT16 length = context->callStack.length;
	if (length == 0)
	{
		// The stack can become empty because of either an unhandled event or en explicit pop.
		// We currently do not distinguish between the two in our logging.
		if (isPopStatement)
		{
			PrtHandleError(PRT_STATUS_EVENT_UNHANDLED, context);
		}
		else if (PrtPrimGetEvent(context->currentTrigger) == PRT_SPECIAL_EVENT_HALT)
		{
			PrtHaltMachine(context);
			return PRT_TRUE;
		}
		else
		{
			PrtHandleError(PRT_STATUS_EVENT_UNHANDLED, context);
		}
		return PRT_FALSE;
	}

	context->callStack.length = length - 1;
	const PRT_STATESTACK_INFO popped_state = context->callStack.stateStack[length - 1];
	context->currentState = popped_state.stateIndex;

	const PRT_UINT16 pack_size = PrtGetPackSize(context);
	for (PRT_UINT16 i = 0; i < pack_size; i++)
	{
		context->inheritedDeferredSetCompact[i] = popped_state.inheritedDeferredSetCompact[i];
		context->inheritedActionSetCompact[i] = popped_state.inheritedActionSetCompact[i];
	}
	PrtFree(popped_state.inheritedDeferredSetCompact);
	PrtFree(popped_state.inheritedActionSetCompact);

	PrtUpdateCurrentActionsSet(context);
	PrtUpdateCurrentDeferredSet(context);

	if (isPopStatement)
	{
		PrtLog(PRT_STEP_POP, &state, context, NULL, NULL);
	}
	else
	{
		// unhandled event
		PrtLog(PRT_STEP_UNHANDLED, &state, context, NULL, NULL);
	}
	return PRT_FALSE;
}

PRT_RETURN_KIND PrtResume(PRT_MACHINEINST_PRIV* context, PRT_UINT32 eventId, PRT_VALUE* payload)
{
	receive_result_t* res = (receive_result_t*)PrtMalloc(sizeof(*res));
	res->eventId = eventId;
	res->payload = payload;

	lh_resume resume = context->receiveResumption;
	context->receiveResumption = NULL;

	PrtFree(context->receiveAllowedEvents);
	context->receiveAllowedEvents = NULL;

	// Call the P continuation -- assume it will `return`, since it will override that otherwise.
	context->returnKind = ReturnStatement;
	lh_release_resume(resume, lh_value_ptr(context), lh_value_receive_result_ptr(res));
	return context->returnKind;
}

bool PrtReceiveWaitingOnEvent(PRT_MACHINEINST_PRIV* context, PRT_UINT32 event_value)
{
	if (context->receiveAllowedEvents == NULL)
	{
		return false;
	}

	const PRT_UINT32 sentinel = (PRT_UINT32)(-1);
	for (PRT_UINT32* event_id = context->receiveAllowedEvents; *event_id != sentinel; event_id++)
	{
		if (*event_id == event_value)
		{
			return true;
		}
	}
	return false;
}

PRT_BOOLEAN PrtHandleUserReturn(PRT_MACHINEINST_PRIV* context)
{
	PrtAssert(
		context->postHandlerOperation == DequeueOrReceive ||
		context->returnKind == ReturnStatement ||
		context->returnKind == ReceiveStatement,
		"Can only pop, goto, or raise in entry and action handlers. Not allowed for exit or transition.");

	switch (context->returnKind)
	{
	case PopStatement:
		context->operation = ExitState;
		context->postHandlerOperation = PopState;
		break;
	case GotoStatement:
		context->operation = ExitState;
		context->postHandlerOperation = GotoState;
		break;
	case RaiseStatement:
		context->operation = HandleCurrentEvent;
		break;
	case ReceiveStatement:
		context->operation = DequeueOrReceive;
		break;
	case ReturnStatement:
		context->operation = context->postHandlerOperation;
		break;
	default:
		PRT_DBG_ASSERT(0, "Unexpected case in switch");
		return PRT_FALSE;
	}
	return PRT_TRUE;
}

PRT_BOOLEAN PrtCallEventHandler(PRT_MACHINEINST_PRIV* context, PRT_SM_FUN function, PRT_VALUE*** args)
{
	PrtAssert(context->receiveResumption == NULL && context->receiveAllowedEvents == NULL,
		"When waiting on receive, must resume");
	context->returnKind = ReturnStatement;
	prt_receive_handler(context, function, args);
	return PrtHandleUserReturn(context);
}

PRT_BOOLEAN PrtCallEntryHandler(PRT_MACHINEINST_PRIV* context)
{
	PrtUpdateCurrentActionsSet(context);
	PrtUpdateCurrentDeferredSet(context);

	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PrtLog(PRT_STEP_ENTRY, &state, context, NULL, NULL);

	PRT_STATEDECL* currentState = PrtGetCurrentStateDecl(context);
	PRT_FUNDECL* entryFun = currentState->entryFun;
	return PrtCallEventHandler(context, entryFun->implementation, &context->handlerArguments);
}

PRT_BOOLEAN PrtCallExitHandler(PRT_MACHINEINST_PRIV* context)
{
	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PrtLog(PRT_STEP_EXIT, &state, context, NULL, NULL);

	PRT_STATEDECL* currentState = PrtGetCurrentStateDecl(context);
	PRT_FUNDECL* exitFun = currentState->exitFun;
	// exit handlers are always 0-ary
	return PrtCallEventHandler(context, exitFun->implementation, NULL);
}

PRT_BOOLEAN PrtCallTransitionHandler(PRT_MACHINEINST_PRIV* context)
{
	const PRT_UINT32 trans_index = PrtFindTransition(context, PrtPrimGetEvent(context->currentTrigger));
	PRT_STATEDECL* state_decl = PrtGetCurrentStateDecl(context);
	PRT_FUNDECL* trans_fun = state_decl->transitions[trans_index].transFun;
	return PrtCallEventHandler(context, trans_fun->implementation, &context->handlerArguments);
}

PRT_BOOLEAN PrtHandleEvent(PRT_MACHINEINST_PRIV* context)
{
	PrtAssert(context->receiveResumption == NULL, "Should be in DequeueOrReceive state");

	const PRT_UINT32 eventValue = PrtPrimGetEvent(context->currentTrigger);

	// on eventValue push state ;
	if (PrtIsPushTransition(context, eventValue))
	{
		PrtTakeTransition(context, eventValue);
		context->operation = StateEntry;
		return PRT_TRUE;
	}

	// on eventValue ...
	if (PrtIsTransitionPresent(context, eventValue))
	{
		context->operation = ExitState;
		context->postHandlerOperation = HandleTransition;
		return PRT_TRUE;
	}

	if (PrtIsActionInstalled(eventValue, context->currentActionSetCompact))
	{
		PRT_DODECL* curr_action_decl = PrtGetAction(context, eventValue);
		PRT_FUNDECL* do_fun = curr_action_decl->doFun;
		PRT_MACHINESTATE state;
		PrtGetMachineState((PRT_MACHINEINST*)context, &state);

		// ignore eventValue
		if (do_fun == NULL)
		{
			PRT_VALUE* event = PrtMkEventValue(eventValue);
			PrtLog(PRT_STEP_IGNORE, &state, context, event, NULL);
			PrtFreeValue(event);

			// always more to do...
			context->operation = DequeueOrReceive;
			context->postHandlerOperation = DequeueOrReceive;
			return PRT_TRUE;
		}

		// on eventValue do <fun>
		PrtLog(PRT_STEP_DO, &state, context, NULL, NULL);
		return PrtCallEventHandler(context, do_fun->implementation, &context->handlerArguments);
	}

	// event unhandled; try popping and retrying until there's an error.
	context->operation = ExitState;
	context->postHandlerOperation = UnhandledEvent;
	return PRT_TRUE;
}

static PRT_BOOLEAN
PrtDequeueOrReceive(_Inout_ PRT_MACHINEINST_PRIV* context, PRT_VALUE* trigger, PRT_VALUE* payload)
{
	if (context->receiveResumption == NULL)
	{
		PrtSetTriggerPayload(context, trigger, payload);
		context->operation = HandleCurrentEvent;
		return PRT_TRUE;
	}

	PrtResume(context, PrtPrimGetEvent(trigger), payload);
	PrtFreeValue(trigger);
	return PrtHandleUserReturn(context);
}

static PRT_BOOLEAN
PrtStepStateMachine(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	PrtAssert(context->isRunning, "The caller should have set context->isRunning to TRUE");

	switch (context->operation)
	{
	case StateEntry:
		context->postHandlerOperation = DequeueOrReceive;
		return PrtCallEntryHandler(context);
	case DequeueOrReceive:
		PrtLockMutex(context->stateMachineLock);
		// If the machine is blocked on a receive statement, then
		// PrtDequeueEvent is guaranteed to return an event meant
		// for that receive statement.
		PRT_VALUE *trigger, *payload;
		const PRT_BOOLEAN did_dequeue = PrtDequeueEvent(context, &trigger, &payload);
		PrtUnlockMutex(context->stateMachineLock);

		// Similarly to above, PrtDequeueOrReceive will either
		// kick off a new event handling sequence or resume the
		// blocked event handler
		return did_dequeue && PrtDequeueOrReceive(context, trigger, payload);
	case HandleCurrentEvent:
		// Either a raise or a normal dequeue; offload to complex function
		context->postHandlerOperation = DequeueOrReceive;
		return PrtHandleEvent(context);
	case ExitState:
		return PrtCallExitHandler(context);
	case PopState:
		PRT_DBG_ASSERT(context->postHandlerOperation == PopState,
			"pop should only be reachable through ExitState(PopState)");
		context->operation = DequeueOrReceive;
		context->postHandlerOperation = DequeueOrReceive;
		return !PrtPopState(context, PRT_TRUE);
	case GotoState:
		PRT_DBG_ASSERT(context->postHandlerOperation == GotoState,
			"goto should only be reachable through ExitState(GotoState)");
		context->currentState = context->destStateIndex;
		context->operation = StateEntry;
		return PRT_TRUE;
	case HandleTransition:
		PRT_DBG_ASSERT(context->postHandlerOperation == HandleTransition,
			"transition handlers should only be reachable through ExitState(HandleTransition)");
		context->postHandlerOperation = TakeTransition;
		return PrtCallTransitionHandler(context);
	case TakeTransition:
		PRT_DBG_ASSERT(context->postHandlerOperation == TakeTransition,
			"state transitions should only be reachable through HandleTransition");
		PrtTakeTransition(context, PrtPrimGetEvent(context->currentTrigger));
		context->operation = StateEntry;
		return PRT_TRUE;
	case UnhandledEvent:
		PRT_DBG_ASSERT(context->postHandlerOperation == UnhandledEvent,
			"unhandled state popping should only be reachable through ExitState(UnhandledEvent)");
		context->operation = HandleCurrentEvent;
		return !PrtPopState(context, PRT_FALSE);
	}

	return PRT_FALSE;
}

void
PrtRunStateMachine(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	// protecting against re-entry using isRunning boolean.
	PrtLockMutex(context->stateMachineLock);
	if (context->isHalted || context->isRunning)
	{
		PrtUnlockMutex(context->stateMachineLock);
		return;
	}
	context->isRunning = PRT_TRUE;
	PrtUnlockMutex(context->stateMachineLock);

	// This function now just wraps the new PrtStepStateMachine method
	while (PrtStepStateMachine(context))
	{
	}

	PrtLockMutex(context->stateMachineLock);
	context->isRunning = PRT_FALSE;
	PrtUnlockMutex(context->stateMachineLock);
}

PRT_API PRT_STEP_RESULT
PrtStepProcess(PRT_PROCESS* process)
{
	PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;

	PrtLockMutex(privateProcess->processLock);
	PRT_COOPERATIVE_SCHEDULER* info = (PRT_COOPERATIVE_SCHEDULER*)privateProcess->schedulerInfo;
	info->threadsWaiting++;
	PRT_UINT32 machineCount = privateProcess->machineCount;
	PrtUnlockMutex(privateProcess->processLock);

	PRT_BOOLEAN terminating = PRT_FALSE;
	PRT_BOOLEAN hasMoreWork = PRT_FALSE;
	// Run all state machines belonging to this process.
	for (int i = machineCount - 1; i >= 0; i--)
	{
		PrtLockMutex(privateProcess->processLock);
		terminating = privateProcess->terminating;
		if (terminating)
		{
			break;
		}
		PRT_MACHINEINST_PRIV* context = (PRT_MACHINEINST_PRIV*)privateProcess->machines[i];
		PrtUnlockMutex(privateProcess->processLock);

		// todo: assign each context a persistent thread id. only allow wake-ups on the same thread.
		if (context != NULL)
		{
			// protecting against re-entry using isRunning boolean.
			PrtLockMutex(context->stateMachineLock);
			if (context->isHalted || context->isRunning)
			{
				PrtUnlockMutex(context->stateMachineLock);
			}
			else
			{
				context->isRunning = PRT_TRUE;
				PrtUnlockMutex(context->stateMachineLock);
				hasMoreWork |= PrtStepStateMachine(context);

				PrtLockMutex(context->stateMachineLock);
				context->isRunning = PRT_FALSE;
				PrtUnlockMutex(context->stateMachineLock);
			}
		}
	}

	if (!terminating)
	{
		PrtLockMutex(privateProcess->processLock);
	}

	hasMoreWork |= machineCount < privateProcess->machineCount;
	info->threadsWaiting--;
	PRT_UINT32 threadsWaiting = info->threadsWaiting;
	PrtUnlockMutex(privateProcess->processLock);

	if (terminating && threadsWaiting == 0)
	{
		PrtReleaseSemaphore(info->allThreadsStopped);
	}

	return terminating ? PRT_STEP_TERMINATING : (hasMoreWork ? PRT_STEP_MORE : PRT_STEP_IDLE);
}

PRT_UINT32
PrtFindTransition(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_UINT32 eventIndex
)
{
	PRT_UINT32 i;
	PRT_UINT32 nTransitions;

	PRT_TRANSDECL* transTable = PrtGetTransitionTable(context, context->currentState, &nTransitions);

	for (i = 0; i < nTransitions; ++i)
	{
		if (transTable[i].triggerEvent->value.valueUnion.ev == eventIndex)
		{
			break;
		}
	}
	return i;
}

void
PrtTakeTransition(
	_Inout_	        PRT_MACHINEINST_PRIV* context,
	_In_	        PRT_UINT32 event_index)
{
	PRT_UINT32 n_transitions;
	PRT_TRANSDECL* trans_table = PrtGetTransitionTable(context, context->currentState, &n_transitions);
	const PRT_UINT32 trans_index = PrtFindTransition(context, event_index);
	if (trans_table[trans_index].transFun == NULL)
	{
		PrtPushState(context, trans_table[trans_index].destStateIndex);
	}
	else
	{
		context->currentState = trans_table[trans_index].destStateIndex;
	}
}

static void
RemoveElementFromQueue(_Inout_ PRT_MACHINEINST_PRIV* context, _In_ PRT_UINT32 i)
{
	PRT_EVENTQUEUE* queue = &context->eventQueue;
	PRT_UINT32 queueLength = queue->eventsSize;
	PRT_UINT32 head = queue->headIndex;

	//
	// Collapse the event queue on the removed event
	// by moving the previous elements forward.
	//
	for (; i > 0; i--)
	{
		PRT_INT32 index = (head + i) % queueLength;
		PRT_INT32 prev = (index - 1 + queueLength) % queueLength;
		queue->events[index] = queue->events[prev];
	}

	//
	// Adjust the queue size
	//
	queue->headIndex = (queue->headIndex + 1) % queueLength;
	queue->size--;

	PRT_DBG_ASSERT(queue->size <= queueLength, "Check Failed");
}

PRT_BOOLEAN
PrtDequeueEvent(_Inout_ PRT_MACHINEINST_PRIV* context, _Out_ PRT_VALUE** trigger, _Out_ PRT_VALUE** payload)
{
	PRT_EVENTQUEUE* queue = &context->eventQueue;
	const PRT_UINT32 queue_length = queue->eventsSize;
	const PRT_UINT32 head = queue->headIndex;

	*trigger = NULL;
	*payload = NULL;

	PRT_DBG_ASSERT(queue->size <= queue_length, "Check Failed");

	const bool waiting_on_receive = context->receiveResumption != NULL;

	for (PRT_UINT32 i = 0; i < queue->size; i++)
	{
		const PRT_UINT32 index = (head + i) % queue_length;
		PRT_EVENT e = queue->events[index];
		const PRT_UINT32 trigger_event_id = PrtPrimGetEvent(e.trigger);

		// receive takes precedence over all others
		if (PrtReceiveWaitingOnEvent(context, trigger_event_id) ||
			!waiting_on_receive && !PrtIsEventDeferred(trigger_event_id, context->currentDeferredSetCompact))
		{
			*trigger = e.trigger;
			*payload = e.payload;
			RemoveElementFromQueue(context, i);
			PrtLog(PRT_STEP_DEQUEUE, &e.state, context, e.trigger, e.payload);
			return PRT_TRUE;
		}
	}

	if (PrtReceiveWaitingOnEvent(context, PRT_SPECIAL_EVENT_NULL) ||
		!waiting_on_receive && PrtStateHasDefaultTransitionOrAction(context))
	{
		*trigger = PrtMkEventValue(PRT_SPECIAL_EVENT_NULL);
		*payload = PrtMkNullValue();
		return PRT_TRUE;
	}

	return PRT_FALSE;
}

FORCEINLINE
PRT_STATEDECL*
PrtGetCurrentStateDecl(_In_ PRT_MACHINEINST_PRIV* context)
{
	return &(program->machines[context->instanceOf]->states[context->currentState]);
}

FORCEINLINE
PRT_TYPE*
PrtGetPayloadType(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_VALUE* event
)
{
	return program->events[PrtPrimGetEvent(event)]->type;
}

FORCEINLINE
PRT_UINT16
PrtGetPackSize(
	_In_	PRT_MACHINEINST_PRIV* context
)
{
	PRT_UINT32 nEvents = program->nEvents;
	PrtAssert(0 < nEvents, "Illegal number of events");
	PRT_UINT32 highestEventIndex = nEvents - 1;
	return 1 + (PRT_UINT16)(highestEventIndex / (sizeof(PRT_UINT32) * 8));
}

FORCEINLINE
PRT_SM_FUN
PrtGetEntryFunction(
	_In_	PRT_MACHINEINST_PRIV* context
)
{
	PRT_FUNDECL* entryFun = program->machines[context->instanceOf]->states[context->currentState].entryFun;
	return entryFun->implementation;
}

FORCEINLINE
PRT_SM_FUN
PrtGetExitFunction(
	_In_	PRT_MACHINEINST_PRIV* context
)
{
	PRT_FUNDECL* exitFun = program->machines[context->instanceOf]->states[context->currentState].exitFun;
	return exitFun->implementation;
}

FORCEINLINE
PRT_DODECL*
PrtGetAction(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_UINT32 currEvent
)
{
	PRT_UINT32 ui, nActions;
	PRT_DODECL* actionDecl = NULL;
	//check if action is defined for the current state
	PRT_BOOLEAN isActionInstalled =
		PrtIsActionInstalled(currEvent, PrtGetActionsPacked(context, context->currentState));
	if (isActionInstalled)
	{
		//
		// get action function
		//
		PRT_STATEDECL* stateDecl = PrtGetCurrentStateDecl(context);
		nActions = stateDecl->nDos;
		for (ui = 0; ui < nActions; ui++)
		{
			if (stateDecl->dos[ui].triggerEvent->value.valueUnion.ev == currEvent)
			{
				actionDecl = &stateDecl->dos[ui];
				return actionDecl;
			}
		}
	}

	//
	// Scan the parent states
	//
	const PRT_STATESTACK currStack = context->callStack;
	PRT_STATEDECL* stateTable = program->machines[context->instanceOf]->states;
	for (PRT_INT32 i = currStack.length - 1; i >= 0; i--)
	{
		const PRT_UINT32 topOfStackState = currStack.stateStack[i].stateIndex;
		isActionInstalled = PrtIsActionInstalled(currEvent, PrtGetActionsPacked(context, topOfStackState));
		if (isActionInstalled)
		{
			//
			// get action function
			//
			nActions = stateTable[topOfStackState].nDos;
			for (ui = 0; ui < nActions; ui++)
			{
				if (stateTable[topOfStackState].dos[ui].triggerEvent->value.valueUnion.ev == currEvent)
				{
					actionDecl = &stateTable[topOfStackState].dos[ui];
					return actionDecl;
				}
			}
		}
	}

	PRT_DBG_ASSERT(actionDecl != NULL, "Action must not be NULL");
	return actionDecl;
}

FORCEINLINE
PRT_UINT32*
PrtGetDeferredPacked(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_UINT32 stateIndex
)
{
	return program->machines[context->instanceOf]->states[stateIndex].defersSet->packedEvents;
}

FORCEINLINE
PRT_UINT32*
PrtGetActionsPacked(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_UINT32 stateIndex
)
{
	return program->machines[context->instanceOf]->states[stateIndex].doSet->packedEvents;
}

FORCEINLINE
PRT_UINT32*
PrtGetTransitionsPacked(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_UINT32 stateIndex
)
{
	return program->machines[context->instanceOf]->states[stateIndex].transSet->packedEvents;
}

FORCEINLINE
PRT_TRANSDECL*
PrtGetTransitionTable(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_UINT32 stateIndex,
	_Out_	     PRT_UINT32* nTransitions
)
{
	*nTransitions = program->machines[context->instanceOf]->states[stateIndex].nTransitions;
	return program->machines[context->instanceOf]->states[stateIndex].transitions;
}

PRT_BOOLEAN
PrtAreGuidsEqual(
	_In_	     PRT_GUID guid1,
	_In_	     PRT_GUID guid2
)
{
	return guid1.data1 == guid2.data1 && guid1.data2 == guid2.data2 && guid1.data3 == guid2.data3 && guid1.data4 ==
		guid2.data4;
}

PRT_BOOLEAN
PrtIsEventMaxInstanceExceeded(
	_In_	     PRT_EVENTQUEUE* queue,
	_In_	     PRT_UINT32 eventIndex,
	_In_	     PRT_UINT32 maxInstances
)
{
	PRT_UINT32 queueSize = queue->eventsSize;
	PRT_UINT32 head = queue->headIndex;
	PRT_UINT32 tail = queue->tailIndex;
	PRT_UINT16 currMaxInstance = 0;
	PRT_BOOLEAN isMaxInstancesExceeded = PRT_FALSE;
	//
	// head is ahead of tail
	//
	if (head > tail)
	{
		//
		// Check from head to end of Array
		//
		while (head < queueSize)
		{
			if (PrtPrimGetEvent(queue->events[head].trigger) == eventIndex)
			{
				currMaxInstance = currMaxInstance + 1;
				head++;
			}
			else
			{
				head++;
			}
		}
		//
		// Reset Head to the start of Array
		head = 0;
	}

	//
	// Check from start of Array till head
	//
	while (head < tail)
	{
		if (PrtPrimGetEvent(queue->events[head].trigger) == eventIndex)
		{
			currMaxInstance = currMaxInstance + 1;
			head++;
		}
		else
		{
			head++;
		}
	}

	if (currMaxInstance >= maxInstances)
	{
		isMaxInstancesExceeded = PRT_TRUE;
	}

	return isMaxInstancesExceeded;
}

FORCEINLINE
PRT_BOOLEAN
PrtStateHasDefaultTransitionOrAction(
	_In_	PRT_MACHINEINST_PRIV* context
)
{
	PRT_STATEDECL* stateDecl = PrtGetCurrentStateDecl(context);
	PRT_BOOLEAN hasDefaultTransition = (stateDecl->transSet->packedEvents[0] & 0x1) == 1;
	PRT_BOOLEAN hasDefaultAction = (context->currentActionSetCompact[0] & 0x1) == 1;
	return hasDefaultTransition || hasDefaultAction;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsSpecialEvent(
	_In_	PRT_VALUE* event
)
{
	return (PrtIsNullValue(event) || PrtPrimGetEvent(event) == PRT_SPECIAL_EVENT_NULL);
}

FORCEINLINE
PRT_BOOLEAN
PrtIsEventReceivable(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_UINT32 eventIndex
)
{
	PRT_UINT32* caseSet = context->packedReceiveCases;
	return (caseSet[eventIndex / (sizeof(PRT_UINT32) * 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32) * 8)))) != 0;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsEventDeferred(
	_In_	     PRT_UINT32 eventIndex,
	_In_	     PRT_UINT32* defSet
)
{
	return (defSet[eventIndex / (sizeof(PRT_UINT32) * 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32) * 8)))) != 0;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsActionInstalled(
	_In_	     PRT_UINT32 eventIndex,
	_In_	     PRT_UINT32* actionSet
)
{
	return (actionSet[eventIndex / (sizeof(PRT_UINT32) * 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32) * 8)))) != 0;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsTransitionPresent(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_UINT32 eventIndex
)
{
	PRT_UINT32* transitionsPacked = PrtGetTransitionsPacked(context, context->currentState);
	return (transitionsPacked[eventIndex / (sizeof(PRT_UINT32) * 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32) * 8))))
		!= 0;
}

PRT_BOOLEAN
PrtIsPushTransition(
	_In_	     PRT_MACHINEINST_PRIV* context,
	_In_	     PRT_UINT32 event
)
{
	PRT_UINT32 nTransitions;

	PRT_TRANSDECL* transTable = PrtGetTransitionTable(context, context->currentState, &nTransitions);
	for (PRT_UINT16 i = 0; i < nTransitions; ++i)
	{
		if (transTable[i].transFun == NULL && transTable[i].triggerEvent->value.valueUnion.ev == event)
		{
			return PRT_TRUE;
		}
	}
	return PRT_FALSE;
}

PRT_UINT32*
PrtClonePackedSet(
	_In_	     PRT_UINT32* packedSet,
	_In_	     PRT_UINT32 size
)
{
	PRT_UINT32* clone = (PRT_UINT32 *)PrtCalloc(size, sizeof(PRT_UINT32));
	for (PRT_UINT32 i = 0; i < size; i++)
	{
		clone[i] = packedSet[i];
	}
	return clone;
}

void
PrtUpdateCurrentActionsSet(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	PRT_UINT16 packSize = PrtGetPackSize(context);
	PRT_UINT32* currActionsPacked = PrtGetActionsPacked(context, context->currentState);
	PRT_UINT32* currTransitionsPacked = PrtGetTransitionsPacked(context, context->currentState);
	PRT_UINT32* currDefSetPacked = PrtGetDeferredPacked(context, context->currentState);
	//
	// A = (A -d) + a - e
	//
	for (PRT_UINT16 i = 0; i < packSize; i++)
	{
		context->currentActionSetCompact[i] = context->inheritedActionSetCompact[i] & ~currDefSetPacked[i]; // A - d
		context->currentActionSetCompact[i] |= currActionsPacked[i];
		context->currentActionSetCompact[i] &= ~currTransitionsPacked[i];
	}
}

void
PrtUpdateCurrentDeferredSet(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	PRT_UINT16 packSize = PrtGetPackSize(context);
	PRT_UINT32* currActionsPacked = PrtGetActionsPacked(context, context->currentState);
	PRT_UINT32* currTransitionsPacked = PrtGetTransitionsPacked(context, context->currentState);
	PRT_UINT32* currDefSetPacked = PrtGetDeferredPacked(context, context->currentState);

	//
	// D = (D + d) - a - e
	//
	for (PRT_UINT16 i = 0; i < packSize; i++)
	{
		context->currentDeferredSetCompact[i] = context->inheritedDeferredSetCompact[i] | currDefSetPacked[i]; // D + d
		context->currentDeferredSetCompact[i] &= ~currActionsPacked[i]; // - a
		context->currentDeferredSetCompact[i] &= ~currTransitionsPacked[i]; // -e
	}
}

void
PrtResizeEventQueue(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	PRT_UINT32 maxEventQueueSize = program->machines[context->instanceOf]->maxQueueSize;
	PRT_UINT32 currEventQueueSize = context->eventQueue.eventsSize;
	PRT_UINT32 newQueueSize = (maxEventQueueSize != 0xffffffff && currEventQueueSize * 2 > maxEventQueueSize)
		? maxEventQueueSize
		: currEventQueueSize * 2;
	PRT_EVENT* oldQueue = context->eventQueue.events;
	PRT_UINT32 oldHead = context->eventQueue.headIndex;
	PRT_UINT32 oldTail = context->eventQueue.tailIndex;
	PRT_EVENT* newQueue = (PRT_EVENT*)PrtCalloc(newQueueSize, sizeof(PRT_EVENT));
	PRT_UINT32 newHead = 0;
	PRT_UINT32 newTail = 0;

	//
	// Check from head to end of Array
	//
	while (oldHead < currEventQueueSize)
	{
		newQueue[newTail] = oldQueue[oldHead];
		newTail++;
		oldHead++;
	}
	//
	// Reset Head to the start of Array
	oldHead = 0;

	//
	// Check from start of Array till head
	//
	while (oldHead < oldTail)
	{
		newQueue[newTail] = oldQueue[oldHead];
		newTail++;
		oldHead++;
	}

	//Update the Queue
	context->eventQueue.events = newQueue;
	context->eventQueue.headIndex = newHead;
	context->eventQueue.size = newTail - newHead;
	context->eventQueue.tailIndex = newTail;
	context->eventQueue.eventsSize = newQueueSize;

	//Release the older Queue
	PrtFree(oldQueue);
}

void
PrtHaltMachine(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PrtLog(PRT_STEP_HALT, &state, context, NULL, NULL);
	PrtCleanupMachine(context);
}

void
PrtCleanupMachine(
	_Inout_	PRT_MACHINEINST_PRIV* context
)
{
	// Set the halted flag
	PrtLockMutex(context->stateMachineLock);
	if (context->isHalted)
	{
		PrtUnlockMutex(context->stateMachineLock);
		return;
	}
	context->isHalted = PRT_TRUE;
	PrtUnlockMutex(context->stateMachineLock);

	// If the machine is blocked on a receive, let it clean up
	if (context->receiveResumption != NULL)
	{
		// cleanup is based on context->isHalted. The parameters here shouldn't matter,
		// but are chosen to be likely to cause errors if there is a code generation issue.
		const PRT_RETURN_KIND op = PrtResume(context, -1, NULL);
		PrtAssert(op == ReturnStatement, "cleanup of blocked machine failed.");
	}

	// Free machine's state
	PrtLockMutex(context->stateMachineLock);
	if (context->eventQueue.events != NULL)
	{
		PRT_EVENT* queue = context->eventQueue.events;
		PRT_UINT32 head = context->eventQueue.headIndex;
		PRT_UINT32 count = 0;

		while (count < context->eventQueue.size && head < context->eventQueue.eventsSize)
		{
			PrtFreeValue(queue[head].payload);
			PrtFreeValue(queue[head].trigger);
			head++;
			count++;
		}

		head = 0;
		while (count < context->eventQueue.size)
		{
			PrtFreeValue(queue[head].payload);
			PrtFreeValue(queue[head].trigger);
			head++;
			count++;
		}
	}

	PrtFree(context->eventQueue.events);

	for (PRT_INT32 i = 0; i < context->callStack.length; i++)
	{
		PRT_STATESTACK_INFO* info = &context->callStack.stateStack[i];
		PrtFree(info->inheritedActionSetCompact);
		PrtFree(info->inheritedDeferredSetCompact);
	}

	PrtFree(context->currentActionSetCompact);
	PrtFree(context->currentDeferredSetCompact);
	PrtFree(context->inheritedActionSetCompact);
	PrtFree(context->inheritedDeferredSetCompact);

	if (context->varValues != NULL)
	{
		PRT_MACHINEDECL* mdecl = program->machines[context->instanceOf];

		for (PRT_UINT32 i = 0; i < mdecl->nVars; i++)
		{
			PrtFreeValue(context->varValues[i]);
		}
	}

	PrtFree(context->varValues);

	PrtFreeTriggerPayload(context);
	PrtFreeValue(context->id);
	PrtFreeValue(context->recvMap);

	PrtUnlockMutex(context->stateMachineLock);
}

void
PrtHandleError(
	_In_	     PRT_STATUS ex,
	_In_	     PRT_MACHINEINST_PRIV* context
)
{
	((PRT_PROCESS_PRIV *)context->process)->errorHandler(ex, (PRT_MACHINEINST *)context);
}

void PRT_CALL_CONV
PrtAssertDefaultFn(
	_In_	     PRT_INT32 condition,
	_In_opt_z_	     PRT_CSTRING message
)
{
	if (condition != 0)
	{
		return;
	}

	PrtPrintf("ASSERT");
	if (message != NULL)
	{
		PrtPrintf(": ");
		PrtPrintf(message);
	}
	abort();
}

void PRT_CALL_CONV
PrtPrintfDefaultFn(_In_opt_z_ PRT_CSTRING message)
{
	// do not allow % signs in message to be interpreted as arguments.
	printf_s("%s", message);
}

PRT_API void PRT_CALL_CONV
PrtUpdateAssertFn(
	PRT_ASSERT_FUN assertFn
)
{
	_PrtAssert = assertFn;
}

PRT_API void PRT_CALL_CONV
PrtUpdatePrintFn(
	PRT_PRINT_FUN printFn
)
{
	PrtPrintf = printFn;
}

void
PrtLog(
	_In_	     PRT_STEP step,
	_In_	     PRT_MACHINESTATE* senderState,
	_In_	     PRT_MACHINEINST_PRIV* receiver,
	_In_	     PRT_VALUE* eventId,
	_In_	     PRT_VALUE* payload
)
{
	((PRT_PROCESS_PRIV *)receiver->process)->logHandler(step, senderState, (PRT_MACHINEINST *)receiver, eventId,
		payload);
}

void
PrtCheckIsLocalMachineId(
	_In_	     PRT_MACHINEINST* context,
	_In_	     PRT_VALUE* id
)
{
	if (context->process->guid.data1 == id->valueUnion.mid->processId.data1 &&
		context->process->guid.data2 == id->valueUnion.mid->processId.data2)
		return;
	PrtHandleError(PRT_STATUS_ILLEGAL_SEND, (PRT_MACHINEINST_PRIV *)context);
}

/*********************************************************************************

Public Functions

*********************************************************************************/

void PrtTraverseEventset(PRT_EVENTSETDECL* evset, PRT_BOOLEAN doInstall)
{
	if (doInstall)
	{
		if (evset->packedEvents == NULL)
		{
			PRT_UINT32 unitSize = sizeof(PRT_UINT32) * 8;
			PRT_UINT32 packedArraySize = program->nEvents / unitSize + 1;
			evset->packedEvents = (PRT_UINT32 *)PrtCalloc(packedArraySize, sizeof(PRT_UINT32));
			for (PRT_UINT32 i = 0; i < evset->nEvents; i++)
			{
				PRT_UINT32 eventIndex = evset->events[i]->value.valueUnion.ev;
				PRT_UINT32 arrayOffset = eventIndex / unitSize;
				PRT_UINT32 eventMask = 1 << (eventIndex % unitSize);
				evset->packedEvents[arrayOffset] |= eventMask;
			}
		}
	}
	else
	{
		if (evset->packedEvents != NULL)
		{
			PrtFree(evset->packedEvents);
			evset->packedEvents = NULL;
		}
	}
}

void PrtTraverseState(PRT_STATEDECL* state, PRT_BOOLEAN doInstall)
{
	PrtTraverseEventset(state->defersSet, doInstall);
	PrtTraverseEventset(state->doSet, doInstall);
	PrtTraverseEventset(state->transSet, doInstall);
}

void PrtTraverseMachine(PRT_MACHINEDECL* machine, PRT_BOOLEAN doInstall)
{
	for (PRT_UINT32 i = 0; i < machine->nStates; i++)
	{
		PrtTraverseState(&machine->states[i], doInstall);
	}
}

void PrtInstallProgram(_In_ PRT_PROGRAMDECL* p)
{
	PrtAssert(p != NULL && program == NULL, "p and program must be non-NULL");

	lh_register_malloc((lh_mallocfun*)PrtMalloc, (lh_callocfun*)PrtCalloc, (lh_reallocfun*)PrtRealloc, (lh_freefun*)PrtFree);

	program = p;
	for (PRT_UINT32 i = 0; i < p->nEvents; i++)
	{
		p->events[i]->value.valueUnion.ev = i;
	}
	for (PRT_UINT32 i = 0; i < p->nMachines; i++)
	{
		p->machines[i]->declIndex = i;
		PrtTraverseMachine(p->machines[i], PRT_TRUE);
	}
	for (PRT_UINT32 i = 0; i < p->nForeignTypes; i++)
	{
		p->foreignTypes[i]->declIndex = i;
	}
}

void PrtUninstallProgram()
{
	PrtAssert(program != NULL, "program must be non-NULL");
	for (PRT_UINT32 i = 0; i < program->nEvents; i++)
	{
		program->events[i]->value.valueUnion.ev = 0;
	}
	for (PRT_UINT32 i = 0; i < program->nMachines; i++)
	{
		program->machines[i]->declIndex = 0;
		PrtTraverseMachine(program->machines[i], PRT_FALSE);
	}
	for (PRT_UINT32 i = 0; i < program->nForeignTypes; i++)
	{
		program->foreignTypes[i]->declIndex = 0;
	}
	program = NULL;
}

PRT_PROCESS*
PrtStartProcess(
	_In_	     PRT_GUID guid,
	_In_	     PRT_PROGRAMDECL* p,
	_In_	     PRT_ERROR_FUN errorFun,
	_In_	     PRT_LOG_FUN logFun
)
{
	PrtInstallProgram(p);

	PRT_PROCESS_PRIV* process = (PRT_PROCESS_PRIV *)PrtMalloc(sizeof(PRT_PROCESS_PRIV));
	process->guid = guid;
	process->errorHandler = errorFun;
	process->logHandler = logFun;
	process->processLock = PrtCreateMutex();
	process->machineCount = 0;
	process->machines = NULL;
	process->numMachines = 0;
	process->schedulingPolicy = PRT_SCHEDULINGPOLICY_TASKNEUTRAL;
	process->schedulerInfo = NULL;
	process->terminating = PRT_FALSE;
	return (PRT_PROCESS *)process;
}

PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtLookupMachineByName(_In_ PRT_STRING name, _Out_ PRT_UINT32* id)
{
	*id = 0;
	for (PRT_UINT32 i = 0; i < program->nMachines; i++)
	{
		if (strcmp(name, program->machines[i]->name) == 0)
		{
			*id = i;
			return PRT_TRUE;
		}
	}
	return PRT_FALSE;
}

PRT_API PRT_BOOLEAN
PrtWaitForWork(PRT_PROCESS* process)
{
	PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
	PrtLockMutex(privateProcess->processLock);

	PrtAssert(privateProcess->schedulingPolicy == PRT_SCHEDULINGPOLICY_COOPERATIVE,
		"PrtWaitForWork can only be called when PrtSetSchedulingPolicy has set PRT_SCHEDULINGPOLICY_COOPERATIVE mode");
	PRT_COOPERATIVE_SCHEDULER* info = (PRT_COOPERATIVE_SCHEDULER*)privateProcess->schedulerInfo;

	info->threadsWaiting++;

	PrtUnlockMutex(privateProcess->processLock);

	PrtWaitSemaphore(info->workAvailable, -1);

	PrtLockMutex(privateProcess->processLock);
	info->threadsWaiting--;
	PRT_BOOLEAN terminating = privateProcess->terminating;
	PRT_UINT32 threadsWaiting = info->threadsWaiting;
	PrtUnlockMutex(privateProcess->processLock);

	if (terminating && threadsWaiting == 0)
	{
		PrtReleaseSemaphore(info->allThreadsStopped);
	}
	return terminating;
}

static void PrtDestroyCooperativeScheduler(PRT_COOPERATIVE_SCHEDULER* info)
{
	if (info != NULL)
	{
		PrtDestroySemaphore(info->workAvailable);
		PrtDestroySemaphore(info->allThreadsStopped);
		PrtFree(info);
	}
}

PRT_API void
PrtSetSchedulingPolicy(PRT_PROCESS* process, PRT_SCHEDULINGPOLICY policy)
{
	PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
	if (privateProcess->schedulingPolicy != policy)
	{
		privateProcess->schedulingPolicy = policy;
		if (policy == PRT_SCHEDULINGPOLICY_COOPERATIVE)
		{
			PRT_COOPERATIVE_SCHEDULER* info = (PRT_COOPERATIVE_SCHEDULER*)PrtMalloc(sizeof(PRT_COOPERATIVE_SCHEDULER));
			PrtAssert(info != NULL, "Out of memory");

			info->workAvailable = PrtCreateSemaphore(0, 32767);
			info->threadsWaiting = 0;
			info->allThreadsStopped = PrtCreateSemaphore(0, 32767);

			privateProcess->schedulerInfo = info;
		}
		else if (policy == PRT_SCHEDULINGPOLICY_TASKNEUTRAL)
		{
			// this is where we could implement other policies...
			PrtDestroyCooperativeScheduler(privateProcess->schedulerInfo);
			privateProcess->schedulerInfo = NULL;
		}
		else
		{
			PrtAssert(PRT_FALSE,
				"PrtSetSchedulingPolicy must set either PRT_SCHEDULINGPOLICY_TASKNEUTRAL or PRT_SCHEDULINGPOLICY_COOPERATIVE"
			);
		}
	}
}

PRT_API void
PrtRunProcess(PRT_PROCESS* process)
{
	while (1)
	{
		PRT_STEP_RESULT result = PrtStepProcess(process);
		switch (result)
		{
		case PRT_STEP_TERMINATING:
			return;
		case PRT_STEP_IDLE:
			if (PrtWaitForWork(process) == PRT_TRUE)
			{
				return;
			}
			break;
		case PRT_STEP_MORE:
			PrtYieldThread();
			break;
		}
	}
}

void
PrtStopProcess(
	_Inout_	PRT_PROCESS* process
)
{
	PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV *)process;

	PrtLockMutex(privateProcess->processLock);
	privateProcess->terminating = PRT_TRUE;
	PRT_BOOLEAN waitForThreads = PRT_FALSE;
	PRT_COOPERATIVE_SCHEDULER* info = NULL;

	if (privateProcess->schedulingPolicy == PRT_SCHEDULINGPOLICY_COOPERATIVE)
	{
		info = (PRT_COOPERATIVE_SCHEDULER*)privateProcess->schedulerInfo;
		int count = info->threadsWaiting;
		if (count > 0)
		{
			waitForThreads = PRT_TRUE;
			// unblock all threads so the PrtRunProcess call terminates.
			for (int i = 0; i < count; i++)
			{
				PrtReleaseSemaphore(info->workAvailable);
			}
		}
	}
	PrtUnlockMutex(privateProcess->processLock);

	if (waitForThreads)
	{
		PrtWaitSemaphore(info->allThreadsStopped, -1);
	}

	// ok, now we can safely start deleting things...
	for (PRT_UINT32 i = 0; i < privateProcess->numMachines; i++)
	{
		PRT_MACHINEINST* context = privateProcess->machines[i];
		PRT_MACHINEINST_PRIV* privContext = (PRT_MACHINEINST_PRIV *)context;

		PrtCleanupMachine(privContext);
		if (privContext->stateMachineLock != NULL)
		{
			PrtDestroyMutex(privContext->stateMachineLock);
		}
		PrtFree(context);
	}

	PrtFree(privateProcess->machines);
	PrtDestroyCooperativeScheduler(info);
	PrtDestroyMutex(privateProcess->processLock);
	PrtUninstallProgram();
	PrtFree(process);
}

PRT_BOOLEAN PrtInterfaceInCreatesSet(PRT_UINT32 interfaceCreated, PRT_INTERFACESETDECL* creates)
{
	for (PRT_UINT32 i = 0; i < creates->nInterfaces; i++)
	{
		if (interfaceCreated == creates->interfacesIndex[i])
		{
			return PRT_TRUE;
		}
	}

	return PRT_FALSE;
}

PRT_MACHINEINST*
PrtMkInterface(
	_In_	     PRT_MACHINEINST* creator,
	_In_	     PRT_UINT32 IName,
	_In_	     PRT_UINT32 numArgs,
	...
)
{
	PRT_MACHINEINST_PRIV* context = (PRT_MACHINEINST_PRIV*)creator;
	PRT_VALUE* payload;
	const PRT_UINT32 interfaceCreated = program->linkMap[context->interfaceBound][IName];
	const PRT_UINT32 instance_of = program->interfaceDefMap[interfaceCreated];

	// Check the CreateOk condition
	PrtAssert(PrtInterfaceInCreatesSet(interfaceCreated, program->machines[creator->instanceOf]->creates),
		"Created interface is not in the creates set of the machine");

	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE** args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			PRT_VALUE** argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];

		if (numArgs > 1)
		{
			PRT_MACHINEDECL* machineDecl = program->machines[instance_of];
			PRT_FUNDECL* entryFun = machineDecl->states[machineDecl->initStateIndex].entryFun;
			PRT_TYPE* payloadType = entryFun->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PRT_MACHINEINST* result = (PRT_MACHINEINST*)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)context->process,
		interfaceCreated, instance_of, payload);
	// must now free this payload because PrtMkMachinePrivate clones it.
	PrtFreeValue(payload);
	return result;
}

PRT_MACHINEINST*
PrtMkMachine(
	_Inout_	         PRT_PROCESS* process,
	_In_	         PRT_UINT32 interfaceName,
	_In_	         PRT_UINT32 numArgs,
	...
)
{
	PRT_VALUE* payload;
	PRT_UINT32 instanceOf = program->interfaceDefMap[interfaceName];

	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE** args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			PRT_VALUE** argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];

		if (numArgs > 1)
		{
			PRT_MACHINEDECL* machineDecl = program->machines[instanceOf];
			PRT_FUNDECL* entryFun = machineDecl->states[machineDecl->initStateIndex].entryFun;
			PRT_TYPE* payloadType = entryFun->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PRT_MACHINEINST* result = (PRT_MACHINEINST*)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)process, interfaceName,
		instanceOf, payload);
	// free the payload since we cloned it here, and PrtMkMachinePrivate also clones it.
	PrtFreeValue(payload);
	return result;
}

PRT_MACHINEINST*
PrtGetMachine(
	_In_	     PRT_PROCESS* process,
	_In_	     PRT_VALUE* id
)
{
	PrtAssert(id->discriminator == PRT_VALUE_KIND_MID, "id is not legal PRT_MACHINEID");
	PRT_MACHINEID* machineId = id->valueUnion.mid;
	//Comented out by Ankush Desai.
	//PrtAssert(PrtAreGuidsEqual(process->guid, machineId->processId), "id does not belong to process");
	PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV *)process;
	PrtAssert((0 < machineId->machineId) && (machineId->machineId <= privateProcess->numMachines), "id out of bounds");
	return privateProcess->machines[machineId->machineId - 1];
}

void PRT_CALL_CONV PrtGetMachineState(_In_ PRT_MACHINEINST* context, _Inout_ PRT_MACHINESTATE* state)
{
	PRT_MACHINEINST_PRIV* priv = (PRT_MACHINEINST_PRIV*)context;
	state->machineId = context->id->valueUnion.mid->machineId;
	state->machineName = program->machines[context->instanceOf]->name;
	state->stateId = priv->currentState;
	state->stateName = PrtGetCurrentStateDecl(priv)->name;
}

void
PrtSend(
	_Inout_	        PRT_MACHINESTATE* senderState,
	_Inout_	        PRT_MACHINEINST* receiver,
	_In_	        PRT_VALUE* event,
	_In_	        PRT_UINT32 numArgs,
	...
)
{
	PRT_VALUE* payload;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE** args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			//TODO: Confirm if the code below is correct.
			PRT_VALUE** argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_TYPE* payloadType = PrtGetPayloadType((PRT_MACHINEINST_PRIV *)receiver, event);
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PrtSendPrivate(senderState, (PRT_MACHINEINST_PRIV *)receiver, event, payload);
}

void
PRT_CALL_CONV PrtSendInternal(
	_Inout_	        PRT_MACHINEINST* sender,
	_Inout_	        PRT_MACHINEINST* receiver,
	_In_	        PRT_VALUE* event,
	_In_	        PRT_UINT32 numArgs,
	...
)
{
	PRT_MACHINESTATE senderState;
	PrtGetMachineState(sender, &senderState);

	PRT_VALUE* payload;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE** args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			//TODO: Confirm if the code below is correct.
			PRT_VALUE** argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_TYPE* payloadType = PrtGetPayloadType((PRT_MACHINEINST_PRIV *)receiver, event);
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}

	PrtSendPrivate(&senderState, (PRT_MACHINEINST_PRIV *)receiver, event, payload);
}

static void ResizeBuffer(_Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize, _Inout_ PRT_UINT32 numCharsWritten,
	PRT_UINT32 resizeNum)
{
	PRT_UINT32 padding = 100;
	if (*buffer == NULL)
	{
		*bufferSize = resizeNum + 1 + padding;
		*buffer = (char *)PrtCalloc(*bufferSize, sizeof(char));
	}
	else if (*bufferSize < numCharsWritten + resizeNum + 1)
	{
		PRT_UINT32 newBufferSize = numCharsWritten + resizeNum + 1 + padding;
		char* newBuffer = (char *)PrtCalloc(newBufferSize, sizeof(char));
		strcpy_s(newBuffer, newBufferSize, *buffer);
		PrtFree(*buffer);
		*buffer = newBuffer;
		*bufferSize = newBufferSize;
	}
}

static void PrtUserPrintUint16(_In_ PRT_UINT16 i, _Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize,
	_Inout_                                    PRT_UINT32* numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 16);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%u", i);
}

static void PrtUserPrintUint32(_In_ PRT_UINT32 i, _Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize,
	_Inout_                                    PRT_UINT32* numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 32);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%u", i);
}

static void PrtUserPrintUint64(_In_ PRT_UINT64 i, _Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize,
	_Inout_                                    PRT_UINT32* numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 64);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%llu", (unsigned long long)i);
}

static void PrtUserPrintFloat(_In_ PRT_FLOAT i, _Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize,
	_Inout_                                   PRT_UINT32* numCharsWritten)
{
	const int flt_size = sizeof(PRT_FLOAT);
	const PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 8 * flt_size);
	const char* fmt = flt_size == 4 ? "%f" : "%lf";
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, fmt, i);
}

static void PrtUserPrintInt(_In_ PRT_INT i, _Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize,
	_Inout_                                 PRT_UINT32* numCharsWritten)
{
	if (sizeof(PRT_INT) == 4)
	{
		PrtUserPrintUint32((PRT_UINT32)i, buffer, bufferSize, numCharsWritten);
	}
	PrtUserPrintUint64(i, buffer, bufferSize, numCharsWritten);
}

static void PrtUserPrintString(_In_ PRT_STRING s, _Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize,
	_Inout_                                    PRT_UINT32* numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, (PRT_UINT32)strlen(s) + 1);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%s", s);
}

static void PrtUserPrintMachineId(_In_ PRT_MACHINEID id, _Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize,
	_Inout_                                       PRT_UINT32* numCharsWritten)
{
	PrtUserPrintString("< (", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint32(id.processId.data1, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint16(id.processId.data2, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint16(id.processId.data3, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint64(id.processId.data4, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString("), ", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint32(id.machineId, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString(">", buffer, bufferSize, numCharsWritten);
}

static void PrtUserPrintType(_In_ PRT_TYPE* type, _Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize,
	_Inout_                                  PRT_UINT32* numCharsWritten)
{
	PRT_TYPE_KIND kind = type->typeKind;
	switch (kind)
	{
	case PRT_KIND_NULL:
		PrtUserPrintString("null", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_ANY:
		PrtUserPrintString("any", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_BOOL:
		PrtUserPrintString("bool", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_EVENT:
		PrtUserPrintString("event", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_MACHINE:
		PrtUserPrintString("machine", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_INT:
		PrtUserPrintString("int", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_FLOAT:
		PrtUserPrintString("float", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_FOREIGN:
		PrtUserPrintString("foreign", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE* mtype = type->typeUnion.map;
		PrtUserPrintString("map[", buffer, bufferSize, numCharsWritten);
		PrtUserPrintType(mtype->domType, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintType(mtype->codType, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_NMDTUPTYPE* ntype = type->typeUnion.nmTuple;
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		for (PRT_UINT32 i = 0; i < ntype->arity; ++i)
		{
			PrtUserPrintString(ntype->fieldNames[i], buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(": ", buffer, bufferSize, numCharsWritten);
			PrtUserPrintType(ntype->fieldTypes[i], buffer, bufferSize, numCharsWritten);
			if (i < ntype->arity - 1)
			{
				PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
			}
			else
			{
				PrtUserPrintString(")", buffer, bufferSize, numCharsWritten);
			}
		}
		break;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE* stype = type->typeUnion.seq;
		PrtUserPrintString("seq[", buffer, bufferSize, numCharsWritten);
		PrtUserPrintType(stype->innerType, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_TUPTYPE* ttype = type->typeUnion.tuple;
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		if (ttype->arity == 1)
		{
			PrtUserPrintType(ttype->fieldTypes[0], buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(",)", buffer, bufferSize, numCharsWritten);
		}
		else
		{
			for (PRT_UINT32 i = 0; i < ttype->arity; ++i)
			{
				PrtUserPrintType(ttype->fieldTypes[i], buffer, bufferSize, numCharsWritten);
				if (i < ttype->arity - 1)
				{
					PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
				}
				else
				{
					PrtUserPrintString(")", buffer, bufferSize, numCharsWritten);
				}
			}
		}
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtUserPrintType: Invalid type");
		break;
	}
}

static void PrtUserPrintValue(_In_ PRT_VALUE* value, _Inout_ char** buffer, _Inout_ PRT_UINT32* bufferSize,
	_Inout_                                   PRT_UINT32* numCharsWritten)
{
	PRT_STRING frgnStr;
	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALUE_KIND_NULL:
		PrtUserPrintString("null", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALUE_KIND_BOOL:
		PrtUserPrintString(PrtPrimGetBool(value) == PRT_TRUE ? "true" : "false", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALUE_KIND_INT:
		PrtUserPrintInt(PrtPrimGetInt(value), buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALUE_KIND_FLOAT:
		PrtUserPrintFloat(PrtPrimGetFloat(value), buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALUE_KIND_STRING:
		PrtUserPrintString(PrtPrimGetString(value), buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALUE_KIND_EVENT:
		PrtUserPrintString("<", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(PrtPrimGetEvent(value), buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(">", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALUE_KIND_MID:
		PrtUserPrintMachineId(PrtPrimGetMachine(value), buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALUE_KIND_FOREIGN:
		frgnStr = program->foreignTypes[value->valueUnion.frgn->typeTag]->toStringFun(value->valueUnion.frgn->value);
		PrtUserPrintString(frgnStr, buffer, bufferSize, numCharsWritten);
		PrtFree(frgnStr);
		break;
	case PRT_VALUE_KIND_MAP:
	{
		PRT_MAPVALUE* mval = value->valueUnion.map;
		PRT_MAPNODE* next = mval->first;
		PrtUserPrintString("{", buffer, bufferSize, numCharsWritten);
		while (next != NULL)
		{
			PrtUserPrintValue(next->key, buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(" --> ", buffer, bufferSize, numCharsWritten);
			PrtUserPrintValue(next->value, buffer, bufferSize, numCharsWritten);
			if (next->bucketNext != NULL)
			{
				PrtUserPrintString("*", buffer, bufferSize, numCharsWritten);
			}

			if (next->insertNext != NULL)
			{
				PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
			}

			next = next->insertNext;
		}

		PrtUserPrintString("} (", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(mval->size, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" / ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(PrtMapCapacity(value), buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(")", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_VALUE_KIND_SEQ:
	{
		PRT_SEQVALUE* sVal = value->valueUnion.seq;
		PrtUserPrintString("[", buffer, bufferSize, numCharsWritten);
		for (PRT_UINT32 i = 0; i < sVal->size; ++i)
		{
			PrtUserPrintValue(sVal->values[i], buffer, bufferSize, numCharsWritten);
			if (i < sVal->size - 1)
			{
				PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
			}
		}

		PrtUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_VALUE_KIND_TUPLE:
	{
		PRT_TUPVALUE* tval = value->valueUnion.tuple;
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		if (tval->size == 1)
		{
			PrtUserPrintValue(tval->values[0], buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(",)", buffer, bufferSize, numCharsWritten);
		}
		else
		{
			for (PRT_UINT32 i = 0; i < tval->size; ++i)
			{
				PrtUserPrintValue(tval->values[i], buffer, bufferSize, numCharsWritten);
				if (i < tval->size - 1)
				{
					PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
				}
				else
				{
					PrtUserPrintString(")", buffer, bufferSize, numCharsWritten);
				}
			}
		}
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtUserPrintValue: Invalid value");
		break;
	}
}

static void PrtUserPrintStep(_In_ PRT_STEP step, PRT_MACHINESTATE* senderState, _In_ PRT_MACHINEINST* receiver,
	_In_                                  PRT_VALUE* event, _In_ PRT_VALUE* payload,
	_Inout_                                  char** buffer,
	_Inout_                                  PRT_UINT32* bufferSize,
	_Inout_                                  PRT_UINT32* numCharsWritten)
{
	PRT_MACHINEINST_PRIV* c = (PRT_MACHINEINST_PRIV *)receiver;
	PRT_STRING machineName = program->machines[c->instanceOf]->name;
	PRT_UINT32 machineId = c->id->valueUnion.mid->machineId;
	PRT_STRING stateName = PrtGetCurrentStateDecl(c)->name;
	PRT_STRING eventName;

	switch (step)
	{
	case PRT_STEP_HALT:
		PrtUserPrintString("<HaltLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") halted in state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_ENQUEUE:
		eventName = program->events[PrtPrimGetEvent(event)]->name;
		PrtUserPrintString("<EnqueueLog> Enqueued event ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintValue(payload, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" on Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(")\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_DEQUEUE:
		eventName = program->events[PrtPrimGetEvent(event)]->name;
		PrtUserPrintString("<DequeueLog> Dequeued event ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintValue(payload, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" by Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(")\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_ENTRY:
		PrtUserPrintString("<StateLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") entered state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_CREATE:
		PrtUserPrintString("<CreateLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") is created\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_GOTO:
	{
		PRT_MACHINEINST_PRIV* context = (PRT_MACHINEINST_PRIV *)receiver;
		PRT_STRING destStateName = program->machines[context->instanceOf]->states[context->destStateIndex].name;
		PrtUserPrintString("<GotoLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") goes to state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(destStateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintValue(payload, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_STEP_RAISE:
		eventName = program->events[PrtPrimGetEvent(event)]->name;
		PrtUserPrintString("<RaiseLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") raised event ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintValue(payload, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_POP:
		PrtUserPrintString("<PopLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") popped and reentered state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_PUSH:
		PrtUserPrintString("<PushLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") pushed\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_UNHANDLED:
		eventName = program->events[PrtPrimGetEvent(c->currentTrigger)]->name;
		PrtUserPrintString("<PopLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") popped with unhandled event ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" and reentered state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_DO:
		PrtUserPrintString("<ActionLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") executed action in state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_EXIT:
		PrtUserPrintString("<ExitLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") exiting state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_IGNORE:
		eventName = program->events[PrtPrimGetEvent(event)]->name;
		PrtUserPrintString("<ActionLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") ignored event ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" in state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	default:
		PrtAssert(PRT_FALSE, "Illegal PRT_STEP value");
		break;
	}
}

void PRT_CALL_CONV PrtPrintValue(_In_ PRT_VALUE* value)
{
	char* buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintValue(value, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringValue(_In_ PRT_VALUE* value)
{
	char* buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintValue(value, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}

PRT_STRING PRT_CALL_CONV PrtCopyString(_In_ const PRT_STRING value)
{
	if (value == NULL)
	{
		return NULL;
	}
	const size_t bufferSize = strlen(value) + 1;
	PRT_STRING buffer = (PRT_STRING)PrtCalloc(bufferSize, sizeof(char));
	strcpy_s(buffer, bufferSize, value);
	return buffer;
}

void PRT_CALL_CONV PrtPrintType(_In_ PRT_TYPE* type)
{
	char* buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintType(type, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringType(_In_ PRT_TYPE* type)
{
	char* buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintType(type, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}

void PRT_CALL_CONV PrtPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINESTATE* senderState, _In_ PRT_MACHINEINST* receiver,
	_In_                                     PRT_VALUE* event, _In_ PRT_VALUE* payload)
{
	char* buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintStep(step, senderState, receiver, event, payload, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringStep(_In_ PRT_STEP step, _In_ PRT_MACHINESTATE* senderState,
	_In_                                              PRT_MACHINEINST*
	receiver, _In_ PRT_VALUE* event, _In_ PRT_VALUE* payload)
{
	char* buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintStep(step, senderState, receiver, event, payload, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}

void PRT_CALL_CONV PrtFormatPrintf(_In_ PRT_CSTRING msg, ...)
{
	PrtPrintf(msg);
	va_list argp;
	va_start(argp, msg);
	PRT_UINT32 numArgs = va_arg(argp, PRT_UINT32);
	PRT_VALUE** args = (PRT_VALUE **)PrtCalloc(numArgs, sizeof(PRT_VALUE *));
	for (PRT_UINT32 i = 0; i < numArgs; i++)
	{
		args[i] = va_arg(argp, PRT_VALUE *);
	}
	PRT_UINT32 numSegs = va_arg(argp, PRT_UINT32);
	for (PRT_UINT32 i = 0; i < numSegs; i++)
	{
		PRT_UINT32 argIndex = va_arg(argp, PRT_UINT32);
		PrtPrintValue(args[argIndex]);
		PRT_CSTRING seg = va_arg(argp, PRT_CSTRING);
		PrtPrintf(seg);
	}
	va_end(argp);
	PrtFree(args);
}

PRT_STRING PRT_CALL_CONV PrtFormatString(_In_ PRT_CSTRING baseString, ...)
{
	PRT_STRING ret = PrtMalloc(sizeof(PRT_CHAR) * (strlen(baseString) + 1));
	strcpy(ret, baseString);
	va_list argp;
	va_start(argp, baseString);
	PRT_UINT32 numArgs = va_arg(argp, PRT_UINT32);
	PRT_VALUE** args = (PRT_VALUE **)PrtCalloc(numArgs, sizeof(PRT_VALUE *));
	for (PRT_UINT32 i = 0; i < numArgs; i++)
	{
		args[i] = va_arg(argp, PRT_VALUE *);
	}
	PRT_UINT32 numSegs = va_arg(argp, PRT_UINT32);
	for (PRT_UINT32 i = 0; i < numSegs; i++)
	{
		PRT_UINT32 argIndex = va_arg(argp, PRT_UINT32);
		PRT_STRING arg = PrtToStringValue(args[argIndex]);
		PRT_CSTRING seg = va_arg(argp, PRT_CSTRING);
		ret = PrtRealloc(ret, sizeof(PRT_CHAR) * (strlen(ret) + 1 + strlen(arg) + strlen(seg)));
		strcat(ret, arg);
		strcat(ret, seg);
		PrtFree(arg);
	}
	va_end(argp);
	PrtFree(args);
	return ret;
}