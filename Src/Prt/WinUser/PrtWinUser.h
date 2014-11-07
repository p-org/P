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

PRT_API void PRT_CALL_CONV PrtWinUserPrintUint16(_In_ PRT_UINT16 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten);

PRT_API void PRT_CALL_CONV PrtWinUserPrintUint32(_In_ PRT_UINT32 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten);

PRT_API void PRT_CALL_CONV PrtWinUserPrintUint64(_In_ PRT_UINT64 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten);

PRT_API void PRT_CALL_CONV PrtWinUserPrintInt32(_In_ PRT_INT32 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten);

PRT_API void PRT_CALL_CONV PrtWinUserPrintString(_In_ PRT_STRING s, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten);

PRT_API void PRT_CALL_CONV PrtWinUserPrintMachineId(_In_ PRT_MACHINEID id, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten);

/** Prints a type to the output stream
* @param[in] type The type to print.
*/
PRT_API	void PRT_CALL_CONV PrtWinUserPrintType(_In_ PRT_TYPE *type, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten);

/** Prints a value to the output stream
* @param[in] value The value to print.
*/
PRT_API	void PRT_CALL_CONV PrtWinUserPrintValue(_In_ PRT_VALUE *value, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten);

PRT_API void PRT_CALL_CONV PrtWinUserPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST_PRIV *c, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten);

#ifdef __cplusplus
}
#endif
#endif