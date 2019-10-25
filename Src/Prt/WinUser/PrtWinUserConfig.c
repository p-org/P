#include "PrtConfig.h"

PRT_RECURSIVE_MUTEX PRT_CALL_CONV PrtCreateMutex()
{
	PRT_RECURSIVE_MUTEX mutex = CreateMutex(NULL, FALSE, NULL);
	PrtAssert(mutex != NULL, "Unable to create mutex");
	return mutex;
}

void PRT_CALL_CONV PrtDestroyMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	BOOL result = CloseHandle(mutex);
	PrtAssert(result != FALSE, "Unable to release mutex");
}

void PRT_CALL_CONV PrtLockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	DWORD result = WaitForSingleObject(mutex, INFINITE);
	PrtAssert(result == WAIT_OBJECT_0, "Unable to wait for mutex");
}

void PRT_CALL_CONV PrtUnlockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	BOOL result = ReleaseMutex(mutex);
	PrtAssert(result != FALSE, "Unable to unlock mutex");
}

PRT_SEMAPHORE PRT_CALL_CONV PrtCreateSemaphore(int initialCount, int maximumCount)
{
	HANDLE h = CreateSemaphore(NULL, initialCount, maximumCount, NULL);
	PrtAssert(h != NULL, "Unable to create semaphore");
	return h;
}

void PRT_CALL_CONV PrtDestroySemaphore(_In_ PRT_SEMAPHORE semaphore)
{
	BOOL result = CloseHandle(semaphore);
	PrtAssert(result != FALSE, "Unable to release semaphore");
}

PRT_BOOLEAN PRT_CALL_CONV PrtWaitSemaphore(_In_ PRT_SEMAPHORE semaphore, _In_ long maxWaitTime)
{
	DWORD status = WaitForSingleObjectEx(semaphore, maxWaitTime == -1 ? INFINITE : maxWaitTime, TRUE);
	return (status == WAIT_OBJECT_0) ? PRT_TRUE : PRT_FALSE;
}

PRT_BOOLEAN PRT_CALL_CONV PrtReleaseSemaphore(_In_ PRT_SEMAPHORE semaphore)
{
	DWORD status = ReleaseSemaphore(semaphore, 1, NULL);
	return (status != 0) ? PRT_TRUE : PRT_FALSE;
}

void PRT_CALL_CONV PrtYieldThread(void)
{
	// windows doesn't need this since it has preemtive multitasking.
}

void* PRT_CALL_CONV PrtMalloc(_In_ size_t size)
{
	void* ptr = malloc(size);
	return ptr;
}

void* PRT_CALL_CONV PrtCalloc(_In_ size_t nmemb, _In_ size_t size)
{
	PrtAssert(size > 0, "Size must be positive to avoid platform-specific behavior");
	PrtAssert(nmemb > 0, "Size must be positive to avoid platform-specific behavior");

	void* ptr = calloc(nmemb, size);
	PrtAssert(ptr != NULL, "Memory allocation error");
	return ptr;
}

void* PRT_CALL_CONV PrtRealloc(_Inout_ void* ptr, _In_ size_t size)
{
	ptr = realloc(ptr, size);
	return ptr;
}

void PRT_CALL_CONV PrtFree(void* ptr)
{
	free(ptr);
}

PRT_BOOLEAN PRT_CALL_CONV PrtChoose()
{
	PRT_UINT32 value = rand();
	if (value < RAND_MAX / 2)
		return PRT_FALSE;
	return PRT_TRUE;
}