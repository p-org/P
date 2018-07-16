#include "PrtExecution.h"

// Can only run one P program at a time
PRT_PROGRAMDECL *program;

PRT_TYPE NullType =
{
	PRT_KIND_NULL,
	(struct PRT_MAPTYPE *)NULL
};

PRT_TYPE AnyType = 
{
	PRT_KIND_ANY,
	(struct PRT_MAPTYPE *)NULL
};

PRT_EVENTDECL _P_EVENT_NULL_STRUCT =
{
    {
        PRT_VALUE_KIND_NULL,
        PRT_SPECIAL_EVENT_NULL
    },
	"null",
	0,
	&NullType
};

PRT_EVENTDECL _P_EVENT_HALT_STRUCT =
{
    {
        PRT_VALUE_KIND_EVENT,
        PRT_SPECIAL_EVENT_HALT
    },
	"halt",
	4294967295U,
	&AnyType
};

PRT_VALUE* PRT_CALL_CONV _P_NO_OP_IMPL(_Inout_ struct PRT_MACHINEINST *context, _Inout_ PRT_VALUE*** refLocals) {
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



void PRT_CALL_CONV PrtSetGlobalVarEx(_Inout_ PRT_MACHINEINST_PRIV *context, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(value), "value is not valid");
	PRT_VALUE *oldValue = context->varValues[varIndex];
	context->varValues[varIndex] = cloneValue ? PrtCloneValue(value) : value;
	if (oldValue != NULL)
	{
		PrtAssert(PrtIsValidValue(oldValue), "Variable must contain a valid value");
		PrtFreeValue(oldValue);
	}
}

void PRT_CALL_CONV PrtSetGlobalVar(_Inout_ PRT_MACHINEINST_PRIV *context, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE *value)
{
	PrtSetGlobalVarEx(context, varIndex, value, PRT_TRUE);
}


void PRT_CALL_CONV PrtSetLocalVarEx(_Inout_ PRT_VALUE **locals, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(value), "value is not valid");
	PRT_VALUE *oldValue = locals[varIndex];
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
static void PrtScheduleWork(PRT_MACHINEINST_PRIV *context)
{
    PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV*)context->process;
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


