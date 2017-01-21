#include "PrtExecution.h"

void PrtSetForeignTypes(
	_In_ PRT_PROGRAMDECL *program)
{
	prtNumForeignTypeDecls = program->nForeignTypes;
	prtForeignTypeDecls = program->foreignTypes;
}

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
	PrtSetForeignTypes(program);

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
		PRT_MACHINEINST_PRIV * privContext = (PRT_MACHINEINST_PRIV *)context;
		PrtCleanupMachine(privContext);
		if (privContext->stateMachineLock != NULL)
		{
			PrtDestroyMutex(privContext->stateMachineLock);
		}
		PrtFree(context);
	}

	PrtFree(privateProcess->machines);
	PrtDestroyCooperativeScheduler(info);
	PrtDestroyMutex(privateProcess->processLock);
	PrtFree(process);
}

FORCEINLINE
PRT_FUNDECL *
GetFunDeclHelper(_In_ PRT_PROCESS	*process, _In_ PRT_UINT32 instanceOf, _In_ PRT_UINT32 funIndex)
{
	PRT_UINT32 isMachineLocal = funIndex % 2;
	PRT_UINT32 arrayIndex = funIndex / 2;
	if (isMachineLocal)
	{
		return &process->program->machines[instanceOf]->funs[arrayIndex];
	}
	else
	{
		return process->program->globalFuns[arrayIndex];
	}
}


PRT_MACHINEINST *
PrtMkInterfaceOrMachine(
	_In_ PRT_MACHINEINST*		creator,
    _In_ PRT_UINT32				IorM,
	_In_ PRT_UINT32				numArgs,
	...
)
{
	PRT_MACHINEINST_PRIV* context = (PRT_MACHINEINST_PRIV*)creator;
	PRT_VALUE *payload = NULL;
	PRT_UINT32 renamedName = context->process->program->linkMap[context->renamedName][IorM];
	PRT_UINT32 instanceOf = ((PRT_PROCESS_PRIV *)context->process)->program->renameMap[renamedName];

	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else 
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
#if __PX4_NUTTX
			PRT_FUN_PARAM_STATUS argStatus = (PRT_FUN_PARAM_STATUS)va_arg(argp, int);
#else
			PRT_FUN_PARAM_STATUS argStatus = va_arg(argp, PRT_FUN_PARAM_STATUS);
#endif
			PRT_VALUE *arg;
			PRT_VALUE **argPtr;
			switch (argStatus)
			{
			case PRT_FUN_PARAM_CLONE:
				arg = va_arg(argp, PRT_VALUE *);
				args[i] = PrtCloneValue(arg);
				break;
			case PRT_FUN_PARAM_SWAP:
				PrtAssert(PRT_FALSE, "Illegal parameter type in PrtRaise");
				break;
			case PRT_FUN_PARAM_MOVE:
				argPtr = va_arg(argp, PRT_VALUE **);
				args[i] = *argPtr;
				*argPtr = NULL;
				break;
			}
		}
		va_end(argp);
		payload = args[0];

		if (numArgs > 1)
		{
			PRT_MACHINEDECL *machineDecl = context->process->program->machines[instanceOf];
			PRT_UINT32 entryFunIndex = machineDecl->states[machineDecl->initStateIndex].entryFunIndex;
			PRT_TYPE *payloadType = GetFunDeclHelper(context->process, instanceOf, entryFunIndex)->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
    return (PRT_MACHINEINST*)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)context->process, renamedName, instanceOf, payload);
}

PRT_MACHINEINST *
PrtMkMachine(
	_Inout_  PRT_PROCESS		*process,
	_In_ PRT_UINT32				renamedMachine,
	_In_ PRT_UINT32				numArgs,
	...
)
{
	PRT_VALUE *payload = NULL;
	PRT_UINT32 instanceOf = ((PRT_PROCESS_PRIV *)process)->program->renameMap[renamedMachine];

	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
#if __PX4_NUTTX
			PRT_FUN_PARAM_STATUS argStatus = (PRT_FUN_PARAM_STATUS)va_arg(argp, int);
#else
			PRT_FUN_PARAM_STATUS argStatus = va_arg(argp, PRT_FUN_PARAM_STATUS);
#endif
			PRT_VALUE *arg;
			PRT_VALUE **argPtr;
			switch (argStatus)
			{
			case PRT_FUN_PARAM_CLONE:
				arg = va_arg(argp, PRT_VALUE *);
				args[i] = PrtCloneValue(arg);
				break;
			case PRT_FUN_PARAM_SWAP:
				PrtAssert(PRT_FALSE, "Illegal parameter type in PrtRaise");
				break;
			case PRT_FUN_PARAM_MOVE:
				argPtr = va_arg(argp, PRT_VALUE **);
				args[i] = *argPtr;
				*argPtr = NULL;
				break;
			}
		}
		va_end(argp);
		payload = args[0];

		if (numArgs > 1)
		{
			PRT_MACHINEDECL *machineDecl = process->program->machines[instanceOf];
			PRT_UINT32 entryFunIndex = machineDecl->states[machineDecl->initStateIndex].entryFunIndex;
			PRT_TYPE *payloadType = GetFunDeclHelper(process, instanceOf, entryFunIndex)->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PRT_MACHINEINST* result = (PRT_MACHINEINST*)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)process, renamedMachine, instanceOf, payload);

	// free the payload since we cloned it here, and PrtMkMachinePrivate also clones it.
	PrtFreeValue(payload);
}

