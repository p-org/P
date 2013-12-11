/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

    SmfPublicTypes.h

Abstract:
    This header file contains declarations of all the public types which can be 
	used inside driver code.

Environment:

    Kernel mode only.		

***********************************************************************************/

#pragma once
#include "SmfDepends.h"



/*********************************************************************************

		Structures / Unions and Tables Declaration

*********************************************************************************/

//
// State Machine Attributes used for initializing the Machine during creation
//
typedef struct _SMF_MACHINE_ATTRIBUTES SMF_MACHINE_ATTRIBUTES, *PSMF_MACHINE_ATTRIBUTES;

//
// Driver Decl which provides the template for a driver
//
typedef struct _SMF_DRIVERDECL SMF_DRIVERDECL, *PSMF_DRIVERDECL;

//
// State Machine variable declaration 
//
typedef struct _SMF_VARDECL SMF_VARDECL, * const SMF_VARDECL_TABLE;

//
// Event Set decl for the deferred/Ignored Events set
//
typedef struct _SMF_EVENTSETDECL SMF_EVENTSETDECL, * const SMF_EVENTSETDECL_TABLE;

//
// Event Decl (All events in the Program are of this type)
//
typedef struct _SMF_EVENTDECL SMF_EVENTDECL, * const SMF_EVENTDECL_TABLE;

//
// Machine Decl Template (Machine Types declared in the program)
//
typedef struct _SMF_MACHINEDECL SMF_MACHINEDECL, * const SMF_MACHINEDECL_TABLE;

//
// Type Decl Template (User defined types in the program (Tuples, Named Tuples, Sets, Dictionaries)
//
typedef struct _SMF_TYPEDECL SMF_TYPEDECL, * const SMF_TYPEDECL_TABLE;

//
// Type Decl Template (User defined types in the program (Tuples, Named Tuples, Sets, Dictionaries)
//
typedef struct _SMF_PACKED_VALUE SMF_PACKED_VALUE, *PSMF_PACKED_VALUE;

//
// State Decl 
//
typedef struct _SMF_STATEDECL SMF_STATEDECL, * SMF_STATEDECL_TABLE;

//
// Transition Table Decl
//
typedef struct _SMF_TRANSDECL SMF_TRANSDECL, *SMF_TRANSDECL_TABLE;

//
// Action Table Decl
//
typedef struct _SMF_ACTIONDECL SMF_ACTIONDECL, *SMF_ACTIONDECL_TABLE, *PSMF_ACTIONDECL;

//
// State Machine Variable Values
//
typedef ULONG_PTR SMF_VARVALUE, *SMF_VARVALUE_TABLE;

//
//For External Memory Context accessed by foreign function
//
typedef struct _SMF_EXCONTEXT SMF_EXCONTEXT, * PSMF_EXCONTEXT;


/*********************************************************************************

		Handles / Indices / Flags Declaration

*********************************************************************************/
//
// State Machine Handle (Machine ID)
//
typedef ULONG_PTR SMF_MACHINE_HANDLE, *PSMF_MACHINE_HANDLE;

//
// Local Variable Types 
//
typedef LONG32 SMF_VARTYPE, *PSMF_VARTYPE;

//
// Runtime Flags Associated with States and Events
//
typedef ULONG32 SMF_RUNTIMEFLAGS, *PSMF_RUNTIMEFLAGS;

//
// Machine Decl Index
//
typedef ULONG32 SMF_MACHINEDECL_INDEX;

//
// Type Decl Index
//
typedef ULONG32 SMF_TYPEDECL_INDEX;


//
// Event Decl Index
//
typedef ULONG32 SMF_EVENTSETDECL_INDEX;

//
// Event Decl Index
//
typedef ULONG32 SMF_EVENTDECL_INDEX, * SMF_EVENTDECL_INDEX_TABLE,  *SMF_EVENTDECL_INDEX_PACKEDTABLE;

//
// Bit Set
//
typedef ULONG32 const* const SMF_BIT_SET;


//
// Variable Decl Index
//
typedef ULONG32 SMF_VARDECL_INDEX;

//
// State Decl Index
//
typedef ULONG32 SMF_STATEDECL_INDEX;

//
// Transtition Decl Index
//
typedef ULONG32 SMF_TRANSDECL_INDEX, *SMF_TRANSDECL_INDEX_PACKEDTABLE;

//
// Action Decl Index
//
typedef ULONG32 SMF_ACTIONDECL_INDEX, *SMF_ACTIONDECL_INDEX_PACKEDTABLE;

/*********************************************************************************

		Function Pointer Types Declarations

*********************************************************************************/
// 
// Function Pointer to Entry/Exit/Constructor Function corresponding to each state
//
typedef VOID (*PSMF_OPAQUE_FUN)(PVOID);