PRT_MACHINEINST_PRIV *
PrtMkMachinePrivate(
_Inout_  PRT_PROCESS_PRIV		*process,
_In_  PRT_UINT32				interfaceName,
_In_  PRT_UINT32				instanceOf,
_In_  PRT_VALUE					*payload
)
{
	PrtLockMutex(process->processLock);

	PRT_UINT32 packSize;
	PRT_UINT8 eQSize;
	PRT_MACHINEINST_PRIV *context;
	PRT_UINT32 i;

	eQSize = PRT_QUEUE_LEN_DEFAULT;

	// Make space in process list for new machine
	PRT_UINT32 numMachines = process->numMachines;
	PRT_UINT32 machineCount = process->machineCount;
	PRT_MACHINEINST **machines = process->machines;
	if (machineCount == 0)
	{
		machines = (PRT_MACHINEINST **)PrtCalloc(1, sizeof(PRT_MACHINEINST *));
		process->machines = machines;
		process->machineCount = 1;
	}
	else if (machineCount == numMachines) {
		PRT_MACHINEINST **newMachines = (PRT_MACHINEINST **)PrtCalloc(2 * machineCount, sizeof(PRT_MACHINEINST *));
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
	context = (PRT_MACHINEINST_PRIV*)PrtMalloc(sizeof(PRT_MACHINEINST_PRIV));

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
	PRT_TYPE *domType = PrtMkPrimitiveType(PRT_KIND_MACHINE);
	PRT_TYPE *codType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE *recvMapType = PrtMkMapType(domType, codType);
	context->recvMap = PrtMkDefaultValue(recvMapType);
	PrtFreeType(domType);
	PrtFreeType(codType);
	PrtFreeType(recvMapType);

	// Initialize Machine Internal Variables
	//
	context->isRunning = PRT_FALSE;
	context->isHalted = PRT_FALSE; 
    context->nextOperation = EntryOperation;
	context->lastOperation = ReturnStatement;
	context->exitReason = NotExit;
	context->eventValue = 0;

	context->destStateIndex = 0;
	context->currentTrigger = NULL;
	context->currentPayload = PrtCloneValue(payload);

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

	packSize = PrtGetPackSize(context);

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
	context->packedReceiveCases = NULL;

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

PRT_VALUE *
PrtGetCurrentTrigger(
_Inout_ PRT_MACHINEINST_PRIV		*context
)
{
	return context->currentTrigger;
}

PRT_VALUE *
PrtGetCurrentPayload(
_Inout_ PRT_MACHINEINST_PRIV		*context
)
{
	return context->currentPayload;
}

void
PrtSendPrivate(
_In_ PRT_MACHINESTATE           *state,
_Inout_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_VALUE					*event,
_In_ PRT_VALUE					*payload
)
{
	PRT_EVENTQUEUE *queue;
	PRT_UINT32 tail;
	PRT_UINT32 eventMaxInstances;
	PRT_UINT32 maxQueueSize;
	PRT_UINT32 eventIndex;

	PrtAssert(!PrtIsSpecialEvent(event), "Enqueued event must not be null");
	PrtAssert(PrtInhabitsType(payload, PrtGetPayloadType(context, event)), "Payload must be member of event payload type");

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

	eventIndex = PrtPrimGetEvent(event);
	eventMaxInstances = program->events[eventIndex]->eventMaxInstances;
	maxQueueSize = program->machines[context->instanceOf]->maxQueueSize;

	queue = &context->eventQueue;

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

	tail = queue->tailIndex;

	//
	// Add event to the queue
	//
	queue->events[tail].trigger = event;
	queue->events[tail].payload = payload;
	if (state != NULL) {
		queue->events[tail].state = *state;
	}
	else {
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

	// Check if this event unblocks a blocking "receive" operation.
	// TODO: This needs to be fixed after the receive is implemented
    //if (context->receive != NULL)
	if (PRT_FALSE)
	{
        if (PrtIsEventReceivable(context, PrtPrimGetEvent(event)))
        {
            // receive is now unblocked, so tell the next call to PrtStepStateMachine to pick
            // up in the DoEntry state where it will re-initialize the call stack so the
            // Receive can continue where it left off.
            context->nextOperation = EntryOperation;
            PrtUnlockMutex(context->stateMachineLock);
            PrtScheduleWork(context);
        }
        else
        {
            // No point scheduling work if the receive is still blocked.
            PrtUnlockMutex(context->stateMachineLock);
        }
    }
    else 
    {
        PrtUnlockMutex(context->stateMachineLock);
        PrtScheduleWork(context);
    }
	return;
}

void
PrtEnqueueInOrder(
_In_ PRT_VALUE					*source,
_In_ PRT_INT64					seqNum,
_Inout_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_VALUE					*event,
_In_ PRT_VALUE					*payload
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
	else
	{
		PrtMapUpdate(context->recvMap, source, PrtMkIntValue((PRT_INT32)seqNum));
	}
	PrtUnlockMutex(context->stateMachineLock);

	// get the name of the sender machine.
	PRT_MACHINEINST_PRIV* senderMachine = (PRT_MACHINEINST_PRIV*)PrtGetMachine(context->process, source);
	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)senderMachine, &state);
	PrtSendPrivate(&state, context, event, payload);
}

PRT_VALUE *MakeTupleFromArray(_In_ PRT_TYPE *tupleType, _In_ PRT_VALUE **elems)
{
	PRT_UINT32 arity = tupleType->typeUnion.tuple->arity;
	PRT_VALUE *payload = PrtMkDefaultValue(tupleType);
	for (PRT_UINT32 i = 0; i < arity; i++)
	{
		PrtTupleSetEx(payload, i, elems[i], PRT_FALSE);
	}
	return payload;
}

PRT_VALUE *PrtMkTuple(_In_ PRT_TYPE *tupleType, ...)
{
	PRT_UINT32 arity = tupleType->typeUnion.tuple->arity;
	PRT_VALUE *tup = PrtMkDefaultValue(tupleType);

	va_list argp;
	va_start(argp, tupleType);
	for (PRT_UINT32 i = 0; i < arity; i++) {
		PRT_VALUE** argPtr = va_arg(argp, PRT_VALUE **);
		PrtTupleSetEx(tup, i, *argPtr, PRT_FALSE);
		*argPtr = NULL;
	}
	va_end(argp);

	return tup;
}

void
PrtGoto(
	_Inout_ PRT_MACHINEINST_PRIV		*context,
	_In_ PRT_UINT32						destStateIndex,
	_In_ PRT_UINT32						numArgs,
	...
)
{
	PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
	PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
	context->lastOperation = GotoStatement;
	context->destStateIndex = destStateIndex;
	context->currentTrigger = PrtMkEventValue(PRT_SPECIAL_EVENT_NULL);
	PRT_VALUE *payload = NULL;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else 
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			PRT_VALUE **argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_FUNDECL *entryFun = program->machines[context->instanceOf]->states[destStateIndex].entryFun;
			PRT_TYPE *payloadType = entryFun->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	context->currentPayload = payload;

	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PrtLog(PRT_STEP_GOTO, &state, context, NULL, payload);
}

void
PrtRaise(
	_Inout_ PRT_MACHINEINST_PRIV		*context,
	_In_ PRT_VALUE						*event,
	_In_ PRT_UINT32						numArgs,

	...
)
{
	PrtAssert(!PrtIsSpecialEvent(event), "Raised event must not be null");
	PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
	PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
	context->lastOperation = RaiseStatement;
	context->currentTrigger = event;
	PRT_VALUE *payload = NULL;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			PRT_VALUE **argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_TYPE *payloadType = PrtGetPayloadType(context, event); 
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PrtAssert(PrtInhabitsType(payload, PrtGetPayloadType(context, event)), "Payload must be member of event payload type");
	context->currentPayload = payload;

	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PrtLog(PRT_STEP_RAISE, &state, context, event, payload);
}

PRT_BOOLEAN
PrtReceive(
_Inout_ PRT_MACHINEINST_PRIV	*context
)
{
	/*
	PRT_FUNDECL *funDecl = funStackInfo->funDecl; 
	for (PRT_UINT32 i = 0; i < funDecl->nReceives; i++)
	{
		if (funDecl->receives[i].receiveIndex == receiveIndex)
		{
			context->receive = &funDecl->receives[i];
			break;
		}
	}
	PrtAssert(context->receive != NULL, "receiveIndex must correspond to a valid receive");
	funStackInfo->returnTo = receiveIndex;
	PrtLockMutex(context->stateMachineLock);
	PrtAssert(context->isRunning, "Machine must be running");
	if (PrtDequeueEvent(context, funStackInfo))
	{
		PrtUnlockMutex(context->stateMachineLock);
		return PRT_TRUE;
	}
	else
	{
		PrtPushFrame(context, funStackInfo);
		return PRT_FALSE;
	}
	*/
	return PRT_FALSE;
}

PRT_UINT32 PrtReceiveAsync(
	_Inout_ PRT_MACHINEINST_PRIV *context,
	_In_    PRT_UINT32           *handledEvents,
	_Out_   PRT_VALUE            *payload)
{
	// 1) Register handledEvents (maybe as bitfield) with the machine 
	//    as the current receive event set.
	// 2) Set up effect via libhandler, register callback with the
	//    machine context.
	// 3) Upon resume, pass values back to payload and return value.
	PrtAssert(PRT_FALSE, "receive not yet implemented!");
	return PRT_SPECIAL_EVENT_NULL;
}

void 
PrtFreeTriggerPayload(_In_ PRT_MACHINEINST_PRIV	*context)
{
	if (context->currentTrigger != NULL)
	{
		PrtFreeValue(context->currentTrigger);
		context->currentTrigger = NULL;
	}
	if (context->currentPayload != NULL)
	{
		PrtFreeValue(context->currentPayload);
		context->currentPayload = NULL;
	}
}


void
PrtPushState(
_Inout_ PRT_MACHINEINST_PRIV		*context,
_In_	PRT_UINT32				stateIndex
)
{
	PRT_UINT16 i;
	PRT_UINT16 packSize;
	PRT_UINT16 length;
	PRT_UINT32 *currDef;
	PRT_UINT32 *currActions;
	PRT_UINT32 *currTransitions;

	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);

	packSize = PrtGetPackSize(context);
	length = context->callStack.length;
	currDef = PrtGetDeferredPacked(context, context->currentState);
	currActions = PrtGetActionsPacked(context, context->currentState);
	currTransitions = PrtGetTransitionsPacked(context, context->currentState);

	PrtAssert(length < PRT_MAX_STATESTACK_DEPTH, "State stack overflow");

	context->callStack.stateStack[length].stateIndex = context->currentState;
	context->callStack.stateStack[length].inheritedDeferredSetCompact = PrtClonePackedSet(context->inheritedDeferredSetCompact, packSize);
	context->callStack.stateStack[length].inheritedActionSetCompact = PrtClonePackedSet(context->inheritedActionSetCompact, packSize);
	context->callStack.length = length + 1;

	// Update the defered set inherited by state-machine
	// D = (D + d) - a - e
	for (i = 0; i < packSize; ++i)
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
_Inout_ PRT_MACHINEINST_PRIV		*context
)
{
	context->lastOperation = PopStatement;
	PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
	PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
	// Actual pop happens in PrtPopState; the exit function must be executed first.
}

PRT_BOOLEAN
PrtPopState(
_Inout_ PRT_MACHINEINST_PRIV		*context,
_In_ PRT_BOOLEAN				isPopStatement
)
{
	PRT_UINT16 i;
	PRT_UINT16 packSize;
	PRT_UINT16 length;
	PRT_STATESTACK_INFO poppedState;
	PRT_BOOLEAN isHalted = PRT_FALSE;

	i = 0;
	packSize = PrtGetPackSize(context);
	length = context->callStack.length;

	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);

	if (length == 0)
	{
		// The stack can become empty because of either an unhandled event or en explicit pop.
		// We currently do not distinguish between the two in our logging.
		if (isPopStatement)
		{
			PrtHandleError(PRT_STATUS_EVENT_UNHANDLED, context);
		}
		else if (context->eventValue == PRT_SPECIAL_EVENT_HALT)
		{
			PrtHaltMachine(context);
			isHalted = PRT_TRUE;
		}
		else
		{
			PrtHandleError(PRT_STATUS_EVENT_UNHANDLED, context);
		}
		return isHalted;
	}

	context->callStack.length = length - 1;
	poppedState = context->callStack.stateStack[length - 1];
	context->currentState = poppedState.stateIndex;

	for (i = 0; i < packSize; i++)
	{
		context->inheritedDeferredSetCompact[i] = poppedState.inheritedDeferredSetCompact[i];
		context->inheritedActionSetCompact[i] = poppedState.inheritedActionSetCompact[i];
	}
	PrtFree(poppedState.inheritedDeferredSetCompact);
	PrtFree(poppedState.inheritedActionSetCompact);

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
	return isHalted;
}

