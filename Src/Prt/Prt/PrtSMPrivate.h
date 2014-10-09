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
#include "PrtConfig.h"
#include "PrtSMTypeDefs.h"

/*********************************************************************************

Private Functions

*********************************************************************************/
//
// Run the machine until no further work can be done.
// DoEntry is true if called and entry function should be executed
//

_IRQL_requires_(DISPATCH_LEVEL)
_Requires_lock_held_(Context->StateMachineLock)
_Releases_lock_(Context->StateMachineLock)
void
PrtRunStateMachine(
__inout _At_(Context->Irql, _IRQL_restores_)
PRT_SMCONTEXT	    *context,
__in PRT_BOOLEAN	doEntryOrExit
);


void
PrtEnqueueEvent(
__in PRT_MACHINE_HANDLE			machine,
__in PRT_VALUE					*event,
__in PRT_VALUE					*payload
);

//
//Dequeue an event given the current state of the context
//
PRT_TRIGGER
PrtDequeueEvent(
__inout PRT_SMCONTEXT	*context
);

//
// Makes transition to the next state given a non-deferred input event 
// (may Pop state on an unhandled event exception)
//
void
PrtTakeTransition(
__inout PRT_SMCONTEXT		*context,
__in PRT_UINT32				eventIndex
);

//
//check if the current state has After Transition and if it does take the transition 
//and execute exit function .
//
void
PrtTakeDefaultTransition(
__inout PRT_SMCONTEXT		*context
);

//
// Push the current state onto the call stack and adds
// the deferred list of the current state to the Context's
// deferred list.
//
void
PrtPushState(
__inout PRT_SMCONTEXT		*context,
__in	PRT_BOOLEAN			isCallStatement
);

//
// Pops the current state from call stack and removes
// its events from the Context's deferred list.
//
void
PrtPopState(
__inout PRT_SMCONTEXT		*context,
__in PRT_BOOLEAN			restoreTrigger
);


PRT_TYPE
PrtGetPayloadType(
PRT_SMCONTEXT *context,
PRT_VALUE	  *event
);


/*********************************************************************************

Functions used for Life Time Management of the statemachine.

*********************************************************************************/
//
//Remove State Machine Free all the memory allocated to this statemachine
//
void
PrtRemoveMachine(
__in PRT_SMCONTEXT			*context
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
__in PRT_SMCONTEXT			*context
);

//
//Get machine Pointer back from the machine Handle
//
FORCEINLINE
PRT_SMCONTEXT *
PrtGetStateMachinePointer(
__in PRT_MACHINE_HANDLE				handle
);


/*********************************************************************************

Helper Functions.

*********************************************************************************/
//
// Call Exception handler
//
void
PrtExceptionHandler(
__in PRT_EXCEPTIONS ex,
__in PRT_SMCONTEXT *context
);


//
// Call external logging call back
//
void
PrtLog(
__in PRT_STEP step,
__in PRT_SMCONTEXT *context
);


//
// Dynamically resize the queue
//
PRT_INT16
PrtResizeEventQueue(
__in PRT_SMCONTEXT *context
);

//
//Check if the current events maxinstance exceeded
//
PRT_BOOLEAN
PrtIsEventMaxInstanceExceeded(
__in PRT_EVENTQUEUE			*queue,
__in PRT_UINT32				eventIndex,
__in PRT_UINT32				maxInstances,
__in PRT_UINT16				queueSize
);

//
// Check if the Current State has out-going After Transition
//
FORCEINLINE
PRT_BOOLEAN
PrtStateHasDefaultTransition(
__in PRT_SMCONTEXT			*context
);

//
// Get the Current State Decl
//
FORCEINLINE
PRT_STATEDECL
PrtGetCurrentStateDecl(
__in PRT_SMCONTEXT			*context
);

//
// Check if the event is deferred in the current state
//
FORCEINLINE
PRT_BOOLEAN
PrtIsEventDeferred(
__in PRT_UINT32		eventIndex,
__in PRT_UINT32*		defSet
);

//
// Check if the transition corresponding to event exits in the current state
//
FORCEINLINE
PRT_BOOLEAN
PrtIsTransitionPresent(
__in PRT_UINT32				eventIndex,
__in PRT_SMCONTEXT			*context
);


FORCEINLINE
PRT_BOOLEAN
PrtIsActionInstalled(
__in PRT_UINT32		eventIndex,
__in PRT_UINT32*		actionSet
);

//
// Check if the Event Buffer is Empty
//
FORCEINLINE
PRT_BOOLEAN
PrtIsQueueEmpty(
__in PRT_EVENTQUEUE		*queue
);

//
// Gets the exit function of the current state
//
FORCEINLINE
PRT_MACHINE_FUN*
PrtGetExitFunction(
__in PRT_SMCONTEXT		*context
);

//
// Gets the entry function of the current state
//
FORCEINLINE
PRT_MACHINE_FUN*
PrtGetEntryFunction(
__in PRT_SMCONTEXT		*context
);

//
// Gets the correct action function for the event
//
FORCEINLINE
PRT_ACTIONDECL*
PrtGetAction(
__in PRT_SMCONTEXT		*context
);

//
// Gets the packed actions of StateIndex
//

FORCEINLINE
PRT_UINT32*
PrtGetActionsPacked(
__in PRT_SMCONTEXT			*context,
__in PRT_UINT32				stateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE
PRT_UINT32*
PrtGetDeferredPacked(
__in PRT_SMCONTEXT			*context,
__in PRT_UINT32				stateIndex
);

FORCEINLINE
PRT_UINT32*
PrtGetTransitionsPacked(
__in PRT_SMCONTEXT			*context,
__in PRT_UINT32				stateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE
PRT_TRANSDECL_TABLE
PrtGetTransTable(
__in PRT_SMCONTEXT			*context,
__in PRT_UINT32				stateIndex,
__out PRT_UINT32			*nTransitions
);

//
// Gets the size of the Packed defered/action/transition set  
//
FORCEINLINE
PRT_UINT16
PrtGetPackSize(
__in PRT_SMCONTEXT			*context
);


//
// Check if the transition on event is a call transition
//
PRT_BOOLEAN
PrtIsCallTransition(
PRT_SMCONTEXT			*context,
PRT_UINT32				event
);


//
// Create a clone of packed set
//
void*
PrtClonePackedSet(
void*					packedSet,
PRT_UINT32					size
);

//
// Calculate Actions set for the current State 
//
void
PrtUpdateCurrentActionsSet(
PRT_SMCONTEXT			*context
);

//
// Calculate Deferred set for the current State
//
void
PrtUpdateCurrentDeferredSet(
PRT_SMCONTEXT			*context
);

//
// Free the allocated memory of SMContext
//
void
PrtFreeSMContext(
PRT_SMCONTEXT			*context
);
