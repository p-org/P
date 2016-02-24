#include "PrtWinUser.h"

void ResizeBuffer(_Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 numCharsWritten, PRT_UINT32 resizeNum)
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

void PrtWinUserPrintUint16(_In_ PRT_UINT16 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	ResizeBuffer(buffer, bufferSize, *numCharsWritten, 16);
	*numCharsWritten += sprintf(*buffer + *numCharsWritten, "%lu", i);
}
void PrtWinUserPrintUint32(_In_ PRT_UINT32 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	ResizeBuffer(buffer, bufferSize, *numCharsWritten, 32);
	*numCharsWritten += sprintf(*buffer + *numCharsWritten, "%lu", i);
}

void PrtWinUserPrintUint64(_In_ PRT_UINT64 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	ResizeBuffer(buffer, bufferSize, *numCharsWritten, 64);
	*numCharsWritten += sprintf(*buffer + *numCharsWritten, "%llu", i);
}

void PrtWinUserPrintInt32(_In_ PRT_INT32 i, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	ResizeBuffer(buffer, bufferSize, *numCharsWritten, 32);
	*numCharsWritten += sprintf(*buffer + *numCharsWritten, "%d", i);
}

void PrtWinUserPrintString(_In_ PRT_STRING s, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	ResizeBuffer(buffer, bufferSize, *numCharsWritten, strlen(s));
	*numCharsWritten += sprintf(*buffer + *numCharsWritten, "%s", s);
}

