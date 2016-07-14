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
	* @returns a string representing value.
	*/
	PRT_API PRT_STRING PRT_CALL_CONV PrtToStringValue(_In_ PRT_VALUE *value);

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
	* @param[in] machine The machine that made the step.
	*/
	PRT_API void PRT_CALL_CONV PrtPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *sender, _In_ PRT_MACHINEINST *receiver, _In_ PRT_VALUE* event, _In_ PRT_VALUE* payload);

	/** Converts a step to a string.
	* @param[in] step The step to print.
	* @param[in] machine The machine that made the step.
	* @returns a string representing the step.
	*/
	PRT_API PRT_STRING PRT_CALL_CONV PrtToStringStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *sender, _In_ PRT_MACHINEINST *receiver, _In_ PRT_VALUE* event, _In_ PRT_VALUE* payload);

	PRT_VALUE* PrtFormatPrintf(_In_ PRT_CSTRING msg, ...);
#ifdef __cplusplus
}
#endif
#endif