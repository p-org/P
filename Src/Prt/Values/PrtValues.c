#include "PrtValues.h"

PRT_PRIMVALUE *PrtMkBoolValue(_In_ PRT_BOOLEAN value)
{
	PRT_PRIMVALUE *primVal;
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtMkPrimitiveType(PRT_KIND_BOOL);
	primVal->value.bl = value;
	return primVal;
}

PRT_PRIMVALUE *PrtMkEventValue(_In_ PRT_UINT32 value)
{
	PRT_PRIMVALUE *primVal;
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtMkPrimitiveType(PRT_KIND_EVENT);
	primVal->value.ev = value;
	return primVal;
}

PRT_PRIMVALUE *PrtMkIntValue(_In_ PRT_INT32 value)
{
	PRT_PRIMVALUE *primVal;
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtMkPrimitiveType(PRT_KIND_INT);
	primVal->value.nt = value;
	return primVal;
}

PRT_PRIMVALUE *PrtMkIdValue(_In_ PRT_UINT32 value)
{
	PRT_PRIMVALUE *primVal;
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtMkPrimitiveType(PRT_KIND_ID);
	primVal->value.id = value;
	return primVal;
}

PRT_PRIMVALUE *PrtMkMIdValue(_In_ PRT_UINT32 value)
{
	PRT_PRIMVALUE *primVal;
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtMkPrimitiveType(PRT_KIND_MID);
	primVal->value.md = value;
	return primVal;
}

PRT_FORGNVALUE *PrtMkForeignValue(_In_ PRT_FORGNTYPE *type, _In_ void *value)
{
	PRT_FORGNVALUE *forgnVal;
	PrtAssert(type->typeKind == PRT_KIND_FORGN, "Did not receive foreign type.");
	forgnVal = (PRT_FORGNVALUE *)PrtMalloc(sizeof(PRT_FORGNVALUE));
	forgnVal->type = PrtCloneType((PRT_TYPE)type);
	forgnVal->value = type->cloner(type->typeTag, value);
	return forgnVal;
}

PRT_VALUE PrtMkDefaultValue(_In_ PRT_TYPE type)
{
	PRT_TYPE_KIND kind = *type;
	switch (kind)
	{
	case PRT_KIND_ANY:
		return (PRT_VALUE)PrtMkIdValue(PRT_NULL_ID);
	case PRT_KIND_BOOL:
		return (PRT_VALUE)PrtMkBoolValue(PRT_FALSE);
	case PRT_KIND_EVENT:
		return (PRT_VALUE)PrtMkEventValue(PRT_NULL_ID);
	case PRT_KIND_ID:
		return (PRT_VALUE)PrtMkIdValue(PRT_NULL_ID);
	case PRT_KIND_INT:
		return (PRT_VALUE)PrtMkIntValue(0);
	case PRT_KIND_MID:
		return (PRT_VALUE)PrtMkMIdValue(PRT_NULL_ID);
	case PRT_KIND_FORGN:
	{
		PRT_FORGNVALUE *forgnVal;
		forgnVal = (PRT_FORGNVALUE *)PrtMalloc(sizeof(PRT_FORGNVALUE));
		forgnVal->type = (PRT_TYPE)PrtMkAbsentType();
		forgnVal->value = NULL;
		return (PRT_VALUE)forgnVal;
	}
	case PRT_KIND_MAP:
	{
		PRT_MAPVALUE *mapVal;
		mapVal = (PRT_MAPVALUE *)PrtMalloc(sizeof(PRT_MAPVALUE));
		mapVal->type = PrtCloneType(type);
		mapVal->size = 0;
		mapVal->nBuckets = 0;
		mapVal->buckets = NULL;
		mapVal->first = NULL;
		return (PRT_VALUE)mapVal;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tupVal;
		PRT_NMDTUPTYPE *ntype = (PRT_NMDTUPTYPE *)type;
		tupVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		tupVal->type = PrtCloneType(type);
		tupVal->values = (PRT_VALUE *)PrtCalloc(ntype->arity, sizeof(PRT_VALUE));
		for (i = 0; i < ntype->arity; ++i)
		{
			tupVal->values[i] = PrtMkDefaultValue(ntype->fieldTypes[i]);
		}

		return (PRT_VALUE)tupVal;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQVALUE *seqVal;
		seqVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
		seqVal->type = PrtCloneType(type);
		seqVal->size = 0;
		seqVal->capacity = 0;
		seqVal->values = NULL;

		return (PRT_VALUE)seqVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tupVal;
		PRT_TUPTYPE *ttype = (PRT_TUPTYPE *)type;
		tupVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		tupVal->type = PrtCloneType(type);
		tupVal->values = (PRT_VALUE *)PrtCalloc(ttype->arity, sizeof(PRT_VALUE));
		for (i = 0; i < ttype->arity; ++i)
		{
			tupVal->values[i] = PrtMkDefaultValue(ttype->fieldTypes[i]);
		}

		return (PRT_VALUE)tupVal;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
}

void PrtPrimSetBool(_Inout_ PRT_PRIMVALUE *prmVal, _In_ PRT_BOOLEAN value)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_BOOL, "Invalid type on primitive set");
	prmVal->value.bl = value;
}

