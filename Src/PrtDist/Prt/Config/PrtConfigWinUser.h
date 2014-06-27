/**
* \file PrtConfigWinUser.h
* \brief Defines the Windows user configurations.
*/
#ifndef PRTCONFIG_WINUSER_H
#define PRTCONFIG_WINUSER_H

/** "unsafe" string functions are used safely. Allows for portability of code between operating systems. */
#define _CRT_SECURE_NO_WARNINGS

#ifndef PRT_ARCH
#error No architecture was specified (see PrtConfig.h for details)
#elif PRT_ARCH == PRT_ARCH_X86
#define _X86_
#elif PRT_PLAT == PRT_ARCH_X64
#define _AMD64_
#else
#error An invalid architecture was specified (see PrtConfig.h for details)
#endif

#ifdef PRT_DEBUG
#ifndef _DEBUG
#define _DEBUG
#endif

#define _CRTDBG_MAP_ALLOC

#include <stdlib.h>
#include <malloc.h>
#include <crtdbg.h>
#include <sal.h>
#include <stddef.h>
#include <synchapi.h>
#include <windows.h>
#include <stdio.h>

#define PRT_DBG_ASSERT(condition, message) PrtAssert((condition), (message))
#define PRT_DBG_START_MEM_BALANCED_REGION { _CrtMemState prtDbgMemStateInitial, prtDbgMemStateFinal, prtDbgMemStateDiff; _CrtMemCheckpoint(&prtDbgMemStateInitial);
#define PRT_DBG_END_MEM_BALANCED_REGION _CrtMemCheckpoint(&prtDbgMemStateFinal); PrtAssert(!_CrtMemDifference(&prtDbgMemStateDiff, &prtDbgMemStateInitial, &prtDbgMemStateFinal), "Memory leak"); }

#else

#include <sal.h>
#include <stddef.h>
#include <synchapi.h>
#include <windows.h>
#include <stdlib.h>
#include <stdio.h>
#include <malloc.h>

#endif

#if !defined(DISTRIBUTED_RUNTIME)
/** PRT_UINT8 is always an 8-bit unsigned integer. */
typedef unsigned __int8  PRT_UINT8;
/** PRT_UINT16 is always a 16-bit unsigned integer. */
typedef unsigned __int16 PRT_UINT16;
/** PRT_UINT32 is always a 32-bit unsigned integer. */
typedef unsigned __int32 PRT_UINT32;
/** PRT_UINT64 is always a 64-bit unsigned integer. */
typedef unsigned __int64 PRT_UINT64;

/** PRT_INT8 is always an 8-bit signed integer. */
typedef signed __int8  PRT_INT8;
/** PRT_INT16 is always a 16-bit signed integer. */
typedef signed __int16 PRT_INT16;
/** PRT_INT32 is always a 32-bit signed integer. */
typedef signed __int32 PRT_INT32;
/** PRT_INT64 is always a 64-bit signed integer. */
typedef signed __int64 PRT_INT64;

/** PRT_CHAR is always an ASCII character. */
typedef char PRT_CHAR;
/** PRT_STRING is always an array of ASCII characters. */
typedef char * PRT_STRING;
/** PRT_CSTRING is always a constant array of ASCII characters. */
typedef char const * PRT_CSTRING;
#else
#include"../../PrtDistributedIDL/PrtDistributed_TypesAndValues_h.h"
#endif


/** PRT_HANDLE is always a type s.t. sizeof(PRT_HANDLE) >= sizeof(void*). */
typedef void * PRT_HANDLE;

/** PRT_RECURSIVE_MUTEX identifies a recursive mutex. */
typedef HANDLE PRT_RECURSIVE_MUTEX;

/**
* Configuration-specific startup (for instance, opening log files). Will be called by the runtime in PrtStartup()
* @param[in] param Configuration-specific startup data.
* @see PrtSpecialShutdown
* @see PrtStartup
* @see PrtShutdown
*/
void PrtSpecialStartup(_In_ void *param);

/**
* Configuration-specific shutdown (for instance, closing log files). Will be called by the runtime in PrtShutdown()
* @param[in] param Configuration-specific shutdown data.
* @see PrtSpecialStartup
* @see PrtStartup
* @see PrtShutdown
*/
void PrtSpecialShutdown(_In_ void *param);

/**
* Terminates the process if `condition == 0` (with configuration-specific logging)
* @param[in] condition A value expected to be non-zero
* @param[in] message A message to be logged if condition is zero
*/
void PrtAssert(_In_ int condition, _In_opt_z_ PRT_CSTRING message);

/**
* Creates a fresh unnamed and unlocked recursive mutex. The mutex must be unlocked by a thread as many times as it was locked.
* @return A configuration-specific value identifying the mutex.
* @see PrtReleaseMutex
* @see PrtLockMutex
* @see PrtUnlockMutex
*/
PRT_RECURSIVE_MUTEX PrtCreateMutex();

/**
* Allows the system to dispose of this mutex. Release must be called at most once per mutex, and a released mutex never be used again.
* @param[in] mutex A mutex that has been created, but has not yet been released.
* @see PrtCreateMutex
* @see PrtLockMutex
* @see PrtUnlockMutex
*/
void PrtReleaseMutex(_In_ PRT_RECURSIVE_MUTEX mutex);

/**
* Blocks until the mutex is locked. If the locking thread already owns the mutex, then succeeds and increments the lock count. 
* @param[in] mutex The mutex to lock.
* @see PrtUnlockMutex
* @see PrtCreateMutex
* @see PrtReleaseMutex
*/
void PrtLockMutex(_In_ PRT_RECURSIVE_MUTEX mutex);

/**
* Unlocks a locked mutex. Should not be called more times than the mutex has been locked.
* @param[in] mutex The mutex to unlock.
* @see PrtLockMutex
* @see PrtCreateMutex
* @see PrtReleaseMutex
*/
void PrtUnlockMutex(_In_ PRT_RECURSIVE_MUTEX mutex);

/**
* Calls system-specific implementation of malloc. 
* Fails eagerly if memory cannot be allocated.
* @param[in] size Number of bytes to allocate.
* @returns A pointer to a memory location
* @see PrtFree
*/
void *PrtMalloc(_In_ size_t size);

/**
* Calls system-specific implementation of free.
* @param[in,out] ptr A pointer to a memory block to be freed.
* @see PrtMalloc
* @see PrtCalloc
* @see PrtRealloc
*/
void PrtFree(void *ptr);

/**
* Calls system-specific implementation of calloc.
* Fails eagerly if memory cannot be allocated.
* @param[in] nmemb Number of bytes to allocate per member.
* @param[in] size Number of bytes to allocate per member.
* @returns A pointer to a memory location
* @see PrtFree
*/
void *PrtCalloc(_In_ size_t nmemb, _In_ size_t size);

/**
* Calls system-specific implementation of realloc.
* Fails eagerly if memory cannot be allocated.
* @param[in,out] ptr A pointer to a memory block to reallocate.
* @param[in] size Number of bytes to reallocate per member.
* @returns A pointer to a memory location or NULL if size = 0
* @see PrtFree
*/
void *PrtRealloc(_Inout_ void *ptr, _In_ size_t size);
#endif