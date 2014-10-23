/*
Outstanding issues:
1. Default transitions and actions
2. Transition functions
3. Push can return with a raised event
4. Polling without deadlock
*/

#include "PrtExecution.h"

/*********************************************************************************

Public Functions

*********************************************************************************/
PRT_PROCESS * PrtStartProcess(
	_In_ PRT_GUID guid,
	_In_ PRT_PROGRAMDECL *program,
	_In_ PRT_ERROR_FUN errorFun,
	_In_ PRT_LOG_FUN logFun
	)
{
	PRT_PROCESS_PRIV *process;

	process = (PRT_PROCESS_PRIV *)PrtMalloc(sizeof(PRT_PROCESS_PRIV));
	process->process.guid = guid;
	process->process.program = program;
	process->errorHandler = errorFun;
	process->logHandler = logFun;
	process->lock = PrtCreateMutex();
	process->machineCount = 0;
	process->machines = NULL;
	process->numMachines = 0;
	return &process->process;
}

void PrtStopProcess(_Inout_ PRT_PROCESS* process)
{

}

PRT_SM_CONTEXT * PrtMkMachine(
__in  PRT_PROCESS				*process,
__in  PRT_UINT32				instanceOf,
__in  PRT_VALUE					*payload
)
{
	PRT_UINT32 packSize;
	PRT_UINT32 nVars;
	PRT_UINT8 eQSize;
	PRT_SM_CONTEXT_PRIV *context;
	PRT_UINT32 i;
	PRT_SM_CONTEXT *publicContext;

	nVars = process->program->machines[instanceOf].nVars;
	eQSize = PRT_QUEUE_LEN_DEFAULT;

	//
	// Allocate memory for state machine context
	//
	context = (PRT_SM_CONTEXT_PRIV*)PrtMalloc(sizeof(PRT_SM_CONTEXT_PRIV));

	//
	// Add it to the array of machines in the process
	//
	PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV *)context->context.process;
	PRT_UINT32 numMachines = privateProcess->numMachines;
	PRT_UINT32 machineCount = privateProcess->machineCount;
	PRT_SM_CONTEXT **machines = privateProcess->machines;
	if (machineCount == numMachines) {
		PRT_SM_CONTEXT **newMachines = (PRT_SM_CONTEXT **)PrtCalloc(2 * machineCount, sizeof(PRT_SM_CONTEXT_PRIV *));
		for (PRT_UINT32 i = 0; i < machineCount; i++)
		{
			newMachines[i] = machines[i];
		}
		machines = newMachines;
		privateProcess->machines = newMachines;
		privateProcess->machineCount = 2 * machineCount;
	}
	machines[numMachines] = (PRT_SM_CONTEXT *)context;
	privateProcess->numMachines++;

	publicContext = &context->context;

	//
	// Initialize Machine Identity
	//
	publicContext->process = process;
	publicContext->instanceOf = instanceOf;
	PRT_MACHINEID id;
	id.machineId = privateProcess->numMachines; // index begins with 1 since 0 is reserved
	id.processId = process->guid;
	publicContext->id = PrtMkMachineValue(id);

	//
	// Initialize Machine Internal Variables
	//
	context->currentState = process->program->machines[context->context.instanceOf].initStateIndex;
	context->isRunning = FALSE;
	context->isHalted = FALSE;
	context->lastOperation = ReturnStatement;

	context->trigger.event = PrtMkNullValue();
	context->trigger.payload = PrtCloneValue(payload);
	context->stateExecFun = PrtStateEntry;
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
	context->eventQueue.events = (PRT_TRIGGER*)PrtCalloc(eQSize, sizeof(PRT_TRIGGER));
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
	context->inheritedActionsSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));
	context->currentActionsSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));

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
	process->program->machines[publicContext->instanceOf].extCtor(publicContext, payload);

	//
	// Run the state machine
	//
	PrtRunStateMachine(context, FALSE);

	return publicContext;
}

