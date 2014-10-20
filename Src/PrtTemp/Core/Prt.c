#include "PrtExecution.h"

/*********************************************************************************

Public Functions

*********************************************************************************/

PRT_SM_CONTEXT *
PrtMkMachine(
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

	if (context == NULL)
	{
		PrtAssert(PRT_FALSE, "Failed to Allocated Memory");
		return NULL;
	}

	publicContext = &context->context;

	//
	// Initialize Machine Identity
	//
	publicContext->process = process;
	publicContext->instanceOf = instanceOf;
	// publicContext->thisP 

	//
	// Initialize Machine Internal Variables
	//
	context->currentState = publicContext->process->program->machines[context->context.instanceOf].initStateIndex;
	context->isRunning = FALSE;
	context->isHalted = FALSE;
	context->lastOperation = OtherStatement;

	context->trigger.event = PrtMkNullValue();
	context->trigger.payload = PrtCloneValue(payload);
	context->returnTo = PrtEntryFunStart;
	context->stateExecFun = PrtStateEntry;

	//
	// Allocate memory for local variables
	//
	context->varValues = nVars == 0 ? NULL : PrtCalloc(nVars, sizeof(PRT_VALUE*));

	//
	// If failed to allocate memory
	//
	if ((nVars > 0) && (context->varValues == NULL))
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return NULL;
	}

	//
	// Initialize local variables
	//
	if (nVars > 0)
	{
		for (i = 0; i < nVars; i++)
		{
			context->varValues[i] = PrtMkDefaultValue(publicContext->process->program->machines[instanceOf].vars[i].type);
		}
	}

	//
	// Machine Call State Depth
	//
	context->callStack.length = 0;

	//
	// Initialize event queue
	//
	context->currentLengthOfEventQueue = eQSize;
	context->eventQueue.events = (PRT_TRIGGER*)PrtCalloc(eQSize, sizeof(PRT_TRIGGER));
	//
	// Failed to allocate memory
	//
	if (context->eventQueue.events == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return NULL;
	}

	context->eventQueue.headIndex = 0;
	context->eventQueue.tailIndex = 0;
	context->eventQueue.size = 0;
	context->eventQueue.isFull = FALSE;

	//
	// Initialize Inherited Deferred Set 
	//
	packSize = PrtGetPackSize(context);
	context->inheritedDeferredSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));
	//
	// Failed to Allocate memory
	//
	if (context->inheritedDeferredSetCompact == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return NULL;
	}

	//
	// Initialize the current deferred set
	//
	context->currentDeferredSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));
	//
	// Failed to allocate memory
	//
	if (context->currentDeferredSetCompact == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return NULL;
	}

	//
	// Initialize actions
	//
	context->inheritedActionsSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));
	if (context->inheritedActionsSetCompact == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return NULL;
	}

	context->currentActionsSetCompact = (PRT_UINT32*)PrtCalloc(packSize, sizeof(PRT_UINT32));
	if (context->currentActionsSetCompact == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return NULL;
	}

	//
	// Allocate External context Structure
	//
	publicContext->process->program->machines[publicContext->instanceOf].extCtor(publicContext, payload);
	//
	// Failed to allocate memory
	//
	if (publicContext->extContext == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return NULL;
	}

	//
	//Initialize state machine lock
	//
	context->stateMachineLock = PrtCreateMutex();

	//
	//Log
	//
	PrtLog(PRT_STEP_CREATE, context);

	//
	//Acquire the lock while stabilizing the state machine
	//
	PrtLockMutex(context->stateMachineLock);
	//
	// Run the state machine
	//
	PrtRunStateMachine(context, TRUE);

	//
	// Add it to the process allMachines log
	//
	PRT_UINT32 numMachines = ((PRT_PROCESS_PRIV *)context->context.process)->numMachines;
	PRT_UINT32 machineCount = ((PRT_PROCESS_PRIV *)context->context.process)->machineCount;
	PRT_SM_CONTEXT **machines = ((PRT_PROCESS_PRIV *)context->context.process)->machines;
	if (machineCount == numMachines) {
		PRT_SM_CONTEXT **newMachines = (PRT_SM_CONTEXT **)PrtCalloc(2 * machineCount, sizeof(PRT_SM_CONTEXT_PRIV *));
		for (PRT_UINT32 i = 0; i < machineCount; i++)
		{
			newMachines[i] = machines[i];
		}
		machines = newMachines;
		((PRT_PROCESS_PRIV *)context->context.process)->machines = newMachines;
		((PRT_PROCESS_PRIV *)context->context.process)->machineCount = 2 * machineCount;
	}
	machines[numMachines] = (PRT_SM_CONTEXT *)context;
	((PRT_PROCESS_PRIV *)context->context.process)->numMachines++;
	return &context->context;
}


