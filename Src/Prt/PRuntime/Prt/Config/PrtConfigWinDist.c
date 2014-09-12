#include "PrtConfig.h"
#if PRT_PLAT == PRT_PLAT_WINDIST
void PrtSpecialStartup(_In_ void *param)
{
}

void PrtSpecialShutdown(_In_ void *param)
{
}

void PrtAssert(_In_ int condition, _In_opt_z_ PRT_CSTRING message)
{
	if (condition != 0)
	{
		return;
	}
	else if (message == NULL)
	{
		fprintf_s(stderr, "ASSERT");
	}
	else
	{
		fprintf_s(stderr, "ASSERT: %s", message);
	}

	abort();
}

PRT_RECURSIVE_MUTEX PrtCreateMutex()
{
	PRT_RECURSIVE_MUTEX mutex = CreateMutex(NULL, FALSE, NULL);
	PrtAssert(mutex != NULL, "Unable to create mutex");
	return mutex;
}

void PrtReleaseMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	BOOL result = CloseHandle(mutex);
	PrtAssert(result != FALSE, "Unable to release mutex");
}

void PrtLockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	DWORD result = WaitForSingleObject(mutex, INFINITE);
	PrtAssert(result == WAIT_OBJECT_0, "Unable to wait for mutex");
}

void
PrtUnlockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	BOOL result = ReleaseMutex(mutex);
	PrtAssert(result != FALSE, "Unable to unlock mutex");
}

void *PrtMalloc(_In_ size_t size)
{
	void *ptr = malloc(size);
	PrtAssert(ptr != NULL, "Memory allocation error");
	return ptr;
}

void PrtFree(void *ptr)
{
	free(ptr);
}

void *PrtCalloc(_In_ size_t nmemb, _In_ size_t size)
{
	void *ptr = calloc(nmemb, size);
	PrtAssert(ptr != NULL, "Memory allocation error");
	return ptr;
}

void *PrtRealloc(_Inout_ void *ptr, _In_ size_t size)
{
	void *temp = ptr;
	ptr = realloc(ptr, size);
	PrtAssert(size == 0 || ptr != NULL, "Memory allocation error");
	if (ptr == NULL)
		PrtFree(temp);
	return ptr;

}

#endif