#ifndef PRT_EXECUTION_H
#define PRT_EXECUTION_H

#include "PrtProgram.h"

#ifdef __cplusplus
extern "C"{
#endif

	/** The kinds of steps a state machine can perform. Used for logging.
	*   @see PrtMkMachine
	*/
	typedef enum PRT_STEP
	{
		PRT_STEP_CREATE = 0,      /**< Occurs when a machine is created.                            */
		PRT_STEP_DEQUEUE = 1,     /**< Occurs when an event is dequeued.                            */
		PRT_STEP_DO = 2,          /**< Occurs when an do handler is executed.                       */
		PRT_STEP_ENQUEUE = 3,     /**< Occurs when an event is enqueued.                            */
		PRT_STEP_ENTRY = 4,       /**< Occurs when an entry function is invoked.                    */
		PRT_STEP_EXIT = 5,        /**< Occurs when an exit function is invoked.                     */
		PRT_STEP_GOTO = 6,        /**< Occurs when a goto statement happens.                        */
		PRT_STEP_HALT = 7,        /**< Occurs when a machine halts.                                 */
		PRT_STEP_POP = 8,         /**< Occurs when a state is popped.                               */
		PRT_STEP_PUSH = 9,        /**< Occurs when a state is pushed.                               */
		PRT_STEP_RAISE = 10,      /**< Occurs when an event is raised.                              */
		PRT_STEP_IGNORE = 11,	  /**< Occurs when an event is ignored                              */
		PRT_STEP_UNHANDLED = 12,  /**< Occurs when an event is unhandled.                           */
		PRT_STEP_COUNT = 13,      /**< The number of valid step members.                            */
	} PRT_STEP;

	/** Status codes for normal and error conditions. Used for error reporting and indicating success/failure of API operations.
	*   An error status is in the exclusive range (PRT_STATUS_SUCCESS, PRT_STATUS_COUNT).
	*   @see PrtMkMachine
	*/
	typedef enum PRT_STATUS
	{
		PRT_STATUS_SUCCESS = 0,        /**< Indicates normal completion of operation. Any status greater than SUCCESS is an error. */
		PRT_STATUS_ASSERT = 1,        /**< Indicates an assertion failure.                               */
		PRT_STATUS_EVENT_OVERFLOW = 2,  /**< Indicates too many occurrences of the same event in a queue.  */
		PRT_STATUS_EVENT_UNHANDLED = 3,  /**< Indicates failure of a machine to handle an event.            */
		PRT_STATUS_QUEUE_OVERFLOW = 4,   /**< Indicates that a queue has grown too large.                   */
		PRT_STATUS_ILLEGAL_SEND = 5,	 /**< Indicates illegal use of send primitive for sending message across process */
		PRT_STATUS_COUNT = 6,            /**< The valid number of status codes.                             */
	} PRT_STATUS;

	/** Represents a running P program. Every process has a GUID and client is responsible
	*   for ensuring that any communicating set of processes have unique GUIDs. Processes
	*   that never communicate may have the same GUIDs.
	*   @see PrtStartProcess
	*   @see PrtStopProcess
	*/
	typedef struct PRT_PROCESS {
		PRT_GUID         guid;     /**< The unique ID for this process. Cannot be 0-0-0-0. */
	} PRT_PROCESS;

	/** The state of running machine in a process.
	*   @see PrtMkMachine
	*/
	typedef struct PRT_MACHINEINST
	{
		PRT_PROCESS		    *process;     /**< The process that owns this machine.             */
		PRT_UINT32			instanceOf;   /**< Index of machine type in PRT_PROGRAMDECL.       */
		PRT_VALUE			*id;          /**< The id of this machine.                         */
	} PRT_MACHINEINST;

	/** The scheduling policy determines how the state machine is executed.
	* On the caller's thread or on a separate thread.
	*   @see PrtSetSchedulingPolicy
	*/
	typedef enum PRT_SCHEDULINGPOLICY
	{
		PRT_SCHEDULINGPOLICY_TASKNEUTRAL,   /**< The default policy is task neutral, meaning the caller's thread is used to run the state machine */
		PRT_SCHEDULINGPOLICY_COOPERATIVE    /**< This policy means the caller plans to advance the state machine from a separate thread using PrtRunProcess */
	} PRT_SCHEDULINGPOLICY;

	/** Represents a snapshot of the state of a machine at a given point in time.  This is useful for logging.
	*/
	typedef struct PRT_MACHINESTATE
	{
		int			machineId;		/**< the machine instance id (you can use this in PrtGetMachine) */
		PRT_STRING  machineName;	/**< the name of the machine type */
		int			stateId;		/**< the state the machine was in at the time this snapshot was taken */
		PRT_STRING  stateName;		/**< the name of the machine type */
	} PRT_MACHINESTATE;

	/** An error function that will be called whenever an error arises. */
	typedef void(PRT_CALL_CONV * PRT_ERROR_FUN)(PRT_STATUS, PRT_MACHINEINST *);

	/** A log function that will be called whenever a step occurs. If an event is the reason, then sender, eventId and payload are also provided.
	* the caller retains ownership of all these pointers.
	*/
	typedef void(PRT_CALL_CONV * PRT_LOG_FUN)(PRT_STEP step, PRT_MACHINESTATE* senderState, PRT_MACHINEINST *receiver, PRT_VALUE *eventid, PRT_VALUE *payload);

	
	extern PRT_EVENTDECL _P_EVENT_NULL_STRUCT;
	extern PRT_EVENTDECL _P_EVENT_HALT_STRUCT;

	//
	// Max call stack size of each machine
	//
#define PRT_MAX_STATESTACK_DEPTH 16

#define PRT_MAX_FUNSTACK_DEPTH 16

#define PRT_MAX_EVENTSTACK_DEPTH 10

	//
	// Initial length of the event queue for each machine
	//
#define PRT_QUEUE_LEN_DEFAULT 64

    typedef struct PRT_COOPERATIVE_SCHEDULER
    {
        PRT_SEMAPHORE           workAvailable;      /* semaphore to signal blocked PrtRunProcess threads */
        PRT_UINT32              threadsWaiting;     /* number of PrtRunProcess threads waiting for work */
        PRT_SEMAPHORE           allThreadsStopped;  /* all PrtRunProcess threads have terminated */
    } PRT_COOPERATIVE_SCHEDULER;

	typedef struct PRT_PROCESS_PRIV {
		PRT_GUID				guid;
		PRT_ERROR_FUN	        errorHandler;
		PRT_LOG_FUN				logHandler;
		PRT_RECURSIVE_MUTEX		processLock;
		PRT_UINT32				numMachines;
		PRT_UINT32				machineCount;
		PRT_MACHINEINST			**machines;
        PRT_BOOLEAN             terminating;        /* PrtStopProcess has been called */
        PRT_SCHEDULINGPOLICY    schedulingPolicy;
        void*                   schedulerInfo;      /* for example, this could be PRT_COOPERATIVE_SCHEDULER */

	} PRT_PROCESS_PRIV;

	typedef enum PRT_LASTOPERATION
	{
		ReturnStatement,
		PopStatement,
		RaiseStatement,
		GotoStatement
	} PRT_LASTOPERATION;

    typedef enum PRT_NEXTOPERATION
    {
        EntryOperation,
        DequeueOperation,
        HandleEventOperation,
        ReceiveOperation
    } PRT_NEXTOPERATION;

	typedef enum PRT_EXITREASON
	{
		NotExit,
		OnTransition,
		OnTransitionAfterExit,
		OnPopStatement,
		OnGotoStatement,
		OnUnhandledEvent
	} PRT_EXITREASON;

	typedef struct PRT_EVENT
	{
		PRT_VALUE *trigger;
		PRT_VALUE *payload;
		PRT_MACHINESTATE state;
	} PRT_EVENT;

	typedef struct PRT_EVENTQUEUE
	{
		PRT_UINT32		 eventsSize;
		PRT_EVENT		*events;
		PRT_UINT32		 headIndex;
		PRT_UINT32		 tailIndex;
		PRT_UINT32		 size;
	} PRT_EVENTQUEUE;

	typedef struct PRT_STATESTACK_INFO
	{
		PRT_UINT32			stateIndex;
		PRT_UINT32*			inheritedDeferredSetCompact;
		PRT_UINT32*			inheritedActionSetCompact;
	} PRT_STATESTACK_INFO;

	typedef struct PRT_STATESTACK
	{
		PRT_STATESTACK_INFO stateStack[PRT_MAX_STATESTACK_DEPTH];
		PRT_UINT16			length;
	} PRT_STATESTACK;

	typedef struct PRT_EVENTSTACK
	{
		PRT_EVENT			events[PRT_MAX_EVENTSTACK_DEPTH];
		PRT_UINT16			length;
	} PRT_EVENTSTACK;

	typedef struct PRT_MACHINEINST_PRIV {
		PRT_PROCESS		    *process;     
		PRT_UINT32			instanceOf;   
		PRT_VALUE			*id;          
		PRT_VALUE           *recvMap;
		PRT_VALUE			**varValues;
		PRT_RECURSIVE_MUTEX stateMachineLock;
		PRT_BOOLEAN			isRunning;
        PRT_NEXTOPERATION   nextOperation;
		PRT_EXITREASON		exitReason;
		PRT_UINT32			eventValue;
		PRT_BOOLEAN			isHalted;
		PRT_UINT32			currentState;
		PRT_STATESTACK		callStack;
		PRT_UINT32			*packedReceiveCases;
		PRT_UINT32			destStateIndex;
		PRT_VALUE			*currentTrigger;
		PRT_VALUE			*currentPayload;
		PRT_EVENTQUEUE		eventQueue;
		PRT_LASTOPERATION	lastOperation;
		PRT_UINT32          *inheritedDeferredSetCompact;
		PRT_UINT32          *currentDeferredSetCompact;
		PRT_UINT32          *inheritedActionSetCompact;
		PRT_UINT32          *currentActionSetCompact;
		PRT_UINT32			interfaceBound;
	} PRT_MACHINEINST_PRIV;

	/** Starts a new Process running program.
	*   @param[in] guid Id for process; client must guarantee uniqueness for processes that may communicate. Cannot be 0-0-0-0.
	*   @param[in] program Program to run (not cloned). Client must free. Client cannot free or modify before calling PrtStopProcess.
	*   @param[in] errorFun  The error function to call if an error status occurrs. Must be thread-safe. If NULL, then no error reporting.
	*   @param[in] loggerFun The logging function to call when a machine step occurrs. Must be thread-safe. If NULL, then no logging.
	*   @returns A pointer to a new process. Client must free with PrtStopProcess
	*   @see PRT_PROCESS
	*   @see PrtStopProcess
	*/
	PRT_API PRT_PROCESS * PRT_CALL_CONV PrtStartProcess(
		_In_ PRT_GUID guid,
		_In_ PRT_PROGRAMDECL *program,
		_In_ PRT_ERROR_FUN errorFun,
		_In_ PRT_LOG_FUN loggerFun
	);

	/** Set the scheduling policy for this process.  The default policy is TaskNeutral
	*   @param[in] policy The new policy.
	*   @see PRT_PROCESS
	*   @see PRT_SCHEDULINGPOLICY
	*   @see PrtStartProcess
	*/
	PRT_API void PRT_CALL_CONV PrtSetSchedulingPolicy(_In_ PRT_PROCESS *process, _In_ PRT_SCHEDULINGPOLICY policy);

	/** Call this method if you set PRT_SCHEDULINGPOLICY to Cooperative.  This means the caller wants to control which thread
	*   runs the state machine, where this thread will block when there is no work to do, and it will automatically wake up
	*   via a semaphore when there is work to do.  It will terminate when you call PrtStopProcess.  You must then ensure you
	*   do not call PrtRunProcess after PrtStopProcess because the process will be deleted at that point.
	*   @param[in] process The process defines which state machines this method will run.
	*   @see PRT_SCHEDULINGPOLICY
	*   @see PrtSetSchedulingPolicy
	*/
	PRT_API void PRT_CALL_CONV PrtRunProcess(PRT_PROCESS *process);


	typedef enum PRT_STEP_RESULT
	{
		PRT_STEP_IDLE = 0,         /**< No more work */
		PRT_STEP_MORE = 1,         /**< More work is available */
		PRT_STEP_TERMINATING = 2,  /**< We are terminating the process  */
	} PRT_STEP_RESULT;


	/** Call this method if you set PRT_SCHEDULINGPOLICY to Cooperative.  This means the caller wants to control which thread
	*   runs the state machine. PrtStepProcess does one step and returns so the caller can also yield
	*   the thread, this is how this method is different from PrtRunProcess.   It returns PRT_FALSE if there is no work to do,
	*   at which time you should call PrtWaitForWork.  It will terminate if you call PrtStopProcess.
	*   @param[in] process The process defines which state machines this method will run.
	*   @see PRT_SCHEDULINGPOLICY
	*   @see PrtSetSchedulingPolicy
	*/
	PRT_API PRT_STEP_RESULT PRT_CALL_CONV PrtStepProcess(PRT_PROCESS *process);


	/** Call this method when PrtStepProcess returns PRT_STEP_IDLE.  This means PrtStepProcess has found that all machines
	* are waiting for work.  This method will block on a semaphore until more work becomes available.  It will also return
	* if you call PrtStopProcess.
	*   @param[in] process The process defines which state machines this method will run.
	*   @returns   PRT_TRUE if we are terminating (PrtStopProcess has been called).
	*   @see PrtStepProcess
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtWaitForWork(PRT_PROCESS *process);

	/** Stops a started process. Reclaims all resources allocated to the process.
	*   Client must call exactly once for each started process. Once called,
	*   no other API function affecting this process can occur from any thread.
	*   Once called, no interaction with data owned by this process should occur from any thread.
	*  This method also causes PrtRunProcess and PrtWaitForWork to terminate.
	*   @param[in,out] process The process to stop.
	*   @see PRT_PROCESS
	*   @see PrtStartProcess
	*/
	PRT_API void PRT_CALL_CONV PrtStopProcess(_Inout_ PRT_PROCESS* process);

	/** Creates a new machine instance in remote container process. Will be freed when container process is stopped.
	* @param[in,out] process    The process that will own this machine.
	* @param[in]     instanceOf An index of a machine type in process' program.
	* @param[in]     payload The payload to pass to the start state of machine instance (cloned, user frees).
	* @param[in]	 pointer to the container machine where the state-machine should be created
	* @returns       A pointer to a PRT_MACHINEINST.
	* @see PrtSend
	* @see PRT_MACHINEINST
	*/
	PRT_MACHINEINST * PRT_CALL_CONV PrtMkMachineRemote(
		_Inout_ PRT_PROCESS *process,
		_In_ PRT_UINT32 instanceOf,
		_In_ PRT_VALUE *payload,
		_In_ PRT_VALUE* container);

	/** Creates a new machine instance in process. Will be freed when process is stopped.
	* @param[in,out] process    The process that will own this machine.
	* @param[in]     instanceOf interface (machine).
	* @param[in]     payload The payload to pass to the start state of machine instance (cloned, user frees).
	* @returns       A pointer to a PRT_MACHINEINST.
	* @see PrtSend
	* @see PRT_MACHINEINST
	*/
	PRT_API PRT_MACHINEINST * PRT_CALL_CONV PrtMkMachine(
		_Inout_ PRT_PROCESS *process,
		_In_ PRT_UINT32 interfaceName,
		_In_ PRT_UINT32	numArgs,
		...);

	/** Creates a new machine instance in process. Will be freed when process is stopped.
	* @param[in,out] process    The process that will own this machine.
	* @param[in]     context	context of the creator machine.
	* @param[in]     interfaceName	interface machine to be created.
	* @returns       A pointer to a PRT_MACHINEINST.
	* @see PrtSend
	* @see PRT_MACHINEINST
	*/
	PRT_API PRT_MACHINEINST * PRT_CALL_CONV PrtMkInterface(
		_In_ PRT_MACHINEINST*		context,
		_In_ PRT_UINT32				interfaceName,
		_In_ PRT_UINT32				numArgs,
		...
	);

	/** Gets machine instance corresponding to id in process.
	* @param[in] process    The process containing the machine id.
	* @param[in] id  The id of the machine.
	* @returns       A pointer to a PRT_MACHINEINST or NULL if id is not valid for process.
	* @see PrtMkMachine
	* @see PRT_MACHINEINST
	*/
	PRT_API PRT_MACHINEINST * PRT_CALL_CONV PrtGetMachine(
		_In_ PRT_PROCESS *process,
		_In_ PRT_VALUE *id);

	/** Gets the current state of this machine .
	* @param[in] context    The machine that we want to get the state of.
	* @param[inout] state   The state is writtent to the fields of this structure.
	* @see PrtMkMachine
	* @see PRT_MACHINEINST
	*/
	PRT_API void PRT_CALL_CONV PrtGetMachineState(
		_In_ PRT_MACHINEINST *context,
		_Inout_ PRT_MACHINESTATE *state);


	/** Sends message to P state machine.
	* @param[in] senderState The current state of the sender machine (this state will be passed through to the PRT_STEP_DEQUEUE in logging
	* so you can figure out at that time where the event came from).
	* @param[in,out] receiver The machine that will receive this message.
	* @param[in] event The event to send with this message (cloned, user frees).
	* @param[in] numArgs The number of arguments in the payload.
	*/
	PRT_API void PRT_CALL_CONV PrtSend(
		_In_ PRT_MACHINESTATE *senderState,
		_Inout_ PRT_MACHINEINST *receiver,
		_In_ PRT_VALUE *evt,
		_In_ PRT_UINT32	numArgs,
		...
	);

	/** Sends message to P state machine.  This is for internal use only.
	* @param[in] sender The sender machine (from which we compute the PRT_MACHINESTATE) for PrtSend.
	* @param[in,out] receiver The machine that will receive this message.
	* @param[in] event The event to send with this message (cloned, user frees).
	* @param[in] numArgs The number of arguments in the payload.
	*/
	PRT_API void PRT_CALL_CONV PrtSendInternal(
		_Inout_ PRT_MACHINEINST *sender,
		_Inout_ PRT_MACHINEINST *receiver,
		_In_ PRT_VALUE *evt,
		_In_ PRT_UINT32	numArgs,
		...
	);

	/** Sets a global variable to variable
	* @param[in,out] context The context to modify.
	* @param[in] varIndex The index of the variable to modify.
	* @param[in] value The value to set. (Will be cloned)
	*/
	PRT_API void PRT_CALL_CONV PrtSetGlobalVar(_Inout_ PRT_MACHINEINST_PRIV * context, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE * value);

	/** Sets a global variable to variable
	* @param[in,out] context The context to modify.
	* @param[in] varIndex The index of the variable to modify.
	* @param[in] status Indicates whether this operation is move or swap
	* @param[in,out] value The pointer to the value to move or swap
	* @param[in]     type The type of data pointed to by value
	*/
	PRT_API void PRT_CALL_CONV PrtSetGlobalVarLinear(_Inout_ PRT_MACHINEINST_PRIV * context, _In_ PRT_UINT32 varIndex, _In_ PRT_FUN_PARAM_STATUS status, _Inout_ PRT_VALUE ** value, _In_ PRT_TYPE *type);

	/** Sets a global variable to variable
	* @param[in,out] context The context to modify.
	* @param[in] varIndex The index of the variable to modify.
	* @param[in] value The value to set. (Will be cloned if cloneValue is PRT_TRUE)
	* @param[in] cloneValue Only set to PRT_FALSE if value will be forever owned by this machine.
	*/
	PRT_API void PRT_CALL_CONV PrtSetGlobalVarEx(_Inout_ PRT_MACHINEINST_PRIV * context, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE * value, _In_ PRT_BOOLEAN cloneValue);

	PRT_MACHINEINST_PRIV *
		PrtMkMachinePrivate(
		_Inout_  PRT_PROCESS_PRIV		*process,
		_In_  PRT_UINT32				interfaceName,
		_In_  PRT_UINT32				instanceOf,
		_In_  PRT_VALUE					*payload
		);

	PRT_API void PRT_CALL_CONV PrtSetLocalVarLinear(
		_Inout_ PRT_VALUE **locals,
		_In_ PRT_UINT32 varIndex,
		_In_ PRT_FUN_PARAM_STATUS status,
		_Inout_ PRT_VALUE **value,
		_In_ PRT_TYPE *type
	);

	PRT_API void PRT_CALL_CONV PrtSetLocalVarEx(
		_Inout_ PRT_VALUE **locals,
		_In_ PRT_UINT32 varIndex,
		_In_ PRT_VALUE *value,
		_In_ PRT_BOOLEAN cloneValue
		);

	PRT_VALUE *MakeTupleFromArray(
		_In_ PRT_TYPE *tupleType, 
		_In_ PRT_VALUE **elems
		);
	
	void
		PrtSendPrivate(
		_In_ PRT_MACHINESTATE           *state,
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_VALUE					*event,
		_In_ PRT_VALUE					*payload
		);

	PRT_API void PRT_CALL_CONV
		PrtGoto(
			_Inout_ PRT_MACHINEINST_PRIV		*context,
			_In_ PRT_UINT32						destStateIndex,
			_In_ PRT_UINT32						numArgs,
			...
		);
	
	PRT_API void PRT_CALL_CONV
		PrtRaise(
		_Inout_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_VALUE						*event,
		_In_ PRT_UINT32						numArgs,
		...
		);

	void
		PrtPushState(
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_In_	PRT_UINT32			stateIndex
		);

	PRT_API void PRT_CALL_CONV
		PrtPop(
		_Inout_ PRT_MACHINEINST_PRIV		*context
		);

	PRT_BOOLEAN
		PrtPopState(
		_Inout_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_BOOLEAN				isPopStatement
		);

	FORCEINLINE
		void
		PrtRunExitFunction(
		_In_ PRT_MACHINEINST_PRIV			*context
		);

	FORCEINLINE
		void
		PrtRunTransitionFunction(
			_In_ PRT_MACHINEINST_PRIV			*context,
			_In_ PRT_UINT32						transIndex
		);

	PRT_UINT32
		PrtFindTransition(
		_In_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_UINT32					eventIndex
		);

	void
		PrtTakeTransition(
		_Inout_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_UINT32					eventIndex
		);

	PRT_BOOLEAN
		PrtDequeueEvent(
		_Inout_ PRT_MACHINEINST_PRIV	*context
		);

	FORCEINLINE
		PRT_STATEDECL *
		PrtGetCurrentStateDecl(
		_In_ PRT_MACHINEINST_PRIV			*context
		);

	PRT_TYPE*
		PrtGetPayloadType(
		_In_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_VALUE				*event
		);

	FORCEINLINE
		PRT_UINT16
		PrtGetPackSize(
		_In_ PRT_MACHINEINST_PRIV			*context
		);

	FORCEINLINE
		PRT_SM_FUN
		PrtGetEntryFunction(
		_In_ PRT_MACHINEINST_PRIV		*context
		);

	FORCEINLINE
		PRT_SM_FUN
		PrtGetExitFunction(
		_In_ PRT_MACHINEINST_PRIV		*context
		);

	FORCEINLINE
		PRT_DODECL*
		PrtGetAction(
		_In_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_UINT32					currEvent
		);

	FORCEINLINE
		PRT_UINT32*
		PrtGetDeferredPacked(
		_In_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_UINT32				stateIndex
		);

	FORCEINLINE
		PRT_UINT32*
		PrtGetActionsPacked(
		_In_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_UINT32				stateIndex
		);

	FORCEINLINE
		PRT_UINT32*
		PrtGetTransitionsPacked(
		_In_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_UINT32				stateIndex
		);

	FORCEINLINE
		PRT_TRANSDECL*
		PrtGetTransitionTable(
		_In_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_UINT32				stateIndex,
		_Out_ PRT_UINT32			*nTransitions
		);

	PRT_BOOLEAN
		PrtAreGuidsEqual(
		_In_ PRT_GUID guid1,
		_In_ PRT_GUID guid2
		);

	PRT_BOOLEAN
		PrtIsEventMaxInstanceExceeded(
		_In_ PRT_EVENTQUEUE			*queue,
		_In_ PRT_UINT32				eventIndex,
		_In_ PRT_UINT32				maxInstances
		);

	FORCEINLINE
		PRT_BOOLEAN
		PrtStateHasDefaultTransitionOrAction(
		_In_ PRT_MACHINEINST_PRIV			*context
		);

	FORCEINLINE
		PRT_BOOLEAN
		PrtIsSpecialEvent(
		_In_ PRT_VALUE * event
		);

	FORCEINLINE
		PRT_BOOLEAN
		PrtIsEventReceivable(
		_In_ PRT_MACHINEINST_PRIV *context,
		_In_ PRT_UINT32		eventIndex
		);

	FORCEINLINE
		PRT_BOOLEAN
		PrtIsEventDeferred(
		_In_ PRT_UINT32		eventIndex,
		_In_ PRT_UINT32*		defSet
		);

	FORCEINLINE
		PRT_BOOLEAN
		PrtIsActionInstalled(
		_In_ PRT_UINT32		eventIndex,
		_In_ PRT_UINT32*	actionSet
		);

	FORCEINLINE
		PRT_BOOLEAN
		PrtIsTransitionPresent(
		_In_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_UINT32				eventIndex
		);

	PRT_BOOLEAN
		PrtIsPushTransition(
		_In_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_UINT32					event
		);

	PRT_UINT32 *
		PrtClonePackedSet(
		_In_ PRT_UINT32 *				packedSet,
		_In_ PRT_UINT32					size
		);

	void
		PrtUpdateCurrentActionsSet(
		_Inout_ PRT_MACHINEINST_PRIV			*context
		);

	void
		PrtUpdateCurrentDeferredSet(
		_Inout_ PRT_MACHINEINST_PRIV			*context
		);

	void
		PrtResizeEventQueue(
		_Inout_ PRT_MACHINEINST_PRIV *context
		);

	void
		PrtHaltMachine(
		_Inout_ PRT_MACHINEINST_PRIV			*context
		);

	void
		PrtCleanupMachine(
		_Inout_ PRT_MACHINEINST_PRIV			*context
		);

	PRT_API void
		PrtHandleError(
		_In_ PRT_STATUS ex,
		_In_ PRT_MACHINEINST_PRIV *context
		);

	void PRT_CALL_CONV
		PrtAssertDefaultFn(
		_In_ PRT_INT32 condition,
		_In_opt_z_ PRT_CSTRING message
		);

	PRT_API void PRT_CALL_CONV
		PrtUpdateAssertFn(
		PRT_ASSERT_FUN assertFn
		);

	PRT_API void PRT_CALL_CONV
		PrtUpdatePrintFn(
		PRT_PRINT_FUN printFn
		);

	void PRT_CALL_CONV
		PrtPrintfDefaultFn(
		_In_opt_z_ PRT_CSTRING message
		);

	PRT_API void
		PrtLog(
		_In_ PRT_STEP step,
		_In_ PRT_MACHINESTATE* state,
		_In_ PRT_MACHINEINST_PRIV *receiver,
		_In_ PRT_VALUE* eventId,
		_In_ PRT_VALUE* payload
		);

	PRT_API void
		PrtCheckIsLocalMachineId(
		_In_ PRT_MACHINEINST *context,
		_In_ PRT_VALUE *id
		);

	PRT_VALUE *
		PrtGetCurrentTrigger(
		_Inout_ PRT_MACHINEINST_PRIV	*context
		);

	PRT_VALUE *
		PrtGetCurrentPayload(
		_Inout_ PRT_MACHINEINST_PRIV		*context
		);

	void
		PrtPushNewEventHandlerFrame(
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_FUNDECL				*funDecl,
		_In_ PRT_FUN_PARAM_STATUS       payloadStatus, 
		_In_ PRT_VALUE					**locals
		);

	void
		PrtPushNewFrame(
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_BOOLEAN				isFunApp,
		_In_ PRT_FUNDECL				*funDecl,
		...
		);

	PRT_API PRT_BOOLEAN
		PrtReceive(
		_Inout_ PRT_MACHINEINST_PRIV	*context
		);

	PRT_API void
		PrtRunStateMachine(
		_Inout_ PRT_MACHINEINST_PRIV	    *context
		);

	PRT_API void PRT_CALL_CONV PrtEnqueueInOrder(
		_In_ PRT_VALUE					*source,
		_In_ PRT_INT64					seqNum,
		_Inout_ PRT_MACHINEINST_PRIV	*machine,
		_In_ PRT_VALUE					*evt,
		_In_ PRT_VALUE					*payload
		);

	/** Prints a value to the output stream
	* @param[in] value The non-null value to print.
	*/
	PRT_API void PRT_CALL_CONV PrtPrintValue(_In_ PRT_VALUE *value);

	/** Converts a value to a string.
	* @param[in] value The non-null value to print.
	* @returns a string representing value. You must call PrtFreeString to release the string memory when you are done.
	*/
	PRT_API PRT_STRING PRT_CALL_CONV PrtToStringValue(_In_ PRT_VALUE *value);


	/** Create a PRT_STRING object.
	* @param[in] value The string to copy.
	* @returns a string representing value. You must call PrtFree to release the string memory when you are done.
	*/
	PRT_API PRT_STRING PRT_CALL_CONV PrtCopyString(_In_ const PRT_STRING value);

	/** Prints a type to the output stream
	* @param[in] type The non-null type to print.
	*/
	PRT_API void PRT_CALL_CONV PrtPrintType(_In_ PRT_TYPE *type);

	/** Converts a type to a string.
	* @param[in] type The non-null value to print.
	* @returns a string representing type.
	*/
	PRT_API PRT_STRING PRT_CALL_CONV PrtToStringType(_In_ PRT_TYPE *type);

	/** Prints a step to the output stream, this is designed to be called from the LogHandler function.
	* @param[in] step The step to print.
	* @param[in] senderState The state of the sender at the time they sent a message (if this is PRT_STEP_DEQUEUE).
	* @param[in] machine The machine that is making this step.
	* @param[in] event The event if this is a PRT_STEP_ENQUEUE or PRT_STEP_DEQUEUE.
	* @param[in] payload The payload of the event, if there is one.
	*/
	PRT_API void PRT_CALL_CONV PrtPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINESTATE *senderState, PRT_MACHINEINST *machine, _In_ PRT_VALUE* event, _In_ PRT_VALUE* payload);

	/** Converts a step to a string.
	* @param[in] step The step to print.
	* @param[in] senderState The state of the sender at the time they sent a message (if this is PRT_STEP_DEQUEUE).
	* @param[in] machine The machine that made the step.
	* @param[in] event The event if this is a PRT_STEP_ENQUEUE or PRT_STEP_DEQUEUE.
	* @param[in] payload The payload of the event, if there is one.
	* @returns a string representing the step, the caller must free this string using PrtFree.
	*/
	PRT_API PRT_STRING PRT_CALL_CONV PrtToStringStep(_In_ PRT_STEP step, _In_ PRT_MACHINESTATE *senderState, _In_ PRT_MACHINEINST *receiver, _In_ PRT_VALUE* event, _In_ PRT_VALUE* payload);

	PRT_API void PRT_CALL_CONV PrtFormatPrintf(_In_ PRT_CSTRING msg, ...);
#ifdef __cplusplus
}
#endif
#endif