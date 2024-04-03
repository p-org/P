/**
* \file PrtSGXUserConfig.h
* \brief Defines the Linux user configurations.
*/
#ifndef PRTCONFIG_SGXUSER_H
#define PRTCONFIG_SGXUSER_H


#if defined(PRT_USE_CLANG)
#define FORCEINLINE __attribute__((always_inline))
#elif defined(PRT_USE_GCC)
#define FORCEINLINE
#else
#define FORCEINLINE
#endif

#ifdef __cplusplus
extern "C"{
#endif

    /** Calling convention */
#define PRT_CALL_CONV


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

#include <sal.h>
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <sgx_thread.h>
#include <sgx_trts.h>
#include "ext_compat.h"

	/** PRT uses these definitions for boolean values */
	typedef enum PRT_BOOLEAN
	{
		PRT_FALSE = 0,   /**< 0 means false */
		PRT_TRUE = 1    /**< 1 means true  */
	} PRT_BOOLEAN;

  #if __x86_64__ || __ppc64__
    typedef int64_t  PRT_INT;
    typedef double   PRT_FLOAT;
  #else
    typedef int32_t  PRT_INT;
    typedef float   PRT_FLOAT;
  #endif
	/** PRT_UINT8 is always an 8-bit unsigned integer. */
	typedef uint8_t  PRT_UINT8;
	/** PRT_UINT16 is always a 16-bit unsigned integer. */
	typedef uint16_t PRT_UINT16;
	/** PRT_UINT32 is always a 32-bit unsigned integer. */
	typedef uint32_t PRT_UINT32;
	/** PRT_UINT64 is always a 64-bit unsigned integer. */
	typedef uint64_t PRT_UINT64;

	/** PRT_INT8 is always an 8-bit signed integer. */
	typedef int8_t  PRT_INT8;
	/** PRT_INT16 is always a 16-bit signed integer. */
	typedef int16_t PRT_INT16;
	/** PRT_INT32 is always a 32-bit signed integer. */
	typedef int32_t PRT_INT32;
	/** PRT_INT64 is always a 64-bit signed integer. */
	typedef int64_t PRT_INT64;

	/** PRT_CHAR is always an ASCII character. */
	typedef char PRT_CHAR;
	/** PRT_STRING is always an array of ASCII characters. */
	typedef char * PRT_STRING;
	/** PRT_CSTRING is always a constant array of ASCII characters. */
	typedef char const * PRT_CSTRING;

	/** PRT_RECURSIVE_MUTEX identifies a recursive mutex. */
    typedef sgx_thread_mutex_t PRT_MUTEX_OBJ;
    typedef sgx_thread_mutexattr_t PRT_MUTEX_ATTR;
	typedef PRT_MUTEX_OBJ* PRT_RECURSIVE_MUTEX;

    /** PRT_SEMAPHORE identifies a platform specific semaphore object. */
    // FIXME: define a real semaphore
    typedef sgx_thread_mutex_t* PRT_SEMAPHORE;

#ifdef __cplusplus
}
#endif
#endif
#endif
