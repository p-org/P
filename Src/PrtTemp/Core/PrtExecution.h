#include "../API/Prt.h"

typedef struct PRT_PROCESS_PRIV {
	PRT_PROCESS			    process;
	PRT_ERROR_FUN	        errorHandler;
	PRT_LOG_FUN				log;
	PRT_RECURSIVE_MUTEX		lock;
	PRT_UINT32				numMachines;
	PRT_UINT32				machineCount;
	PRT_SMCONTEXT			**machines;
} PRT_PROCESS_PRIV;

//
// To specify where to return in entry or exit functions
//
typedef enum PRT_RETURNTO
{
	//
	// If ReturnTo points to this value then the call was a call edge/transition
	// and control should return to dequeue
	//
	PrtEntryFunEnd = INT_MAX,
	//
	// If ReturnTo points to this value then the call returns to the start of Entry Function
	//
	PrtEntryFunStart = 0,
	//
	// If ReturnTo points to this value then the call returns to the start of Exit function
	//
	PrtExitFunStart = 0,

	//
	// If ReturnTo points to this value then the call returns to the start of Action
	//
	PrtActionFunStart = 0
} PRT_RETURNTO;

//
// To indicate whether to execute entry or exit function
//
typedef enum PRT_STATE_EXECFUN
{
	//
	// If StateExecFun points to StateEntry, then we should execute the entry function for this state
	//
	PrtStateEntry,
	//
	// If StateExecFun points to StateExit, then we should execute the exit function for this state
	//
	PrtStateExit,

	//
	// If StateExecFun points to StateAction, then we should execute the action corresponding to trigger event for the current state
	//
	PrtStateAction
	//
	// None of the above just execute the dequeue function
	//
} PRT_STATE_EXECFUN;

