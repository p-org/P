#include "PrtUser.h"

static void ResizeBuffer(_Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 numCharsWritten, PRT_UINT32 resizeNum)
{
	PRT_UINT32 padding = 100;
	if (*buffer == NULL)
	{
		*bufferSize = resizeNum + 1 + padding;
		*buffer = (char *)PrtCalloc(*bufferSize, sizeof(char));
	}
	else if (*bufferSize < numCharsWritten + resizeNum + 1)
	{
		PRT_UINT32 newBufferSize = numCharsWritten + resizeNum + 1 + padding;
		char *newBuffer = (char *)PrtCalloc(newBufferSize, sizeof(char));
		strcpy_s(newBuffer, newBufferSize, *buffer);
		PrtFree(*buffer);
		*buffer = newBuffer;
		*bufferSize = newBufferSize;
	}
}

static void PrtUserPrintUint16(_In_ PRT_UINT16 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 16);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%u", i);
}
static void PrtUserPrintUint32(_In_ PRT_UINT32 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 32);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%u", i);
}

static void PrtUserPrintUint64(_In_ PRT_UINT64 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 64);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%llu", i);
}

static void PrtUserPrintInt32(_In_ PRT_INT32 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, 32);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%d", i);
}

static void PrtUserPrintString(_In_ PRT_STRING s, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_UINT32 written = *numCharsWritten;
	ResizeBuffer(buffer, bufferSize, written, (PRT_UINT32)strlen(s) + 1);
	*numCharsWritten += sprintf_s(*buffer + written, *bufferSize - written, "%s", s);
}

