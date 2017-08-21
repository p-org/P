#include "PrtExecution.h"

PRT_PROGRAMDECL *program;

/*********************************************************************************

Public Functions

*********************************************************************************/

void PrtTraverseEventset(PRT_EVENTSETDECL *evset, PRT_BOOLEAN doInstall)
{
    if (doInstall)
    {
        if (evset->packedEvents == NULL)
        {
            PRT_UINT32 unitSize = sizeof(PRT_UINT32) * 8;
            PRT_UINT32 packedArraySize = program->nEvents / unitSize + 1;
            evset->packedEvents = (PRT_UINT32 *)PrtCalloc(packedArraySize, sizeof(PRT_UINT32));
            for (PRT_UINT32 i = 0; i < evset->nEvents; i++)
            {
                PRT_UINT32 eventIndex = evset->events[i]->value.valueUnion.ev;
                PRT_UINT32 arrayOffset = eventIndex / unitSize;
                PRT_UINT32 eventMask = 1 << (eventIndex % unitSize);
                evset->packedEvents[arrayOffset] |= eventMask;
            }
        }
    }
    else
    {
        if (evset->packedEvents != NULL)
        {
            PrtFree(evset->packedEvents);
            evset->packedEvents = NULL;
        }
    }
}

void PrtTraverseFun(PRT_FUNDECL *fun, PRT_BOOLEAN doInstall)
{
    for (PRT_UINT32 i = 0; i < fun->nReceives; i++)
    {
        PrtTraverseEventset(fun->receives[i].caseSet, doInstall);
    }
}

void PrtTraverseState(PRT_STATEDECL *state, PRT_BOOLEAN doInstall)
{
    PrtTraverseEventset(state->defersSet, doInstall);
    PrtTraverseEventset(state->doSet, doInstall);
    PrtTraverseEventset(state->transSet, doInstall);

    if (state->entryFun != NULL)
    {
        PrtTraverseFun(state->entryFun, doInstall);
    }
    if (state->exitFun != NULL)
    {
        PrtTraverseFun(state->exitFun, doInstall);
    }
    for (PRT_UINT32 i = 0; i < state->nDos; i++)
    {
        if (state->dos[i].doFun != NULL)
        {
            PrtTraverseFun(state->dos[i].doFun, doInstall);
        }
    }
    for (PRT_UINT32 i = 0; i < state->nTransitions; i++)
    {
        if (state->transitions[i].transFun != NULL)
        {
            PrtTraverseFun(state->transitions[i].transFun, doInstall);
        }
    }
}

void PrtTraverseMachine(PRT_MACHINEDECL *machine, PRT_BOOLEAN doInstall)
{
    for (PRT_UINT32 i = 0; i < machine->nStates; i++)
    {
        PrtTraverseState(&machine->states[i], doInstall);
    }
}

void PrtInstallProgram(_In_ PRT_PROGRAMDECL *p)
{
    PrtAssert(p != NULL && program == NULL, "p and program must be non-NULL");
    program = p;
    for (PRT_UINT32 i = 0; i < p->nEvents; i++)
	{
		p->events[i]->value.valueUnion.ev = i;
	}
	for (PRT_UINT32 i = 0; i < p->nMachines; i++)
	{
		p->machines[i]->declIndex = i;
        PrtTraverseMachine(p->machines[i], PRT_TRUE);
	}
    for (PRT_UINT32 i = 0; i < p->nGlobalFuns; i++)
    {
        PrtTraverseFun(p->globalFuns[i], PRT_TRUE);
    }
	for (PRT_UINT32 i = 0; i < p->nForeignTypes; i++)
	{
		p->foreignTypes[i]->declIndex = i;
	}
}

void PrtUninstallProgram()
{
    PrtAssert(program != NULL, "program must be non-NULL");
    for (PRT_UINT32 i = 0; i < program->nEvents; i++)
    {
        program->events[i]->value.valueUnion.ev = 0;
    }
    for (PRT_UINT32 i = 0; i < program->nMachines; i++)
    {
        program->machines[i]->declIndex = 0;
        PrtTraverseMachine(program->machines[i], PRT_FALSE);
    }
    for (PRT_UINT32 i = 0; i < program->nGlobalFuns; i++)
    {
        PrtTraverseFun(program->globalFuns[i], PRT_FALSE);
    }
    for (PRT_UINT32 i = 0; i < program->nForeignTypes; i++)
    {
        program->foreignTypes[i]->declIndex = 0;
    }
    program = NULL;
}

PRT_PROCESS *
PrtStartProcess(
    _In_ PRT_GUID guid,
    _In_ PRT_PROGRAMDECL *p,
    _In_ PRT_ERROR_FUN errorFun,
    _In_ PRT_LOG_FUN logFun
)
{
    PrtInstallProgram(p);

    PRT_PROCESS_PRIV *process;
    process = (PRT_PROCESS_PRIV *)PrtMalloc(sizeof(PRT_PROCESS_PRIV));
    process->guid = guid;
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
    PrtUninstallProgram();
	PrtFree(process);
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
	PRT_UINT32 renamedName = program->linkMap[context->renamedName][IorM];
	PRT_UINT32 instanceOf = program->renameMap[renamedName];

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
				PrtAssert(PRT_FALSE, "Illegal parameter type in PrtMkInterfaceOrMachine");
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
			PRT_MACHINEDECL *machineDecl = program->machines[instanceOf];
			PRT_FUNDECL *entryFun = machineDecl->states[machineDecl->initStateIndex].entryFun;
			PRT_TYPE *payloadType = entryFun->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PRT_MACHINEINST* result = (PRT_MACHINEINST*)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)context->process, renamedName, instanceOf, payload);
	// must now free this payload because PrtMkMachinePrivate clones it.
	PrtFreeValue(payload);
	return result;
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
	PRT_UINT32 instanceOf = program->renameMap[renamedMachine];

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
				PrtAssert(PRT_FALSE, "Illegal parameter type in PrtMkMachine");
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
			PRT_MACHINEDECL *machineDecl = program->machines[instanceOf];
			PRT_FUNDECL *entryFun = machineDecl->states[machineDecl->initStateIndex].entryFun;
			PRT_TYPE *payloadType = entryFun->payloadType;
			payload = MakeTupleFromArray(payloadType, args);
		}
		PrtFree(args);
	}
	PRT_MACHINEINST* result = (PRT_MACHINEINST*)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)process, renamedMachine, instanceOf, payload);
	// free the payload since we cloned it here, and PrtMkMachinePrivate also clones it.
	PrtFreeValue(payload);
	return result;
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
	state->machineName = program->machines[context->instanceOf]->name;
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
				PrtAssert(PRT_FALSE, "Illegal parameter type in PrtSend");
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
				PrtAssert(PRT_FALSE, "Illegal parameter type in PrtSendInternal");
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