FORCEINLINE
void
PrtRunExitFunction(
_In_ PRT_MACHINEINST_PRIV			*context
)
{
	PRT_STATEDECL *stateDecl = PrtGetCurrentStateDecl(context);
	context->lastOperation = ReturnStatement;

	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PrtLog(PRT_STEP_EXIT, &state, context, NULL, NULL);
	PRT_FUNDECL *exitFun = program->machines[context->instanceOf]->states[context->currentState].exitFun;
	PrtGetExitFunction(context)((PRT_MACHINEINST *)context, NULL);
}

FORCEINLINE
void
PrtRunTransitionFunction(
	_In_ PRT_MACHINEINST_PRIV			*context,
	_In_ PRT_UINT32						transIndex
)
{
	PRT_STATEDECL *stateDecl = PrtGetCurrentStateDecl(context);
	context->lastOperation = ReturnStatement; 
	PRT_FUNDECL *transFun = stateDecl->transitions[transIndex].transFun;
	PRT_DBG_ASSERT(transFun != NULL, "Must be valid function");
	PRT_VALUE** refLocals[1] = { &context->currentPayload };
	transFun->implementation((PRT_MACHINEINST *)context, refLocals);
}

static PRT_BOOLEAN
PrtStepStateMachine(
	_Inout_ PRT_MACHINEINST_PRIV	*context
)
{
	PRT_BOOLEAN lockHeld = PRT_FALSE;
	PRT_DODECL *currActionDecl;
	PRT_UINT32 eventValue;
	PRT_BOOLEAN hasMoreWork = PRT_FALSE;

    PrtAssert(context->isRunning, "The caller should have set context->isRunning to TRUE");

	switch (context->nextOperation)
	{
	case EntryOperation:
		goto DoEntry;
	case DequeueOperation:
		goto DoDequeue;
	case HandleEventOperation:
		goto DoHandleEvent;
	case ReceiveOperation:
		goto DoReceive;
	}

DoEntry:
	PrtUpdateCurrentActionsSet(context);
	PrtUpdateCurrentDeferredSet(context);

	context->lastOperation = ReturnStatement;
	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PRT_STATEDECL* currentState = PrtGetCurrentStateDecl(context);
	PrtLog(PRT_STEP_ENTRY, &state, context, NULL, NULL);
	
	PRT_FUNDECL *entryFun = currentState->entryFun;
	PRT_VALUE** refLocals[1] = { &context->currentPayload };
	entryFun->implementation((PRT_MACHINEINST *)context, refLocals);
	
	goto CheckLastOperation;

DoAction:
	currActionDecl = PrtGetAction(context, eventValue);
	PRT_FUNDECL *doFun = currActionDecl->doFun;
	context->lastOperation = ReturnStatement;
	if (doFun == NULL)
	{
		PRT_VALUE* event = PrtMkEventValue(eventValue);
		PRT_MACHINESTATE state;
		PrtGetMachineState((PRT_MACHINEINST*)context, &state);
		PrtLog(PRT_STEP_IGNORE, &state, context, event, NULL);
		PrtFree(event);
		PrtFreeTriggerPayload(context);
	}
	else
	{
		PRT_MACHINESTATE state;
		PrtGetMachineState((PRT_MACHINEINST*)context, &state);
		PrtLog(PRT_STEP_DO, &state, context, NULL, NULL);
		
		PRT_VALUE** refLocals[1] = { &context->currentPayload };
		doFun->implementation((PRT_MACHINEINST *)context, refLocals);
	}
	goto CheckLastOperation;

CheckLastOperation:
	if (context->packedReceiveCases != NULL)
	{
		// We are at a blocking "receive"; so, wait for PrtSendPrivate to unblock us.
		context->nextOperation = ReceiveOperation;
		lockHeld = PRT_TRUE; // tricky case, the lock was grabbed in PrtRecive().
		goto Finish;
	}
	switch (context->lastOperation)
	{
	case PopStatement:
		context->exitReason = OnPopStatement;
		PrtRunExitFunction(context);
		goto CheckLastOperation;
    
	case GotoStatement:
		context->exitReason = OnGotoStatement;
		PrtRunExitFunction(context);
		goto CheckLastOperation;

	case RaiseStatement:
		context->nextOperation = HandleEventOperation;
		hasMoreWork = PRT_TRUE;
		goto Finish;

	case ReturnStatement:
		switch (context->exitReason)
		{
		case NotExit:
			PrtFreeTriggerPayload(context); // ??
			context->nextOperation = DequeueOperation;
			hasMoreWork = PRT_TRUE;
			goto Finish;

		case OnPopStatement:
			PrtFreeTriggerPayload(context); // ??
			hasMoreWork = !PrtPopState(context, PRT_TRUE);
			context->nextOperation = DequeueOperation;
			context->exitReason = NotExit;
			goto Finish;

		case OnGotoStatement:
			hasMoreWork = PRT_TRUE;
			context->currentState = context->destStateIndex;
			context->nextOperation = EntryOperation;
			context->exitReason = NotExit;
			goto Finish;

		case OnUnhandledEvent:
			hasMoreWork = !PrtPopState(context, PRT_FALSE);
			context->nextOperation = HandleEventOperation;
			context->exitReason = NotExit; 
			goto Finish;

		case OnTransition:
			context->exitReason = OnTransitionAfterExit;
			PrtRunTransitionFunction(context, PrtFindTransition(context, context->eventValue));
			goto CheckLastOperation;

		case OnTransitionAfterExit:
			hasMoreWork = PRT_TRUE;
			PrtTakeTransition(context, context->eventValue);
			context->nextOperation = EntryOperation;
			context->exitReason = NotExit;
			goto Finish;

		default:
			PRT_DBG_ASSERT(0, "Unexpected case in switch");
			context->nextOperation = DequeueOperation;
			goto Finish;
		}
		break;

	default:
		PRT_DBG_ASSERT(0, "Unexpected case in switch");
		context->nextOperation = DequeueOperation;
		goto Finish;
	}

DoDequeue:
	PrtAssert(!lockHeld, "Lock should not be held at this point");
	lockHeld = PRT_TRUE;
	PrtLockMutex(context->stateMachineLock);

	PrtAssert(context->packedReceiveCases == NULL, "Machine must not be blocked at a receive");
	if (PrtDequeueEvent(context))
	{
		lockHeld = PRT_FALSE;
		PrtUnlockMutex(context->stateMachineLock);
		goto DoHandleEvent;
	}
	else
	{
		context->nextOperation = DequeueOperation;
		goto Finish;
	}

DoHandleEvent:
	PrtAssert(context->packedReceiveCases == NULL, "Must not be blocked at a receive");
	if (context->currentTrigger != NULL)
	{
		eventValue = PrtPrimGetEvent(context->currentTrigger);
		PrtFreeValue(context->currentTrigger);
		context->currentTrigger = NULL;
	}
	else
	{
		eventValue = context->eventValue;
	}
	if (PrtIsPushTransition(context, eventValue))
	{
		PrtTakeTransition(context, eventValue);
		goto DoEntry;
	}
	else if (PrtIsTransitionPresent(context, eventValue))
	{
		context->exitReason = OnTransition;
		context->eventValue = eventValue;
		PrtRunExitFunction(context);
		goto CheckLastOperation;
	}
	else if (PrtIsActionInstalled(eventValue, context->currentActionSetCompact))
	{
		goto DoAction;
	}
	else
	{
		context->exitReason = OnUnhandledEvent;
		context->eventValue = eventValue;
		PrtRunExitFunction(context);
		goto CheckLastOperation;
	}

DoReceive:
	PrtAssert(context->packedReceiveCases != NULL, "Must be blocked at a receive");
	// This is a no-op because we are still blocked on receive until PrtSendPrivate notices
	// we receive the unblocking event.  We do this instead of checking for receive != null
	// so that we can be sure to unlock the stateMachineLock once and only once.
	goto Finish;

Finish:
	if (lockHeld)
	{
		PrtUnlockMutex(context->stateMachineLock);
	}

	return hasMoreWork;
}

