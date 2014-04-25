/**
* \file PrtConfigWinUser.h
* \brief Defines the Windows user configurations.
*/
#ifndef PRTCONFIG_WINUSER_H
#define PRTCONFIG_WINUSER_H

#include <sal.h>
#include <stddef.h>

#ifndef PRT_ARCH
#error No architecture was specified (see PrtConfig.h for details)
#elif PRT_ARCH == PRT_ARCH_X86
#define _X86_
#elif PRT_PLAT == PRT_ARCH_X64
#define _AMD64_
#else
#error An invalid architecture was specified (see PrtConfig.h for details)
#endif

#include<synchapi.h>
#include<windows.h>

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
* Terminates the process if `condition == PRT_FALSE` (with configuration-specific logging)
* @param[in] condition A boolean value expected to be true
* @param[in] code A code to be logged if condition is false
* @param[in] message A message to be logged if condition is false
*/
void PrtAssert(_In_ PRT_BOOLEAN condition, _In_ PRT_UINT32 code, _In_opt_z_ PRT_CSTRING message);

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

#endif