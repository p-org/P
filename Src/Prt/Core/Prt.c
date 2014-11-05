#include "PrtExecution.h"

/*********************************************************************************

Public Functions

*********************************************************************************/
PRT_PROCESS * PrtStartProcess(
	_In_ PRT_GUID guid,
	_In_ PRT_PROGRAMDECL *program,
	_In_ PRT_ERROR_FUN errorFun,
	_In_ PRT_LOG_FUN logFun
	)
{
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
	return (PRT_PROCESS *)process;
}

void PrtStopProcess(_Inout_ PRT_PROCESS* process)
{
	PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV *)process;
	for (PRT_UINT32 i = 0; i < privateProcess->numMachines; i++)
	{
		PRT_SM_CONTEXT *context = privateProcess->machines[i];
		if (context->isModel)
			PrtCleanupModel(context);
		else 
			PrtCleanupMachine((PRT_SM_CONTEXT_PRIV *)context);
		PrtFree(context);
	}
	PrtFree(privateProcess->machines);
	PrtDestroyMutex(privateProcess->processLock);
	PrtFree(process);
}

PRT_SM_CONTEXT *PrtMkMachine(
__in  PRT_PROCESS				*process,
__in  PRT_UINT32				instanceOf,
__in  PRT_VALUE					*payload
)
{
	return (PRT_SM_CONTEXT *)PrtMkMachinePrivate((PRT_PROCESS_PRIV *)process, instanceOf, payload);
}

PRT_SM_CONTEXT *PrtMkModel(
	__in  PRT_PROCESS				*process,
	__in  PRT_UINT32				instanceOf,
	__in  PRT_VALUE					*payload
	)
{
	PRT_SM_CONTEXT *context;
	PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV *)process;

	PrtLockMutex(privateProcess->processLock);

	//
	// Allocate memory for state machine context
	//
	context = (PRT_SM_CONTEXT *)PrtMalloc(sizeof(PRT_SM_CONTEXT));

	//
	// Add it to the array of machines in the process
	//
	PRT_UINT32 numMachines = privateProcess->numMachines;
	PRT_UINT32 machineCount = privateProcess->machineCount;
	PRT_SM_CONTEXT **machines = privateProcess->machines;
	if (machineCount == numMachines) {
		PRT_SM_CONTEXT **newMachines = (PRT_SM_CONTEXT **)PrtCalloc(2 * machineCount, sizeof(PRT_SM_CONTEXT *));
		for (PRT_UINT32 i = 0; i < machineCount; i++)
		{
			newMachines[i] = machines[i];
		}
		machines = newMachines;
		privateProcess->machines = newMachines;
		privateProcess->machineCount = 2 * machineCount;
	}
	machines[numMachines] = (PRT_SM_CONTEXT *)context;
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

PRT_SM_CONTEXT * PrtGetMachine(
	_In_ PRT_PROCESS *process,
	_In_ PRT_VALUE *id)
{
	PRT_MACHINEID *machineId;
	PRT_PROCESS_PRIV *privateProcess;
	PrtAssert(id->discriminator == PRT_VALKIND_MID, "id is not legal PRT_MACHINEID");
	machineId = id->valueUnion.mid;
	PrtAssert(PrtAreGuidsEqual(process->guid, machineId->processId), "id does not belong to process");
	privateProcess = (PRT_PROCESS_PRIV *)process;
	PrtAssert(0 < machineId->machineId && machineId->machineId <= privateProcess->numMachines, "id out of bounds");
	return privateProcess->machines[machineId->machineId - 1];
}

void
PrtSend(
__in PRT_SM_CONTEXT				*machine,
__in PRT_VALUE					*event,
__in PRT_VALUE					*payload
)
{
	if (machine->isModel)
	{
		machine->process->program->modelImpls[machine->instanceOf].sendFun(machine, event, payload);
		return;
	}
	PrtSendPrivate((PRT_SM_CONTEXT_PRIV *)machine, event, payload);
}