PRT_SM_CONTEXT * PrtMkModel(
	__in  PRT_PROCESS				*process,
	__in  PRT_UINT32				instanceOf,
	__in  PRT_VALUE					*payload
	)
{
	PRT_SM_CONTEXT_PRIV *context;
	PRT_SM_CONTEXT *publicContext;

	//
	// Allocate memory for state machine context
	//
	context = (PRT_SM_CONTEXT_PRIV*)PrtMalloc(sizeof(PRT_SM_CONTEXT_PRIV));

	//
	// Add it to the array of machines in the process
	//
	PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV *)context->context.process;
	PRT_UINT32 numMachines = privateProcess->numMachines;
	PRT_UINT32 machineCount = privateProcess->machineCount;
	PRT_SM_CONTEXT **machines = privateProcess->machines;
	if (machineCount == numMachines) {
		PRT_SM_CONTEXT **newMachines = (PRT_SM_CONTEXT **)PrtCalloc(2 * machineCount, sizeof(PRT_SM_CONTEXT_PRIV *));
		for (PRT_UINT32 i = 0; i < machineCount; i++)
		{
			newMachines[i] = machines[i];
		}
		machines = newMachines;
		privateProcess->machines = newMachines;
		privateProcess->machineCount = 2 * machineCount;
	}
	machines[numMachines] = (PRT_SM_CONTEXT *)context;
	privateProcess->numMachines++;

	publicContext = &context->context;

	//
	// Initialize Machine Identity
	//
	publicContext->process = process;
	publicContext->instanceOf = instanceOf;
	PRT_MACHINEID id;
	id.machineId = privateProcess->numMachines; // index begins with 1 since 0 is reserved
	id.processId = process->guid;
	publicContext->id = PrtMkModelValue(id);

	//
	// Initialize Machine Internal Variables
	//
	context->currentState = process->program->machines[context->context.instanceOf].initStateIndex;
	context->isRunning = FALSE;
	context->isHalted = FALSE;
	context->lastOperation = ReturnStatement;

	context->trigger.event = PrtMkNullValue();
	context->trigger.payload = PrtCloneValue(payload);
	context->returnTo = 0;
	context->stateExecFun = PrtStateEntry;

	//
	// Allocate memory for local variables and initialize them
	//
	context->varValues = NULL;

	//
	// Machine Call State Depth
	//
	context->callStack.length = 0;

	//
	// Initialize event queue
	//
	context->eventQueue.eventsSize = 0;
	context->eventQueue.events = NULL;
	context->eventQueue.headIndex = 0;
	context->eventQueue.tailIndex = 0;
	context->eventQueue.size = 0;

	//
	// Initialize Inherited Deferred Set 
	//
	context->inheritedDeferredSetCompact = NULL;

	//
	// Initialize the current deferred set
	//
	context->currentDeferredSetCompact = NULL;

	//
	// Initialize actions
	//
	context->inheritedActionsSetCompact = NULL;
	context->currentActionsSetCompact = NULL;

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
	process->program->machines[publicContext->instanceOf].extCtor(publicContext, payload);

	return publicContext;
}

PRT_BOOLEAN AreGuidsEqual(PRT_GUID guid1, PRT_GUID guid2)
{
	return guid1.data1 == guid2.data1 && guid1.data2 == guid2.data2 && guid1.data3 == guid2.data3 && guid1.data4 == guid2.data4;
}

PRT_SM_CONTEXT * PrtGetMachine(
	_In_ PRT_PROCESS *process,
	_In_ PRT_VALUE *id)
{
	PRT_MACHINEID *machineId;
	PRT_PROCESS_PRIV *privateProcess;
	PrtAssert(id->discriminator == PRT_VALKIND_MID, "id is not legal PRT_MACHINEID");
	machineId = id->valueUnion.mid;
	PrtAssert(AreGuidsEqual(process->guid, machineId->processId), "id does not belong to process");
	privateProcess = (PRT_PROCESS_PRIV *)process;
	PrtAssert(0 < machineId->machineId && machineId->machineId <= privateProcess->numMachines, "id out of bounds");
	return privateProcess->machines[machineId->machineId - 1];
}

PRT_BOOLEAN
PrtIsEventMaxInstanceExceeded(
__in PRT_EVENTQUEUE			*queue,
__in PRT_UINT32				eventIndex,
__in PRT_UINT32				maxInstances
)
{
	//
	// Declarations
	//
	PRT_UINT32 queueSize;
	PRT_UINT32 head;
	PRT_UINT32 tail;
	PRT_UINT16 currMaxInstance;
	PRT_BOOLEAN isMaxInstancesExceeded;
	//
	// Code
	//
	queueSize = queue->eventsSize;
	head = queue->headIndex;
	tail = queue->tailIndex;
	currMaxInstance = 0;
	isMaxInstancesExceeded = FALSE;
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
			if (PrtPrimGetEvent(queue->events[head].event) == eventIndex)
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
		if (PrtPrimGetEvent(queue->events[head].event) == eventIndex)
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
		isMaxInstancesExceeded = TRUE;
	}

	return isMaxInstancesExceeded;
}

void
PrtEnqueueEvent(
__in PRT_SM_CONTEXT_PRIV		*context,
__in PRT_VALUE					*event,
__in PRT_VALUE					*payload
)
{
	PRT_EVENTQUEUE *queue;
	PRT_UINT32 tail;
	PRT_UINT32 eventMaxInstances;
	PRT_UINT32 maxQueueSize;
	PRT_UINT32 eventIndex;
	
	//check that the enqueued message is event type
	PrtAssert(event->type->typeKind == PRT_KIND_EVENT, "Parameter event is not of type EVENT");
	PrtAssert(PrtIsSubtype(payload->type, PrtGetPayloadType(context, event)), "Payload type mismatch");
	PrtAssert(!PrtIsSpecialEvent(event), "Enqueued event cannot be null");
	
	//check if the machine is still alive or halted
	if (context->isHalted)
	{
		//drop the event silently
		return;
	}

	eventIndex = PrtPrimGetEvent(event);
	eventMaxInstances = context->context.process->program->events[eventIndex].eventMaxInstances;
	maxQueueSize = context->context.process->program->machines[context->context.instanceOf].maxQueueSize;

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
	queue->events[tail].event = PrtCloneValue(event);
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
		PrtRunStateMachine(context, TRUE);
	}

	return;
}