typedef VOID (*PSMF_OPAQUE_CONST_FUN)(PVOID, PVOID);

typedef VOID (*PSMF_OPAQUE_BUILDDEF_FUN)(PVOID, PVOID);
typedef VOID (*PSMF_OPAQUE_CLONE_FUN)(PVOID, PVOID, PVOID);
typedef VOID (*PSMF_OPAQUE_DESTROY_FUN)(PVOID, PVOID);
typedef BOOLEAN (*PSMF_OPAQUE_EQUALS_FUN)(PVOID, PVOID, PVOID);
typedef ULONG (*PSMF_OPAQUE_HASHCODE_FUN)(PVOID, PVOID);

/*********************************************************************************

		Enum Types Declarations

*********************************************************************************/

//
// Exceptions Thrown by Runtime
//
typedef enum _SMF_EXCEPTIONS SMF_EXCEPTIONS;

//
// Built in Types for Local Variables
//
typedef enum _SMF_BUILTINTYPE SMF_BUILTINTYPE;

//
// Runtime Flags Corresponding to each state/event
//
typedef enum _SMF_RUNTIMEFLAG SMF_RUNTIMEFLAG;



/*********************************************************************************

Type Name : SMF_PACKED_VALUE

Description : 
	Structure for Representing Values Packed with a Type Tag

Fields :

Type --
	Type of the local variable

Value --
	The actual value stored inside

*********************************************************************************/

struct _SMF_PACKED_VALUE
{
	SMF_TYPEDECL_INDEX Type;
	ULONG_PTR Value;
};


/*********************************************************************************

Type Name : SMF_MACHINE_ATTRIBUTES

Description : 
	Structure to store state-machine attributes used for initializing the state-machine 

Fields :

Driver --
	Pointer to the driver decl for current program.

InstanceOf --
	Index in the MachineDecl Table, indicating the type of machine to be created.

InitValues -- 
	Values of Local variable of the Machine being created, used to initialize local
	variables before creating the state-machine

PDeviceObj --
	Pointer to Device Object for the current device.

*********************************************************************************/
struct _SMF_MACHINE_ATTRIBUTES
{
	PSMF_DRIVERDECL Driver;
	SMF_MACHINEDECL_INDEX InstanceOf;
	SMF_PACKED_VALUE Arg;
	PVOID ConstructorParam;
#ifdef KERNEL_MODE
	PDEVICE_OBJECT PDeviceObj;
#endif
};

/*********************************************************************************

Type Name : SMF_EXCONTEXT

Description : 
	Structure having pointer to the External Context (foreign memory Blob)

Fields :

freeThis -- 
	boolean value to indicate if the memory pointed to by PExMem should
	freed when the statemachine is deleted. 
	<TRUE> = Free memory pointed by pExMem
	<FALSE> = Memory is not freed by Runtime when state machine is deleted, developers
	responsibility to free this memory

	Note : State Machine nolonger points to PExMem once deleted

PExMem -- 
	Pointer to the external memory context corresponding to each statemachine, 
	passed as parameter to foreign functions 

*********************************************************************************/
struct _SMF_EXCONTEXT
{
	BOOLEAN FreeThis;
	PVOID PExMem;
	PVOID ConstructorParam;
};


/*********************************************************************************

Type Name : SMF_DRIVERDECL

Description : 
	Structure for storing information about the current driver program 

Fields :

NEvents --
	Number of Events in the Driver (Program)

Events -- 
	Table of all the events in Driver 

NMachines -- 
	Number of Machine Types in the Driver Program

Machines --
	Table containing declarations of all the Machine Types

*********************************************************************************/

struct _SMF_DRIVERDECL
{
	const ULONG32 NEvents;
	SMF_EVENTDECL_TABLE Events;
	const ULONG32 NMachines;
	SMF_MACHINEDECL_TABLE Machines;
	const ULONG32 NTypes;
	SMF_TYPEDECL_TABLE Types;

};

/*********************************************************************************

Type Name : SMF_EVENTDECL

Description : 
	Structure for storing information about Event  

Fields :

MyIndex --
	My index in the EventDecl Table in DriverDecl

Name --
	Name of the Event (String)

MaxInstances --
        Maximum number of instances of this event in a queue

Type --
	Type of the payload of the event

*********************************************************************************/

struct _SMF_EVENTDECL
{
	const SMF_EVENTDECL_INDEX MyIndex;
    const PCWSTR Name;
	const UINT16 MaxInstances;
	const SMF_TYPEDECL_INDEX Type;
};

