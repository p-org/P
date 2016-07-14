#include "program.h"

void ErrorHandler(PRT_STATUS status, PRT_MACHINEINST *ptr)
{
    if (status == PRT_STATUS_ASSERT)
    {
        fprintf_s(stdout, "exiting with PRT_STATUS_ASSERT (assertion failure)\n");
        exit(1);
    }
    else if (status == PRT_STATUS_EVENT_OVERFLOW)
    {
        fprintf_s(stdout, "exiting with PRT_STATUS_EVENT_OVERFLOW\n");
        exit(1);
    }
    else if (status == PRT_STATUS_EVENT_UNHANDLED)
    {
        fprintf_s(stdout, "exiting with PRT_STATUS_EVENT_UNHANDLED\n");
        exit(1);
    }
    else if (status == PRT_STATUS_QUEUE_OVERFLOW)
    {
        fprintf_s(stdout, "exiting with PRT_STATUS_QUEUE_OVERFLOW \n");
        exit(1);
    }
    else if (status == PRT_STATUS_ILLEGAL_SEND)
    {
        fprintf_s(stdout, "exiting with PRT_STATUS_ILLEGAL_SEND \n");
        exit(1);
    }
    else
    {
        fprintf_s(stdout, "unexpected PRT_STATUS in ErrorHandler: %d\n", status);
        exit(2);
    }


}



HANDLE threadsTerminated;
long threadsRunning = 0;
static PRT_BOOLEAN cooperative = PRT_FALSE;
static int threads = 1;

// todo: make tester useful for performance testing also, not finished yet...
static PRT_BOOLEAN perf = PRT_FALSE;
static long steps = 0;
static long startTime = 0;
static long perfEndTime = 0;
static const char* parg = NULL;

void Log(PRT_STEP step, PRT_MACHINEINST *sender, PRT_MACHINEINST *receiver, PRT_VALUE* event, PRT_VALUE* payload)
{ 
	PrtPrintStep(step, sender, receiver, event, payload);
}

static PRT_BOOLEAN ParseCommandLine(int argc, char *argv[])
{
    for (int i = 1; i < argc; i++)
    {
        char* arg = argv[i];
        if (arg[0] == '-' || arg[0] == '/')
        {
            if (_stricmp(arg + 1, "cooperative") == 0)
            {
                cooperative = PRT_TRUE;
            }
            else if (_stricmp(arg + 1, "threads") == 0)
            {
                if (i + 1 < argc)
                {
                    threads = atoi(argv[++i]);
                    if (threads <= 0)
                    {
                        threads = 1;
                    }
                }
            }
			else if (_stricmp(arg + 1, "perf") == 0)
			{
				if (i + 1 < argc)
				{
					perf = PRT_TRUE;
					perfEndTime = atoi(argv[++i]);
					if (perfEndTime <= 0)
					{
						perfEndTime = 1;
					}
				}
			}
			else if (_stricmp(arg + 1, "arg") == 0)
			{
				if (i + 1 < argc)
				{
					parg = argv[++i];
				}
			}
            else if (_stricmp(arg + 1, "h") == 0 || _stricmp(arg + 1, "help") == 0 || _stricmp(arg + 1, "?") == 0)
            {
                return PRT_FALSE;
            }
            else
            {
                printf("Unknown argument: '%s'\n", arg);
                return PRT_FALSE;
            }
        }
        else 
        {
            printf("Unknown argument: '%s'\n", arg);
            return PRT_FALSE;
        }
    }
    return PRT_TRUE;
}

static void PrintUsage(void)
{
    printf("Usage: Tester [options]\n");
    printf("This program tests the compiled state machine in program.c and program.h\n");
    printf("Options:\n");
    printf("   -cooperative     run state machine with the cooperative scheduler\n");
	printf("   -threads [n]     run P using multiple threads");
	printf("   -perf [n]        run performance test that outputs #steps every 10 seconds, terminating after n seconds");
	printf("   -arg [x]         pass argument 'x' to P main machine");
}

static void RunPerfTest()
{

}

void PRT_CALL_CONV  MyAssert(PRT_INT32 condition, PRT_CSTRING message)
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
	exit(1);
}


static void RunToIdle(LPVOID process)
{
    // In the tester we run the state machines until there is no more work to do then we exit
    // instead of blocking indefinitely.  This is then equivalent of the non-cooperative case
    // where we PrtRunStateMachine once (inside PrtMkMachine).  So we do NOT call PrtWaitForWork.
    // PrtWaitForWork(process);
    InterlockedIncrement(&threadsRunning);
    PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
    while (privateProcess->terminating == PRT_FALSE)
    {
        if (PRT_STEP_IDLE == PrtStepProcess(process))
        {
            break;
        }
    }
    long count = InterlockedDecrement(&threadsRunning);
    if (count == 0)
    {
        SetEvent(threadsTerminated);
    }
}

int main(int argc, char *argv[])
{
    if (!ParseCommandLine(argc, argv))
    {
        PrintUsage();
        return 1;
    }

	PRT_DBG_START_MEM_BALANCED_REGION
	{
		PRT_PROCESS *process;
		PRT_GUID processGuid;
		PRT_VALUE *payload;
		processGuid.data1 = 1;
		processGuid.data2 = 0;
		processGuid.data3 = 0;
		processGuid.data4 = 0;
		process = PrtStartProcess(processGuid, &P_GEND_PROGRAM, ErrorHandler, Log);
        if (cooperative)
        {
            PrtSetSchedulingPolicy(process, PRT_SCHEDULINGPOLICY_COOPERATIVE);
        }
		if (parg == NULL)
		{
			payload = PrtMkNullValue();
		}
		else
		{
			int i = atoi(parg);
			payload = PrtMkIntValue(i);
		}

		PrtUpdateAssertFn(MyAssert);

		PrtMkMachine(process, _P_MACHINE_MAIN, payload);

        if (cooperative)
        {
            // test some multithreading across state machines.
            threadsTerminated = CreateEvent(NULL, TRUE, FALSE, NULL);
            for (int i = 0; i < threads; i++)
            {
                DWORD threadId;
                CreateThread(NULL, 16000, (LPTHREAD_START_ROUTINE)RunToIdle, process, 0, &threadId);
            }
            WaitForSingleObject(threadsTerminated, INFINITE);
        }

		PrtFreeValue(payload);
		PrtStopProcess(process);
	}
	PRT_DBG_END_MEM_BALANCED_REGION

	//_CrtSetReportMode(_CRT_ERROR, _CRTDBG_MODE_DEBUG);
	//_CrtDumpMemoryLeaks();
}
