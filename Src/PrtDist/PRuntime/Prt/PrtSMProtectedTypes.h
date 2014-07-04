/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

PrtProtectedTypes.h

Abstract:
This header file contains Type declarations of protected nature,
these Types should be used only inside entry/exit functions of a state
and P runtime. And included inside external driver code

Environment:

Kernel mode only.

***********************************************************************************/

#pragma once
#include "PrtSMPublicTypes.h"
#include "Config\PrtConfig.h"
#define MAX_INSTANCES_NIL INT_MAX
/*********************************************************************************

Reserved Constants

*********************************************************************************/
//
//Reserved Events 
//
enum _PRT_RESERVED_EVENT
{
	PrtResEventStart = LONG_MAX,
	//
	// If Trigger points to this value then the transition taken was a Default transition
	//
	PrtDefaultEvent = LONG_MAX - 1
};

//
//Reserved States
//
enum _PRT_RESERVED_STATE
{
	PrtResStateStart = LONG_MAX
};

//
// To specify where to return in entry or exit functions
//
enum _PRT_RETURNTO
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
	// If ReturnTo points to this value then the call returnds to the start of Exit function
	//
	PrtExitFunStart = 0,

	//
	// If ReturnTo points to this value then the call returns to the start of Action
	//
	PrtActionFunStart = 0
};

//
// To indicate whether to execute entry or exit function
//
enum _PRT_STATE_EXECFUN
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
};

/*********************************************************************************

Type Name : PRT_LASTOPERATION

Description :
Enum for last P operation performed inside entry/exit function
*********************************************************************************/
enum _PRT_LASTOPERATION
{
	PopStatement,
	RaiseStatement,
	CallStatement,
	OtherStatement
};



/*********************************************************************************

Type Name : PRT_TRANSHISTORY_STEP

Description :
Enum for specifying the type of step taken by the state-machine,
this is used for storing the transition history
*********************************************************************************/
typedef enum _PRT_TRACE_STEP
{
	//
	// Trace enqueue of an event
	//
	traceEnqueue,
	//
	// Trace dequeue of an event
	//
	traceDequeue,
	//
	// Trace State Change (entry into a new state)
	//
	traceStateChange,

	//
	// Trace creation of a new state-machine
	//
	traceCreateMachine,

	//
	// Trace raise of an event
	//
	traceRaiseEvent,

	//
	// Trace Pop from a state
	//
	tracePop,

	//
	// Trace Call Statement
	//
	traceCallStatement,

	//
	// Trace Call Edge
	//
	traceCallEdge,

	// 
	// Trace Unhandled Event causing Pop
	//
	traceUnhandledEvent,

	//
	// Trace actions 
	//
	traceActions,

	//
	// Trace Queue Resize
	//
	traceQueueResize,

	//
	// trace Exit Function
	//
	traceExit,
	//
	// trace deletion of a machine
	//
	traceDelete
};

/*********************************************************************************

Macros Constants

*********************************************************************************/
//
// Max call stack depth of each machine
//
#define PRT_MAX_CALL_DEPTH 16 

//
// Max Transition History Depth for each machine
//
#define PRT_MAX_HISTORY_DEPTH 64

//
// Max length of the event queue for each machine
//
#define PRT_QUEUE_LEN_DEFAULT 16



/*********************************************************************************

Modifiable Version of Packed Set

*********************************************************************************/

typedef ULONG32 *PPRT_EVENTDECL_INDEX_PACKEDTABLE;

/*********************************************************************************

Struct Declarations

*********************************************************************************/
//
// Trigger tuple <event, arg>
//
typedef struct _PRT_TRIGGER PRT_TRIGGER, *PPRT_TRIGGER;

//
// Structure for Statemachine Context
//
typedef struct _PRT_SMCONTEXT PRT_SMCONTEXT, *PPRT_SMCONTEXT;

//
// Event Buffer 
//
typedef struct _PRT_EVENTQUEUE PRT_EVENTQUEUE, *PPRT_EVENTQUEUE;

