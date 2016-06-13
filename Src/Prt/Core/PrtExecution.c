#include "PrtExecution.h"

/* Initialize the function to default assert function */
PRT_ASSERT_FUN _PrtAssert = &PrtAssertDefaultFn;

/* Initialize the function to default print fucntion*/
PRT_PRINT_FUN PrtPrintf = &PrtPrintfDefaultFn;

void PRT_CALL_CONV PrtSetGlobalVarEx(_Inout_ PRT_MACHINEINST_PRIV *context, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneValue)
{
	PRT_DBG_ASSERT(PrtIsValidValue(value), "value is not valid");
	PRT_VALUE *oldValue = context->varValues[varIndex];
	context->varValues[varIndex] = cloneValue ? PrtCloneValue(value) : value;
	if (oldValue != NULL)
	{
		PRT_DBG_ASSERT(PrtIsValidValue(oldValue), "Variable must contain a valid value");
		PrtFreeValue(oldValue);
	}
}

void PRT_CALL_CONV PrtSetGlobalVar(_Inout_ PRT_MACHINEINST_PRIV *context, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE *value)
{
	PrtSetGlobalVarEx(context, varIndex, value, PRT_TRUE);
}

void PRT_CALL_CONV PrtSetLocalVarEx(_Inout_ PRT_VALUE **locals, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneValue)
{
	PRT_DBG_ASSERT(PrtIsValidValue(value), "value is not valid");
	PRT_VALUE *oldValue = locals[varIndex];
	locals[varIndex] = cloneValue ? PrtCloneValue(value) : value;
	if (oldValue != NULL)
	{
		PRT_DBG_ASSERT(PrtIsValidValue(oldValue), "Variable must contain a valid value");
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
_In_  PRT_UINT32				instanceOf,
_In_  PRT_VALUE					*payload
)
{
	PRT_UINT32 packSize;
	PRT_UINT32 nVars;
	PRT_UINT8 eQSize;
	PRT_MACHINEINST_PRIV *context;
	PRT_UINT32 i;

	PrtLockMutex(process->processLock);

	nVars = process->program->machines[instanceOf].nVars;
	eQSize = PRT_QUEUE_LEN_DEFAULT;

	//
	// Allocate memory for state machine context
	//
	context = (PRT_MACHINEINST_PRIV*)PrtMalloc(sizeof(PRT_MACHINEINST_PRIV));

	//
	// Add it to the array of machines in the process
	//
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
	context->extContext = NULL;
	context->isModel = PRT_FALSE;

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
	context->currentState = process->program->machines[context->instanceOf].initStateIndex;
	context->isRunning = PRT_FALSE;
	context->isHalted = PRT_FALSE; 
    context->nextOperation = EntryOperation;
	context->lastOperation = ReturnStatement;
	context->exitReason = NotExit;
	context->eventValue = 0;

	context->currentTrigger = NULL;
	context->currentPayload = PrtCloneValue(payload);

	//
	// Allocate memory for local variables and initialize them
	//
	context->varValues = NULL;
	if (nVars > 0)
	{
		context->varValues = PrtCalloc(nVars, sizeof(PRT_VALUE*));
		for (i = 0; i < nVars; i++)
		{
			context->varValues[i] = PrtMkDefaultValue(process->program->machines[instanceOf].vars[i].type);
		}
	}

	context->receive = NULL;

	//
	// Initialize various stacks
	//
	context->callStack.length = 0;
	context->funStack.length = 0;

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

	//
	//Log
	//
	PrtLog(PRT_STEP_CREATE, context);

	//
	// Allocate external context Structure
	//
	process->program->machines[context->instanceOf].extCtorFun((PRT_MACHINEINST *)context, payload);

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
_Inout_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_VALUE					*event,
_In_ PRT_VALUE					*payload,
_In_ PRT_BOOLEAN				doTransfer
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
		return;
	}

	eventIndex = PrtPrimGetEvent(event);
	eventMaxInstances = context->process->program->events[eventIndex].eventMaxInstances;
	maxQueueSize = context->process->program->machines[context->instanceOf].maxQueueSize;

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
	queue->events[tail].trigger = PrtCloneValue(event);
	queue->events[tail].payload = doTransfer ? payload : PrtCloneValue(payload);
	queue->size++;
	queue->tailIndex = (tail + 1) % queue->eventsSize;

	//
	//Log
	//
	PrtLog(PRT_STEP_ENQUEUE, context);

	// Check if this event unblocks a blocking "receive" operation.  
    if (context->receive != NULL)
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

	PrtSendPrivate(context, event, payload, PRT_TRUE);
}

