/**
* \file Prt.h
* \brief The main interface to the runtime
* Use these methods to start and stop instances of the runtime for a given P program.
*/
#ifndef PRT_H
#define PRT_H

#include "PrtProgram.h"

#ifdef __cplusplus
extern "C"{
#endif

typedef struct PRT_PROCESS {
	PRT_GUID guid;
	PRT_PROGRAMDECL *program;
} PRT_PROCESS;

typedef struct PRT_SMCONTEXT
{
	PRT_PROCESS		    *parentProcess;
	PRT_UINT32			instanceOf;
	PRT_VALUE			*thisP;
	PRT_VALUE			**values;
	void				*extContext;
} PRT_SMCONTEXT;

	/*********************************************************************************

	Enum Types Declarations

	*********************************************************************************/

	typedef enum PRT_STEP
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
		// trace Halting of a machine
		//
		traceHalt
	} PRT_STEP;

	/*********************************************************************************

	P Exceptions

	*********************************************************************************/

	typedef enum PRT_STATUS
	{
		//
		// Nothing bad happened
		//
		Success,
		//
		// Unhandled event exception
		//
		UnhandledEvent,
		//
		// Tried to enqueue an event more than max instances
		//
		MaxInstanceExceeded,
		//
		// Max Queue size exceeded
		//
		MaxQueueSizeExceeded
	} PRT_STATUS;


	typedef void(PRT_CALL_CONV *PRT_ERROR_FUN)(PRT_STATUS, void*);

	typedef void(PRT_CALL_CONV *PRT_LOG_FUN)(PRT_STEP, void*);


	PRT_API PRT_PROCESS * PRT_CALL_CONV PrtStartProcess(
		__in PRT_GUID guid,
		__in PRT_PROGRAMDECL *program,
		__in PRT_ERROR_FUN errorFun,
		__in PRT_LOG_FUN loggerFun
		);

	PRT_API void PRT_CALL_CONV PrtStopProcess(PRT_PROCESS* stopProcess);

	/** Send message to P state machine.
	* @param[in] machine : target machine to send message
	* @param[in] event : event to be sent.
	* @param[in] payload : payload to be send with 'even'.
	* @see PrtEnqueueEvent
	*/
	PRT_API void PRT_CALL_CONV PrtSend(__in PRT_SMCONTEXT *machine, __in PRT_VALUE *evt, __in PRT_VALUE *payload);

	/** Send message to P state machine.
	* @param[in] process : p process, instance of a P program.
	* @param[in] instanceOfMachine : instance of a statemachine in 'process'.
	* @param[in] payload : payload to be used in the start state.
	* @param[out] pSM : pointer to the StateMachine
	* @see PrtEnqueueEvent
	*/

	PRT_API PRT_STATUS PRT_CALL_CONV PrtMkMachine(
		__in PRT_PROCESS *process, 
		__in PRT_UINT32 instanceOfMachine, 
		__in PRT_VALUE *payload, 
		__out PRT_SMCONTEXT *pSM);

#ifdef __cplusplus
}
#endif
#endif
