/**
* \file PrtWinUser.h
* \brief The main interface to the PrtWinUser runtime
* Extends Prt interface with Windows User-mode specific values.
*/
#ifndef PRTWINUSER_H
#define PRTWINUSER_H

#include "PrtExecution.h"

#ifdef __cplusplus
extern "C"{
#endif

	/** Converts foreign value to string
	* @param[in] typetag of the foreign type
	* @param[in] foreign value
	*/
	extern PRT_FORGN_TOSTRING PrtForeignValueToString;

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

	/** Prints a step to the output stream
	* @param[in] step The step to print.
	* @param[in] machine The machine that made the step.
	*/
	PRT_API void PRT_CALL_CONV PrtPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *machine);

	/** Converts a step to a string.
	* @param[in] step The step to print.
	* @param[in] machine The machine that made the step.
	* @returns a string representing the step.
	*/
	PRT_API PRT_STRING PRT_CALL_CONV PrtToStringStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *machine);

#ifdef __cplusplus
}
#endif
#endif