void
PrtRaise(
_Inout_ PRT_MACHINEINST_PRIV		*context,
_In_ PRT_VALUE						*event,
_In_ PRT_VALUE						*payload
)
{
	PrtAssert(!PrtIsSpecialEvent(event), "Raised event must not be null");
	PrtAssert(PrtInhabitsType(payload, PrtGetPayloadType(context, event)), "Payload must be member of event payload type");
	context->lastOperation = RaiseStatement;
	PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
	PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
	context->currentTrigger = PrtCloneValue(event);
	context->currentPayload = PrtCloneValue(payload);
	PrtLog(PRT_STEP_RAISE, context);
}

PRT_BOOLEAN
PrtReceive(
_Inout_ PRT_MACHINEINST_PRIV	*context,
_Inout_ PRT_FUNSTACK_INFO		*funStackInfo,
_In_ PRT_UINT16					receiveIndex
)
{
	PRT_UINT32 funIndex = funStackInfo->funIndex;
	PRT_FUNDECL *funDecl = &context->process->program->machines[context->instanceOf].funs[funIndex];
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
}

PRT_FUNSTACK_INFO *
PrtTopOfFunStack(
_In_ PRT_MACHINEINST_PRIV	*context)
{
	PrtAssert(0 < context->funStack.length, "Illegal fun stack access");
	return &context->funStack.funs[0];
}

PRT_FUNSTACK_INFO *
PrtBottomOfFunStack(
_In_ PRT_MACHINEINST_PRIV	*context)
{
	PrtAssert(0 < context->funStack.length, "Illegal fun stack access");
	return &context->funStack.funs[context->funStack.length - 1];
}

static void 
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
PrtPushNewEventHandlerFrame(
	_Inout_ PRT_MACHINEINST_PRIV	*context,
	_In_ PRT_UINT32					funIndex,
	_In_ PRT_FUN_PARAM_STATUS       payloadStatus,
	_In_ PRT_VALUE					**locals
)
{
	PrtAssert(payloadStatus != PRT_FUN_PARAM_CLONE, "Incorrect payload status value");
	PRT_UINT16 length = context->funStack.length;
	PrtAssert(length < PRT_MAX_FUNSTACK_DEPTH, "Fun stack overflow");
	context->funStack.length = length + 1;
	context->funStack.funs[length].funIndex = funIndex;
	PRT_BOOLEAN freeLocals = PRT_FALSE;
	PRT_FUNDECL *funDecl = &(context->process->program->machines[context->instanceOf].funs[funIndex]);
	PRT_VALUE ***refArgs = NULL;
	if (locals == NULL && funDecl->maxNumLocals != 0)
	{
		locals = PrtCalloc(funDecl->maxNumLocals, sizeof(PRT_VALUE *));
		freeLocals = PRT_TRUE;
	}
	PRT_UINT32 count = funDecl->numEnvVars;
	if (funDecl->name == NULL)
	{
		PrtAssert(0 < count, "numEnvVars must be positive for anonymous function");
		PRT_UINT32 payloadIndex = count - 1;
		if (locals[payloadIndex] != NULL)
		{
			PrtFreeValue(locals[payloadIndex]);
		}
		locals[payloadIndex] = context->currentPayload;
		context->currentPayload = NULL;
		if (payloadStatus == PRT_FUN_PARAM_REF)
		{
			refArgs = PrtCalloc(1, sizeof(PRT_VALUE **));
			refArgs[0] = &context->currentPayload;
		}
		else 
		{
			if (context->currentTrigger != NULL)
			{
				PrtFreeValue(context->currentTrigger);
				context->currentTrigger = NULL;
			}
		}
	}
	else
	{
		if (payloadStatus != PRT_FUN_PARAM_REF)
		{
			PrtFreeTriggerPayload(context);
		}
	}
	if (funDecl->localsNmdTupType != NULL)
	{
		PRT_UINT32 size = funDecl->localsNmdTupType->typeUnion.nmTuple->arity;
		for (PRT_UINT32 i = 0; i < size; i++)
		{
			PRT_TYPE *indexType = funDecl->localsNmdTupType->typeUnion.nmTuple->fieldTypes[i];
			if (locals[count] != NULL)
			{
				PrtFreeValue(locals[count]);
			}
			locals[count] = PrtMkDefaultValue(indexType);
			count++;
		}
	}
	context->funStack.funs[length].locals = locals;
	context->funStack.funs[length].freeLocals = freeLocals;
	context->funStack.funs[length].refArgs = refArgs;
	context->funStack.funs[length].returnTo = 0xFFFF;
	context->funStack.funs[length].rcase = NULL;
}

