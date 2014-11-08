#include "program.h"

void P_DTOR_Ghost_IMPL(PRT_MACHINEINST *context) { }

void P_DTOR_Real_IMPL(PRT_MACHINEINST *context) { }

void P_CTOR_Ghost_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value) { }

void P_CTOR_Real_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value) { }

void ErrorHandler(PRT_STATUS status, void *ptr) { }

void Log(PRT_STEP step, void *vcontext)
{
	static FILE *logfile = NULL;
	PRT_MACHINEINST_PRIV *c = (PRT_MACHINEINST_PRIV*)vcontext;
	if (logfile == NULL)
	{
	  char fileName[100] = "PRT_PPROCESS_LOG_";
	  char processId[100];
	  _itoa(c->id->valueUnion.mid->processId.data1, processId, 10);
	  strcat_s(fileName, 100, processId);
	  strcat_s(fileName, 100, ".txt");
	  logfile = fopen(fileName, "w+");
	}

	PRT_STRING log = NULL;
	PRT_UINT32 logLength = 0;
	PRT_UINT32 printLength = 0;
	PrtWinUserPrintStep(step, c, &log, &logLength, &printLength);
	PrtLockMutex(((PRT_PROCESS_PRIV*)c->process)->processLock);
	fputs(log, logfile);
	fflush(logfile);
	PrtFree(log);
	PrtUnlockMutex(((PRT_PROCESS_PRIV*)c->process)->processLock);
}

void main()
{
        PRT_PROCESS *process;
	PRT_GUID processGuid;
	processGuid.data1 = 1;
	processGuid.data2 = 0;
	processGuid.data3 = 0;
	processGuid.data4 = 0;
	process = PrtStartProcess(processGuid, &P_GEND_PROGRAM, ErrorHandler, Log);
	PrtMkMachine(process, _P_MACHINE_MAIN, PrtMkNullValue());
	PrtStopProcess(process);
}