/*********************************************************************************

Protected Functions

*********************************************************************************/

void
PrtPop(
__inout PRT_SM_CONTEXT_PRIV		*context
)
{
	//
	// Set operation performed to PopStatement
	//
	context->lastOperation = PopStatement;

	//free the previous event and payload values
	PrtFreeValue(context->trigger.event);
	PrtFreeValue(context->trigger.payload);

	// Set Trigger to NULL indicating that the last statement executed was Pop 
	// TRIGGER access in Exit function after Pop is evaluated to NULL
	context->trigger.event = PrtMkNullValue();
	context->trigger.payload = PrtMkNullValue();

}

void
PrtRaise(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_VALUE	*event,
__in PRT_VALUE	*payload
)
{
	//check that the enqueued message is event type
	PrtAssert(event->type->typeKind == PRT_KIND_EVENT, "Parameter event is not of type EVENT");
	PrtAssert(PrtIsSubtype(payload->type, PrtGetPayloadType(context, event)), "Payload type mismatch");
	PrtAssert(!PrtIsSpecialEvent(event), "Raised event cannot be null");
	//
	// Set operation to raiseStatement
	//
	context->lastOperation = RaiseStatement;

	// Free the memory associated with trigger
	PrtFreeValue(context->trigger.event);
	PrtFreeValue(context->trigger.payload);

	context->trigger.payload = PrtCloneValue(payload);
	context->trigger.event = PrtCloneValue(event);

	//
	//Log
	//
	PrtLog(PRT_STEP_RAISE, context);
}

void
PrtPush(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_UINT32				stateIndex
)
{
	//
	// Push current state on top of the stack
	//
	PrtPushState(context, TRUE);

	//
	// Set Trigger to NULL after a push statement
	//
	PrtFreeValue(context->trigger.event);
	PrtFreeValue(context->trigger.payload);

	context->trigger.event = PrtMkNullValue();
	context->trigger.payload = PrtMkNullValue();
	//
	// Change current state
	//
	context->currentState = stateIndex;
	//
	// Last operation set to push statement
	//
	context->lastOperation = PushStatement;

	//
	//Log
	//
	PrtLog(PRT_STEP_PUSH, context);

	return;
}

FORCEINLINE
PRT_BOOLEAN
PrtStateHasDefaultTransition(
__in PRT_SM_CONTEXT_PRIV			*context
)
{
	return (context->context.process->program->eventSets[PrtGetCurrentStateDecl(context).transSetIndex].packedEvents[0] & 0x1) == 1;
}

FORCEINLINE
PRT_STATEDECL
PrtGetCurrentStateDecl(
__in PRT_SM_CONTEXT_PRIV			*context
)
{
	return context->context.process->program->machines[context->context.instanceOf].states[context->currentState];
}

FORCEINLINE
void
PrtRunExitFunction(
__in PRT_SM_CONTEXT_PRIV			*context
)
{
	context->returnTo = 0;
	context->lastOperation = ReturnStatement;
	PRT_UINT32 exitFunIndex = context->context.process->program->machines[context->context.instanceOf].states[context->currentState].exitFunIndex;
	PrtLog(PRT_STEP_EXIT, context);
	PrtGetExitFunction(context)(&context->context, exitFunIndex, NULL);
	PRT_DBG_ASSERT(context->lastOperation == ReturnStatement, "Exit function must terminate with a ReturnStatement");
}

