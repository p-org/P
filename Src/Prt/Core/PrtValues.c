#include "PrtValues.h"

/** Maximum load factor before hashtable is resized. */
#define PRT_MAXHASHLOAD 0.75

/** A 32-bit prime modulus for AC composition of hashcodes */
#define PRT_HASH_AC_COMPOSEMOD 4294967291

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

/** The null machine id */
const PRT_MACHINEID PrtNullMachineId = { { 0, 0, 0, 0 }, PRT_SPECIAL_EVENT_NULL };

PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeFieldName(_In_ PRT_STRING name)
{
	PRT_UINT32 i;
	PRT_UINT32 code = 0;
	PRT_UINT32 len = strnlen(name, PRT_MAXFLDNAME_LENGTH);
	for (i = 0; i < len; ++i)
	{
		code += name[i];
		code += (code << 10);
		code ^= (code >> 6);
	}

	code += (code << 3);
	code ^= (code >> 11);
	code += (code << 15);
	return code;
}

PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeUInt32(_In_ PRT_UINT32 value)
{
	PRT_UINT32 i;
	PRT_UINT32 code = 0;
	for (i = 0; i < 4; ++i)
	{
		code += (value & 0x000000FF);
		code += (code << 10);
		code ^= (code >> 6);
		value = (value >> 8);
	}

	code += (code << 3);
	code ^= (code >> 11);
	code += (code << 15);
	return code;
}

PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeTwoUInt32(_In_ PRT_UINT32 value1, _In_ PRT_UINT32 value2)
{
	PRT_UINT32 i;
	PRT_UINT32 code = 0;
	for (i = 0; i < 4; ++i)
	{
		code += (value1 & 0x000000FF);
		code += (code << 10);
		code ^= (code >> 6);
		value1 = (value1 >> 8);
	}

	for (i = 0; i < 4; ++i)
	{
		code += (value2 & 0x000000FF);
		code += (code << 10);
		code ^= (code >> 6);
		value2 = (value2 >> 8);
	}

	code += (code << 3);
	code ^= (code >> 11);
	code += (code << 15);
	return code;
}

PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeMachineId(_In_ PRT_MACHINEID id)
{
	PRT_UINT32 i;
	PRT_UINT32 code = 0;

	PRT_UINT32 value = id.processId.data1;
	for (i = 0; i < 4; ++i)
	{
		code += (value & 0x000000FF);
		code += (code << 10);
		code ^= (code >> 6);
		value = (value >> 8);
	}

	value = id.processId.data2;
	for (i = 0; i < 4; ++i)
	{
		code += (value & 0x000000FF);
		code += (code << 10);
		code ^= (code >> 6);
		value = (value >> 8);
	}

	value = id.processId.data3;
	for (i = 0; i < 4; ++i)
	{
		code += (value & 0x000000FF);
		code += (code << 10);
		code ^= (code >> 6);
		value = (value >> 8);
	}

	//// Hash lower-order bits
	value = (PRT_UINT32)id.processId.data4;
	for (i = 0; i < 4; ++i)
	{
		code += (value & 0x000000FF);
		code += (code << 10);
		code ^= (code >> 6);
		value = (value >> 8);
	}

	//// Hash higher-order bits
	value = (PRT_UINT32)(id.processId.data4 >> 32);
	for (i = 0; i < 4; ++i)
	{
		code += (value & 0x000000FF);
		code += (code << 10);
		code ^= (code >> 6);
		value = (value >> 8);
	}

	value = id.machineId;
	for (i = 0; i < 4; ++i)
	{
		code += (value & 0x000000FF);
		code += (code << 10);
		code ^= (code >> 6);
		value = (value >> 8);
	}

	code += (code << 3);
	code ^= (code >> 11);
	code += (code << 15);
	return code;
}

PRT_VALUE * PRT_CALL_CONV PrtMkBoolValue(_In_ PRT_BOOLEAN value)
{
	PrtAssert(value == PRT_TRUE || value == PRT_FALSE, "Expected a bool value");

	PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALKIND_BOOL;
	retVal->valueUnion.bl = value;
	return retVal;
}

PRT_VALUE * PRT_CALL_CONV PrtMkEventValue(_In_ PRT_UINT32 value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALKIND_EVENT;
	retVal->valueUnion.ev = value;
	return retVal;
}

PRT_VALUE * PRT_CALL_CONV PrtMkIntValue(_In_ PRT_INT32 value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALKIND_INT;
	retVal->valueUnion.nt = value;
	return retVal;
}

PRT_VALUE * PRT_CALL_CONV PrtMkNullValue()
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALKIND_NULL;
	retVal->valueUnion.ev = PRT_SPECIAL_EVENT_NULL;
	return retVal;
}

PRT_VALUE * PRT_CALL_CONV PrtMkMachineValue(_In_ PRT_MACHINEID value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	PRT_MACHINEID *id = (PRT_MACHINEID *)PrtMalloc(sizeof(PRT_MACHINEID));
	retVal->discriminator = PRT_VALKIND_MID;
	retVal->valueUnion.mid = id;
	id->machineId = value.machineId;
	id->processId.data1 = value.processId.data1;
	id->processId.data2 = value.processId.data2;
	id->processId.data3 = value.processId.data3;
	id->processId.data4 = value.processId.data4;
	return retVal;
}

PRT_UINT64 MkDefaultForeignValue(_In_ PRT_UINT16 typeTag)
{
	return 0;
}

PRT_UINT64 CloneForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal)
{
	return frgnVal;
}

void FreeForeignValue(_In_ PRT_UINT16 typeTag, _Inout_ PRT_UINT64 frgnVal)
{
}

PRT_UINT32 HashForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal)
{
	return (PRT_UINT32)frgnVal;
}

PRT_STRING ToStringForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal)
{
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 15);
	strcpy_s(str, 15, "foreign_val");
	return str;
}

PRT_BOOLEAN IsEqualForeignValue(_In_ PRT_UINT16 typeTag1, _In_ PRT_UINT64 frgnVal1, _In_ PRT_UINT16 typeTag2, _In_ PRT_UINT64 frgnVal2)
{
	return typeTag1 == typeTag2 && frgnVal1 == frgnVal2;
}

