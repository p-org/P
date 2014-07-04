/*********************************************************************************

Copyright (c) Microsoft Corporation

Module Name:

PrtRuntime.c

Abstract:
This module contains implementation of P runtime for executing and maintaining the
statemachines

Environment:

Kernel mode only.

***********************************************************************************/

#include "PrtSMPrivate.h"
#include "Config\PrtConfig.h"
#include "Values\PrtDTTypes.h"
#include "Values\PrtDTValues.h"
#include "PrtSMPrivateTypes.h"
#include "PrtSMLogger.h"

/*********************************************************************************

Public Functions

*********************************************************************************/
#if (PRT_PLAT == PRT_PLAT_WINKMDF)
//
// Initializes StateMachine attributes used for creating a machine of type InstanceOf
//
VOID
PrtInitAttributes(
__inout PPRT_MACHINE_ATTRIBUTES Attributes,
__in PDEVICE_OBJECT				PDeviceObj,
__in PPRT_DRIVERDECL			Driver,
__in PRT_MACHINEDECL_INDEX		InstanceOf,
__in PPRT_PACKED_VALUE			Arg,
__in PVOID						ConstructorParam
)
#else
VOID
PrtInitAttributes(
__inout PPRT_MACHINE_ATTRIBUTES Attributes,
__in PPRT_PROGRAMDECL			Program,
__in PRT_MACHINEDECL_INDEX		InstanceOf,
__in PPRT_VALUE					Arg,
__in PVOID						ConstructorParam
)
#endif
/*++

Routine Description:

Kernel mode driver can call this rountine to Initialize Machine_Attribute structure,
which is used for creating new state-machine.
This function should be called after creating Attribute structure and before calling
PrtCreate() function to creating the statemachine


Arguments:

Attributes - Pointer to the Attribute Structure to be Initialized.

PDeviceObj - Pointer to the Device which is trying to create and use this state-machine

Driver - Pointer to driver declaration of the current program.

InstanceOf - Index into the Machine Type Table in driver decl.

Return Value:

NONE (VOID)

--*/

{

	Attributes->Program = Program;
	Attributes->InstanceOf = InstanceOf;
	Attributes->Arg = Arg;
	Attributes->ConstructorParam = ConstructorParam;
#ifdef KERNEL_MODE
	Attributes->PDeviceObj = PDeviceObj;
#endif

	return;
}


PRT_BOOLEAN
PrtCreate(
__in PPRT_MACHINE_ATTRIBUTES	InitAttributes,
__out PPRT_MACHINE_HANDLE		PSmHandle
)
/*++

Routine Description:

Kernel mode driver can call this rountine to create state-machine.
This function should creates and runs the state-machine


Arguments:

InitAttributes - Pointer to the Attribute Structure to be used for machine creation.

PSmHandle - Pointer to the out variable smHandle, which is set to handle of created
machine.


Return Value:

Type : NTSTATUS
STATUS_SUCCES = If statemachine was created successfully
STATUS_INSUFFICIENT_RESOURCES = If failed to allocate memory


--*/
{
	//
	// Declarations
	//
	UINT packSize;
	ULONG nVars;
	UCHAR eQSize;
	PPRT_SMCONTEXT context;
	ULONG i;

	//
	// Code
	//

	nVars = InitAttributes->Program->Machines[InitAttributes->InstanceOf].NVars;
	eQSize = PRT_QUEUE_LEN_DEFAULT;

	//
	// Allocate memory for state machine context
	//
	context = (PPRT_SMCONTEXT)PrtAllocateMemory(sizeof(PRT_SMCONTEXT));

	if (context == NULL)
	{
		PrtAssert(PRT_FALSE, "Failed to Allocated Memory");
		return PRT_FALSE;
	}

	//
	// Set State Machine Signature
	//
	context->StateMachineSignature = PrtStateMachine_Signature;


	//
	// Initialize Machine Identity
	//
	context->Program = InitAttributes->Program;
	context->InstanceOf = InitAttributes->InstanceOf;

	//
	// Initialize Machine Internal Variables
	//
	context->CurrentState = context->Program->Machines[context->InstanceOf].Initial;
	context->This = PrtGetStateMachineHandle(context);
	*PSmHandle = PrtGetStateMachineHandle(context);
	context->IsRunning = FALSE;
	context->LastOperation = OtherStatement;

	context->Trigger.Event = PrtMkDefaultValue(PrtMkPrimitiveType(PRT_KIND_EVENT));
	context->Trigger.Payload = PrtCloneValue(InitAttributes->Arg);
	context->ReturnTo = PrtEntryFunStart;
	context->StateExecFun = PrtStateEntry;

	//
	// Allocate memory for local variables
	//
	context->Values = nVars == 0 ? NULL : (PRT_VARVALUE_TABLE)PrtAllocateMemory(nVars*sizeof(PRT_VARVALUE));
	//
	// If failed to allocate memory
	//
	if ((nVars > 0) && (context->Values == NULL))
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return PRT_FALSE;
	}

	//
	// Initialize local variables
	//
	if (nVars > 0)
	{
		for (i = 0; i < nVars; i++)
		{
			context->Values[i] = PrtMkDefaultValue(InitAttributes->Program->Types[i]);
		}
	}

	//
	// Machine Call State Depth
	//
	context->CallStack.Length = 0;

	//
	// Initialize event queue
	//
	context->CurrentLengthOfEventQueue = eQSize;
	context->EventQueue.Events = (PPRT_TRIGGER)PrtAllocateMemory(eQSize * sizeof(PRT_TRIGGER));
	//
	// Failed to allocate memory
	//
	if (context->EventQueue.Events == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return PRT_FALSE;
	}

	context->EventQueue.Head = 0;
	context->EventQueue.Tail = 0;
	context->EventQueue.Size = 0;
	context->EventQueue.IsFull = FALSE;


	//
	// Initialize Inherited Deferred Set 
	//
	packSize = PrtGetPackSize(context);
	context->InheritedDeferred = (PPRT_EVENTDECL_INDEX_PACKEDTABLE)PrtAllocateMemory(packSize*sizeof(PRT_EVENTDECL_INDEX));
	//
	// Failed to Allocate memory
	//
	if (context->InheritedDeferred == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return PRT_FALSE;
	}
	memset(context->InheritedDeferred, 0, packSize*sizeof(PRT_EVENTDECL_INDEX));

	//
	// Initialize the current deferred set
	//
	context->CurrentDeferred = (PPRT_EVENTDECL_INDEX_PACKEDTABLE)PrtAllocateMemory(packSize*sizeof(PRT_EVENTDECL_INDEX));
	//
	// Failed to allocate memory
	//
	if (context->CurrentDeferred == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return PRT_FALSE;
	}
	memset(context->CurrentDeferred, 0, packSize*sizeof(PRT_EVENTDECL_INDEX));

	//
	// Initialize actions
	//
	context->InheritedActions = (PPRT_EVENTDECL_INDEX_PACKEDTABLE)PrtAllocateMemory(packSize * sizeof(PRT_ACTIONDECL_INDEX_PACKEDTABLE));
	if (context->InheritedActions == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return PRT_FALSE;
	}
	memset(context->InheritedActions, 0, packSize*sizeof(PRT_EVENTDECL_INDEX));

	context->CurrentActions = (PPRT_EVENTDECL_INDEX_PACKEDTABLE)PrtAllocateMemory(packSize * sizeof(PRT_ACTIONDECL_INDEX_PACKEDTABLE));
	if (context->CurrentActions == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return PRT_FALSE;
	}
	memset(context->CurrentActions, 0, packSize*sizeof(PRT_EVENTDECL_INDEX));

	//
	// Allocate External Context Structure
	//
	context->ExtContext = (PPRT_EXCONTEXT)PrtAllocateMemory(sizeof(PRT_EXCONTEXT));
	//
	// Failed to allocate memory
	//
	if (context->ExtContext == NULL)
	{
		PrtFreeSMContext(context);
		PrtAssert(PRT_FALSE, "Failed to Allocate Memory");
		return PRT_FALSE;
	}
	// Initialize ExtContext
	context->ExtContext->FreeThis = FALSE;
	context->ExtContext->ConstructorParam = InitAttributes->ConstructorParam;
	//
	// call machine constructor
	//
	context->Program->Machines[context->InstanceOf].constructorFun(InitAttributes->ConstructorParam, context->ExtContext);

	//
	//Initialize state machine lock
	//
	PrtInitializeLock(context);

