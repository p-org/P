#include "Env.h"
#include "Prt.h"

HANDLE terminationEvent;

void EnvInitialize()
{
	terminationEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
}

void EnvWait()
{
	WaitForSingleObject(terminationEvent, INFINITE);
}

PRT_VALUE *P_FUN_StopProgram_FOREIGN(PRT_MACHINEINST *context)
{
	SetEvent(terminationEvent);
	return NULL;
}