PRT_BOOLEAN
PrtIsEventMaxInstanceExceeded(
__in PRT_EVENTQUEUE			*queue,
__in PRT_UINT32				eventIndex,
__in PRT_UINT32				maxInstances,
__in PRT_UINT16				queueSize
)
{
	//
	// Declarations
	//
	PRT_UINT32 head;
	PRT_UINT32 tail;
	PRT_UINT16 currMaxInstance;
	PRT_BOOLEAN isMaxInstancesExceeded;
	//
	// Code
	//

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
__in PRT_SM_CONTEXT_PRIV			*context,
__in PRT_VALUE					*event,
__in PRT_VALUE					*payload
)
{
	PRT_EVENTQUEUE *queue;
	PRT_UINT32 tail;
	PRT_UINT32 currMaxInstance;
	PRT_INT16 newQueueSize;
	PRT_UINT32 eventIndex;
	
	//check that the enqueued message is event type
	PrtAssert(event->type->typeKind == PRT_KIND_EVENT, "Parameter event is not of type EVENT");
	PrtAssert(PrtIsSubtype(payload->type, PrtGetPayloadType(context, event)), "Payload type mismatch");


	eventIndex = PrtPrimGetEvent(event);
	PrtAssert(PrtIsNullValue(event), "Enqueued Event Cannot be a NULL event");
	PrtAssert(eventIndex != PRT_SPECIAL_EVENT_DEFAULT_OR_NULL, "Enqueued Event Cannot be a DEFAULT event");
	
	//check if the machine is still alive or halted
	if (context->isHalted)
	{
		//drop the event silently
		return;
	}

	currMaxInstance = context->context.process->program->events[eventIndex].eventMaxInstances;

	PrtLockMutex(context->stateMachineLock);
	// queue is full resize the queue if possible
	if (context->eventQueue.size == context->context.process->program->machines[context->context.instanceOf].maxQueueSize)
	{
		PrtUnlockMutex(context->stateMachineLock);
		PrtExceptionHandler(PRT_STATUS_QUEUE_OVERFLOW, context);
		return;
	}

	//Check if we need to resize the queue
	if (context->eventQueue.isFull)
	{
		newQueueSize = PrtResizeEventQueue(context);
	}

	queue = &context->eventQueue;
	//check if Event.MaxInstances is NIL
	//check if the <event, payload> is in Queue
	if (currMaxInstance != 0xffffffff && PrtIsEventMaxInstanceExceeded(queue, eventIndex, currMaxInstance, context->currentLengthOfEventQueue))
	{
		//
		//  Check if event is occuring more than maxinstances
		//
		PrtUnlockMutex(context->stateMachineLock);
		PrtExceptionHandler(PRT_STATUS_EVENT_OVERFLOW , context);
		return;

	}


	tail = queue->tailIndex;

	PrtAssert(!(context->currentLengthOfEventQueue == context->context.process->program->machines[context->context.instanceOf].maxQueueSize && queue->isFull), "Queue Size Exceeded the Maximum Limit");
	//
	// Add event to the queue
	//
	queue->events[tail].event = PrtCloneValue(event);
	queue->events[tail].payload = PrtCloneValue(payload);
	queue->size++;
	queue->tailIndex = (tail + 1) % context->currentLengthOfEventQueue;
	queue->isFull = (queue->tailIndex == queue->headIndex) ? TRUE : FALSE;

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

		PrtRunStateMachine(context, FALSE);
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
	context->trigger.event = PrtMkNullValue();;
	context->trigger.payload = PrtMkNullValue();

}