void
PrtRunStateMachine(
	_Inout_ PRT_MACHINEINST_PRIV	*context
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
	while (PrtStepStateMachine(context)) {
		;
	}

	PrtLockMutex(context->stateMachineLock);
	context->isRunning = PRT_FALSE;
	PrtUnlockMutex(context->stateMachineLock);
}

PRT_API PRT_STEP_RESULT
PrtStepProcess(PRT_PROCESS *process
)
{
    PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
	PRT_COOPERATIVE_SCHEDULER* info;
	PRT_UINT32 machineCount;

    PrtLockMutex(privateProcess->processLock);
	info = (PRT_COOPERATIVE_SCHEDULER*)privateProcess->schedulerInfo;
	info->threadsWaiting++;
	machineCount = privateProcess->machineCount;
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
		PRT_MACHINEINST_PRIV *context = (PRT_MACHINEINST_PRIV*)privateProcess->machines[i];
		PrtUnlockMutex(privateProcess->processLock);

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
_In_ PRT_MACHINEINST_PRIV		*context,
_In_ PRT_UINT32					eventIndex
)
{
	PRT_UINT32 i;
	PRT_UINT32 nTransitions;
	PRT_TRANSDECL* transTable;

	transTable = PrtGetTransitionTable(context, context->currentState, &nTransitions);

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
_Inout_ PRT_MACHINEINST_PRIV		*context,
_In_ PRT_UINT32					eventIndex
)
{
	PRT_UINT32 nTransitions;
	PRT_TRANSDECL *transTable = PrtGetTransitionTable(context, context->currentState, &nTransitions);
	PRT_UINT32 transIndex = PrtFindTransition(context, eventIndex);
	if (transTable[transIndex].transFun == NULL)
	{
		PrtPushState(context, transTable[transIndex].destStateIndex);
	}
	else
	{
		context->currentState = transTable[transIndex].destStateIndex;
	}
}

