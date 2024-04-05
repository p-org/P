/**
* \file PrtNuttxUserConfig.h
* \brief Defines the NuttX user configurations.
* see http://www.nuttx.org/
*/
#ifndef PRTCONFIG_WINUSER_H
#define PRTCONFIG_WINUSER_H

#ifdef __cplusplus
extern "C"{
#endif

    /** "unsafe" string functions are used safely. Allows for portability of code between operating systems. */
#ifndef _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS
#endif

    /** Calling convention __cdecl is not supported by gcc. */
#define PRT_CALL_CONV

    /* These argument annotations are not supported by gcc */
#define _In_
#define _Inout_
#define _Out_
#define _In_opt_z_
#define __in

#define FORCEINLINE

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

#include <nuttx/kmalloc.h>
#include <nuttx/pthread.h>
#include <semaphore.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include "SafeString.h"

#ifdef PRT_DEBUG
#ifndef _DEBUG
#define _DEBUG
#endif

#define fprintf_s(a,b) fprintf(a,b)
#define sprintf_s(a,b,c) sprintf(a,c)

    //#define PrtMalloc(size) malloc(size)
    //#define PrtCalloc(nmemb, size) calloc(nmemb, size)
#define PRT_DBG_ASSERT(condition, message) PrtAssert((condition), (message))
#define PRT_DBG_START_MEM_BALANCED_REGION { _CrtMemState prtDbgMemStateInitial, prtDbgMemStateFinal, prtDbgMemStateDiff; _CrtMemCheckpoint(&prtDbgMemStateInitial);
#define PRT_DBG_END_MEM_BALANCED_REGION _CrtMemCheckpoint(&prtDbgMemStateFinal); PrtAssert(!_CrtMemDifference(&prtDbgMemStateDiff, &prtDbgMemStateInitial, &prtDbgMemStateFinal), "Memory leak"); }

#endif

#ifdef PRT_USE_IDL
#include "PrtBaseTypes_IDL.h"
#else
    /** PRT uses these definitions for boolean values */
    typedef enum PRT_BOOLEAN
    {
        PRT_FALSE = 0,   /**< 0 means false */
        PRT_TRUE = 1    /**< 1 means true  */
    } PRT_BOOLEAN;

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

#endif

    /** PRT_RECURSIVE_MUTEX identifies a recursive mutex. */
    typedef pthread_mutex_t* PRT_RECURSIVE_MUTEX;

    /** PRT_SEMAPHORE identifies a platform specific semaphore object. */
    typedef sem_t* PRT_SEMAPHORE;

    /** Function for Assertion will be called whenever an assertion is checked */
    typedef void(PRT_CALL_CONV * PRT_ASSERT_FUN)(PRT_INT32, PRT_CSTRING);

    /** Function for printing string, will be invoked whenever print statement is called from the runtime */
    typedef void(PRT_CALL_CONV * PRT_PRINT_FUN)(PRT_CSTRING);

    /* declare the function to assert function */
    extern PRT_ASSERT_FUN PrtAssert;

    /* declare the function to print fucntion*/
    extern PRT_PRINT_FUN PrtPrintf;

    /**
    * Creates a fresh unnamed and unlocked recursive mutex. The mutex must be unlocked by a thread as many times as it was locked.
    * @return A configuration-specific value identifying the mutex.
    * @see PrtDestroyMutex
    * @see PrtLockMutex
    * @see PrtUnlockMutex
    */
    PRT_API PRT_RECURSIVE_MUTEX PRT_CALL_CONV PrtCreateMutex(void);

    /**
    * Allows the system to dispose of this mutex. Destroy must be called at most once per mutex, and a destroyed mutex never be used again.
    * @param[in] mutex A mutex that has been created by PrtCreateMutex.
    * @see PrtCreateMutex
    * @see PrtLockMutex
    * @see PrtUnlockMutex
    */
    PRT_API void PRT_CALL_CONV PrtDestroyMutex(_In_ PRT_RECURSIVE_MUTEX mutex);

    /**
    * Blocks until the mutex is locked. If the locking thread already owns the mutex, then succeeds and increments the lock count.
    * @param[in] mutex The mutex to lock.
    * @see PrtUnlockMutex
    * @see PrtCreateMutex
    * @see PrtDestroyMutex
    */
    PRT_API void PRT_CALL_CONV PrtLockMutex(_In_ PRT_RECURSIVE_MUTEX mutex);

    /**
    * Unlocks a locked mutex. Should not be called more times than the mutex has been locked.
    * @param[in] mutex The mutex to unlock.
    * @see PrtLockMutex
    * @see PrtCreateMutex
    * @see PrtDestroyMutex
    */
    PRT_API void PRT_CALL_CONV PrtUnlockMutex(_In_ PRT_RECURSIVE_MUTEX mutex);

    /**
    * Creates a fresh unnamed semaphore. The semaphore must be released after wait succeeds.
    * @param[in] initialCount The initial number of semaphores available.
    * @param[in] maximumCount The maximum number of semaphores available.
    * @return A configuration-specific value identifying the semaphore.
    * @see PrtDestroySemaphore
    * @see PrtWaitSemaphore
    * @see PrtReleaseSemaphore
    */
    PRT_API PRT_SEMAPHORE PRT_CALL_CONV PrtCreateSemaphore(int initialCount, int maximumCount);

    /**
    * Allows the system to dispose of this mutex. Destroy must be called at most once per mutex, and a destroyed mutex never be used again.
    * @param[in] semaphore A semaphore that has been created by PrtCreateSemaphore.
    * @see PrtCreateSemaphore
    * @see PrtWaitSemaphore
    * @see PrtReleaseSemaphore
    */
    PRT_API void PRT_CALL_CONV PrtDestroySemaphore(_In_ PRT_SEMAPHORE semaphore);

    /**
    * Blocks until the semaphore is available.  Only one thread will be granted access per available count.
    * This is not re-entrant like Mutex is, if the same thread waits again it will block.
    * @param[in] semaphore The semaphore to wait on.
    * @param[in] maxWaitTime The maximum time to wait in milliseconds or -1 for infinite wait.
    * @return PRT_TRUE if access is granted or false if a timeout occurred.
    * @see PrtCreateSemaphore
    * @see PrtReleaseSemaphore
    * @see PrtDestroySemaphore
    */
    PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtWaitSemaphore(_In_ PRT_SEMAPHORE semaphore, _In_ long maxWaitTime);

    /**
    * Releases a semaphore, this must be called once for each time PrtWaitSemaphore succeeds.
    * This increases the count of available semaphores, allowing other threads access.
    * Any thread can call this method to increase the count, not just the thread that was granted access.
    * @param[in] semaphore The semaphore to release.
    * @return PRT_TRUE if the count is increased, or false if the maximum count has been exceeded.
    * @see PrtCreateSemaphore
    * @see PrtWaitSemaphore
    * @see PrtDestroySemaphore
    */
    PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtReleaseSemaphore(_In_ PRT_SEMAPHORE semaphore);

    /**
    * Yields the current thread.  This is only used in PrtRunProcess when the PRT_SCHEDULINGPOLICY is
    * set to Cooperative.
    * @see PRT_SCHEDULINGPOLICY
    */
    PRT_API void PRT_CALL_CONV PrtYieldThread(void);

    /**
    * Calls system-specific implementation of malloc.
    * Fails eagerly if memory cannot be allocated.
    * @param[in] size Number of bytes to allocate.
    * @returns A pointer to a memory location
    * @see PrtFree
    */
    PRT_API void * PRT_CALL_CONV PrtMalloc(_In_ size_t size);

    /**
    * Calls system-specific implementation of calloc.
    * Fails eagerly if memory cannot be allocated.
    * @param[in] nmemb Number of bytes to allocate per member.
    * @param[in] size Number of bytes to allocate per member.
    * @returns A pointer to a memory location
    * @see PrtFree
    */
    PRT_API void * PRT_CALL_CONV PrtCalloc(_In_ size_t nmemb, _In_ size_t size);

    /**
    * Calls system-specific implementation of realloc.
    * Fails eagerly if memory cannot be allocated.
    * @param[in,out] ptr A pointer to a memory block to reallocate.
    * @param[in] size Number of bytes to reallocate per member.
    * @returns A pointer to a memory location or NULL if size = 0
    * @see PrtFree
    */
    void * PRT_CALL_CONV PrtRealloc(_Inout_ void * ptr, _In_ size_t size);

    /**
    * Calls system-specific implementation of free.
    * @param[in,out] ptr A pointer to a memory block to be freed.
    * @see PrtMalloc
    * @see PrtCalloc
    * @see PrtRealloc
    */
    PRT_API void PRT_CALL_CONV PrtFree(void * ptr);

    /** Nondeterministic Boolean choice
    * @returns A nondeterministic Boolean value.
    */
    PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtChoose(void);

#ifdef __cplusplus
}
#endif

#endif