PRT_BOOLEAN
PrtIsSpecialEvent(
PRT_VALUE * event
)
{
	return (PrtIsNullValue(event) || PrtPrimGetEvent(event) == PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
}

PRT_TYPE*
PrtGetPayloadType(
PRT_SM_CONTEXT_PRIV *context,
PRT_VALUE	  *event
)
{
	return context->context.process->program->events[PrtPrimGetEvent(event)].type;
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
	PrtAssert(PrtIsSpecialEvent(event), "Raised Event Cannot be a NULL or DEFAULT event");
	//
	// Set operation to raiseStatement
	//
	context->lastOperation = RaiseStatement;

	//Free the memory associated with trigger
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
PrtCall(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_UINT32				stateIndex
)
{
	//
	// Push current state on top of the stack
	//
	PrtPushState(context, TRUE);

	//
	// Set Trigger to NULL after a call edge
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
	// Last operation set to call Statement
	//
	context->lastOperation = CallStatement;

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

void
PrtRunStateMachine(
__inout PRT_SM_CONTEXT_PRIV	    *context,
__in PRT_BOOLEAN	doEntryOrExit
)
{
	//
	// Declarations
	//
	PRT_TRIGGER e;
	PRT_BOOLEAN isLockAcq;
	PRT_DODECL *currActionDecl;
	//
	// Code
	//

	e.event = PrtMkNullValue();
	e.payload = PrtMkNullValue();

	isLockAcq = TRUE;

	context->isRunning = TRUE;

	//// If doEntry is false, then the current state of the machine
	//// has already been executed in a previous call to PrtStabilize
	if (!doEntryOrExit)
	{
		//// We only return to the top of the loop if the state changed
		//// so the entry function should be executed
		doEntryOrExit = TRUE;
		goto DoDequeue;
	}

	//Since we are not accessing event queues we can release the locks
	if (isLockAcq)
	{
		isLockAcq = FALSE;
		PrtUnlockMutex(context->stateMachineLock);
	}


DoEntryOrExitOrActionFunction:

	// SM is entering or re-entering the state (entry or action or exit)
	// update the current deferred and actions set
	PrtUpdateCurrentActionsSet(context);
	PrtUpdateCurrentDeferredSet(context);

	//// Step 1. Execute the entry function or Exit Function
	// 
	// Check whether to execute entry or exit function
	//
	if (context->stateExecFun == PrtStateEntry)
	{
		// handle the case when we are entering a state with an unhandled event
		if (context->returnTo == PrtEntryFunEnd && PrtIsNullValue(context->trigger.event))
		{
			goto DoTakeTransition;
		}

		//
		// Execute Entry Function
		//
		//

		//
		//Log
		//
		if (context->returnTo == PrtEntryFunStart)
			PrtLog(PRT_STEP_MOVE, context);
		// Initialize context before executing entry function
		//
		context->lastOperation = OtherStatement;
		//
		// Execute the Entry function
		//
		PRT_UINT32 entryFunIndex = context->context.process->program->machines[context->context.instanceOf].states[context->currentState].entryFunIndex;
		PrtGetEntryFunction(context)(&context->context, entryFunIndex, NULL);

		//// Step 2. Handle any raised event -- call --- Pop -- others
		switch (context->lastOperation)
		{
		case PopStatement:
			context->stateExecFun = PrtStateExit;
			context->returnTo = PrtExitFunStart;
			goto DoEntryOrExitOrActionFunction;
			break;
		case RaiseStatement:
			if (PrtIsTransitionPresent(PrtPrimGetEvent(context->trigger.event), context))
			{

				if (PrtIsCallTransition(context, PrtPrimGetEvent(context->trigger.event)))
				{
					//
					// call transition so no exit function executed
					//
					goto DoTakeTransition;
				}

				else
				{
					// execute exit function

					context->stateExecFun = PrtStateExit;
					context->returnTo = PrtExitFunStart;
					goto DoEntryOrExitOrActionFunction;
				}
			}
			//
			// check if there is an action installed for this event
			//
			else if (PrtIsActionInstalled(PrtPrimGetEvent(context->trigger.event), context->currentActionsSetCompact))
			{
				context->stateExecFun = PrtStateAction;
				context->returnTo = PrtActionFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			//
			// Unhandled raised event
			//
			else
			{
				context->stateExecFun = PrtStateExit;
				context->returnTo = PrtExitFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			break;
		case CallStatement:
			context->stateExecFun = PrtStateEntry;
			context->returnTo = PrtEntryFunStart;
			goto DoEntryOrExitOrActionFunction;
			break;
		case OtherStatement:
			goto DoDequeue;
			break;
		default:
			break;
		}
	}
	else if (context->stateExecFun == PrtStateExit)
	{
		//
		//Execute the exit function
		//
		// Initialize context before executing exit function
		//
		context->lastOperation = OtherStatement;
		//
		// Execute the exit function for the current state
		//
		PRT_UINT32 exitFunIndex = context->context.process->program->machines[context->context.instanceOf].states[context->currentState].exitFunIndex;
		PrtLog(PRT_STEP_EXIT, context);
		PrtGetExitFunction(context)(&context->context, exitFunIndex, NULL);

		//// Step 2. Handle call or others
		switch (context->lastOperation)
		{
		case RaiseStatement:
		case PopStatement:
			PrtAssert(PRT_FALSE, "Pop or Raise is not allowed inside Exit Function");
			break;
		case CallStatement:
			context->stateExecFun = PrtStateEntry;
			context->returnTo = PrtEntryFunStart;
			goto DoEntryOrExitOrActionFunction;
		case OtherStatement:
			goto DoTakeTransition;
		default:
			break;
		}
	}
	else if (context->stateExecFun == PrtStateAction)
	{
		//
		// Execute the action installed corresponding to trigger
		//
		//
		// Get the current action decl
		currActionDecl = PrtGetAction(context);

		//
		//Log
		//
		if (context->returnTo == PrtActionFunStart)
			PrtLog(PRT_STEP_DO, context);
		//
		// Initialize context before executing entry function
		//
		context->lastOperation = OtherStatement;
		//
		// Execute the Entry function
		//
		context->context.process->program->machines[context->context.instanceOf].funs[currActionDecl->doFunIndex].implementation(&context->context, currActionDecl->doFunIndex, NULL);

		//// Step 2. Handle any raised event -- call --- Pop -- others
		switch (context->lastOperation)
		{
		case PopStatement:
			context->stateExecFun = PrtStateExit;
			context->returnTo = PrtExitFunStart;
			goto DoEntryOrExitOrActionFunction;
			break;
		case RaiseStatement:
			if (PrtIsTransitionPresent(context->trigger.event->valueUnion.ev, context))
			{

				if (PrtIsCallTransition(context, context->trigger.event->valueUnion.ev))
				{
					//
					// call transition so no exit function executed
					//
					goto DoTakeTransition;
				}

				else
				{
					// execute exit function
					context->stateExecFun = PrtStateExit;
					context->returnTo = PrtExitFunStart;
					goto DoEntryOrExitOrActionFunction;
				}
			}
			//
			// check if there is an action installed for this event
			//
			else if (PrtIsActionInstalled(PrtPrimGetEvent(context->trigger.event), context->currentActionsSetCompact))
			{
				context->stateExecFun = PrtStateAction;
				context->returnTo = PrtActionFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			//
			// Unhandled raised event
			//
			else
			{
				context->stateExecFun = PrtStateExit;
				context->returnTo = PrtExitFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			break;
		case CallStatement:
			context->stateExecFun = PrtStateEntry;
			context->returnTo = PrtEntryFunStart;
			goto DoEntryOrExitOrActionFunction;
			break;
		case OtherStatement:
			goto DoDequeue;
			break;
		default:
			break;
		}
	}


DoDequeue:

	//// Step 3. Try to get an event from the queue.
	if (!isLockAcq)
	{
		isLockAcq = TRUE;
		PrtLockMutex(context->stateMachineLock);
	}

	e = PrtDequeueEvent(context);

	//Successfully dequeued an event
	if (PrtIsNullValue(e.event))
	{
		//Release Lock
		isLockAcq = FALSE;
		PrtUnlockMutex(context->stateMachineLock);
		if (PrtIsCallTransition(context, PrtPrimGetEvent(e.event)))
		{
			goto DoTakeTransition;
		}
		//
		// Transition corresponding to dequeued event (Ankush : this takes care of local priority of e over actions)
		//
		else if (PrtIsTransitionPresent(PrtPrimGetEvent(context->trigger.event), context))
		{
			context->stateExecFun = PrtStateExit;
			context->returnTo = PrtExitFunStart;
			goto DoEntryOrExitOrActionFunction;
		}
		//
		// check if there is an action installed for this event
		//
		else if (PrtIsActionInstalled(PrtPrimGetEvent(context->trigger.event), context->currentActionsSetCompact))
		{
			context->stateExecFun = PrtStateAction;
			context->returnTo = PrtActionFunStart;
			goto DoEntryOrExitOrActionFunction;
		}
		//
		// Unhandled dequeued event
		//
		else
		{
			context->stateExecFun = PrtStateExit;
			context->returnTo = PrtExitFunStart;
			goto DoEntryOrExitOrActionFunction;
		}

	}
	// failed to dequeue an event -> two possibility either take default branch(if available) else block
	else if (PrtStateHasDefaultTransition(context))
	{
		//release lock
		isLockAcq = FALSE;
		PrtUnlockMutex(context->stateMachineLock);

		//Free memory
		PrtFreeValue(context->trigger.event);
		PrtFreeValue(context->trigger.payload);

		context->trigger.event = PrtMkEventValue(PRT_SPECIAL_EVENT_DEFAULT_OR_NULL);
		context->trigger.payload = PrtMkNullValue();
		context->stateExecFun = PrtStateExit;
		context->returnTo = PrtExitFunStart;
		goto DoEntryOrExitOrActionFunction;
	}
	else
	{
		context->isRunning = FALSE;
		//Release Lock
		isLockAcq = FALSE;
		PrtUnlockMutex(context->stateMachineLock);
		return;

	}


DoTakeTransition:

	if ((PrtIsNullValue(context->trigger.event)))
	{
		//
		// The last statement executed was a pop statement
		//
		PrtPopState(context, TRUE);

		//
		//Log
		//
		PrtLog(PRT_STEP_POP, context);

		if (context->returnTo == PrtEntryFunEnd)
		{
			//
			// Pop returned to a call edge and hence we should return to the dequeue of 
			// current state
			//
			goto DoDequeue;
		}
		else
		{
			//
			// Pop returns to end of a call statement and hence should execute the next
			//statement in entry/exit function.
			goto DoEntryOrExitOrActionFunction;
		}
	}
	else if (PrtPrimGetEvent(context->trigger.event) == PRT_SPECIAL_EVENT_DEFAULT_OR_NULL)
	{
		//
		// Take default transition
		//
		PrtTakeDefaultTransition(context);
		goto DoEntryOrExitOrActionFunction;
	}
	else
	{
		//
		// Trigger is non-null and hence its a raise or dequeue or unhandled event
		//
		PrtTakeTransition(context, PrtPrimGetEvent(context->trigger.event));

		//
		// If the machine is halted because of halt event then return
		//
		if (context->isHalted)
			return;


		goto DoEntryOrExitOrActionFunction;

	}

	return;
}

void
PrtTakeDefaultTransition(
__inout PRT_SM_CONTEXT_PRIV		*context
)
{
	//
	// Declarations
	//
	ULONG i;
	PRT_UINT32 nTransitions;
	PRT_TRANSDECL* transTable;

	//
	// Code
	//


	transTable = PrtGetTransTable(context, context->currentState, &nTransitions);

	for (i = 0; i < nTransitions; ++i)
	{
		//check if transition is After
		if (transTable[i].triggerEventIndex == PRT_SPECIAL_EVENT_DEFAULT_OR_NULL)
		{
			//check if its a call transition
			if (transTable[i].isPush != FALSE)
			{
				context->returnTo = (PRT_UINT16)PrtEntryFunEnd;
				PrtPushState(context, FALSE);
			}

			//update the state
			context->currentState = transTable[i].destStateIndex;
			context->returnTo = PrtEntryFunStart;
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
	//
	// Declarations
	//
	PRT_UINT32 i;
	PRT_UINT32 nTransitions;
	PRT_TRANSDECL* transTable;

	//
	//code
	//

	transTable = PrtGetTransTable(context, context->currentState, &nTransitions);

	for (i = 0; i < nTransitions; ++i)
	{
		if ((transTable[i].triggerEventIndex == eventIndex))
		{

			//check if its a call transition
			if (transTable[i].isPush != FALSE)
			{
				context->returnTo = (PRT_UINT16)PrtEntryFunEnd;
				PrtPushState(context, FALSE);
			}

			// change CurrentState state and set returnTo to smfEntryFunStart 
			// next to execute is the entry function of the destination state
			context->currentState = transTable[i].destStateIndex;
			context->returnTo = PrtEntryFunStart;
			context->stateExecFun = PrtStateEntry;

			//
			//Log
			//
			PrtLog(PRT_STEP_PUSH, context);

			return;
		}
	}
	if (context->callStack.length > 0)
	{
		PrtPopState(context, FALSE);
		//
		//Log
		//
		PrtLog(PRT_STEP_UNHANDLED, context);
	}
	else
	{
		if (PrtPrimGetEvent(context->trigger.event) == PRT_SPECIAL_EVENT_HALT)
		{
			PrtHaltMachine(context);
			return;
		}
		else
		{
			//Exception
			PrtExceptionHandler(PRT_STATUS_EVENT_UNHANDLED, context);
			return;
		}
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
__in	PRT_BOOLEAN			isCallStatement
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

		if (isCallStatement)
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
	//
	// Declarations
	//
	PRT_UINT16 i;
	PRT_UINT16 packSize;
	PRT_UINT16 length;
	PRT_STACKSTATE_INFO poppedState;
	// 
	// Code
	//
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
	for (i = 0; i<packSize; i++)
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
	else
	{
		//
		// Poppped because of an unhandled event
		//
		// TODO : Confirm if we are here then there is definitely no action defined for this event.
		//
		// If the popped state is ExitFunction then its an error
		// there is an unhandled event in exit function
		//
		PrtAssert(poppedState.stateExecFun == PrtStateEntry || poppedState.stateExecFun == PrtStateAction, "Unhandled Event in Exit Function");

		//
		// assert that we are popping back because of an call-edge and not because of a call statement (implicit pop)
		//
		if (poppedState.returnTo != PrtEntryFunEnd)
		{
			// TBD: This is the case when a push statement terminates in a raised event
			// Temporarily making it an assertion failure
			PrtExceptionHandler(PRT_STATUS_ASSERT, context);
		}

		//check if there is a push transition defined for the unhandled event
		if (PrtIsTransitionPresent(PrtPrimGetEvent(context->trigger.event), context) && PrtIsCallTransition(context, PrtPrimGetEvent(context->trigger.event)))
		{
			context->stateExecFun = PrtStateEntry;
			context->returnTo = (PRT_UINT16)PrtEntryFunEnd;
		}
		else
		{
			context->stateExecFun = PrtStateExit;
			context->returnTo = (PRT_UINT16)PrtExitFunStart;
		}
	}
	return;
}


PRT_TRIGGER
PrtDequeueEvent(
__inout PRT_SM_CONTEXT_PRIV	*context
)
{
	//
	// Declarations
	//
	INT queueLength;
	PRT_EVENTQUEUE *queue;
	PRT_UINT32* deferPacked;
	INT i, head;
	PRT_TRIGGER e;

	//
	// Code
	//

	queueLength = context->currentLengthOfEventQueue;
	queue = &context->eventQueue;
	deferPacked = PrtGetDeferredPacked(context, context->currentState);
	head = queue->headIndex;
	e.event = PrtMkNullValue();
	e.payload = PrtMkNullValue();

	PRT_DBG_ASSERT(queue->size <= queueLength, "Check Failed");
	PRT_DBG_ASSERT(queue->size >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->headIndex >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->tailIndex >= 0, "Check Failed");

	if (PrtIsQueueEmpty(queue)) {
		return e;
	}

	//
	// Find the element to dequeue
	//
	for (i = 0; i < queue->size; i++) {
		INT index = (head + i) % queueLength;
		e = queue->events[index];
		if (!PrtIsEventDeferred(PrtPrimGetEvent(e.event), context->currentDeferredSetCompact)) {
			break;
		}
	}

	//
	// Check if not found
	//
	if (i == queue->size) {
		e.event = PrtMkNullValue();
		e.payload = PrtMkNullValue();
		return e;
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
	queue->isFull = FALSE;
	queue->size--;

	//
	// Free old trigger, if any.
	//
	PrtFreeValue(context->trigger.event);
	PrtFreeValue(context->trigger.payload);

	//
	// Store the event and argument
	//
	context->trigger.event = e.event;
	context->trigger.payload = e.payload;

	PRT_DBG_ASSERT(queue->size <= queueLength, "Check Failed");
	PRT_DBG_ASSERT(queue->size >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->headIndex >= 0, "Check Failed");
	PRT_DBG_ASSERT(queue->tailIndex >= 0, "Check Failed");

	//
	//Log
	//
	PrtLog(PRT_STEP_DEQUEUE, context);
	return e;
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
	return (UINT16)(((nEvents == 0) || (nEvents % (sizeof(PRT_UINT32)* 8) != 0))
		? (1 + (nEvents / (sizeof(PRT_UINT32)* 8)))
		: (nEvents / (sizeof(PRT_UINT32)* 8)));
}

FORCEINLINE
PRT_BOOLEAN
PrtIsQueueEmpty(
__in PRT_EVENTQUEUE		*queue
)
{
	return !(queue->isFull) && (queue->headIndex == queue->tailIndex);
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
PRT_SM_FUN
PrtGetEntryFunction(
__in PRT_SM_CONTEXT_PRIV		*context
)
{
	PRT_UINT32 entryFunIndex = context->context.process->program->machines[context->context.instanceOf].states[context->currentState].entryFunIndex;
	return context->context.process->program->machines[context->context.instanceOf].funs[entryFunIndex].implementation;
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
__in PRT_SM_CONTEXT_PRIV			*context,
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
__in PRT_SM_CONTEXT_PRIV		*context,
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
__in PRT_SM_CONTEXT_PRIV			*context,
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
__in PRT_SM_CONTEXT_PRIV		*context,
__in PRT_UINT32				stateIndex,
__out PRT_UINT32			*nTransitions
)
{
	*nTransitions = context->context.process->program->machines[context->context.instanceOf].states[stateIndex].nTransitions;
	return context->context.process->program->machines[context->context.instanceOf].states[stateIndex].transitions;
}

PRT_BOOLEAN
PrtIsCallTransition(
PRT_SM_CONTEXT_PRIV			*context,
PRT_UINT32				event
)
{
	PRT_UINT16 i;
	PRT_UINT32 nTransitions;
	PRT_TRANSDECL* transTable;
	PRT_BOOLEAN isCallTransition;

	transTable = PrtGetTransTable(context, context->currentState, &nTransitions);
	isCallTransition = FALSE;
	for (i = 0; i < nTransitions; ++i)
	{
		//check if transition is Call
		if (transTable[i].isPush && transTable[i].triggerEventIndex == event)
		{
			isCallTransition = TRUE;
		}
	}

	return isCallTransition;
}

FORCEINLINE
PRT_BOOLEAN
PrtIsTransitionPresent(
__in PRT_UINT32				eventIndex,
__in PRT_SM_CONTEXT_PRIV			*context
)
{
	PRT_UINT32* trabsitionsPacked = PrtGetTransitionsPacked(context, context->currentState);
	if ((trabsitionsPacked[eventIndex / (sizeof(PRT_UINT32)* 8)] & (1 << (eventIndex % (sizeof(PRT_UINT32)* 8)))) != 0)
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


PRT_INT16
PrtResizeEventQueue(
__in PRT_SM_CONTEXT_PRIV *context
)
{
	PRT_INT32 maxEventQueueSize = context->context.process->program->machines[context->context.instanceOf].maxQueueSize;
	PRT_INT16 currEventQueueSize = context->currentLengthOfEventQueue;
	PRT_INT32 newQueueSize = context->currentLengthOfEventQueue * 2 > maxEventQueueSize ? maxEventQueueSize : context->currentLengthOfEventQueue * 2;
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
	context->eventQueue.isFull = FALSE;
	context->currentLengthOfEventQueue = (PRT_UINT8)newQueueSize;

	//Release the older Queue
	PrtFree(oldQueue);

	return context->currentLengthOfEventQueue;
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
PrtExceptionHandler(
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
	((PRT_PROCESS_PRIV *)context->context.process)->log(step, context);
}