static void
RemoveElementFromQueue(_Inout_ PRT_MACHINEINST_PRIV *context, _In_ PRT_UINT32 i)
{
	PRT_EVENTQUEUE *queue = &context->eventQueue;
	PRT_UINT32 queueLength = queue->eventsSize;
	PRT_UINT32 head = queue->headIndex;
	
	//
	// Collapse the event queue on the removed event
	// by moving the previous elements forward.
	//
	for (; i > 0; i--) {
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
PrtDequeueEvent(
	_Inout_ PRT_MACHINEINST_PRIV	*context
)
{
	PRT_EVENTQUEUE *queue = &context->eventQueue;
	PRT_UINT32 queueLength = queue->eventsSize;
	PRT_UINT32 head = queue->headIndex;

	PRT_DBG_ASSERT(queue->size <= queueLength, "Check Failed");

	for (PRT_UINT32 i = 0; i < queue->size; i++) {
		PRT_UINT32 index = (head + i) % queueLength;
		PRT_EVENT e = queue->events[index];
		PRT_UINT32 triggerIndex = PrtPrimGetEvent(e.trigger);
		if (context->packedReceiveCases == NULL)
		{
			if (!PrtIsEventDeferred(triggerIndex, context->currentDeferredSetCompact))
			{
				PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
				PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
				context->currentTrigger = e.trigger;
				context->currentPayload = e.payload;
				RemoveElementFromQueue(context, i);
				PrtLog(PRT_STEP_DEQUEUE, &e.state, context, e.trigger, e.payload);
				return PRT_TRUE;
			}
		}
		else
		{
			if (PrtIsEventReceivable(context, triggerIndex))
			{
				PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
				PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
				context->currentTrigger = e.trigger;
				context->currentPayload = e.payload;
				RemoveElementFromQueue(context, i);

				//TODO: Need to handle receive correctly
				/*
				PrtLog(PRT_STEP_DEQUEUE, &e.state, context, e.trigger, e.payload);
				for (PRT_UINT32 j = 0; j < context->receive->nCases; j++)
				{
					PRT_CASEDECL *rcase = &context->receive->cases[j];
					if (triggerIndex == rcase->triggerEvent->value.valueUnion.ev)
					{
						frame->rcase = rcase;
						PrtPushNewEventHandlerFrame(context, rcase->fun, PRT_FUN_PARAM_MOVE, frame->locals);
						break;
					}
				}
				*/
				context->packedReceiveCases = NULL;
				return PRT_TRUE;
			}
		}
	}

	if (context->packedReceiveCases == NULL)
	{
		if (PrtStateHasDefaultTransitionOrAction(context))
		{
			PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
			PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
			context->currentTrigger = PrtMkEventValue(PRT_SPECIAL_EVENT_NULL);
			context->currentPayload = PrtMkNullValue();
			return PRT_TRUE;
		}
		else
		{
			PrtFreeTriggerPayload(context);
			return PRT_FALSE;
		}
	}
	else
	{
		//TODO: Need to handle receive correctly
		/*
		PRT_BOOLEAN hasDefaultCase = (context->packedReceiveCases[0] & 0x1) == 1;
		if (hasDefaultCase)
		{
			PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
			PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
			context->currentTrigger = PrtMkEventValue(PRT_SPECIAL_EVENT_NULL);
			context->currentPayload = PrtMkNullValue();
			for (PRT_UINT32 j = 0; j < context->receive->nCases; j++)
			{
				PRT_CASEDECL *rcase = &context->receive->cases[j];
				if (PRT_SPECIAL_EVENT_NULL == rcase->triggerEvent->value.valueUnion.ev)
				{
					frame->rcase = rcase;
					PrtPushNewEventHandlerFrame(context, rcase->fun, PRT_FUN_PARAM_MOVE, frame->locals);
					break;
				}
			}
			context->receive = NULL;
			return PRT_TRUE;
		}
		else
		{
			return PRT_FALSE;
		}
		*/
		return PRT_FALSE;
	}
}

FORCEINLINE
PRT_STATEDECL *
PrtGetCurrentStateDecl(
_In_ PRT_MACHINEINST_PRIV			*context
)
{
	return &(program->machines[context->instanceOf]->states[context->currentState]);
}

FORCEINLINE
PRT_TYPE*
PrtGetPayloadType(
_In_ PRT_MACHINEINST_PRIV *context,
_In_ PRT_VALUE	  *event
)
{
	return program->events[PrtPrimGetEvent(event)]->type;
}

FORCEINLINE
PRT_UINT16
PrtGetPackSize(
_In_ PRT_MACHINEINST_PRIV			*context
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
_In_ PRT_MACHINEINST_PRIV		*context
)
{
	PRT_FUNDECL *entryFun = program->machines[context->instanceOf]->states[context->currentState].entryFun;
	return entryFun->implementation;
}

FORCEINLINE
PRT_SM_FUN
PrtGetExitFunction(
_In_ PRT_MACHINEINST_PRIV		*context
)
{
	PRT_FUNDECL *exitFun = program->machines[context->instanceOf]->states[context->currentState].exitFun;
	return exitFun->implementation;
}

FORCEINLINE
PRT_DODECL*
PrtGetAction(
_In_ PRT_MACHINEINST_PRIV		*context,
_In_ PRT_UINT32					currEvent
)
{
	PRT_BOOLEAN isActionInstalled = PRT_FALSE;
	PRT_UINT32 ui, nActions;
	PRT_STATESTACK currStack;
	PRT_STATEDECL *stateTable;
	PRT_UINT32 topOfStackState;
	PRT_STATEDECL *stateDecl;
	PRT_DODECL *actionDecl = NULL;
	PRT_INT32 i;
	//check if action is defined for the current state
	isActionInstalled = PrtIsActionInstalled(currEvent, PrtGetActionsPacked(context, context->currentState));
	if (isActionInstalled)
	{
		//
		// get action function
		//
		stateDecl = PrtGetCurrentStateDecl(context);
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
	currStack = context->callStack;
	stateTable = program->machines[context->instanceOf]->states;
	for (i = currStack.length - 1; i >= 0; i--)
	{
		topOfStackState = currStack.stateStack[i].stateIndex;
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
_In_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_UINT32				stateIndex
)
{
	return program->machines[context->instanceOf]->states[stateIndex].defersSet->packedEvents;
}

FORCEINLINE
PRT_UINT32*
PrtGetActionsPacked(
_In_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_UINT32				stateIndex
)
{
	return program->machines[context->instanceOf]->states[stateIndex].doSet->packedEvents;
}

FORCEINLINE
PRT_UINT32*
PrtGetTransitionsPacked(
_In_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_UINT32				stateIndex
)
{
	return program->machines[context->instanceOf]->states[stateIndex].transSet->packedEvents;
}

FORCEINLINE
PRT_TRANSDECL*
PrtGetTransitionTable(
_In_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_UINT32				stateIndex,
_Out_ PRT_UINT32			*nTransitions
)
{
	*nTransitions = program->machines[context->instanceOf]->states[stateIndex].nTransitions;
	return program->machines[context->instanceOf]->states[stateIndex].transitions;
}

PRT_BOOLEAN
PrtAreGuidsEqual(
_In_ PRT_GUID guid1,
_In_ PRT_GUID guid2
)
{
	return guid1.data1 == guid2.data1 && guid1.data2 == guid2.data2 && guid1.data3 == guid2.data3 && guid1.data4 == guid2.data4;
}

PRT_BOOLEAN
PrtIsEventMaxInstanceExceeded(
_In_ PRT_EVENTQUEUE			*queue,
_In_ PRT_UINT32				eventIndex,
_In_ PRT_UINT32				maxInstances
)
{
	PRT_UINT32 queueSize;
	PRT_UINT32 head;
	PRT_UINT32 tail;
	PRT_UINT16 currMaxInstance;
	PRT_BOOLEAN isMaxInstancesExceeded;

	queueSize = queue->eventsSize;
	head = queue->headIndex;
	tail = queue->tailIndex;
	currMaxInstance = 0;
	isMaxInstancesExceeded = PRT_FALSE;
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
_In_ PRT_MACHINEINST_PRIV			*context
)
{
	PrtAssert(context->packedReceiveCases == NULL, "This function should not be called at a receive");
	PRT_STATEDECL *stateDecl = PrtGetCurrentStateDecl(context);
	PRT_BOOLEAN hasDefaultTransition = (stateDecl->transSet->packedEvents[0] & 0x1) == 1;
	PRT_BOOLEAN hasDefaultAction = (context->currentActionSetCompact[0] & 0x1) == 1;
	return hasDefaultTransition || hasDefaultAction;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsSpecialEvent(
_In_ PRT_VALUE *event
)
{
	return (PrtIsNullValue(event) || PrtPrimGetEvent(event) == PRT_SPECIAL_EVENT_NULL);
}

FORCEINLINE
PRT_BOOLEAN
PrtIsEventReceivable(
_In_ PRT_MACHINEINST_PRIV *context,
_In_ PRT_UINT32		eventIndex
)
{
	PrtAssert(context->packedReceiveCases != NULL, "PrtIsEventReceivable should be called only at a receive");
	PRT_UINT32 *caseSet = context->packedReceiveCases;
	return (caseSet[eventIndex / (sizeof(PRT_UINT32) * 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32) * 8)))) != 0;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsEventDeferred(
_In_ PRT_UINT32		eventIndex,
_In_ PRT_UINT32*	defSet
)
{
	return (defSet[eventIndex / (sizeof(PRT_UINT32) * 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32) * 8)))) != 0;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsActionInstalled(
_In_ PRT_UINT32		eventIndex,
_In_ PRT_UINT32*		actionSet
)
{
	return (actionSet[eventIndex / (sizeof(PRT_UINT32) * 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32) * 8)))) != 0;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsTransitionPresent(
_In_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_UINT32				eventIndex
)
{
	PRT_UINT32* transitionsPacked = PrtGetTransitionsPacked(context, context->currentState);
	return (transitionsPacked[eventIndex / (sizeof(PRT_UINT32) * 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32) * 8)))) != 0;
}

PRT_BOOLEAN
PrtIsPushTransition(
_In_ PRT_MACHINEINST_PRIV		*context,
_In_ PRT_UINT32					event
)
{
	PRT_UINT16 i;
	PRT_UINT32 nTransitions;
	PRT_TRANSDECL* transTable;

	transTable = PrtGetTransitionTable(context, context->currentState, &nTransitions);
	for (i = 0; i < nTransitions; ++i)
	{
		if (transTable[i].transFun == NULL && transTable[i].triggerEvent->value.valueUnion.ev == event)
		{
			return PRT_TRUE;
		}
	}
	return PRT_FALSE;
}