PRT_VALUE *
PrtPushNewFrame(
	_Inout_ PRT_MACHINEINST_PRIV	*context,
	_In_ PRT_UINT32					funIndex,
	...
)
{
	PRT_UINT16 length = context->funStack.length;
	PrtAssert(length < PRT_MAX_FUNSTACK_DEPTH, "Fun stack overflow");
	context->funStack.length = length + 1;
	context->funStack.funs[length].funIndex = funIndex;
	PRT_FUNDECL *funDecl = &(context->process->program->machines[context->instanceOf].funs[funIndex]);
	PRT_VALUE **locals = NULL;
	PRT_VALUE ***refArgs = NULL;
	PRT_BOOLEAN freeLocals = PRT_FALSE;
	if (funDecl->maxNumLocals == 0)
	{
		PrtAssert(funDecl->localsNmdTupType == NULL, "Incorrect maxNumLocals value");
	}
	else
	{
		PRT_UINT32 numLocals = 0;
		if (funDecl->localsNmdTupType != NULL)
		{
			numLocals = funDecl->localsNmdTupType->typeUnion.nmTuple->arity;
		}
		locals = PrtCalloc(funDecl->maxNumLocals, sizeof(PRT_VALUE *));
		freeLocals = PRT_TRUE;
		PRT_UINT32 numParameters = funDecl->maxNumLocals - numLocals;
		PRT_UINT32 count = 0;
		if (0 < numParameters)
		{
			refArgs = PrtCalloc(numParameters, sizeof(PRT_VALUE **));
			va_list argp;
			va_start(argp, funIndex);
			for (PRT_UINT32 i = 0; i < numParameters; i++)
			{
#if __PX4_NUTTX
                PRT_FUN_PARAM_STATUS argStatus = (PRT_FUN_PARAM_STATUS)va_arg(argp, int);
#else
				PRT_FUN_PARAM_STATUS argStatus = va_arg(argp, PRT_FUN_PARAM_STATUS);
#endif
				PRT_VALUE *arg;
				PRT_VALUE **argPtr;
				switch (argStatus)
				{
				case PRT_FUN_PARAM_CLONE:
					arg = va_arg(argp, PRT_VALUE *);
					locals[count] = PrtCloneValue(arg);
					break;
				case PRT_FUN_PARAM_REF:
					argPtr = va_arg(argp, PRT_VALUE **);
					refArgs[count] = argPtr;
					locals[count] = *argPtr;
					*argPtr = NULL;
					break;
				case PRT_FUN_PARAM_XFER:
					argPtr = va_arg(argp, PRT_VALUE **);
					locals[count] = *argPtr;
					*argPtr = NULL;
					break;
				}
				count++;
			}
			va_end(argp);
		}
		for (PRT_UINT32 i = 0; i < numLocals; i++)
		{
			PRT_TYPE *indexType = funDecl->localsNmdTupType->typeUnion.nmTuple->fieldTypes[i];
			locals[count] = PrtMkDefaultValue(indexType);
			count++;
		}
	}
	context->funStack.funs[length].locals = locals;
	context->funStack.funs[length].freeLocals = freeLocals;
	context->funStack.funs[length].refArgs = refArgs;
	context->funStack.funs[length].returnTo = 0xFFFF;
	context->funStack.funs[length].rcase = NULL;
	return NULL;
}