PRT_FORGN_MKDEF PrtMkDefaultForeignValue = &MkDefaultForeignValue;
PRT_FORGN_CLONE PrtCloneForeignValue = &CloneForeignValue;
PRT_FORGN_FREE PrtFreeForeignValue = &FreeForeignValue;
PRT_FORGN_GETHASHCODE PrtHashForeignValue = &HashForeignValue;
PRT_FORGN_ISEQUAL PrtIsEqualForeignValue = &IsEqualForeignValue;
PRT_FORGN_TOSTRING PrtToStringForeignValue = &ToStringForeignValue;

void PRT_CALL_CONV PrtUpdateForeignFn(
	PRT_FORGN_MKDEF MkDefault,
	PRT_FORGN_CLONE Clone,
	PRT_FORGN_FREE Free,
	PRT_FORGN_GETHASHCODE Hash,
	PRT_FORGN_ISEQUAL IsEqual,
	PRT_FORGN_TOSTRING ToString)
{
	PrtMkDefaultForeignValue = MkDefault;
	PrtCloneForeignValue = Clone;
	PrtFreeForeignValue = Free;
	PrtHashForeignValue = Hash;
	PrtIsEqualForeignValue = IsEqual;
	PrtToStringForeignValue = ToString;
}

PRT_VALUE * PRT_CALL_CONV PrtMkForeignValue(
	_In_ PRT_UINT16 typeTag,
	_In_ PRT_UINT64 value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	PRT_FORGNVALUE *frgn = (PRT_FORGNVALUE *)PrtMalloc(sizeof(PRT_FORGNVALUE));
	retVal->discriminator = PRT_VALKIND_FORGN;
	retVal->valueUnion.frgn = frgn;
	frgn->typeTag = typeTag;
	frgn->value = PrtCloneForeignValue(typeTag, value);
	return retVal;
}

PRT_UINT64 *foreignTypeToValueCounter;
PRT_UINT16 foreignTypeToValueCounterSize;
PRT_VALUE * PRT_CALL_CONV PrtMkFreshForeignValue(
	_In_ PRT_TYPE *type)
{
	PrtAssert(type->typeKind == PRT_KIND_FORGN, "Expected foreign type");
	PRT_UINT16 typeTag = type->typeUnion.typeTag;
	if (foreignTypeToValueCounterSize <= typeTag)
	{
		PRT_UINT64 *oldValueCounter = foreignTypeToValueCounter;
		PRT_UINT16 oldValueCounterSize = foreignTypeToValueCounterSize;
		foreignTypeToValueCounterSize = 2 * typeTag + 1;
		foreignTypeToValueCounter = (PRT_UINT64 *)malloc(sizeof(PRT_UINT64)* (foreignTypeToValueCounterSize));
		for (PRT_UINT16 i = 0; i < oldValueCounterSize; i++)
		{
			foreignTypeToValueCounter[i] = oldValueCounter[i];
		}
		for (PRT_UINT16 i = oldValueCounterSize; i < foreignTypeToValueCounterSize; i++)
		{
			foreignTypeToValueCounter[i] = 0;
		}
		PrtFree(oldValueCounter);
	}
	PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	PRT_FORGNVALUE *frgn = (PRT_FORGNVALUE *)PrtMalloc(sizeof(PRT_FORGNVALUE));
	retVal->discriminator = PRT_VALKIND_FORGN;
	retVal->valueUnion.frgn = frgn;
	frgn->typeTag = typeTag;
	frgn->value = foreignTypeToValueCounter[typeTag] + 1;
	foreignTypeToValueCounter[typeTag] = frgn->value;
	return retVal;
}

void PRT_CALL_CONV PrtCleanupForeignData()
{
	if (foreignTypeToValueCounter != NULL)
	{
		PrtFree(foreignTypeToValueCounter);
	}
}

PRT_VALUE * PRT_CALL_CONV PrtMkDefaultValue(_In_ PRT_TYPE *type)
{
	PrtAssert(PrtIsValidType(type), "Invalid type expression.");
	PRT_TYPE_KIND kind = type->typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		return PrtMkNullValue();
	case PRT_KIND_BOOL:
		return PrtMkBoolValue(PRT_FALSE);
	case PRT_KIND_EVENT:
		return PrtMkEventValue(PRT_SPECIAL_EVENT_NULL);
	case PRT_KIND_MACHINE:
		return PrtMkMachineValue(PrtNullMachineId);
	case PRT_KIND_INT:
		return PrtMkIntValue(0);
	case PRT_KIND_NULL:
		return PrtMkNullValue();
	case PRT_KIND_FORGN:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_FORGNVALUE *frgn = (PRT_FORGNVALUE *)PrtMalloc(sizeof(PRT_FORGNVALUE));
		retVal->discriminator = PRT_VALKIND_FORGN;
		retVal->valueUnion.frgn = frgn;
		frgn->typeTag = type->typeUnion.typeTag;
		frgn->value = PrtMkDefaultForeignValue(type->typeUnion.typeTag);
		return retVal;
	}
	case PRT_KIND_MAP:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_MAPVALUE *map = (PRT_MAPVALUE *)PrtMalloc(sizeof(PRT_MAPVALUE));
		retVal->discriminator = PRT_VALKIND_MAP;
		retVal->valueUnion.map = map;

		map->size = 0;
		map->capNum = 0;
		map->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[0], sizeof(PRT_MAPNODE *));
		map->first = NULL;
		map->last = NULL;
		return retVal;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_TUPVALUE *tup = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		retVal->discriminator = PRT_VALKIND_TUPLE;
		retVal->valueUnion.tuple = tup;
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = type->typeUnion.nmTuple;
		tup->size = ntype->arity;
		tup->values = (PRT_VALUE **)PrtCalloc(ntype->arity, sizeof(PRT_VALUE*));
		for (i = 0; i < ntype->arity; ++i)
		{
			tup->values[i] = PrtMkDefaultValue(ntype->fieldTypes[i]);
		}

		return retVal;
	}
	case PRT_KIND_SEQ:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_SEQVALUE *seq = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
		retVal->discriminator = PRT_VALKIND_SEQ;
		retVal->valueUnion.seq = seq;

		seq->size = 0;
		seq->capacity = 0;
		seq->values = NULL;
		return retVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_TUPVALUE *tup = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		retVal->discriminator = PRT_VALKIND_TUPLE;
		retVal->valueUnion.tuple = tup;
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = type->typeUnion.tuple;
		tup->size = ttype->arity;
		tup->values = (PRT_VALUE **)PrtCalloc(ttype->arity, sizeof(PRT_VALUE*));
		for (i = 0; i < ttype->arity; ++i)
		{
			tup->values[i] = PrtMkDefaultValue(ttype->fieldTypes[i]);
		}

		return retVal;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtMkDefaultValue: Invalid type");
		return NULL;
	}
}