PRT_BOOLEAN PrtPrimGetBool(_In_ PRT_PRIMVALUE *prmVal)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_BOOL, "Invalid type on primitive get");
	return prmVal->value.bl;
}

void PrtPrimSetEvent(_Inout_ PRT_PRIMVALUE *prmVal, _In_ PRT_UINT32 value)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_EVENT, "Invalid type on primitive set");
	prmVal->value.ev = value;
}

PRT_UINT32 PrtPrimGetEvent(_In_ PRT_PRIMVALUE *prmVal)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_EVENT, "Invalid type on primitive get");
	return prmVal->value.ev;
}

void PrtPrimSetInt(_Inout_ PRT_PRIMVALUE *prmVal, _In_ PRT_INT32 value)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_INT, "Invalid type on primitive set");
	prmVal->value.nt = value;
}

PRT_INT32 PrtPrimGetInt(_In_ PRT_PRIMVALUE *prmVal)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_INT, "Invalid type on primitive get");
	return prmVal->value.nt;
}

void PrtPrimSetId(_Inout_ PRT_PRIMVALUE *prmVal, _In_ PRT_UINT32 value)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_ID, "Invalid type on primitive set");
	prmVal->value.id = value;
}

PRT_UINT32 PrtPrimGetId(_In_ PRT_PRIMVALUE *prmVal)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_ID, "Invalid type on primitive get");
	return prmVal->value.id;
}

void PrtPrimSetMId(_Inout_ PRT_PRIMVALUE *prmVal, _In_ PRT_UINT32 value)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_MID, "Invalid type on primitive set");
	prmVal->value.md = value;
}

PRT_UINT32 PrtPrimGetMId(_In_ PRT_PRIMVALUE *prmVal)
{
	PrtAssert(*(prmVal->type) == PRT_KIND_MID, "Invalid type on primitive get");
	return prmVal->value.md;
}

void PrtTupleSet(_Inout_ PRT_TUPVALUE *tuple, _In_ PRT_UINT32 index, _In_ PRT_VALUE value)
{
	//// Eager dereferencing of inputs to check pointer validity
	PrtAssert(*(tuple->type) == PRT_KIND_TUPLE || *(tuple->type) == PRT_KIND_NMDTUP, "Cannot perform tuple set on this value");
	PrtAssert(**value >= 0 && **value < PRT_TYPE_KIND_COUNT, "Invalid value");

	PRT_UINT32 arity;
	PRT_TYPE *fieldTypes;
	PRT_VALUE clone;
	if (*(tuple->type) == PRT_KIND_TUPLE)
	{
		PRT_TUPTYPE *ttype = (PRT_TUPTYPE *)tuple->type;
		arity = ttype->arity;
		fieldTypes = ttype->fieldTypes;
	}
	else if (*(tuple->type) == PRT_KIND_NMDTUP)
	{
		PRT_NMDTUPTYPE *ntype = (PRT_NMDTUPTYPE *)tuple->type;
		arity = ntype->arity;
		fieldTypes = ntype->fieldTypes;
	}
	else 
	{
		PRT_DBG_ASSERT(PRT_FALSE, "impossible");
		return;
	}

	PrtAssert(index < arity, "Invalid tuple index");
	PrtAssert(PrtIsSubtype(*value, fieldTypes[index]), "Invalid type on tuple set");

	clone = PrtCloneValue(value);
	PrtFreeValue(tuple->values[index]);
	tuple->values[index] = clone;
}

PRT_VALUE PrtTupleGet(_In_ PRT_TUPVALUE *tuple, _In_ PRT_UINT32 index)
{
	//// Eager dereferencing of inputs to check pointer validity
	PrtAssert(*(tuple->type) == PRT_KIND_TUPLE || *(tuple->type) == PRT_KIND_NMDTUP, "Cannot perform tuple get on this value");

	PRT_UINT32 arity;
	if (*(tuple->type) == PRT_KIND_TUPLE)
	{
		PRT_TUPTYPE *ttype = (PRT_TUPTYPE *)tuple->type;
		arity = ttype->arity;
	}
	else if (*(tuple->type) == PRT_KIND_NMDTUP)
	{
		PRT_NMDTUPTYPE *ntype = (PRT_NMDTUPTYPE *)tuple->type;
		arity = ntype->arity;
	}
	else
	{
		PRT_DBG_ASSERT(PRT_FALSE, "impossible");
		return NULL;
	}

	PrtAssert(index < arity, "Invalid tuple index");
	return PrtCloneValue(tuple->values[index]);
}