/*********************************************************************************

Type Name : SMF_MACHINEDECL

Description : 
	Structure for storing information about Machine Type

Fields :

MyIndex --
	Index into the Machine Decl Table in ProtectedMachineDecl.h

Name --
	Name of the machine

NVars -- 
	Number of local variables

Vars --
	Values of the local variables

NStates --
	Number of states in the machine
States --
	Collection of StateDecl 

SizeOfEventQueue --
	maximum size of the event queue

NEventSets --
	number of event sets declared for the given machine

EventSets --
	Collection of event sets 

Initial --
	Initial state of the statemachine

constructorFun
	Pointer to the constructor function called on creation of machine of this type

*********************************************************************************/

struct _SMF_MACHINEDECL
{
	const SMF_MACHINEDECL_INDEX MyIndex;
	const PCWSTR Name;
	const ULONG32 NVars;
	SMF_VARDECL_TABLE Vars;
	const ULONG32 NStates;
	SMF_STATEDECL_TABLE States;
	UCHAR MaxSizeOfEventQueue;
	const ULONG32 NEventSets;
	SMF_EVENTSETDECL_TABLE EventSets;
	const SMF_STATEDECL_INDEX Initial;
	const PSMF_OPAQUE_CONST_FUN constructorFun;
};

/*********************************************************************************

Type Name : SMF_TYPEDECL

Description : 
	Structure for storing information about each user defined type. All of the described types
	are reference types, stored in the VARTABLE as type SmfRefType.

Fields :

Name --
	string representing the name of the datatype

Size --
	size in memory of a value of this type

Primitive --
	true iff the given type is a primitive (i.e. fits in ULONG_PTR, has no special clone/build default/destroy behavior.

PrimitiveDefault --
	for primitive types, store the default initialization value

SuperTypes --
	A Bit Set containing all the supertypes. Each Type is identified by its index in the Types enum

SubTypes --
	A Bit Set containing all the subtypes. Each Type is identified by its index in the Types enum

Clone --
	function pointer to a routine that performs a deep clone 
	of a value of memory from one location in memory, to another. Calle responsible for
	memory management

BuildDefault --
	function pointer to a routine that initializes the default value of the type, in the given memory.

Destroy --
	function pointer to a routine that performs cleanup for a value of this type. For example,
	if this were a sequence, the destroy routine would be responsible for freeing all elements
	allocated in the sequence


*********************************************************************************/

struct _SMF_TYPEDECL
{
    const PCWSTR Name;
	const SIZE_T Size;
	const BOOLEAN Primitive;
	ULONG_PTR PrimitiveDefault;
	SMF_BIT_SET SuperTypes;
	SMF_BIT_SET SubTypes;
	PSMF_OPAQUE_CLONE_FUN Clone;
	PSMF_OPAQUE_BUILDDEF_FUN BuildDefault;
	PSMF_OPAQUE_DESTROY_FUN Destroy;
    PSMF_OPAQUE_EQUALS_FUN Equals;
    PSMF_OPAQUE_HASHCODE_FUN HashCode;
};

/*********************************************************************************

Type Name : SMF_VARDECL

Description : 
	Structure for Representing local variables in a statemachine

Fields :

MyIndex --
	Index in to the Values Table in SMF_SMContext

MyMachine--
	Index into the MachineDecl table indicating the type of machine which contains 
	this variable

Name --
	Name of the local variable

Type --
	Type of the local variable

RefType --
	If the type is SmfRefType, then RefType is an index into the Types
	table of the driver, with the description of the type that this
	RefType points to.


*********************************************************************************/

struct _SMF_VARDECL
{
	const SMF_VARDECL_INDEX MyIndex;
	const SMF_MACHINEDECL_INDEX MyMachine;
	const PCWSTR Name;
	const SMF_TYPEDECL_INDEX Type;
};

/*********************************************************************************

Type Name : SMF_EVENTSETDECL

Description : 
	Structure for Representing Event set in a statemachine

Fields :

MyIndex --
	Index in to the Event Set Table in Machine_Decl

MyMachine--
	Index into the MachineDecl table indicating the type of machine which contains 
	this event set

Name --
	Name of the Event set

EventIndexPackedTable --
	Packed Version of EventSet in the form of bit-vector array, where each element is
	of type ULONG32

*********************************************************************************/

struct _SMF_EVENTSETDECL
{
	const SMF_VARDECL_INDEX MyIndex;
	const SMF_MACHINEDECL_INDEX MyMachine;

	const PCWSTR Name;	
	SMF_EVENTDECL_INDEX_PACKEDTABLE EventIndexPackedTable;
};