void
PrtRunStateMachine(
__inout PRT_SM_CONTEXT_PRIV	    *context,
__in PRT_BOOLEAN	doDequeue
)
{
	PRT_BOOLEAN lockHeld;
	PRT_DODECL *currActionDecl;
	PRT_UINT32 eventValue;

	// The state machine lock is held at entry iff an event was just enqueued.
	lockHeld = doDequeue;

	context->isRunning = TRUE;

	if (doDequeue)
	{
		goto DoDequeue;
	}

DoEntryOrAction:
	PrtUpdateCurrentActionsSet(context);
	PrtUpdateCurrentDeferredSet(context);

	PRT_DBG_ASSERT(context->stateExecFun != PrtDequeue, "stateExecFun must not be PrtDequeue");
	if (context->stateExecFun == PrtStateEntry)
	{
		//
		// Log
		//
		if (context->returnTo == 0)
			PrtLog(PRT_STEP_MOVE, context);
		//
		// Initialize context before executing entry function
		//
		context->lastOperation = ReturnStatement;
		//
		// Execute the Entry function
		//
		PRT_UINT32 entryFunIndex = context->context.process->program->machines[context->context.instanceOf].states[context->currentState].entryFunIndex;
		PrtGetEntryFunction(context)(&context->context, entryFunIndex, NULL);

		switch (context->lastOperation)
		{
		case PopStatement:
			goto DoPop;
			break;
		case RaiseStatement:
			eventValue = PrtPrimGetEvent(context->trigger.event);
			if (PrtIsTransitionPresent(eventValue, context))
			{
				if (!PrtIsPushTransition(context, eventValue))
				{
					PrtRunExitFunction(context);
				}
				goto DoTakeTransition;
			}
			else if (PrtIsActionInstalled(eventValue, context->currentActionsSetCompact))
			{
				context->stateExecFun = PrtStateAction;
				context->returnTo = 0;
				goto DoEntryOrAction;
			}
			else
			{
				//
				// Unhandled raised event
				//
				PrtRunExitFunction(context);
				goto DoTakeTransition;
			}
			break;
		case PushStatement:
			context->stateExecFun = PrtStateEntry;
			context->returnTo = 0;
			goto DoEntryOrAction;
			break;
		case ReturnStatement:
			goto DoDequeue;
			break;
		default:
			PRT_DBG_ASSERT(0, "Unexpected case in switch");
			break;
		}
	}
	else 
	{
		PRT_DBG_ASSERT(context->stateExecFun == PrtStateAction, "stateExecFun must be PrtStateAction");
		//
		// Get the current action decl
		//
		currActionDecl = PrtGetAction(context);
		//
		// Log
		//
		if (context->returnTo == 0)
			PrtLog(PRT_STEP_DO, context);
		//
		// Initialize context before executing entry function
		//
		context->lastOperation = ReturnStatement;
		//
		// Execute the Action function
		//
		context->context.process->program->machines[context->context.instanceOf].funs[currActionDecl->doFunIndex].implementation(&context->context, currActionDecl->doFunIndex, NULL);

		switch (context->lastOperation)
		{
		case PopStatement:
			goto DoPop;
			break;
		case RaiseStatement:
			eventValue = PrtPrimGetEvent(context->trigger.event);
			if (PrtIsTransitionPresent(eventValue, context))
			{
				if (!PrtIsPushTransition(context, eventValue))
				{
					PrtRunExitFunction(context);
				}
				goto DoTakeTransition;
			}
			else if (PrtIsActionInstalled(eventValue, context->currentActionsSetCompact))
			{
				context->stateExecFun = PrtStateAction;
				context->returnTo = 0;
				goto DoEntryOrAction;
			}
			else
			{
				//
				// Unhandled raised event
				//
				goto DoTakeTransition;
			}
			break;
		case PushStatement:
			context->stateExecFun = PrtStateEntry;
			context->returnTo = 0;
			goto DoEntryOrAction;
			break;
		case ReturnStatement:
			goto DoDequeue;
			break;
		default:
			PRT_DBG_ASSERT(0, "Unexpected case in switch");
			break;
		}
	}

DoDequeue:
	if (!lockHeld)
	{
		lockHeld = TRUE;
		PrtLockMutex(context->stateMachineLock);
	}

	if (PrtDequeueEvent(context))
	{
		// release Lock
		lockHeld = FALSE;
		PrtUnlockMutex(context->stateMachineLock);
		if (PrtIsPushTransition(context, PrtPrimGetEvent(context->trigger.event)))
		{
			goto DoTakeTransition;
		}
		else if (PrtIsTransitionPresent(PrtPrimGetEvent(context->trigger.event), context))
		{
			PrtRunExitFunction(context);
			goto DoTakeTransition;
		}
		else if (PrtIsActionInstalled(PrtPrimGetEvent(context->trigger.event), context->currentActionsSetCompact))
		{
			context->stateExecFun = PrtStateAction;
			context->returnTo = 0;
			goto DoEntryOrAction;
		}
		else
		{
			PrtRunExitFunction(context);
			goto DoTakeTransition;
		}
	}
	else if (PrtStateHasDefaultTransition(context))
	{
		// release lock
		lockHeld = FALSE;
		PrtUnlockMutex(context->stateMachineLock);
		PrtRunExitFunction(context);
		goto DoTakeTransition;
	}
	else
	{
		context->isRunning = FALSE;
		// release Lock
		lockHeld = FALSE;
		PrtUnlockMutex(context->stateMachineLock);
		return;
	}

DoPop:
	PrtRunExitFunction(context);
	PrtPopState(context, PRT_TRUE);
	PrtLog(PRT_STEP_POP, context);
	if (context->stateExecFun == PrtDequeue)
	{
		//
		// Pop returned to a push edge; we should return to dequeue
		//
		goto DoDequeue;
	}
	else
	{
		//
		// Pop returns to end of a push statement; we should execute the next statement in entry/action function
		//
		goto DoEntryOrAction;
	}

DoTakeTransition:
	if (PrtPrimGetEvent(context->trigger.event) == PRT_SPECIAL_EVENT_DEFAULT_OR_NULL)
	{
		PrtTakeDefaultTransition(context);
		goto DoEntryOrAction;
	}
	else
	{
		PrtTakeTransition(context, PrtPrimGetEvent(context->trigger.event));
		if (context->isHalted)
			return;
		goto DoEntryOrAction;
	}

	PRT_DBG_ASSERT(PRT_FALSE, "Cannot get here");
	return;
}