void PRT_CALL_CONV PrtPrimSetBool(_Inout_ PRT_VALUE *prmVal, _In_ PRT_BOOLEAN value)
{
	PrtAssert(value == PRT_TRUE || value == PRT_FALSE, "Expected a bool value");
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALKIND_BOOL, "Invalid type on primitive set");
	prmVal->valueUnion.bl = value;
}

PRT_BOOLEAN PRT_CALL_CONV PrtPrimGetBool(_In_ PRT_VALUE *prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALKIND_BOOL, "Invalid type on primitive get");
	return prmVal->valueUnion.bl;
}

void PRT_CALL_CONV PrtPrimSetEvent(_Inout_ PRT_VALUE *prmVal, _In_ PRT_UINT32 value)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALKIND_EVENT, "Invalid type on primitive set");
	prmVal->valueUnion.ev = value;
}

PRT_UINT32 PRT_CALL_CONV PrtPrimGetEvent(_In_ PRT_VALUE *prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALKIND_EVENT, "Invalid type on primitive get");
	return prmVal->valueUnion.ev;
}

void PRT_CALL_CONV PrtPrimSetInt(_Inout_ PRT_VALUE *prmVal, _In_ PRT_INT32 value)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALKIND_INT, "Invalid type on primitive set");
	prmVal->valueUnion.nt = value;
}

PRT_INT32 PRT_CALL_CONV PrtPrimGetInt(_In_ PRT_VALUE *prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALKIND_INT, "Invalid type on primitive get");
	return prmVal->valueUnion.nt;
}

void PRT_CALL_CONV PrtPrimSetMachine(_Inout_ PRT_VALUE *prmVal, _In_ PRT_MACHINEID value)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALKIND_MID, "Invalid type on primitive set");
	PRT_MACHINEID *id = prmVal->valueUnion.mid;
	id->machineId = value.machineId;
	id->processId.data1 = value.processId.data1;
	id->processId.data2 = value.processId.data2;
	id->processId.data3 = value.processId.data3;
	id->processId.data4 = value.processId.data4;
}

PRT_MACHINEID PRT_CALL_CONV PrtPrimGetMachine(_In_ PRT_VALUE *prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALKIND_MID, "Invalid type on primitive get");
	return *prmVal->valueUnion.mid;
}

void PRT_CALL_CONV PrtTupleSetEx(_Inout_ PRT_VALUE *tuple, _In_ PRT_UINT32 index, _In_ PRT_VALUE *value, PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(tuple), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(tuple->discriminator == PRT_VALKIND_TUPLE, "Cannot perform tuple set on this value");
	PrtAssert(0 <= index && index < tuple->valueUnion.tuple->size, "Invalid tuple index");

	PrtFreeValue(tuple->valueUnion.tuple->values[index]);
	tuple->valueUnion.tuple->values[index] = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
}

void PRT_CALL_CONV PrtTupleSet(_Inout_ PRT_VALUE *tuple, _In_ PRT_UINT32 index, _In_ PRT_VALUE *value)
{
	PrtTupleSetEx(tuple, index, value, PRT_TRUE);
}

PRT_VALUE * PRT_CALL_CONV PrtTupleGet(_In_ PRT_VALUE *tuple, _In_ PRT_UINT32 index)
{
	PrtAssert(PrtIsValidValue(tuple), "Invalid value expression.");
	PrtAssert(tuple->discriminator == PRT_VALKIND_TUPLE, "Cannot perform tuple get on this value");
	PrtAssert(0 <= index && index < tuple->valueUnion.tuple->size, "Invalid tuple index");

	return PrtCloneValue(tuple->valueUnion.tuple->values[index]);
}

PRT_VALUE * PRT_CALL_CONV PrtTupleGetNC(_In_ PRT_VALUE *tuple, _In_ PRT_UINT32 index)
{
	PrtAssert(PrtIsValidValue(tuple), "Invalid value expression.");
	PrtAssert(tuple->discriminator == PRT_VALKIND_TUPLE, "Cannot perform tuple get on this value");
	PrtAssert(0 <= index && index < tuple->valueUnion.tuple->size, "Invalid tuple index");

	return tuple->valueUnion.tuple->values[index];
}

void PRT_CALL_CONV PrtSeqUpdateEx(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index, _In_ PRT_VALUE *value, PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALKIND_SEQ, "Invalid value");
	PrtAssert(index->discriminator == PRT_VALKIND_INT, "Invalid value");
	PrtAssert(0 <= index->valueUnion.nt && (PRT_UINT32)index->valueUnion.nt < seq->valueUnion.seq->size, "Invalid index");

	PrtFreeValue(seq->valueUnion.seq->values[index->valueUnion.nt]);
	seq->valueUnion.seq->values[index->valueUnion.nt] = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
}

void PRT_CALL_CONV PrtSeqUpdate(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index, _In_ PRT_VALUE *value)
{
	PrtSeqUpdateEx(seq, index, value, PRT_TRUE);
}

void PRT_CALL_CONV PrtSeqInsertEx(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index, _In_ PRT_VALUE* value, PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALKIND_SEQ, "Invalid value");
	PrtAssert(index->discriminator == PRT_VALKIND_INT, "Invalid value");
	PrtAssert(0 <= index->valueUnion.nt && (PRT_UINT32)index->valueUnion.nt <= seq->valueUnion.seq->size, "Invalid index");

	PRT_VALUE *clone;
	clone = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
	if (seq->valueUnion.seq->capacity == 0)
	{
		seq->valueUnion.seq->values = (PRT_VALUE **)PrtMalloc(sizeof(PRT_VALUE*));
		seq->valueUnion.seq->values[0] = clone;
		seq->valueUnion.seq->capacity = 1;
	}
	else if (seq->valueUnion.seq->size < seq->valueUnion.seq->capacity)
	{
		PRT_UINT32 i;
		PRT_VALUE **values = seq->valueUnion.seq->values;
		if (seq->valueUnion.seq->size > 0)
		{
			for (i = seq->valueUnion.seq->size - 1; i >= (PRT_UINT32)index->valueUnion.nt; --i)
			{
				values[i + 1] = values[i];
				if (i == 0)
				{
					break;
				}
			}
		}

		values[index->valueUnion.nt] = clone;
	}
	else
	{
		PRT_UINT32 i;
		PRT_VALUE **values;
		seq->valueUnion.seq->capacity = 2 * seq->valueUnion.seq->capacity;
		values = (PRT_VALUE **)PrtCalloc(seq->valueUnion.seq->capacity, sizeof(PRT_VALUE*));
		for (i = 0; i < seq->valueUnion.seq->size; ++i)
		{
			if (i < (PRT_UINT32)index->valueUnion.nt)
			{
				values[i] = seq->valueUnion.seq->values[i];
			}
			else
			{
				values[i + 1] = seq->valueUnion.seq->values[i];
			}
		}

		values[index->valueUnion.nt] = clone;
		PrtFree(seq->valueUnion.seq->values);
		seq->valueUnion.seq->values = values;
	}

	seq->valueUnion.seq->size = seq->valueUnion.seq->size + 1;
}