void PrtNmdTupleSet(_Inout_ PRT_TUPVALUE *tuple, _In_ PRT_STRING name, _In_ PRT_VALUE value)
{
	//// Eager dereferencing of inputs to check pointer validity
	PrtAssert(*(tuple->type) == PRT_KIND_NMDTUP, "Cannot perform tuple set on this value");
	PrtAssert(name != NULL && name[0] != '\0', "Invalid field name");
	PrtAssert(**value >= 0 && **value < PRT_TYPE_KIND_COUNT, "Invalid value");

	PRT_NMDTUPTYPE *type = (PRT_NMDTUPTYPE *)tuple->type;
	PRT_UINT32 arity = type->arity;
	PRT_TYPE *fieldTypes = type->fieldTypes;
	PRT_STRING *fieldNames = type->fieldNames;
	PRT_VALUE clone;

	PRT_UINT32 index;
	for (index = 0; index < arity; ++index)
	{
		if (strncmp(fieldNames[index], name, PRT_MAXFLDNAME_LENGTH) == 0)
		{
			break;
		}
	}

	PrtAssert(index < arity, "Invalid tuple field name");
	PrtAssert(PrtIsSubtype(*value, fieldTypes[index]), "Invalid type on tuple set");
	clone = PrtCloneValue(value);
	PrtFreeValue(tuple->values[index]);
	tuple->values[index] = clone;
}

PRT_VALUE PrtNmdTupleGet(_In_ PRT_TUPVALUE *tuple, _In_ PRT_STRING name)
{
	//// Eager dereferencing of inputs to check pointer validity
	PrtAssert(*(tuple->type) == PRT_KIND_NMDTUP, "Cannot perform tuple set on this value");
	PrtAssert(name != NULL && name[0] != '\0', "Invalid field name");

	PRT_NMDTUPTYPE *type = (PRT_NMDTUPTYPE *)tuple->type;
	PRT_UINT32 arity = type->arity;
	PRT_STRING *fieldNames = type->fieldNames;

	PRT_UINT32 index;
	for (index = 0; index < arity; ++index)
	{
		if (strncmp(fieldNames[index], name, PRT_MAXFLDNAME_LENGTH) == 0)
		{
			break;
		}
	}

	PrtAssert(index < arity, "Invalid tuple field name");
	return PrtCloneValue(tuple->values[index]);
}

void PrtSeqUpdate(_Inout_ PRT_SEQVALUE *seq, _In_ PRT_UINT32 index, _In_ PRT_VALUE value)
{
	PRT_SEQTYPE *seqType;
	PRT_VALUE clone;
	PrtAssert(*(seq->type) == PRT_KIND_SEQ, "Invalid value");
	PrtAssert(index >= 0 && index < seq->size, "Invalid index");
	PrtAssert(*(*value) >= 0 && *(*value) < PRT_TYPE_KIND_COUNT, "Invalid value");
	seqType = (PRT_SEQTYPE *)seq->type;
	PrtAssert(PrtIsSubtype(*value, seqType->innerType), "Invalid type on sequence update");
	clone = PrtCloneValue(value);
	PrtFreeValue(seq->values[index]);
	seq->values[index] = clone;
}

void PrtSeqInsert(_Inout_ PRT_SEQVALUE *seq, _In_ PRT_UINT32 index, _In_ PRT_VALUE value)
{
	PRT_VALUE clone;
	PRT_SEQTYPE *seqType;
	PrtAssert(*(seq->type) == PRT_KIND_SEQ, "Invalid value");
	PrtAssert(index >= 0 && index <= seq->size, "Invalid index");
	PrtAssert(*(*value) >= 0 && *(*value) < PRT_TYPE_KIND_COUNT, "Invalid value");
	seqType = (PRT_SEQTYPE *)seq->type;
	PrtAssert(PrtIsSubtype(*value, seqType->innerType), "Invalid type on sequence update");

	clone = PrtCloneValue(value);
	if (seq->capacity == 0)
	{
		seq->values = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		seq->values[0] = clone;
		seq->capacity = 1;
	}
	else if (seq->size < seq->capacity)
	{
		PRT_UINT32 i;
		PRT_VALUE *values = seq->values;
		if (seq->size > 0)
		{
			for (i = seq->size - 1; i >= index; --i)
			{
				values[i + 1] = values[i];
				if (i == 0)
				{
					break;
				}
			}
		}

		values[index] = clone;
	}
	else 
	{
		PRT_UINT32 i;
		PRT_VALUE *values;
		seq->capacity = 2 * seq->capacity;
		values = (PRT_VALUE *)PrtCalloc(seq->capacity, sizeof(PRT_VALUE));
		for (i = 0; i < seq->size; ++i)
		{
			if (i < index)
			{
				values[i] = seq->values[i];
			}
			else 
			{
				values[i + 1] = seq->values[i];
			}
		}

		values[index] = clone;
		PrtFree(seq->values);
		seq->values = values;
	}

	seq->size = seq->size + 1;
}

