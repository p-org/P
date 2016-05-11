#include "PrtExecution.h"

/*********************************************************************************

Public Functions

*********************************************************************************/
PRT_PROCESS * 
PrtStartProcess(
	_In_ PRT_GUID guid,
	_In_ PRT_PROGRAMDECL *program,
	_In_ PRT_ERROR_FUN errorFun,
	_In_ PRT_LOG_FUN logFun
)
{
	prtNumForeignTypeDecls = program->nForeignTypes;
	prtForeignTypeDecls = program->foreignTypes;

	PRT_PROCESS_PRIV *process;
	process = (PRT_PROCESS_PRIV *)PrtMalloc(sizeof(PRT_PROCESS_PRIV));
	process->guid = guid;
	process->program = program;
	process->errorHandler = errorFun;
	process->logHandler = logFun;
	process->processLock = PrtCreateMutex();
	process->machineCount = 0;
	process->machines = NULL;
	process->numMachines = 0;
    process->workAvailable = PrtCreateSemaphore(0, 32767);
    process->schedulingPolicy = TaskNeutral;
    process->threadsWaiting = 0;
    process->allThreadsStopped = PrtCreateSemaphore(0, 32767);
    process->terminating = PRT_FALSE;
    process->running = PRT_TRUE;

	return (PRT_PROCESS *)process;
}
static PRT_API PRT_BOOLEAN
PrtInternalStepProcess(PRT_PROCESS *process
)
{
    PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
    PrtLockMutex(privateProcess->processLock);

    PRT_BOOLEAN hasMoreWork = PRT_FALSE;

    // fun all state machines belonging to this process.
    for (int i = privateProcess->machineCount - 1; i >= 0; i--)
    {
        PRT_MACHINEINST_PRIV *machine = (PRT_MACHINEINST_PRIV*)privateProcess->machines[i];
        hasMoreWork |= PrtStepStateMachine(machine);
    }
    PrtUnlockMutex(privateProcess->processLock);
    return hasMoreWork;
}


PRT_API PRT_BOOLEAN
PrtStepProcess(PRT_PROCESS *process
)
{
    PRT_BOOLEAN hasMoreWork = PrtInternalStepProcess(process);
    return hasMoreWork;
}

PRT_API void 
PrtWaitForWork(PRT_PROCESS* process)
{
    PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
    PrtLockMutex(privateProcess->processLock);
    privateProcess->threadsWaiting++;
    PrtUnlockMutex(privateProcess->processLock);

    PrtWaitSemaphore(privateProcess->workAvailable, -1);

    PrtLockMutex(privateProcess->processLock);
    privateProcess->threadsWaiting--;
    if (privateProcess->terminating && privateProcess->threadsWaiting == 0)
    {
        PrtReleaseSemaphore(privateProcess->allThreadsStopped);
    }
    PrtUnlockMutex(privateProcess->processLock);
}

PRT_API void
PrtSetSchedulingPolicy(PRT_PROCESS *process, PRT_SCHEDULINGPOLICY policy)
{
    PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
    privateProcess->schedulingPolicy = policy;
}

PRT_API void
PrtRunProcess(PRT_PROCESS *process
)
{
    PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
    privateProcess->running = PRT_TRUE;
    while (privateProcess->running == PRT_TRUE)
    {
        if (PRT_FALSE == PrtStepProcess(process)) 
        {
            PrtWaitForWork(process);
        }
        PrtYieldThread();
    }
}

void 
PrtStopProcess(
	_Inout_ PRT_PROCESS* process
)
{
    PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV *)process;

    PrtLockMutex(privateProcess->processLock);
    privateProcess->running = PRT_FALSE;
    privateProcess->terminating = PRT_TRUE;
    PRT_BOOLEAN waitForThreads = PRT_FALSE;
    int count = privateProcess->threadsWaiting;
    if (count > 0)
    {
        waitForThreads = PRT_TRUE;
        // unblock all threads so the PrtRunProcess call terminates.
        for (int i = 0; i < count; i++)
        {
            PrtReleaseSemaphore(privateProcess->workAvailable);
        }
    }
    PrtUnlockMutex(privateProcess->processLock);

    if (waitForThreads)
    {
        PrtWaitSemaphore(privateProcess->allThreadsStopped, -1);
    }

    PrtLockMutex(privateProcess->processLock);

    // ok, now we can safely start deleting things...
	for (PRT_UINT32 i = 0; i < privateProcess->numMachines; i++)
	{
		PRT_MACHINEINST *context = privateProcess->machines[i];
		if (context->isModel)
			PrtCleanupModel(context);
		else 
			PrtCleanupMachine((PRT_MACHINEINST_PRIV *)context);
		PrtFree(context);
	}

	PrtFree(privateProcess->machines);	
    PrtDestroySemaphore(privateProcess->workAvailable);
    PrtDestroySemaphore(privateProcess->allThreadsStopped);
    PrtUnlockMutex(privateProcess->processLock);
    // must come last.
	PrtDestroyMutex(privateProcess->processLock);
	PrtFree(process);
}