PRT_UINT32 *
PrtClonePackedSet(
_In_ PRT_UINT32 *				packedSet,
_In_ PRT_UINT32					size
)
{
	PRT_UINT32 *clone;
	PRT_UINT32 i;

	clone = (PRT_UINT32 *)PrtCalloc(size, sizeof(PRT_UINT32));
	for (i = 0; i < size; i++)
	{
		clone[i] = packedSet[i];
	}
	return clone;
}

void
PrtUpdateCurrentActionsSet(
_Inout_ PRT_MACHINEINST_PRIV			*context
)
{
	PRT_UINT16 i;
	PRT_UINT32 *currActionsPacked;
	PRT_UINT32 *currTransitionsPacked;
	PRT_UINT32 *currDefSetPacked;
	PRT_UINT16 packSize;

	packSize = PrtGetPackSize(context);
	currActionsPacked = PrtGetActionsPacked(context, context->currentState);
	currTransitionsPacked = PrtGetTransitionsPacked(context, context->currentState);
	currDefSetPacked = PrtGetDeferredPacked(context, context->currentState);
	//
	// A = (A -d) + a - e
	//
	for (i = 0; i < packSize; i++)
	{
		context->currentActionSetCompact[i] = context->inheritedActionSetCompact[i] & ~currDefSetPacked[i]; // A - d
		context->currentActionSetCompact[i] |= currActionsPacked[i];
		context->currentActionSetCompact[i] &= ~currTransitionsPacked[i];
	}
}

void
PrtUpdateCurrentDeferredSet(
_Inout_ PRT_MACHINEINST_PRIV			*context
)
{
	PRT_UINT16 i;
	PRT_UINT32 *currActionsPacked;
	PRT_UINT32 *currTransitionsPacked;
	PRT_UINT32 *currDefSetPacked;
	PRT_UINT16 packSize;

	packSize = PrtGetPackSize(context);
	currActionsPacked = PrtGetActionsPacked(context, context->currentState);
	currTransitionsPacked = PrtGetTransitionsPacked(context, context->currentState);
	currDefSetPacked = PrtGetDeferredPacked(context, context->currentState);

	//
	// D = (D + d) - a - e
	//
	for (i = 0; i < packSize; i++)
	{
		context->currentDeferredSetCompact[i] = context->inheritedDeferredSetCompact[i] | currDefSetPacked[i]; // D + d
		context->currentDeferredSetCompact[i] &= ~currActionsPacked[i]; // - a
		context->currentDeferredSetCompact[i] &= ~currTransitionsPacked[i]; // -e
	}
}

void
PrtResizeEventQueue(
_Inout_ PRT_MACHINEINST_PRIV *context
)
{
	PRT_UINT32 maxEventQueueSize = program->machines[context->instanceOf]->maxQueueSize;
	PRT_UINT32 currEventQueueSize = context->eventQueue.eventsSize;
	PRT_UINT32 newQueueSize = (maxEventQueueSize != 0xffffffff && currEventQueueSize * 2 > maxEventQueueSize) ? maxEventQueueSize : currEventQueueSize * 2;
	PRT_EVENT* oldQueue = context->eventQueue.events;
	PRT_UINT32 oldHead = context->eventQueue.headIndex;
	PRT_UINT32 oldTail = context->eventQueue.tailIndex;
	PRT_EVENT *newQueue = (PRT_EVENT*)PrtCalloc(newQueueSize, sizeof(PRT_EVENT));
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
_Inout_ PRT_MACHINEINST_PRIV			*context
)
{
	PRT_MACHINESTATE state;
	PrtGetMachineState((PRT_MACHINEINST*)context, &state);
	PrtLog(PRT_STEP_HALT, &state, context, NULL, NULL);
	PrtCleanupMachine(context);
}

void
PrtCleanupMachine(
_Inout_ PRT_MACHINEINST_PRIV			*context
)
{
	PrtLockMutex(context->stateMachineLock);
	if (context->isHalted)
	{
		PrtUnlockMutex(context->stateMachineLock);
		return;
	}
	context->isHalted = PRT_TRUE;

	if (context->eventQueue.events != NULL)
	{
		PRT_EVENT *queue = context->eventQueue.events;
		PRT_UINT32 head = context->eventQueue.headIndex;
		PRT_UINT32 count = 0;

		while (count < context->eventQueue.size && head < context->eventQueue.eventsSize)
		{
			if (queue[head].payload != NULL)
			{
				PrtFreeValue(queue[head].payload);
			}
			if (queue[head].trigger != NULL) {
				PrtFreeValue(queue[head].trigger);
			}
			head++;
			count++;
		}
		head = 0;
		while (count < context->eventQueue.size)
		{
			if (queue[head].payload != NULL)
			{
				PrtFreeValue(queue[head].payload);
			}
			if (queue[head].trigger != NULL) {
				PrtFreeValue(queue[head].trigger);
			}
			head++;
			count++;
		}

		PrtFree(context->eventQueue.events);
	}

	for (PRT_INT32 i = 0; i < context->callStack.length; i++)
	{
		PRT_STATESTACK_INFO *info = &context->callStack.stateStack[i];
		if (info->inheritedActionSetCompact != NULL)
		{
			PrtFree(info->inheritedActionSetCompact);
		}
		if (info->inheritedDeferredSetCompact != NULL)
		{
			PrtFree(info->inheritedDeferredSetCompact);
		}
	}

	if (context->currentActionSetCompact != NULL)
	{
		PrtFree(context->currentActionSetCompact);
	}

	if (context->currentDeferredSetCompact != NULL)
	{
		PrtFree(context->currentDeferredSetCompact);
	}

	if (context->inheritedActionSetCompact != NULL)
	{
		PrtFree(context->inheritedActionSetCompact);
	}

	if (context->inheritedDeferredSetCompact != NULL)
	{
		PrtFree(context->inheritedDeferredSetCompact);
	}

	if (context->varValues != NULL)
	{
		PRT_UINT32 i;
		PRT_MACHINEDECL *mdecl = program->machines[context->instanceOf];

		for (i = 0; i < mdecl->nVars; i++) {
			PrtFreeValue(context->varValues[i]);
		}
		PrtFree(context->varValues);
	}

	PrtFreeValue(context->id);

	PrtFreeTriggerPayload(context);

	if (context->recvMap != NULL)
	{
		PrtFreeValue(context->recvMap);
	}

	PrtUnlockMutex(context->stateMachineLock);
}

void
PrtHandleError(
_In_ PRT_STATUS ex,
_In_ PRT_MACHINEINST_PRIV *context
)
{
	((PRT_PROCESS_PRIV *)context->process)->errorHandler(ex, (PRT_MACHINEINST *)context);
}

void PRT_CALL_CONV
PrtAssertDefaultFn(
_In_ PRT_INT32 condition,
_In_opt_z_ PRT_CSTRING message
)
{
	if (condition != 0)
	{
		return;
	}
	else if (message == NULL)
	{
		PrtPrintf("ASSERT");
	}
	else
	{
		char buffer[256];
		int n_chars_written = snprintf(buffer, 256, "ASSERT: %s", message);
		//Truncate on overflow
		if (n_chars_written >= 256)
		{
			buffer[255] = '\0';
		}
		PrtPrintf(buffer);
	}
	abort();
}

