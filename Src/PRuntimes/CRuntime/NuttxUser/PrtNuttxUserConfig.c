#include "PrtNuttxUserConfig.h"
#include "Prt.h"
#include <nuttx/kmalloc.h>

PRT_RECURSIVE_MUTEX PRT_CALL_CONV PrtCreateMutex()
{
    pthread_mutex_t* mutex = malloc(sizeof(pthread_mutex_t));
    pthread_mutexattr_t attr;
    attr.pshared = 0;
#ifdef CONFIG_MUTEX_TYPES
    attr.type = PTHREAD_MUTEX_RECURSIVE;
#endif

    int status = pthread_mutex_init(mutex, &attr);

	PrtAssert(status == 0, "Unable to create mutex");
	return mutex;
}

void PRT_CALL_CONV PrtDestroyMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
    pthread_mutex_destroy(mutex);
	free(mutex);
}

void PRT_CALL_CONV PrtLockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
    pthread_mutex_lock(mutex);
}

void PRT_CALL_CONV PrtUnlockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
    pthread_mutex_unlock(mutex);
}

PRT_API PRT_SEMAPHORE PRT_CALL_CONV PrtCreateSemaphore(int initialCount, int maximumCount)
{
    sem_t* semaphore = malloc(sizeof(sem_t));

    // value of 1 means semaphore is available.
    sem_init(semaphore, /* shared */ 0, /* value*/ initialCount);

    PrtAssert(semaphore != NULL, "Unable to create semaphore");
    return semaphore;
}

PRT_API void PRT_CALL_CONV PrtDestroySemaphore(_In_ PRT_SEMAPHORE semaphore)
{
    sem_destroy(semaphore);
    free(semaphore);
}

PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtWaitSemaphore(_In_ PRT_SEMAPHORE semaphore, _In_ long maxWaitTime)
{
    int rc = 0;
    if (maxWaitTime == -1)
    {
        rc = sem_wait(semaphore);
    }
    else
    {
        struct timespec ts;
        ts.tv_sec = maxWaitTime / 1000; // seconds
        maxWaitTime -= (ts.tv_sec * 1000);
        ts.tv_nsec = maxWaitTime * 1000; // nanoseconds
        rc = sem_timedwait(semaphore, &ts);
    }
    return (rc == OK) ? PRT_TRUE : PRT_FALSE;
}

PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtReleaseSemaphore(_In_ PRT_SEMAPHORE semaphore)
{
    int status = sem_post(semaphore);
    return (status > 0) ? PRT_TRUE : PRT_FALSE;
}

PRT_API void PRT_CALL_CONV PrtYieldThread()
{
    sched_yield();
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
	PRT_UINT32 value = rand();
	if (value < MAX_RAND / 2)
		return PRT_FALSE;
	else
		return PRT_TRUE;
}