PRT_MACHINEINST *
PrtMkMachine(
	_Inout_  PRT_PROCESS			*process,
	_In_  PRT_UINT32				instanceOf,
	_In_  PRT_VALUE					*payload
)
{
	return (PRT_MACHINEINST *)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)process, instanceOf, payload);
}

PRT_MACHINEINST *
PrtMkModel(
	_Inout_  PRT_PROCESS			*process,
	_In_  PRT_UINT32				instanceOf,
	_In_  PRT_VALUE					*payload
)
{
	PRT_MACHINEINST *context;
	PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV *)process;

	PrtLockMutex(privateProcess->processLock);

	context = (PRT_MACHINEINST *)PrtMalloc(sizeof(PRT_MACHINEINST));

	PRT_UINT32 numMachines = privateProcess->numMachines;
	PRT_UINT32 machineCount = privateProcess->machineCount;
	PRT_MACHINEINST **machines = privateProcess->machines;
    if (machineCount == 0)
    {
        machines = (PRT_MACHINEINST **)PrtCalloc(1, sizeof(PRT_MACHINEINST *));
        privateProcess->machines = machines;
        privateProcess->machineCount = 1;
    }
    else if (machineCount == numMachines) 
    {
		PRT_MACHINEINST **newMachines = (PRT_MACHINEINST **)PrtCalloc(2 * machineCount, sizeof(PRT_MACHINEINST *));
		for (PRT_UINT32 i = 0; i < machineCount; i++)
		{
			newMachines[i] = machines[i];
		}
		machines = newMachines;
		privateProcess->machines = newMachines;
		privateProcess->machineCount = 2 * machineCount;
	}
	machines[numMachines] = (PRT_MACHINEINST *)context;
	privateProcess->numMachines++;

	context->process = process;
	context->instanceOf = instanceOf;
	PRT_MACHINEID id;
	id.machineId = privateProcess->numMachines; // index begins with 1 since 0 is reserved
	id.processId = process->guid;
	context->id = PrtMkMachineValue(id);
	context->extContext = NULL;
	context->isModel = PRT_TRUE;
	process->program->modelImpls[context->instanceOf].ctorFun(context, payload);

	PrtUnlockMutex(privateProcess->processLock);

	return context;
}

PRT_MACHINEINST * 
PrtGetMachine(
	_In_ PRT_PROCESS *process,
	_In_ PRT_VALUE *id
)
{
	PRT_MACHINEID *machineId;
	PRT_PROCESS_PRIV *privateProcess;
	PrtAssert(id->discriminator == PRT_VALKIND_MID, "id is not legal PRT_MACHINEID");
	machineId = id->valueUnion.mid;
	//Comented out by Ankush Desai.
	//PrtAssert(PrtAreGuidsEqual(process->guid, machineId->processId), "id does not belong to process");
	privateProcess = (PRT_PROCESS_PRIV *)process;
	PrtAssert((0 < machineId->machineId) && (machineId->machineId <= privateProcess->numMachines), "id out of bounds");
	return privateProcess->machines[machineId->machineId - 1];
}

void
PrtSend(
	_Inout_ PRT_MACHINEINST			*context,
	_In_ PRT_VALUE					*event,
	_In_ PRT_VALUE					*payload,
	_In_ PRT_BOOLEAN				doTransfer
)
{
	if (context->isModel)
	{
		context->process->program->modelImpls[context->instanceOf].sendFun(context, event, payload, doTransfer);
		return;
	}
	PrtSendPrivate((PRT_MACHINEINST_PRIV *)context, event, payload, doTransfer);
}
