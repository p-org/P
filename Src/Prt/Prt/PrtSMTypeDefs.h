#include "PrtConfig.h"

/*********************************************************************************

Structures / Unions 

*********************************************************************************/

//
// Program Decl which provides the template for a driver
//
typedef struct _PRT_PROGRAMDECL PRT_PROGRAMDECL;

//
// State Machine variable declaration 
//
typedef struct _PRT_VARDECL PRT_VARDECL, *const PRT_VARDECL_TABLE;

//
// Event Set decl for the deferred/Ignored Events set
//
typedef struct _PRT_EVENTSETDECL PRT_EVENTSETDECL, *const PRT_EVENTSETDECL_TABLE;

//
// Event Decl (All events in the Program are of this type)
//
typedef struct _PRT_EVENTDECL PRT_EVENTDECL, *const PRT_EVENTDECL_TABLE;

//
// Machine Decl Template (Machine Types declared in the program)
//
typedef struct _PRT_MACHINEDECL PRT_MACHINEDECL, *const PRT_MACHINEDECL_TABLE;


//
// State Decl 
//
typedef struct _PRT_STATEDECL PRT_STATEDECL, *PRT_STATEDECL_TABLE;

//
// Transition Table Decl
//
typedef struct _PRT_TRANSDECL PRT_TRANSDECL, *PRT_TRANSDECL_TABLE;

//
// Action Table Decl
//
typedef struct _PRT_ACTIONDECL PRT_ACTIONDECL, *PRT_ACTIONDECL_TABLE;


//
//For External Memory Context accessed by foreign function
//
typedef struct _PRT_EXCONTEXT PRT_EXCONTEXT;


//
// State Machine Handle (Machine ID)
//
typedef ULONG_PTR PRT_MACHINE_HANDLE;

/*********************************************************************************

Struct Declarations

*********************************************************************************/
//
// Trigger tuple <event, arg>
//
typedef struct _PRT_TRIGGER PRT_TRIGGER;

//
// Structure for Statemachine Context
//
typedef struct _PRT_SMCONTEXT PRT_SMCONTEXT;

//
// Event Buffer 
//
typedef struct _PRT_EVENTQUEUE PRT_EVENTQUEUE;

//
// Call Stack Element for each statemachine tuple <state, Event, Arg, ReturnTo>
//
typedef struct _PRT_STACKSTATE_INFO PRT_STACKSTATE_INFO;

//
// Call Stack for each state-machine
//
typedef struct _PRT_STATESTACK PRT_STATESTACK;


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
// Enum type to specify where the control should return after returning from a call statement
//
typedef enum _PRT_RETURNTO PRT_RETURNTO;

//
// Enum type to specify if RunMachine() should execute current state entry or exit function
//
typedef enum _PRT_STATE_EXECFUN PRT_STATE_EXECFUN;

 