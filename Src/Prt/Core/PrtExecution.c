#include "PrtExecution.h"

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
		for (PRT_UINT32 i = 0; i < machineCount; i++)
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
	// Initialize Machine Internal Variables
	//
	context->currentState = process->program->machines[context->instanceOf].initStateIndex;
	context->isRunning = PRT_FALSE;
	context->isHalted = PRT_FALSE;
	context->lastOperation = ReturnStatement;

	context->currentEvent.trigger = PrtMkEventValue(PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
	context->currentEvent.payload = PrtCloneValue(payload);
	context->stateControl = PrtStateEntry;
	context->returnTo = 0;

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

	//
	// Machine Call State Depth
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

	//
	//Log
	//
	PrtLog(PRT_STEP_CREATE, context);

	//
	// Allocate External context Structure
	//
	process->program->machines[context->instanceOf].extCtorFun((PRT_MACHINEINST *)context, payload);

	PrtUnlockMutex(process->processLock);

	//
	// Run the state machine
	//
	PrtRunStateMachine(context, PRT_FALSE);

	return context;
}

void
PrtSendPrivate(
	_Inout_ PRT_MACHINEINST_PRIV		*context,
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
	PrtAssert(PrtIsSubtype(payload->type, PrtGetPayloadType(context, event)), "Actual payload type must be subtype of event payload type");
	
	if (context->isHalted)
	{
		// drop the event silently
		return;
	}

	eventIndex = PrtPrimGetEvent(event);
	eventMaxInstances = context->process->program->events[eventIndex].eventMaxInstances;
	maxQueueSize = context->process->program->machines[context->instanceOf].maxQueueSize;

	PrtLockMutex(context->stateMachineLock);

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
	queue->events[tail].payload = PrtCloneValue(payload);
	queue->size++;
	queue->tailIndex = (tail + 1) % queue->eventsSize;

	//
	//Log
	//
	PrtLog(PRT_STEP_ENQUEUE, context);
	//
	// Now try to run the machine if its not running already
	//
	if (context->isRunning)
	{
		PrtUnlockMutex(context->stateMachineLock);
		return;
	}
	else
	{
		context->stateControl = PrtDequeue;
		context->returnTo = 0;
		PrtRunStateMachine(context, PRT_TRUE);
	}

	return;
}

void
PrtRaise(
	_Inout_ PRT_MACHINEINST_PRIV		*context,
	_In_ PRT_VALUE					*event,
	_In_ PRT_VALUE					*payload
)
{
	PrtAssert(!PrtIsSpecialEvent(event), "Raised event must not be null");
	PrtAssert(PrtIsSubtype(payload->type, PrtGetPayloadType(context, event)), "Actual payload type must be subtype of event payload type");
	
	context->lastOperation = RaiseStatement;

	PrtFreeValue(context->currentEvent.trigger);
	PrtFreeValue(context->currentEvent.payload);
	context->currentEvent.payload = PrtCloneValue(payload);
	context->currentEvent.trigger = PrtCloneValue(event);

	PrtLog(PRT_STEP_RAISE, context);
}

void
PrtPush(
	_Inout_ PRT_MACHINEINST_PRIV		*context,
	_In_ PRT_UINT32					stateIndex
)
{
	context->lastOperation = PushStatement;

	PrtPushState(context, stateIndex, PRT_TRUE);
}

