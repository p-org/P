#include "PrtWinUser.h"
#include "PrtDist.h"
#include "PrtExecution.h"

/** Array of prime hash table capacities. */
const PRT_UINT32 PrtHashtableCapacities[] =
{
	3, 13, 31, 61, 127,
	251, 509, 1021, 2039, 4093,
	8191, 16381, 32749, 65521, 131071,
	262139, 524287, 1048573, 2097143, 4194301,
	8388593, 16777213, 33554393, 67108859, 134217689,
	268435399, 536870909, 1073741789, 2147483647, 4294967291
};

PRT_TYPE*
PrtDistDeserializeType(
__in PRT_TYPE* type
)
{
	PRT_TYPE_KIND kind = type->typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_NULL:
		return PrtMkPrimitiveType(kind);
	case PRT_KIND_FORGN:
	{
		PrtAssert(PRT_FALSE, "Invalid type : Foreign Type not expected");
		return PrtMkPrimitiveType(PRT_KIND_NULL);
	}
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = type->typeUnion.map;
		return PrtMkMapType(PrtDistDeserializeType(mtype->domType), PrtDistDeserializeType(mtype->codType));
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i = 0;
		PRT_NMDTUPTYPE *ntype = type->typeUnion.nmTuple;
		PRT_TYPE* clone = PrtMkNmdTupType(ntype->arity);

		PRT_STRING_NODE *currN = ntype->fieldNamesSerialized;
		PRT_TYPE_NODE * currT = ntype->fieldTypesSerialized;

		while (currN != NULL)
		{
			PrtSetFieldName(clone, i, currN->name);
			PrtSetFieldType(clone, i, PrtDistDeserializeType(currT->type));
			i = i + 1;
			currN = currN->nextNode;
			currT = currT->nextNode;
		}
		clone->typeUnion.nmTuple->fieldNamesSerialized = NULL;
		clone->typeUnion.nmTuple->fieldTypesSerialized = NULL;

		return clone;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = type->typeUnion.seq;
		return PrtMkSeqType(PrtDistDeserializeType(stype->innerType));
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i = 0;
		PRT_TUPTYPE *ttype = type->typeUnion.tuple;
		PRT_TYPE* clone = PrtMkTupType(ttype->arity);
		PRT_TYPE_NODE * currT = ttype->fieldTypesSerialized;

		while (currT != NULL)
		{
			PrtSetFieldType(clone, i, PrtDistDeserializeType(currT->type));
			i = i + 1;
			currT = currT->nextNode;
		}

		return clone;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return PrtMkPrimitiveType(PRT_KIND_NULL);
	}

}

PRT_VALUE*
PrtDistDeserializeValue(
__in PRT_VALUE* value
)

{
	PRT_TYPE_KIND kind = value->type->typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return NULL;
	case PRT_KIND_BOOL:

	case PRT_KIND_EVENT:

	case PRT_KIND_MACHINE:

	case PRT_KIND_INT:
		return PrtCloneValue(value);
	case PRT_KIND_FORGN:
	{
		PrtAssert(PRT_FALSE, "Foreign Type not Expected");
		return NULL;
	}
	case PRT_KIND_MAP:
	{
		PRT_TYPE* tt = PrtDistDeserializeType(value->type);
		PRT_VALUE *retVal = PrtMkDefaultValue(tt);
		PRT_MAPVALUE *mVal = value->valueUnion.map;
		PRT_MAPVALUE *cVal = retVal->valueUnion.map;
		if (mVal->capNum > 0)
		{
			//// Eagerly allocate capacity in the clone to avoid intermediate rehashings.
			PrtFree(cVal->buckets);
			cVal->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[mVal->capNum], sizeof(PRT_MAPNODE *));
			cVal->capNum = mVal->capNum;

		}
		PRT_MAPNODE *next = mVal->first;
		while (next != NULL)
		{
			PrtMapUpdate(retVal, PrtDistDeserializeValue(next->key), PrtDistDeserializeValue(next->value));
			next = next->insertNext;
		}

		return retVal;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_TYPE* tt = PrtDistDeserializeType(value->type);
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(tt);
		retVal->discriminator = PRT_VALKIND_TUPLE;

		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type->typeUnion.nmTuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));

		PRT_VALUE_NODE *curr = tVal->valuesSerialized;
		PRT_UINT32 i = 0;
		while (curr != NULL)
		{
			cVal->values[i] = PrtDistDeserializeValue(curr->value);
			curr = curr->nextNode;
			i = i + 1;
		}
		cVal->valuesSerialized = NULL;

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_TYPE* tt = PrtDistDeserializeType(value->type);
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(tt);
		retVal->discriminator = PRT_VALKIND_TUPLE;

		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type->typeUnion.tuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));

		PRT_VALUE_NODE *curr = tVal->valuesSerialized;
		PRT_UINT32 i = 0;
		while (curr != NULL)
		{
			cVal->values[i] = PrtDistDeserializeValue(curr->value);
			curr = curr->nextNode;
			i = i + 1;
		}
		cVal->valuesSerialized = NULL;
		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_SEQ:
	{
		PRT_TYPE* tt = PrtDistDeserializeType(value->type);
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(tt);
		retVal->discriminator = PRT_VALKIND_SEQ;
		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		PRT_SEQVALUE *cVal = (PRT_SEQVALUE *)PrtCalloc(1, sizeof(PRT_SEQVALUE));
		cVal->capacity = sVal->capacity;
		cVal->size = sVal->size;
		if (sVal->capacity == 0)
		{
			cVal->values = NULL;
		}
		else
		{
			PRT_UINT32 i = 0;
			cVal->values = (PRT_VALUE **)PrtCalloc(sVal->capacity, sizeof(PRT_VALUE*));
			PRT_VALUE_NODE *curr = sVal->valuesSerialized;
			while (curr != NULL)
			{
				cVal->values[i] = PrtDistDeserializeValue(curr->value);
				curr = curr->nextNode;
				i = i + 1;
			}

		}
		cVal->valuesSerialized = NULL;
		retVal->valueUnion.seq = cVal;
		return retVal;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
}

