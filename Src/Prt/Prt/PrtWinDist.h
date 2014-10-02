/**
* \file PrtConfigWinDist.h
* \brief Defines the Distributed Runtime configuration.
*/
#include "PrtHeaders.h"
#include "PrtDistributed.h"

#ifdef __cplusplus
extern "C"{
#endif

#ifndef PRTCONFIG_WINDIST_H
#define PRTCONFIG_WINDIST_H


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

/** PRT_STATUS is the status of the operation performed */
typedef PRT_BOOLEAN PRT_STATUS;

/** PRT_RECURSIVE_MUTEX identifies a recursive mutex. */
typedef HANDLE PRT_RECURSIVE_MUTEX;


/**
* Terminates the process if `condition == 0` (with configuration-specific logging)
* @param[in] condition A value expected to be non-zero
* @param[in] message A message to be logged if condition is zero
*/
VOID PrtAssert(_In_ int condition, _In_opt_z_ PRT_CSTRING message);

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
VOID PrtReleaseMutex(_In_ PRT_RECURSIVE_MUTEX mutex);

/**
* Blocks until the mutex is locked. If the locking thread already owns the mutex, then succeeds and increments the lock count.
* @param[in] mutex The mutex to lock.
* @see PrtUnlockMutex
* @see PrtCreateMutex
* @see PrtReleaseMutex
*/
VOID PrtLockMutex(_In_ PRT_RECURSIVE_MUTEX mutex);

/**
* Unlocks a locked mutex. Should not be called more times than the mutex has been locked.
* @param[in] mutex The mutex to unlock.
* @see PrtLockMutex
* @see PrtCreateMutex
* @see PrtReleaseMutex
*/
VOID PrtUnlockMutex(_In_ PRT_RECURSIVE_MUTEX mutex);

/**
* Calls system-specific implementation of malloc.
* Fails eagerly if memory cannot be allocated.
* @param[in] size Number of bytes to allocate.
* @returns A pointer to a memory location
* @see PrtFree
*/
PVOID PrtMalloc(_In_ size_t size);

/**
* Calls system-specific implementation of free.
* @param[in,out] ptr A pointer to a memory block to be freed.
* @see PrtMalloc
* @see PrtCalloc
* @see PrtRealloc
*/
VOID PrtFree(PVOID ptr);

/**
* Calls system-specific implementation of calloc.
* Fails eagerly if memory cannot be allocated.
* @param[in] nmemb Number of bytes to allocate per member.
* @param[in] size Number of bytes to allocate per member.
* @returns A pointer to a memory location
* @see PrtFree
*/
PVOID PrtCalloc(_In_ size_t nmemb, _In_ size_t size);

/**
* Calls system-specific implementation of realloc.
* Fails eagerly if memory cannot be allocated.
* @param[in,out] ptr A pointer to a memory block to reallocate.
* @param[in] size Number of bytes to reallocate per member.
* @returns A pointer to a memory location or NULL if size = 0
* @see PrtFree
*/
PVOID PrtRealloc(_Inout_ PVOID ptr, _In_ size_t size);
#endif

#ifdef __cplusplus
}
#endif