void PRT_CALL_CONV PrtSeqInsert(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index, _In_ PRT_VALUE* value)
{
	PrtSeqInsertEx(seq, index, value, PRT_TRUE);
}

void PRT_CALL_CONV PrtSeqRemove(_Inout_ PRT_VALUE *seq, _In_ PRT_VALUE *index)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALKIND_SEQ, "Invalid value");
	PrtAssert(index->discriminator == PRT_VALKIND_INT, "Invalid value");
	PrtAssert(0 <= index->valueUnion.nt && (PRT_UINT32)index->valueUnion.nt < seq->valueUnion.seq->size, "Invalid index");

	PRT_UINT32 i;
	PRT_VALUE **values = seq->valueUnion.seq->values;
	PRT_UINT32 seqSize = seq->valueUnion.seq->size;
	PrtFreeValue(values[index->valueUnion.nt]);
	for (i = index->valueUnion.nt; i < seqSize - 1; ++i)
	{
		values[i] = values[i + 1];
	}

	seq->valueUnion.seq->size = seq->valueUnion.seq->size - 1;
}

PRT_VALUE * PRT_CALL_CONV PrtSeqGet(_In_ PRT_VALUE *seq, _In_ PRT_VALUE *index)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALKIND_SEQ, "Invalid value");
	PrtAssert(index->discriminator == PRT_VALKIND_INT, "Invalid value");
	PrtAssert(0 <= index->valueUnion.nt && (PRT_UINT32)index->valueUnion.nt < seq->valueUnion.seq->size, "Invalid index");

	return PrtCloneValue(seq->valueUnion.seq->values[index->valueUnion.nt]);
}

PRT_VALUE * PRT_CALL_CONV PrtSeqGetNC(_In_ PRT_VALUE *seq, _In_ PRT_VALUE *index)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALKIND_SEQ, "Invalid value");
	PrtAssert(index->discriminator == PRT_VALKIND_INT, "Invalid value");
	PrtAssert(0 <= index->valueUnion.nt && (PRT_UINT32)index->valueUnion.nt < seq->valueUnion.seq->size, "Invalid index");

	return seq->valueUnion.seq->values[index->valueUnion.nt];
}

PRT_UINT32 PRT_CALL_CONV PrtSeqSizeOf(_In_ PRT_VALUE *seq)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALKIND_SEQ, "Invalid value");

	return seq->valueUnion.seq->size;
}

/** Expands the map and rehashes its key-value pairs */
void PRT_CALL_CONV PrtMapExpand(_Inout_ PRT_VALUE *map)
{
	if (map->valueUnion.map->capNum + 1 >= sizeof(PrtHashtableCapacities) / sizeof(PRT_UINT32))
	{
		//// Map has reached maximum capacity.
		return;
	}

	map->valueUnion.map->capNum = map->valueUnion.map->capNum + 1;
	//// Erase all bucket-next pointers
	PRT_MAPNODE *next = map->valueUnion.map->first;
	while (next != NULL)
	{
		next->bucketNext = NULL;
		next = next->insertNext;
	}

	//// Resize buckets
	PrtFree(map->valueUnion.map->buckets);
	map->valueUnion.map->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[map->valueUnion.map->capNum], sizeof(PRT_MAPNODE *));

	//// Do the rehash, updating the bucketNext pointers
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	next = map->valueUnion.map->first;
	while (next != NULL)
	{
		bucketNum = PrtGetHashCodeValue(next->key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
		bucket = map->valueUnion.map->buckets[bucketNum];
		if (bucket != NULL)
		{
			next->bucketNext = bucket;
		}

		map->valueUnion.map->buckets[bucketNum] = next;
		next = next->insertNext;
	}
}

void PRT_CALL_CONV PrtMapUpdateEx(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key, _In_ PRT_BOOLEAN cloneKey, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	PRT_MAPNODE *node = NULL;
	PRT_BOOLEAN isNewKey = PRT_FALSE;

	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	if (bucket == NULL)
	{
		isNewKey = PRT_TRUE;
		node = (PRT_MAPNODE *)PrtMalloc(sizeof(PRT_MAPNODE));
		node->key = cloneKey == PRT_TRUE ? PrtCloneValue(key) : key;
		node->value = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
		node->bucketNext = NULL;
		node->insertNext = NULL;
		map->valueUnion.map->buckets[bucketNum] = node;
	}
	else
	{
		PRT_VALUE *valueClone = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
		PRT_MAPNODE *next = bucket;
		isNewKey = PRT_TRUE;
		while (next != NULL)
		{
			if (PrtIsEqualValue(next->key, key))
			{
				//// Then need to free the unused key.
				if (cloneKey != PRT_TRUE)
				{
					PrtFreeValue(key);
				}

				PrtFreeValue(next->value);
				next->value = valueClone;
				isNewKey = PRT_FALSE;
				break;
			}

			next = next->bucketNext;
		}

		if (isNewKey == PRT_TRUE)
		{
			node = (PRT_MAPNODE *)PrtMalloc(sizeof(PRT_MAPNODE));
			node->key = cloneKey == PRT_TRUE ? PrtCloneValue(key) : key;
			node->value = valueClone;
			node->bucketNext = bucket;
			node->insertNext = NULL;
			map->valueUnion.map->buckets[bucketNum] = node;
		}
	}

	if (isNewKey == PRT_TRUE)
	{
		if (map->valueUnion.map->last == NULL)
		{
			map->valueUnion.map->first = node;
			map->valueUnion.map->last = node;
			node->insertPrev = NULL;
		}
		else
		{
			node->insertPrev = map->valueUnion.map->last;
			map->valueUnion.map->last->insertNext = node;
			map->valueUnion.map->last = node;
		}

		map->valueUnion.map->size = map->valueUnion.map->size + 1;

		if (((double)map->valueUnion.map->size) / ((double)PrtHashtableCapacities[map->valueUnion.map->capNum]) > ((double)PRT_MAXHASHLOAD))
		{
			PrtMapExpand(map);
		}
	}
}

