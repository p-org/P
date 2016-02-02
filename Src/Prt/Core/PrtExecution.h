#ifndef PRT_EXECUTION_H
#define PRT_EXECUTION_H

#include "Prt.h"

#ifdef __cplusplus
extern "C"{
#endif

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

	typedef struct PRT_PROCESS_PRIV {
		PRT_GUID				guid;
		PRT_PROGRAMDECL			*program;
		PRT_ERROR_FUN	        errorHandler;
		PRT_LOG_FUN				logHandler;
		PRT_RECURSIVE_MUTEX		processLock;
		PRT_UINT32				numMachines;
		PRT_UINT32				machineCount;
		PRT_MACHINEINST			**machines;
	} PRT_PROCESS_PRIV;

	typedef enum PRT_LASTOPERATION
	{
		ReturnStatement,
		PopStatement,
		RaiseStatement
	} PRT_LASTOPERATION;

	typedef struct PRT_EVENT
	{
		PRT_VALUE *trigger;
		PRT_VALUE *payload;
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

	typedef struct PRT_FUNSTACK_INFO
	{
		PRT_UINT32		funIndex;
		PRT_VALUE		**locals;
		PRT_BOOLEAN		freeLocals;
		PRT_UINT16		returnTo;
		PRT_CASEDECL	*rcase;
	} PRT_FUNSTACK_INFO;

	typedef struct PRT_FUNSTACK
	{
		PRT_FUNSTACK_INFO	funs[PRT_MAX_FUNSTACK_DEPTH];
		PRT_UINT16			length;
	} PRT_FUNSTACK;

	typedef struct PRT_EVENTSTACK
	{
		PRT_EVENT			events[PRT_MAX_EVENTSTACK_DEPTH];
		PRT_UINT16			length;
	} PRT_EVENTSTACK;

	typedef struct PRT_MACHINEINST_PRIV {
		PRT_PROCESS		    *process;
		PRT_UINT32			instanceOf;
		PRT_VALUE			*id;
		void				*extContext;
		PRT_BOOLEAN			isModel;
		PRT_VALUE           *recvMap;
		PRT_VALUE			**varValues;
		PRT_RECURSIVE_MUTEX stateMachineLock;
		PRT_BOOLEAN			isRunning;
		PRT_BOOLEAN			isHalted;
		PRT_UINT32			currentState;
		PRT_RECEIVEDECL		*receive;
		PRT_STATESTACK		callStack;
		PRT_FUNSTACK		funStack;
		PRT_VALUE			*currentTrigger;
		PRT_VALUE			*currentPayload;
		PRT_EVENTQUEUE		eventQueue;
		PRT_LASTOPERATION	lastOperation;
		PRT_UINT32          *inheritedDeferredSetCompact;
		PRT_UINT32          *currentDeferredSetCompact;
		PRT_UINT32          *inheritedActionSetCompact;
		PRT_UINT32          *currentActionSetCompact;
	} PRT_MACHINEINST_PRIV;

	/** Sets a global variable to variable
	* @param[in,out] context The context to modify.
	* @param[in] varIndex The index of the variable to modify.
	* @param[in] value The value to set. (Will be cloned)
	*/
	PRT_API void PRT_CALL_CONV PrtSetGlobalVar(_Inout_ PRT_MACHINEINST_PRIV * context, _In_ PRT_UINT32 varIndex, _In_ PRT_VALUE * value);

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
		_In_  PRT_UINT32				instanceOf,
		_In_  PRT_VALUE					*payload
		);

	PRT_API void PRT_CALL_CONV PrtSetLocalVarEx(
		_Inout_ PRT_VALUE **locals,
		_In_ PRT_UINT32 varIndex,
		_In_ PRT_VALUE *value,
		_In_ PRT_BOOLEAN cloneValue
		);

	void
		PrtSendPrivate(
		_Inout_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_VALUE					*event,
		_In_ PRT_VALUE					*payload
		);

	PRT_API void PRT_CALL_CONV
		PrtRaise(
		_Inout_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_VALUE					*event,
		_In_ PRT_VALUE					*payload
		);

	PRT_API void PRT_CALL_CONV
		PrtPush(
		_Inout_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_UINT32					stateIndex
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

	void
		PrtPopState(
		_Inout_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_BOOLEAN				isPopStatement
		);

	FORCEINLINE
		void
		PrtRunExitFunction(
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
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_Inout_ PRT_FUNSTACK_INFO		*frame
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

	PRT_API PRT_SM_FUN PRT_CALL_CONV
		PrtGetFunction(
		_In_ PRT_MACHINEINST_PRIV		*context,
		_In_ PRT_UINT32 funIndex
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
		_In_ PRT_MACHINEINST_PRIV		*context
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

	void
		PrtCleanupModel(
		_Inout_ PRT_MACHINEINST			*context
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
		_In_ PRT_MACHINEINST_PRIV *context
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

	PRT_FUNSTACK_INFO *
		PrtTopOfFunStack(
		_In_ PRT_MACHINEINST_PRIV	*context
		);

	PRT_FUNSTACK_INFO *
		PrtBottomOfFunStack(
		_In_ PRT_MACHINEINST_PRIV	*context
		);

	void
		PrtPushNewEventHandlerFrame(
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_UINT32					funIndex,
		_In_ PRT_VALUE					**locals
		);

	void
		PrtPushNewFrame(
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_UINT32					funIndex,
		_In_ PRT_VALUE					*parameters
		);

	PRT_API void
		PrtPushFrame(
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_FUNSTACK_INFO *funStackInfo
		);

	PRT_API void
		PrtPopFrame(
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_Inout_ PRT_FUNSTACK_INFO *funStackInfo
		);

	PRT_API void
		PrtFreeLocals(
		_In_ PRT_MACHINEINST_PRIV		*context,
		_Inout_ PRT_FUNSTACK_INFO		*frame
		);

	PRT_API PRT_VALUE *
		PrtWrapFunStmt(
		_Inout_ PRT_FUNSTACK_INFO		*frame,
		_In_ PRT_UINT16					funCallIndex,
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_In_ PRT_UINT32					funIndex,
		_In_ PRT_VALUE					*parameters
		);

	PRT_API PRT_BOOLEAN
		PrtReceive(
		_Inout_ PRT_MACHINEINST_PRIV	*context,
		_Inout_ PRT_FUNSTACK_INFO		*funStackInfo,
		_In_ PRT_UINT16					receiveIndex
		);

	PRT_API void
		PrtRunStateMachine(
		_Inout_ PRT_MACHINEINST_PRIV	    *context,
		_In_ PRT_BOOLEAN				doDequeue
		);

	PRT_API void PRT_CALL_CONV PrtEnqueueInOrder(
		_In_ PRT_VALUE					*source,
		_In_ PRT_INT64					seqNum,
		_Inout_ PRT_MACHINEINST_PRIV	*machine,
		_In_ PRT_VALUE					*evt,
		_In_ PRT_VALUE					*payload
		);

#ifdef __cplusplus
}
#endif
#endif