#ifndef NDEBUG
	PrtTraceStep(context, traceCreateMachine);
#endif
	//
	//Acquire the lock while stabilizing the state machine
	//
	PrtAcquireLock(context);
	//
	// Run the state machine
	//
	PrtRunStateMachine(context, TRUE);


	return TRUE;
}



PRT_MACHINE_HANDLE
PrtNew(
__in PPRT_PROGRAMDECL			PDriverDecl,
__inout PPRT_SMCONTEXT			Context,
__in PRT_MACHINEDECL_INDEX		InstanceOf,
__in PPRT_VALUE			Arg
)
{
	PPRT_MACHINE_ATTRIBUTES mAttributes;
	PRT_MACHINE_HANDLE smHandle;
	mAttributes = (PPRT_MACHINE_ATTRIBUTES)PrtAllocateMemory(sizeof(PRT_MACHINE_ATTRIBUTES));
	PRT_ASSERTMSG("Failed to create Machine Attributes in New", mAttributes != NULL);
#ifdef KERNEL_MODE
	PrtInitAttributes(mAttributes, Context->PDeviceObj, PDriverDecl, InstanceOf, Arg, Context->ExtContext->ConstructorParam);
#else
	PrtInitAttributes(mAttributes, PDriverDecl, InstanceOf, Arg, Context->ExtContext->ConstructorParam);
#endif
	PrtCreate(mAttributes, &smHandle);
#ifdef KERNEL_MODE
	ExFreePool(mAttributes);
#else
	free(mAttributes);
#endif
	return smHandle;
}

BOOLEAN
PrtIsEventMaxInstanceExceeded(
__in PPRT_EVENTQUEUE		Queue,
__in PRT_EVENTDECL_INDEX	EventIndex,
__in UINT16					MaxInstances,
__in UINT16					QueueSize
)
/*++

Routine Description:

Kernel mode driver can call this rountine to check if an events maxinstance exceeded in
Queue.


Arguments:

Queue - Pointer to Event Queue to be searched.

EventIndex - Event to be checked.

MaxInstances - MaxInstances of the event in the queue.

QueueSize - Maximum size of the Array/Queue

Return Value:

Type - BOOLEAN
<TRUE> - If the event is present in queue
<FALSE> - If event is not present

--*/
{
	//
	// Declarations
	//
	UINT head;
	UINT tail;
	UINT16 currMaxInstance;
	BOOLEAN isMaxInstancesExceeded;
	//
	// Code
	//
	
	head = Queue->Head;
	tail = Queue->Tail;
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
		while (head < QueueSize)
		{
			if ((Queue->Events[head].Event == EventIndex))
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
		if ((Queue->Events[head].Event == EventIndex))
		{
			currMaxInstance = currMaxInstance + 1;
			head++;
		}
		else
		{
			head++;
		}
	}

	if (currMaxInstance >= MaxInstances)
	{
		isMaxInstancesExceeded = TRUE;
	}


	return isMaxInstancesExceeded;
}

