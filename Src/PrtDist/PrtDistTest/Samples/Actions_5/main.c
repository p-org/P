#include "program.h"

void P_DTOR_Ghost_IMPL(PRT_SM_CONTEXT *context) { }

void P_DTOR_Real_IMPL(PRT_SM_CONTEXT *context) { }

void P_CTOR_Ghost_IMPL(PRT_SM_CONTEXT *context, PRT_VALUE *value) { }

void P_CTOR_Real_IMPL(PRT_SM_CONTEXT *context, PRT_VALUE *value) { }

PRT_STRING PrtValueToString(_In_ PRT_VALUE *value)
{
	PRT_STRING retString = (PRT_STRING)PrtCalloc(100, sizeof(char));
	PRT_TYPE_KIND kind = value->type->typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		break;
	case PRT_KIND_BOOL:
		sprintf_s(retString, 100, PrtPrimGetBool(value) == PRT_TRUE ? "true" : "false");
		break;
	case PRT_KIND_EVENT:
	{
						   PRT_UINT32 event = PrtPrimGetEvent(value);
						   sprintf_s(retString, 100, "<%d>", event);
						   break;
	}
	case PRT_KIND_NULL:
		sprintf_s(retString, 100, "null");
		break;
	case PRT_KIND_MACHINE:
	{
							 PRT_MACHINEID machine = PrtPrimGetMachine(value);
							 sprintf_s(retString, 100, "<%d>", machine);

							 break;
	}
	case PRT_KIND_INT:
		sprintf_s(retString, 100, "%d", PrtPrimGetInt(value));
		break;
	case PRT_KIND_FORGN:
		sprintf_s(retString, 100, "foreign");
		break;
	case PRT_KIND_MAP:
	{
						 PRT_MAPVALUE *mval = value->valueUnion.map;
						 PRT_MAPNODE *next = mval->first;
						 sprintf_s(retString, 100, "{");
						 while (next != NULL)
						 {
							 PrtValueToString(next->key);
							 sprintf_s(retString, 100, " --> ");
							 PrtValueToString(next->value);
							 if (next->bucketNext != NULL)
							 {
								 sprintf_s(retString, 100, "*");
							 }

							 if (next->insertNext != NULL)
							 {
								 sprintf_s(retString, 100, ", ");
							 }

							 next = next->insertNext;
						 }

						 sprintf_s(retString, 100, "} (%d / %d)", mval->size, PrtMapCapacity(value));
						 break;
	}
	case PRT_KIND_NMDTUP:
	{
							PRT_UINT32 i;
							PRT_TUPVALUE *tval = value->valueUnion.tuple;
							PRT_NMDTUPTYPE *ntype = value->type->typeUnion.nmTuple;
							sprintf_s(retString, 100, "(");
							for (i = 0; i < ntype->arity; ++i)
							{
								sprintf_s(retString, 100, "%s = ", ntype->fieldNames[i]);
								PrtValueToString(tval->values[i]);
								if (i < ntype->arity - 1)
								{
									sprintf_s(retString, 100, ", ");
								}
								else
								{
									sprintf_s(retString, 100, ")");
								}
							}

							break;
	}
	case PRT_KIND_SEQ:
	{
						 PRT_UINT32 i;
						 PRT_SEQVALUE *sVal = value->valueUnion.seq;
						 sprintf_s(retString, 100, "[");
						 for (i = 0; i < sVal->size; ++i)
						 {
							 PrtValueToString(sVal->values[i]);
							 if (i < sVal->size - 1)
							 {
								 sprintf_s(retString, 100, ", ");
							 }
						 }

						 sprintf_s(retString, 100, "]");
						 break;
	}
	case PRT_KIND_TUPLE:
	{
						   PRT_UINT32 i;
						   PRT_TUPVALUE *tval = value->valueUnion.tuple;
						   PRT_TUPTYPE *ttype = value->type->typeUnion.tuple;
						   sprintf_s(retString, 100, "(");
						   if (ttype->arity == 1)
						   {
							   PrtValueToString(tval->values[0]);
							   sprintf_s(retString, 100, ",)");
						   }
						   else
						   {
							   for (i = 0; i < ttype->arity; ++i)
							   {
								   PrtValueToString(tval->values[i]);
								   if (i < ttype->arity - 1)
								   {
									   sprintf_s(retString, 100, ", ");
								   }
								   else
								   {
									   sprintf_s(retString, 100, ")");
								   }
							   }
						   }

						   break;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		break;
	}

	return retString;
}

void ErrorHandler(PRT_STATUS status, void *ptr) { }

PRT_STATEDECL *GetCurrentStateDecl(PRT_SM_CONTEXT_PRIV *context)
{
	return &(context->process->program->machines[context->instanceOf].states[context->currentState]);
}

