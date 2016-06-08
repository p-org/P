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
    process->schedulingPolicy = PRT_SCHEDULINGPOLICY_TASKNEUTRAL;
    process->schedulerInfo = NULL;
    process->terminating = PRT_FALSE;

    return (PRT_PROCESS *)process;
}

PRT_API PRT_BOOLEAN
PrtWaitForWork(PRT_PROCESS* process)
{
    PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
    PrtLockMutex(privateProcess->processLock);

    PrtAssert(privateProcess->schedulingPolicy == PRT_SCHEDULINGPOLICY_COOPERATIVE, "PrtWaitForWork can only be called when PrtSetSchedulingPolicy has set PRT_SCHEDULINGPOLICY_COOPERATIVE mode");
    PRT_COOPERATIVE_SCHEDULER* info = (PRT_COOPERATIVE_SCHEDULER*)privateProcess->schedulerInfo;

    info->threadsWaiting++;

    PrtUnlockMutex(privateProcess->processLock);

    PrtWaitSemaphore(info->workAvailable, -1);

    PrtLockMutex(privateProcess->processLock);
    info->threadsWaiting--;
    PRT_BOOLEAN terminating = privateProcess->terminating;
    PRT_UINT32 threadsWaiting = info->threadsWaiting;
    PrtUnlockMutex(privateProcess->processLock);

    if (terminating && threadsWaiting == 0)
    {
        PrtReleaseSemaphore(info->allThreadsStopped);
    }
    return terminating;
}

static void PrtDestroyCooperativeScheduler(PRT_COOPERATIVE_SCHEDULER* info)
{
    if (info != NULL)
    {
        PrtDestroySemaphore(info->workAvailable);
        PrtDestroySemaphore(info->allThreadsStopped);
        PrtFree(info);
    }
}

PRT_API void
PrtSetSchedulingPolicy(PRT_PROCESS *process, PRT_SCHEDULINGPOLICY policy)
{
    PRT_PROCESS_PRIV* privateProcess = (PRT_PROCESS_PRIV*)process;
    if (privateProcess->schedulingPolicy != policy)
    {
        privateProcess->schedulingPolicy = policy;
        if (policy == PRT_SCHEDULINGPOLICY_COOPERATIVE)
        {
            PRT_COOPERATIVE_SCHEDULER* info = (PRT_COOPERATIVE_SCHEDULER*)PrtMalloc(sizeof(PRT_COOPERATIVE_SCHEDULER));
            PrtAssert(info != NULL, "Out of memory");

            info->workAvailable = PrtCreateSemaphore(0, 32767);
            info->threadsWaiting = 0;
            info->allThreadsStopped = PrtCreateSemaphore(0, 32767);

            privateProcess->schedulerInfo = info;
        }
        else if (policy == PRT_SCHEDULINGPOLICY_TASKNEUTRAL)
        {
            // this is where we could implement other policies...
            PrtDestroyCooperativeScheduler(privateProcess->schedulerInfo);
            privateProcess->schedulerInfo = NULL;
        }
        else 
        {
            PrtAssert(PRT_FALSE, "PrtSetSchedulingPolicy must set either PRT_SCHEDULINGPOLICY_TASKNEUTRAL or PRT_SCHEDULINGPOLICY_COOPERATIVE");
        }
    }
}

PRT_API void
PrtRunProcess(PRT_PROCESS *process
)
{
    while (1)
    {
        PRT_STEP_RESULT result = PrtStepProcess(process);
        switch (result) {
        case PRT_STEP_TERMINATING:
            return;
        case PRT_STEP_IDLE:
            if (PrtWaitForWork(process) == PRT_TRUE)
            {
                return;
            }
            break;
        case PRT_STEP_MORE:
            PrtYieldThread();
            break;
        }
    }
}

void
PrtStopProcess(
    _Inout_ PRT_PROCESS* process
)
{
    PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV *)process;

    PrtLockMutex(privateProcess->processLock);
	privateProcess->terminating = PRT_TRUE;
	PRT_BOOLEAN waitForThreads = PRT_FALSE;
    PRT_COOPERATIVE_SCHEDULER* info = NULL;

    if (privateProcess->schedulingPolicy == PRT_SCHEDULINGPOLICY_COOPERATIVE)
    {
        info = (PRT_COOPERATIVE_SCHEDULER*)privateProcess->schedulerInfo;
        int count = info->threadsWaiting;
        if (count > 0)
        {
            waitForThreads = PRT_TRUE;
            // unblock all threads so the PrtRunProcess call terminates.
            for (int i = 0; i < count; i++)
            {
                PrtReleaseSemaphore(info->workAvailable);
            }
        }
    }
    PrtUnlockMutex(privateProcess->processLock);

    if (waitForThreads)
    {
        PrtWaitSemaphore(info->allThreadsStopped, -1);
    }
    
    // ok, now we can safely start deleting things...
    for (PRT_UINT32 i = 0; i < privateProcess->numMachines; i++)
    {
        PRT_MACHINEINST *context = privateProcess->machines[i];
        if (context->isModel)
        {
            PrtCleanupModel(context);
        }
        else
        {
            PRT_MACHINEINST_PRIV * privContext = (PRT_MACHINEINST_PRIV *)context;
            PrtCleanupMachine(privContext);
            if (privContext->stateMachineLock != NULL)
            {
                PrtDestroyMutex(privContext->stateMachineLock);
            }
        }
        PrtFree(context);
    }

    PrtFree(privateProcess->machines);
    PrtDestroyCooperativeScheduler(info);
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
    PrtAssert(context != NULL, "Out of memory");

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
