/**
* \file PrtConfigWinUser.h
* \brief Defines the Windows user configurations.
*/
#ifndef PRTCONFIG_WINUSER_H
#define PRTCONFIG_WINUSER_H

#ifdef __cplusplus
extern "C" {
#endif

	/** Calling convention */
#define PRT_CALL_CONV __cdecl

/** Linking method */
#ifdef PRT_API_IMPL
#define PRT_API __declspec(dllexport)
#else
#ifdef PRT_STATIC
#define PRT_API
#else
#ifdef _DLL
#define PRT_API __declspec(dllimport)
#else
#define PRT_API
#endif
#endif
#endif

#include <malloc.h>
#include <sal.h>
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <windows.h>

/** PRT uses these definitions for boolean values */
	typedef enum PRT_BOOLEAN
	{
		PRT_FALSE = 0,
		/**< 0 means false */
		PRT_TRUE = 1 /**< 1 means true  */
	} PRT_BOOLEAN;

	/** PRT_INT represents the primitive int type in P */
#if _WIN64
	typedef signed __int64 PRT_INT;
	typedef double PRT_FLOAT;
#else
	typedef signed __int32 PRT_INT;
	typedef float PRT_FLOAT;
#endif

	/** PRT_UINT8 is always an 8-bit unsigned integer. */
	typedef unsigned __int8 PRT_UINT8;
	/** PRT_UINT16 is always a 16-bit unsigned integer. */
	typedef unsigned __int16 PRT_UINT16;
	/** PRT_UINT32 is always a 32-bit unsigned integer. */
	typedef unsigned __int32 PRT_UINT32;
	/** PRT_UINT64 is always a 64-bit unsigned integer. */
	typedef unsigned __int64 PRT_UINT64;

	/** PRT_INT8 is always an 8-bit signed integer. */
	typedef signed __int8 PRT_INT8;
	/** PRT_INT16 is always a 16-bit signed integer. */
	typedef signed __int16 PRT_INT16;
	/** PRT_INT32 is always a 32-bit signed integer. */
	typedef signed __int32 PRT_INT32;
	/** PRT_INT64 is always a 64-bit signed integer. */
	typedef signed __int64 PRT_INT64;

	/** PRT_CHAR is always an ASCII character. */
	typedef char PRT_CHAR;
	/** PRT_STRING is always an array of ASCII characters. */
	typedef char* PRT_STRING;
	/** PRT_CSTRING is always a constant array of ASCII characters. */
	typedef char const* PRT_CSTRING;

	/** PRT_RECURSIVE_MUTEX identifies a recursive mutex. */
	typedef HANDLE PRT_RECURSIVE_MUTEX;

	/** PRT_SEMAPHORE identifies a platform specific semaphore object. */
	typedef HANDLE PRT_SEMAPHORE;

#ifdef __cplusplus
}
#endif

#endif
