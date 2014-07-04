/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

PrtSMPrivate.h

Abstract:
This header file contains declarations for functions used internally by P Runtime,
these are private functions and should not be called out side of P runtime.

Environment:

Kernel mode only.

***********************************************************************************/


#pragma once
#include "PrtSMProtected.h"
#include "PrtSMPublicTypes.h"
#include "Config\PrtConfig.h"
#include "Values\PrtDTTypes.h"
#include "Values\PrtDTValues.h"

//
// Run the machine until no further work can be done.
// DoEntry is true if called and entry function should be executed
//

_IRQL_requires_(DISPATCH_LEVEL)
_Requires_lock_held_(Context->StateMachineLock)
_Releases_lock_(Context->StateMachineLock)
VOID
PrtRunStateMachine(
__inout _At_(Context->Irql, _IRQL_restores_)
PPRT_SMCONTEXT	    context,
__in PRT_BOOLEAN	doEntryOrExit
);


VOID
PrtEnqueueEvent(
__in PRT_MACHINE_HANDLE			machine,
__in PRT_EVENTDECL_INDEX		eventIndex,
__in PPRT_VALUE					payload,
);

//
//Dequeue an event given the current state of the context
//
PRT_TRIGGER
PrtDequeueEvent(
__inout PPRT_SMCONTEXT	context
);

//
// Makes transition to the next state given a non-deferred input event 
// (may Pop state on an unhandled event exception)
//
VOID
PrtTakeTransition(
__inout PPRT_SMCONTEXT		context,
__in PRT_EVENTDECL_INDEX	eventIndex
);

//
//check if the current state has After Transition and if it does take the transition 
//and execute exit function .
//
VOID
PrtTakeDefaultTransition(
__inout PPRT_SMCONTEXT		context
);

//
// Push the current state onto the call stack and adds
// the deferred list of the current state to the Context's
// deferred list.
//
VOID
PrtPushState(
__inout PPRT_SMCONTEXT		context,
__in	PRT_BOOLEAN			isCallStatement
);

//
// Pops the current state from call stack and removes
// its events from the Context's deferred list.
//
VOID
PrtPopState(
__inout PPRT_SMCONTEXT		context,
__in PRT_BOOLEAN			restoreTrigger
);



/*********************************************************************************

Functions used for Life Time Management of the statemachine.

*********************************************************************************/
//
//Remove State Machine Free all the memory allocated to this statemachine
//
VOID
PrtRemoveMachine(
__in PPRT_SMCONTEXT			context
);


/*********************************************************************************

Functions to get machine handles and pointers.

*********************************************************************************/
//
//Get machine handle from machine Pointer
//
FORCEINLINE
PRT_MACHINE_HANDLE
PrtGetStateMachineHandle(
__in PPRT_SMCONTEXT			context
);

//
//Get machine Pointer back from the machine Handle
//
FORCEINLINE
PPRT_SMCONTEXT
PrtGetStateMachinePointer(
__in PRT_MACHINE_HANDLE				handle
);


/*********************************************************************************

User Mode/ Kernel Mode Functions.

*********************************************************************************/
FORCEINLINE
_Acquires_lock_(Context->StateMachineLock)
_IRQL_raises_(DISPATCH_LEVEL)
VOID
PrtAcquireLock(
_In_ _At_(Context->Irql, _IRQL_saves_)
PPRT_SMCONTEXT	context
);

FORCEINLINE
_IRQL_requires_(DISPATCH_LEVEL)
_Requires_lock_held_(Context->StateMachineLock)
_Releases_lock_(Context->StateMachineLock)
VOID
PrtReleaseLock(
_In_ _At_(Context->Irql, _IRQL_restores_)
PPRT_SMCONTEXT	context
);

FORCEINLINE
VOID
PrtInitializeLock(
PPRT_SMCONTEXT				context
);


/*********************************************************************************

Helper Functions.

*********************************************************************************/

//
// Dynamically resize the queue
//
PRT_INT16
PrtResizeEventQueue(
__in PPRT_SMCONTEXT context
);

