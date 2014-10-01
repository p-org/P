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
#include "PrtConfig.h"
#include "PrtProcess.h"

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
	PrtDefaultEvent = LONG_MAX - 1,
	//
	// Special delete event
	//
	PrtHaltEvent = 0
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

struct _PRT_TRIGGER
{
	PRT_VALUE *event;
	PRT_VALUE *payload;
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
	PRT_TRIGGER		*events;
	PRT_UINT16		 headIndex;
	PRT_UINT16		 tailIndex;
	PRT_UINT16		 size;
	PRT_BOOLEAN		isFull;
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
	PRT_UINT32			stateIndex;
	PRT_TRIGGER			trigger;
	PRT_UINT16			returnTo;
	PRT_STATE_EXECFUN	stateExecFun;
	PRT_UINT32*			inheritedDefSetCompact;
	PRT_UINT32*			inheritedActSetCompact;
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
	PRT_STACKSTATE_INFO statesStack[PRT_MAX_CALL_DEPTH];
	PRT_UINT16			length;
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
	PRT_PPROCESS		*parentProcess;
	PRT_PROGRAMDECL		*program;
	PRT_UINT32			instanceOf;
	PRT_VALUE			**values;
	PRT_UINT32			currentState;
	PRT_MACHINE_HANDLE	thisP;
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

	PRT_EXCONTEXT*		extContext;
	PRT_RECURSIVE_MUTEX stateMachineLock;

};