void
PrtTakeDefaultTransition(
__inout PRT_SM_CONTEXT_PRIV		*context
)
{
	ULONG i;
	PRT_UINT32 nTransitions;
	PRT_TRANSDECL* transTable;
	
	transTable = PrtGetTransTable(context, context->currentState, &nTransitions);

	for (i = 0; i < nTransitions; ++i)
	{
		//check if transition is After
		if (transTable[i].triggerEventIndex == PRT_SPECIAL_EVENT_DEFAULT_OR_NULL)
		{
			//check if its a push transition
			if (transTable[i].isPush)
			{
				context->stateExecFun = PrtDequeue;
				context->returnTo = 0;
				PrtPushState(context, PRT_FALSE);
			}

			//update the state
			context->currentState = transTable[i].destStateIndex;
			context->returnTo = 0;
			context->stateExecFun = PrtStateEntry;
			return;
		}
	}

	return;
}

void
PrtTakeTransition(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_UINT32				eventIndex
)
{
	PRT_UINT32 i;
	PRT_UINT32 nTransitions;
	PRT_TRANSDECL* transTable;

	transTable = PrtGetTransTable(context, context->currentState, &nTransitions);

	for (i = 0; i < nTransitions; ++i)
	{
		if (transTable[i].triggerEventIndex == eventIndex)
		{
			if (transTable[i].isPush)
			{
				context->stateExecFun = PrtDequeue;
				context->returnTo = 0;
				PrtPushState(context, PRT_FALSE);
				PrtLog(PRT_STEP_PUSH, context);
			}

			context->currentState = transTable[i].destStateIndex;
			context->stateExecFun = PrtStateEntry;
			context->returnTo = 0;
			return;
		}
	}
	if (context->callStack.length > 0)
	{
		PrtPopState(context, PRT_FALSE);
		PrtLog(PRT_STEP_UNHANDLED, context);
	}
	else if (PrtPrimGetEvent(context->trigger.event) == PRT_SPECIAL_EVENT_HALT)
	{
		PrtHaltMachine(context);
	}
	else
	{
		PrtHandleError(PRT_STATUS_EVENT_UNHANDLED, context);
	}

	return;
}

void
PrtHaltMachine(
__inout PRT_SM_CONTEXT_PRIV			*context
)
{
	if (context->currentActionsSetCompact != NULL)
	{
		PrtFree(context->currentActionsSetCompact);
	}

	if (context->currentDeferredSetCompact != NULL)
	{
		PrtFree(context->currentDeferredSetCompact);
	}

	if (context->eventQueue.events != NULL)
	{
		PrtFree(context->eventQueue.events);
	}

	if (context->context.extContext != NULL)
	{
		PrtFree(context->context.extContext);
	}

	if (context->inheritedActionsSetCompact != NULL)
	{
		PrtFree(context->inheritedActionsSetCompact);
	}

	if (context->inheritedDeferredSetCompact != NULL)
	{
		PrtFree(context->inheritedDeferredSetCompact);
	}

	if (context->varValues != NULL)
	{
		UINT i;
		PRT_MACHINEDECL *mdecl = &(context->context.process->program->machines[context->context.instanceOf]);

		for (i = 0; i < mdecl->nVars; i++) {
			PrtFreeValue(context->varValues[i]);
		}
		PrtFree(context->varValues);
	}

	context->isHalted = TRUE;

	PrtLog(PRT_STEP_HALT, context);
}

void
PrtPushState(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in	PRT_BOOLEAN			isPushStatement
)
{
	//
	// Declarations
	//
	PRT_UINT16 i;
	PRT_UINT16 packSize;
	PRT_UINT16 length;
	PRT_UINT32 *currDef;
	PRT_UINT32 *currActions;
	PRT_UINT32 *currTransitions;

	//
	// Code
	//
	packSize = PrtGetPackSize(context);
	length = context->callStack.length;
	currDef = PrtGetDeferredPacked(context, context->currentState);
	currActions = PrtGetActionsPacked(context, context->currentState);
	currTransitions = PrtGetTransitionsPacked(context, context->currentState);

	PrtAssert(length < PRT_MAX_CALL_DEPTH, "Call Stack Overflow");
	//
	// push <state, trigger, arg, ReturnTo, StateExecFun, defSet, ActSet>
	//
	context->callStack.statesStack[length].stateIndex = context->currentState;
	context->callStack.statesStack[length].trigger.event = context->trigger.event;
	context->callStack.statesStack[length].trigger.payload = context->trigger.payload;
	context->callStack.statesStack[length].returnTo = context->returnTo;
	context->callStack.statesStack[length].stateExecFun = context->stateExecFun;
	context->callStack.statesStack[length].inheritedDefSetCompact = (PRT_UINT32*)PrtClonePackedSet(context->inheritedDeferredSetCompact, packSize);
	context->callStack.statesStack[length].inheritedActSetCompact = (PRT_UINT32*)PrtClonePackedSet(context->inheritedActionsSetCompact, packSize);

	context->callStack.length = length + 1;

	// Update the defered set inherited by state-machine
	// D = (D + d) - a - e
	for (i = 0; i < packSize; ++i)
	{

		// Update the actions set inherited by state-machine
		// A = (A - d) + a - e
		context->inheritedActionsSetCompact[i] &= ~currDef[i]; // A - d
		context->inheritedActionsSetCompact[i] |= currActions[i]; // + a
		context->inheritedActionsSetCompact[i] &= ~currTransitions[i]; // -e

		if (isPushStatement)
		{
			context->inheritedDeferredSetCompact[i] = ~context->inheritedActionsSetCompact[i]; // !a
		}
		else
		{
			context->inheritedDeferredSetCompact[i] |= currDef[i]; // D + d
			context->inheritedDeferredSetCompact[i] &= ~currActions[i]; // - a 
			context->inheritedDeferredSetCompact[i] &= ~currTransitions[i]; // - e
		}
	}
}