//
// Call Stack Element for each statemachine tuple <state, Event, Arg, ReturnTo>
//
typedef struct _PRT_STACKSTATE_INFO PRT_STACKSTATE_INFO, *PPRT_STACKSTATE_INFO;

//
// Call Stack for each state-machine
//
typedef struct _PRT_STATESTACK PRT_STATESTACK, *PPRT_STATESTACK;

//
// Transition Properties
//
typedef struct _PRT_TRANSHISTORY PRT_TRANSHISTORY;

/*********************************************************************************

Indexes and Counter

*********************************************************************************/
//
// Index into the Transition Table
//
typedef UCHAR PRT_TRANSHISTORY_INDEX;

//
//Machine Reference Count for maintainting references
//
typedef LONG32 PRT_MACHINEREFCOUNT;

/*********************************************************************************

Enum Declarations

*********************************************************************************/
//
//Reserved Events Enum
//
typedef enum _PRT_RESERVED_EVENT PRT_RESERVED_EVENT;

//
//Reserved States
//
typedef enum _PRT_RESERVED_STATE PRT_RESERVED_STATE;

//
//Reserved Machines
//
typedef enum _PRT_RESERVED_MACHINE PRT_RESERVED_MACHINE;

//
//Last Operation Performed in entry function
//
typedef enum _PRT_LASTOPERATION PRT_LASTOPERATION;

//
// Enum type to identify the type of Step taken by statemachine
//
typedef enum _PRT_TRANSHISTORY_STEP PRT_TRANSHISTORY_STEP;

//
// Enum type to identify the type of Step taken by statemachine
//
typedef enum _PRT_TRACE_STEP PRT_TRACE_STEP;

//
// Enum type to specify where the control should return after returning from a call statement
//
typedef enum _PRT_RETURNTO PRT_RETURNTO;

//
// Enum type to specify if RunMachine() should execute current state entry or exit function
//
typedef enum _PRT_STATE_EXECFUN PRT_STATE_EXECFUN;

/*********************************************************************************

Abstract Function types

*********************************************************************************/
//
//State Entry Functions
//Function type used by all state machines for entry functions                                                     
//
typedef VOID(PRT_ENTRYFUN)(__inout PPRT_SMCONTEXT Context);
typedef PRT_ENTRYFUN *PPRT_ENTRYFUN;

//
//Function type used by all state machines for constructors
//
typedef VOID(PRT_CONSTRUCTORFUN)(__in PVOID ConstructorParam, __inout PPRT_EXCONTEXT exContext);
typedef PRT_CONSTRUCTORFUN *PPRT_CONSTRUCTFUN;

//
//Function type used by all state machines for exit functions                                                     
//
typedef VOID(PRT_EXITFUN)(__inout PPRT_SMCONTEXT Context);
typedef PRT_EXITFUN *PPRT_EXITFUN;

//
//Function type used by all state machines for exit functions                                                     
//
typedef VOID(PRT_ACTIONFUN)(__inout PPRT_SMCONTEXT Context);
typedef PRT_ACTIONFUN *PPRT_ACTIONFUN;

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

struct _PRT_TRIGGER
{
	PPRT_VALUE Event;
	PPRT_VALUE Payload;
};


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
struct _PRT_EVENTQUEUE
{
	PPRT_TRIGGER Events;
	PRT_UINT16 Head;
	PRT_UINT16 Tail;
	PRT_UINT16 Size;
	PRT_BOOLEAN IsFull;
};

/*********************************************************************************

Type Name : PRT_TRANSHISTORY

Description :
Structure for Transition History Element for storing transition history of
a state machine,

Fields :

StateEntered --
New state entered by the state machine

OnStep --
Operation that caused the state to change.

OnEvent --
If the state change was caused by an event, OnEvent points to that event.

*********************************************************************************/

struct _PRT_TRANSHISTORY
{
	PRT_STATEDECL_INDEX StateEntered;
	PRT_TRANSHISTORY_STEP OnStep;
	PRT_EVENTDECL_INDEX OnEvent;
};

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

