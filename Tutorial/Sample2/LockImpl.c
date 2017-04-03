#include "Sample2.h"

static PRT_UINT32 numLockInstances = 0;
typedef struct LockContext {
	PRT_UINT32 refCount;
	PRT_UINT32 instance;
	HANDLE mutex;
} LockContext;

PRT_UINT64 PRT_FORGN_MKDEF_LockPtr_IMPL(void)
{
	return 0;
}

PRT_UINT64 PRT_FORGN_CLONE_LockPtr_IMPL(PRT_UINT64 frgnVal)
{
	if (frgnVal == 0) return 0;
	LockContext *lockContext = (LockContext *)frgnVal;
	lockContext->refCount++;
	return frgnVal;
}

void PRT_FORGN_FREE_LockPtr_IMPL(PRT_UINT64 frgnVal)
{
	if (frgnVal == 0) return;
	LockContext *lockContext = (LockContext *)frgnVal;
	lockContext->refCount--;
	if (lockContext->refCount == 0)
	{
		CloseHandle(lockContext->mutex);
		PrtFree(lockContext);
	}
}

PRT_BOOLEAN PRT_FORGN_ISEQUAL_LockPtr_IMPL(PRT_UINT64 frgnVal1, PRT_UINT64 frgnVal2)
{
	return frgnVal1 == frgnVal2;
}

PRT_STRING PRT_FORGN_TOSTRING_LockPtr_IMPL(PRT_UINT64 frgnVal)
{
	if (frgnVal == 0) return "";
	LockContext *lockContext = (LockContext *)frgnVal;
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	sprintf_s(str, 100, "Lock : %d", lockContext->instance);
	return str;
}

PRT_UINT32 PRT_FORGN_GETHASHCODE_LockPtr_IMPL(PRT_UINT64 frgnVal)
{
	return (PRT_UINT32)frgnVal;
}

PRT_VALUE *P_FUN_CreateLock_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE *data)
{
	LockContext *lockContext = (LockContext *)PrtMalloc(sizeof(LockContext));
	lockContext->refCount = 1;
	lockContext->mutex = CreateMutex(NULL, FALSE, NULL);
	lockContext->instance = numLockInstances;
	numLockInstances++;

	PrtAssert(lockContext->mutex != NULL, "CreateMutex failed");
	return PrtMkForeignValue((PRT_UINT64)lockContext, &P_GEND_TYPE_LockPtr);
}

PRT_VALUE *P_FUN_AcquireLock_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE *l, PRT_VALUE *client)
{
	LockContext *lockContext = (LockContext *)PrtGetForeignValue(l);
	WaitForSingleObject(lockContext->mutex, INFINITE);
	return NULL;
}

PRT_VALUE *P_FUN_ReleaseLock_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE *l, PRT_VALUE *data)
{
	LockContext *lockContext = (LockContext *)PrtGetForeignValue(l);
	ReleaseMutex(lockContext->mutex);
	return NULL;
}