void PRT_CALL_CONV PrtMapUpdate(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key, _In_ PRT_VALUE *value)
{
	PrtMapUpdateEx(map, key, PRT_TRUE, value, PRT_TRUE);
}

void PRT_CALL_CONV PrtMapInsertEx(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key, _In_ PRT_BOOLEAN cloneKey, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneValue)
{
	PrtAssert(!PrtMapExists(map, key), "key must not be in map");

	PrtMapUpdateEx(map, key, cloneKey, value, cloneValue);
}

void PRT_CALL_CONV PrtMapInsert(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key, _In_ PRT_VALUE *value)
{
	PrtMapInsertEx(map, key, PRT_TRUE, value, PRT_TRUE);
}

void PRT_CALL_CONV PrtMapRemove(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	if (bucket == NULL)
	{
		return;
	}

	PRT_MAPNODE *next = bucket;
	PRT_MAPNODE *prev = NULL;
	while (next != NULL)
	{
		if (PrtIsEqualValue(next->key, key))
		{
			PrtFreeValue(next->key);
			PrtFreeValue(next->value);
			if (next == bucket)
			{
				map->valueUnion.map->buckets[bucketNum] = next->bucketNext;
			}
			else
			{
				prev->bucketNext = next->bucketNext;
			}

			if (next->insertPrev == NULL)
			{
				//// Then this was the first key
				map->valueUnion.map->first = next->insertNext;
			}
			else
			{
				//// Otherwise the next of the previous key is the next of this key
				next->insertPrev->insertNext = next->insertNext;
			}

			if (next->insertNext == NULL)
			{
				//// Then this was the last key
				map->valueUnion.map->last = next->insertPrev;
			}
			else
			{
				//// Otherwise the previous of the next key is the previous of this key
				next->insertNext->insertPrev = next->insertPrev;
			}

			PrtFree(next);
			map->valueUnion.map->size = map->valueUnion.map->size - 1;
			return;
		}

		prev = next;
		next = next->bucketNext;
	}
}

PRT_VALUE * PRT_CALL_CONV PrtMapGet(_In_ PRT_VALUE *map, _In_ PRT_VALUE* key)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	PrtAssert(bucket != NULL, "Invalid map get; key not found");
	PRT_MAPNODE *next = bucket;
	while (next != NULL)
	{
		if (PrtIsEqualValue(next->key, key))
		{
			return PrtCloneValue(next->value);
		}

		next = next->bucketNext;
	}

	PrtAssert(PRT_FALSE, "Invalid map get; key not found");
	return NULL;
}

PRT_VALUE * PRT_CALL_CONV PrtMapGetNC(_In_ PRT_VALUE *map, _In_ PRT_VALUE* key)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	PrtAssert(bucket != NULL, "Invalid map get; key not found");
	PRT_MAPNODE *next = bucket;
	while (next != NULL)
	{
		if (PrtIsEqualValue(next->key, key))
		{
			return next->value;
		}

		next = next->bucketNext;
	}

	PrtAssert(PRT_FALSE, "Invalid map get; key not found");
	return NULL;
}

PRT_VALUE * PRT_CALL_CONV PrtMapGetKeys(_In_ PRT_VALUE *map)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
	PRT_SEQVALUE *seqVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
	retVal->discriminator = PRT_VALKIND_SEQ;
	retVal->valueUnion.seq = seqVal;

	if (map->valueUnion.map->size == 0)
	{
		seqVal->size = 0;
		seqVal->capacity = 0;
		seqVal->values = NULL;
	}
	else
	{
		seqVal->size = map->valueUnion.map->size;
		seqVal->capacity = map->valueUnion.map->size;
		seqVal->values = (PRT_VALUE **)PrtCalloc(map->valueUnion.map->size, sizeof(PRT_VALUE*));
		PRT_MAPNODE* next = map->valueUnion.map->first;
		PRT_UINT32 i = 0;
		while (next != NULL)
		{
			seqVal->values[i] = PrtCloneValue(next->key);
			++i;
			next = next->insertNext;
		}
	}
	return retVal;
}

PRT_VALUE * PRT_CALL_CONV PrtMapGetValues(_In_ PRT_VALUE *map)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
	PRT_SEQVALUE *seqVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
	retVal->discriminator = PRT_VALKIND_SEQ;
	retVal->valueUnion.seq = seqVal;

	if (map->valueUnion.map->size == 0)
	{
		seqVal->size = 0;
		seqVal->capacity = 0;
		seqVal->values = NULL;
	}
	else
	{
		seqVal->size = map->valueUnion.map->size;
		seqVal->capacity = map->valueUnion.map->size;
		seqVal->values = (PRT_VALUE **)PrtCalloc(map->valueUnion.map->size, sizeof(PRT_VALUE*));
		PRT_MAPNODE* next = map->valueUnion.map->first;
		PRT_UINT32 i = 0;
		while (next != NULL)
		{
			seqVal->values[i] = PrtCloneValue(next->value);
			++i;
			next = next->insertNext;
		}
	}

	return retVal;
}

PRT_BOOLEAN PRT_CALL_CONV PrtMapExists(_In_ PRT_VALUE *map, _In_ PRT_VALUE *key)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	if (bucket == NULL)
	{
		return PRT_FALSE;
	}

	PRT_MAPNODE *next = bucket;
	while (next != NULL)
	{
		if (PrtIsEqualValue(next->key, key))
		{
			return PRT_TRUE;
		}

		next = next->bucketNext;
	}

	return PRT_FALSE;
}

PRT_BOOLEAN PRT_CALL_CONV PrtMapIsSameMapping(_In_ PRT_VALUE *map, _In_ PRT_VALUE* key, _In_ PRT_VALUE* value)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	if (bucket == NULL)
	{
		return PRT_FALSE;
	}

	PRT_MAPNODE *next = bucket;
	while (next != NULL)
	{
		if (PrtIsEqualValue(next->key, key))
		{
			return PrtIsEqualValue(next->value, value);
		}

		next = next->bucketNext;
	}

	return PRT_FALSE;
}