struct _PRT_STACKSTATE_INFO
{
	PRT_STATEDECL_INDEX StateIndex;
	PRT_TRIGGER Trigger;
	PRT_UINT16 ReturnTo;
	PRT_STATE_EXECFUN StateExecFun;
	PRT_EVENTDECL_INDEX_PACKEDTABLE InheritedDef;
	PRT_ACTIONDECL_INDEX_PACKEDTABLE InheritedAct;
};

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

struct _PRT_STATESTACK
{
	PRT_STACKSTATE_INFO StatesStack[PRT_MAX_CALL_DEPTH];
	PRT_UINT16 Length;
};

/*********************************************************************************

Type Name : PRT_SMCONTEXT

Description :
Structure for storing the state-machine context, it stores all the information
for a given state-machine

Fields :

TransitionHistory --
Stores the history of steps taken by the statemachine, size of the history
is fixed to PRT_MAX_HISTORY_DEPTH and stores only the latest PRT_MAX_HISTORY_DEPTH
steps taken by the state machine

TransHistoryIndex --
Points to the lastest Step performed by statemachine

StateMachineSignature --
Stores StateMachine_Signature if the state-machine is valid or exist, points
to other value if state-machine is invalid/deleted/freed

Driver --
Pointer to the Driver_Decl for this program

InstanceOf --
Index into the MachineDecl array, indicating the type of Current Machine.

Values --
Values of the Local Variables

Current --
Current state of the state machine

This --
State machine handle

Trigger --
Stores the Trigger value for the current state of the state-machine
Look at PRT_TRIGGER for more details

StateExecFun --
To indicate whether the PrtRunStateMachine() function should execute state entry or
exit function for the current state.

ReturnTo --
Acts as program counter when inside entry/exit function, used for going to next statement
after returning from a sub-statemachine call

IsRunning --
<TRUE> -- State machine is running.
<FALSE> -- State machine is blocked.

CallStack --
Call stack for the current state machine

EventQueue --
Event Buffer/Queue for the current state machine (FIFO Queue)

MaxLengthOfEventQueue --
Max. length of the circular event queue, by default is set to PRT_MAX_QUEUE_LEN_DEFAULT
can be overridden in Machine_Decl

Deferred --
Packed deffered set Inherited from the parent states

LastOperation --
Last operation performed in the entry/exit function

ExtContext --
Pointer to the external memory which can be access from foreign function

RefCount --
Reference count indicating number of existing reference on the state-machine
<0> - indicates machine can be freed
<1> - when the machine is created

StateMachineLock --
SPINLOCK for exclusive access to state-machine queue

PDeviceObj --
Pointer to device which created this state-machine

SmWorkItem --
Worker Item for executing state-machine at passive level.

*********************************************************************************/

//size of the history is fixed to PRT_MAX_HISTORY_DEPTH and stores
//	only the latest PRT_MAX_HISTORY_DEPTH steps taken by the state machine
struct _PRT_SMCONTEXT
{
	ULONG StateMachineSignature;
	PPRT_PROGRAMDECL Program;
	PRT_MACHINEDECL_INDEX InstanceOf;
	PRT_VARVALUE_TABLE Values;
	PRT_STATEDECL_INDEX CurrentState;
	PRT_MACHINE_HANDLE This;
	PRT_TRIGGER Trigger;


	PRT_UINT16 ReturnTo;
	PRT_STATE_EXECFUN StateExecFun;

	PRT_BOOLEAN IsRunning;
	PRT_STATESTACK CallStack;
	PRT_EVENTQUEUE EventQueue;
	PRT_UINT8 CurrentLengthOfEventQueue;
	PPRT_EVENTDECL_INDEX_PACKEDTABLE InheritedDeferred;
	PPRT_EVENTDECL_INDEX_PACKEDTABLE CurrentDeferred;

	PRT_ACTIONDECL_INDEX_PACKEDTABLE InheritedActions;
	PRT_ACTIONDECL_INDEX_PACKEDTABLE CurrentActions;

	PRT_LASTOPERATION LastOperation;

	PPRT_EXCONTEXT ExtContext;
	PRT_RECURSIVE_MUTEX StateMachineLock;

};