void InsertValueNode(PRT_VALUE_NODE** head, PRT_VALUE *value){
	PRT_VALUE_NODE* curr = *head;
	if (curr == NULL)
	{
		PRT_VALUE_NODE* curr = (PRT_VALUE_NODE*)PrtCalloc(1, sizeof(PRT_VALUE_NODE));
		curr->value = value;
		curr->nextNode = NULL;
		*head = curr;
	}
	else
	{
		while (curr->nextNode != NULL)
		{
			curr = curr->nextNode;
		}
		curr->nextNode = (PRT_VALUE_NODE*)PrtCalloc(1, sizeof(PRT_VALUE_NODE));
		curr->nextNode->value = value;
		curr->nextNode->nextNode = NULL;
	}

}

void InsertStringNode(PRT_STRING_NODE** head, PRT_STRING value){
	PRT_STRING_NODE* curr = *head;
	if (curr == NULL)
	{
		PRT_STRING_NODE* curr = (PRT_STRING_NODE*)PrtCalloc(1, sizeof(PRT_STRING_NODE));
		curr->name = value;
		curr->nextNode = NULL;
		*head = curr;
	}
	else
	{
		while (curr->nextNode != NULL)
		{
			curr = curr->nextNode;
		}
		curr->nextNode = (PRT_STRING_NODE*)PrtCalloc(1, sizeof(PRT_STRING_NODE));
		curr->nextNode->name = value;
		curr->nextNode->nextNode = NULL;
	}

}

void InsertTypeNode(PRT_TYPE_NODE** head, PRT_TYPE* value){
	PRT_TYPE_NODE* curr = *head;
	if (curr == NULL)
	{
		PRT_TYPE_NODE* curr = (PRT_TYPE_NODE*)PrtCalloc(1, sizeof(PRT_TYPE_NODE));
		curr->type = value;
		curr->nextNode = NULL;
		*head = curr;
	}
	else
	{
		while (curr->nextNode != NULL)
		{
			curr = curr->nextNode;
		}
		curr->nextNode = (PRT_TYPE_NODE*)PrtCalloc(1, sizeof(PRT_TYPE_NODE));
		curr->nextNode->type = value;
		curr->nextNode->nextNode = NULL;
	}
}

PRT_TYPE*
PrtDistSerializeType(
__in PRT_TYPE* type
)
{
	PRT_TYPE_KIND kind = type->typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_NULL:
		return PrtMkPrimitiveType(kind);
	case PRT_KIND_FORGN:
	{
		PrtAssert(PRT_FALSE, "Invalid foreign type");
		return PrtMkPrimitiveType(PRT_KIND_NULL);
	}
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = type->typeUnion.map;
		return PrtMkMapType(PrtDistSerializeType(mtype->domType), PrtDistSerializeType(mtype->codType));
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = type->typeUnion.nmTuple;
		PRT_TYPE* clone = PrtMkNmdTupType(ntype->arity);
		for (i = 0; i < ntype->arity; ++i)
		{
			InsertStringNode(&clone->typeUnion.nmTuple->fieldNamesSerialized, ntype->fieldNames[i]);
			InsertTypeNode(&clone->typeUnion.nmTuple->fieldTypesSerialized, PrtDistSerializeType(ntype->fieldTypes[i]));
		}


		return clone;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = type->typeUnion.seq;
		return PrtMkSeqType(PrtDistSerializeType(stype->innerType));
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = type->typeUnion.tuple;
		PRT_TYPE* clone = PrtMkTupType(ttype->arity);
		for (i = 0; i < ttype->arity; ++i)
		{
			InsertTypeNode(&clone->typeUnion.tuple->fieldTypesSerialized, PrtDistSerializeType(ttype->fieldTypes[i]));
		}

		return clone;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return PrtMkPrimitiveType(PRT_KIND_NULL);
	}
}

