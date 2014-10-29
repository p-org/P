/**
* \file PrtWinUser.h
* \brief The main interface to the PrtWinUser runtime
* Extends Prt interface with Windows User-mode specific values.
*/
#ifndef PRTWINUSER_H
#define PRTWINUSER_H

#include "../API/Prt.h"

#ifdef __cplusplus
extern "C"{
#endif

/** Prints a type to the output stream
* @param[in] type The type to print.
*/
PRT_API	void PRT_CALL_CONV PrtPrintType(_In_ PRT_TYPE *type);

/** Prints a value to the output stream
* @param[in] value The value to print.
*/
PRT_API	void PRT_CALL_CONV PrtPrintValue(_In_ PRT_VALUE *value);

#ifdef __cplusplus
}
#endif
#endif