void PRT_CALL_CONV
PrtPrintfDefaultFn(
_In_opt_z_ PRT_CSTRING message
)
{
	// do not allow % signs in message to be interpreted as arguments.
	printf_s("%s", message);
}

PRT_API void PRT_CALL_CONV
PrtUpdateAssertFn(
PRT_ASSERT_FUN assertFn
){
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
_In_ PRT_STEP step,
_In_ PRT_MACHINESTATE* senderState,
_In_ PRT_MACHINEINST_PRIV *receiver,
_In_ PRT_VALUE* eventId, 
_In_ PRT_VALUE* payload
) 
{
	((PRT_PROCESS_PRIV *)receiver->process)->logHandler(step, senderState, (PRT_MACHINEINST *)receiver,  eventId, payload);
}

void
PrtCheckIsLocalMachineId(
_In_ PRT_MACHINEINST *context,
_In_ PRT_VALUE *id
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

void PrtTraverseEventset(PRT_EVENTSETDECL *evset, PRT_BOOLEAN doInstall)
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

void PrtTraverseState(PRT_STATEDECL *state, PRT_BOOLEAN doInstall)
{
	PrtTraverseEventset(state->defersSet, doInstall);
	PrtTraverseEventset(state->doSet, doInstall);
	PrtTraverseEventset(state->transSet, doInstall);
}

void PrtTraverseMachine(PRT_MACHINEDECL *machine, PRT_BOOLEAN doInstall)
{
	for (PRT_UINT32 i = 0; i < machine->nStates; i++)
	{
		PrtTraverseState(&machine->states[i], doInstall);
	}
}

void PrtInstallProgram(_In_ PRT_PROGRAMDECL *p)
{
	PrtAssert(p != NULL && program == NULL, "p and program must be non-NULL");
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

PRT_PROCESS *
PrtStartProcess(
	_In_ PRT_GUID guid,
	_In_ PRT_PROGRAMDECL *p,
	_In_ PRT_ERROR_FUN errorFun,
	_In_ PRT_LOG_FUN logFun
)
{
	PrtInstallProgram(p);

	PRT_PROCESS_PRIV *process;
	process = (PRT_PROCESS_PRIV *)PrtMalloc(sizeof(PRT_PROCESS_PRIV));
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
	for (PRT_UINT32 i = 0; i < program->nMachines; i++) {
		if (strcmp(name, program->machines[i]->name) == 0) {
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

	PrtAssert(privateProcess->schedulingPolicy == PRT_SCHEDULINGPOLICY_COOPERATIVE, "PrtWaitForWork can only be called when PrtSetSchedulingPolicy has set PRT_SCHEDULINGPOLICY_COOPERATIVE mode");
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
PrtSetSchedulingPolicy(PRT_PROCESS *process, PRT_SCHEDULINGPOLICY policy)
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
			PrtAssert(PRT_FALSE, "PrtSetSchedulingPolicy must set either PRT_SCHEDULINGPOLICY_TASKNEUTRAL or PRT_SCHEDULINGPOLICY_COOPERATIVE");
		}
	}
}

PRT_API void
PrtRunProcess(PRT_PROCESS *process
)
{
	while (1)
	{
		PRT_STEP_RESULT result = PrtStepProcess(process);
		switch (result) {
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
	_Inout_ PRT_PROCESS* process
)
{
	PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV *)process;

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
		PRT_MACHINEINST *context = privateProcess->machines[i];
		PRT_MACHINEINST_PRIV * privContext = (PRT_MACHINEINST_PRIV *)context;
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
	for(PRT_UINT32 i = 0; i < creates->nInterfaces; i++)
	{
		if(interfaceCreated == creates->interfacesIndex[i])
		{
			return PRT_TRUE;
		}
	}

	return PRT_FALSE;
}

PRT_MACHINEINST *
PrtMkInterface(
	_In_ PRT_MACHINEINST*		creator,
	_In_ PRT_UINT32				IName,
	_In_ PRT_UINT32				numArgs,
	...
)
{
	PRT_MACHINEINST_PRIV* context = (PRT_MACHINEINST_PRIV*)creator;
	PRT_VALUE *payload = NULL;
	PRT_UINT32 interfaceCreated = program->linkMap[context->interfaceBound][IName];
	const PRT_UINT32 instance_of = program->interfaceDefMap[interfaceCreated];

	// Check the CreateOk condition
	PrtAssert(PrtInterfaceInCreatesSet(interfaceCreated, program->machines[creator->instanceOf]->creates), "Created Inteface is not in the creates set of the machine");

	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			//TODO: Confirm if the code below is correct.
			PRT_VALUE **argPtr;
			argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];

		if (numArgs > 1)
		{
			PRT_MACHINEDECL *machineDecl = program->machines[instance_of];
			PRT_FUNDECL *entryFun = machineDecl->states[machineDecl->initStateIndex].entryFun;
			PRT_TYPE *payloadType = entryFun->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PRT_MACHINEINST* result = (PRT_MACHINEINST*)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)context->process, interfaceCreated, instance_of, payload);
	// must now free this payload because PrtMkMachinePrivate clones it.
	PrtFreeValue(payload);
	return result;
}

PRT_MACHINEINST *
PrtMkMachine(
	_Inout_  PRT_PROCESS		*process,
	_In_ PRT_UINT32				interfaceName,
	_In_ PRT_UINT32				numArgs,
	...
)
{
	PRT_VALUE *payload;
	PRT_UINT32 instanceOf = program->interfaceDefMap[interfaceName];

	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{

			PRT_VALUE **argPtr;
			argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];

		if (numArgs > 1)
		{
			PRT_MACHINEDECL *machineDecl = program->machines[instanceOf];
			PRT_FUNDECL *entryFun = machineDecl->states[machineDecl->initStateIndex].entryFun;
			PRT_TYPE *payloadType = entryFun->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PRT_MACHINEINST* result = (PRT_MACHINEINST*)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)process, interfaceName, instanceOf, payload);
	// free the payload since we cloned it here, and PrtMkMachinePrivate also clones it.
	PrtFreeValue(payload);
	return result;
}

PRT_MACHINEINST *
PrtGetMachine(
	_In_ PRT_PROCESS *process,
	_In_ PRT_VALUE *id
)
{
	PRT_MACHINEID *machineId;
	PRT_PROCESS_PRIV *privateProcess;
	PrtAssert(id->discriminator == PRT_VALUE_KIND_MID, "id is not legal PRT_MACHINEID");
	machineId = id->valueUnion.mid;
	//Comented out by Ankush Desai.
	//PrtAssert(PrtAreGuidsEqual(process->guid, machineId->processId), "id does not belong to process");
	privateProcess = (PRT_PROCESS_PRIV *)process;
	PrtAssert((0 < machineId->machineId) && (machineId->machineId <= privateProcess->numMachines), "id out of bounds");
	return privateProcess->machines[machineId->machineId - 1];
}

void PRT_CALL_CONV PrtGetMachineState(_In_ PRT_MACHINEINST *context, _Inout_ PRT_MACHINESTATE* state)
{
	PRT_MACHINEINST_PRIV *priv = (PRT_MACHINEINST_PRIV*)context;
	state->machineId = context->id->valueUnion.mid->machineId;
	state->machineName = program->machines[context->instanceOf]->name;
	state->stateId = priv->currentState;
	state->stateName = PrtGetCurrentStateDecl(priv)->name;
}

