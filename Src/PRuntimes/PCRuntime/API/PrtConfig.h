/**
* \file PrtConfig.h
* \brief Defines available runtime configurations.
* The runtime can be configured to compile on different platforms
* and with different concurrency models.

*/
#ifndef PRTCONFIG_H
#define PRTCONFIG_H

#ifdef __cplusplus
extern "C" {
#endif

#ifndef PRT_DEBUG
	/**
	* Expands to PrtAssert if PRT_DEBUG is defined.
	*/
#define PRT_DBG_ASSERT(condition, message)

	/**
	* Marks the beginning of a region of code that may allocate memory,
	* but will deallocate all such memory by the end of the region.
	* @see PRT_DBG_END_MEM_BALANCED_REGION
	*/
#define PRT_DBG_START_MEM_BALANCED_REGION

	/**
	* Marks the end of a region of code that may have allocated memory,
	* but must have deallocate all such memory be now. An exception is thrown
	* if memory was not returned to the starting state.
	* @see PRT_DBG_START_MEM_BALANCED_REGION
	*/
#define PRT_DBG_END_MEM_BALANCED_REGION
#endif

	/** "unsafe" string functions are used safely. Allows for portability of code between operating systems. */
#ifndef _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS
#endif

#ifdef PRT_DEBUG
#ifndef _DEBUG
#define _DEBUG
#endif
#define _CRTDBG_MAP_ALLOC
#include <crtdbg.h>
#endif

#if defined(PRT_PLAT_WINUSER)
#include "PrtWinUserConfig.h"
#elif defined(PRT_PLAT_LINUXUSER)
#include "PrtLinuxUserConfig.h"
#elif defined(PRT_PLAT_NUTTXUSER)
#include "PrtNuttxUserConfig.h"
#elif defined(PRT_PLAT_SGXUSER)
#include "PrtSGXUserConfig.h"
#else
#error An invalid platform was specified (see PrtConfig.h for details)
#endif

/** Function for Assertion will be called whenever an assertion is checked */
	typedef void (PRT_CALL_CONV * PRT_ASSERT_FUN)(PRT_INT32, PRT_CSTRING);

	/* declare the global function pointer for PrtAssert */
	extern PRT_ASSERT_FUN _PrtAssert;

	/** Function for printing string, will be invoked whenever print statement is called from the runtime */
	typedef void (PRT_CALL_CONV * PRT_PRINT_FUN)(PRT_CSTRING);

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
	PRT_API void* PRT_CALL_CONV PrtMalloc(_In_ size_t size);

	/**
	* Calls system-specific implementation of calloc.
	* Fails eagerly if memory cannot be allocated.
	* @param[in] nmemb Number of bytes to allocate per member.
	* @param[in] size Number of bytes to allocate per member.
	* @returns A pointer to a memory location
	* @see PrtFree
	*/
	PRT_API void* PRT_CALL_CONV PrtCalloc(_In_ size_t nmemb, _In_ size_t size);

	/**
	* Calls system-specific implementation of realloc.
	* Fails eagerly if memory cannot be allocated.
	* @param[in,out] ptr A pointer to a memory block to reallocate.
	* @param[in] size Number of bytes to reallocate per member.
	* @returns A pointer to a memory location or NULL if size = 0
	* @see PrtFree
	*/
	void* PRT_CALL_CONV PrtRealloc(_Inout_ void* ptr, _In_ size_t size);

	/**
	* Calls system-specific implementation of free.
	* @param[in,out] ptr A pointer to a memory block to be freed.
	* @see PrtMalloc
	* @see PrtCalloc
	* @see PrtRealloc
	*/
	PRT_API void PRT_CALL_CONV PrtFree(void* ptr);

	/** Nondeterministic Boolean choice
	* @returns A nondeterministic Boolean value.
	*/
	PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtChoose();

#ifdef PRT_NO_ASSERT
#define PrtAssert(c,m)
#else

#define _PrtStringify(x) #x
#define PrtStringify(x) _PrtStringify(x)
#define PrtAssert(c,m) do { int cond = (c); if (!(cond)) { \
	  _PrtAssert(cond, "[" __FILE__ ":" PrtStringify(__LINE__) "] " m); } } while(0)
#endif

#ifdef PRT_DEBUG
	//#define PrtMalloc(size) malloc(size)
	//#define PrtCalloc(nmemb, size) calloc(nmemb, size)
#define PRT_DBG_ASSERT(condition, message) PrtAssert(condition, message)
#define PRT_DBG_START_MEM_BALANCED_REGION { _CrtMemState prtDbgMemStateInitial, prtDbgMemStateFinal, prtDbgMemStateDiff; _CrtMemCheckpoint(&prtDbgMemStateInitial);
#define PRT_DBG_END_MEM_BALANCED_REGION _CrtMemCheckpoint(&prtDbgMemStateFinal); PrtAssert(!_CrtMemDifference(&prtDbgMemStateDiff, &prtDbgMemStateInitial, &prtDbgMemStateFinal), "Memory leak"); }

#endif
#ifdef __cplusplus
}
#endif
#endif
