/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

    SmfProtectedTypes.h

Abstract:
    This header file contains Type declarations of protected nature,
	these Types should be used only inside entry/exit functions of a state 
	and P runtime. And included inside external driver code

Environment:

    Kernel mode only.		

***********************************************************************************/

#pragma once
#include "SmfPublicTypes.h"

/*********************************************************************************

		Reserved Constants

*********************************************************************************/
//
//Reserved Events 
//
enum _SMF_RESERVED_EVENT
{
	SmfResEventStart = LONG_MAX,
//
// If Trigger points to this value then the transition taken was a Default transition
//
	SmfDefaultEvent = LONG_MAX,

};

//
//Reserved States
//
enum _SMF_RESERVED_STATE
{
    SmfResStateStart = LONG_MAX
};

//
// To specify where to return in entry or exit functions
//
enum _SMF_RETURNTO
{
//
// If ReturnTo points to this value then the call was a call edge/transition
// and control should return to dequeue
//
	SmfEntryFunEnd = UINT16_MAX,
//
// If ReturnTo points to this value then the call returns to the start of Entry Function
//
	SmfEntryFunStart = 0,
//
// If ReturnTo points to this value then the call returnds to the start of Exit function
//
	SmfExitFunStart = 0,

//
// If ReturnTo points to this value then the call returns to the start of Action
//
	SmfActionFunStart = 0
};

//
// To indicate whether to execute entry or exit function
//
enum _SMF_STATE_EXECFUN
{
//
// If StateExecFun points to StateEntry, then we should execute the entry function for this state
//
	SmfStateEntry,
//
// If StateExecFun points to StateExit, then we should execute the exit function for this state
//
	SmfStateExit,

//
// If StateExecFun points to StateAction, then we should execute the action corresponding to trigger event for the current state
//
	SmfStateAction
};

/*********************************************************************************

Type Name : SMF_LASTOPERATION

Description : 
	Enum for State Runtime Flags
*********************************************************************************/
enum _SMF_RUNTIMEFLAG
{
	SmfNoFlag = 0x00,
//
// Runtime state flag to indicate that the entry function of the state should 
// be executed at passive level
	SmfEntryFunPassiveLevel = 0x01,
//
// Runtime state flag to indicate that the exit function of the state should 
// be executed at passive level
	SmfExitFunPassiveLevel = 0x02
};

/*********************************************************************************

Type Name : SMF_LASTOPERATION

Description : 
	Enum for last P operation performed inside entry/exit function
*********************************************************************************/
enum _SMF_LASTOPERATION
{
	PopStatement,
	RaiseStatement,
	CallStatement,
	OtherStatement
};

/*********************************************************************************

Type Name : SMF_TRANSHISTORY_STEP

Description : 
	Enum for specifying the type of step taken by the state-machine, 
	this is used for storing the transition history
*********************************************************************************/
typedef enum _SMF_TRANSHISTORY_STEP
{
//
// Executed Transition on Event (not-call Transition) (raise or dequeue)
//
	onEvent,
//
// Executed Call Statement
//
	onCallS,
//
// Executed Pop Statement
//
	onPop,
// 
// Executed Transition on Event + Call Edge
//
	onCallE,
//
// Unhandled Event caused pop
//
	onUnhandledEvent
};


/*********************************************************************************

Type Name : SMF_TRANSHISTORY_STEP

Description : 
	Enum for specifying the type of step taken by the state-machine, 
	this is used for storing the transition history
*********************************************************************************/
typedef enum _SMF_TRACE_STEP
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
	traceExit
};

/*********************************************************************************

		Macros Constants

*********************************************************************************/
//
// Max call stack depth of each machine
//
#define SMF_MAX_CALL_DEPTH 16 

//
// Max Transition History Depth for each machine
//
#define SMF_MAX_HISTORY_DEPTH 64

//
// Max length of the event queue for each machine
//
#define SMF_QUEUE_LEN_DEFAULT 16



/*********************************************************************************

		Modifiable Version of Packed Set

*********************************************************************************/

typedef ULONG32 *PSMF_EVENTDECL_INDEX_PACKEDTABLE;

/*********************************************************************************

		Struct Declarations

*********************************************************************************/
//
// Trigger tuple <event, arg>
//
typedef struct _SMF_TRIGGER SMF_TRIGGER, *PSMF_TRIGGER;

//
// Structure for Statemachine Context
//
typedef struct _SMF_SMCONTEXT SMF_SMCONTEXT, *PSMF_SMCONTEXT;

