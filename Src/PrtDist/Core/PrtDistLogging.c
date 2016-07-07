#include "PrtDist.h"

static FILE *logfile = NULL;
PRT_CHAR logfileName[MAX_LOG_SIZE];


void PrtOpenLogFile(PRT_CHAR* logDirectory)
{
    logfileName[0] = '\0';
    size_t len = 0;
    if (logDirectory != NULL)
    {
        len = strlen(logDirectory);
        if (len > 0)
        {
            strcpy(logfileName, logDirectory);
            if (logDirectory[len - 1] != '\\' && logDirectory[len - 1] != '/')
            {
                strcat_s(logfileName, MAX_LOG_SIZE, "\\");
                len++;
            }
        }
    }
    strcat_s(logfileName, MAX_LOG_SIZE, "PRT_CONTAINER_LOG_");
    if (ContainerProcess != NULL)
    {
        PRT_CHAR processId[MAX_LOG_SIZE];
        _itoa(ContainerProcess->guid.data1, processId, 10);
        strcat_s(logfileName, MAX_LOG_SIZE, processId);
    }
    strcat_s(logfileName, MAX_LOG_SIZE, ".txt");
    logfile = fopen(logfileName, "a+");
}

void PrtCloseLogFile()
{
    if (logfile != NULL)
    {
        fclose(logfile);
        logfile = NULL;
    }
}

void
PrtDistSMExceptionHandler(
__in PRT_STATUS exception,
__in PRT_MACHINEINST* vcontext
)
{

	PRT_MACHINEINST *context = (PRT_MACHINEINST*)vcontext;
	PRT_STRING MachineName = context->process->program->machines[context->instanceOf].name;
	PRT_UINT32 MachineId = context->id->valueUnion.mid->machineId;


	PRT_MACHINEINST_PRIV *c = (PRT_MACHINEINST_PRIV*)vcontext;

	PrtLockMutex(((PRT_PROCESS_PRIV*)c->process)->processLock);

	PRT_CHAR log[MAX_LOG_SIZE];

	switch (exception)
	{
	case PRT_STATUS_EVENT_UNHANDLED:
		sprintf_s(log,
			MAX_LOG_SIZE,
			"<EXCEPTION> Machine %s(%d) : Unhandled Event Exception\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_EVENT_OVERFLOW:
		sprintf_s(log,
			MAX_LOG_SIZE,
			"<EXCEPTION> Machine %s(%d) : MaxInstance of Event Exceeded Exception\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_QUEUE_OVERFLOW:
		sprintf_s(log,
			MAX_LOG_SIZE,
			"<EXCEPTION> Queue Size Exceeded Max Limits in Machine %s(%d)\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_ILLEGAL_SEND:
		sprintf_s(log,
			MAX_LOG_SIZE,
			"<EXCEPTION> Machine %s(%d) : Illegal use of send for sending message across process (source and target machines are in different process) ", 
			MachineName,
			MachineId);
		break;
	default:
		sprintf_s(log,
			MAX_LOG_SIZE,
			"<EXCEPTION> Machine %s(%d) : Unknown Exception\n",
			MachineName,
			MachineId);
		break;
	}

    PrtDistLog(log);

	PrtUnlockMutex(((PRT_PROCESS_PRIV*)c->process)->processLock);

#ifdef PRT_DEBUG
	int msgboxID = MessageBoxEx(
		NULL,
		log,
        logfileName,
		MB_OK,
		LANG_NEUTRAL
		);

	switch (msgboxID)
	{
	case IDOK:
		exit(0);
	default:
		exit(-1);
	}
#endif
	exit(-1);

}

void PrtDistSMLogHandler(PRT_STEP step, void *vcontext)
{
	PRT_MACHINEINST_PRIV *c = (PRT_MACHINEINST_PRIV*)vcontext;
	PrtLockMutex(((PRT_PROCESS_PRIV*)ContainerProcess)->processLock);

	if (logfile == NULL)
	{
        PrtOpenLogFile(NULL);
	}

    if (logfile != NULL)
    {
        PRT_STRING buffer = NULL;
        PRT_STRING log = NULL;
        if (step == PRT_STEP_COUNT) //special logging
        {
            log = (PRT_STRING)vcontext;
            fputs("<PRTDIST_LOG>  ", logfile);
        }
        else
        {
            log = buffer = PrtToStringStep(step, vcontext);
        }
        fputs(log, logfile);
        if (log[strlen(log) - 1] != '\n') {
            fputs("\n", logfile);
        }
        fflush(logfile);

        if (buffer != NULL)
        {
            PrtFree(buffer);
        }
    }

	PrtUnlockMutex(((PRT_PROCESS_PRIV*)ContainerProcess)->processLock);
}

void PrtDistLog(PRT_STRING log)
{
	PrtDistSMLogHandler(PRT_STEP_COUNT, log);
}