void PrtSeqRemove(_Inout_ PRT_SEQVALUE *seq, _In_ PRT_UINT32 index)
{
	PRT_UINT32 i;
	PRT_VALUE *values;
	PrtAssert(*(seq->type) == PRT_KIND_SEQ, "Invalid value");
	PrtAssert(index >= 0 && index < seq->size, "Invalid index");
	values = seq->values;
	PrtFreeValue(values[index]);
	for (i = index; i < seq->size - 1; ++i)
	{
		values[i] = values[i + 1];
	}

	seq->size = seq->size - 1;
}

PRT_VALUE PrtSeqGet(_In_ PRT_SEQVALUE *seq, _In_ PRT_UINT32 index)
{
	PrtAssert(*(seq->type) == PRT_KIND_SEQ, "Invalid value");
	PrtAssert(index >= 0 && index < seq->size, "Invalid index");
	return PrtCloneValue(seq->values[index]);
}

PRT_UINT32 PrtSeqSizeOf(_In_ PRT_SEQVALUE *seq)
{
	PrtAssert(*(seq->type) == PRT_KIND_SEQ, "Invalid value");
	return seq->size;
}

PRT_VALUE PrtCloneValue(_In_ PRT_VALUE value)
{
	PRT_TYPE_KIND kind = **value;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return NULL;
	case PRT_KIND_BOOL:
	{
		PRT_PRIMVALUE *pVal = (PRT_PRIMVALUE *)value;
		return (PRT_VALUE)PrtMkBoolValue(pVal->value.bl);
	}
	case PRT_KIND_EVENT:
	{
		PRT_PRIMVALUE *pVal = (PRT_PRIMVALUE *)value;
		return (PRT_VALUE)PrtMkEventValue(pVal->value.ev);
	}
	case PRT_KIND_ID:
	{
		PRT_PRIMVALUE *pVal = (PRT_PRIMVALUE *)value;
		return (PRT_VALUE)PrtMkIdValue(pVal->value.id);
	}
	case PRT_KIND_INT:
	{
		PRT_PRIMVALUE *pVal = (PRT_PRIMVALUE *)value;
		return (PRT_VALUE)PrtMkIntValue(pVal->value.nt);
	}
	case PRT_KIND_MID:
	{
		PRT_PRIMVALUE *pVal = (PRT_PRIMVALUE *)value;
		return (PRT_VALUE)PrtMkMIdValue(pVal->value.md);
	}
	case PRT_KIND_FORGN:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return NULL;
	}
	case PRT_KIND_MAP:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return NULL;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = (PRT_TUPVALUE *)value;
		PRT_UINT32 arity = ((PRT_NMDTUPTYPE *)tVal->type)->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		cVal->type = PrtCloneType(tVal->type);
		cVal->values = (PRT_VALUE *)PrtCalloc(arity, sizeof(PRT_VALUE));
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtCloneValue(tVal->values[i]);
		}

		return (PRT_VALUE)cVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = (PRT_TUPVALUE *)value;
		PRT_UINT32 arity = ((PRT_TUPTYPE *)tVal->type)->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		cVal->type = PrtCloneType(tVal->type);
		cVal->values = (PRT_VALUE *)PrtCalloc(arity, sizeof(PRT_VALUE));
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtCloneValue(tVal->values[i]);
		}

		return (PRT_VALUE)cVal;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQVALUE *sVal = (PRT_SEQVALUE *)value;
		PRT_SEQVALUE *cVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
		cVal->type = PrtCloneType(sVal->type);
		cVal->capacity = sVal->capacity;
		cVal->size = sVal->size;
		if (sVal->capacity == 0)
		{
			cVal->values = NULL;
		}
		else 
		{
			PRT_UINT32 i;
			cVal->values = (PRT_VALUE *)PrtCalloc(sVal->capacity, sizeof(PRT_VALUE));
			for (i = 0; i < sVal->size; ++i)
			{
				cVal->values[i] = PrtCloneValue(sVal->values[i]);
			}
		}

		return (PRT_VALUE)cVal;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
}

void PrtFreeValue(_Inout_ PRT_VALUE value)
{

}