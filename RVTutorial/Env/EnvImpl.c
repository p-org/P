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

PRT_VALUE *P_FUN_Continue_FOREIGN(PRT_MACHINEINST *context)
{
	char input[2];
	while (1)
	{
		fputs("Continue (Y/N): ", stdout);
		fflush(stdout);
		fgets(input, 2, stdin);
		fseek(stdin, 0, SEEK_END);
		if (input[0] == 'Y')
		{
			return PrtMkBoolValue(PRT_TRUE);
		}
		else if (input[0] == 'N')
		{
			return PrtMkBoolValue(PRT_FALSE);
		}
		else
		{
			fputs("Illegal input\n", stdout);
			fflush(stdout);
		}
	}
}
