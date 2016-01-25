#include "PrtWinUserConfig.h"
#include "Prt.h"

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
	if (value < RAND_MAX / 2)
		return PRT_FALSE;
	else
		return PRT_TRUE;
}