PRT_MACHINEINST *
PrtGetMachine(
    _In_ PRT_PROCESS *process,
    _In_ PRT_VALUE *id
)
{
    PRT_MACHINEID *machineId;
    PRT_PROCESS_PRIV *privateProcess;
    PrtAssert(id->discriminator == PRT_VALUE_KIND_MID, "id is not legal PRT_MACHINEID");
    machineId = id->valueUnion.mid;
    //Comented out by Ankush Desai.
    //PrtAssert(PrtAreGuidsEqual(process->guid, machineId->processId), "id does not belong to process");
    privateProcess = (PRT_PROCESS_PRIV *)process;
    PrtAssert((0 < machineId->machineId) && (machineId->machineId <= privateProcess->numMachines), "id out of bounds");
    return privateProcess->machines[machineId->machineId - 1];
}

void PRT_CALL_CONV PrtGetMachineState(_In_ PRT_MACHINEINST *context, _Inout_ PRT_MACHINESTATE* state)
{
	PRT_MACHINEINST_PRIV *priv = (PRT_MACHINEINST_PRIV*)context;
	state->machineId = context->id->valueUnion.mid->machineId;
	state->machineName = (PRT_STRING)context->process->program->machines[context->instanceOf]->name;
	state->stateId = priv->currentState;
	state->stateName = PrtGetCurrentStateDecl(priv)->name;
}

void
PrtSend(
	_Inout_ PRT_MACHINESTATE 		*senderState,
    _Inout_ PRT_MACHINEINST			*receiver,
    _In_ PRT_VALUE					*event,
	_In_ PRT_UINT32					numArgs,
	...
)
{
	PRT_VALUE *payload = NULL;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
#if __PX4_NUTTX
			PRT_FUN_PARAM_STATUS argStatus = (PRT_FUN_PARAM_STATUS)va_arg(argp, int);
#else
			PRT_FUN_PARAM_STATUS argStatus = va_arg(argp, PRT_FUN_PARAM_STATUS);
#endif
			PRT_VALUE *arg;
			PRT_VALUE **argPtr;
			switch (argStatus)
			{
			case PRT_FUN_PARAM_CLONE:
				arg = va_arg(argp, PRT_VALUE *);
				args[i] = PrtCloneValue(arg);
				break;
			case PRT_FUN_PARAM_SWAP:
				PrtAssert(PRT_FALSE, "Illegal parameter type in PrtRaise");
				break;
			case PRT_FUN_PARAM_MOVE:
				argPtr = va_arg(argp, PRT_VALUE **);
				args[i] = *argPtr;
				*argPtr = NULL;
				break;
			}
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_TYPE *payloadType = PrtGetPayloadType((PRT_MACHINEINST_PRIV *)receiver, event);
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
    PrtSendPrivate(senderState, (PRT_MACHINEINST_PRIV *)receiver, event, payload);
}


void 
PRT_CALL_CONV PrtSendInternal(
	_Inout_ PRT_MACHINEINST *sender,
	_Inout_ PRT_MACHINEINST *receiver,
	_In_ PRT_VALUE *event,
	_In_ PRT_UINT32	numArgs,
	...
)
{
	PRT_MACHINESTATE senderState;
	PrtGetMachineState(sender, &senderState);

	PRT_VALUE *payload = NULL;
	if (numArgs == 0)
	{
		payload = PrtMkNullValue();
	}
	else
	{
		PRT_VALUE **args = PrtCalloc(numArgs, sizeof(PRT_VALUE*));
		va_list argp;
		va_start(argp, numArgs);
		for (PRT_UINT32 i = 0; i < numArgs; i++)
		{
#if __PX4_NUTTX
			PRT_FUN_PARAM_STATUS argStatus = (PRT_FUN_PARAM_STATUS)va_arg(argp, int);
#else
			PRT_FUN_PARAM_STATUS argStatus = va_arg(argp, PRT_FUN_PARAM_STATUS);
#endif
			PRT_VALUE *arg;
			PRT_VALUE **argPtr;
			switch (argStatus)
			{
			case PRT_FUN_PARAM_CLONE:
				arg = va_arg(argp, PRT_VALUE *);
				args[i] = PrtCloneValue(arg);
				break;
			case PRT_FUN_PARAM_SWAP:
				PrtAssert(PRT_FALSE, "Illegal parameter type in PrtRaise");
				break;
			case PRT_FUN_PARAM_MOVE:
				argPtr = va_arg(argp, PRT_VALUE **);
				args[i] = *argPtr;
				*argPtr = NULL;
				break;
			}
		}
		va_end(argp);
		payload = args[0];
		if (numArgs > 1)
		{
			PRT_TYPE *payloadType = PrtGetPayloadType((PRT_MACHINEINST_PRIV *)receiver, event);
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}

	PrtSendPrivate(&senderState, (PRT_MACHINEINST_PRIV *)receiver, event, payload);
}