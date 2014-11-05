#ifndef PRT_EXECUTION_H
#define PRT_EXECUTION_H

#include "Prt.h"

#ifdef __cplusplus
extern "C"{
#endif

typedef struct PRT_PROCESS_PRIV {
	PRT_GUID				guid;
	PRT_PROGRAMDECL			*program;
	PRT_ERROR_FUN	        errorHandler;
	PRT_LOG_FUN				logHandler;
	PRT_RECURSIVE_MUTEX		processLock;
	PRT_UINT32				numMachines;
	PRT_UINT32				machineCount;
	PRT_SM_CONTEXT			**machines;
} PRT_PROCESS_PRIV;

typedef enum PRT_STATE_EXECFUN
{
	//
	// If StateExecFun points to StateEntry, then we should execute the entry function for this state
	//
	PrtStateEntry,
	//
	// If StateExecFun points to StateAction, then we should execute the action corresponding to trigger event for the current state
	//
	PrtStateAction,
	//
	// None of the above just execute the dequeue function
	//
	PrtDequeue
} PRT_STATE_EXECFUN;

/*********************************************************************************

Type Name : PRT_LASTOPERATION

Description :
Enum for last P operation performed inside entry/exit function
*********************************************************************************/
typedef enum PRT_LASTOPERATION
{
	ReturnStatement,
	PopStatement,
	RaiseStatement,
	PushStatement
} PRT_LASTOPERATION;

/*********************************************************************************

Macros Constants

*********************************************************************************/
//
// Max call stack depth of each machine
//
#define PRT_MAX_CALL_DEPTH 16 

//
// Max length of the event queue for each machine
//
#define PRT_QUEUE_LEN_DEFAULT 64


/*********************************************************************************

Type Name : PRT_EVENT

Description :
Structure to store Trigger value for a state-machine,
Trigger value in entry function indicates the event on which we entered the state
Tigger value in exit function indicates the event on which we leave a state

Fields :

event --
Event on which we enter or exit a state

payload --
Payload value corresponding to event

*********************************************************************************/

typedef struct PRT_EVENT
{
	PRT_VALUE *trigger;
	PRT_VALUE *payload;
} PRT_EVENT;


/*********************************************************************************

Type Name : PRT_EVENTQUEUE

Description :
Structure for Event buffer for a state-machine,
Event buffer implements FIFO logic and has a fixed size for a machine type
corresponding to the value specified in machine declaration

Fields :

Events --
Array of <Events, Arg>, implementing a circular queue

Head --
Head of the Event Queue, pointing to the event next.
Tail --
Tail of the Event Queue, pointing to las event
*********************************************************************************/
typedef struct PRT_EVENTQUEUE
{
	PRT_UINT32		 eventsSize;
	PRT_EVENT		*events;
	PRT_UINT32		 headIndex;
	PRT_UINT32		 tailIndex;
	PRT_UINT32		 size;
} PRT_EVENTQUEUE;

/*********************************************************************************

Type Name : PRT_STACKSTATE_INFO

Description :
Structure for Call Stack Element, storing State information on stack because
of a call statement or call edge

Fields :

StateIndex --
State to be pushed on stack

Trigger --
Local trigger value

ReturnTo --
Program counter for returning to proper statement in the entry function on a pop.

StateExecFun --
To store whether the call statement was executed from entry function or exit function
of the current state.
*********************************************************************************/

typedef struct PRT_STACKSTATE_INFO
{
	PRT_UINT32			stateIndex;
	PRT_EVENT			currEvent;
	PRT_UINT16			returnTo;
	PRT_STATE_EXECFUN	stateExecFun;
	PRT_UINT32*			inheritedDeferredSetCompact;
	PRT_UINT32*			inheritedActionsSetCompact;
} PRT_STACKSTATE_INFO;

/*********************************************************************************

Type Name : PRT_STATESTACK

Description :
Structure for Call Stack of a statemachine to push state-info on a call statement or
call edge

Fields :

StatesStack --
Array of State-Info for implementing call-stack, size of the call stack is fixed to
PRT_MAX_CALL_DEPTH

Length --
Length/depth of the call-stack
*********************************************************************************/

typedef struct PRT_STATESTACK
{
	PRT_STACKSTATE_INFO statesStack[PRT_MAX_CALL_DEPTH];
	PRT_UINT16			length;
} PRT_STATESTACK;

typedef struct PRT_SM_CONTEXT_PRIV {
	PRT_PROCESS		    *process;
	PRT_UINT32			instanceOf;
	PRT_VALUE			*id;  
	void				*extContext;
	PRT_BOOLEAN			isModel;
	PRT_VALUE			**varValues;
	PRT_EVENT			currEvent;
	PRT_RECURSIVE_MUTEX stateMachineLock;
	PRT_BOOLEAN			isRunning;
	PRT_BOOLEAN			isHalted;
	PRT_UINT32			currentState;
	PRT_STATESTACK		callStack;
	PRT_EVENTQUEUE		eventQueue;
	PRT_LASTOPERATION	lastOperation;
	PRT_STATE_EXECFUN	stateExecFun;
	PRT_UINT16			returnTo;
	PRT_UINT32*			inheritedDeferredSetCompact;
	PRT_UINT32*			currentDeferredSetCompact;
	PRT_UINT32*			inheritedActionsSetCompact;
	PRT_UINT32*			currentActionsSetCompact;
} PRT_SM_CONTEXT_PRIV;


//
// Raise event 
//
PRT_API void PRT_CALL_CONV
PrtRaise(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_VALUE					*event,
__in PRT_VALUE					*payload
);

//
// Pop current state and return to the caller state
//
PRT_API void PRT_CALL_CONV
PrtPop(
__inout PRT_SM_CONTEXT_PRIV		*context
);

//
// Execute push statement
//
PRT_API void PRT_CALL_CONV
PrtPush(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_UINT32					stateIndex
);

PRT_SM_CONTEXT_PRIV *
PrtMkMachinePrivate(
	__in  PRT_PROCESS_PRIV			*process,
	__in  PRT_UINT32				instanceOf,
	__in  PRT_VALUE					*payload
);

PRT_BOOLEAN 
PrtAreGuidsEqual(
	__in PRT_GUID guid1, 
	__in PRT_GUID guid2
);

void
PrtSendPrivate(
__in PRT_SM_CONTEXT_PRIV		*context,
__in PRT_VALUE					*event,
__in PRT_VALUE					*payload
);

//
// Run the machine until no further work can be done.
// DoEntry is true if called and entry function should be executed
//
void
PrtRunStateMachine(
__inout
PRT_SM_CONTEXT_PRIV	    *context,
__in PRT_BOOLEAN	doDequeue
);


//
//Dequeue an event given the current state of the context
//
PRT_BOOLEAN
PrtDequeueEvent(
__inout PRT_SM_CONTEXT_PRIV	*context
);

PRT_UINT32
PrtFindTransition(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_UINT32					eventIndex
);

void
PrtTakeTransition(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_UINT32				eventIndex
);

//
// Push the current state onto the call stack and adds
// the deferred list of the current state to the Context's
// deferred list.
//
void
PrtPushState(
__inout PRT_SM_CONTEXT_PRIV	*context,
__in	PRT_BOOLEAN			isCallStatement
);

//
// Pops the current state from call stack and removes
// its events from the Context's deferred list.
//
void
PrtPopState(
__inout PRT_SM_CONTEXT_PRIV		*context,
__in PRT_BOOLEAN			restoreTrigger
);


/*********************************************************************************

Helper Functions.

*********************************************************************************/

PRT_TYPE*
PrtGetPayloadType(
PRT_SM_CONTEXT_PRIV *context,
PRT_VALUE	  *event
);

void
PrtHaltMachine(
__inout PRT_SM_CONTEXT_PRIV			*context
);

//
// Call Exception handler
//
void
PrtHandleError(
__in PRT_STATUS ex,
__in PRT_SM_CONTEXT_PRIV *context
);

//
// Call external logging call back
//
void
PrtLog(
__in PRT_STEP step,
__in PRT_SM_CONTEXT_PRIV *context
);

//
// Dynamically resize the queue
//
void
PrtResizeEventQueue(
__in PRT_SM_CONTEXT_PRIV *context
);

//
//Check if the current events maxinstance exceeded
//
PRT_BOOLEAN
PrtIsEventMaxInstanceExceeded(
__in PRT_EVENTQUEUE			*queue,
__in PRT_UINT32				eventIndex,
__in PRT_UINT32				maxInstances
);

//
// Check if the Current State has out-going default Transition
//
FORCEINLINE
PRT_BOOLEAN
PrtStateHasDefaultTransition(
__in PRT_SM_CONTEXT_PRIV			*context
);

//
// Check if event is null or default
// 
FORCEINLINE
PRT_BOOLEAN
PrtIsSpecialEvent(
__in PRT_VALUE * event
);

//
// Get the Current State Decl
//
FORCEINLINE
PRT_STATEDECL *
PrtGetCurrentStateDecl(
__in PRT_SM_CONTEXT_PRIV			*context
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
__in PRT_SM_CONTEXT_PRIV	*context,
__in PRT_UINT32				eventIndex
);

FORCEINLINE
PRT_BOOLEAN
PrtIsActionInstalled(
__in PRT_UINT32		eventIndex,
__in PRT_UINT32*	actionSet
);

//
// Gets the exit function of the current state
//
FORCEINLINE
PRT_SM_FUN
PrtGetExitFunction(
__in PRT_SM_CONTEXT_PRIV		*context
);

//
// Gets the entry function of the current state
//
FORCEINLINE
PRT_SM_FUN
PrtGetEntryFunction(
__in PRT_SM_CONTEXT_PRIV		*context
);

//
// Gets the correct action function for the event
//
FORCEINLINE
PRT_DODECL*
PrtGetAction(
__in PRT_SM_CONTEXT_PRIV		*context
);

//
// Gets the packed actions of StateIndex
//

FORCEINLINE
PRT_UINT32*
PrtGetActionsPacked(
__in PRT_SM_CONTEXT_PRIV	*context,
__in PRT_UINT32				stateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE
PRT_UINT32*
PrtGetDeferredPacked(
__in PRT_SM_CONTEXT_PRIV	*context,
__in PRT_UINT32				stateIndex
);

FORCEINLINE
PRT_UINT32*
PrtGetTransitionsPacked(
__in PRT_SM_CONTEXT_PRIV	*context,
__in PRT_UINT32				stateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE
PRT_TRANSDECL*
PrtGetTransitionTable(
__in PRT_SM_CONTEXT_PRIV	*context,
__in PRT_UINT32				stateIndex,
__out PRT_UINT32			*nTransitions
);

//
// Gets the size of the Packed defered/action/transition set  
//
FORCEINLINE
PRT_UINT16
PrtGetPackSize(
__in PRT_SM_CONTEXT_PRIV			*context
);

//
// Check if the transition on event is a call transition
//
PRT_BOOLEAN
PrtIsPushTransition(
PRT_SM_CONTEXT_PRIV		*context,
PRT_UINT32				event
);

FORCEINLINE
PRT_BOOLEAN
PrtStateHasDefaultTransitionOrAction(
__in PRT_SM_CONTEXT_PRIV			*context
);

//
// Create a clone of packed set
//
PRT_UINT32 *
PrtClonePackedSet(
PRT_UINT32 *				packedSet,
PRT_UINT32					size
);

//
// Calculate Actions set for the current State 
//
void
PrtUpdateCurrentActionsSet(
PRT_SM_CONTEXT_PRIV			*context
);

//
// Calculate Deferred set for the current State
//
void
PrtUpdateCurrentDeferredSet(
PRT_SM_CONTEXT_PRIV			*context
);

void
PrtCleanupMachine(
PRT_SM_CONTEXT_PRIV			*context
);

void
PrtCleanupModel(
PRT_SM_CONTEXT			*context
);

#ifdef __cplusplus
}
#endif
#endif