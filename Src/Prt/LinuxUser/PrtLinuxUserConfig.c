#include "PrtLinuxUserConfig.h"
#include "Prt.h"

PRT_RECURSIVE_MUTEX PRT_CALL_CONV PrtCreateMutex()
{
  pthread_mutexattr_t attr;
  pthread_mutexattr_init(&attr);
  pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
  PRT_RECURSIVE_MUTEX mutex = malloc(sizeof(pthread_mutex_t));
  pthread_mutex_init(mutex, &attr);
  PrtAssert(mutex != NULL, "Unable to create mutex");
  return mutex;
}

void PRT_CALL_CONV PrtDestroyMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
  int result = pthread_mutex_destroy(mutex);
  free(mutex);
  PrtAssert(result == 0, "Unable to release mutex");
}

void PRT_CALL_CONV PrtLockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
  int result = pthread_mutex_lock(mutex);
  PrtAssert(result == 0, "Unable to wait for mutex");
}

void PRT_CALL_CONV PrtUnlockMutex(_In_ PRT_RECURSIVE_MUTEX mutex)
{
  int result = pthread_mutex_unlock(mutex);
  PrtAssert(result == 0, "Unable to unlock mutex");
}

PRT_API PRT_SEMAPHORE PRT_CALL_CONV PrtCreateSemaphore(int initialCount, int maximumCount)
{
#ifdef __APPLE__
  dispatch_semaphore_t* semaphore = malloc(sizeof(dispatch_semaphore_t));
#else
    sem_t* semaphore = malloc(sizeof(sem_t));
#endif

    // value of 1 means semaphore is available.
#ifdef __APPLE__
    *semaphore = dispatch_semaphore_create(initialCount);
#else
    sem_init(semaphore, /* shared */ 0, /* value*/ initialCount);
#endif

    PrtAssert(semaphore != NULL, "Unable to create semaphore");
    return semaphore;
}

PRT_API void PRT_CALL_CONV PrtDestroySemaphore(_In_ PRT_SEMAPHORE semaphore)
{
#ifdef __APPLE__
  dispatch_release(*semaphore);
  free(semaphore);
#else
    sem_destroy(semaphore);
    free(semaphore);
#endif
}

PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtWaitSemaphore(_In_ PRT_SEMAPHORE semaphore, _In_ long maxWaitTime)
{
    int rc = 0;
    if (maxWaitTime == -1)
    {
#ifdef __APPLE__
      rc = dispatch_semaphore_wait(*semaphore, DISPATCH_TIME_FOREVER);
#else
        rc = sem_wait(semaphore);
#endif
    }
    else
    {
#ifdef __APPLE__
      rc = dispatch_semaphore_wait(*semaphore, dispatch_time(DISPATCH_TIME_NOW, maxWaitTime));
#else
        struct timespec ts;
        ts.tv_sec = maxWaitTime / 1000; // seconds
        maxWaitTime -= (ts.tv_sec * 1000);
        ts.tv_nsec = maxWaitTime * 1000; // nanoseconds
        rc = sem_timedwait(semaphore, &ts);
#endif
    }
#ifdef __APPLE__
    return (rc == 0) ? PRT_TRUE : PRT_FALSE;
#else
    return (rc > 0) ? PRT_TRUE : PRT_FALSE;
#endif
}

PRT_API PRT_BOOLEAN PRT_CALL_CONV PrtReleaseSemaphore(_In_ PRT_SEMAPHORE semaphore)
{
#ifdef __APPLE__
  int status = dispatch_semaphore_signal(*semaphore);
    return (status == 0) ? PRT_TRUE : PRT_FALSE;
#else
    int status = sem_post(semaphore);
    return (status > 0) ? PRT_TRUE : PRT_FALSE;
#endif
}

PRT_API void PRT_CALL_CONV PrtYieldThread()
{
    sched_yield();
}

void * PRT_CALL_CONV PrtMalloc(_In_ size_t size)
{
  void *ptr = malloc(size);
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
  ptr = realloc(ptr, size);
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