PRT_VALUE*
PrtDistSerializeValue(
__in PRT_VALUE* value
)
{
	PRT_TYPE_KIND kind = value->type->typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return NULL;
	case PRT_KIND_BOOL:

	case PRT_KIND_EVENT:

	case PRT_KIND_MACHINE:

	case PRT_KIND_INT:
		return PrtCloneValue(value);
	case PRT_KIND_FORGN:
	{
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
	case PRT_KIND_MAP:
	{
		PRT_VALUE *retVal = PrtMkDefaultValue(value->type);
		retVal->type = PrtDistSerializeType(value->type);
		PRT_MAPVALUE *mVal = value->valueUnion.map;
		PRT_MAPVALUE *cVal = retVal->valueUnion.map;
		if (mVal->capNum > 0)
		{
			//// Eagerly allocate capacity in the clone to avoid intermediate rehashings.
			PrtFree(cVal->buckets);
			cVal->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[mVal->capNum], sizeof(PRT_MAPNODE *));
			cVal->capNum = mVal->capNum;
		}
		PRT_MAPNODE *next = mVal->first;
		while (next != NULL)
		{
			PrtMapUpdateEx(retVal, PrtDistSerializeValue(next->key), PrtDistSerializeValue(next->value), PRT_FALSE);
			next = next->insertNext;
		}

		return retVal;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtDistSerializeType(value->type);
		retVal->discriminator = PRT_VALKIND_TUPLE;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type->typeUnion.nmTuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));
		cVal->valuesSerialized = NULL;
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtCloneValue(tVal->values[i]);
			InsertValueNode(&cVal->valuesSerialized, PrtDistSerializeValue(tVal->values[i]));
		}

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtDistSerializeType(value->type);
		retVal->discriminator = PRT_VALKIND_TUPLE;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type->typeUnion.tuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));
		cVal->valuesSerialized = NULL;
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtCloneValue(tVal->values[i]);
			InsertValueNode(&cVal->valuesSerialized, PrtDistSerializeValue(tVal->values[i]));
		}

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_SEQ:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtDistSerializeType(value->type);
		retVal->discriminator = PRT_VALKIND_SEQ;
		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		PRT_SEQVALUE *cVal = (PRT_SEQVALUE *)PrtCalloc(1, sizeof(PRT_SEQVALUE));
		cVal->capacity = sVal->capacity;
		cVal->size = sVal->size;
		if (sVal->capacity == 0)
		{
			cVal->values = NULL;
		}
		else
		{
			PRT_UINT32 i;
			cVal->valuesSerialized = NULL;
			cVal->values = (PRT_VALUE **)PrtCalloc(sVal->capacity, sizeof(PRT_VALUE*));
			for (i = 0; i < sVal->size; ++i)
			{
				cVal->values[i] = PrtCloneValue(sVal->values[i]);
				InsertValueNode(&cVal->valuesSerialized, PrtDistSerializeValue(sVal->values[i]));
			}
		}
		retVal->valueUnion.seq = cVal;
		return retVal;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
}