void
PrtSend(
	_Inout_ PRT_MACHINESTATE 		*senderState,
	_Inout_ PRT_MACHINEINST			*receiver,
	_In_ PRT_VALUE					*event,
	_In_ PRT_UINT32					numArgs,
	...
)
{
	PRT_VALUE *payload = NULL;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			PRT_VALUE **argPtr;
			//TODO: Confirm if the code below is correct.
			argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
			
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_TYPE *payloadType = PrtGetPayloadType((PRT_MACHINEINST_PRIV *)receiver, event);
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PrtSendPrivate(senderState, (PRT_MACHINEINST_PRIV *)receiver, event, payload);
}


void
PRT_CALL_CONV PrtSendInternal(
	_Inout_ PRT_MACHINEINST *sender,
	_Inout_ PRT_MACHINEINST *receiver,
	_In_ PRT_VALUE *event,
	_In_ PRT_UINT32	numArgs,
	...
)
{
	PRT_MACHINESTATE senderState;
	PrtGetMachineState(sender, &senderState);

	PRT_VALUE *payload = NULL;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
			PRT_VALUE **argPtr;
			//TODO: Confirm if the code below is correct.
			argPtr = va_arg(argp, PRT_VALUE **);
			args[i] = *argPtr;
			*argPtr = NULL;
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_TYPE *payloadType = PrtGetPayloadType((PRT_MACHINEINST_PRIV *)receiver, event);
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}

	PrtSendPrivate(&senderState, (PRT_MACHINEINST_PRIV *)receiver, event, payload);
}

static void ResizeBuffer(_Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 numCharsWritten, PRT_UINT32 resizeNum)
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
		char *newBuffer = (char *)PrtCalloc(newBufferSize, sizeof(char));
		strcpy_s(newBuffer, newBufferSize, *buffer);
		PrtFree(*buffer);
		*buffer = newBuffer;
		*bufferSize = newBufferSize;
	}
}

static void PrtUserPrintUint16(_In_ PRT_UINT16 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 16);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%u", i);
}
static void PrtUserPrintUint32(_In_ PRT_UINT32 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 32);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%u", i);
}

static void PrtUserPrintUint64(_In_ PRT_UINT64 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 64);
    *numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%llu", (unsigned long long)i);
}

static void PrtUserPrintFloat(_In_ PRT_FLOAT i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	if (sizeof(PRT_FLOAT) == 4)
	{
		PRT_UINT32 written = *numCharsWritten;
		ResizeBuffer(buffer, bufferSize, written, 32);
		*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%f", i);
	}
	else
	{
		PRT_UINT32 written = *numCharsWritten;
		ResizeBuffer(buffer, bufferSize, written, 64);
		*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%lf", (double)i);
	}
}

static void PrtUserPrintInt(_In_ PRT_INT i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	if (sizeof(PRT_INT) == 4)
	{
		PrtUserPrintUint32((PRT_UINT32)i, buffer, bufferSize, numCharsWritten);
	}
	else
	{
		PrtUserPrintUint64(i, buffer, bufferSize, numCharsWritten);
	}
}

static void PrtUserPrintString(_In_ PRT_STRING s, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, (PRT_UINT32)strlen(s) + 1);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%s", s);
}

static void PrtUserPrintMachineId(_In_ PRT_MACHINEID id, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
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

static void PrtUserPrintType(_In_ PRT_TYPE *type, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
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
		PRT_MAPTYPE *mtype = type->typeUnion.map;
		PrtUserPrintString("map[", buffer, bufferSize, numCharsWritten);
		PrtUserPrintType(mtype->domType, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintType(mtype->codType, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = type->typeUnion.nmTuple;
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		for (i = 0; i < ntype->arity; ++i)
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
		PRT_SEQTYPE *stype = type->typeUnion.seq;
		PrtUserPrintString("seq[", buffer, bufferSize, numCharsWritten);
		PrtUserPrintType(stype->innerType, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = type->typeUnion.tuple;
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		if (ttype->arity == 1)
		{
			PrtUserPrintType(ttype->fieldTypes[0], buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(",)", buffer, bufferSize, numCharsWritten);
		}
		else
		{
			for (i = 0; i < ttype->arity; ++i)
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

static void PrtUserPrintValue(_In_ PRT_VALUE *value, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
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
		PRT_MAPVALUE *mval = value->valueUnion.map;
		PRT_MAPNODE *next = mval->first;
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
		PRT_UINT32 i;
		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		PrtUserPrintString("[", buffer, bufferSize, numCharsWritten);
		for (i = 0; i < sVal->size; ++i)
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
		PRT_UINT32 i;
		PRT_TUPVALUE *tval = value->valueUnion.tuple;
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		if (tval->size == 1)
		{
			PrtUserPrintValue(tval->values[0], buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(",)", buffer, bufferSize, numCharsWritten);
		}
		else
		{
			for (i = 0; i < tval->size; ++i)
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

static void PrtUserPrintStep(_In_ PRT_STEP step, PRT_MACHINESTATE *senderState, _In_ PRT_MACHINEINST *receiver, _In_ PRT_VALUE* event, _In_ PRT_VALUE* payload,
	_Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_MACHINEINST_PRIV * c = (PRT_MACHINEINST_PRIV *)receiver;
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
		PRT_MACHINEINST_PRIV *context = (PRT_MACHINEINST_PRIV *)receiver;
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
		eventName = program->events[c->eventValue]->name;
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

void PRT_CALL_CONV PrtPrintValue(_In_ PRT_VALUE *value)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintValue(value, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringValue(_In_ PRT_VALUE *value)
{
	char *buffer = NULL;
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
	size_t bufferSize = strlen(value) + 1;
	PRT_STRING buffer = (PRT_STRING)PrtCalloc(bufferSize, sizeof(char));
	strcpy_s(buffer, bufferSize, (const char*)value);
	return buffer;
}

void PRT_CALL_CONV PrtPrintType(_In_ PRT_TYPE *type)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintType(type, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringType(_In_ PRT_TYPE *type)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintType(type, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}

void PRT_CALL_CONV PrtPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINESTATE *senderState, _In_ PRT_MACHINEINST *receiver, _In_ PRT_VALUE* event, _In_ PRT_VALUE* payload)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintStep(step, senderState, receiver, event, payload, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringStep(_In_ PRT_STEP step, _In_ PRT_MACHINESTATE *senderState, _In_ PRT_MACHINEINST *receiver, _In_ PRT_VALUE* event, _In_ PRT_VALUE* payload)
{
	char *buffer = NULL;
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
	PRT_UINT32 numArgs, numSegs;
	numArgs = va_arg(argp, PRT_UINT32);
	PRT_VALUE **args = (PRT_VALUE **)PrtCalloc(numArgs, sizeof(PRT_VALUE *));
	for (PRT_UINT32 i = 0; i < numArgs; i++)
	{
		args[i] = va_arg(argp, PRT_VALUE *);
	}
	numSegs = va_arg(argp, PRT_UINT32);
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
