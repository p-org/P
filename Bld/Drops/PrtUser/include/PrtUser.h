/**
* \file PrtUser.h
* \brief The main interface to the PrtUser runtime
* Extends Prt interface with Windows User-mode specific values.
*/
#ifndef PRTUSER_H
#define PRTUSER_H

#include "PrtExecution.h"

#ifdef __cplusplus
extern "C"{
#endif

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