void
PrtDistExceptionHandler(
__in PRT_STATUS exception,
__in void* vcontext
)
{
	PRT_SM_CONTEXT *context = (PRT_SM_CONTEXT*)vcontext;
	PRT_STRING MachineName = context->process->program->machines[context->instanceOf].name;
	PRT_UINT32 MachineId = context->id->valueUnion.mid->machineId;

	switch (exception)
	{
	case PRT_STATUS_EVENT_UNHANDLED:
		printf(
			"<EXCEPTION> Machine %s(%d) : Unhandled Event Exception\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_EVENT_OVERFLOW:
		printf(
			"<EXCEPTION> Machine %s(%d) : MaxInstance of Event Exceeded Exception\n",
			MachineName,
			MachineId);
		break;
	case PRT_STATUS_QUEUE_OVERFLOW:
		printf(
			"<EXCEPTION> Queue Size Exceeded Max Limits in Machine %s(%d)\n",
			MachineName,
			MachineId);
		break;
	default:
		printf(
			"<EXCEPTION> Machine %s(%d) : Unknown Exception\n",
			MachineName,
			MachineId);
		break;
	}

	exit(-1);

}

void
PrtDistLogHandler(
__in PRT_STEP step,
__in void* vcontext
)
{
	static FILE *logfile = NULL;
	PRT_SM_CONTEXT_PRIV *c = (PRT_SM_CONTEXT_PRIV*) vcontext;
	PRT_STRING MachineName = c->context.process->program->machines[c->context.instanceOf].name;
	PRT_UINT32 MachineId = c->context.id->valueUnion.mid->machineId;
	PRT_STRING eventName = c->context.process->program->events[PrtPrimGetEvent(c->trigger.event)].name;
	PRT_VALUE* payloadValue;
	char fileName[100] = "PRT_PPROCESS_LOG_";
	char processId[100];
	_itoa(c->context.id->valueUnion.mid->processId.data1, processId, 10);
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
		PRT_STRING stateName = PrtGetCurrentStateDecl(c)->name;
		sprintf_s(log, 1000, "<DeleteLog> Machine %s(0x%lu) Deleted in State %s \n", MachineName, MachineId, stateName);
		break;
	}
	case PRT_STEP_ENQUEUE:
	{
		PRT_UINT32 eventIndex = PrtPrimGetEvent(c->eventQueue.events[c->eventQueue.tailIndex == 0 ? (c->eventQueue.eventsSize - 1) : (c->eventQueue.tailIndex - 1)].event);
		PRT_STRING eventName = c->context.process->program->events[eventIndex].name;
		payloadValue = (c->eventQueue.events[c->eventQueue.tailIndex == 0 ? (c->eventQueue.eventsSize - 1) : (c->eventQueue.tailIndex - 1)].payload);
		sprintf_s(log, 1000, "<EnqueueLog> Enqueued Event < %s, %s > on Machine %s(0x%lu) \n", eventName, PrtValueToString(payloadValue), MachineName, MachineId);
		break;
	}
	case PRT_STEP_DEQUEUE:
	{
		payloadValue = (c->trigger.payload);
		sprintf_s(log, 1000, "<DequeueLog> Dequeued Event < %s, %s > by Machine %s(0x%lu) \n", eventName, PrtValueToString(payloadValue), MachineName, MachineId);
		break;
	}
	case PRT_STEP_ENTRY:
		sprintf_s(log, 1000, "<StateLog> Machine %s(0x%lu) entered state %s\n", MachineName, MachineId, PrtGetCurrentStateDecl(c)->name);
		break;
	case PRT_STEP_CREATE:
		sprintf_s(log, 1000, "<CreateLog> Machine %s(0x%lu) is created\n", MachineName, MachineId);
		break;
	case PRT_STEP_RAISE:
		payloadValue = (c->trigger.payload);
		sprintf_s(log, 1000, "<RaiseLog> Machine %s(0x%lu) raised event < %s, %s >\n", MachineName, MachineId, eventName, PrtValueToString(payloadValue));
		break;
	case PRT_STEP_POP:
		sprintf_s(log, 1000, "<PopLog> Machine %ws(0x%lu) executed POP and entered state %ws\n", MachineName, MachineId, PrtGetCurrentStateDecl(c)->name);
		break;
	case PRT_STEP_PUSH:
		sprintf_s(log, 1000, "<CallLog> Machine %s(0x%lu) did push and entered state %s\n", MachineName, MachineId, PrtGetCurrentStateDecl(c)->name);
		break;
	case PRT_STEP_UNHANDLED:
		sprintf_s(log, 1000, "<PopLog> Machine %s(0x%lu) executed POP because of unhandled event %s and entered state %ws\n", MachineName, MachineId, PrtGetCurrentStateDecl(c)->name);
		break;
	case PRT_STEP_DO:
		sprintf_s(log, 1000, "<ActionLog> Machine %s(0x%lu) Executed Action - %s \n", MachineName, MachineId, PrtGetCurrentStateDecl(c)->name);
		break;
	case PRT_STEP_EXIT:
		sprintf_s(log, 1000, "<ExitLog> Machine %s(0x%lu) exited state %ws and executing its exit function\n", MachineName, MachineId, PrtGetCurrentStateDecl(c)->name);
		break;
	default:
		sprintf_s(log, 1000, (PRT_STRING) vcontext);
		break;
	}

	PrtLockMutex(((PRT_PROCESS_PRIV*)c->context.process)->processLock);
	fputs(log, logfile);
	fflush(logfile);
	PrtUnlockMutex(((PRT_PROCESS_PRIV*)c->context.process)->processLock);
}



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