void PrtWinUserPrintMachineId(_In_ PRT_MACHINEID id, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PrtWinUserPrintString("< (", buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintUint32(id.processId.data1, buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintString(", ", buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintUint16(id.processId.data2, buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintString(", ", buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintUint16(id.processId.data3, buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintString(", ", buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintUint64(id.processId.data4, buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintString("), ", buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintUint32(id.machineId, buffer, bufferSize, numCharsWritten);
	PrtWinUserPrintString(">", buffer, bufferSize, numCharsWritten);
}

void PrtWinUserPrintType(_In_ PRT_TYPE *type, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_TYPE_KIND kind = type->typeKind;
	switch (kind)
	{
	case PRT_KIND_NULL:
		PrtWinUserPrintString("null", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_ANY:
		PrtWinUserPrintString("any", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_BOOL:
		PrtWinUserPrintString("bool", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_EVENT:
		PrtWinUserPrintString("event", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_MACHINE:
		PrtWinUserPrintString("machine", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_INT:
		PrtWinUserPrintString("int", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_FORGN:
		PrtWinUserPrintString("foreign", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = type->typeUnion.map;
		PrtWinUserPrintString("map[", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintType(mtype->domType, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(", ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintType(mtype->codType, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = type->typeUnion.nmTuple;
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		for (i = 0; i < ntype->arity; ++i)
		{
			PrtWinUserPrintString(ntype->fieldNames[i], buffer, bufferSize, numCharsWritten);
			PrtWinUserPrintString(": ", buffer, bufferSize, numCharsWritten);
			PrtWinUserPrintType(ntype->fieldTypes[i], buffer, bufferSize, numCharsWritten);
			if (i < ntype->arity - 1)
			{
				PrtWinUserPrintString(", ", buffer, bufferSize, numCharsWritten);
			}
			else
			{
				PrtWinUserPrintString(")", buffer, bufferSize, numCharsWritten);
			}
		}
		break;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = type->typeUnion.seq;
		PrtWinUserPrintString("seq[", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintType(stype->innerType, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = type->typeUnion.tuple;
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		if (ttype->arity == 1)
		{
			PrtWinUserPrintType(ttype->fieldTypes[0], buffer, bufferSize, numCharsWritten);
			PrtWinUserPrintString(",)", buffer, bufferSize, numCharsWritten);
		}
		else
		{
			for (i = 0; i < ttype->arity; ++i)
			{
				PrtWinUserPrintType(ttype->fieldTypes[i], buffer, bufferSize, numCharsWritten);
				if (i < ttype->arity - 1)
				{
					PrtWinUserPrintString(", ", buffer, bufferSize, numCharsWritten);
				}
				else
				{
					PrtWinUserPrintString(")", buffer, bufferSize, numCharsWritten);
				}
			}
		}
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtWinUserPrintType: Invalid type");
		break;
	}
}

void PrtWinUserPrintValue(_In_ PRT_VALUE *value, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
{
	PRT_STRING frgnStr;
	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALKIND_NULL:
		PrtWinUserPrintString("null", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_BOOL:
		PrtWinUserPrintString(PrtPrimGetBool(value) == PRT_TRUE ? "true" : "false", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_INT:
		PrtWinUserPrintInt32(PrtPrimGetInt(value), buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_EVENT:
		PrtWinUserPrintString("<", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(PrtPrimGetEvent(value), buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(">", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_MID:
		PrtWinUserPrintMachineId(PrtPrimGetMachine(value), buffer, bufferSize, numCharsWritten);
		break;
	case PRT_VALKIND_FORGN:
		frgnStr = PrtToStringForeignValue(((PRT_FORGNVALUE*)value->valueUnion.frgn)->typeTag, ((PRT_FORGNVALUE*)value->valueUnion.frgn)->value);
		PrtWinUserPrintString(frgnStr, buffer, bufferSize, numCharsWritten);
		PrtFree(frgnStr);
		break;
	case PRT_VALKIND_MAP:
	{
		PRT_MAPVALUE *mval = value->valueUnion.map;
		PRT_MAPNODE *next = mval->first;
		PrtWinUserPrintString("{", buffer, bufferSize, numCharsWritten);
		while (next != NULL)
		{
			PrtWinUserPrintValue(next->key, buffer, bufferSize, numCharsWritten);
			PrtWinUserPrintString(" --> ", buffer, bufferSize, numCharsWritten);
			PrtWinUserPrintValue(next->value, buffer, bufferSize, numCharsWritten);
			if (next->bucketNext != NULL)
			{
				PrtWinUserPrintString("*", buffer, bufferSize, numCharsWritten);
			}

			if (next->insertNext != NULL)
			{
				PrtWinUserPrintString(", ", buffer, bufferSize, numCharsWritten);
			}

			next = next->insertNext;
		}

		PrtWinUserPrintString("} (", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(mval->size, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(" / ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(PrtMapCapacity(value), buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(")", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_VALKIND_SEQ:
	{
		PRT_UINT32 i;
		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		PrtWinUserPrintString("[", buffer, bufferSize, numCharsWritten);
		for (i = 0; i < sVal->size; ++i)
		{
			PrtWinUserPrintValue(sVal->values[i], buffer, bufferSize, numCharsWritten);
			if (i < sVal->size - 1)
			{
				PrtWinUserPrintString(", ", buffer, bufferSize, numCharsWritten);
			}
		}

		PrtWinUserPrintString("]", buffer, bufferSize, numCharsWritten);
		break;
	}
	case PRT_VALKIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tval = value->valueUnion.tuple;
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		if (tval->size == 1)
		{
			PrtWinUserPrintValue(tval->values[0], buffer, bufferSize, numCharsWritten);
			PrtWinUserPrintString(",)", buffer, bufferSize, numCharsWritten);
		}
		else
		{
			for (i = 0; i < tval->size; ++i)
			{
				PrtWinUserPrintValue(tval->values[i], buffer, bufferSize, numCharsWritten);
				if (i < tval->size - 1)
				{
					PrtWinUserPrintString(", ", buffer, bufferSize, numCharsWritten);
				}
				else
				{
					PrtWinUserPrintString(")", buffer, bufferSize, numCharsWritten);
				}
			}
		}
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtWinUserPrintValue: Invalid value");
		break;
	}
}

void PrtWinUserPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *machine, _Inout_ char **buffer, _Inout_ PRT_UINT32 *bufferSize, _Inout_ PRT_UINT32 *numCharsWritten)
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
		PrtWinUserPrintString("<HaltLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") halted in state ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_ENQUEUE:
		eventIndex = PrtPrimGetEvent(c->eventQueue.events[c->eventQueue.tailIndex == 0 ? (c->eventQueue.eventsSize - 1) : (c->eventQueue.tailIndex - 1)].trigger);
		eventName = c->process->program->events[eventIndex].name;
		payloadValue = (c->eventQueue.events[c->eventQueue.tailIndex == 0 ? (c->eventQueue.eventsSize - 1) : (c->eventQueue.tailIndex - 1)].payload);
		PrtWinUserPrintString("<EnqueueLog> Enqueued event ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintValue(payloadValue, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(" on Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(")\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_DEQUEUE:
		eventName = c->process->program->events[PrtPrimGetEvent(PrtGetCurrentTrigger(c))].name;
		payloadValue = PrtGetCurrentPayload(c);
		PrtWinUserPrintString("<DequeueLog> Dequeued event ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintValue(payloadValue, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(" by Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(")\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_ENTRY:
		PrtWinUserPrintString("<StateLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") entered state ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_CREATE:
		PrtWinUserPrintString("<CreateLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") is created\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_RAISE:
		eventName = c->process->program->events[PrtPrimGetEvent(PrtGetCurrentTrigger(c))].name;
		payloadValue = PrtGetCurrentPayload(c);
		PrtWinUserPrintString("<RaiseLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") raised event ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(" with payload ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintValue(payloadValue, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_POP:
		PrtWinUserPrintString("<PopLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") popped and reentered state ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_PUSH:
		PrtWinUserPrintString("<PushLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") pushed\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_UNHANDLED:
		eventName = c->process->program->events[PrtPrimGetEvent(PrtGetCurrentTrigger(c))].name;
		PrtWinUserPrintString("<PopLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") popped with unhandled event ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(eventName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(" and reentered state ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_DO:
		PrtWinUserPrintString("<ActionLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") executed action in state ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_EXIT:
		PrtWinUserPrintString("<ExitLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") exiting state ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("\n", buffer, bufferSize, numCharsWritten);
		break;
	case PRT_STEP_IGNORE:
		PrtWinUserPrintString("<ActionLog> Machine ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(machineName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("(", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintUint32(machineId, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(") ignored event in state ", buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString(stateName, buffer, bufferSize, numCharsWritten);
		PrtWinUserPrintString("\n", buffer, bufferSize, numCharsWritten);
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

	PrtWinUserPrintValue(value, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringValue(_In_ PRT_VALUE *value)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtWinUserPrintValue(value, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}

void PRT_CALL_CONV PrtPrintType(_In_ PRT_TYPE *type)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtWinUserPrintType(type, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringType(_In_ PRT_TYPE *type)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtWinUserPrintType(type, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}

void PRT_CALL_CONV PrtPrintStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *machine)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtWinUserPrintStep(step, machine, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	PrtPrintf(buffer);
	PrtFree(buffer);
}

PRT_STRING PRT_CALL_CONV PrtToStringStep(_In_ PRT_STEP step, _In_ PRT_MACHINEINST *machine)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtWinUserPrintStep(step, machine, &buffer, &bufferSize, &nChars);
	PRT_DBG_ASSERT(buffer[nChars] == '\0', "Expected null terminated result");
	return buffer;
}