void
PrtPopState(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_BOOLEAN			restoreTrigger
)
{
	PRT_UINT16 i;
	PRT_UINT16 packSize;
	PRT_UINT16 length;
	PRT_STACKSTATE_INFO poppedState;
	i = 0;
	packSize = PrtGetPackSize(context);
	length = context->callStack.length;

	PrtAssert(length > 0, "PopState Called on Empty Stack");

	context->callStack.length = length - 1;
	poppedState = context->callStack.statesStack[length - 1];
	context->currentState = poppedState.stateIndex;

	//
	// Restore the Deferred Set and Actions Set
	//
	for (i = 0; i < packSize; i++)
	{
		context->inheritedDeferredSetCompact[i] = poppedState.inheritedDefSetCompact[i];
		context->inheritedActionsSetCompact[i] = poppedState.inheritedActSetCompact[i];
	}
	//
	// Free the allocated memory for def and act state
	//
	PrtFree(poppedState.inheritedDefSetCompact);
	PrtFree(poppedState.inheritedActSetCompact);

	//
	// Restore the trigger value
	//
	if (restoreTrigger)
	{
		PrtFreeValue(context->trigger.event);
		PrtFreeValue(context->trigger.payload);

		context->trigger.event = poppedState.trigger.event;
		context->trigger.payload = poppedState.trigger.payload;
		context->returnTo = poppedState.returnTo;
		context->stateExecFun = poppedState.stateExecFun;
	}
	
	return;
}