//
// Event Buffer 
//
typedef struct _SMF_EVENTQUEUE SMF_EVENTQUEUE, *PSMF_EVENTQUEUE;

//
// Call Stack Element for each statemachine tuple <state, Event, Arg, ReturnTo>
//
typedef struct _SMF_STACKSTATE_INFO SMF_STACKSTATE_INFO, *PSMF_STACKSTATE_INFO;

//
// Call Stack for each state-machine
//
typedef struct _SMF_STATESTACK SMF_STATESTACK, *PSMF_STATESTACK;

//
// Transition Properties
//
typedef struct _SMF_TRANSHISTORY SMF_TRANSHISTORY;

/*********************************************************************************

		Indexes and Counter

*********************************************************************************/
//
// Index into the Transition Table
//
typedef UCHAR SMF_TRANSHISTORY_INDEX;

//
//Machine Reference Count for maintainting references
//
typedef LONG32 SMF_MACHINEREFCOUNT;

/*********************************************************************************

		Enum Declarations

*********************************************************************************/
//
//Reserved Events Enum
//
typedef enum _SMF_RESERVED_EVENT SMF_RESERVED_EVENT;
 
//
//Reserved States
//
typedef enum _SMF_RESERVED_STATE SMF_RESERVED_STATE;

//
//Reserved Machines
//
typedef enum _SMF_RESERVED_MACHINE SMF_RESERVED_MACHINE;

//
//Last Operation Performed in entry function
//
typedef enum _SMF_LASTOPERATION SMF_LASTOPERATION;

//
// Enum type to identify the type of Step taken by statemachine
//
typedef enum _SMF_TRANSHISTORY_STEP SMF_TRANSHISTORY_STEP;

//
// Enum type to identify the type of Step taken by statemachine
//
typedef enum _SMF_TRACE_STEP SMF_TRACE_STEP;

//
// Enum type to specify where the control should return after returning from a call statement
//
typedef enum _SMF_RETURNTO SMF_RETURNTO;

//
// Enum type to specify if RunMachine() should execute current state entry or exit function
//
typedef enum _SMF_STATE_EXECFUN SMF_STATE_EXECFUN;

/*********************************************************************************

		Abstract Function types 

*********************************************************************************/
//
//State Entry Functions
//Function type used by all state machines for entry functions                                                     
//
typedef VOID (SMF_ENTRYFUN)(__inout PSMF_SMCONTEXT Context);
typedef SMF_ENTRYFUN *PSMF_ENTRYFUN;

//
//Function type used by all state machines for constructors
//
typedef VOID (SMF_CONSTRUCTORFUN)(__in PVOID ConstructorParam, __inout PSMF_EXCONTEXT exContext);
typedef SMF_CONSTRUCTORFUN *PSMF_CONSTRUCTFUN;

//
//Function type used by all state machines for exit functions                                                     
//
typedef VOID (SMF_EXITFUN)(__inout PSMF_SMCONTEXT Context);
typedef SMF_EXITFUN *PSMF_EXITFUN;

//
//Function type used by all state machines for exit functions                                                     
//
typedef VOID (SMF_ACTIONFUN)(__inout PSMF_SMCONTEXT Context);
typedef SMF_ACTIONFUN *PSMF_ACTIONFUN;

/*********************************************************************************

Type Name : SMF_TRIGGER

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

struct _SMF_TRIGGER
{
	SMF_EVENTDECL_INDEX Event;
	SMF_PACKED_VALUE Arg;
};


/*********************************************************************************

Type Name : SMF_EVENTQUEUE 

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
struct _SMF_EVENTQUEUE 
{
	PSMF_TRIGGER Events;
    UINT16 Head;
    UINT16 Tail;
	UINT16 Size;
	BOOLEAN IsFull;
};

/*********************************************************************************

Type Name : SMF_TRANSHISTORY 

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

struct _SMF_TRANSHISTORY
{
	SMF_STATEDECL_INDEX StateEntered;
	SMF_TRANSHISTORY_STEP OnStep;
	SMF_EVENTDECL_INDEX OnEvent;
};

/*********************************************************************************

Type Name : SMF_STACKSTATE_INFO 

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

struct _SMF_STACKSTATE_INFO
{
	SMF_STATEDECL_INDEX StateIndex;
	SMF_TRIGGER Trigger;
	UINT16 ReturnTo;
	SMF_STATE_EXECFUN StateExecFun;
	SMF_EVENTDECL_INDEX_PACKEDTABLE InheritedDef;
	SMF_ACTIONDECL_INDEX_PACKEDTABLE InheritedAct;
};

/*********************************************************************************

Type Name : SMF_STATESTACK

Description : 
	Structure for Call Stack of a statemachine to push state-info on a call statement or
	call edge

Fields :

StatesStack -- 
	Array of State-Info for implementing call-stack, size of the call stack is fixed to
	SMF_MAX_CALL_DEPTH

Length -- 
	Length/depth of the call-stack
*********************************************************************************/