PRT_UINT32 PRT_CALL_CONV PrtMapSizeOf(_In_ PRT_VALUE *map)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	return map->valueUnion.map->size;
}

PRT_UINT32 PRT_CALL_CONV PrtMapCapacity(_In_ PRT_VALUE *map)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALKIND_MAP, "Invalid value");

	return PrtHashtableCapacities[map->valueUnion.map->capNum];
}

PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeValue(_In_ PRT_VALUE* inputValue)
{
	PrtAssert(PrtIsValidValue(inputValue), "Invalid value expression.");
	if (PrtIsNullValue(inputValue))
	{
		return PrtGetHashCodeUInt32(0);
	}

	PRT_VALUE_KIND kind = inputValue->discriminator;
	switch (kind)
	{
	case PRT_VALKIND_BOOL:
		return PrtGetHashCodeUInt32(0x00400000 ^ ((PRT_UINT32)inputValue->valueUnion.bl));
	case PRT_VALKIND_EVENT:
		return PrtGetHashCodeUInt32(0x00800000 ^ (inputValue->valueUnion.ev));
	case PRT_VALKIND_MID:
		return PrtGetHashCodeUInt32(0x01000000 ^ PrtGetHashCodeMachineId(*inputValue->valueUnion.mid));
	case PRT_VALKIND_INT:
		return PrtGetHashCodeUInt32(0x02000000 ^ ((PRT_UINT32)inputValue->valueUnion.nt));
	case PRT_VALKIND_FORGN:
	{
		return 0x08000000 ^ PrtHashForeignValue(inputValue->valueUnion.frgn->typeTag, inputValue->valueUnion.frgn->value);
	}
	case PRT_VALKIND_MAP:
	{
		//// Hash function designed so two maps with same key-value pairs are hashed equally (independently of order).
		//// Hash codes are added on the finite field Z_{PRT_HASH_AC_COMPOSEMOD}.
		PRT_MAPVALUE *mVal = inputValue->valueUnion.map;
		PRT_MAPNODE *next = mVal->first;
		PRT_UINT64 code = 1;
		PRT_UINT64 pointCode;
		while (next != NULL)
		{
			pointCode = (PRT_UINT64)PrtGetHashCodeTwoUInt32(PrtGetHashCodeValue(next->key), PrtGetHashCodeValue(next->value));
			if (pointCode == 0)
			{
				pointCode = 1;
			}

			code = (code + pointCode) % PRT_HASH_AC_COMPOSEMOD;
			next = next->insertNext;
		}

		return 0x10000000 ^ (PRT_UINT32)code;
	}
	case PRT_VALKIND_SEQ:
	{
		PRT_UINT32 i;
		PRT_UINT32 j;
		PRT_UINT32 code = 0;
		PRT_UINT32 pointCode;
		PRT_SEQVALUE *sVal = inputValue->valueUnion.seq;
		for (i = 0; i < sVal->size; ++i)
		{
			pointCode = PrtGetHashCodeValue(sVal->values[i]);
			for (j = 0; j < 4; ++j)
			{
				code += (pointCode & 0x000000FF);
				code += (code << 10);
				code ^= (code >> 6);
				pointCode = (pointCode >> 8);
			}
		}

		code += (code << 3);
		code ^= (code >> 11);
		code += (code << 15);
		return 0x40000000 ^ code;
	}
	case PRT_VALKIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_UINT32 j;
		PRT_UINT32 code = 0;
		PRT_UINT32 pointCode;
		PRT_TUPVALUE *tVal = inputValue->valueUnion.tuple;
		for (i = 0; i < tVal->size; ++i)
		{
			pointCode = PrtGetHashCodeValue(tVal->values[i]);
			for (j = 0; j < 4; ++j)
			{
				code += (pointCode & 0x000000FF);
				code += (code << 10);
				code ^= (code >> 6);
				pointCode = (pointCode >> 8);
			}
		}

		code += (code << 3);
		code ^= (code >> 11);
		code += (code << 15);
		return 0x80000000 ^ code;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtGetHashCodeValue: Invalid value");
		return 0;
	}
}

