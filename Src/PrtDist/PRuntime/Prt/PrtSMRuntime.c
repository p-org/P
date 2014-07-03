/*********************************************************************************

Copyright (c) Microsoft Corporation

Module Name:

SmfRuntime.c

Abstract:
This module contains implementation of P runtime for executing and maintaining the
statemachines

Environment:

Kernel mode only.

***********************************************************************************/

#include "PrtSMPrivate.h"
#include "Config\PrtConfig.h"
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
SmfInitAttributes(
__inout PSMF_MACHINE_ATTRIBUTES Attributes,
__in PDEVICE_OBJECT				PDeviceObj,
__in PSMF_DRIVERDECL			Driver,
__in SMF_MACHINEDECL_INDEX		InstanceOf,
__in PSMF_PACKED_VALUE			Arg,
__in PVOID						ConstructorParam
)
#else
VOID
SmfInitAttributes(
__inout PSMF_MACHINE_ATTRIBUTES Attributes,
__in PSMF_DRIVERDECL			Driver,
__in SMF_MACHINEDECL_INDEX		InstanceOf,
__in PSMF_PACKED_VALUE			Arg,
__in PVOID						ConstructorParam
)
#endif
/*++

Routine Description:

Kernel mode driver can call this rountine to Initialize Machine_Attribute structure,
which is used for creating new state-machine.
This function should be called after creating Attribute structure and before calling
SmfCreate() function to creating the statemachine


Arguments:

Attributes - Pointer to the Attribute Structure to be Initialized.

PDeviceObj - Pointer to the Device which is trying to create and use this state-machine

Driver - Pointer to driver declaration of the current program.

InstanceOf - Index into the Machine Type Table in driver decl.

Return Value:

NONE (VOID)

--*/

{
	TRY{

		Attributes->Driver = Driver;
		Attributes->InstanceOf = InstanceOf;
		Attributes->Arg = *Arg;
		Attributes->ConstructorParam = ConstructorParam;
#ifdef KERNEL_MODE
		Attributes->PDeviceObj = PDeviceObj;
#endif

	} FINALLY{

	}

	return;
}


