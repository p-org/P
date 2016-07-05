/**
* \file PrtProgram.h
* \brief Defines the representation of P programs in C.
* A P program will be compiled into a set of constant expressions
* using these data structures.
*/
#ifndef PRTPROGRAM_H
#define PRTPROGRAM_H

#ifdef __cplusplus
extern "C"{
#endif

#include "PrtValues.h"

/** These are reserved indices in the array of function decls.
*/
typedef enum PRT_SPECIAL_ACTIONS
{
	PRT_SPECIAL_ACTION_PUSH_OR_IGN = 0 /**< The index of push action. */
} PRT_SPECIAL_ACTION;

struct PRT_MACHINEINST; /* forward declaration */

/** A PRT_SM_FUN function is a pointer to a P function.
*   context is the current machine context.
*   Returns a non-null pointer if function has a return type. Otherwise returns C null value. Caller frees return.
*/
typedef PRT_VALUE *(PRT_CALL_CONV * PRT_SM_FUN)(_Inout_ struct PRT_MACHINEINST *context);

/** A PRT_SM_EXTCTOR function constructs the external blob attached to a machine.
*   context is the machine context to construct.
*   value is the value passed to the new M(...) operation. It will be the P null value if no value was passed.
*   Function frees value.
*/
typedef void(PRT_CALL_CONV * PRT_SM_EXTCTOR)(_Inout_ struct PRT_MACHINEINST * context, _Inout_ PRT_VALUE * value);

/** A PRT_SM_EXTDTOR function destructs the external blob attached to a machine.
*   context is the machine context to destruct.
*/
typedef void(PRT_CALL_CONV * PRT_SM_EXTDTOR)(_Inout_ struct PRT_MACHINEINST * context);

/** A PRT_SM_MODELSEND function sends an event to a model machine.
*  process is the calling process.
*  evnt is the id of the event being sent.
*  payload is the data being sent.
*  Function frees event.
*  Function frees payload iff doTransfer is true.
*/
typedef void(PRT_CALL_CONV * PRT_SM_MODELSEND)(
	_Inout_ struct PRT_MACHINEINST * sender,
	_Inout_ struct PRT_MACHINEINST * context,
	_Inout_ PRT_VALUE * evnt, 
	_Inout_ PRT_VALUE * payload,
	_In_    PRT_BOOLEAN doTransfer);

/** Represents a P event declaration */
typedef struct PRT_EVENTDECL
{
	PRT_UINT32 declIndex;         /**< The index of event in program                                           */
	PRT_STRING name;              /**< The name of this event set                                              */
	PRT_UINT32 eventMaxInstances; /**< The value of maximum instances of the event that can occur in the queue */
	PRT_TYPE   *type;	          /**< The type of the payload associated with this event                      */

	PRT_UINT32 nAnnotations;      /**< Number of annotations                                                   */
	void       **annotations;     /**< An array of annotations                                                 */
} PRT_EVENTDECL;

/** Represents a set of P events and the set packed into a bit vector */
typedef struct PRT_EVENTSETDECL
{
	PRT_UINT32 declIndex;      /**< The index of event set in the program  */
	PRT_UINT32 *packedEvents;  /**< The events packed into an array of ints */
} PRT_EVENTSETDECL;

/** Represents a P variable declaration */
typedef struct PRT_VARDECL
{
	PRT_UINT32 declIndex;      /**< The index of variable in owner machine */
	PRT_UINT32 ownerMachIndex; /**< The index of owner machine in program  */
	PRT_STRING name;           /**< The name of this variable              */
	PRT_TYPE   *type;          /**< The type of this variable              */

	PRT_UINT32 nAnnotations;   /**< Number of annotations                  */
	void       **annotations;  /**< An array of annotations                */
} PRT_VARDECL;

typedef struct PRT_CASEDECL
{
	PRT_UINT32 triggerEventIndex;
	PRT_UINT32 funIndex;
} PRT_CASEDECL;

typedef struct PRT_RECEIVEDECL
{
	PRT_UINT16 receiveIndex;
	PRT_UINT32 caseSetIndex;
	PRT_UINT32 nCases;
	PRT_CASEDECL *cases;
} PRT_RECEIVEDECL;

/** Represents a P function declaration */
typedef struct PRT_FUNDECL
{
	PRT_UINT32 declIndex;        /**< index of function in owner machine                                    */
	PRT_UINT32 ownerMachIndex;   /**< index of owner machine in program                                     */
	PRT_STRING name;             /**< name (NULL is anonymous)                                              */
	PRT_SM_FUN implementation;   /**< implementation                                                        */
	PRT_UINT32 maxNumLocals;     /**< number of local variables including nested scopes                     */
	PRT_UINT32 numEnvVars;       /**< number of local variables in enclosing scopes (0 for named functions) */
	PRT_TYPE *localsNmdTupType;  /**< type of local variables tuple (not including nested scopes)           */
	PRT_UINT32 nReceives;        /**< number of receive statements in body                                  */
	PRT_RECEIVEDECL *receives;   /**< array of receive decls in body                                        */

	PRT_UINT32 nAnnotations;     /**< number of annotations                                                 */
	void       **annotations;    /**< array of annotations                                                  */
} PRT_FUNDECL;

/** Represents a P transition declaration */
typedef struct PRT_TRANSDECL
{
	PRT_UINT32  declIndex;         /**< The index of this decl in owner state           */
	PRT_UINT32  ownerStateIndex;   /**< The index of owner state in owner machine       */
	PRT_UINT32  ownerMachIndex;    /**< The index of owner machine in program           */
	PRT_UINT32  triggerEventIndex; /**< The index of the trigger event in program       */
	PRT_UINT32  destStateIndex;    /**< The index of destination state in owner machine */
	PRT_UINT32  transFunIndex;     /**< The index of function to execute when this transition is triggered */

	PRT_UINT32  nAnnotations;      /**< Number of annotations                         */
	void        **annotations;     /**< An array of annotations                       */
} PRT_TRANSDECL;

/** Represents a P do declaration */
typedef struct PRT_DODECL
{
	PRT_UINT32      declIndex;         /**< The index of this decl in owner state                  */
	PRT_UINT32      ownerStateIndex;   /**< The index of owner state in owner machine              */
	PRT_UINT32      ownerMachIndex;    /**< The index of owner machine in program                  */
	PRT_UINT32      triggerEventIndex; /**< The index of the trigger event in program              */
	PRT_UINT32      doFunIndex;        /**< The index of function to execute when this do is triggered  */

	PRT_UINT32      nAnnotations;      /**< Number of annotations                         */
	void            **annotations;     /**< An array of annotations                       */
} PRT_DODECL;

/** Represents a P state declaration */
typedef struct PRT_STATEDECL
{
	PRT_UINT32  declIndex;       /**< The index of state in owner machine    */
	PRT_UINT32  ownerMachIndex;  /**< The index of owner machine in program  */
	PRT_STRING  name;            /**< The name of this state                 */
	PRT_UINT32  nTransitions;    /**< The number of transitions              */
	PRT_UINT32  nDos;            /**< The number of do handlers              */

	PRT_UINT32      defersSetIndex; /**< The index of the defers set in program             */
	PRT_UINT32      transSetIndex;  /**< The index of the transition trigger set in program */
	PRT_UINT32      doSetIndex;     /**< The index of the do set in program                 */
	PRT_TRANSDECL   *transitions;   /**< The array of transitions                           */
	PRT_DODECL      *dos;           /**< The array of installed actions                     */
	PRT_UINT32      entryFunIndex;  /**< The index of entry function in owner machine       */
	PRT_UINT32      exitFunIndex;   /**< The index of exit function in owner machine        */

	PRT_UINT32      nAnnotations;   /**< Number of annotations                              */
	void            **annotations;  /**< An array of annotations                            */
} PRT_STATEDECL;

/** Represents a P machine declaration */
typedef struct PRT_MACHINEDECL
{
	PRT_UINT32       declIndex;         /**< The index of machine in program     */
	PRT_STRING       name;              /**< The name of this machine            */
	PRT_UINT32       nVars;             /**< The number of state variables       */
	PRT_UINT32       nStates;           /**< The number of states                */
	PRT_UINT32       nFuns;             /**< The number of functions             */

	PRT_UINT32       maxQueueSize;      /**< The max queue size                  */
	PRT_UINT32       initStateIndex;    /**< The index of the initial state      */
	PRT_VARDECL      *vars;             /**< The array of variable declarations  */
	PRT_STATEDECL    *states;           /**< The array of state declarations     */
	PRT_FUNDECL      *funs;             /**< The array of fun declarations       */
	PRT_SM_EXTCTOR   extCtorFun;        /**< external blob constructor           */
	PRT_SM_EXTDTOR   extDtorFun;        /**< external blob destructor            */

	PRT_UINT32      nAnnotations;   /**< Number of annotations                              */
	void            **annotations;  /**< An array of annotations                            */
} PRT_MACHINEDECL;

/** Represents a P model machine declaration */
typedef struct PRT_MODELIMPLDECL
{
	PRT_UINT32       declIndex;     /**< The index of model implementation in program       */
	PRT_STRING       name;          /**< The name of this machine                           */

	PRT_SM_EXTCTOR      ctorFun;    /**< Function that creates instances of this machine    */
	PRT_SM_MODELSEND    sendFun;    /**< Function that sends to instances of this machine   */
	PRT_SM_EXTDTOR		dtorFun;    /**< Function that destroys instances of this machine   */

	PRT_UINT32      nAnnotations;   /**< Number of annotations                              */
	void            **annotations;  /**< An array of annotations                            */
} PRT_MODELIMPLDECL;

/** Represents a P program declaration */
typedef struct PRT_PROGRAMDECL
{
	PRT_UINT32      nEvents;        /**< The number of events      */
	PRT_UINT32      nEventSets;     /**< The number of event sets  */
	PRT_UINT32      nMachines;      /**< The number of machines    */
	PRT_UINT32      nModelImpls;    /**< The number of model implementations */
	PRT_UINT16      nForeignTypes;  /**< The number of foreign types */

	PRT_EVENTDECL       *events;          /**< The array of events                 */
	PRT_EVENTSETDECL    *eventSets;       /**< The array of event set declarations */
	PRT_MACHINEDECL     *machines;        /**< The array of machines               */
	PRT_MODELIMPLDECL   *modelImpls;      /**< The array of model implementations  */
	PRT_FOREIGNTYPEDECL *foreignTypes;    /**< The array of foreign types */

	PRT_UINT32      nAnnotations;   /**< Number of annotations               */
	void            **annotations;  /**< An array of annotations             */
} PRT_PROGRAMDECL;

#ifdef __cplusplus
}
#endif
#endif