PRT_BOOLEAN PRT_CALL_CONV PrtIsEqualValue(_In_ PRT_VALUE *value1, _In_ PRT_VALUE *value2)
{
	PrtAssert(PrtIsValidValue(value1), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value2), "Invalid value expression.");

	PRT_VALUE_KIND kind1 = value1->discriminator;
	PRT_VALUE_KIND kind2 = value2->discriminator;

	if (PrtIsNullValue(value1) && PrtIsNullValue(value2))
	{
		return PRT_TRUE;
	}
	else if (kind1 != kind2)
	{
		return PRT_FALSE;
	}
	else if (value1 == value2)
	{
		return PRT_TRUE;
	}

	switch (kind1)
	{
	case PRT_VALKIND_NULL:
		//// Checked for equality with null earlier.
		return PRT_FALSE;
	case PRT_VALKIND_BOOL:
		return
			value1->valueUnion.bl == value2->valueUnion.bl ? PRT_TRUE : PRT_FALSE;
	case PRT_VALKIND_EVENT:
		return
			value1->valueUnion.ev == value2->valueUnion.ev ? PRT_TRUE : PRT_FALSE;
	case PRT_VALKIND_MID:
	{
		PRT_MACHINEID *id1 = value1->valueUnion.mid;
		PRT_MACHINEID *id2 = value2->valueUnion.mid;
		return
			id1->processId.data1 == id2->processId.data1 &&
			id1->processId.data2 == id2->processId.data2 &&
			id1->processId.data3 == id2->processId.data3 &&
			id1->processId.data4 == id2->processId.data4 &&
			id1->machineId == id2->machineId ? PRT_TRUE : PRT_FALSE;
	}
	case PRT_VALKIND_INT:
		return
			value1->valueUnion.nt == value2->valueUnion.nt ? PRT_TRUE : PRT_FALSE;
	case PRT_VALKIND_FORGN:
	{
		PRT_FORGNVALUE *fVal1 = value1->valueUnion.frgn;
		PRT_FORGNVALUE *fVal2 = value2->valueUnion.frgn;
		return PrtIsEqualForeignValue(fVal1->typeTag, fVal1->value, fVal2->typeTag, fVal2->value);
	}
	case PRT_VALKIND_MAP:
	{
		PRT_MAPVALUE *mVal1 = value1->valueUnion.map;
		PRT_MAPVALUE *mVal2 = value2->valueUnion.map;

		if (mVal1->size != mVal2->size)
		{
			return PRT_FALSE;
		}

		PRT_MAPNODE *next = mVal1->first;
		while (next != NULL)
		{
			if (!PrtMapIsSameMapping(value2, next->key, next->value))
			{
				return PRT_FALSE;
			}

			next = next->insertNext;
		}

		return PRT_TRUE;
	}
	case PRT_VALKIND_SEQ:
	{
		PRT_UINT32 i;
		PRT_SEQVALUE *sVal1 = value1->valueUnion.seq;
		PRT_SEQVALUE *sVal2 = value2->valueUnion.seq;

		if (sVal1->size != sVal2->size)
		{
			return PRT_FALSE;
		}

		for (i = 0; i < sVal1->size; ++i)
		{
			if (!PrtIsEqualValue(sVal1->values[i], sVal2->values[i]))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	case PRT_VALKIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal1 = value1->valueUnion.tuple;
		PRT_TUPVALUE *tVal2 = value2->valueUnion.tuple;

		if (tVal1->size != tVal2->size)
		{
			return PRT_FALSE;
		}

		for (i = 0; i < tVal1->size; ++i)
		{
			if (!PrtIsEqualValue(tVal1->values[i], tVal2->values[i]))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtIsEqualValue: Invalid value");
		return PRT_FALSE;
	}
}

PRT_VALUE * PRT_CALL_CONV PrtCloneValue(_In_ PRT_VALUE *value)
{
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");

	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALKIND_NULL:
		return PrtMkNullValue();
	case PRT_VALKIND_BOOL:
		return PrtMkBoolValue(value->valueUnion.bl);
	case PRT_VALKIND_EVENT:
		return PrtMkEventValue(value->valueUnion.ev);
	case PRT_VALKIND_MID:
		return PrtMkMachineValue(*value->valueUnion.mid);
	case PRT_VALKIND_INT:
		return PrtMkIntValue(value->valueUnion.nt);
	case PRT_VALKIND_FORGN:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		PRT_FORGNVALUE *fVal = value->valueUnion.frgn;
		PRT_FORGNVALUE *cVal = (PRT_FORGNVALUE *)PrtMalloc(sizeof(PRT_FORGNVALUE));
		retVal->discriminator = PRT_VALKIND_FORGN;
		retVal->valueUnion.frgn = cVal;
		cVal->typeTag = fVal->typeTag;
		cVal->value = PrtCloneForeignValue(fVal->typeTag, fVal->value);
		return retVal;
	}
	case PRT_VALKIND_MAP:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_MAPVALUE *map = (PRT_MAPVALUE *)PrtMalloc(sizeof(PRT_MAPVALUE));
		retVal->discriminator = PRT_VALKIND_MAP;
		retVal->valueUnion.map = map;
		PRT_MAPVALUE *mVal = value->valueUnion.map;
		map->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[mVal->capNum], sizeof(PRT_MAPNODE *));
		map->capNum = mVal->capNum;
		map->size = 0;
		map->first = NULL;
		map->last = NULL;
		PRT_MAPNODE *next = mVal->first;
		while (next != NULL)
		{
			PrtMapUpdate(retVal, next->key, next->value);
			next = next->insertNext;
		}

		return retVal;
	}
	case PRT_VALKIND_TUPLE:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		retVal->discriminator = PRT_VALKIND_TUPLE;
		retVal->valueUnion.tuple = cVal;

		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->valueUnion.tuple->size;
		cVal->size = arity;
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE *));
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtCloneValue(tVal->values[i]);
		}

		return retVal;
	}
	case PRT_VALKIND_SEQ:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		PRT_SEQVALUE *cVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
		retVal->discriminator = PRT_VALKIND_SEQ;
		retVal->valueUnion.seq = cVal;

		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		cVal->capacity = sVal->capacity;
		cVal->size = sVal->size;
		if (sVal->capacity == 0)
		{
			cVal->values = NULL;
		}
		else
		{
			PRT_UINT32 i;
			cVal->values = (PRT_VALUE **)PrtCalloc(sVal->capacity, sizeof(PRT_VALUE *));
			for (i = 0; i < sVal->size; ++i)
			{
				cVal->values[i] = PrtCloneValue(sVal->values[i]);
			}
		}

		return retVal;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtCloneValue: Invalid value");
		return NULL;
	}
}

PRT_BOOLEAN PRT_CALL_CONV PrtIsNullValue(_In_ PRT_VALUE *value)
{
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");

	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALKIND_NULL:
		return PRT_TRUE;
	case PRT_VALKIND_EVENT:
		return value->valueUnion.ev == PRT_SPECIAL_EVENT_NULL ? PRT_TRUE : PRT_FALSE;
	case PRT_VALKIND_MID:
	{
		PRT_MACHINEID *id = value->valueUnion.mid;
		return
			id->processId.data1 == PrtNullMachineId.processId.data1 &&
			id->processId.data2 == PrtNullMachineId.processId.data2 &&
			id->processId.data3 == PrtNullMachineId.processId.data3 &&
			id->processId.data4 == PrtNullMachineId.processId.data4 &&
			id->machineId == PrtNullMachineId.machineId ? PRT_TRUE : PRT_FALSE;
	}
	case PRT_VALKIND_BOOL:
	case PRT_VALKIND_INT:
	case PRT_VALKIND_FORGN:
	case PRT_VALKIND_MAP:
	case PRT_VALKIND_TUPLE:
	case PRT_VALKIND_SEQ:
		return PRT_FALSE;
	default:
		PrtAssert(PRT_FALSE, "PrtIsNullValue: Invalid value");
		return PRT_FALSE;
	}
}

PRT_VALUE * PRT_CALL_CONV PrtCastValue(_In_ PRT_VALUE *value, _In_ PRT_TYPE *type)
{
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(PrtIsValidType(type), "Invalid type expression.");
	PrtAssert(PrtInhabitsType(value, type), "Invalid type cast");
	return value;
}

