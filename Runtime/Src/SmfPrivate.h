/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

    SmfPrivate.h

Abstract:
    This header file contains declarations for functions used internally by P Runtime, 
	these are private functions and should not be called out side of P runtime.

Environment:

    Kernel mode only.		

***********************************************************************************/


#pragma once
#include "SmfProtected.h"
#include "SmfPublicTypes.h"



#ifdef KERNEL_MODE
/*********************************************************************************

		Functions used for executing the state machine.

*********************************************************************************/
//
//Service Worker Item in Worker Item Queue
//
IO_WORKITEM_ROUTINE_EX SmfRunStateMachineWorkItemNonBlocking;

VOID 
SmfRunStateMachineWorkItemNonBlocking(
_In_ PVOID IoObject,
_In_opt_ PVOID Context,
_In_ PIO_WORKITEM IoWorkItem
);
//
//Enqueue the current statemachine execution onto the workerItem list
//
VOID 
SmfEnqueueStateMachineAsWorkerItemNonBlocking(
__in PSMF_SMCONTEXT		Context
);

IO_WORKITEM_ROUTINE_EX SmfRunStateMachineWorkItemPassiveFlag;

VOID 
SmfRunStateMachineWorkItemPassiveFlag(
_In_ PVOID IoObject,
_In_opt_ PVOID Context,
_In_ PIO_WORKITEM IoWorkItem
);
//
//Enqueue the current statemachine execution onto the workerItem list
//
VOID 
SmfEnqueueStateMachineAsWorkerItemPassiveFlag(
__in PSMF_SMCONTEXT		Context
);


//
//Check if the Entry Function Needs Passive Level Execution 
//
BOOLEAN 
SmfIsEntryFunRequiresPassiveLevel(
__in PSMF_SMCONTEXT		Context
);

//
//Check if the Exit Function Needs Passive Level Execution 
//
BOOLEAN 
SmfIsExitFunRequiresPassiveLevel(
__in PSMF_SMCONTEXT		Context
);

#endif

//
// Convenience Macros for manipulating complex types
//
#define TYPEDEF(Driver, RefType)	((Driver)->Types[(RefType)])
#define CLONE(Driver, RefType)	((PSMF_CLONEFUN)(Driver)->Types[(RefType)].Clone)
#define BUILD_DEFAULT(Driver, RefType)	((PSMF_BUILDDEFFUN)(Driver)->Types[(RefType)].BuildDefault)
#define DESTROY(Driver, RefType)	((PSMF_DESTROYFUN)(Driver)->Types[(RefType)].Destroy)
#define EQUALS(Driver, RefType)	((PSMF_EQUALSFUN)(Driver)->Types[(RefType)].Equals)
#define HASHCODE(Driver, RefType)	((PSMF_HASHCODEFUN)(Driver)->Types[(RefType)].HashCode)
#define PRIMITIVE(Driver, RefType)	((BOOLEAN)(Driver)->Types[(RefType)].Primitive)

//
// Run the machine until no further work can be done.
// DoEntry is true if called and entry function should be executed
//

_IRQL_requires_(DISPATCH_LEVEL)
_Requires_lock_held_(Context->StateMachineLock)
_Releases_lock_(Context->StateMachineLock)
VOID 
SmfRunStateMachine(
__inout _At_(Context->Irql, _IRQL_restores_)
    PSMF_SMCONTEXT	    Context,
__in BOOLEAN			DoEntryOrExit
);                                                              

//
//Dequeue an event given the current state of the context
//
SMF_TRIGGER 
SmfDequeueEvent(
__inout PSMF_SMCONTEXT	Context
);        

//
// Makes transition to the next state given a non-deferred input event 
// (may Pop state on an unhandled event exception)
//
VOID 
SmfTakeTransition(
__inout PSMF_SMCONTEXT		Context, 
__in SMF_EVENTDECL_INDEX	EventIndex
);  

//
//check if the current state has After Transition and if it does take the transition 
//and execute exit function .
//
VOID 
SmfTakeDefaultTransition(
__inout PSMF_SMCONTEXT		Context
);

//
// Push the current state onto the call stack and adds
// the deferred list of the current state to the Context's
// deferred list.
//
VOID 
SmfPushState(
__inout PSMF_SMCONTEXT		Context,
__in	BOOLEAN				isCallStatement
);

//
// Pops the current state from call stack and removes
// its events from the Context's deferred list.
//
VOID 
SmfPopState(
__inout PSMF_SMCONTEXT		Context, 
__in BOOLEAN				RestoreTrigger
);



/*********************************************************************************

		Functions used for Life Time Management of the statemachine.

*********************************************************************************/
//
//Remove State Machine Free all the memory allocated to this statemachine
//
VOID 
SmfRemoveMachine(
__in PSMF_SMCONTEXT			Context
);


/*********************************************************************************

		Functions to get machine handles and pointers.

*********************************************************************************/
//
//Get machine handle from machine Pointer
//
FORCEINLINE
SMF_MACHINE_HANDLE 
SmfGetStateMachineHandle(
__in PSMF_SMCONTEXT			Context
);

//
//Get machine Pointer back from the machine Handle
//
FORCEINLINE
PSMF_SMCONTEXT 
SmfGetStateMachinePointer(
__in SMF_MACHINE_HANDLE				Handle
);

#ifdef DISTRIBUTED_RUNTIME
FORCEINLINE
SMF_MACHINE_HANDLE 
SmfGetStateMachineHandleRemote(
__in PSMF_SMCONTEXT_REMOTE			Context
);