//
//Check if the current events maxinstance exceeded
//
PRT_BOOLEAN
PrtIsEventMaxInstanceExceeded(
__in PPRT_EVENTQUEUE			queue,
__in PRT_EVENTDECL_INDEX	eventIndex,
__in PRT_UINT16					maxInstances,
__in PRT_UINT16					queueSize
);

//
// Check if the Current State has out-going After Transition
//
FORCEINLINE
PRT_BOOLEAN
PrtStateHasDefaultTransition(
__in PPRT_SMCONTEXT			context
);

//
// Get the Current State Decl
//
FORCEINLINE
PRT_STATEDECL
PrtGetCurrentStateDecl(
__in PPRT_SMCONTEXT			context
);

//
// Check if the event is deferred in the current state
//
FORCEINLINE
PRT_BOOLEAN
PrtIsEventDeferred(
__in PRT_EVENTDECL_INDEX	eventIndex,
PRT_EVENTDECL_INDEX_PACKEDTABLE defSet
);

//
// Check if the transition corresponding to event exits in the current state
//
FORCEINLINE
PRT_BOOLEAN
PrtIsTransitionPresent(
__in PRT_EVENTDECL_INDEX	eventIndex,
__in PPRT_SMCONTEXT			context
);


FORCEINLINE
PRT_BOOLEAN
PrtIsActionInstalled(
__in PRT_EVENTDECL_INDEX	eventIndex,
PRT_ACTIONDECL_INDEX_PACKEDTABLE actionSet
);

//
// Check if the Event Buffer is Empty
//
FORCEINLINE
PRT_BOOLEAN
PrtIsQueueEmpty(
__in PPRT_EVENTQUEUE		queue
);

//
// Gets the exit function of the current state
//
FORCEINLINE
PPRT_EXITFUN
PrtGetExitFunction(
__in PPRT_SMCONTEXT		context
);

//
// Gets the entry function of the current state
//
FORCEINLINE
PPRT_ENTRYFUN
PrtGetEntryFunction(
__in PPRT_SMCONTEXT		context
);

//
// Gets the correct action function for the event
//
FORCEINLINE
PPRT_ACTIONDECL
PrtGetAction(
__in PPRT_SMCONTEXT			context
);

//
// Gets the packed actions of StateIndex
//

FORCEINLINE
PRT_ACTIONDECL_INDEX_PACKEDTABLE
PrtGetActionsPacked(
__in PPRT_SMCONTEXT			context,
__in PRT_STATEDECL_INDEX	stateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE
PRT_EVENTDECL_INDEX_PACKEDTABLE
PrtGetDeferredPacked(
__in PPRT_SMCONTEXT			context,
__in PRT_STATEDECL_INDEX	stateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE
PRT_TRANSDECL_TABLE
PrtGetTransTable(
__in PPRT_SMCONTEXT			context,
__in PRT_STATEDECL_INDEX	stateIndex,
__out PPRT_UINT16			nTransitions);

//
// Gets the size of the Packed defered/action/transition set  
//
FORCEINLINE
PRT_UINT16
PrtGetPackSize(
__in PPRT_SMCONTEXT			context
);


//
// Update the Transition History 
//
FORCEINLINE
VOID PrtUpdateTransitionHistory(
__in PPRT_SMCONTEXT				context,
__in PRT_TRANSHISTORY_STEP		step,
__in PRT_EVENTDECL_INDEX		eventIndex,
__in PRT_STATEDECL_INDEX		stateEntered
);

//
// Check if the transition on event is a call transition
//
PRT_BOOLEAN
PrtIsCallTransition(
PPRT_SMCONTEXT			context,
PRT_EVENTDECL_INDEX		event
);


//
// Create a clone of packed set
//
PVOID
PrtClonePackedSet(
PVOID					packedSet,
UINT					size
);

//
// Calculate Actions set for the current State 
//
VOID
PrtUpdateCurrentActionsSet(
PPRT_SMCONTEXT			context
);

//
// Calculate Deferred set for the current State
//
VOID
PrtUpdateCurrentDeferredSet(
PPRT_SMCONTEXT			context
);

//
// Free the allocated memory of SMContext
//
VOID
PrtFreeSMContext(
PPRT_SMCONTEXT			context
);