PRT_BOOLEAN
PrtDequeueEvent(
__inout PRT_SM_CONTEXT_PRIV	*context
)
{
	PRT_UINT32 queueLength;
	PRT_EVENTQUEUE *queue;
	PRT_UINT32* deferPacked;
	PRT_UINT32 i, head;

	//
	// Free old trigger
	//
	PrtFreeValue(context->trigger.event);
	PrtFreeValue(context->trigger.payload);

	queue = &context->eventQueue;
	queueLength = queue->eventsSize;
	deferPacked = PrtGetDeferredPacked(context, context->currentState);
	head = queue->headIndex;

	PRT_DBG_ASSERT(queue->size <= queueLength, "Check Failed");
	PRT_DBG_ASSERT(queue->size >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->headIndex >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->tailIndex >= 0, "Check Failed");

	if (PrtIsQueueEmpty(queue)) {
		context->trigger.event = PrtMkEventValue(PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
		context->trigger.payload = PrtMkNullValue();
		return PRT_FALSE;
	}

	//
	// Find the element to dequeue
	//
	for (i = 0; i < queue->size; i++) {
		PRT_UINT32 index = (head + i) % queueLength;
		PRT_TRIGGER e = queue->events[index];
		if (!PrtIsEventDeferred(PrtPrimGetEvent(e.event), context->currentDeferredSetCompact)) {
			context->trigger = e;
			break;
		}
	}

	//
	// Check if not found
	//
	if (i == queue->size) {
		context->trigger.event = PrtMkEventValue(PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
		context->trigger.payload = PrtMkNullValue();
		return PRT_FALSE;
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
PRT_BOOLEAN
PrtIsSpecialEvent(
PRT_VALUE * event
)
{
	return (PrtIsNullValue(event) || PrtPrimGetEvent(event) == PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
}

FORCEINLINE
PRT_TYPE*
PrtGetPayloadType(
PRT_SM_CONTEXT_PRIV *context,
PRT_VALUE	  *event
)
{
	return context->context.process->program->events[PrtPrimGetEvent(event)].type;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsEventDeferred(
__in PRT_UINT32		eventIndex,
__in PRT_UINT32*		defSet
)
{
	//
	// Declarations
	//
	PRT_BOOLEAN isDeferred;

	//
	// Code
	//

	isDeferred = FALSE;
	if
		(
		((defSet[eventIndex / (sizeof(PRT_UINT32)* 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32)* 8)))) != 0)
		)
	{
		isDeferred = TRUE;
	}

	return isDeferred;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsActionInstalled(
__in PRT_UINT32		eventIndex,
__in PRT_UINT32*		actionSet
)
{
	//
	// Declarations
	//
	PRT_BOOLEAN isActionInstalled;

	//
	// Code
	//

	isActionInstalled = FALSE;
	if
		(
		((actionSet[eventIndex / (sizeof(PRT_UINT32)* 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32)* 8)))) != 0)
		)
	{
		isActionInstalled = TRUE;
	}

	return isActionInstalled;
}

FORCEINLINE
PRT_UINT16
PrtGetPackSize(
__in PRT_SM_CONTEXT_PRIV			*context
)
{
	ULONG32 nEvents = context->context.process->program->nEvents;
	return (UINT16)(((nEvents == 0) || (nEvents % (sizeof(PRT_UINT32) * 8) != 0))
		? (1 + (nEvents / (sizeof(PRT_UINT32) * 8)))
		: (nEvents / (sizeof(PRT_UINT32) * 8)));
}

FORCEINLINE
PRT_BOOLEAN
PrtIsQueueEmpty(
__in PRT_EVENTQUEUE		*queue
)
{
	return queue->size == 0;
}

FORCEINLINE
PRT_SM_FUN
PrtGetEntryFunction(
__in PRT_SM_CONTEXT_PRIV		*context
)
{
	PRT_UINT32 entryFunIndex = context->context.process->program->machines[context->context.instanceOf].states[context->currentState].entryFunIndex;
	return context->context.process->program->machines[context->context.instanceOf].funs[entryFunIndex].implementation;
}

FORCEINLINE
PRT_SM_FUN
PrtGetExitFunction(
__in PRT_SM_CONTEXT_PRIV		*context
)
{
	PRT_UINT32 exitFunIndex = context->context.process->program->machines[context->context.instanceOf].states[context->currentState].exitFunIndex;
	return context->context.process->program->machines[context->context.instanceOf].funs[exitFunIndex].implementation;
}

FORCEINLINE
PRT_DODECL*
PrtGetAction(
__in PRT_SM_CONTEXT_PRIV		*context
)
{
	PRT_UINT32 currEvent = PrtPrimGetEvent(context->trigger.event);
	PRT_BOOLEAN isActionInstalled = FALSE;
	PRT_UINT32 i, nActions;
	PRT_STATESTACK currStack;
	PRT_STATEDECL *stateTable;
	PRT_UINT32 topOfStackState;

	PRT_DODECL *actionDecl = NULL;

	//check if action is defined for the current state
	isActionInstalled = PrtIsActionInstalled(currEvent, PrtGetActionsPacked(context, context->currentState));
	if (isActionInstalled)
	{
		//
		// get action function
		//
		nActions = PrtGetCurrentStateDecl(context).nDos;
		for (i = 0; i < nActions; i++)
		{
			if (PrtGetCurrentStateDecl(context).dos[i].triggerEventIndex == currEvent)
			{
				actionDecl = &PrtGetCurrentStateDecl(context).dos[i];
				return actionDecl;
			}
		}
	}

	//
	// Scan the parent states
	//
	currStack = context->callStack;
	stateTable = context->context.process->program->machines[context->context.instanceOf].states;
	for (i = currStack.length - 1; i >= 0; i--)
	{
		topOfStackState = currStack.statesStack[i].stateIndex;
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

	PrtAssert(actionDecl != NULL, "Action cannot be NULL");
	return actionDecl;
}


FORCEINLINE
PRT_UINT32*
PrtGetDeferredPacked(
__in PRT_SM_CONTEXT_PRIV	*context,
__in PRT_UINT32				stateIndex
)
{
	PRT_EVENTSETDECL* evSets = context->context.process->program->eventSets;
	PRT_UINT32 evSetIndex = context->context.process->program->machines[context->context.instanceOf].states[stateIndex].defersSetIndex;
	return evSets[evSetIndex].packedEvents;
}

FORCEINLINE
PRT_UINT32*
PrtGetActionsPacked(
__in PRT_SM_CONTEXT_PRIV	*context,
__in PRT_UINT32				stateIndex
)
{
	PRT_EVENTSETDECL* evSets = context->context.process->program->eventSets;
	PRT_UINT32 evSetIndex = context->context.process->program->machines[context->context.instanceOf].states[stateIndex].doSetIndex;
	return evSets[evSetIndex].packedEvents;
}

FORCEINLINE
PRT_UINT32*
PrtGetTransitionsPacked(
__in PRT_SM_CONTEXT_PRIV	*context,
__in PRT_UINT32				stateIndex
)
{
	PRT_EVENTSETDECL* evSets = context->context.process->program->eventSets;
	PRT_UINT32 evSetIndex = context->context.process->program->machines[context->context.instanceOf].states[stateIndex].transSetIndex;
	return evSets[evSetIndex].packedEvents;
}


FORCEINLINE
PRT_TRANSDECL*
PrtGetTransTable(
__in PRT_SM_CONTEXT_PRIV	*context,
__in PRT_UINT32				stateIndex,
__out PRT_UINT32			*nTransitions
)
{
	*nTransitions = context->context.process->program->machines[context->context.instanceOf].states[stateIndex].nTransitions;
	return context->context.process->program->machines[context->context.instanceOf].states[stateIndex].transitions;
}

PRT_BOOLEAN
PrtIsPushTransition(
PRT_SM_CONTEXT_PRIV			*context,
PRT_UINT32				event
)
{
	PRT_UINT16 i;
	PRT_UINT32 nTransitions;
	PRT_TRANSDECL* transTable;
	PRT_BOOLEAN isPushTransition;

	transTable = PrtGetTransTable(context, context->currentState, &nTransitions);
	isPushTransition = FALSE;
	for (i = 0; i < nTransitions; ++i)
	{
		//check if transition is Call
		if (transTable[i].isPush && transTable[i].triggerEventIndex == event)
		{
			isPushTransition = TRUE;
		}
	}

	return isPushTransition;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsTransitionPresent(
__in PRT_UINT32				eventIndex,
__in PRT_SM_CONTEXT_PRIV			*context
)
{
	PRT_UINT32* transitionsPacked = PrtGetTransitionsPacked(context, context->currentState);
	if ((transitionsPacked[eventIndex / (sizeof(PRT_UINT32)* 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32)* 8)))) != 0)
	{
		return TRUE;
	}

	return FALSE;
}

void*
PrtClonePackedSet(
void*					packedSet,
PRT_UINT32					size
)
{
	PRT_UINT32 *clone;
	PRT_UINT32 i;

	clone = (PRT_UINT32 *)PrtCalloc(size, sizeof(PRT_UINT32));
	for (i = 0; i<size; i++)
	{
		clone[i] = ((PRT_UINT32*)packedSet)[i];
	}

	return clone;
}

void
PrtUpdateCurrentActionsSet(
PRT_SM_CONTEXT_PRIV			*context
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
		context->currentActionsSetCompact[i] = context->inheritedActionsSetCompact[i] & ~currDefSetPacked[i]; // A - d
		context->currentActionsSetCompact[i] |= currActionsPacked[i];
		context->currentActionsSetCompact[i] &= ~currTransitionsPacked[i];
	}
}

void
PrtUpdateCurrentDeferredSet(
PRT_SM_CONTEXT_PRIV			*context
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
__in PRT_SM_CONTEXT_PRIV *context
)
{
	PRT_INT32 maxEventQueueSize = context->context.process->program->machines[context->context.instanceOf].maxQueueSize;
	PRT_INT16 currEventQueueSize = context->eventQueue.eventsSize;
	PRT_INT32 newQueueSize = (maxEventQueueSize != 0xffffffff && currEventQueueSize * 2 > maxEventQueueSize) ? maxEventQueueSize : currEventQueueSize * 2;
	PRT_TRIGGER* oldQueue = context->eventQueue.events;
	PRT_INT16 oldHead = context->eventQueue.headIndex;
	PRT_INT16 oldTail = context->eventQueue.tailIndex;
	PRT_TRIGGER *newQueue = (PRT_TRIGGER*)PrtCalloc(newQueueSize, sizeof(PRT_TRIGGER));
	PRT_INT16 newHead = 0;
	PRT_INT16 newTail = 0;

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
PrtFreeSMContext(
PRT_SM_CONTEXT_PRIV			*context
)
{
	if (context->currentActionsSetCompact != NULL)
	{
		PrtFree(context->currentActionsSetCompact);
	}

	if (context->currentDeferredSetCompact != NULL)
	{
		PrtFree(context->currentDeferredSetCompact);
	}

	if (context->eventQueue.events != NULL)
	{
		PrtFree(context->eventQueue.events);
	}

	if (context->context.extContext != NULL)
	{
		PrtFree(context->context.extContext);
	}

	if (context->inheritedActionsSetCompact != NULL)
	{
		PrtFree(context->inheritedActionsSetCompact);
	}

	if (context->inheritedDeferredSetCompact != NULL)
	{
		PrtFree(context->inheritedDeferredSetCompact);
	}

	if (context->varValues != NULL)
	{
		UINT i;
		PRT_MACHINEDECL *mdecl = &(context->context.process->program->machines[context->context.instanceOf]);

		for (i = 0; i < mdecl->nVars; i++) {
			PrtFreeValue(context->varValues[i]);
		}
		PrtFree(context->varValues);
	}

	PrtDestroyMutex(context->stateMachineLock);

	PrtFree(context);
}

void
PrtHandleError(
__in PRT_STATUS ex,
__in PRT_SM_CONTEXT_PRIV *context
)
{
	((PRT_PROCESS_PRIV *)context->context.process)->errorHandler(ex, context);
}

void
PrtLog(
__in PRT_STEP step,
__in PRT_SM_CONTEXT_PRIV *context
)
{
	((PRT_PROCESS_PRIV *)context->context.process)->logHandler(step, context);
}