void
PrtPushState(
_Inout_ PRT_MACHINEINST_PRIV		*context,
_In_	PRT_UINT32				stateIndex,
_In_	PRT_BOOLEAN				isPushStatement
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

	PrtAssert(length < PRT_MAX_CALL_DEPTH, "Call stack overflow");

	if (isPushStatement)
	{
		context->callStack.stateStack[length].currEvent = context->currentEvent;
		context->currentEvent.trigger = PrtMkEventValue(PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
		context->currentEvent.payload = PrtMkNullValue();
	}
	else
	{
		context->callStack.stateStack[length].currEvent.trigger = NULL;
		context->callStack.stateStack[length].currEvent.payload = NULL;
	}
	context->callStack.stateStack[length].stateIndex = context->currentState;
	context->callStack.stateStack[length].stateControl = context->stateControl;
	context->callStack.stateStack[length].returnTo = context->returnTo;
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

		if (isPushStatement)
		{
			context->inheritedDeferredSetCompact[i] = ~context->inheritedActionSetCompact[i]; // !a
		}
		else
		{
			context->inheritedDeferredSetCompact[i] |= currDef[i]; // D + d
			context->inheritedDeferredSetCompact[i] &= ~currActions[i]; // - a 
			context->inheritedDeferredSetCompact[i] &= ~currTransitions[i]; // - e
		}
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

	PrtFreeValue(context->currentEvent.trigger);
	PrtFreeValue(context->currentEvent.payload);
	context->currentEvent.trigger = PrtMkEventValue(PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
	context->currentEvent.payload = PrtMkNullValue();
	// Actual pop happens in PrtPopState; the exit function must be executed first.
	// The above assignment to context->currentEvent is only for the benefit of the exit function.
}

void
PrtPopState(
	_Inout_ PRT_MACHINEINST_PRIV		*context,
	_In_ PRT_BOOLEAN				isPopStatement
)
{
	PRT_UINT16 i;
	PRT_UINT16 packSize;
	PRT_UINT16 length;
	PRT_STATESTACK_INFO poppedState;
	i = 0;
	packSize = PrtGetPackSize(context);
	length = context->callStack.length;

	if (length == 0)
	{
		if (PrtPrimGetEvent(context->currentEvent.trigger) == PRT_SPECIAL_EVENT_HALT)
		{
			PrtHaltMachine(context);
		}
		else
		{
			PrtHandleError(PRT_STATUS_EVENT_UNHANDLED, context);
		}
		return;
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

	context->stateControl = poppedState.stateControl;
	context->returnTo = poppedState.returnTo;

	if (isPopStatement)
	{
		PrtFreeValue(context->currentEvent.trigger);
		PrtFreeValue(context->currentEvent.payload);
		context->currentEvent = poppedState.currEvent;
		PrtLog(PRT_STEP_POP, context);
	}
	else
	{
		// unhandled event
		if (context->stateControl != PrtDequeue)
		{
			PrtFreeValue(poppedState.currEvent.trigger);
			PrtFreeValue(poppedState.currEvent.payload);
		}
		PrtLog(PRT_STEP_UNHANDLED, context);
	}
}

FORCEINLINE
void
PrtRunExitFunction(
	_In_ PRT_MACHINEINST_PRIV			*context,
	_In_ PRT_UINT32						transIndex
)
{
	PRT_STATEDECL *stateDecl = PrtGetCurrentStateDecl(context);
	context->returnTo = 0;
	context->lastOperation = ReturnStatement;
	PrtLog(PRT_STEP_EXIT, context);
	PRT_UINT32 exitFunIndex = context->process->program->machines[context->instanceOf].states[context->currentState].exitFunIndex;
	PrtGetExitFunction(context)((PRT_MACHINEINST *)context, exitFunIndex, NULL); 
	if (transIndex < stateDecl->nTransitions)
	{
		PRT_UINT32 transFunIndex = stateDecl->transitions[transIndex].transFunIndex;
		PRT_DBG_ASSERT(transFunIndex != PRT_SPECIAL_ACTION_PUSH_OR_IGN, "Must be valid function index");
		context->process->program->machines[context->instanceOf].funs[transFunIndex].implementation((PRT_MACHINEINST *)context, transFunIndex, NULL);
	}
	PRT_DBG_ASSERT(context->lastOperation == ReturnStatement, "Exit function must terminate with a ReturnStatement");
}

void
PrtRunStateMachine(
	_Inout_ PRT_MACHINEINST_PRIV	    *context,
	_In_ PRT_BOOLEAN				doDequeue
)
{
	PRT_BOOLEAN lockHeld;
	PRT_DODECL *currActionDecl;
	PRT_UINT32 eventValue;

	// The state machine lock is held at entry iff an event was just enqueued.
	lockHeld = doDequeue;

	context->isRunning = PRT_TRUE;

	if (doDequeue)
	{
		goto DoDequeue;
	}
	else
	{
		goto DoEntryOrAction;
	}

DoEntryOrAction:
	PrtUpdateCurrentActionsSet(context);
	PrtUpdateCurrentDeferredSet(context);

	PRT_DBG_ASSERT(context->stateControl != PrtDequeue, "stateControl must not be PrtDequeue");
	if (context->stateControl == PrtStateEntry)
	{
		if (context->returnTo == 0)
			PrtLog(PRT_STEP_ENTRY, context);
		context->lastOperation = ReturnStatement;
		PRT_UINT32 entryFunIndex = context->process->program->machines[context->instanceOf].states[context->currentState].entryFunIndex;
		PrtGetEntryFunction(context)((PRT_MACHINEINST *)context, entryFunIndex, NULL);
	}
	else
	{
		PRT_DBG_ASSERT(context->stateControl == PrtStateAction, "stateControl must be PrtStateAction");
		currActionDecl = PrtGetAction(context);
		PRT_UINT32 doFunIndex = currActionDecl->doFunIndex;
		context->lastOperation = ReturnStatement;
		if (doFunIndex == PRT_SPECIAL_ACTION_PUSH_OR_IGN)
		{
			PrtLog(PRT_STEP_IGNORE, context);
		}
		else
		{
			if (context->returnTo == 0)
				PrtLog(PRT_STEP_DO, context);
			context->process->program->machines[context->instanceOf].funs[doFunIndex].implementation((PRT_MACHINEINST *)context, doFunIndex, NULL);
		}
	}
	switch (context->lastOperation)
	{
	case PopStatement:
		PrtRunExitFunction(context, PrtGetCurrentStateDecl(context)->nTransitions);
		PrtPopState(context, PRT_TRUE);
		if (context->stateControl == PrtDequeue)
		{
			goto DoDequeue;
		}
		else
		{
			goto DoEntryOrAction;
		}
		break;
	case RaiseStatement:
		context->stateControl = PrtDequeue;
		context->returnTo = 0;
		goto DoHandleEvent;
		break;
	case PushStatement:
		context->stateControl = PrtStateEntry;
		context->returnTo = 0;
		goto DoEntryOrAction;
		break;
	case ReturnStatement:
		context->stateControl = PrtDequeue;
		context->returnTo = 0;
		PrtFreeValue(context->currentEvent.trigger);
		PrtFreeValue(context->currentEvent.payload);
		context->currentEvent.trigger = NULL;
		context->currentEvent.payload = NULL;
		goto DoDequeue;
		break;
	default:
		PRT_DBG_ASSERT(0, "Unexpected case in switch");
		break;
	}

DoDequeue:
	if (!lockHeld)
	{
		lockHeld = PRT_TRUE;
		PrtLockMutex(context->stateMachineLock);
	}

	if (PrtDequeueEvent(context))
	{
		lockHeld = PRT_FALSE;
		PrtUnlockMutex(context->stateMachineLock);
		goto DoHandleEvent;
	}
	else
	{
		context->isRunning = PRT_FALSE;
		lockHeld = PRT_FALSE;
		PrtUnlockMutex(context->stateMachineLock);
		return;
	}

DoHandleEvent:
	eventValue = PrtPrimGetEvent(context->currentEvent.trigger);
	if (PrtIsPushTransition(context, eventValue))
	{
		PrtTakeTransition(context, eventValue);
		context->stateControl = PrtStateEntry;
		context->returnTo = 0;
		goto DoEntryOrAction;
	}
	else if (PrtIsTransitionPresent(context, eventValue))
	{
		PrtRunExitFunction(context, PrtFindTransition(context, eventValue));
		PrtTakeTransition(context, eventValue);
		context->stateControl = PrtStateEntry;
		context->returnTo = 0;
		goto DoEntryOrAction;
	}
	else if (PrtIsActionInstalled(eventValue, context->currentActionSetCompact))
	{
		context->stateControl = PrtStateAction;
		context->returnTo = 0;
		goto DoEntryOrAction;
	}
	else
	{
		PrtRunExitFunction(context, PrtGetCurrentStateDecl(context)->nTransitions);
		PrtPopState(context, PRT_FALSE);
		PrtUpdateCurrentActionsSet(context);
		PrtUpdateCurrentDeferredSet(context);
		goto DoHandleEvent;
	}

	PRT_DBG_ASSERT(PRT_FALSE, "Must not get here");
	return;
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
		PrtPushState(context, transTable[transIndex].destStateIndex, PRT_FALSE);
	}
	else 
	{
		context->currentState = transTable[transIndex].destStateIndex;
	}
}

PRT_BOOLEAN
PrtDequeueEvent(
	_Inout_ PRT_MACHINEINST_PRIV	*context
)
{
	PRT_UINT32 queueLength;
	PRT_EVENTQUEUE *queue;
	PRT_UINT32* deferPacked;
	PRT_UINT32 i, head;

	PRT_DBG_ASSERT(context->currentEvent.trigger == NULL && context->currentEvent.payload == NULL, "Precondition of PrtDequeue failed");

	queue = &context->eventQueue;
	queueLength = queue->eventsSize;
	deferPacked = PrtGetDeferredPacked(context, context->currentState);
	head = queue->headIndex;

	PRT_DBG_ASSERT(queue->size <= queueLength, "Check Failed");
	PRT_DBG_ASSERT(queue->size >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->headIndex >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->tailIndex >= 0, "Check Failed");

	//
	// Find the element to dequeue
	//
	for (i = 0; i < queue->size; i++) {
		PRT_UINT32 index = (head + i) % queueLength;
		PRT_EVENT e = queue->events[index];
		if (!PrtIsEventDeferred(PrtPrimGetEvent(e.trigger), context->currentDeferredSetCompact)) {
			context->currentEvent = e;
			break;
		}
	}

	//
	// Check if not found
	//
	if (i == queue->size) {
		if (PrtStateHasDefaultTransitionOrAction(context))
		{
			context->currentEvent.trigger = PrtMkEventValue(PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
			context->currentEvent.payload = PrtMkNullValue();
			return PRT_TRUE;
		}
		else 
		{
			return PRT_FALSE;
		}
	}

	//
	// Collapse the event queue on the removed event
	// by moving the previous elements forward.
	//
	for (; i > 0; i--) {
		INT index = (head + i) % queueLength;
		INT prev = (index - 1 + queueLength) % queueLength;
		queue->events[index] = queue->events[prev];
	}

	//
	// Adjust the queue size
	//
	queue->headIndex = (queue->headIndex + 1) % queueLength;
	queue->size--;

	PRT_DBG_ASSERT(queue->size <= queueLength, "Check Failed");
	PRT_DBG_ASSERT(queue->size >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->headIndex >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->tailIndex >= 0, "Check Failed");

	//
	//Log
	//
	PrtLog(PRT_STEP_DEQUEUE, context);
	return PRT_TRUE;
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
	ULONG32 nEvents = context->process->program->nEvents;
	return (UINT16)(((nEvents == 0) || (nEvents % (sizeof(PRT_UINT32) * 8) != 0))
		? (1 + (nEvents / (sizeof(PRT_UINT32) * 8)))
		: (nEvents / (sizeof(PRT_UINT32) * 8)));
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
	_In_ PRT_MACHINEINST_PRIV		*context
)
{
	PRT_UINT32 currEvent = PrtPrimGetEvent(context->currentEvent.trigger);
	PRT_BOOLEAN isActionInstalled = PRT_FALSE;
	PRT_UINT32 i, nActions;
	PRT_STATESTACK currStack;
	PRT_STATEDECL *stateTable;
	PRT_UINT32 topOfStackState;
	PRT_STATEDECL *stateDecl;
	PRT_DODECL *actionDecl = NULL;

	//check if action is defined for the current state
	isActionInstalled = PrtIsActionInstalled(currEvent, PrtGetActionsPacked(context, context->currentState));
	if (isActionInstalled)
	{
		//
		// get action function
		//
		stateDecl = PrtGetCurrentStateDecl(context);
		nActions = stateDecl->nDos;
		for (i = 0; i < nActions; i++)
		{
			if (stateDecl->dos[i].triggerEventIndex == currEvent)
			{
				actionDecl = &stateDecl->dos[i];
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
			for (i = 0; i < nActions; i++)
			{
				if (stateTable[topOfStackState].dos[i].triggerEventIndex == currEvent)
				{
					actionDecl = &stateTable[topOfStackState].dos[i];
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
	return (PrtIsNullValue(event) || PrtPrimGetEvent(event) == PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
}

FORCEINLINE
PRT_BOOLEAN
PrtIsEventDeferred(
	_In_ PRT_UINT32		eventIndex,
	_In_ PRT_UINT32*	defSet
)
{
	return (defSet[eventIndex / (sizeof(PRT_UINT32)* 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32)* 8)))) != 0;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsActionInstalled(
	_In_ PRT_UINT32		eventIndex,
	_In_ PRT_UINT32*		actionSet
)
{
	return (actionSet[eventIndex / (sizeof(PRT_UINT32)* 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32)* 8)))) != 0;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsTransitionPresent(
	_In_ PRT_MACHINEINST_PRIV	*context,
	_In_ PRT_UINT32				eventIndex
)
{
	PRT_UINT32* transitionsPacked = PrtGetTransitionsPacked(context, context->currentState);
	return (transitionsPacked[eventIndex / (sizeof(PRT_UINT32)* 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32)* 8)))) != 0;
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
	for (i = 0; i<size; i++)
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
	PrtCleanupMachine(context);
	context->isHalted = PRT_TRUE;
	PrtLog(PRT_STEP_HALT, context);
}

void
PrtCleanupMachine(
	_Inout_ PRT_MACHINEINST_PRIV			*context
)
{
	if (context->eventQueue.events != NULL)
	{
		PRT_EVENT *queue = context->eventQueue.events;
		PRT_UINT32 head = context->eventQueue.headIndex;
		PRT_UINT32 tail = context->eventQueue.tailIndex;
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
	
	for (int i = 0; i < context->callStack.length; i++)
	{
		PRT_STATESTACK_INFO *info = &context->callStack.stateStack[i];
		if (info->currEvent.payload != NULL)
		{
			PrtFreeValue(info->currEvent.payload);
		}
		if (info->currEvent.trigger != NULL)
		{
			PrtFreeValue(info->currEvent.trigger);
		}
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
		UINT i;
		PRT_MACHINEDECL *mdecl = &(context->process->program->machines[context->instanceOf]);

		for (i = 0; i < mdecl->nVars; i++) {
			PrtFreeValue(context->varValues[i]);
		}
		PrtFree(context->varValues);
	}

	if (context->stateMachineLock != NULL)
	{
		PrtDestroyMutex(context->stateMachineLock);
	}

	if (context->extContext != NULL)
		context->process->program->machines[context->instanceOf].extDtorFun((PRT_MACHINEINST *)context);
	PrtFreeValue(context->id);
	if (context->currentEvent.payload != NULL)
	{
		PrtFreeValue(context->currentEvent.payload);
	}
	if (context->currentEvent.trigger != NULL)
	{
		PrtFreeValue(context->currentEvent.trigger);
	}
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

void
PrtLog(
	_In_ PRT_STEP step,
	_In_ PRT_MACHINEINST_PRIV *context
)
{
	((PRT_PROCESS_PRIV *)context->process)->logHandler(step, (PRT_MACHINEINST *)context);
}