VOID
PrtEnqueueEvent(
__in PRT_MACHINE_HANDLE			Machine,
__in PPRT_VALUE					Event,
__in PPRT_VALUE					Arg
)
/*++

Routine Description:

Kernel mode driver can call this rountine to Enqueue an event onto the statemachine.
If the state-machine is not running then it enqueues the event and runs state-machine
as well.


Arguments:

Machine - Handle of the machine in which event is enqueued.

EventIndex - Event to be checked.

Arg - Argument associated with the event.


Return Value:

NONE (VOID)

--*/
{
	//
	// Declarations
	//
	PPRT_SMCONTEXT context;
	PPRT_EVENTQUEUE queue;
	UINT tail;
	UINT16 currMaxInstance;
	UCHAR newQueueSize;
	PRT_EVENTDECL_INDEX EventIndex;
	//
	// Code
	//
	EventIndex = Event->valueUnion.primValue->value.ev;
	PRT_ASSERTMSG("Enqueued Event Cannot be a NULL event", PrtIsNullValue(Event));
	PRT_ASSERTMSG("Enqueued Event Cannot be a DEFAULT event", EventIndex != PrtDefaultEvent);

	context = PrtGetStateMachinePointer(Machine);
	currMaxInstance = context->Program->Events[EventIndex].MaxInstances;

	PrtAcquireLock(context);
	// queue is full resize the queue if possible
	if (context->EventQueue.Size == context->Program->Machines[context->InstanceOf].MaxSizeOfEventQueue)
	{
		PrtReleaseLock(context);
		PrtReportException(MaxQueueSizeExceeded, context);
		return;
	}

	//Check if we need to resize the queue
	if (context->EventQueue.IsFull)
	{
		newQueueSize = PrtResizeEventQueue(context);
#ifndef NDEBUG
		PrtTraceStep(context, traceQueueResize);
#endif
	}

	queue = &context->EventQueue;
	//check if Event.MaxInstances is NIL
	//check if the <event, payload> is in Queue
	if (currMaxInstance != MAX_INSTANCES_NIL && PrtIsEventMaxInstanceExceeded(queue, EventIndex, currMaxInstance, context->CurrentLengthOfEventQueue))
	{
		//
		//  Check if event is occuring more than maxinstances
		//
		PrtReleaseLock(context);
		PrtReportException(MaxInstanceExceeded, context);
		return;

	}


	tail = queue->Tail;

	PRT_ASSERT(!(context->CurrentLengthOfEventQueue == context->Program->Machines[context->InstanceOf].MaxSizeOfEventQueue && queue->IsFull));
	//
	// Add event to the queue
	//
	queue->Events[tail].Event = PrtCloneValue(Event);
	queue->Events[tail].Payload = PrtCloneValue(Arg);
	queue->Size++;
	queue->Tail = (tail + 1) % context->CurrentLengthOfEventQueue;
	queue->IsFull = (queue->Tail == queue->Head) ? TRUE : FALSE;

#ifndef NDEBUG
	PrtTraceStep(context, traceEnqueue);
#endif
	//
	// Now try to run the machine if its not running already
	//
	if (context->IsRunning)
	{
		PrtReleaseLock(context);
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

VOID
PrtPop(
__inout PPRT_SMCONTEXT		Context
)
/*++

Routine Description:

Kernel mode driver can call this rountine from the entry function only to
Pop the current state and return to parent state. This function should not be called
from exit function.


Arguments:

Context - Pointer to the State-Machine context.

Return Value:

NONE (VOID)

--*/
{
	//
	// Set operation performed to PopStatement
	//
	Context->LastOperation = PopStatement;

	//
	// Set Trigger to NULL indicating that the last statement executed was Pop 
	// TRIGGER access in Exit function after Pop is evaluated to NULL
	Context->Trigger.Event = PrtMkEventValue(PRT_NULL_VALUE);
	Context->Trigger.Payload = PrtMkDefaultValue(PrtMkPrimitiveType(PRT_KIND_NULL));

}

#define IS_SPECIAL_EVENT(Event)		( PrtIsNullValue(Event) || (Event->valueUnion.primValue->value.ev) == PrtDefaultEvent)
#define PAYLOAD_TYPE(Ctxt, Event)	(Ctxt)->Driver->Events[(Event)].Type

VOID
PrtRaise(
__inout PPRT_SMCONTEXT		Context,
__in PRT_EVENTDECL_INDEX	EventIndex,
__in PPRT_PACKED_VALUE		Arg
)
/*++

Routine Description:

Kernel mode driver can call this rountine from the entry function only to
Raise an internal event. This function should not be called
from exit function.


Arguments:

Context - Pointer to the State-Machine context.

EventIndex - Internal Private event raised.

Return Value:

NONE (VOID)

--*/
{
	PRT_ASSERTMSG("Raised Event Cannot be a NULL event", EventIndex != PrtNull);
	PRT_ASSERTMSG("Raised Event Cannot be a DEFAULT event", EventIndex != PrtDefaultEvent);

	//
	// Set operation to raiseStatement
	//
	Context->LastOperation = RaiseStatement;

	//TODO : Free the memory

	//
	// Set trigger to <raisedevent, null>
	//
	Context->Trigger.Payload = Arg;
	Context->Trigger.Event = ;

#ifndef NDEBUG
	PrtTraceStep(Context, traceRaiseEvent);
#endif
}


VOID
PrtCall(
__inout PPRT_SMCONTEXT		Context,
PRT_STATEDECL_INDEX			State
)
/*++

Routine Description:

Kernel mode driver can call this rountine from the entry function or exit function
only to goto State.


Arguments:

Context - Pointer to the State-Machine context.

State - Called State.

Return Value:

NONE (VOID)

--*/
{
	//
	// Push current state on top of the stack
	//
	PrtPushState(Context, TRUE);

	//
	// Set Trigger to NULL after a call edge
	//
	Context->Trigger = g_PrtNullTrigger;

	//
	// Change current state
	//
	Context->CurrentState = State;
	//
	// Last operation set to call Statement
	//
	Context->LastOperation = CallStatement;

	PrtUpdateTransitionHistory(Context, onCallS, PrtNull, State);
#ifndef NDEBUG
	PrtTraceStep(Context, traceCallStatement);
#endif
	return;
}


/*********************************************************************************

Private Functions

*********************************************************************************/
#ifdef KERNEL_MODE
VOID
PrtRunStateMachineWorkItemPassiveFlag(
_In_ PVOID IoObject,
_In_opt_ PVOID Context,
_In_ PIO_WORKITEM IoWorkItem
)
/*++

Routine Description:

Kernel mode driver calls this rountine from a worker thread to execute the state-machine
at Passive level.


Arguments:

IoObject - Not Used

Context - Pointer to the State-Machine context.

IoWorkItem - Not Used

Return Value:

NONE (VOID)

--*/
{
	UNREFERENCED_PARAMETER(IoWorkItem);
	UNREFERENCED_PARAMETER(IoObject);
	PRT_ASSERT(Context != NULL);
	KeAcquireSpinLock(&((PPRT_SMCONTEXT)Context)->StateMachineLock, &((PPRT_SMCONTEXT)Context)->Irql);

	//
	// Run Queued Statemachine at passive level
	//
	PrtRunStateMachine((PPRT_SMCONTEXT)Context, TRUE);
}

VOID
PrtRunStateMachineWorkItemNonBlocking(
_In_ PVOID IoObject,
_In_opt_ PVOID Context,
_In_ PIO_WORKITEM IoWorkItem
)
/*++

Routine Description:

Kernel mode driver calls this rountine from a worker thread to execute the state-machine
at Passive level.


Arguments:

IoObject - Not Used

Context - Pointer to the State-Machine context.

IoWorkItem - Not Used

Return Value:

NONE (VOID)

--*/
{
	UNREFERENCED_PARAMETER(IoWorkItem);
	UNREFERENCED_PARAMETER(IoObject);
	PRT_ASSERT(Context != NULL);
	KeAcquireSpinLock(&((PPRT_SMCONTEXT)Context)->StateMachineLock, &((PPRT_SMCONTEXT)Context)->Irql);

	//
	// Run Queued Statemachine
	//
	PrtRunStateMachine((PPRT_SMCONTEXT)Context, FALSE);
}

VOID
PrtEnqueueStateMachineAsWorkerItemPassiveFlag
(__in PPRT_SMCONTEXT		Context
)
{
	IoQueueWorkItemEx(Context->SmWorkItem, PrtRunStateMachineWorkItemPassiveFlag, DelayedWorkQueue, Context);
}

VOID
PrtEnqueueStateMachineAsWorkerItemNonBlocking
(__in PPRT_SMCONTEXT		Context
)
{
	IoQueueWorkItemEx(Context->SmWorkItem, PrtRunStateMachineWorkItemNonBlocking, DelayedWorkQueue, Context);
}
#endif

FORCEINLINE
BOOLEAN
PrtStateHasDefaultTransition(
__in PPRT_SMCONTEXT			Context
)
{
	return PrtGetCurrentStateDecl(Context).HasDefaultTransition;
}

FORCEINLINE
PRT_STATEDECL
PrtGetCurrentStateDecl(
__in PPRT_SMCONTEXT			Context
)
{
	return Context->Driver->Machines[Context->InstanceOf].States[Context->CurrentState];
}


BOOLEAN
PrtIsEntryFunRequiresPassiveLevel(
__in PPRT_SMCONTEXT		Context
)
{
	PRT_STATEDECL StateDecl = Context->Driver->Machines[Context->InstanceOf].States[Context->CurrentState];
	PRT_RUNTIMEFLAGS StateRuntimeFlags = StateDecl.Flags;
	if (StateRuntimeFlags & PrtEntryFunPassiveLevel)
	{
		return TRUE;
	}
	else
	{
		return FALSE;
	}
}

_IRQL_requires_(DISPATCH_LEVEL)
_Requires_lock_held_(Context->StateMachineLock)
_Releases_lock_(Context->StateMachineLock)
VOID
PrtRunStateMachine(
__inout _At_(Context->Irql, _IRQL_restores_)
PPRT_SMCONTEXT	    Context,
__in BOOLEAN			DoEntryOrExit
)

{
	//
	// Declarations
	//
	BOOLEAN freeStateMachine;
	PRT_TRIGGER e;
	BOOLEAN isLockAcq;
	PPRT_ACTIONDECL currActionDecl;
	//
	// Code
	//
	

	freeStateMachine = FALSE;
	e = g_PrtNullTrigger;
	isLockAcq = TRUE;

	Context->IsRunning = TRUE;

	//// If doEntry is false, then the current state of the machine
	//// has already been executed in a previous call to PrtStabilize
	if (!DoEntryOrExit)
	{
		//// We only return to the top of the loop if the state changed
		//// so the entry function should be executed
		DoEntryOrExit = TRUE;
		goto DoDequeue;
	}

	//Since we are not accessing event queues we can release the locks
	if (isLockAcq)
	{
		isLockAcq = FALSE;
		PrtReleaseLock(Context);
	}


DoEntryOrExitOrActionFunction:

	// SM is entering or re-entering the state (entry or action or exit)
	// update the current deferred and actions set
	PrtUpdateCurrentActionsSet(Context);
	PrtUpdateCurrentDeferredSet(Context);

	//// Step 1. Execute the entry function or Exit Function
	// 
	// Check whether to execute entry or exit function
	//
	if (Context->StateExecFun == PrtStateEntry)
	{
		// handle the case when we are entering a state with an unhandled event
		if (Context->ReturnTo == PrtEntryFunEnd && Context->Trigger.Event != PrtNull)
		{
			goto DoTakeTransition;
		}

		//
		// Execute Entry Function
		//

#ifndef NDEBUG
		if (Context->ReturnTo == PrtEntryFunStart)
			PrtTraceStep(Context, traceStateChange);
#endif

#ifdef KERNEL_MODE
		//Before Executing the State Entry Function check State Flags
		if (PrtIsEntryFunRequiresPassiveLevel(Context) && (KeGetCurrentIrql() != PASSIVE_LEVEL))
		{
			PrtEnqueueStateMachineAsWorkerItemPassiveFlag(Context);
			LEAVE;
		}
#endif
		//
		// Initialize context before executing entry function
		//
		Context->LastOperation = OtherStatement;
		//
		// Execute the Entry function
		//
		PrtGetEntryFunction(Context)(Context);

		//// Step 2. Handle any raised event -- call --- Pop -- others
		switch (Context->LastOperation)
		{
		case PopStatement:
			Context->StateExecFun = PrtStateExit;
			Context->ReturnTo = PrtExitFunStart;
			goto DoEntryOrExitOrActionFunction;
			break;
		case RaiseStatement:
			PRT_ASSERT(! PrtIsNullValue(Context->Trigger.Event));
			PRT_ASSERT(Context->Trigger.Event != PrtDefaultEvent);

			if (PrtIsTransitionPresent(Context->Trigger.Event, Context))
			{

				if (PrtIsCallTransition(Context, Context->Trigger.Event))
				{
					//
					// call transition so no exit function executed
					//
					goto DoTakeTransition;
				}

				else
				{
					// execute exit function

					Context->StateExecFun = PrtStateExit;
					Context->ReturnTo = PrtExitFunStart;
					goto DoEntryOrExitOrActionFunction;
				}
			}
			//
			// check if there is an action installed for this event
			//
			else if (PrtIsActionInstalled(Context->Trigger.Event, Context->CurrentActions))
			{
				Context->StateExecFun = PrtStateAction;
				Context->ReturnTo = PrtActionFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			//
			// Unhandled raised event
			//
			else
			{
				Context->StateExecFun = PrtStateExit;
				Context->ReturnTo = PrtExitFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			break;
		case CallStatement:
			Context->StateExecFun = PrtStateEntry;
			Context->ReturnTo = PrtEntryFunStart;
			goto DoEntryOrExitOrActionFunction;
			break;
		case OtherStatement:
			goto DoDequeue;
			break;
		default:
			break;
		}
	}
	else if (Context->StateExecFun == PrtStateExit)
	{
		//
		//Execute the exit function
		//

#ifdef KERNEL_MODE
		//Before Executing the State Exit Function check State Flags
		if (PrtIsExitFunRequiresPassiveLevel(Context) && (KeGetCurrentIrql() != PASSIVE_LEVEL))
		{
			PrtEnqueueStateMachineAsWorkerItemPassiveFlag(Context);
			LEAVE;
		}
#endif
		// Initialize context before executing exit function
		//
		Context->LastOperation = OtherStatement;
		//
		// Execute the exit function for the current state
		//
		if (PrtGetCurrentStateDecl(Context).ExitFunc != NULL)
		{
#ifndef NDEBUG
			if (Context->ReturnTo == PrtExitFunStart)
				PrtTraceStep(Context, traceExit);
#endif
			PrtGetExitFunction(Context)(Context);
		}


		//// Step 2. Handle call or others
		switch (Context->LastOperation)
		{
		case RaiseStatement:
		case PopStatement:
			PRT_ASSERTMSG("Pop or Raise is not allowed inside Exit Function", FALSE);
			break;
		case CallStatement:
			Context->StateExecFun = PrtStateEntry;
			Context->ReturnTo = PrtEntryFunStart;
			goto DoEntryOrExitOrActionFunction;
		case OtherStatement:
			goto DoTakeTransition;
		default:
			break;
		}

	}
	else if (Context->StateExecFun == PrtStateAction)
	{
		//
		// Execute the action installed corresponding to trigger
		//
		//
		// Get the current action decl
		currActionDecl = PrtGetAction(Context);
#ifdef KERNEL_MODE
		//Before Executing the Action Function check State Flags
		if (currActionDecl->IsActionFunPassiveLevel && (KeGetCurrentIrql() != PASSIVE_LEVEL))
		{
			PrtEnqueueStateMachineAsWorkerItemPassiveFlag(Context);
			LEAVE;
		}
#endif

#ifndef NDEBUG
		if (Context->ReturnTo == PrtActionFunStart)
			PrtTraceStep(Context, traceActions, (currActionDecl->Name));
#endif
		//
		// Initialize context before executing entry function
		//
		Context->LastOperation = OtherStatement;
		//
		// Execute the Entry function
		//
		currActionDecl->ActionFun(Context);

		//// Step 2. Handle any raised event -- call --- Pop -- others
		switch (Context->LastOperation)
		{
		case PopStatement:
			Context->StateExecFun = PrtStateExit;
			Context->ReturnTo = PrtExitFunStart;
			goto DoEntryOrExitOrActionFunction;
			break;
		case RaiseStatement:
			PRT_ASSERT(!PrtIsNullValue(Context->Trigger.Event));
			PRT_ASSERT(Context->Trigger.Event->valueUnion.primValue->value.ev != PrtDefaultEvent);

			if (PrtIsTransitionPresent(Context->Trigger.Event, Context))
			{

				if (PrtIsCallTransition(Context, Context->Trigger.Event))
				{
					//
					// call transition so no exit function executed
					//
					goto DoTakeTransition;
				}

				else
				{
					// execute exit function

					Context->StateExecFun = PrtStateExit;
					Context->ReturnTo = PrtExitFunStart;
					goto DoEntryOrExitOrActionFunction;
				}
			}
			//
			// check if there is an action installed for this event
			//
			else if (PrtIsActionInstalled(Context->Trigger.Event, Context->CurrentActions))
			{
				Context->StateExecFun = PrtStateAction;
				Context->ReturnTo = PrtActionFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			//
			// Unhandled raised event
			//
			else
			{
				Context->StateExecFun = PrtStateExit;
				Context->ReturnTo = PrtExitFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			break;
		case CallStatement:
			Context->StateExecFun = PrtStateEntry;
			Context->ReturnTo = PrtEntryFunStart;
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
		PrtAcquireLock(Context);
	}

	e = PrtDequeueEvent(Context);

	//Successfully dequeued an event
	if (PrtIsNullValue(e.Event))
	{
		//Release Lock
		isLockAcq = FALSE;
		PrtReleaseLock(Context);
		if (PrtIsCallTransition(Context, e.Event))
		{
			goto DoTakeTransition;
		}
		//
		// Transition corresponding to dequeued event (Ankush : this takes care of local priority of e over actions)
		//
		else if (PrtIsTransitionPresent(Context->Trigger.Event, Context))
		{
			Context->StateExecFun = PrtStateExit;
			Context->ReturnTo = PrtExitFunStart;
			goto DoEntryOrExitOrActionFunction;
		}
		//
		// check if there is an action installed for this event
		//
		else if (PrtIsActionInstalled(Context->Trigger.Event, Context->CurrentActions))
		{
			Context->StateExecFun = PrtStateAction;
			Context->ReturnTo = PrtActionFunStart;
			goto DoEntryOrExitOrActionFunction;
		}
		//
		// Unhandled dequeued event
		//
		else
		{
			Context->StateExecFun = PrtStateExit;
			Context->ReturnTo = PrtExitFunStart;
			goto DoEntryOrExitOrActionFunction;
		}

	}
	// failed to dequeue an event -> two possibility either take default branch(if available) else block
	else if (PrtStateHasDefaultTransition(Context))
	{
		//release lock
		isLockAcq = FALSE;
		PrtReleaseLock(Context);

		//TODO Free event and Payload

		Context->Trigger.Event = (ULONG32)PrtDefaultEvent;
		Context->Trigger.Payload = PrtMkNullValue();
		Context->StateExecFun = PrtStateExit;
		Context->ReturnTo = PrtExitFunStart;
		goto DoEntryOrExitOrActionFunction;
	}
	else
	{
		Context->IsRunning = FALSE;
		//check if the reference count is zero
		/*if (Context->RefCount == 0)
		{
			freeStateMachine = TRUE;
		}*/ 
		// TODO Fix ME 
		//Release Lock
		isLockAcq = FALSE;
		PrtReleaseLock(Context);

		//// Step 3.b. Safely Remove the statemachine.
		if (freeStateMachine)
		{
			PrtDelete(Context);
		}
		return;

	}


DoTakeTransition:

	if ((Context->Trigger.Event == g_PrtNullTrigger.Event))
	{
		//
		// The last statement executed was a pop statement
		//
		PrtPopState(Context, TRUE);


#ifndef NDEBUG
		PrtTraceStep(Context, tracePop);
#endif

		if (Context->ReturnTo == PrtEntryFunEnd)
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
	else if (Context->Trigger.Event == PrtDefaultEvent)
	{
		//
		// Take After transition
		//
		PrtTakeDefaultTransition(Context);
		goto DoEntryOrExitOrActionFunction;
	}
	else
	{
		//
		// Trigger is non-null and hence its a raise or dequeue or unhandled event
		//
		PrtTakeTransition(Context, Context->Trigger.Event);

		goto DoEntryOrExitOrActionFunction;

	}

	return;

}

VOID
PrtTakeDefaultTransition(
__inout PPRT_SMCONTEXT		Context
)
{
	//
	// Declarations
	//
	ULONG i;
	UINT16 nTransitions;
	PRT_TRANSDECL* transTable;

	//
	// Code
	//


	transTable = PrtGetTransTable(Context, Context->CurrentState, &nTransitions);

	for (i = 0; i < nTransitions; ++i)
	{
		//check if transition is After
		if (transTable[i].EventIndex == PrtDefaultEvent)
		{
			//check if its a call transition
			if (transTable[i].IsPush != FALSE)
			{
				Context->ReturnTo = PrtEntryFunEnd;
				PrtPushState(Context, FALSE);
			}

			//update the state
			Context->CurrentState = transTable[i].Destination;
			Context->ReturnTo = PrtEntryFunStart;
			Context->StateExecFun = PrtStateEntry;
			return;
		}
	}

	return;

}



VOID
PrtTakeTransition(
__inout PPRT_SMCONTEXT		Context,
__in PRT_EVENTDECL_INDEX	EventIndex
)
{
	//
	// Declarations
	//
	ULONG i;
	UINT16 nTransitions;
	PRT_TRANSDECL* transTable;

	//
	//code
	//

	transTable = PrtGetTransTable(Context, Context->CurrentState, &nTransitions);

	for (i = 0; i < nTransitions; ++i)
	{
		if ((transTable[i].EventIndex == EventIndex))
		{

			//check if its a call transition
			if (transTable[i].IsPush != FALSE)
			{
#ifndef NDEBUG
				PrtTraceStep(Context, traceCallEdge);
#endif
				Context->ReturnTo = PrtEntryFunEnd;
				PrtPushState(Context, FALSE);
			}
			
			// change CurrentState state and set returnTo to smfEntryFunStart 
			// next to execute is the entry function of the destination state
			Context->CurrentState = transTable[i].Destination;
			Context->ReturnTo = PrtEntryFunStart;
			Context->StateExecFun = PrtStateEntry;
			return;
		}
	}
	if (Context->CallStack.Length > 0)
	{
		PrtPopState(Context, FALSE);
#ifndef NDEBUG
		PrtTraceStep(Context, traceUnhandledEvent);
#endif
	}
	else
	{
		if (Context->Trigger.Event == 1) // 1 == Event_delete in the generated code
		{
#ifndef NDEBUG
			PrtTraceStep(Context, traceDelete);
#endif
			PrtReportException(UnhandledEvent, Context); // Needs to be fixed and update the delete logic.
		}
		else
		{
			//Exception
			PrtReportException(UnhandledEvent, Context);
		}
	}

	return;
}

VOID
PrtPushState(
__inout PPRT_SMCONTEXT		Context,
__in	BOOLEAN				isCallStatement
)
{
	//
	// Declarations
	//
	UINT16 i;
	UINT16 packSize;
	UINT16 length;
	PRT_EVENTDECL_INDEX_PACKEDTABLE currDef;
	PRT_ACTIONDECL_INDEX_PACKEDTABLE currActions;
	PRT_TRANSDECL_INDEX_PACKEDTABLE currTransitions;

	//
	// Code
	//
	packSize = PrtGetPackSize(Context);
	length = Context->CallStack.Length;
	currDef = PrtGetDeferredPacked(Context, Context->CurrentState);
	currActions = PrtGetActionsPacked(Context, Context->CurrentState);
	currTransitions = Context->Program->Machines[Context->InstanceOf].States[Context->CurrentState].TransitionsPacked;

	PRT_ASSERTMSG("Call Stack Overflow", length < PRT_MAX_CALL_DEPTH);
	//
	// push <state, trigger, arg, ReturnTo, StateExecFun, defSet, ActSet>
	//
	Context->CallStack.StatesStack[length].StateIndex = Context->CurrentState;
	Context->CallStack.StatesStack[length].Trigger.Event = Context->Trigger.Event;
	Context->CallStack.StatesStack[length].Trigger.Payload = Context->Trigger.Payload;
	Context->CallStack.StatesStack[length].ReturnTo = Context->ReturnTo;
	Context->CallStack.StatesStack[length].StateExecFun = Context->StateExecFun;
	Context->CallStack.StatesStack[length].InheritedDef = (PRT_EVENTDECL_INDEX_PACKEDTABLE)PrtClonePackedSet(Context->InheritedDeferred, packSize);
	Context->CallStack.StatesStack[length].InheritedAct = (PRT_ACTIONDECL_INDEX_PACKEDTABLE)PrtClonePackedSet(Context->InheritedActions, packSize);

	Context->CallStack.Length = length + 1;

	// Update the defered set inherited by state-machine
	// D = (D + d) - a - e
	for (i = 0; i < packSize; ++i)
	{

		// Update the actions set inherited by state-machine
		// A = (A - d) + a - e
		Context->InheritedActions[i] &= ~currDef[i]; // A - d
		Context->InheritedActions[i] |= currActions[i]; // + a
		Context->InheritedActions[i] &= ~currTransitions[i]; // -e

		if (isCallStatement)
		{
			Context->InheritedDeferred[i] = ~Context->InheritedActions[i]; // !a
		}
		else
		{
			Context->InheritedDeferred[i] |= currDef[i]; // D + d
			Context->InheritedDeferred[i] &= ~currActions[i]; // - a 
			Context->InheritedDeferred[i] &= ~currTransitions[i]; // - e
		}
	}
}

VOID
PrtPopState(
__inout PPRT_SMCONTEXT		Context,
__in BOOLEAN				RestoreTrigger
)
{
	//
	// Declarations
	//
	UINT16 i;
	UINT16 packSize;
	UINT16 length;
	PRT_EVENTDECL_INDEX *def;
	PRT_STACKSTATE_INFO poppedState;
	// 
	// Code
	//
	i = 0;
	packSize = PrtGetPackSize(Context);
	length = Context->CallStack.Length;
	def = NULL;

	PRT_ASSERTMSG("PopState Called on Empty Stack", length > 0);

	Context->CallStack.Length = length - 1;
	poppedState = Context->CallStack.StatesStack[length - 1];
	Context->CurrentState = poppedState.StateIndex;

	//
	// Restore the Deferred Set and Actions Set
	//
	for (i = 0; i<packSize; i++)
	{
		Context->InheritedDeferred[i] = poppedState.InheritedDef[i];
		Context->InheritedActions[i] = poppedState.InheritedAct[i];
	}
	//
	// Free the allocated memory for def and act state
	//
	PrtFreeMemory(poppedState.InheritedDef);
	PrtFreeMemory(poppedState.InheritedAct);

	//
	// Restore the trigger value
	//
	if (RestoreTrigger)
	{
		//TODO : Free the current event and arg.

		Context->Trigger.Event = poppedState.Trigger.Event;
		Context->Trigger.Payload = poppedState.Trigger.Payload;
		Context->ReturnTo = poppedState.ReturnTo;
		Context->StateExecFun = poppedState.StateExecFun;
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
		PRT_ASSERTMSG("Unhandled Event in Exit Function", poppedState.StateExecFun == PrtStateEntry || poppedState.StateExecFun == PrtStateAction);

		//
		// assert that we are popping back because of an call-edge and not because of a call statement (implicit pop)
		//
		if (poppedState.ReturnTo != PrtEntryFunEnd)
		{
			PrtReportException(UnhandledEventInCallS, Context);
		}

		//check if there is a push transition defined for the unhandled event
		if (PrtIsTransitionPresent(Context->Trigger.Event, Context) && PrtIsCallTransition(Context, Context->Trigger.Event))
		{
			Context->StateExecFun = PrtStateEntry;
			Context->ReturnTo = PrtEntryFunEnd;
		}
		else
		{
			Context->StateExecFun = PrtStateExit;
			Context->ReturnTo = PrtExitFunStart;
		}
	}
	return;
}


PRT_TRIGGER
PrtDequeueEvent(
__inout PPRT_SMCONTEXT	Context
)
{
	//
	// Declarations
	//
	INT queueLength;
	PPRT_EVENTQUEUE queue;
	PRT_EVENTDECL_INDEX_PACKEDTABLE deferred;
	INT i, head;
	PRT_TRIGGER e;

	//
	// Code
	//

	queueLength = Context->CurrentLengthOfEventQueue;
	queue = &Context->EventQueue;
	deferred = PrtGetDeferredPacked(Context, Context->CurrentState);
	head = queue->Head;
	e.Event = NULL; // TODO : Set to NULL event
	e.Payload = NULL; //TODO : Set to NULL 

	PRT_ASSERT(queue->Size <= queueLength);
	PRT_ASSERT(queue->Size >= 0);
	PRT_ASSERT(queue->Head >= 0);
	PRT_ASSERT(queue->Tail >= 0);

	if (PrtIsQueueEmpty(queue)) {
		return e;
	}

	//
	// Find the element to dequeue
	//
	for (i = 0; i < queue->Size; i++) {
		INT index = (head + i) % queueLength;
		e = queue->Events[index];
		if (!PrtIsEventDeferred(e.Event, Context->CurrentDeferred)) {
			break;
		}
	}

	//
	// Check if not found
	//
	if (i == queue->Size) {
		e.Event = NULL; // TODO : Set to NULL event
		e.Payload = NULL; //TODO : Set to NULL 
		return e;
	}

	//
	// Collapse the event queue on the removed event
	// by moving the previous elements forward.
	//
	for (; i > 0; i--) {
		INT index = (head + i) % queueLength;
		INT prev = (index - 1 + queueLength) % queueLength;
		queue->Events[index] = queue->Events[prev];
	}

	//
	// Adjust the queue size
	//
	queue->Head = (queue->Head + 1) % queueLength;
	queue->IsFull = FALSE;
	queue->Size--;

	//
	// Free old payload, if any.
	//
	//TODO : Free old payload

	//
	// Store the event and argument
	//
	Context->Trigger.Event = e.Event;
	Context->Trigger.Payload = e.Payload;

#ifndef NDEBUG
	PrtTraceStep(Context, traceDequeue);
#endif

	PRT_ASSERT(queue->Size <= queueLength);
	PRT_ASSERT(queue->Size >= 0);
	PRT_ASSERT(queue->Head >= 0);
	PRT_ASSERT(queue->Tail >= 0);

	return e;
}

/*********************************************************************************

Machine Managerment Functions

*********************************************************************************/


VOID
PrtRemoveMachine(
__in PPRT_SMCONTEXT			Context
)
{
#ifdef KERNEL_MODE
	IoFreeWorkItem(Context->SmWorkItem);
#endif
	if (Context->ExtContext->FreeThis)
	{
		PrtFreeMemory(Context->ExtContext->PExMem);
	}
	PrtFreeSMContext(Context);
}

/*********************************************************************************

Helper Functions

*********************************************************************************/
FORCEINLINE
_Acquires_lock_(Context->StateMachineLock)
_IRQL_raises_(DISPATCH_LEVEL)
VOID
PrtAcquireLock(
_In_ _At_(Context->Irql, _IRQL_saves_)
PPRT_SMCONTEXT	Context
)
{

#ifdef KERNEL_MODE
	KeAcquireSpinLock(&Context->StateMachineLock, &Context->Irql);
#else
	AcquireSRWLockExclusive(&Context->StateMachineLock);
#endif

}

FORCEINLINE
_IRQL_requires_(DISPATCH_LEVEL)
_Requires_lock_held_(Context->StateMachineLock)
_Releases_lock_(Context->StateMachineLock)
VOID
PrtReleaseLock(
_In_ _At_(Context->Irql, _IRQL_restores_)
PPRT_SMCONTEXT	Context
)
{
#ifdef KERNEL_MODE
	KeReleaseSpinLock(&Context->StateMachineLock, Context->Irql);
#else
	ReleaseSRWLockExclusive(&Context->StateMachineLock);
#endif
}

FORCEINLINE
VOID
PrtInitializeLock(
PPRT_SMCONTEXT				Context
)
{
#ifdef KERNEL_MODE
	KeInitializeSpinLock(&Context->StateMachineLock);
#else
	InitializeSRWLock(&Context->StateMachineLock);
#endif
}

FORCEINLINE
PVOID
PrtAllocateMemory(
UINT						SizeOf
)
{
#ifdef KERNEL_MODE
	return ExAllocatePoolWithTag(NonPagedPool, SizeOf, '1gaT');
#else
	return malloc(SizeOf);
#endif
}

FORCEINLINE
ULONG_PTR
PrtAllocateType(
__in PPRT_PROGRAMDECL			Driver,
__in PRT_TYPEDECL_INDEX			Type)
{
	PVOID mem = PrtAllocateMemory((UINT)Driver->Types[Type].Size);
	PRT_ASSERTMSG("Failed to create An Instance of a Complex type", mem != NULL);
	return (ULONG_PTR)mem;
}


FORCEINLINE
VOID
PrtFreeMemory(
PVOID						PointerTo
)
{
#ifdef KERNEL_MODE
	ExFreePool(PointerTo);
#else
	free(PointerTo);
#endif
}

FORCEINLINE
BOOLEAN
PrtIsIndexInSet(
__in ULONG32 Index,
PRT_BIT_SET Set
)
{
	// TODO: Why is this in a Try/Catch in PrtIsEventDeferred? Does the TryCatch buy you something on an index outside of the array?
	return ((Set[Index / (sizeof(PRT_EVENTDECL_INDEX) * 8)] & (1 << (Index % (sizeof(PRT_EVENTDECL_INDEX) * 8)))) != 0);
}


FORCEINLINE
BOOLEAN
PrtIsEventDeferred(
__in PRT_EVENTDECL_INDEX	EventIndex,
PRT_EVENTDECL_INDEX_PACKEDTABLE
DeferredSet
)
{
	//
	// Declarations
	//
	BOOLEAN isDeferred;

	//
	// Code
	//

	isDeferred = FALSE;
	if
		(
		((DeferredSet[EventIndex / (sizeof(PRT_EVENTDECL_INDEX) * 8)] & (1 << (EventIndex % (sizeof(PRT_EVENTDECL_INDEX) * 8)))) != 0)
		)
	{
		isDeferred = TRUE;
	}

	return isDeferred;
}

FORCEINLINE
BOOLEAN
PrtIsActionInstalled(
__in PRT_EVENTDECL_INDEX	EventIndex,
PRT_ACTIONDECL_INDEX_PACKEDTABLE
ActionSet
)
{
	//
	// Declarations
	//
	BOOLEAN isActionInstalled;

	//
	// Code
	//

	isActionInstalled = FALSE;
	if
		(
		((ActionSet[EventIndex / (sizeof(PRT_EVENTDECL_INDEX) * 8)] & (1 << (EventIndex % (sizeof(PRT_EVENTDECL_INDEX) * 8)))) != 0)
		)
	{
		isActionInstalled = TRUE;
	}



	return isActionInstalled;
}


FORCEINLINE
UINT16
PrtGetPackSize(
__in PPRT_SMCONTEXT			Context
)
{
	ULONG32 nEvents = Context->Program->NEvents;
	return (UINT16)(((nEvents == 0) || (nEvents % (sizeof(PRT_EVENTDECL_INDEX) * 8) != 0))
		? (1 + (nEvents / (sizeof(PRT_EVENTDECL_INDEX) * 8)))
		: (nEvents / (sizeof(PRT_EVENTDECL_INDEX) * 8)));
}

FORCEINLINE
BOOLEAN
PrtIsQueueEmpty(
__in PPRT_EVENTQUEUE		Queue
)
{
	return !Queue->IsFull && Queue->Head == Queue->Tail;
}

FORCEINLINE
PPRT_EXITFUN
PrtGetExitFunction(
__in PPRT_SMCONTEXT			Context
)
{
	return (PPRT_EXITFUN)Context->Program->Machines[Context->InstanceOf].States[Context->CurrentState].ExitFunc;
}

FORCEINLINE
PPRT_ENTRYFUN
PrtGetEntryFunction(
__in PPRT_SMCONTEXT			Context
)
{
	return (PPRT_ENTRYFUN)Context->Program->Machines[Context->InstanceOf].States[Context->CurrentState].EntryFunc;
}



FORCEINLINE
PPRT_ACTIONDECL
PrtGetAction(
__in PPRT_SMCONTEXT			Context
)
{

	PRT_EVENTDECL_INDEX currEvent = Context->Trigger.Event;
	BOOLEAN isActionInstalled = FALSE;
	INT i, nActions;
	PRT_STATESTACK currStack;
	PRT_STATEDECL_TABLE stateTable;
	PRT_STATEDECL_INDEX topOfStackState;

	PPRT_ACTIONDECL actionDecl = NULL;

	//check if action is defined for the current state
	isActionInstalled = PrtIsActionInstalled(currEvent, PrtGetCurrentStateDecl(Context).ActionsPacked);
	if (isActionInstalled)
	{
		//
		// get action function
		//
		nActions = PrtGetCurrentStateDecl(Context).NActions;
		for (i = 0; i < nActions; i++)
		{
			if (PrtGetCurrentStateDecl(Context).Actions[i].EventIndex == currEvent)
			{
				actionDecl = &PrtGetCurrentStateDecl(Context).Actions[i];
				return actionDecl;
			}
		}
	}

	//
	// Scan the parent states
	//
	currStack = Context->CallStack;
	stateTable = Context->Program->Machines[Context->InstanceOf].States;
	for (i = currStack.Length - 1; i >= 0; i--)
	{
		topOfStackState = currStack.StatesStack[i].StateIndex;
		isActionInstalled = PrtIsActionInstalled(currEvent, stateTable[topOfStackState].ActionsPacked);
		if (isActionInstalled)
		{
			//
			// get action function
			//
			nActions = stateTable[topOfStackState].NActions;
			for (i = 0; i < nActions; i++)
			{
				if (stateTable[topOfStackState].Actions[i].EventIndex == currEvent)
				{
					actionDecl = &stateTable[topOfStackState].Actions[i];
					return actionDecl;
				}
			}
		}
	}


	PRT_ASSERT(actionDecl != NULL);
	return actionDecl;
}


FORCEINLINE
PRT_EVENTDECL_INDEX_PACKEDTABLE
PrtGetDeferredPacked(
__in PPRT_SMCONTEXT			Context,
__in PRT_STATEDECL_INDEX	StateIndex
)
{
	PRT_EVENTSETDECL_INDEX evSet = Context->Program->Machines[Context->InstanceOf].States[StateIndex].Defers;
	return Context->Program->Machines[Context->InstanceOf].EventSets[evSet].EventIndexPackedTable;
}

FORCEINLINE
PRT_ACTIONDECL_INDEX_PACKEDTABLE
PrtGetActionsPacked(
__in PPRT_SMCONTEXT			Context,
__in PRT_STATEDECL_INDEX	StateIndex
)
{
	return Context->Program->Machines[Context->InstanceOf].States[StateIndex].ActionsPacked;
}


FORCEINLINE
PRT_TRANSDECL_TABLE
PrtGetTransTable(
__in PPRT_SMCONTEXT			Context,
__in PRT_STATEDECL_INDEX	StateIndex,
__out UINT16				*NTransitions
)
{
	*NTransitions = Context->Program->Machines[Context->InstanceOf].States[StateIndex].NTransitions;
	return Context->Program->Machines[Context->InstanceOf].States[StateIndex].Transitions;
}

FORCEINLINE
PRT_MACHINE_HANDLE
PrtGetStateMachineHandle(
__in PPRT_SMCONTEXT			Context
)
/*++

Routine Description:

Kernel mode driver (P Runtime code) can call this rountine
to get state-machine handle from Pointer to the state-machine.


Arguments:

Context - Pointer to the State-Machine context.

Return Value:

PRT_MACHINE_HANDLE - Handle to the State-Machine.

--*/
{
	return (PRT_MACHINE_HANDLE)((ULONG_PTR)Context ^ 0x11);
}

FORCEINLINE
PPRT_SMCONTEXT
PrtGetStateMachinePointer(
__in PRT_MACHINE_HANDLE				Handle
)
/*++

Routine Description:

Kernel mode driver (P Runtime code) can call this rountine
to get state-machine pointer from Handle to the state-machine.


Arguments:

handle - Handle to the State-Machine.

Return Value:

PPRT_SMCONTEXT - Pointer to the StateMachine context

--*/
{
	//
	// Declarations
	//
	PPRT_SMCONTEXT tempSMPointer;

	//
	// Code
	//

	tempSMPointer = (PPRT_SMCONTEXT)(Handle ^ 0x11);
	//
	//Check if the State Machine is still pointing to valid state-machine
	//
	if (tempSMPointer->StateMachineSignature != PrtStateMachine_Signature)
	{
		//
		// Signature doesnt match so the state-machine is freed
		// Exception : Trying to dereference an invalid machine pointer
		PrtReportException(IllegalAccess, NULL);
	}

	return tempSMPointer;
}


BOOLEAN
PrtIsCallTransition(
PPRT_SMCONTEXT			Context,
PRT_EVENTDECL_INDEX		Event
)
{
	//
	// Declarations
	//
	ULONG i;
	UINT16 nTransitions;
	PRT_TRANSDECL* transTable;
	BOOLEAN isCallTransition;

	//
	// Code
	//

	transTable = PrtGetTransTable(Context, Context->CurrentState, &nTransitions);
	isCallTransition = FALSE;
	for (i = 0; i < nTransitions; ++i)
	{
		//check if transition is Call
		if (transTable[i].IsPush && transTable[i].EventIndex == Event)
		{
			isCallTransition = TRUE;
		}
	}



	return isCallTransition;
}

FORCEINLINE
BOOLEAN
PrtIsTransitionPresent(
__in PRT_EVENTDECL_INDEX	EventIndex,
__in PPRT_SMCONTEXT			Context
)
{
	if ((PrtGetCurrentStateDecl(Context).TransitionsPacked[EventIndex / (sizeof(PRT_EVENTDECL_INDEX) * 8)] & (1 << (EventIndex % (sizeof(PRT_EVENTDECL_INDEX) * 8)))) != 0)
	{
		return TRUE;
	}

	return FALSE;
}

PVOID
PrtClonePackedSet(
PVOID					PackedSet,
UINT					Size
)
{
	PULONG32 clone;
	UINT i;

	clone = (PULONG32)PrtAllocateMemory(Size * sizeof(ULONG32));
	for (i = 0; i<Size; i++)
	{
		clone[i] = ((PULONG32)PackedSet)[i];
	}

	return clone;
}

VOID
PrtUpdateCurrentActionsSet(
PPRT_SMCONTEXT			Context
)
{
	UINT i;
	PRT_ACTIONDECL_INDEX_PACKEDTABLE currActions;
	PRT_TRANSDECL_INDEX_PACKEDTABLE currTransitions;
	PRT_EVENTDECL_INDEX_PACKEDTABLE currDefSet;
	UINT16 packSize;

	packSize = PrtGetPackSize(Context);
	currActions = PrtGetCurrentStateDecl(Context).ActionsPacked;
	currTransitions = PrtGetCurrentStateDecl(Context).TransitionsPacked;
	currDefSet = PrtGetDeferredPacked(Context, Context->CurrentState);
	//
	// A = (A -d) + a - e
	//
	for (i = 0; i < packSize; i++)
	{
		Context->CurrentActions[i] = Context->InheritedActions[i] & ~currDefSet[i]; // A - d
		Context->CurrentActions[i] |= currActions[i];
		Context->CurrentActions[i] &= ~currTransitions[i];
	}
}

VOID
PrtUpdateCurrentDeferredSet(
PPRT_SMCONTEXT			Context
)
{
	UINT i;
	PRT_ACTIONDECL_INDEX_PACKEDTABLE currActions;
	PRT_TRANSDECL_INDEX_PACKEDTABLE currTransitions;
	PRT_EVENTDECL_INDEX_PACKEDTABLE currDefSet;
	UINT16 packSize;

	packSize = PrtGetPackSize(Context);
	currActions = PrtGetCurrentStateDecl(Context).ActionsPacked;
	currTransitions = PrtGetCurrentStateDecl(Context).TransitionsPacked;
	currDefSet = PrtGetDeferredPacked(Context, Context->CurrentState);
	//
	// D = (D + d) - a - e
	//
	for (i = 0; i < packSize; i++)
	{
		Context->CurrentDeferred[i] = Context->InheritedDeferred[i] | currDefSet[i]; // D + d
		Context->CurrentDeferred[i] &= ~currActions[i]; // - a
		Context->CurrentDeferred[i] &= ~currTransitions[i]; // -e
	}
}


PRT_INT16
PrtResizeEventQueue(
__in PPRT_SMCONTEXT Context
)
{
	UCHAR maxEventQueueSize = Context->Program->Machines[Context->InstanceOf].MaxSizeOfEventQueue;
	UCHAR currEventQueueSize = Context->CurrentLengthOfEventQueue;
	UCHAR newQueueSize = Context->CurrentLengthOfEventQueue * 2 > maxEventQueueSize ? maxEventQueueSize : Context->CurrentLengthOfEventQueue * 2;
	PPRT_TRIGGER oldQueue = Context->EventQueue.Events;
	UINT16 oldHead = Context->EventQueue.Head;
	UINT16 oldTail = Context->EventQueue.Tail;
	PPRT_TRIGGER newQueue = (PPRT_TRIGGER)PrtAllocateMemory(newQueueSize * sizeof(PRT_TRIGGER));
	UINT16 newHead = 0;
	UINT16 newTail = 0;


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
	Context->EventQueue.Events = newQueue;
	Context->EventQueue.Head = newHead;
	Context->EventQueue.Size = newTail - newHead;
	Context->EventQueue.Tail = newTail;
	Context->EventQueue.IsFull = FALSE;
	Context->CurrentLengthOfEventQueue = newQueueSize;

	//Release the older Queue
	PrtFreeMemory(oldQueue);

	return Context->CurrentLengthOfEventQueue;
}

VOID
PrtFreeSMContext(
PPRT_SMCONTEXT			Context
)
{
	if (Context->CurrentActions != NULL)
	{
		PrtFreeMemory(Context->CurrentActions);
	}

	if (Context->CurrentDeferred != NULL)
	{
		PrtFreeMemory(Context->CurrentDeferred);
	}

	if (Context->EventQueue.Events != NULL)
	{
		PrtFreeMemory(Context->EventQueue.Events);
	}

	if (Context->ExtContext != NULL)
	{
		PrtFreeMemory(Context->ExtContext);
	}

	if (Context->InheritedActions != NULL)
	{
		PrtFreeMemory(Context->InheritedActions);
	}

	if (Context->InheritedDeferred != NULL)
	{
		PrtFreeMemory(Context->InheritedDeferred);
	}

	if (Context->Values != NULL)
	{
		UINT i;
		PRT_MACHINEDECL *mdecl = &(Context->Program->Machines[Context->InstanceOf]);

		for (i = 0; i < mdecl->NVars; i++) {
			PrtFreeValue(Context->Values[i]);
		}
		PrtFreeMemory(Context->Values);
	}
	PrtFreeMemory(Context);
}

PPRT_EXCONTEXT
PrtGetForeignContext(
__in PRT_MACHINE_HANDLE SmHandle
)
{
	PPRT_SMCONTEXT context;

	context = PrtGetStateMachinePointer(SmHandle);
	return context->ExtContext;
}

