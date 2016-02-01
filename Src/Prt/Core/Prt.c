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

void 
PrtStopProcess(
	_Inout_ PRT_PROCESS* process
)
{
	PRT_PROCESS_PRIV *privateProcess = (PRT_PROCESS_PRIV *)process;
	for (PRT_UINT32 i = 0; i < privateProcess->numMachines; i++)
	{
		PRT_MACHINEINST *context = privateProcess->machines[i];
		if (context->isModel)
			PrtCleanupModel(context);
		else 
			PrtCleanupMachine((PRT_MACHINEINST_PRIV *)context);
		PrtFree(context);
	}
	PrtCleanupForeignData();
	PrtFree(privateProcess->machines);
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
	if (machineCount == numMachines) {
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
	_In_ PRT_VALUE					*payload
)
{
	if (context->isModel)
	{
		context->process->program->modelImpls[context->instanceOf].sendFun(context, event, payload);
		return;
	}
	PrtSendPrivate((PRT_MACHINEINST_PRIV *)context, event, payload);
}