/*********************************************************************************

Type Name : SMF_TRANSDECL

Description : 
	Structure for declaring a transition

Fields :

MyIndex -- 
	Index in to the Transition Table in State_Decl.

MyState --
	Index into the State Table in Machine_Decl to point to the State which includes 
	this transition.

MyMachine --
	Index into the Machines Table in Driver Decl

EventIndex --
	Points to the event which caused transition
	
Destination --
	Target state for this transition

IsPush --
	Is it a push transition / call edge

*********************************************************************************/
struct _SMF_TRANSDECL
{
	const SMF_TRANSDECL_INDEX MyIndex;
	const SMF_STATEDECL_INDEX MyState;
	const SMF_MACHINEDECL_INDEX MyMachine;

	const SMF_EVENTDECL_INDEX EventIndex;
	const SMF_STATEDECL_INDEX Destination;
	const BOOLEAN IsPush;
};

/*********************************************************************************

Type Name : SMF_ACTIONDECL

Description : 
	Structure for declaring a transition

Fields :

MyIndex -- 
	Index in to the Transition Table in State_Decl.

MyState --
	Index into the State Table in Machine_Decl to point to the State which includes 
	this transition.

MyMachine --
	Index into the Machines Table in Driver Decl

EventIndex --
	Points to the event which caused transition
	
Destination --
	Target state for this transition

ActionFun --
	Function Pointer to the action function corresponding to event EventIndex

*********************************************************************************/
struct _SMF_ACTIONDECL
{
	const SMF_ACTIONDECL_INDEX MyIndex;
	const SMF_STATEDECL_INDEX MyState;
	const SMF_MACHINEDECL_INDEX MyMachine;

	const PCWSTR Name;
	const SMF_EVENTDECL_INDEX EventIndex;
	const PSMF_OPAQUE_FUN ActionFun;
	const BOOLEAN IsActionFunPassiveLevel;

};

/*********************************************************************************

Type Name : SMF_STATEDECL

Description : 
	Structure for declaring a 

Fields :

MyIndex --
	Index into the State Table in Machine_Decl

MyMachine --
	Index into the Machine Type Table in Driver_Decl

Name --
	Name of the State

Flags --
	State level run time flags e.g. Passive level execution

EntryFunc --
	Pointer to State Entry function 

ExitFunc --
	Pointer to state exit function

Defers --
	set of events deferred by the current state

NTransitions --
	Number of Transitions in the Transitions Table

Transitions --
	Transitions table for the current state

TransitionsPacked --
	Packed representation of Transitions set

NActions --
	Number of Actions in the Actions Table

Transitions --
	Actions table for the current state

ActionsPacked --
	Packed representation of Actions

HasDefaultTransition --
	<True> -- If current state has an out-going Default transition

*********************************************************************************/

struct _SMF_STATEDECL
{
	const SMF_STATEDECL_INDEX MyIndex;
	const SMF_MACHINEDECL_INDEX MyMachine;
	const PCWSTR Name;
	const SMF_RUNTIMEFLAGS Flags;

	const PSMF_OPAQUE_FUN EntryFunc;
	const PSMF_OPAQUE_FUN ExitFunc;

	const SMF_EVENTSETDECL_INDEX Defers;
	
	const UINT16 NTransitions;
	SMF_TRANSDECL_TABLE Transitions;
	const SMF_TRANSDECL_INDEX_PACKEDTABLE TransitionsPacked;
	
	const UINT16 NActions;
	SMF_ACTIONDECL_TABLE Actions;
	const SMF_ACTIONDECL_INDEX_PACKEDTABLE ActionsPacked;

	const BOOLEAN HasDefaultTransition;

};


/*********************************************************************************

		P Exceptions

*********************************************************************************/

enum _SMF_EXCEPTIONS
{
//
// Unhandled event exception
//
	UnhandledEvent,
//
// P runtime tried to delete a state-machine with non-empty event queue
//
	UnfinishedEvents,
//
// Tried to access an illegal statemachine, statemachine no-longer exists
//
	IllegalAccess,
//
// Tried to enqueue an event twice more than max instances
//
	MaxInstanceExceeded,
//
// Failed to allocate memory on creation of a state-machine
//
	FailedToAllocateMemory,

//
// Call Statement should not terminate with an unhandled event
//
	UnhandledEventInCallS,

//
// Max Queue size exceeded
//
	MaxQueueSizeExceeded
};

/*********************************************************************************

		Builtin Types

		Note: The only type the runtime needs to explicitly be aware of,
		is the Null type, and that is only so that the g_SmfNullPayload event
		can be declared statically here. For operations on all other types, the runtime
		uses the type to index into the Types array of the SMF_DRIVERDECL struct,
		to retrieve the neccessary function pointer/type metadata.

*********************************************************************************/

enum _SMF_BUILTINTYPES
{
//
// Null Type
//
	SmfNullType = 0,
};

//
// SmfNull is the representation for the null value in P, used both as a machine and an event.
//
#define SmfNull	0
