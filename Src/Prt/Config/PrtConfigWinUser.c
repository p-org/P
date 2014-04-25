#include "PrtConfig.h"

void PrtSpecialStartup(_In_ void *param)
{
}

void PrtSpecialShutdown(_In_ void *param)
{
}

void PrtAssert(_In_ PRT_BOOLEAN condition, _In_ PRT_UINT32 code, _In_opt_z_ PRT_CSTRING message)
{
}

PRT_RECURSIVE_MUTEX PrtCreateMutex()
{
	PRT_RECURSIVE_MUTEX mutex = CreateMutex(NULL, FALSE, NULL);
	PrtAssert(mutex != NULL, 0, "Unable to create mutex");
	return mutex;
}

void PrtReleaseMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	BOOL result = CloseHandle(mutex);
	PrtAssert(result != FALSE, 0, "Unable to release mutex");
}

void PrtLockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	DWORD result = WaitForSingleObject(mutex, INFINITE);
	PrtAssert(result == WAIT_OBJECT_0, result, "Unable to wait for mutex");
}

void PrtUnlockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
	BOOL result = ReleaseMutex(mutex);
	PrtAssert(result != FALSE, 0, "Unable to unlock mutex");
}