static void PrtUserPrintMachineId(_In_ PRT_MACHINEID id, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PrtUserPrintString("< (", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint32(id.processId.data1, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint16(id.processId.data2, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint16(id.processId.data3, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint64(id.processId.data4, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString("), ", buffer, bufferSize, numCharsWritten);
	PrtUserPrintUint32(id.machineId, buffer, bufferSize, numCharsWritten);
	PrtUserPrintString(">", buffer, bufferSize, numCharsWritten);
}

static void PrtUserPrintType(_In_ PRT_TYPE *type, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_TYPE_KIND kind = type->typeKind;
	switch (kind)
	{
	case PRT_KIND_NULL:
		PrtUserPrintString("null", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_ANY:
		PrtUserPrintString("any", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_BOOL:
		PrtUserPrintString("bool", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_EVENT:
		PrtUserPrintString("event", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_MACHINE:
		PrtUserPrintString("machine", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_INT:
		PrtUserPrintString("int", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_FORGN:
		PrtUserPrintString("foreign", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = type->typeUnion.map;
		PrtUserPrintString("map[", buffer, bufferSize, numCharsWritten);
		PrtUserPrintType(mtype->domType, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintType(mtype->codType, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = type->typeUnion.nmTuple;
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		for (i = 0; i < ntype->arity; ++i)
		{
			PrtUserPrintString(ntype->fieldNames[i], buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(": ", buffer, bufferSize, numCharsWritten);
			PrtUserPrintType(ntype->fieldTypes[i], buffer, bufferSize, numCharsWritten);
			if (i < ntype->arity - 1)
			{
				PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
			}
			else
			{
				PrtUserPrintString(")", buffer, bufferSize, numCharsWritten);
			}
		}
		break;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = type->typeUnion.seq;
		PrtUserPrintString("seq[", buffer, bufferSize, numCharsWritten);
		PrtUserPrintType(stype->innerType, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = type->typeUnion.tuple;
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		if (ttype->arity == 1)
		{
			PrtUserPrintType(ttype->fieldTypes[0], buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(",)", buffer, bufferSize, numCharsWritten);
		}
		else
		{
			for (i = 0; i < ttype->arity; ++i)
			{
				PrtUserPrintType(ttype->fieldTypes[i], buffer, bufferSize, numCharsWritten);
				if (i < ttype->arity - 1)
				{
					PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
				}
				else
				{
					PrtUserPrintString(")", buffer, bufferSize, numCharsWritten);
				}
			}
		}
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtUserPrintType: Invalid type");
		break;
	}
}

static void PrtUserPrintValue(_In_ PRT_VALUE *value, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_STRING frgnStr;
	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALKIND_NULL:
		PrtUserPrintString("null", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_BOOL:
		PrtUserPrintString(PrtPrimGetBool(value) == PRT_TRUE ? "true" : "false", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_INT:
		PrtUserPrintInt32(PrtPrimGetInt(value), buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_EVENT:
		PrtUserPrintString("<", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(PrtPrimGetEvent(value), buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(">", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_MID:
		PrtUserPrintMachineId(PrtPrimGetMachine(value), buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_FORGN:
		frgnStr = prtForeignTypeDecls[value->valueUnion.frgn->typeTag].toStringFun(value->valueUnion.frgn->value);
		PrtUserPrintString(frgnStr, buffer, bufferSize, numCharsWritten);
		PrtFree(frgnStr);
		break;
	case PRT_VALKIND_MAP:
	{
		PRT_MAPVALUE *mval = value->valueUnion.map;
		PRT_MAPNODE *next = mval->first;
		PrtUserPrintString("{", buffer, bufferSize, numCharsWritten);
		while (next != NULL)
		{
			PrtUserPrintValue(next->key, buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(" --> ", buffer, bufferSize, numCharsWritten);
			PrtUserPrintValue(next->value, buffer, bufferSize, numCharsWritten);
			if (next->bucketNext != NULL)
			{
				PrtUserPrintString("*", buffer, bufferSize, numCharsWritten);
			}

			if (next->insertNext != NULL)
			{
				PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
			}

			next = next->insertNext;
		}

		PrtUserPrintString("} (", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(mval->size, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" / ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(PrtMapCapacity(value), buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(")", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_VALKIND_SEQ:
	{
		PRT_UINT32 i;
		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		PrtUserPrintString("[", buffer, bufferSize, numCharsWritten);
		for (i = 0; i < sVal->size; ++i)
		{
			PrtUserPrintValue(sVal->values[i], buffer, bufferSize, numCharsWritten);
			if (i < sVal->size - 1)
			{
				PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
			}
		}

		PrtUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_VALKIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tval = value->valueUnion.tuple;
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		if (tval->size == 1)
		{
			PrtUserPrintValue(tval->values[0], buffer, bufferSize, numCharsWritten);
			PrtUserPrintString(",)", buffer, bufferSize, numCharsWritten);
		}
		else
		{
			for (i = 0; i < tval->size; ++i)
			{
				PrtUserPrintValue(tval->values[i], buffer, bufferSize, numCharsWritten);
				if (i < tval->size - 1)
				{
					PrtUserPrintString(", ", buffer, bufferSize, numCharsWritten);
				}
				else
				{
					PrtUserPrintString(")", buffer, bufferSize, numCharsWritten);
				}
			}
		}
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtUserPrintValue: Invalid value");
		break;
	}
}

static void PrtUserPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *machine, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_MACHINEINST_PRIV * c = (PRT_MACHINEINST_PRIV *)machine;
	PRT_STRING machineName = c->process->program->machines[c->instanceOf].name;
	PRT_UINT32 machineId = c->id->valueUnion.mid->machineId;
	PRT_STRING stateName = PrtGetCurrentStateDecl(c)->name;
	PRT_VALUE *payloadValue;
	PRT_UINT32 eventIndex;
	PRT_STRING eventName;

	switch (step)
	{
	case PRT_STEP_HALT:
		PrtUserPrintString("<HaltLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") halted in state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_ENQUEUE:
		eventIndex = PrtPrimGetEvent(c->eventQueue.events[c->eventQueue.tailIndex == 0 ? (c->eventQueue.eventsSize - 1) : (c->eventQueue.tailIndex - 1)].trigger);
		eventName = c->process->program->events[eventIndex].name;
		payloadValue = (c->eventQueue.events[c->eventQueue.tailIndex == 0 ? (c->eventQueue.eventsSize - 1) : (c->eventQueue.tailIndex - 1)].payload);
		PrtUserPrintString("<EnqueueLog> Enqueued event ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintValue(payloadValue, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" on Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(")\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_DEQUEUE:
		eventName = c->process->program->events[PrtPrimGetEvent(PrtGetCurrentTrigger(c))].name;
		payloadValue = PrtGetCurrentPayload(c);
		PrtUserPrintString("<DequeueLog> Dequeued event ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintValue(payloadValue, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" by Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(")\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_ENTRY:
		PrtUserPrintString("<StateLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") entered state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_CREATE:
		PrtUserPrintString("<CreateLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") is created\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_RAISE:
		eventName = c->process->program->events[PrtPrimGetEvent(PrtGetCurrentTrigger(c))].name;
		payloadValue = PrtGetCurrentPayload(c);
		PrtUserPrintString("<RaiseLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") raised event ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintValue(payloadValue, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_POP:
		PrtUserPrintString("<PopLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") popped and reentered state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_PUSH:
		PrtUserPrintString("<PushLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") pushed\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_UNHANDLED:
		eventName = c->process->program->events[c->eventValue].name;
		PrtUserPrintString("<PopLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") popped with unhandled event ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(" and reentered state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_DO:
		PrtUserPrintString("<ActionLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") executed action in state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_EXIT:
		PrtUserPrintString("<ExitLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") exiting state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_IGNORE:
		PrtUserPrintString("<ActionLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(") ignored event in state ", buffer, bufferSize, numCharsWritten);
		PrtUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	default:
		PrtAssert(PRT_FALSE, "Illegal PRT_STEP value");
		break;
	}
}

void PRT_CALL_CONV PrtPrintValue(_In_ PRT_VALUE *value)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintValue(value, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringValue(_In_ PRT_VALUE *value)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintValue(value, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}

void PRT_CALL_CONV PrtPrintType(_In_ PRT_TYPE *type)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintType(type, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringType(_In_ PRT_TYPE *type)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintType(type, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}

void PRT_CALL_CONV PrtPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *machine)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintStep(step, machine, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *machine)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtUserPrintStep(step, machine, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}