/*********************************************************************************

Type Name : PRT_LASTOPERATION

Description :
Enum for last P operation performed inside entry/exit function
*********************************************************************************/
typedef enum PRT_LASTOPERATION
{
	PopStatement,
	RaiseStatement,
	CallStatement,
	OtherStatement
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

Type Name : PRT_TRIGGER

Description :
Structure to store Trigger value for a state-machine,
Trigger value in entry function indicates the event on which we entered the state
Tigger value in exit function indicates the event on which we leave a state

Fields :

Event --
Event on which we enter or exit a state

Arg --
Arg Value corresponding to Event

*********************************************************************************/

typedef struct PRT_TRIGGER
{
	PRT_VALUE *event;
	PRT_VALUE *payload;
} PRT_TRIGGER;


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

IsFull --
<TRUE> -- if event queue is full
<FALSE> -- if event queue is not full

*********************************************************************************/
typedef struct PRT_EVENTQUEUE
{
	PRT_TRIGGER		*events;
	PRT_UINT16		 headIndex;
	PRT_UINT16		 tailIndex;
	PRT_UINT16		 size;
	PRT_BOOLEAN		 isFull;
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
	PRT_TRIGGER			trigger;
	PRT_UINT16			returnTo;
	PRT_STATE_EXECFUN	stateExecFun;
	PRT_UINT32*			inheritedDefSetCompact;
	PRT_UINT32*			inheritedActSetCompact;
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

typedef struct PRT_SMCONTEXT_PRIV {
	PRT_SMCONTEXT		context;
	PRT_UINT32			currentState;
	PRT_TRIGGER			trigger;
	PRT_UINT16			returnTo;
	PRT_STATE_EXECFUN	stateExecFun;
	PRT_BOOLEAN			isRunning;
	PRT_BOOLEAN			isHalted;
	PRT_STATESTACK		callStack;
	PRT_EVENTQUEUE		eventQueue;
	PRT_UINT8			currentLengthOfEventQueue;
	PRT_UINT32*			inheritedDeferredSetCompact;
	PRT_UINT32*			currentDeferredSetCompact;
	PRT_UINT32*			inheritedActionsSetCompact;
	PRT_UINT32*			currentActionsSetCompact;
	PRT_LASTOPERATION	lastOperation;
	PRT_RECURSIVE_MUTEX stateMachineLock;
} PRT_SMCONTEXT_PRIV;

//
//Enqueue a private event 
//
void
PrtRaise(
__inout PRT_SMCONTEXT_PRIV		*context,
__in PRT_VALUE	*event,
__in PRT_VALUE	*payload
);

//
// Pop Current state and return to the caller state
//

void
PrtPop(
__inout PRT_SMCONTEXT_PRIV		*context
);

//
// Execute Call Statement
//
void
PrtPush(
__inout PRT_SMCONTEXT_PRIV		*context,
__in PRT_UINT32				stateIndex
);

//
// Run the machine until no further work can be done.
// DoEntry is true if called and entry function should be executed
//

void
PrtRunStateMachine(
__inout
PRT_SMCONTEXT_PRIV	    *context,
__in PRT_BOOLEAN	doEntryOrExit
);


//
//Dequeue an event given the current state of the context
//
PRT_TRIGGER
PrtDequeueEvent(
__inout PRT_SMCONTEXT_PRIV	*context
);

//
// Makes transition to the next state given a non-deferred input event 
// (may Pop state on an unhandled event exception)
//
void
PrtTakeTransition(
__inout PRT_SMCONTEXT_PRIV		*context,
__in PRT_UINT32				eventIndex
);

//
//check if the current state has After Transition and if it does take the transition 
//and execute exit function .
//
void
PrtTakeDefaultTransition(
__inout PRT_SMCONTEXT_PRIV		*context
);

//
// Push the current state onto the call stack and adds
// the deferred list of the current state to the Context's
// deferred list.
//
void
PrtPushState(
__inout PRT_SMCONTEXT_PRIV	*context,
__in	PRT_BOOLEAN			isCallStatement
);

//
// Pops the current state from call stack and removes
// its events from the Context's deferred list.
//
void
PrtPopState(
__inout PRT_SMCONTEXT_PRIV		*context,
__in PRT_BOOLEAN			restoreTrigger
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

Helper Functions.

*********************************************************************************/

PRT_TYPE*
PrtGetPayloadType(
PRT_SMCONTEXT_PRIV *context,
PRT_VALUE	  *event
);

void
PrtHaltMachine(
__inout PRT_SMCONTEXT_PRIV			*context
);

//
// Call Exception handler
//
void
PrtExceptionHandler(
__in PRT_STATUS ex,
__in PRT_SMCONTEXT_PRIV *context
);


//
// Call external logging call back
//
void
PrtLog(
__in PRT_STEP step,
__in PRT_SMCONTEXT_PRIV *context
);


//
// Dynamically resize the queue
//
PRT_INT16
PrtResizeEventQueue(
__in PRT_SMCONTEXT_PRIV *context
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
// Check if the Current State has out-going default Transition
//
FORCEINLINE
PRT_BOOLEAN
PrtStateHasDefaultTransition(
__in PRT_SMCONTEXT_PRIV			*context
);

//
// Get the Current State Decl
//
FORCEINLINE
PRT_STATEDECL
PrtGetCurrentStateDecl(
__in PRT_SMCONTEXT_PRIV			*context
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
__in PRT_SMCONTEXT_PRIV			*context
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
PRT_SM_FUN
PrtGetExitFunction(
__in PRT_SMCONTEXT_PRIV		*context
);

//
// Gets the entry function of the current state
//
FORCEINLINE
PRT_SM_FUN
PrtGetEntryFunction(
__in PRT_SMCONTEXT_PRIV		*context
);

//
// Gets the correct action function for the event
//
FORCEINLINE
PRT_DODECL*
PrtGetAction(
__in PRT_SMCONTEXT_PRIV		*context
);

//
// Gets the packed actions of StateIndex
//

FORCEINLINE
PRT_UINT32*
PrtGetActionsPacked(
__in PRT_SMCONTEXT_PRIV			*context,
__in PRT_UINT32				stateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE
PRT_UINT32*
PrtGetDeferredPacked(
__in PRT_SMCONTEXT_PRIV			*context,
__in PRT_UINT32				stateIndex
);

FORCEINLINE
PRT_UINT32*
PrtGetTransitionsPacked(
__in PRT_SMCONTEXT_PRIV			*context,
__in PRT_UINT32				stateIndex
);

//
// Gets the packed deferred events of StateIndex
//
FORCEINLINE
PRT_TRANSDECL*
PrtGetTransTable(
__in PRT_SMCONTEXT_PRIV		*context,
__in PRT_UINT32				stateIndex,
__out PRT_UINT32			*nTransitions
);

//
// Gets the size of the Packed defered/action/transition set  
//
FORCEINLINE
PRT_UINT16
PrtGetPackSize(
__in PRT_SMCONTEXT_PRIV			*context
);


//
// Check if the transition on event is a call transition
//
PRT_BOOLEAN
PrtIsCallTransition(
PRT_SMCONTEXT_PRIV			*context,
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
PRT_SMCONTEXT_PRIV			*context
);

//
// Calculate Deferred set for the current State
//
void
PrtUpdateCurrentDeferredSet(
PRT_SMCONTEXT_PRIV			*context
);

//
// Free the allocated memory of SMContext
//
void
PrtFreeSMContext(
PRT_SMCONTEXT_PRIV			*context
);