void Log(PRT_STEP step, void *vcontext)
{
	static FILE *logfile = NULL;
	PRT_SM_CONTEXT_PRIV *c = (PRT_SM_CONTEXT_PRIV*)vcontext;
	PRT_STRING MachineName = c->process->program->machines[c->instanceOf].name;
	PRT_UINT32 MachineId = c->id->valueUnion.mid->machineId;
	PRT_VALUE* payloadValue;
	char fileName[100] = "PRT_PPROCESS_LOG_";
	char processId[100];
	_itoa(c->id->valueUnion.mid->processId.data1, processId, 10);
	strcat_s(fileName, 100, processId);
	strcat_s(fileName, 100, ".txt");
	if (logfile == NULL)
	{
		logfile = fopen(fileName, "w+");
	}

	char log[1000];

	switch (step)
	{
	case PRT_STEP_HALT:
	{
						  sprintf_s(log, 1000, "<HaltLog> Machine %s(0x%lu) halted in state %s\n", MachineName, MachineId, GetCurrentStateDecl(c)->name);
						  break;
	}
	case PRT_STEP_ENQUEUE:
	{
							 PRT_UINT32 eventIndex = PrtPrimGetEvent(c->eventQueue.events[c->eventQueue.tailIndex == 0 ? (c->eventQueue.eventsSize - 1) : (c->eventQueue.tailIndex - 1)].trigger);
							 PRT_STRING eventName = c->process->program->events[eventIndex].name;
							 payloadValue = (c->eventQueue.events[c->eventQueue.tailIndex == 0 ? (c->eventQueue.eventsSize - 1) : (c->eventQueue.tailIndex - 1)].payload);
							 sprintf_s(log, 1000, "<EnqueueLog> enqueued event <%s, %s> on Machine %s(0x%lu)\n", eventName, PrtValueToString(payloadValue), MachineName, MachineId);
							 break;
	}
	case PRT_STEP_DEQUEUE:
	{
							 PRT_STRING eventName = c->process->program->events[PrtPrimGetEvent(c->currEvent.trigger)].name;
							 payloadValue = (c->currEvent.payload);
							 sprintf_s(log, 1000, "<DequeueLog> dequeued event <%s, %s> by Machine %s(0x%lu)\n", eventName, PrtValueToString(payloadValue), MachineName, MachineId);
							 break;
	}
	case PRT_STEP_ENTRY:
		sprintf_s(log, 1000, "<StateLog> Machine %s(0x%lu) entered state %s\n", MachineName, MachineId, GetCurrentStateDecl(c)->name);
		break;
	case PRT_STEP_CREATE:
		sprintf_s(log, 1000, "<CreateLog> Machine %s(0x%lu) is created\n", MachineName, MachineId);
		break;
	case PRT_STEP_RAISE:
	{
						   PRT_STRING eventName = c->process->program->events[PrtPrimGetEvent(c->currEvent.trigger)].name;
						   payloadValue = (c->currEvent.payload);
						   sprintf_s(log, 1000, "<RaiseLog> Machine %s(0x%lu) raised event <%s, %s>\n", MachineName, MachineId, eventName, PrtValueToString(payloadValue));
						   break;
	}
	case PRT_STEP_POP:
		sprintf_s(log, 1000, "<PopLog> Machine %s(0x%lu) popped and reentered state %s\n", MachineName, MachineId, GetCurrentStateDecl(c)->name);
		break;
	case PRT_STEP_PUSH:
		sprintf_s(log, 1000, "<CallLog> Machine %s(0x%lu) pushed\n", MachineName, MachineId);
		break;
	case PRT_STEP_UNHANDLED:
	{
							   PRT_STRING eventName = c->process->program->events[PrtPrimGetEvent(c->currEvent.trigger)].name;
							   sprintf_s(log, 1000, "<PopLog> Machine %s(0x%lu) popped with unhandled event %s and reentered state %s\n", MachineName, MachineId, eventName, GetCurrentStateDecl(c)->name);
							   break;
	}
	case PRT_STEP_DO:
		sprintf_s(log, 1000, "<ActionLog> Machine %s(0x%lu) executed action in %s\n", MachineName, MachineId, GetCurrentStateDecl(c)->name);
		break;
	case PRT_STEP_EXIT:
		sprintf_s(log, 1000, "<ExitLog> Machine %s(0x%lu) exited state %s\n", MachineName, MachineId, GetCurrentStateDecl(c)->name);
		break;
	case PRT_STEP_IGNORE:
		sprintf_s(log, 1000, "<ActionLog> Machine %s(0x%lu) ignored event in %s\n", MachineName, MachineId, GetCurrentStateDecl(c)->name);
		break;
	default:
		PRT_DBG_ASSERT(PRT_FALSE, "Must not get here");
		break;
	}

	PrtLockMutex(((PRT_PROCESS_PRIV*)c->process)->processLock);
	fputs(log, logfile);
	fflush(logfile);
	PrtUnlockMutex(((PRT_PROCESS_PRIV*)c->process)->processLock);
}

void main()
{
	PRT_GUID processGuid;
	processGuid.data1 = 1;
	processGuid.data2 = 0;
	processGuid.data3 = 0;
	processGuid.data4 = 0;
	PrtMkMachine(PrtStartProcess(processGuid, &P_GEND_PROGRAM, ErrorHandler, Log), _P_MACHINE_MAIN, PrtMkNullValue());
}