PRT_BOOLEAN PRT_CALL_CONV PrtInhabitsType(_In_ PRT_VALUE *value, _In_ PRT_TYPE *type)
{
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(PrtIsValidType(type), "Invalid type expression.");

	PRT_TYPE_KIND tkind = type->typeKind;
	PRT_VALUE_KIND vkind = value->discriminator;

	if (tkind == PRT_KIND_ANY)
	{
		return PRT_TRUE;
	}

	switch (tkind)
	{
	case PRT_KIND_NULL:
		return PrtIsNullValue(value);
	case PRT_KIND_BOOL:
		return vkind == PRT_VALKIND_BOOL ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_EVENT:
		return (vkind == PRT_VALKIND_EVENT || PrtIsNullValue(value)) ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MACHINE:
		return (vkind == PRT_VALKIND_MID || PrtIsNullValue(value)) ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_INT:
		return vkind == PRT_VALKIND_INT ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_FORGN:
		return vkind == PRT_VALKIND_FORGN ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MAP:
	{
		if (vkind != PRT_VALKIND_MAP)
		{
			return PRT_FALSE;
		}

		PRT_MAPVALUE *mVal = value->valueUnion.map;
		PRT_MAPTYPE *mType = type->typeUnion.map;
		PRT_MAPNODE *next = mVal->first;
		while (next != NULL)
		{
			if (!PrtInhabitsType(next->key, mType->domType) || !PrtInhabitsType(next->value, mType->codType))
			{
				return PRT_FALSE;
			}

			next = next->insertNext;
		}

		return PRT_TRUE;
	}
	case PRT_KIND_NMDTUP:
	{
		if (vkind != PRT_VALKIND_TUPLE)
		{
			return PRT_FALSE;
		}

		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_NMDTUPTYPE *tType = type->typeUnion.nmTuple;
		if (tType->arity != tVal->size)
		{
			return PRT_FALSE;
		}

		for (PRT_UINT32 i = 0; i < tType->arity; ++i)
		{
			if (!PrtInhabitsType(tVal->values[i], tType->fieldTypes[i]))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	case PRT_KIND_TUPLE:
	{
		if (vkind != PRT_VALKIND_TUPLE)
		{
			return PRT_FALSE;
		}

		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_TUPTYPE *tType = type->typeUnion.tuple;
		if (tType->arity != tVal->size)
		{
			return PRT_FALSE;
		}

		for (PRT_UINT32 i = 0; i < tType->arity; ++i)
		{
			if (!PrtInhabitsType(tVal->values[i], tType->fieldTypes[i]))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	case PRT_KIND_SEQ:
	{
		if (vkind != PRT_VALKIND_SEQ)
		{
			return PRT_FALSE;
		}

		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		PRT_SEQTYPE *sType = type->typeUnion.seq;
		if (sVal->size == 0)
		{
			return PRT_TRUE;
		}
		else
		{
			PRT_UINT32 i;
			for (i = 0; i < sVal->size; ++i)
			{
				if (!PrtInhabitsType(sVal->values[i], sType->innerType))
				{
					return PRT_FALSE;
				}
			}
		}

		return PRT_TRUE;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtInhabitsType: Invalid type");
		return PRT_FALSE;
	}
}

void PRT_CALL_CONV PrtFreeValue(_Inout_ PRT_VALUE *value)
{
	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALKIND_BOOL:
	case PRT_VALKIND_EVENT:
	case PRT_VALKIND_INT:
	case PRT_VALKIND_NULL:
	{
		PrtFree(value);
		break;
	}
	case PRT_VALKIND_MID:
	{
		PRT_MACHINEID *id = value->valueUnion.mid;
		PrtFree(id);
		PrtFree(value);
		break;
	}
	case PRT_VALKIND_FORGN:
	{
		PRT_FORGNVALUE *fVal = value->valueUnion.frgn;
		PrtFreeForeignValue(fVal->typeTag, fVal->value);
		PrtFree(fVal);
		PrtFree(value);
		break;
	}
	case PRT_VALKIND_MAP:
	{
		PRT_MAPVALUE *mVal = value->valueUnion.map;
		PRT_MAPNODE *next = mVal->first;
		PRT_MAPNODE *tmp;
		while (next != NULL)
		{
			tmp = next->insertNext;
			PrtFreeValue(next->key);
			PrtFreeValue(next->value);
			PrtFree(next);
			next = tmp;
		}

		PrtFree(mVal->buckets);
		PrtFree(mVal);
		PrtFree(value);
		break;
	}
	case PRT_VALKIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = tVal->size;
		for (i = 0; i < arity; ++i)
		{
			PrtFreeValue(tVal->values[i]);
		}

		PrtFree(tVal->values);
		PrtFree(tVal);
		PrtFree(value);
		break;
	}
	case PRT_VALKIND_SEQ:
	{
		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		if (sVal->values != NULL)
		{
			PRT_UINT32 i;
			for (i = 0; i < sVal->size; ++i)
			{
				PrtFreeValue(sVal->values[i]);
			}

			PrtFree(sVal->values);
		}

		PrtFree(sVal);
		PrtFree(value);
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtFreeValue: Invalid value");
		break;
	}
}

PRT_BOOLEAN PRT_CALL_CONV PrtIsValidValue(_In_ PRT_VALUE *value)
{
	if (value == NULL)
	{
		return PRT_FALSE;
	}

	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALKIND_BOOL:
		return value->discriminator == PRT_VALKIND_BOOL &&
			(value->valueUnion.bl == PRT_TRUE || value->valueUnion.bl == PRT_FALSE);
	case PRT_VALKIND_EVENT:
		return value->discriminator == PRT_VALKIND_EVENT;
	case PRT_VALKIND_MID:
		return value->discriminator == PRT_VALKIND_MID;
	case PRT_VALKIND_INT:
		return value->discriminator == PRT_VALKIND_INT;
	case PRT_VALKIND_NULL:
		return value->discriminator == PRT_VALKIND_NULL &&
			value->valueUnion.ev == PRT_SPECIAL_EVENT_NULL;
	case PRT_VALKIND_FORGN:
		return value->discriminator == PRT_VALKIND_FORGN &&
			value->valueUnion.frgn != NULL;
	case PRT_VALKIND_MAP:
		return value->discriminator == PRT_VALKIND_MAP &&
			value->valueUnion.map != NULL;
	case PRT_VALKIND_SEQ:
		return value->discriminator == PRT_VALKIND_SEQ &&
			value->valueUnion.seq != NULL;
	case PRT_VALKIND_TUPLE:
		return value->discriminator == PRT_VALKIND_TUPLE &&
			value->valueUnion.tuple != NULL &&
			value->valueUnion.tuple->values != NULL;
	default:
		PrtAssert(PRT_FALSE, "Invalid value");
		return PRT_FALSE;
	}
}

PRT_VALUE * PRT_CALL_CONV PrtMkNondetBoolValue()
{
	return PrtMkBoolValue(PrtChoose());
}