struct _SMF_STATESTACK 
{
	SMF_STACKSTATE_INFO StatesStack[SMF_MAX_CALL_DEPTH];
	UINT16 Length;
};


/*********************************************************************************

Type Name : SMF_SMCONTEXT 

Description : 
	Structure for storing the state-machine context, it stores all the information 
	for a given state-machine

Fields :

TransitionHistory -- 
	Stores the history of steps taken by the statemachine, size of the history 
	is fixed to SMF_MAX_HISTORY_DEPTH and stores only the latest SMF_MAX_HISTORY_DEPTH 
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
	Look at SMF_TRIGGER for more details

StateExecFun --
	To indicate whether the SmfRunStateMachine() function should execute state entry or
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
	Max. length of the circular event queue, by default is set to SMF_MAX_QUEUE_LEN_DEFAULT 
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

//size of the history is fixed to SMF_MAX_HISTORY_DEPTH and stores
//	only the latest SMF_MAX_HISTORY_DEPTH steps taken by the state machine
struct _SMF_SMCONTEXT
{
	
	SMF_TRANSHISTORY TransitionHistory[SMF_MAX_HISTORY_DEPTH];
	SMF_TRANSHISTORY_INDEX TransHistoryIndex;
	
	ULONG StateMachineSignature;
	PSMF_DRIVERDECL Driver;
	SMF_MACHINEDECL_INDEX InstanceOf;
	SMF_VARVALUE_TABLE Values;
	SMF_STATEDECL_INDEX CurrentState;
	SMF_MACHINE_HANDLE This;
	SMF_TRIGGER Trigger;


	UINT16 ReturnTo;
	SMF_STATE_EXECFUN StateExecFun;

	BOOLEAN IsRunning;
	SMF_STATESTACK CallStack;
	SMF_EVENTQUEUE EventQueue;
	UCHAR CurrentLengthOfEventQueue;
	PSMF_EVENTDECL_INDEX_PACKEDTABLE InheritedDeferred;
	PSMF_EVENTDECL_INDEX_PACKEDTABLE CurrentDeferred;

	SMF_ACTIONDECL_INDEX_PACKEDTABLE InheritedActions;
	SMF_ACTIONDECL_INDEX_PACKEDTABLE CurrentActions;

	SMF_LASTOPERATION LastOperation;

	PSMF_EXCONTEXT ExtContext;

	SMF_MACHINEREFCOUNT RefCount;

#ifdef KERNEL_MODE
	KSPIN_LOCK StateMachineLock;
	KIRQL Irql;
	PDEVICE_OBJECT PDeviceObj;
	
	PIO_WORKITEM SmWorkItem;
#else
	SRWLOCK StateMachineLock;
#endif
};

//
//Function type used by runtime to clone a complex object
//
typedef VOID (SMF_CLONEFUN)(__in PSMF_DRIVERDECL Driver, __in PVOID src, __in PVOID dst);
typedef SMF_CLONEFUN *PSMF_CLONEFUN;

//
//Function type used to build a default instance of a complex object
//
typedef VOID (SMF_BUILDDEFFUN)(__in PSMF_DRIVERDECL Driver, __in PVOID dst);
typedef SMF_BUILDDEFFUN *PSMF_BUILDDEFFUN;

//
//Function type used to destruct an instance of a complex object. (note that it doesn't free
//the memory of the object itself, if allocated on the heap. It will free internal pointers
//stored in that memory. (e.g. ellements allocated in a list)
//
typedef VOID (SMF_DESTROYFUN)(__in PSMF_DRIVERDECL Driver, __in PVOID obj);
typedef SMF_DESTROYFUN *PSMF_DESTROYFUN;

typedef BOOLEAN (SMF_EQUALSFUN)(__in PSMF_DRIVERDECL Driver, __in PVOID a, __in PVOID b);
typedef SMF_EQUALSFUN *PSMF_EQUALSFUN;

typedef ULONG (SMF_HASHCODEFUN)(__in PSMF_DRIVERDECL Driver, __in PVOID obj);
typedef SMF_HASHCODEFUN *PSMF_HASHCODEFUN;
