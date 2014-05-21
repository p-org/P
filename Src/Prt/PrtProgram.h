/**
* \file PrtProgram.h
* \brief Defines the C representation for P programs.
*/
#ifndef PRTPROGRAM_H
#define PRTPROGRAM_H

#include "Config\PrtConfig.h"
#include "Values\PrtValues.h"

/** A PRT_MACHINE_FUN is either an entry fun, exit fun, or action that acts on machine instance. */
typedef void (*PRT_MACHINE_FUN)(struct PRT_MACHINE_INST *machine);

/** Represents an action installed on a state */
typedef struct PRT_ACTIONDECL
{
	PRT_UINT32      declIndex;         /**< The index of this decl in owner state                  */
	PRT_UINT32      ownerStateIndex;   /**< The index of owner state in owner machine              */
	PRT_UINT32      ownerMachIndex;    /**< The index of owner machine in program                  */
	PRT_STRING      name;              /**< The name of this action                                */
	PRT_UINT32      triggerEventIndex; /**< The index of the trigger event in program              */
	PRT_MACHINE_FUN actionFun;         /**< The function to execute when this action is triggered  */
} PRT_ACTIONDECL;

/** Represents a transition from owner state to dest state with trigger */
typedef struct PRT_TRANSDECL
{
	PRT_UINT32  declIndex;         /**< The index of this decl in owner state           */
	PRT_UINT32  ownerStateIndex;   /**< The index of owner state in owner machine       */
	PRT_UINT32  ownerMachIndex;    /**< The index of owner machine in program           */
	PRT_UINT32  triggerEventIndex; /**< The index of the trigger event in program       */
	PRT_UINT32  destStateIndex;    /**< The index of destination state in owner machine */
	PRT_BOOLEAN isPush;            /**< True if owner state is pushed onto state stack  */
} PRT_TRANSDECL;

/** Represents a state of a machine */
typedef struct PRT_STATEDECL
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
} PRT_STATEDECL;

/** Represents a state variable in a machine */
typedef struct PRT_VARDECL
{
	PRT_UINT32 declIndex;      /**< The index of variable in owner machine */
	PRT_UINT32 ownerMachIndex; /**< The index of owner machine in program  */
	PRT_STRING name;           /**< The name of this variable              */
	PRT_TYPE   type;           /**< The type of this variable              */
} PRT_VARDECL;

/** Represents a set of events */
typedef struct PRT_EVENTSETDECL
{
	PRT_UINT32 declIndex;      /**< The index of event set in owner machine */
	PRT_UINT32 ownerMachIndex; /**< The index of owner machine in program   */
	PRT_STRING name;           /**< The name of this event set              */
	PRT_UINT32 *packedEvents;  /**< The events packed into an array of ints */
} PRT_EVENTSETDECL;

/** Represents a P machine */
typedef struct PRT_MACHINEDECL
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
} PRT_MACHINEDECL;

/** Represents an event */
typedef struct PRT_EVENTDECL
{
	PRT_UINT32 declIndex;    /**< The index of event in program                                */
	PRT_STRING name;         /**< The name of this event                                       */
	PRT_INT32  maxInstances; /**< The max occurrences in any one queue, if non-negative        */
	PRT_TYPE   type;         /**< The type of the payload associated with this event (or NULL) */
} PRT_EVENTDECL;

/** Represents a P program */
typedef struct PRT_PROGRAM
{
	PRT_UINT32      nEvents;   /**< The number of events   */
	PRT_UINT32      nMachines; /**< The number of machines */
	PRT_EVENTDECL   *events;   /**< The array of events    */
	PRT_MACHINEDECL *machines; /**< The array of machines  */
} PRT_PROGRAM;

#endif