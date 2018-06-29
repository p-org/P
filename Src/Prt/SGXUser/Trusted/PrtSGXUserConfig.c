#include "PrtSGXUserConfig.h"
#include "Prt.h"

PRT_RECURSIVE_MUTEX PRT_CALL_CONV PrtCreateMutex()
{
    PRT_MUTEX_ATTR attr; // this parameter is not used by SGX.
	PRT_RECURSIVE_MUTEX mutex = malloc(sizeof(PRT_MUTEX_OBJ));
	sgx_thread_mutex_init(mutex, &attr);
	PrtAssert(mutex != NULL, "Unable to create mutex");
	return mutex;
}

void PRT_CALL_CONV PrtDestroyMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	int result = sgx_thread_mutex_destroy(mutex);
	free(mutex);
	PrtAssert(result == 0, "Unable to release mutex");
}

void PRT_CALL_CONV PrtLockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	int result = sgx_thread_mutex_lock(mutex);
	PrtAssert(result == 0, "Unable to wait for mutex");
}

void PRT_CALL_CONV PrtUnlockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	int result = sgx_thread_mutex_unlock(mutex);
	PrtAssert(result == 0, "Unable to unlock mutex");
}

PRT_API PRT_SEMAPHORE PRT_CALL_CONV PrtCreateSemaphore(int initialCount, int maximumCount)
{
    PrtAssert(PRT_FALSE, "Semaphores are not supported by the SGX runtime.");
    return NULL;
}

PRT_API void PRT_CALL_CONV PrtDestroySemaphore(_In_ PRT_SEMAPHORE semaphore)
{
    PrtAssert(PRT_FALSE, "Semaphores are not supported by the SGX runtime.");
}

PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtWaitSemaphore(_In_ PRT_SEMAPHORE semaphore, _In_ long maxWaitTime)
{
    PrtAssert(PRT_FALSE, "Semaphores are not supported by the SGX runtime.");
    return PRT_FALSE;
}

PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtReleaseSemaphore(_In_ PRT_SEMAPHORE semaphore)
{
    PrtAssert(PRT_FALSE, "Semaphores are not supported by the SGX runtime.");
    return PRT_FALSE;
}

PRT_API void PRT_CALL_CONV PrtYieldThread()
{
    // FIXME: what is this supposed to do?
    // sched_yield();
}

void * PRT_CALL_CONV PrtMalloc(_In_ size_t size)
{
	PrtAssert(size > 0, "Size must be positive to avoid platform-specific behavior");
	void *ptr = malloc(size);
	PrtAssert(ptr != NULL, "Memory allocation error");
	return ptr;
}

void * PRT_CALL_CONV PrtCalloc(_In_ size_t nmemb, _In_ size_t size)
{
	PrtAssert(size > 0, "Size must be positive to avoid platform-specific behavior");
	PrtAssert(nmemb > 0, "Size must be positive to avoid platform-specific behavior");

	void *ptr = calloc(nmemb, size);
	PrtAssert(ptr != NULL, "Memory allocation error");
	return ptr;
}

void * PRT_CALL_CONV PrtRealloc(_Inout_ void *ptr, _In_ size_t size)
{
	PrtAssert(ptr != NULL, "Memory must be non-null to avoid platform-specific behavior");
	PrtAssert(size > 0, "Size must be positive to avoid platform-specific behavior");

	ptr = realloc(ptr, size);
	PrtAssert(ptr != NULL, "Memory allocation error");
	return ptr;
}

void PRT_CALL_CONV PrtFree(void *ptr)
{
	free(ptr);
}

PRT_BOOLEAN PRT_CALL_CONV PrtChoose()
{
	unsigned char value;
    sgx_read_rand(&value, sizeof(value));
    return (value & 1) ? PRT_FALSE : PRT_TRUE;
}
