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
#include "Config\PrtConfig.h"
#include "Values\PrtDTTypes.h"
#include "Values\PrtDTValues.h"
#include "PrtSMTypeDefs.h"


/*********************************************************************************

Function Pointer Types Declarations

*********************************************************************************/
// 
// Function Pointer to Entry/Exit/Action Function corresponding to each state
//
typedef VOID(*PRT_MACHINE_FUN)(PVOID);


/*********************************************************************************

Enum Types Declarations

*********************************************************************************/

//
// Exceptions Thrown by Runtime
//
typedef enum _PRT_EXCEPTIONS PRT_EXCEPTIONS;

struct _PRT_EXCONTEXT
{
	PRT_BOOLEAN FreeThis;
	PVOID PExMem;
};

/** The kinds of program elements that can be annotated. */
typedef enum PRT_ANNOTATION_KIND
{
	PRT_ANNOT_PROGRAM = 0,  /**< A program-wide annotation, uniquely identified.                                                    */
	PRT_ANNOT_MACHINE = 1,  /**< A machine-wide annotation. Specific machine identified by a machine index.                         */
	PRT_ANNOT_EVENT = 2,  /**< An event annotation. Specific event identified by an event index.                                  */
	PRT_ANNOT_EVENTSET = 3,  /**< An event set annotation. Specific set identified by a machine and set index.                       */
	PRT_ANNOT_VAR = 4,  /**< An variable annotation. Specific variable identified by a machine and variable index.              */
	PRT_ANNOT_STATE = 5,  /**< A state annotation. Specific state identified by a machine and state index.                        */
	PRT_ANNOT_TRANS = 6,  /**< A transition annotation. Specific transition identified by a machine, state, and transition index. */
	PRT_ANNOT_ACTION = 7   /**< An action annotation. Specific action identified by a machine, state, and action index.            */
} PRT_ANNOTATION_KIND;

/** Represents an annotation of a program element */
typedef struct PRT_ANNOTATION
{
	PRT_ANNOTATION_KIND kind;       /**< The kind of element being annotated                   */
	PRT_UINT32          index1;     /**< The first index for identifying the element           */
	PRT_UINT32          index2;     /**< The second index for identifying the element          */
	PRT_UINT32          index3;     /**< The third index for identifying the element           */
	PRT_GUID            annotGuid;  /**< The a guid for describing the kind of annotation data */
	void                *annotData; /**< A pointer to opaque annotation data                   */
} PRT_ANNOTATION;

struct _PRT_PROGRAMDECL
{
	PRT_UINT32      nEvents;      /**< The number of events      */
	PRT_UINT32      nMachines;    /**< The number of machines    */
	PRT_UINT32      nAnnotations; /**< The number of annotations */
	PRT_EVENTDECL   *events;      /**< The array of events       */
	PRT_MACHINEDECL *machines;    /**< The array of machines     */
	PRT_ANNOTATION  *annotations; /**< The array of annotations  */

};

struct _PRT_EVENTDECL
{
	PRT_UINT32 declIndex;      /**< The index of event set in owner machine */
	PRT_UINT32 ownerMachIndex; /**< The index of owner machine in program   */
	PRT_STRING name;           /**< The name of this event set              */
	PRT_UINT32 eventMaxInstances; /**< The value of maximum instances of the event that can occur in the queue */
	PRT_TYPE   payloadType;	/** The type of the payload associated with this event */
	PRT_UINT32 *packedEvents;  /**< The events packed into an array of ints */
};

struct _PRT_MACHINEDECL
{
	PRT_UINT32       declIndex;         /**< The index of machine in program     */
	PRT_STRING       name;              /**< The name of this machine            */
	PRT_UINT32       nVars;             /**< The number of state variables       */
	PRT_UINT32       nStates;           /**< The number of states                */
	PRT_UINT32       nEventSets;        /**< The number of event sets            */
	PRT_INT32        maxQueueSize;      /**< The max queue size, if non-negative */
	PRT_UINT32       initStateIndex;    /**< The index of the initial state      */
	PRT_VARDECL      *vars;             /**< The array of variable declarations  */
	PRT_STATEDECL    *states;           /**< The array of state declarations     */
	PRT_EVENTSETDECL *eventSets;        /**< The array of event set declarations */
};


struct _PRT_VARDECL
{
	PRT_UINT32 declIndex;      /**< The index of variable in owner machine */
	PRT_UINT32 ownerMachIndex; /**< The index of owner machine in program  */
	PRT_STRING name;           /**< The name of this variable              */
	PRT_TYPE   type;           /**< The type of this variable              */
};


struct _PRT_EVENTSETDECL
{
	PRT_UINT32 declIndex;      /**< The index of event set in owner machine */
	PRT_UINT32 ownerMachIndex; /**< The index of owner machine in program   */
	PRT_STRING name;           /**< The name of this event set              */
	PRT_UINT32 *packedEvents;  /**< The events packed into an array of ints */
};

struct _PRT_TRANSDECL
{
	PRT_UINT32  declIndex;         /**< The index of this decl in owner state           */
	PRT_UINT32  ownerStateIndex;   /**< The index of owner state in owner machine       */
	PRT_UINT32  ownerMachIndex;    /**< The index of owner machine in program           */
	PRT_UINT32  triggerEventIndex; /**< The index of the trigger event in program       */
	PRT_UINT32  destStateIndex;    /**< The index of destination state in owner machine */
	PRT_BOOLEAN isPush;            /**< True if owner state is pushed onto state stack  */
};

struct _PRT_ACTIONDECL
{
	PRT_UINT32      declIndex;         /**< The index of this decl in owner state                  */
	PRT_UINT32      ownerStateIndex;   /**< The index of owner state in owner machine              */
	PRT_UINT32      ownerMachIndex;    /**< The index of owner machine in program                  */
	PRT_STRING      name;              /**< The name of this action                                */
	PRT_UINT32      triggerEventIndex; /**< The index of the trigger event in program              */
	PRT_MACHINE_FUN actionFun;         /**< The function to execute when this action is triggered  */

};

struct _PRT_STATEDECL
{
	PRT_UINT32  declIndex;       /**< The index of state in owner machine    */
	PRT_UINT32  ownerMachIndex;  /**< The index of owner machine in program  */
	PRT_STRING  name;            /**< The name of this state                 */
	PRT_UINT32  nTransitions;    /**< The number of transitions              */
	PRT_UINT32  nActions;        /**< The number of installed actions        */
	PRT_BOOLEAN hasDefaultTrans; /**< True of there is a default transition  */

	PRT_UINT32      defersSetIndex; /**< The index of the defers set in owner machine             */
	PRT_UINT32      transSetIndex;  /**< The index of the transition trigger set in owner machine */
	PRT_UINT32      actionSetIndex; /**< The index of the action trigger set in owner machine     */
	PRT_TRANSDECL   *transitions;   /**< The array of transitions                                 */
	PRT_ACTIONDECL  *actions;       /**< The array of installed actions                           */
	PRT_MACHINE_FUN entryFun;       /**< The entry function                                       */
	PRT_MACHINE_FUN exitFun;        /**< The exit function                                        */

};


/*********************************************************************************

P Exceptions

*********************************************************************************/

enum _PRT_EXCEPTIONS
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