FORCEINLINE
PSMF_SMCONTEXT_REMOTE
SmfGetStateMachinePointerRemote(
__in SMF_MACHINE_HANDLE				Handle
);
#endif

/*********************************************************************************

		User Mode/ Kernel Mode Functions.

*********************************************************************************/
FORCEINLINE
_Acquires_lock_(Context->StateMachineLock)
_IRQL_raises_(DISPATCH_LEVEL)
VOID 
SmfAcquireLock(
_In_ _At_(Context->Irql, _IRQL_saves_)
    PSMF_SMCONTEXT	Context
);

FORCEINLINE
_IRQL_requires_(DISPATCH_LEVEL)
_Requires_lock_held_(Context->StateMachineLock)
_Releases_lock_(Context->StateMachineLock)
VOID 
SmfReleaseLock(
_In_ _At_(Context->Irql, _IRQL_restores_)
    PSMF_SMCONTEXT	Context
);

FORCEINLINE
VOID 
SmfInitializeLock(
PSMF_SMCONTEXT				Context
);


/*********************************************************************************

		Helper Functions.

*********************************************************************************/

//
// Dynamically resize the queue
//
UCHAR
SmfResizeEventQueue(
__in PSMF_SMCONTEXT Context
);

//
//Check if the current events maxinstance exceeded
//
BOOLEAN 
SmfIsEventMaxInstanceExceeded(
__in PSMF_EVENTQUEUE		Queue, 
__in SMF_EVENTDECL_INDEX	EventIndex,
__in UINT16					MaxInstances,
__in UINT16					QueueSize
);

//
// Check if the Current State has out-going After Transition
//
FORCEINLINE 
BOOLEAN 
SmfStateHasDefaultTransition(
__in PSMF_SMCONTEXT			Context
);

//
// Get the Current State Decl
//
FORCEINLINE
SMF_STATEDECL
SmfGetCurrentStateDecl(
__in PSMF_SMCONTEXT			Context
);

//
// Check if the event is deferred in the current state
//
FORCEINLINE
BOOLEAN 
SmfIsEventDeferred(
__in SMF_EVENTDECL_INDEX	EventIndex, 
SMF_EVENTDECL_INDEX_PACKEDTABLE	
							DefSet
);

//
// Check if the transition corresponding to event exits in the current state
//
FORCEINLINE
BOOLEAN 
SmfIsTransitionPresent(
__in SMF_EVENTDECL_INDEX	EventIndex, 
__in PSMF_SMCONTEXT			Context 
);


FORCEINLINE
BOOLEAN 
SmfIsActionInstalled(
__in SMF_EVENTDECL_INDEX	EventIndex, 
SMF_ACTIONDECL_INDEX_PACKEDTABLE	
							ActionSet
);

//
// Check if the Event Buffer is Empty
//
FORCEINLINE
BOOLEAN 
SmfIsQueueEmpty(
__in PSMF_EVENTQUEUE		Queue
);

//
// Gets the exit function of the current state
//
FORCEINLINE 
PSMF_EXITFUN 
SmfGetExitFunction(
__in PSMF_SMCONTEXT			Context
);

//
// Gets the entry function of the current state
//
FORCEINLINE 
PSMF_ENTRYFUN 
SmfGetEntryFunction(
__in PSMF_SMCONTEXT			Context
);

//
// Gets the correct action function for the event
//
FORCEINLINE 
PSMF_ACTIONDECL
SmfGetAction(
__in PSMF_SMCONTEXT			Context
);

//
// Gets the packed actions of StateIndex
//

FORCEINLINE 
SMF_ACTIONDECL_INDEX_PACKEDTABLE 
SmfGetActionsPacked(
__in PSMF_SMCONTEXT			Context,
__in SMF_STATEDECL_INDEX	StateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE 
SMF_EVENTDECL_INDEX_PACKEDTABLE 
SmfGetDeferredPacked(
__in PSMF_SMCONTEXT			Context,
__in SMF_STATEDECL_INDEX	StateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE 
SMF_TRANSDECL_TABLE 
SmfGetTransTable(
__in PSMF_SMCONTEXT			Context,
__in SMF_STATEDECL_INDEX	StateIndex,
__out UINT16				*NTransitions);

//
// Gets the size of the Packed defered/action/transition set  
//
FORCEINLINE 
UINT16 
SmfGetPackSize(
__in PSMF_SMCONTEXT			Context
);


//
// Update the Transition History 
//
FORCEINLINE
VOID SmfUpdateTransitionHistory(
__in PSMF_SMCONTEXT				Context, 
__in SMF_TRANSHISTORY_STEP		Step,
__in SMF_EVENTDECL_INDEX		EventIndex, 
__in SMF_STATEDECL_INDEX		StateEntered
);

//
// Check if the transition on event is a call transition
//
BOOLEAN
SmfIsCallTransition(
PSMF_SMCONTEXT			Context,
SMF_EVENTDECL_INDEX		Event
);


//
// Create a clone of packed set
//
PVOID
SmfClonePackedSet(
PVOID					PackedSet,
UINT					Size
);

//
// Calculate Actions set for the current State 
//
VOID
SmfUpdateCurrentActionsSet(
PSMF_SMCONTEXT			Context
);

//
// Calculate Deferred set for the current State
//
VOID
SmfUpdateCurrentDeferredSet(
PSMF_SMCONTEXT			Context
);

//
// Free the allocated memory of SMContext
//
VOID
SmfFreeSMContext(
PSMF_SMCONTEXT			Context
);