void
PrtPushFrame(
_Inout_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_FUNSTACK_INFO			*funStackInfo
)
{
	PRT_UINT16 length = context->funStack.length;
	PrtAssert(length < PRT_MAX_FUNSTACK_DEPTH, "Fun stack overflow");
	context->funStack.length = length + 1;
	context->funStack.funs[length].funIndex = funStackInfo->funIndex;
	context->funStack.funs[length].locals = funStackInfo->locals;
	context->funStack.funs[length].refArgs = funStackInfo->refArgs;
	context->funStack.funs[length].freeLocals = funStackInfo->freeLocals;
	context->funStack.funs[length].returnTo = funStackInfo->returnTo;
	context->funStack.funs[length].rcase = funStackInfo->rcase;
}

void
PrtPopFrame(
_Inout_ PRT_MACHINEINST_PRIV	*context,
_Inout_ PRT_FUNSTACK_INFO *funStackInfo
)
{
	PRT_UINT16 length = context->funStack.length;
	PrtAssert(0 < length, "Fun stack underflow");
	PRT_UINT16 top = length - 1;
	funStackInfo->funIndex = context->funStack.funs[top].funIndex;
	funStackInfo->locals = context->funStack.funs[top].locals;
	funStackInfo->refArgs = context->funStack.funs[top].refArgs;
	funStackInfo->freeLocals = context->funStack.funs[top].freeLocals;
	funStackInfo->returnTo = context->funStack.funs[top].returnTo;
	funStackInfo->rcase = context->funStack.funs[top].rcase;
	context->funStack.length = top;
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
	PrtLog(PRT_STEP_PUSH, context);
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
		PrtLog(PRT_STEP_POP, context);
	}
	else
	{
		// unhandled event
		PrtLog(PRT_STEP_UNHANDLED, context);
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
	PrtLog(PRT_STEP_EXIT, context);
	PRT_UINT32 exitFunIndex = context->process->program->machines[context->instanceOf].states[context->currentState].exitFunIndex;
	PrtPushNewEventHandlerFrame(context, exitFunIndex, PRT_FUN_PARAM_REF, NULL);
	PrtGetExitFunction(context)((PRT_MACHINEINST *)context);
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
	PRT_UINT32 transFunIndex = stateDecl->transitions[transIndex].transFunIndex;
	PRT_DBG_ASSERT(transFunIndex != PRT_SPECIAL_ACTION_PUSH_OR_IGN, "Must be valid function index");
	PrtPushNewEventHandlerFrame(context, transFunIndex, PRT_FUN_PARAM_REF, NULL);
	context->process->program->machines[context->instanceOf].funs[transFunIndex].implementation((PRT_MACHINEINST *)context);
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
	if (context->funStack.length == 0)
	{
		PrtLog(PRT_STEP_ENTRY, context);
		PRT_UINT32 entryFunIndex = context->process->program->machines[context->instanceOf].states[context->currentState].entryFunIndex;
		PrtPushNewEventHandlerFrame(context, entryFunIndex, PRT_FUN_PARAM_XFER, NULL);
	}
	PRT_UINT32 funIndex = PrtBottomOfFunStack(context)->funIndex;
	context->process->program->machines[context->instanceOf].funs[funIndex].implementation((PRT_MACHINEINST *)context);
	goto CheckLastOperation;

DoAction:
	currActionDecl = PrtGetAction(context, eventValue);
	PRT_UINT32 doFunIndex = currActionDecl->doFunIndex;
	context->lastOperation = ReturnStatement;
	if (doFunIndex == PRT_SPECIAL_ACTION_PUSH_OR_IGN)
	{
		PrtLog(PRT_STEP_IGNORE, context);
		PrtFreeTriggerPayload(context);
	}
	else
	{
		if (context->funStack.length == 0)
		{
			PrtLog(PRT_STEP_DO, context);
			PrtPushNewEventHandlerFrame(context, doFunIndex, PRT_FUN_PARAM_XFER, NULL);
		}
		funIndex = PrtBottomOfFunStack(context)->funIndex;
		context->process->program->machines[context->instanceOf].funs[funIndex].implementation((PRT_MACHINEINST *)context);
	}
	goto CheckLastOperation;

CheckLastOperation:
	if (context->receive != NULL)
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
        
	case RaiseStatement:
		context->nextOperation = HandleEventOperation;
		hasMoreWork = PRT_TRUE;
		goto Finish;

	case ReturnStatement:
		switch (context->exitReason)
		{
		case NotExit:
			context->nextOperation = DequeueOperation;
			hasMoreWork = PRT_TRUE;
			goto Finish;

		case OnPopStatement:
			hasMoreWork = !PrtPopState(context, PRT_TRUE);
			context->nextOperation = DequeueOperation;
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
			PrtTakeTransition(context, context->eventValue);
			context->exitReason = NotExit; 
			goto DoEntry;

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

	PrtAssert(context->receive == NULL, "Machine must not be blocked at a receive");
	if (PrtDequeueEvent(context, NULL))
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
	PrtAssert(context->receive == NULL, "Must not be blocked at a receive");
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
	PrtAssert(context->receive != NULL, "Must be blocked at a receive");
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
		if (transTable[i].triggerEventIndex == eventIndex)
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
	if (transTable[transIndex].transFunIndex == PRT_SPECIAL_ACTION_PUSH_OR_IGN)
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
	_Inout_ PRT_MACHINEINST_PRIV	*context,
	_Inout_ PRT_FUNSTACK_INFO		*frame
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
		if (context->receive == NULL)
		{
			if (!PrtIsEventDeferred(triggerIndex, context->currentDeferredSetCompact))
			{
				PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
				PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
				context->currentTrigger = e.trigger;
				context->currentPayload = e.payload;
				RemoveElementFromQueue(context, i);
				PrtLog(PRT_STEP_DEQUEUE, context);
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
				PrtLog(PRT_STEP_DEQUEUE, context);
				for (PRT_UINT32 j = 0; j < context->receive->nCases; j++)
				{
					PRT_CASEDECL *rcase = &context->receive->cases[j];
					if (triggerIndex == rcase->triggerEventIndex)
					{
						frame->rcase = rcase;
						PrtPushNewEventHandlerFrame(context, rcase->funIndex, PRT_FUN_PARAM_XFER, frame->locals);
						break;
					}
				}
				context->receive = NULL;
				return PRT_TRUE;
			}
		}
	}

	if (context->receive == NULL)
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
			return PRT_FALSE;
		}
	}
	else
	{
		PRT_BOOLEAN hasDefaultCase = (context->process->program->eventSets[context->receive->caseSetIndex].packedEvents[0] & 0x1) == 1;
		if (hasDefaultCase)
		{
			PrtAssert(context->currentTrigger == NULL, "currentTrigger must be null");
			PrtAssert(context->currentPayload == NULL, "currentPayload must be null");
			context->currentTrigger = PrtMkEventValue(PRT_SPECIAL_EVENT_NULL);
			context->currentPayload = PrtMkNullValue();
			for (PRT_UINT32 j = 0; j < context->receive->nCases; j++)
			{
				PRT_CASEDECL *rcase = &context->receive->cases[j];
				if (PRT_SPECIAL_EVENT_NULL == rcase->triggerEventIndex)
				{
					frame->rcase = rcase;
					PrtPushNewEventHandlerFrame(context, rcase->funIndex, PRT_FUN_PARAM_XFER, frame->locals);
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
	}
}

FORCEINLINE
PRT_STATEDECL *
PrtGetCurrentStateDecl(
_In_ PRT_MACHINEINST_PRIV			*context
)
{
	return &(context->process->program->machines[context->instanceOf].states[context->currentState]);
}

FORCEINLINE
PRT_TYPE*
PrtGetPayloadType(
_In_ PRT_MACHINEINST_PRIV *context,
_In_ PRT_VALUE	  *event
)
{
	return context->process->program->events[PrtPrimGetEvent(event)].type;
}

FORCEINLINE
PRT_UINT16
PrtGetPackSize(
_In_ PRT_MACHINEINST_PRIV			*context
)
{
	PRT_UINT32 nEvents = context->process->program->nEvents;
	PrtAssert(0 < nEvents, "Illegal number of events");
	PRT_UINT32 highestEventIndex = nEvents - 1;
	return 1 + (PRT_UINT16)(highestEventIndex / (sizeof(PRT_UINT32) * 8));
}

PRT_VALUE *
PrtWrapFunStmt(
_Inout_ PRT_FUNSTACK_INFO		*frame,
_In_ PRT_UINT16					funCallIndex,
_Inout_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_UINT32					funIndex
)
{
	PRT_SM_FUN fun = context->process->program->machines[context->instanceOf].funs[funIndex].implementation;
	PRT_VALUE *returnValue = fun((PRT_MACHINEINST *)context);
	if (context->receive != NULL)
	{
		frame->returnTo = funCallIndex;
		PrtPushFrame(context, frame);
	}
	else
	{
		frame->returnTo = 0xFFFF;
	}
	return returnValue;
}

void
PrtFreeLocals(
	_In_ PRT_MACHINEINST_PRIV		*context,
	_Inout_ PRT_FUNSTACK_INFO		*frame
)
{
	if (frame->locals == NULL)
	{
		return;
	}

	if (!frame->freeLocals)
	{
		return;
	}

	PRT_FUNDECL *funDecl = &context->process->program->machines[context->instanceOf].funs[frame->funIndex];

	if (frame->refArgs != NULL)
	{
		PRT_UINT32 numParameters = 1;
		if (funDecl->name != NULL)
		{
			PRT_UINT32 numLocals = 0;
			if (funDecl->localsNmdTupType != NULL)
			{
				numLocals = funDecl->localsNmdTupType->typeUnion.nmTuple->arity;
			}
			numParameters = funDecl->maxNumLocals - numLocals;
		}
		for (PRT_UINT32 i = 0; i < numParameters; i++)
		{
			if (frame->refArgs[i] != NULL)
			{
				*(frame->refArgs[i]) = frame->locals[i];
				frame->locals[i] = NULL;
			}
		}
		PrtFree(frame->refArgs);
	}

	for (PRT_UINT32 i = 0; i < funDecl->maxNumLocals; i++)
	{
		if (frame->locals[i] != NULL)
		{
			PrtFreeValue(frame->locals[i]);
		}
	}
	PrtFree(frame->locals);
}

PRT_SM_FUN
PrtGetFunction(
_In_ PRT_MACHINEINST_PRIV		*context,
_In_ PRT_UINT32 funIndex
)
{
	return context->process->program->machines[context->instanceOf].funs[funIndex].implementation;
}

FORCEINLINE
PRT_SM_FUN
PrtGetEntryFunction(
_In_ PRT_MACHINEINST_PRIV		*context
)
{
	PRT_UINT32 entryFunIndex = context->process->program->machines[context->instanceOf].states[context->currentState].entryFunIndex;
	return context->process->program->machines[context->instanceOf].funs[entryFunIndex].implementation;
}

FORCEINLINE
PRT_SM_FUN
PrtGetExitFunction(
_In_ PRT_MACHINEINST_PRIV		*context
)
{
	PRT_UINT32 exitFunIndex = context->process->program->machines[context->instanceOf].states[context->currentState].exitFunIndex;
	return context->process->program->machines[context->instanceOf].funs[exitFunIndex].implementation;
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
			if (stateDecl->dos[ui].triggerEventIndex == currEvent)
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
	stateTable = context->process->program->machines[context->instanceOf].states;
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
				if (stateTable[topOfStackState].dos[ui].triggerEventIndex == currEvent)
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
	PRT_EVENTSETDECL* evSets = context->process->program->eventSets;
	PRT_UINT32 evSetIndex = context->process->program->machines[context->instanceOf].states[stateIndex].defersSetIndex;
	return evSets[evSetIndex].packedEvents;
}

FORCEINLINE
PRT_UINT32*
PrtGetActionsPacked(
_In_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_UINT32				stateIndex
)
{
	PRT_EVENTSETDECL* evSets = context->process->program->eventSets;
	PRT_UINT32 evSetIndex = context->process->program->machines[context->instanceOf].states[stateIndex].doSetIndex;
	return evSets[evSetIndex].packedEvents;
}

FORCEINLINE
PRT_UINT32*
PrtGetTransitionsPacked(
_In_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_UINT32				stateIndex
)
{
	PRT_EVENTSETDECL* evSets = context->process->program->eventSets;
	PRT_UINT32 evSetIndex = context->process->program->machines[context->instanceOf].states[stateIndex].transSetIndex;
	return evSets[evSetIndex].packedEvents;
}

FORCEINLINE
PRT_TRANSDECL*
PrtGetTransitionTable(
_In_ PRT_MACHINEINST_PRIV	*context,
_In_ PRT_UINT32				stateIndex,
_Out_ PRT_UINT32			*nTransitions
)
{
	*nTransitions = context->process->program->machines[context->instanceOf].states[stateIndex].nTransitions;
	return context->process->program->machines[context->instanceOf].states[stateIndex].transitions;
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
	PrtAssert(context->receive == NULL, "This function should not be called at a receive");
	PRT_STATEDECL *stateDecl = PrtGetCurrentStateDecl(context);
	PRT_BOOLEAN hasDefaultTransition = (context->process->program->eventSets[stateDecl->transSetIndex].packedEvents[0] & 0x1) == 1;
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
	PRT_RECEIVEDECL	*receive = context->receive;
	PrtAssert(receive != NULL, "PrtIsEventReceivable should be called only at a receive");
	PRT_EVENTSETDECL *evSets = context->process->program->eventSets;
	PRT_UINT32 *caseSet = evSets[receive->caseSetIndex].packedEvents;
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
		if (transTable[i].transFunIndex == PRT_SPECIAL_ACTION_PUSH_OR_IGN && transTable[i].triggerEventIndex == event)
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
	PRT_UINT32 maxEventQueueSize = context->process->program->machines[context->instanceOf].maxQueueSize;
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
	PRT_DBG_ASSERT(!context->isModel, "Must be a real machine");
	PrtLog(PRT_STEP_HALT, context);
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

	for (PRT_INT32 i = 0; i < context->funStack.length; i++)
	{
		PRT_FUNSTACK_INFO *info = &context->funStack.funs[i];
		PrtFreeLocals(context, info);
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
		PRT_MACHINEDECL *mdecl = &(context->process->program->machines[context->instanceOf]);

		for (i = 0; i < mdecl->nVars; i++) {
			PrtFreeValue(context->varValues[i]);
		}
		PrtFree(context->varValues);
	}

	if (context->extContext != NULL)
		context->process->program->machines[context->instanceOf].extDtorFun((PRT_MACHINEINST *)context);
	PrtFreeValue(context->id);

	PrtFreeTriggerPayload(context);

	if (context->recvMap != NULL)
	{
		PrtFreeValue(context->recvMap);
	}

	PrtUnlockMutex(context->stateMachineLock);
}

void
PrtCleanupModel(
_Inout_ PRT_MACHINEINST			*context
)
{
	if (context->extContext != NULL)
		context->process->program->modelImpls[context->instanceOf].dtorFun(context);
	PrtFreeValue(context->id);
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
		fprintf_s(stderr, "ASSERT");
	}
	else
	{
		fprintf_s(stderr, "ASSERT: %s", message);
	}
	exit(1);
}

void PRT_CALL_CONV
PrtPrintfDefaultFn(
_In_opt_z_ PRT_CSTRING message
)
{
	printf_s(message);
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
_In_ PRT_MACHINEINST_PRIV *context
)
{
	((PRT_PROCESS_PRIV *)context->process)->logHandler(step, (PRT_MACHINEINST *)context);
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
