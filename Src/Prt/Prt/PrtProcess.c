#include "PrtHeaders.h"

PRT_PPROCESS*
PrtStartPProcess(
__in PRT_UINT16 processId,
__in PRT_PROGRAMDECL *program,
__in PRT_EXCEPHANDLER_FUN exHandler,
__in PRT_LOG_FUN logger
)
{
	PRT_PPROCESS* retPProcess = (PRT_PPROCESS*)PrtMalloc(sizeof(PRT_PPROCESS));
	retPProcess->allMachines = NULL;
	retPProcess->exceptionHandler = exHandler;
	retPProcess->lock = PrtCreateMutex();
	retPProcess->log = logger;
	retPProcess->processId = processId;

	return retPProcess;
}

void
PrtStopPProcess(
PRT_PPROCESS* stopProcess)
{
	//Free all context
	PRT_LINKEDLIST* currContext = stopProcess->allMachines;
	PRT_LINKEDLIST* temp;
	while (currContext != NULL)
	{
		temp = currContext;
		currContext = currContext->next;
		PrtFreeSMContext(temp->data);
		PrtFree(temp);
	}
}

void
PrtPProcessAddMachine(
__in void* context
)
{
	PRT_SMCONTEXT *prtContext = (PRT_SMCONTEXT*)context;
	PRT_LINKEDLIST *curr = prtContext->parentProcess->allMachines;
	if (curr == NULL)
	{
		prtContext->parentProcess->allMachines = (PRT_LINKEDLIST*)PrtMalloc(sizeof(PRT_LINKEDLIST));
		prtContext->parentProcess->allMachines->data = context;
		prtContext->parentProcess->allMachines->next = NULL;
	}
	else
	{
		PRT_LINKEDLIST *newNode = (PRT_LINKEDLIST*)PrtMalloc(sizeof(PRT_LINKEDLIST));
		newNode->data = context;
		newNode->next = NULL;
		while (curr->next != NULL)
		{
			curr = curr->next;
		}
		
		curr->next = newNode;
	}
}