NTSTATUS
SmfCreate(
__in PSMF_MACHINE_ATTRIBUTES	InitAttributes,
__out PSMF_MACHINE_HANDLE		PSmHandle
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
	PSMF_SMCONTEXT context;
	NTSTATUS status;
	ULONG i;

	//
	// Code
	//
	TRY{

		status = STATUS_SUCCESS;
		nVars = InitAttributes->Driver->Machines[InitAttributes->InstanceOf].NVars;
		eQSize = SMF_QUEUE_LEN_DEFAULT;

		//
		// Allocate memory for state machine context
		//
		context = (PSMF_SMCONTEXT)SmfAllocateMemory(sizeof(SMF_SMCONTEXT));

		if (context == NULL)
		{
			status = STATUS_NO_MEMORY;
			LEAVE;
		}

		//
		// Set State Machine Signature
		//
		context->StateMachineSignature = SmfStateMachine_Signature;

		//
		// Initialize the transition history array
		//
		context->TransHistoryIndex = 0;
		memset(context->TransitionHistory, 0, SMF_MAX_HISTORY_DEPTH * sizeof(SMF_TRANSHISTORY));

		//
		// Initialize Machine Identity
		//
		context->Driver = InitAttributes->Driver;
		context->InstanceOf = InitAttributes->InstanceOf;

		//
		// Initialize Machine Internal Variables
		//
		context->CurrentState = context->Driver->Machines[context->InstanceOf].Initial;
		context->This = SmfGetStateMachineHandle(context);
		*PSmHandle = SmfGetStateMachineHandle(context);
		context->IsRunning = FALSE;
		context->LastOperation = OtherStatement;
		context->Trigger.Event = SmfNull;
		context->Trigger.Arg = InitAttributes->Arg;
		context->ReturnTo = SmfEntryFunStart;
		context->StateExecFun = SmfStateEntry;

		//
		// Allocate memory for local variables
		//
		context->Values = nVars == 0 ? NULL : (SMF_VARVALUE_TABLE)SmfAllocateMemory(nVars*sizeof(SMF_VARVALUE));
		//
		// If failed to allocate memory
		//
		if ((nVars > 0) && (context->Values == NULL))
		{
			SmfFreeSMContext(context);
			status = STATUS_NO_MEMORY;
			LEAVE;
		}

		//
		// Initialize local variables
		//
		if (nVars > 0)
		{
			for (i = 0; i < nVars; i++)
			{
				SMF_VARDECL *decl = &(InitAttributes->Driver->Machines[InitAttributes->InstanceOf].Vars[i]);

				if (PRIMITIVE(InitAttributes->Driver, decl->Type)) {
					context->Values[i] = TYPEDEF(InitAttributes->Driver, decl->Type).PrimitiveDefault;
				}
				else {
					context->Values[i] = SmfAllocateDefaultType(InitAttributes->Driver, decl->Type);
				}
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
		context->EventQueue.Events = (PSMF_TRIGGER)SmfAllocateMemory(eQSize * sizeof(SMF_TRIGGER));
		//
		// Failed to allocate memory
		//
		if (context->EventQueue.Events == NULL)
		{
			SmfFreeSMContext(context);
			status = STATUS_NO_MEMORY;
			LEAVE;
		}

		context->EventQueue.Head = 0;
		context->EventQueue.Tail = 0;
		context->EventQueue.Size = 0;
		context->EventQueue.IsFull = FALSE;


		//
		// Initialize Inherited Deferred Set 
		//
		packSize = SmfGetPackSize(context);
		context->InheritedDeferred = (PSMF_EVENTDECL_INDEX_PACKEDTABLE)SmfAllocateMemory(packSize*sizeof(SMF_EVENTDECL_INDEX));
		//
		// Failed to Allocate memory
		//
		if (context->InheritedDeferred == NULL)
		{
			SmfFreeSMContext(context);
			status = STATUS_NO_MEMORY;
			LEAVE;
		}
		memset(context->InheritedDeferred, 0, packSize*sizeof(SMF_EVENTDECL_INDEX));

		//
		// Initialize the current deferred set
		//
		context->CurrentDeferred = (PSMF_EVENTDECL_INDEX_PACKEDTABLE)SmfAllocateMemory(packSize*sizeof(SMF_EVENTDECL_INDEX));
		//
		// Failed to allocate memory
		//
		if (context->CurrentDeferred == NULL)
		{
			SmfFreeSMContext(context);
			status = STATUS_NO_MEMORY;
			LEAVE;
		}
		memset(context->CurrentDeferred, 0, packSize*sizeof(SMF_EVENTDECL_INDEX));

		//
		// Initialize actions
		//
		context->InheritedActions = (PSMF_EVENTDECL_INDEX_PACKEDTABLE)SmfAllocateMemory(packSize * sizeof(SMF_ACTIONDECL_INDEX_PACKEDTABLE));
		if (context->InheritedActions == NULL)
		{
			SmfFreeSMContext(context);
			status = STATUS_NO_MEMORY;
			LEAVE;
		}
		memset(context->InheritedActions, 0, packSize*sizeof(SMF_EVENTDECL_INDEX));

		context->CurrentActions = (PSMF_EVENTDECL_INDEX_PACKEDTABLE)SmfAllocateMemory(packSize * sizeof(SMF_ACTIONDECL_INDEX_PACKEDTABLE));
		if (context->CurrentActions == NULL)
		{
			SmfFreeSMContext(context);
			status = STATUS_NO_MEMORY;
			LEAVE;
		}
		memset(context->CurrentActions, 0, packSize*sizeof(SMF_EVENTDECL_INDEX));

		//
		// Allocate External Context Structure
		//
		context->ExtContext = (PSMF_EXCONTEXT)SmfAllocateMemory(sizeof(SMF_EXCONTEXT));
		//
		// Failed to allocate memory
		//
		if (context->ExtContext == NULL)
		{
			SmfFreeSMContext(context);
			status = STATUS_NO_MEMORY;
			LEAVE;
		}
		// Initialize ExtContext
		context->ExtContext->FreeThis = FALSE;
		context->ExtContext->ConstructorParam = InitAttributes->ConstructorParam;
		//
		// call machine constructor
		//
		context->Driver->Machines[context->InstanceOf].constructorFun(InitAttributes->ConstructorParam, context->ExtContext);

		//
		// Lifecycle Management < refcount = 1>
		//
		context->RefCount = 1;

		//
		//Initialize state machine lock
		//
		SmfInitializeLock(context);


#ifdef KERNEL_MODE
		//
		//Initialize PDeviceObj
		//
		context->PDeviceObj = InitAttributes->PDeviceObj;

		//
		//Initialize workerItem
		//
		context->SmWorkItem = IoAllocateWorkItem(InitAttributes->PDeviceObj);
		//
		// failed to allocate memory
		//
		if (context->SmWorkItem == NULL)
		{
			SmfFreeSMContext(context);
			status = STATUS_NO_MEMORY;
			LEAVE;
		}
#endif

#ifndef NDEBUG
		SmfTraceStep(context, traceCreateMachine);
#endif
		//
		//Acquire the lock while stabilizing the state machine
		//
		SmfAcquireLock(context);
		//
		// Run the state machine
		//
		SmfRunStateMachine(context, TRUE);

	} FINALLY{

	}

	return status;
}

VOID
SmfDelete(
PSMF_SMCONTEXT				Context
)
/*++

Routine Description:

Call this rountine to delete a statemachine;
this function is called from SmfRunStateMachine()

Arguments:

Machine - Machine which needs to be deleted.


Return Value:

NONE (VOID)

--*/
{
	LONG localMachineRefCount;
	BOOLEAN isSMRunning;

	TRY{

		SmfAcquireLock(Context);
		localMachineRefCount = Context->RefCount;
		isSMRunning = Context->IsRunning;
		SmfReleaseLock(Context);

		//
		// Delete is called on a machine with greater than 0 reference count.
		//
		SMF_ASSERTMSG("SmfDelete() is called on a Machine with greater than 0 refCount", localMachineRefCount > 0);

		if (localMachineRefCount < 0)
		{
			SmfReportException(IllegalAccess, Context);
			LEAVE;
		}
		else if (!isSMRunning && (localMachineRefCount) == 0)
		{
			if (SmfIsQueueEmpty(&Context->EventQueue))
			{
				//
				//	Safe to remove the statemachine as refCount is zero and also queue is empty
				//	Set signature to compliment
				//
				Context->StateMachineSignature = ~(ULONG)SmfStateMachine_Signature;
				SmfRemoveMachine(Context);
				LEAVE;
			}
			else
			{
				// Exception, trying to release a statemachine with non-empty queue
				// Set signature to compliment
				Context->StateMachineSignature = ~(ULONG)SmfStateMachine_Signature;
				SmfRemoveMachine(Context);
				SmfReportException(UnfinishedEvents, Context);
				LEAVE;
			}
		}

	} FINALLY{

	}

	return;

}

#ifdef DISTRIBUTED_RUNTIME
SMF_MACHINE_HANDLE
SmfNewRemote(PSMF_DRIVERDECL PDriverDecl, PSMF_SMCONTEXT Context, SMF_MACHINEDECL_INDEX	InstanceOf, PSMF_PACKED_VALUE Arg);
SMF_MACHINE_HANDLE
SmfNew(
__in PSMF_DRIVERDECL			PDriverDecl,
__inout PSMF_SMCONTEXT			Context,
__in SMF_MACHINEDECL_INDEX		InstanceOf,
__in PSMF_PACKED_VALUE			Arg
)
{
	return SmfNewRemote(PDriverDecl, Context, InstanceOf, Arg);
}
#else
SMF_MACHINE_HANDLE
SmfNew(
__in PSMF_DRIVERDECL			PDriverDecl,
__inout PSMF_SMCONTEXT			Context,
__in SMF_MACHINEDECL_INDEX		InstanceOf,
__in PSMF_PACKED_VALUE			Arg
)
{
	PSMF_MACHINE_ATTRIBUTES mAttributes;
	SMF_MACHINE_HANDLE smHandle;
	mAttributes = (PSMF_MACHINE_ATTRIBUTES)SmfAllocateMemory(sizeof(SMF_MACHINE_ATTRIBUTES));
	SMF_ASSERTMSG("Failed to create Machine Attributes in New", mAttributes != NULL);
#ifdef KERNEL_MODE
	SmfInitAttributes(mAttributes, Context->PDeviceObj, PDriverDecl, InstanceOf, Arg, Context->ExtContext->ConstructorParam);
#else
	SmfInitAttributes(mAttributes, PDriverDecl, InstanceOf, Arg, Context->ExtContext->ConstructorParam);
#endif
	SmfCreate(mAttributes, &smHandle);
#ifdef KERNEL_MODE
	ExFreePool(mAttributes);
#else
	free(mAttributes);
#endif
	return smHandle;
}
#endif

BOOLEAN
SmfIsEventMaxInstanceExceeded(
__in PSMF_EVENTQUEUE		Queue,
__in SMF_EVENTDECL_INDEX	EventIndex,
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
	TRY{

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
			LEAVE;
		}

	} FINALLY{

	}

	return isMaxInstancesExceeded;
}

VOID
SmfEnqueueEventInternal(
__in SMF_MACHINE_HANDLE			Machine,
__in SMF_EVENTDECL_INDEX		EventIndex,
__in PSMF_PACKED_VALUE			Arg,
__in BOOLEAN					UseWorkerItem
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
	PSMF_SMCONTEXT context;
	PSMF_EVENTQUEUE queue;
	UINT tail;
	UINT16 currMaxInstance;
	UCHAR newQueueSize;
	//
	// Code
	//

	TRY
	{

		SMF_ASSERTMSG("Enqueued Event Cannot be a NULL event", EventIndex != SmfNull);
		SMF_ASSERTMSG("Enqueued Event Cannot be a DEFAULT event", EventIndex != SmfDefaultEvent);

		context = SmfGetStateMachinePointer(Machine);
		currMaxInstance = context->Driver->Events[EventIndex].MaxInstances;

		SmfAcquireLock(context);
		// queue is full resize the queue if possible
		if (context->EventQueue.Size == context->Driver->Machines[context->InstanceOf].MaxSizeOfEventQueue)
		{
			SmfReleaseLock(context);
			SmfReportException(MaxQueueSizeExceeded, context);
			LEAVE;
		}

		//Check if we need to resize the queue
		if (context->EventQueue.IsFull)
		{
			newQueueSize = SmfResizeEventQueue(context);
#ifndef NDEBUG
			SmfTraceStep(context, traceQueueResize);
#endif
		}

		queue = &context->EventQueue;
		//check if Event.MaxInstances is NIL
		//check if the <event, payload> is in Queue
		if (currMaxInstance != UINT16_MAX && SmfIsEventMaxInstanceExceeded(queue, EventIndex, currMaxInstance, context->CurrentLengthOfEventQueue))
		{
			//
			//  Check if event is occuring more than maxinstances
			//
			SmfReleaseLock(context);
			SmfReportException(MaxInstanceExceeded, context);
			LEAVE;

		}


		tail = queue->Tail;

		SMF_ASSERT(!(context->CurrentLengthOfEventQueue == context->Driver->Machines[context->InstanceOf].MaxSizeOfEventQueue && queue->IsFull));
		//
		// Add event to the queue
		//
		queue->Events[tail].Event = EventIndex;
		queue->Events[tail].Arg = *Arg;
		queue->Size++;
		queue->Tail = (tail + 1) % context->CurrentLengthOfEventQueue;
		queue->IsFull = (queue->Tail == queue->Head) ? TRUE : FALSE;

#ifndef NDEBUG
		SmfTraceStep(context, traceEnqueue);
#endif
		//
		// Now try to run the machine if its not running already
		//
		if (context->IsRunning)
		{
			SmfReleaseLock(context);
			LEAVE;
		}
		else
		{
			if (UseWorkerItem)
			{
#ifdef KERNEL_MODE
				context->IsRunning = TRUE;
				SmfReleaseLock(context);
				SmfEnqueueStateMachineAsWorkerItemNonBlocking(context);
#else
				SmfRunStateMachine(context, FALSE);
#endif

			}
			else
			{
				SmfRunStateMachine(context, FALSE);
			}
		}
	} FINALLY{

	}

	return;
}

VOID
SmfEnqueueEvent(
__in SMF_MACHINE_HANDLE			Machine,
__in SMF_EVENTDECL_INDEX		EventIndex,
__in PSMF_PACKED_VALUE			Arg,
__in BOOLEAN					UseWorkerItem
)
{
	SmfEnqueueEventInternal(Machine, EventIndex, Arg, UseWorkerItem);
}


/*********************************************************************************

Protected Functions

*********************************************************************************/

VOID
SmfPop(
__inout PSMF_SMCONTEXT		Context
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
	Context->Trigger = g_SmfNullTrigger;

}

#define IS_SPECIAL_EVENT(Event)		((Event) == SmfNull || (Event) == SmfDefaultEvent)
#define PAYLOAD_TYPE(Ctxt, Event)	(Ctxt)->Driver->Events[(Event)].Type

VOID
SmfRaise(
__inout PSMF_SMCONTEXT		Context,
__in SMF_EVENTDECL_INDEX	EventIndex,
__in PSMF_PACKED_VALUE		Arg
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
	SMF_ASSERTMSG("Raised Event Cannot be a NULL event", EventIndex != SmfNull);
	SMF_ASSERTMSG("Raised Event Cannot be a DEFAULT event", EventIndex != SmfDefaultEvent);

	//
	// Set operation to raiseStatement
	//
	Context->LastOperation = RaiseStatement;

	if (!IS_SPECIAL_EVENT(Context->Trigger.Event) &&
		!PRIMITIVE(Context->Driver, PAYLOAD_TYPE(Context, Context->Trigger.Event)))
		SmfFreeType(Context->Driver, Context->Trigger.Arg.Type, (PVOID)Context->Trigger.Arg.Value);

	//
	// Set trigger to <raisedevent, null>
	//
	Context->Trigger.Arg = *Arg;
	Context->Trigger.Event = EventIndex;

#ifndef NDEBUG
	SmfTraceStep(Context, traceRaiseEvent);
#endif
}


VOID
SmfCall(
__inout PSMF_SMCONTEXT		Context,
SMF_STATEDECL_INDEX			State
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
	SmfPushState(Context, TRUE);

	//
	// Set Trigger to NULL after a call edge
	//
	Context->Trigger = g_SmfNullTrigger;

	//
	// Change current state
	//
	Context->CurrentState = State;
	//
	// Last operation set to call Statement
	//
	Context->LastOperation = CallStatement;

	SmfUpdateTransitionHistory(Context, onCallS, SmfNull, State);
#ifndef NDEBUG
	SmfTraceStep(Context, traceCallStatement);
#endif
	return;
}


/*********************************************************************************

Private Functions

*********************************************************************************/
#ifdef KERNEL_MODE
VOID
SmfRunStateMachineWorkItemPassiveFlag(
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
	SMF_ASSERT(Context != NULL);
	KeAcquireSpinLock(&((PSMF_SMCONTEXT)Context)->StateMachineLock, &((PSMF_SMCONTEXT)Context)->Irql);

	//
	// Run Queued Statemachine at passive level
	//
	SmfRunStateMachine((PSMF_SMCONTEXT)Context, TRUE);
}

VOID
SmfRunStateMachineWorkItemNonBlocking(
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
	SMF_ASSERT(Context != NULL);
	KeAcquireSpinLock(&((PSMF_SMCONTEXT)Context)->StateMachineLock, &((PSMF_SMCONTEXT)Context)->Irql);

	//
	// Run Queued Statemachine
	//
	SmfRunStateMachine((PSMF_SMCONTEXT)Context, FALSE);
}

VOID
SmfEnqueueStateMachineAsWorkerItemPassiveFlag
(__in PSMF_SMCONTEXT		Context
)
{
	IoQueueWorkItemEx(Context->SmWorkItem, SmfRunStateMachineWorkItemPassiveFlag, DelayedWorkQueue, Context);
}

VOID
SmfEnqueueStateMachineAsWorkerItemNonBlocking
(__in PSMF_SMCONTEXT		Context
)
{
	IoQueueWorkItemEx(Context->SmWorkItem, SmfRunStateMachineWorkItemNonBlocking, DelayedWorkQueue, Context);
}
#endif

FORCEINLINE
BOOLEAN
SmfStateHasDefaultTransition(
__in PSMF_SMCONTEXT			Context
)
{
	return SmfGetCurrentStateDecl(Context).HasDefaultTransition;
}

FORCEINLINE
SMF_STATEDECL
SmfGetCurrentStateDecl(
__in PSMF_SMCONTEXT			Context
)
{
	return Context->Driver->Machines[Context->InstanceOf].States[Context->CurrentState];
}


BOOLEAN
SmfIsEntryFunRequiresPassiveLevel(
__in PSMF_SMCONTEXT		Context
)
{
	SMF_STATEDECL StateDecl = Context->Driver->Machines[Context->InstanceOf].States[Context->CurrentState];
	SMF_RUNTIMEFLAGS StateRuntimeFlags = StateDecl.Flags;
	if (StateRuntimeFlags & SmfEntryFunPassiveLevel)
	{
		return TRUE;
	}
	else
	{
		return FALSE;
	}
}

BOOLEAN
SmfIsExitFunRequiresPassiveLevel(
__in PSMF_SMCONTEXT		Context
)
{
	SMF_STATEDECL StateDecl = Context->Driver->Machines[Context->InstanceOf].States[Context->CurrentState];
	SMF_RUNTIMEFLAGS StateRuntimeFlags = StateDecl.Flags;
	if (StateRuntimeFlags & SmfExitFunPassiveLevel)
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
SmfRunStateMachine(
__inout _At_(Context->Irql, _IRQL_restores_)
PSMF_SMCONTEXT	    Context,
__in BOOLEAN			DoEntryOrExit
)

{
	//
	// Declarations
	//
	BOOLEAN freeStateMachine;
	SMF_TRIGGER e;
	BOOLEAN isLockAcq;
	PSMF_ACTIONDECL currActionDecl;
	//
	// Code
	//
	TRY{

		freeStateMachine = FALSE;
		e = g_SmfNullTrigger;
		isLockAcq = TRUE;

		Context->IsRunning = TRUE;

		//// If doEntry is false, then the current state of the machine
		//// has already been executed in a previous call to SmfStabilize
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
			SmfReleaseLock(Context);
		}


	DoEntryOrExitOrActionFunction:

		// SM is entering or re-entering the state (entry or action or exit)
		// update the current deferred and actions set
		SmfUpdateCurrentActionsSet(Context);
		SmfUpdateCurrentDeferredSet(Context);

		//// Step 1. Execute the entry function or Exit Function
		// 
		// Check whether to execute entry or exit function
		//
		if (Context->StateExecFun == SmfStateEntry)
		{
			// handle the case when we are entering a state with an unhandled event
			if (Context->ReturnTo == SmfEntryFunEnd && Context->Trigger.Event != SmfNull)
			{
				goto DoTakeTransition;
			}

			//
			// Execute Entry Function
			//

#ifndef NDEBUG
			if (Context->ReturnTo == SmfEntryFunStart)
				SmfTraceStep(Context, traceStateChange);
#endif

#ifdef KERNEL_MODE
			//Before Executing the State Entry Function check State Flags
			if (SmfIsEntryFunRequiresPassiveLevel(Context) && (KeGetCurrentIrql() != PASSIVE_LEVEL))
			{
				SmfEnqueueStateMachineAsWorkerItemPassiveFlag(Context);
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
			SmfGetEntryFunction(Context)(Context);

			//// Step 2. Handle any raised event -- call --- Pop -- others
			switch (Context->LastOperation)
			{
			case PopStatement:
				Context->StateExecFun = SmfStateExit;
				Context->ReturnTo = SmfExitFunStart;
				goto DoEntryOrExitOrActionFunction;
				break;
			case RaiseStatement:
				SMF_ASSERT(Context->Trigger.Event != SmfNull);
				SMF_ASSERT(Context->Trigger.Event != SmfDefaultEvent);

				if (SmfIsTransitionPresent(Context->Trigger.Event, Context))
				{

					if (SmfIsCallTransition(Context, Context->Trigger.Event))
					{
						//
						// call transition so no exit function executed
						//
						goto DoTakeTransition;
					}

					else
					{
						// execute exit function

						Context->StateExecFun = SmfStateExit;
						Context->ReturnTo = SmfExitFunStart;
						goto DoEntryOrExitOrActionFunction;
					}
				}
				//
				// check if there is an action installed for this event
				//
				else if (SmfIsActionInstalled(Context->Trigger.Event, Context->CurrentActions))
				{
					Context->StateExecFun = SmfStateAction;
					Context->ReturnTo = SmfActionFunStart;
					goto DoEntryOrExitOrActionFunction;
				}
				//
				// Unhandled raised event
				//
				else
				{
					Context->StateExecFun = SmfStateExit;
					Context->ReturnTo = SmfExitFunStart;
					goto DoEntryOrExitOrActionFunction;
				}
				break;
			case CallStatement:
				Context->StateExecFun = SmfStateEntry;
				Context->ReturnTo = SmfEntryFunStart;
				goto DoEntryOrExitOrActionFunction;
				break;
			case OtherStatement:
				goto DoDequeue;
				break;
			default:
				break;
			}
		}
		else if (Context->StateExecFun == SmfStateExit)
		{
			//
			//Execute the exit function
			//

#ifdef KERNEL_MODE
			//Before Executing the State Exit Function check State Flags
			if (SmfIsExitFunRequiresPassiveLevel(Context) && (KeGetCurrentIrql() != PASSIVE_LEVEL))
			{
				SmfEnqueueStateMachineAsWorkerItemPassiveFlag(Context);
				LEAVE;
			}
#endif
			// Initialize context before executing exit function
			//
			Context->LastOperation = OtherStatement;
			//
			// Execute the exit function for the current state
			//
			if (SmfGetCurrentStateDecl(Context).ExitFunc != NULL)
			{
#ifndef NDEBUG
				if (Context->ReturnTo == SmfExitFunStart)
					SmfTraceStep(Context, traceExit);
#endif
				SmfGetExitFunction(Context)(Context);
			}


			//// Step 2. Handle call or others
			switch (Context->LastOperation)
			{
			case RaiseStatement:
			case PopStatement:
				SMF_ASSERTMSG("Pop or Raise is not allowed inside Exit Function", FALSE);
				break;
			case CallStatement:
				Context->StateExecFun = SmfStateEntry;
				Context->ReturnTo = SmfEntryFunStart;
				goto DoEntryOrExitOrActionFunction;
			case OtherStatement:
				goto DoTakeTransition;
			default:
				break;
			}

		}
		else if (Context->StateExecFun == SmfStateAction)
		{
			//
			// Execute the action installed corresponding to trigger
			//
			//
			// Get the current action decl
			currActionDecl = SmfGetAction(Context);
#ifdef KERNEL_MODE
			//Before Executing the Action Function check State Flags
			if (currActionDecl->IsActionFunPassiveLevel && (KeGetCurrentIrql() != PASSIVE_LEVEL))
			{
				SmfEnqueueStateMachineAsWorkerItemPassiveFlag(Context);
				LEAVE;
			}
#endif

#ifndef NDEBUG
			if (Context->ReturnTo == SmfActionFunStart)
				SmfTraceStep(Context, traceActions, (currActionDecl->Name));
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
				Context->StateExecFun = SmfStateExit;
				Context->ReturnTo = SmfExitFunStart;
				goto DoEntryOrExitOrActionFunction;
				break;
			case RaiseStatement:
				SMF_ASSERT(Context->Trigger.Event != SmfNull);
				SMF_ASSERT(Context->Trigger.Event != SmfDefaultEvent);

				if (SmfIsTransitionPresent(Context->Trigger.Event, Context))
				{

					if (SmfIsCallTransition(Context, Context->Trigger.Event))
					{
						//
						// call transition so no exit function executed
						//
						goto DoTakeTransition;
					}

					else
					{
						// execute exit function

						Context->StateExecFun = SmfStateExit;
						Context->ReturnTo = SmfExitFunStart;
						goto DoEntryOrExitOrActionFunction;
					}
				}
				//
				// check if there is an action installed for this event
				//
				else if (SmfIsActionInstalled(Context->Trigger.Event, Context->CurrentActions))
				{
					Context->StateExecFun = SmfStateAction;
					Context->ReturnTo = SmfActionFunStart;
					goto DoEntryOrExitOrActionFunction;
				}
				//
				// Unhandled raised event
				//
				else
				{
					Context->StateExecFun = SmfStateExit;
					Context->ReturnTo = SmfExitFunStart;
					goto DoEntryOrExitOrActionFunction;
				}
				break;
			case CallStatement:
				Context->StateExecFun = SmfStateEntry;
				Context->ReturnTo = SmfEntryFunStart;
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
			SmfAcquireLock(Context);
		}

		e = SmfDequeueEvent(Context);

		//Successfully dequeued an event
		if (e.Event != SmfNull)
		{
			//Release Lock
			isLockAcq = FALSE;
			SmfReleaseLock(Context);
			if (SmfIsCallTransition(Context, e.Event))
			{
				goto DoTakeTransition;
			}
			//
			// Transition corresponding to dequeued event (Ankush : this takes care of local priority of e over actions)
			//
			else if (SmfIsTransitionPresent(Context->Trigger.Event, Context))
			{
				Context->StateExecFun = SmfStateExit;
				Context->ReturnTo = SmfExitFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			//
			// check if there is an action installed for this event
			//
			else if (SmfIsActionInstalled(Context->Trigger.Event, Context->CurrentActions))
			{
				Context->StateExecFun = SmfStateAction;
				Context->ReturnTo = SmfActionFunStart;
				goto DoEntryOrExitOrActionFunction;
			}
			//
			// Unhandled dequeued event
			//
			else
			{
				Context->StateExecFun = SmfStateExit;
				Context->ReturnTo = SmfExitFunStart;
				goto DoEntryOrExitOrActionFunction;
			}

		}
		// failed to dequeue an event -> two possibility either take default branch(if available) else block
		else if (SmfStateHasDefaultTransition(Context))
		{
			//release lock
			isLockAcq = FALSE;
			SmfReleaseLock(Context);

			if (!IS_SPECIAL_EVENT(Context->Trigger.Event) &&
				!PRIMITIVE(Context->Driver, PAYLOAD_TYPE(Context, Context->Trigger.Event)))
				SmfFreeType(Context->Driver, Context->Trigger.Arg.Type, (PVOID)Context->Trigger.Arg.Value);

			Context->Trigger.Event = (ULONG32)SmfDefaultEvent;
			Context->Trigger.Arg = g_SmfNullPayload;
			Context->StateExecFun = SmfStateExit;
			Context->ReturnTo = SmfExitFunStart;
			goto DoEntryOrExitOrActionFunction;
		}
		else
		{
			Context->IsRunning = FALSE;
			//check if the reference count is zero
			if (Context->RefCount == 0)
			{
				freeStateMachine = TRUE;
			}
			//Release Lock
			isLockAcq = FALSE;
			SmfReleaseLock(Context);

			//// Step 3.b. Safely Remove the statemachine.
			if (freeStateMachine)
			{
				SmfDelete(Context);
			}
			LEAVE;

		}


	DoTakeTransition:

		if ((Context->Trigger.Event == g_SmfNullTrigger.Event))
		{
			//
			// The last statement executed was a pop statement
			//
			SmfPopState(Context, TRUE);

			SmfUpdateTransitionHistory(Context, onPop, SmfNull, Context->CurrentState);

#ifndef NDEBUG
			SmfTraceStep(Context, tracePop);
#endif

			if (Context->ReturnTo == SmfEntryFunEnd)
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
		else if (Context->Trigger.Event == SmfDefaultEvent)
		{
			//
			// Take After transition
			//
			SmfTakeDefaultTransition(Context);
			goto DoEntryOrExitOrActionFunction;
		}
		else
		{
			//
			// Trigger is non-null and hence its a raise or dequeue or unhandled event
			//
			SmfTakeTransition(Context, Context->Trigger.Event);

			goto DoEntryOrExitOrActionFunction;

		}

	} FINALLY{

	}

	return;

}

VOID
SmfTakeDefaultTransition(
__inout PSMF_SMCONTEXT		Context
)
{
	//
	// Declarations
	//
	ULONG i;
	UINT16 nTransitions;
	SMF_TRANSDECL* transTable;

	//
	// Code
	//
	TRY{

		transTable = SmfGetTransTable(Context, Context->CurrentState, &nTransitions);

		for (i = 0; i < nTransitions; ++i)
		{
			//check if transition is After
			if (transTable[i].EventIndex == SmfDefaultEvent)
			{
				//Update the transition history
				SmfUpdateTransitionHistory(Context, onEvent, SmfDefaultEvent, transTable[i].Destination);
				//check if its a call transition
				if (transTable[i].IsPush != FALSE)
				{
					Context->ReturnTo = SmfEntryFunEnd;
					SmfPushState(Context, FALSE);
				}

				//update the state
				Context->CurrentState = transTable[i].Destination;
				Context->ReturnTo = SmfEntryFunStart;
				Context->StateExecFun = SmfStateEntry;
				LEAVE;
			}
		}

	} FINALLY{

	}

	return;

}



VOID
SmfTakeTransition(
__inout PSMF_SMCONTEXT		Context,
__in SMF_EVENTDECL_INDEX	EventIndex
)
{
	//
	// Declarations
	//
	ULONG i;
	UINT16 nTransitions;
	SMF_TRANSDECL* transTable;

	//
	//code
	//
	TRY{

		transTable = SmfGetTransTable(Context, Context->CurrentState, &nTransitions);

		for (i = 0; i < nTransitions; ++i)
		{
			if ((transTable[i].EventIndex == EventIndex))
			{

				//check if its a call transition
				if (transTable[i].IsPush != FALSE)
				{
					//Update the transition history
					SmfUpdateTransitionHistory(Context, onCallE, EventIndex, transTable[i].Destination);
#ifndef NDEBUG
					SmfTraceStep(Context, traceCallEdge);
#endif
					Context->ReturnTo = SmfEntryFunEnd;
					SmfPushState(Context, FALSE);
				}
				else
				{
					SmfUpdateTransitionHistory(Context, onEvent, EventIndex, transTable[i].Destination);
				}
				// change CurrentState state and set returnTo to smfEntryFunStart 
				// next to execute is the entry function of the destination state
				Context->CurrentState = transTable[i].Destination;
				Context->ReturnTo = SmfEntryFunStart;
				Context->StateExecFun = SmfStateEntry;
				LEAVE;
			}
		}
		if (Context->CallStack.Length > 0)
		{
			SmfPopState(Context, FALSE);
			SmfUpdateTransitionHistory(Context, onUnhandledEvent, EventIndex, Context->CurrentState);
#ifndef NDEBUG
			SmfTraceStep(Context, traceUnhandledEvent);
#endif
			LEAVE;
		}
		else
		{
			if (Context->Trigger.Event == 1) // 1 == Event_delete in the generated code
			{
#ifndef NDEBUG
				SmfTraceStep(Context, traceDelete);
#endif
				SmfReportException(UnhandledEvent, Context); // Needs to be fixed and update the delete logic.
				LEAVE;
			}
			else
			{
				//Exception
				SmfReportException(UnhandledEvent, Context);
				LEAVE;
			}
		}


	} FINALLY{

	}

	return;
}

VOID
SmfPushState(
__inout PSMF_SMCONTEXT		Context,
__in	BOOLEAN				isCallStatement
)
{
	//
	// Declarations
	//
	UINT16 i;
	UINT16 packSize;
	UINT16 length;
	SMF_EVENTDECL_INDEX_PACKEDTABLE currDef;
	SMF_ACTIONDECL_INDEX_PACKEDTABLE currActions;
	SMF_TRANSDECL_INDEX_PACKEDTABLE currTransitions;

	//
	// Code
	//
	packSize = SmfGetPackSize(Context);
	length = Context->CallStack.Length;
	currDef = SmfGetDeferredPacked(Context, Context->CurrentState);
	currActions = SmfGetActionsPacked(Context, Context->CurrentState);
	currTransitions = Context->Driver->Machines[Context->InstanceOf].States[Context->CurrentState].TransitionsPacked;

	SMF_ASSERTMSG("Call Stack Overflow", length < SMF_MAX_CALL_DEPTH);
	//
	// push <state, trigger, arg, ReturnTo, StateExecFun, defSet, ActSet>
	//
	Context->CallStack.StatesStack[length].StateIndex = Context->CurrentState;
	Context->CallStack.StatesStack[length].Trigger.Event = Context->Trigger.Event;
	Context->CallStack.StatesStack[length].Trigger.Arg = Context->Trigger.Arg;
	Context->CallStack.StatesStack[length].ReturnTo = Context->ReturnTo;
	Context->CallStack.StatesStack[length].StateExecFun = Context->StateExecFun;
	Context->CallStack.StatesStack[length].InheritedDef = (SMF_EVENTDECL_INDEX_PACKEDTABLE)SmfClonePackedSet(Context->InheritedDeferred, packSize);
	Context->CallStack.StatesStack[length].InheritedAct = (SMF_ACTIONDECL_INDEX_PACKEDTABLE)SmfClonePackedSet(Context->InheritedActions, packSize);

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
SmfPopState(
__inout PSMF_SMCONTEXT		Context,
__in BOOLEAN				RestoreTrigger
)
{
	//
	// Declarations
	//
	UINT16 i;
	UINT16 packSize;
	UINT16 length;
	SMF_EVENTDECL_INDEX *def;
	SMF_STACKSTATE_INFO poppedState;
	// 
	// Code
	//
	i = 0;
	packSize = SmfGetPackSize(Context);
	length = Context->CallStack.Length;
	def = NULL;

	SMF_ASSERTMSG("PopState Called on Empty Stack", length > 0);

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
	SmfFreeMemory(poppedState.InheritedDef);
	SmfFreeMemory(poppedState.InheritedAct);

	//
	// Restore the trigger value
	//
	if (RestoreTrigger)
	{
		if (!IS_SPECIAL_EVENT(Context->Trigger.Event) &&
			!PRIMITIVE(Context->Driver, PAYLOAD_TYPE(Context, Context->Trigger.Event)))
			SmfFreeType(Context->Driver, Context->Trigger.Arg.Type, (PVOID)Context->Trigger.Arg.Value);

		Context->Trigger.Event = poppedState.Trigger.Event;
		Context->Trigger.Arg = poppedState.Trigger.Arg;
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
		SMF_ASSERTMSG("Unhandled Event in Exit Function", poppedState.StateExecFun == SmfStateEntry || poppedState.StateExecFun == SmfStateAction);

		//
		// assert that we are popping back because of an call-edge and not because of a call statement (implicit pop)
		//
		if (poppedState.ReturnTo != SmfEntryFunEnd)
		{
			SmfReportException(UnhandledEventInCallS, Context);
		}

		//check if there is a push transition defined for the unhandled event
		if (SmfIsTransitionPresent(Context->Trigger.Event, Context) && SmfIsCallTransition(Context, Context->Trigger.Event))
		{
			Context->StateExecFun = SmfStateEntry;
			Context->ReturnTo = SmfEntryFunEnd;
		}
		else
		{
			Context->StateExecFun = SmfStateExit;
			Context->ReturnTo = SmfExitFunStart;
		}
	}
	return;
}


SMF_TRIGGER
SmfDequeueEvent(
__inout PSMF_SMCONTEXT	Context
)
{
	//
	// Declarations
	//
	INT queueLength;
	PSMF_EVENTQUEUE queue;
	SMF_EVENTDECL_INDEX_PACKEDTABLE deferred;
	INT i, head;
	SMF_TRIGGER e;

	//
	// Code
	//
	TRY{
		queueLength = Context->CurrentLengthOfEventQueue;
		queue = &Context->EventQueue;
		deferred = SmfGetDeferredPacked(Context, Context->CurrentState);
		head = queue->Head;
		e = g_SmfNullTrigger;

		SMF_ASSERT(queue->Size <= queueLength);
		SMF_ASSERT(queue->Size >= 0);
		SMF_ASSERT(queue->Head >= 0);
		SMF_ASSERT(queue->Tail >= 0);

		if (SmfIsQueueEmpty(queue)) {
			LEAVE;
		}

		//
		// Find the element to dequeue
		//
		for (i = 0; i < queue->Size; i++) {
			INT index = (head + i) % queueLength;
			e = queue->Events[index];
			if (!SmfIsEventDeferred(e.Event, Context->CurrentDeferred)) {
				break;
			}
		}

		//
		// Check if not found
		//
		if (i == queue->Size) {
			e = g_SmfNullTrigger;
			LEAVE;
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
		if (!IS_SPECIAL_EVENT(Context->Trigger.Event) &&
			!PRIMITIVE(Context->Driver, PAYLOAD_TYPE(Context, Context->Trigger.Event)))
			SmfFreeType(Context->Driver, Context->Trigger.Arg.Type, (PVOID)Context->Trigger.Arg.Value);

		//
		// Store the event and argument
		//
		Context->Trigger.Event = e.Event;
		Context->Trigger.Arg = e.Arg;

#ifndef NDEBUG
		SmfTraceStep(Context, traceDequeue);
#endif

	} FINALLY{

	}

	SMF_ASSERT(queue->Size <= queueLength);
	SMF_ASSERT(queue->Size >= 0);
	SMF_ASSERT(queue->Head >= 0);
	SMF_ASSERT(queue->Tail >= 0);

	return e;
}

/*********************************************************************************

Machine Managerment Functions

*********************************************************************************/


VOID
SmfRemoveMachine(
__in PSMF_SMCONTEXT			Context
)
{
#ifdef KERNEL_MODE
	IoFreeWorkItem(Context->SmWorkItem);
#endif
	if (Context->ExtContext->FreeThis)
	{
		SmfFreeMemory(Context->ExtContext->PExMem);
	}
	SmfFreeSMContext(Context);
}

/*********************************************************************************

Helper Functions

*********************************************************************************/
FORCEINLINE
_Acquires_lock_(Context->StateMachineLock)
_IRQL_raises_(DISPATCH_LEVEL)
VOID
SmfAcquireLock(
_In_ _At_(Context->Irql, _IRQL_saves_)
PSMF_SMCONTEXT	Context
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
SmfReleaseLock(
_In_ _At_(Context->Irql, _IRQL_restores_)
PSMF_SMCONTEXT	Context
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
SmfInitializeLock(
PSMF_SMCONTEXT				Context
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
SmfAllocateMemory(
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
SmfAllocateType(
__in PSMF_DRIVERDECL			Driver,
__in SMF_TYPEDECL_INDEX			Type)
{
	PVOID mem = SmfAllocateMemory((UINT)Driver->Types[Type].Size);
	SMF_ASSERTMSG("Failed to create An Instance of a Complex type", mem != NULL);
	return (ULONG_PTR)mem;
}

FORCEINLINE
VOID
SmfFreeType(
__in PSMF_DRIVERDECL			Driver,
__in SMF_TYPEDECL_INDEX			Type,
__in PVOID						Value)
{
	PSMF_DESTROYFUN destroy;
	if (PRIMITIVE(Driver, Type))
		return;
	destroy = DESTROY(Driver, Type);
	if (Value != NULL) {
		if (destroy != NULL)
			destroy(Driver, Value);
		SmfFreeMemory(Value);
	}
}

FORCEINLINE
ULONG_PTR
SmfAllocateDefaultType(
__in PSMF_DRIVERDECL			Driver,
__in SMF_TYPEDECL_INDEX			Type)
{
	ULONG_PTR mem = SmfAllocateType(Driver, Type);
	BUILD_DEFAULT(Driver, Type)(Driver, (PVOID)mem);
	return mem;
}

FORCEINLINE
VOID
SmfFreeMemory(
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
SmfIsIndexInSet(
__in ULONG32 Index,
SMF_BIT_SET Set
)
{
	// TODO: Why is this in a Try/Catch in SmfIsEventDeferred? Does the TryCatch buy you something on an index outside of the array?
	return ((Set[Index / (sizeof(SMF_EVENTDECL_INDEX) * 8)] & (1 << (Index % (sizeof(SMF_EVENTDECL_INDEX) * 8)))) != 0);
}


FORCEINLINE
BOOLEAN
SmfIsEventDeferred(
__in SMF_EVENTDECL_INDEX	EventIndex,
SMF_EVENTDECL_INDEX_PACKEDTABLE
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
	TRY{

		isDeferred = FALSE;
		if
			(
			((DeferredSet[EventIndex / (sizeof(SMF_EVENTDECL_INDEX) * 8)] & (1 << (EventIndex % (sizeof(SMF_EVENTDECL_INDEX) * 8)))) != 0)
			)
		{
			isDeferred = TRUE;
			LEAVE;
		}

	} FINALLY{

	}

	return isDeferred;
}

FORCEINLINE
BOOLEAN
SmfIsActionInstalled(
__in SMF_EVENTDECL_INDEX	EventIndex,
SMF_ACTIONDECL_INDEX_PACKEDTABLE
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
	TRY{

		isActionInstalled = FALSE;
		if
			(
			((ActionSet[EventIndex / (sizeof(SMF_EVENTDECL_INDEX) * 8)] & (1 << (EventIndex % (sizeof(SMF_EVENTDECL_INDEX) * 8)))) != 0)
			)
		{
			isActionInstalled = TRUE;
			LEAVE;
		}

	} FINALLY{

	}

	return isActionInstalled;
}


FORCEINLINE
UINT16
SmfGetPackSize(
__in PSMF_SMCONTEXT			Context
)
{
	ULONG32 nEvents = Context->Driver->NEvents;
	return (UINT16)(((nEvents == 0) || (nEvents % (sizeof(SMF_EVENTDECL_INDEX) * 8) != 0))
		? (1 + (nEvents / (sizeof(SMF_EVENTDECL_INDEX) * 8)))
		: (nEvents / (sizeof(SMF_EVENTDECL_INDEX) * 8)));
}

FORCEINLINE
BOOLEAN
SmfIsQueueEmpty(
__in PSMF_EVENTQUEUE		Queue
)
{
	return !Queue->IsFull && Queue->Head == Queue->Tail;
}

FORCEINLINE
PSMF_EXITFUN
SmfGetExitFunction(
__in PSMF_SMCONTEXT			Context
)
{
	return (PSMF_EXITFUN)Context->Driver->Machines[Context->InstanceOf].States[Context->CurrentState].ExitFunc;
}

FORCEINLINE
PSMF_ENTRYFUN
SmfGetEntryFunction(
__in PSMF_SMCONTEXT			Context
)
{
	return (PSMF_ENTRYFUN)Context->Driver->Machines[Context->InstanceOf].States[Context->CurrentState].EntryFunc;
}



FORCEINLINE
PSMF_ACTIONDECL
SmfGetAction(
__in PSMF_SMCONTEXT			Context
)
{

	SMF_EVENTDECL_INDEX currEvent = Context->Trigger.Event;
	BOOLEAN isActionInstalled = FALSE;
	INT i, nActions;
	SMF_STATESTACK currStack;
	SMF_STATEDECL_TABLE stateTable;
	SMF_STATEDECL_INDEX topOfStackState;

	PSMF_ACTIONDECL actionDecl = NULL;

	TRY{
		//check if action is defined for the current state
		isActionInstalled = SmfIsActionInstalled(currEvent, SmfGetCurrentStateDecl(Context).ActionsPacked);
		if (isActionInstalled)
		{
			//
			// get action function
			//
			nActions = SmfGetCurrentStateDecl(Context).NActions;
			for (i = 0; i < nActions; i++)
			{
				if (SmfGetCurrentStateDecl(Context).Actions[i].EventIndex == currEvent)
				{
					actionDecl = &SmfGetCurrentStateDecl(Context).Actions[i];
					LEAVE;
				}
			}
		}

		//
		// Scan the parent states
		//
		currStack = Context->CallStack;
		stateTable = Context->Driver->Machines[Context->InstanceOf].States;
		for (i = currStack.Length - 1; i >= 0; i--)
		{
			topOfStackState = currStack.StatesStack[i].StateIndex;
			isActionInstalled = SmfIsActionInstalled(currEvent, stateTable[topOfStackState].ActionsPacked);
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
						LEAVE;
					}
				}
			}
		}

	} FINALLY{

	}

	SMF_ASSERT(actionDecl != NULL);
	return actionDecl;
}


FORCEINLINE
SMF_EVENTDECL_INDEX_PACKEDTABLE
SmfGetDeferredPacked(
__in PSMF_SMCONTEXT			Context,
__in SMF_STATEDECL_INDEX	StateIndex
)
{
	SMF_EVENTSETDECL_INDEX evSet = Context->Driver->Machines[Context->InstanceOf].States[StateIndex].Defers;
	return Context->Driver->Machines[Context->InstanceOf].EventSets[evSet].EventIndexPackedTable;
}

FORCEINLINE
SMF_ACTIONDECL_INDEX_PACKEDTABLE
SmfGetActionsPacked(
__in PSMF_SMCONTEXT			Context,
__in SMF_STATEDECL_INDEX	StateIndex
)
{
	return Context->Driver->Machines[Context->InstanceOf].States[StateIndex].ActionsPacked;
}


FORCEINLINE
SMF_TRANSDECL_TABLE
SmfGetTransTable(
__in PSMF_SMCONTEXT			Context,
__in SMF_STATEDECL_INDEX	StateIndex,
__out UINT16				*NTransitions
)
{
	*NTransitions = Context->Driver->Machines[Context->InstanceOf].States[StateIndex].NTransitions;
	return Context->Driver->Machines[Context->InstanceOf].States[StateIndex].Transitions;
}

FORCEINLINE
SMF_MACHINE_HANDLE
SmfGetStateMachineHandle(
__in PSMF_SMCONTEXT			Context
)
/*++

Routine Description:

Kernel mode driver (P Runtime code) can call this rountine
to get state-machine handle from Pointer to the state-machine.


Arguments:

Context - Pointer to the State-Machine context.

Return Value:

SMF_MACHINE_HANDLE - Handle to the State-Machine.

--*/
{
	return (SMF_MACHINE_HANDLE)((ULONG_PTR)Context ^ 0x11);
}

FORCEINLINE
PSMF_SMCONTEXT
SmfGetStateMachinePointer(
__in SMF_MACHINE_HANDLE				Handle
)
/*++

Routine Description:

Kernel mode driver (P Runtime code) can call this rountine
to get state-machine pointer from Handle to the state-machine.


Arguments:

handle - Handle to the State-Machine.

Return Value:

PSMF_SMCONTEXT - Pointer to the StateMachine context

--*/
{
	//
	// Declarations
	//
	PSMF_SMCONTEXT tempSMPointer;

	//
	// Code
	//

	tempSMPointer = (PSMF_SMCONTEXT)(Handle ^ 0x11);
	//
	//Check if the State Machine is still pointing to valid state-machine
	//
	if (tempSMPointer->StateMachineSignature != SmfStateMachine_Signature)
	{
		//
		// Signature doesnt match so the state-machine is freed
		// Exception : Trying to dereference an invalid machine pointer
		SmfReportException(IllegalAccess, g_SmfNullSMMachinePointer);
	}

	return tempSMPointer;
}

#ifdef DISTRIBUTED_RUNTIME
SMF_MACHINE_HANDLE
SmfGetStateMachineHandleRemote(
__in PSMF_SMCONTEXT_REMOTE			Context
)
{
	return (SMF_MACHINE_HANDLE)((ULONG_PTR)Context ^ 0x11);
}

PSMF_SMCONTEXT_REMOTE
SmfGetStateMachinePointerRemote(
__in SMF_MACHINE_HANDLE				Handle
)
{
	return (PSMF_SMCONTEXT_REMOTE)(Handle ^ 0x11);
}
#endif

FORCEINLINE
VOID SmfUpdateTransitionHistory(
__in PSMF_SMCONTEXT				Context,
__in SMF_TRANSHISTORY_STEP		Step,
__in SMF_EVENTDECL_INDEX		EventIndex,
__in SMF_STATEDECL_INDEX		StateEntered
)
/*++

Routine Description:

Kernel mode driver (P Runtime code) can call this rountine
to update the transition history of the state machine.


Arguments:

Context - Pointer to the State-Machine context.

Step - Step taken by the state-machine.

EventIndex - If the step was taken because of an event, EventIndex points to
that event.

StateEntered - target state entered by state-machine because of Step taken.

Return Value:

NONE (VOID)

--*/
{
	//
	// Code
	//
	if (Context->TransHistoryIndex == (SMF_MAX_HISTORY_DEPTH - 1))
	{
		Context->TransHistoryIndex = 0;
	}
	Context->TransitionHistory[Context->TransHistoryIndex].OnEvent = EventIndex;
	Context->TransitionHistory[Context->TransHistoryIndex].StateEntered = StateEntered;
	Context->TransitionHistory[Context->TransHistoryIndex].OnStep = Step;
	Context->TransHistoryIndex++;
}

BOOLEAN
SmfIsCallTransition(
PSMF_SMCONTEXT			Context,
SMF_EVENTDECL_INDEX		Event
)
{
	//
	// Declarations
	//
	ULONG i;
	UINT16 nTransitions;
	SMF_TRANSDECL* transTable;
	BOOLEAN isCallTransition;

	//
	// Code
	//
	TRY{

		transTable = SmfGetTransTable(Context, Context->CurrentState, &nTransitions);
		isCallTransition = FALSE;
		for (i = 0; i < nTransitions; ++i)
		{
			//check if transition is Call
			if (transTable[i].IsPush && transTable[i].EventIndex == Event)
			{
				isCallTransition = TRUE;
				LEAVE;
			}
		}

	} FINALLY{

	}

	return isCallTransition;
}

FORCEINLINE
BOOLEAN
SmfIsTransitionPresent(
__in SMF_EVENTDECL_INDEX	EventIndex,
__in PSMF_SMCONTEXT			Context
)
{
	if ((SmfGetCurrentStateDecl(Context).TransitionsPacked[EventIndex / (sizeof(SMF_EVENTDECL_INDEX) * 8)] & (1 << (EventIndex % (sizeof(SMF_EVENTDECL_INDEX) * 8)))) != 0)
	{
		return TRUE;
	}

	return FALSE;
}

PVOID
SmfClonePackedSet(
PVOID					PackedSet,
UINT					Size
)
{
	PULONG32 clone;
	UINT i;

	clone = (PULONG32)SmfAllocateMemory(Size * sizeof(ULONG32));
	for (i = 0; i<Size; i++)
	{
		clone[i] = ((PULONG32)PackedSet)[i];
	}

	return clone;
}

VOID
SmfUpdateCurrentActionsSet(
PSMF_SMCONTEXT			Context
)
{
	UINT i;
	SMF_ACTIONDECL_INDEX_PACKEDTABLE currActions;
	SMF_TRANSDECL_INDEX_PACKEDTABLE currTransitions;
	SMF_EVENTDECL_INDEX_PACKEDTABLE currDefSet;
	UINT16 packSize;

	packSize = SmfGetPackSize(Context);
	currActions = SmfGetCurrentStateDecl(Context).ActionsPacked;
	currTransitions = SmfGetCurrentStateDecl(Context).TransitionsPacked;
	currDefSet = SmfGetDeferredPacked(Context, Context->CurrentState);
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
SmfUpdateCurrentDeferredSet(
PSMF_SMCONTEXT			Context
)
{
	UINT i;
	SMF_ACTIONDECL_INDEX_PACKEDTABLE currActions;
	SMF_TRANSDECL_INDEX_PACKEDTABLE currTransitions;
	SMF_EVENTDECL_INDEX_PACKEDTABLE currDefSet;
	UINT16 packSize;

	packSize = SmfGetPackSize(Context);
	currActions = SmfGetCurrentStateDecl(Context).ActionsPacked;
	currTransitions = SmfGetCurrentStateDecl(Context).TransitionsPacked;
	currDefSet = SmfGetDeferredPacked(Context, Context->CurrentState);
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


UCHAR
SmfResizeEventQueue(
__in PSMF_SMCONTEXT Context
)
{
	UCHAR maxEventQueueSize = Context->Driver->Machines[Context->InstanceOf].MaxSizeOfEventQueue;
	UCHAR currEventQueueSize = Context->CurrentLengthOfEventQueue;
	UCHAR newQueueSize = Context->CurrentLengthOfEventQueue * 2 > maxEventQueueSize ? maxEventQueueSize : Context->CurrentLengthOfEventQueue * 2;
	PSMF_TRIGGER oldQueue = Context->EventQueue.Events;
	UINT16 oldHead = Context->EventQueue.Head;
	UINT16 oldTail = Context->EventQueue.Tail;
	PSMF_TRIGGER newQueue = (PSMF_TRIGGER)SmfAllocateMemory(newQueueSize * sizeof(SMF_TRIGGER));
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
	SmfFreeMemory(oldQueue);

	return Context->CurrentLengthOfEventQueue;
}

VOID
SmfFreeSMContext(
PSMF_SMCONTEXT			Context
)
{
	if (Context->CurrentActions != NULL)
	{
		SmfFreeMemory(Context->CurrentActions);
	}

	if (Context->CurrentDeferred != NULL)
	{
		SmfFreeMemory(Context->CurrentDeferred);
	}

	if (Context->EventQueue.Events != NULL)
	{
		SmfFreeMemory(Context->EventQueue.Events);
	}

	if (Context->ExtContext != NULL)
	{
		SmfFreeMemory(Context->ExtContext);
	}

	if (Context->InheritedActions != NULL)
	{
		SmfFreeMemory(Context->InheritedActions);
	}

	if (Context->InheritedDeferred != NULL)
	{
		SmfFreeMemory(Context->InheritedDeferred);
	}

	if (Context->Values != NULL)
	{
		UINT i;
		SMF_MACHINEDECL *mdecl = &(Context->Driver->Machines[Context->InstanceOf]);

		for (i = 0; i < mdecl->NVars; i++) {
			if (!PRIMITIVE(Context->Driver, mdecl->Vars[i].Type)) {
				SmfFreeType(Context->Driver, mdecl->Vars[i].Type, (PVOID)Context->Values[i]);
			}
		}
		SmfFreeMemory(Context->Values);
	}
	SmfFreeMemory(Context);
}

PSMF_EXCONTEXT
SmfGetForeignContext(
__in SMF_MACHINE_HANDLE SmHandle
)
{
	PSMF_SMCONTEXT context;

	context = SmfGetStateMachinePointer(SmHandle);
	return context->ExtContext;
}

//
// Pack a value in a preallocated piece of memory 
//
VOID
PackValue(
__in PSMF_DRIVERDECL			Driver,
__in PSMF_PACKED_VALUE			Dst,
__in ULONG_PTR				Value,
__in SMF_TYPEDECL_INDEX			Type
)
{
	Dst->Type = Type;
	if (PRIMITIVE(Driver, Dst->Type)) {
		Dst->Value = Value;
	}
	else {
		Dst->Value = SmfAllocateType(Driver, Dst->Type);
		CLONE(Driver, Dst->Type)(Driver, (PVOID)Dst->Value, (PVOID)Value);
	}
}

//
// Clone a packed value
//
VOID
Clone_PackedValue(
__in PSMF_DRIVERDECL			Driver,
__in PSMF_PACKED_VALUE			Dst,
__in PSMF_PACKED_VALUE			Src
)
{
	Dst->Type = Src->Type;

	if (PRIMITIVE(Driver, Dst->Type)) {
		Dst->Value = Src->Value;
	}
	else {
		Dst->Value = SmfAllocateType(Driver, Dst->Type);
		CLONE(Driver, Dst->Type)(Driver, (PVOID)Dst->Value, (PVOID)Src->Value);
	}
}

//
// Pack a value in a preallocated piece of memory 
//
VOID
Destroy_PackedValue(
__in PSMF_DRIVERDECL			Driver,
__in PSMF_PACKED_VALUE			Obj
)
{
	if (!PRIMITIVE(Driver, Obj->Type)) {
		SmfFreeType(Driver, Obj->Type, (PVOID)Obj->Value);
	}
}
