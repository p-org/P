#include "PrtValues.h"

/** Null Value passed to the Makevalue function for initializing values of event, machine, model to NULL */
#define PRT_NULL_VALUE (INT_MAX - 1)

/** Maximum load factor before hashtable is resized. */
#define PRT_MAXHASHLOAD 0.75

/** A 32-bit prime modulus for AC composition of hashcodes */
#define PRT_HASH_AC_COMPOSEMOD 4294967291

/** Array of prime hash table capacities. */
const PRT_UINT32 PrtHashtableCapacities[] =
{
	3,         13,        31,         61,         127,
	251,       509,       1021,       2039,       4093,
	8191,      16381,     32749,      65521,      131071, 
	262139,    524287,    1048573,    2097143,    4194301,
	8388593,   16777213,  33554393,   67108859,   134217689,
	268435399, 536870909, 1073741789, 2147483647, 4294967291
};

PRT_UINT32 PrtGetHashCodeFieldName(PRT_STRING name)
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

PRT_UINT32 PrtGetHashCodeUInt32(PRT_UINT32 value)
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

PRT_UINT32 PrtGetHashCodeTwoUInt32(PRT_UINT32 value1, PRT_UINT32 value2)
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

PRT_VALUE *PrtMkBoolValue(_In_ PRT_BOOLEAN value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
	retVal->type = PrtMkPrimitiveType(PRT_KIND_BOOL);
	retVal->discriminator = retVal->type.typeKind;
	retVal->valueUnion.primValue = (PRT_PRIMVALUE *)PrtCalloc(1, sizeof(PRT_PRIMVALUE));
	retVal->valueUnion.primValue->discriminator = PRT_KIND_BOOL;
	retVal->valueUnion.primValue->value.bl = value;
	return retVal;
}

PRT_VALUE *PrtMkEventValue(_In_ PRT_UINT32 value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
	if (value == PRT_NULL_VALUE) // NULL KIND
	{
		retVal->type = PrtMkPrimitiveType(PRT_KIND_NULL);
		retVal->discriminator = retVal->type.typeKind;
		retVal->valueUnion.primValue = (PRT_PRIMVALUE *)PrtCalloc(1, sizeof(PRT_PRIMVALUE));
		retVal->valueUnion.primValue->discriminator = PRT_KIND_NULL;
		retVal->valueUnion.primValue->value.ev = value;
	}
	else
	{
		retVal->type = PrtMkPrimitiveType(PRT_KIND_EVENT);
		retVal->discriminator = retVal->type.typeKind;
		retVal->valueUnion.primValue = (PRT_PRIMVALUE *)PrtCalloc(1, sizeof(PRT_PRIMVALUE));
		retVal->valueUnion.primValue->discriminator = PRT_KIND_EVENT;
		retVal->valueUnion.primValue->value.ev = value;
		return retVal;
	}
	return retVal;
}

PRT_VALUE *PrtMkIntValue(_In_ PRT_INT32 value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
	retVal->type = PrtMkPrimitiveType(PRT_KIND_INT);
	retVal->discriminator = retVal->type.typeKind;
	retVal->valueUnion.primValue = (PRT_PRIMVALUE *)PrtCalloc(1, sizeof(PRT_PRIMVALUE));
	retVal->valueUnion.primValue->discriminator = PRT_KIND_INT;
	retVal->valueUnion.primValue->value.nt = value;
	return retVal;
}

PRT_VALUE *PrtMkNullValue()
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
	retVal->type = PrtMkPrimitiveType(PRT_KIND_NULL);
	retVal->discriminator = retVal->type.typeKind;
	retVal->valueUnion.primValue = (PRT_PRIMVALUE *)PrtCalloc(1, sizeof(PRT_PRIMVALUE));
	retVal->valueUnion.primValue->discriminator = PRT_KIND_NULL;
	return retVal;
}


PRT_VALUE *PrtMkMachineValue(_In_ PRT_UINT32 value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
	if (value == PRT_NULL_VALUE) // NULL KIND
	{
		retVal->type = PrtMkPrimitiveType(PRT_KIND_NULL);
		retVal->discriminator = retVal->type.typeKind;
		retVal->valueUnion.primValue = (PRT_PRIMVALUE *)PrtCalloc(1, sizeof(PRT_PRIMVALUE));
		retVal->valueUnion.primValue->discriminator = PRT_KIND_NULL;
		retVal->valueUnion.primValue->value.mach = value;
	}
	else
	{
		retVal->type = PrtMkPrimitiveType(PRT_KIND_MACHINE);
		retVal->discriminator = retVal->type.typeKind;
		retVal->valueUnion.primValue = (PRT_PRIMVALUE *)PrtCalloc(1, sizeof(PRT_PRIMVALUE));
		retVal->valueUnion.primValue->discriminator = PRT_KIND_MACHINE;
		retVal->valueUnion.primValue->value.mach = value;
	}
	return retVal;
}

PRT_VALUE *PrtMkModelValue(_In_ PRT_UINT32 value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
	if (value == PRT_NULL_VALUE) // NULL KIND
	{
		retVal->valueUnion.primValue = (PRT_PRIMVALUE *)PrtCalloc(1, sizeof(PRT_PRIMVALUE));
		retVal->type = PrtMkPrimitiveType(PRT_KIND_NULL);
		retVal->discriminator = retVal->type.typeKind;
		retVal->valueUnion.primValue->discriminator = PRT_KIND_NULL;
		retVal->valueUnion.primValue->value.mach = value;
	}
	else
	{
		retVal->valueUnion.primValue = (PRT_PRIMVALUE *)PrtCalloc(1, sizeof(PRT_PRIMVALUE));
		retVal->type = PrtMkPrimitiveType(PRT_KIND_MODEL);
		retVal->discriminator = retVal->type.typeKind;
		retVal->valueUnion.primValue->discriminator = PRT_KIND_MODEL;
		retVal->valueUnion.primValue->value.mach = value;
	}
	return retVal;
}

#if !defined(IGNORE_FRG)
PRT_VALUE *PrtMkForeignValue(_In_ PRT_TYPE type, _In_ void *value)
{
	PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
	retVal->type = PrtCloneType(type);
	retVal->discriminator = retVal->type.typeKind;
	PRT_FORGNVALUE *forgnVal;
	PrtAssert(type.typeKind == PRT_KIND_FORGN, "Did not receive foreign type.");
	forgnVal = (PRT_FORGNVALUE *)PrtCalloc(1, sizeof(PRT_FORGNVALUE));
	forgnVal->value = type.typeUnion.forgn->cloner(type.typeUnion.forgn->typeTag, value);
	retVal->valueUnion.frgn = forgnVal;
	return retVal;
}
#endif
PRT_VALUE* PrtMkDefaultValue(_In_ PRT_TYPE type)
{
	PRT_TYPE_KIND kind = type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		return PrtMkMachineValue(PRT_NULL_VALUE);
	case PRT_KIND_BOOL:
		return PrtMkBoolValue(PRT_FALSE);
	case PRT_KIND_EVENT:
		return PrtMkEventValue(PRT_NULL_VALUE);
	case PRT_KIND_MACHINE:
		return PrtMkMachineValue(PRT_NULL_VALUE);
	case PRT_KIND_INT:
		return PrtMkIntValue(0);
	case PRT_KIND_NULL:
		return PrtMkNullValue();
	case PRT_KIND_MODEL:
		return PrtMkModelValue(PRT_NULL_VALUE);
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtMkAbsentType();
		retVal->discriminator = retVal->type.typeKind;
		PRT_FORGNVALUE *forgnVal;
		forgnVal = (PRT_FORGNVALUE *)PrtCalloc(1, sizeof(PRT_FORGNVALUE));
		forgnVal->value = NULL;
		retVal->valueUnion.frgn = forgnVal;
		return retVal;
		
#endif
	}
	case PRT_KIND_MAP:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_MAPVALUE *mapVal;
		mapVal = (PRT_MAPVALUE *)PrtCalloc(1, sizeof(PRT_MAPVALUE));
		mapVal->size = 0;
		mapVal->capNum = 0;
		mapVal->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[0], sizeof(PRT_MAPNODE *));
		mapVal->first = NULL;
		mapVal->last = NULL;
		retVal->valueUnion.map = mapVal;
		return retVal;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tupVal;
		PRT_NMDTUPTYPE *ntype = type.typeUnion.nmTuple;
		tupVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		
		tupVal->values = (PRT_VALUE **)PrtCalloc(ntype->arity, sizeof(PRT_VALUE*));
		for (i = 0; i < ntype->arity; ++i)
		{
			tupVal->values[i] = PrtMkDefaultValue(ntype->fieldTypes[i]);
		}
		retVal->valueUnion.tuple = tupVal;
		return retVal;
		
	}
	case PRT_KIND_SEQ:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_SEQVALUE *seqVal;
		seqVal = (PRT_SEQVALUE *)PrtCalloc(1, sizeof(PRT_SEQVALUE));
		seqVal->size = 0;
		seqVal->capacity = 0;
		seqVal->values = NULL;
		seqVal->valuesSerialized = NULL;
		retVal->valueUnion.seq = seqVal;
		return retVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tupVal;
		PRT_TUPTYPE *ttype = type.typeUnion.tuple;
		tupVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		tupVal->values = (PRT_VALUE **)PrtCalloc(ttype->arity, sizeof(PRT_VALUE*));
		for (i = 0; i < ttype->arity; ++i)
		{
			tupVal->values[i] = PrtMkDefaultValue(ttype->fieldTypes[i]);
		}
		retVal->valueUnion.tuple = tupVal;
		return retVal;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
}

void PrtPrimSetBool(_Inout_ PRT_VALUE *prmVal, _In_ PRT_BOOLEAN value)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_BOOL, "Invalid type on primitive set");
	prmVal->valueUnion.primValue->value.bl = value;
}

PRT_BOOLEAN PrtPrimGetBool(_In_ PRT_VALUE *prmVal)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_BOOL, "Invalid type on primitive get");
	return prmVal->valueUnion.primValue->value.bl;
}

void PrtPrimSetEvent(_Inout_ PRT_VALUE *prmVal, _In_ PRT_UINT32 value)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_EVENT, "Invalid type on primitive set");
	prmVal->valueUnion.primValue->value.ev = value;
}

PRT_UINT32 PrtPrimGetEvent(_In_ PRT_VALUE *prmVal)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_EVENT, "Invalid type on primitive get");
	return prmVal->valueUnion.primValue->value.ev;
}

void PrtPrimSetInt(_Inout_ PRT_VALUE *prmVal, _In_ PRT_INT32 value)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_INT, "Invalid type on primitive set");
	prmVal->valueUnion.primValue->value.nt = value;
}

PRT_INT32 PrtPrimGetInt(_In_ PRT_VALUE *prmVal)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_INT, "Invalid type on primitive get");
	return prmVal->valueUnion.primValue->value.nt;
}

void PrtPrimSetMachine(_Inout_ PRT_VALUE *prmVal, _In_ PRT_UINT32 value)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_MACHINE, "Invalid type on primitive set");
	prmVal->valueUnion.primValue->value.mach = value;
}

PRT_UINT32 PrtPrimGetMachine(_In_ PRT_VALUE *prmVal)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_MACHINE, "Invalid type on primitive get");
	return prmVal->valueUnion.primValue->value.mach;
}

void PrtPrimSetModel(_Inout_ PRT_VALUE *prmVal, _In_ PRT_UINT32 value)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_MODEL, "Invalid type on primitive set");
	prmVal->valueUnion.primValue->value.model = value;
}

PRT_UINT32 PrtPrimGetModel(_In_ PRT_VALUE *prmVal)
{
	PrtAssert((prmVal->type.typeKind) == PRT_KIND_MODEL, "Invalid type on primitive get");
	return prmVal->valueUnion.primValue->value.model;
}

void PrtTupleSet(_Inout_ PRT_VALUE *tuple, _In_ PRT_UINT32 index, _In_ PRT_VALUE* value)
{
	//// Eager dereferencing of inputs to check pointer validity
	PrtAssert((tuple->type.typeKind) == PRT_KIND_TUPLE || (tuple->type.typeKind) == PRT_KIND_NMDTUP, "Cannot perform tuple set on this value");
	PrtAssert(value->type.typeKind >= 0 && value->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid value");

	PRT_UINT32 arity;
	PRT_TYPE *fieldTypes;
	PRT_VALUE* clone;
	if ((tuple->type.typeKind) == PRT_KIND_TUPLE)
	{
		PRT_TUPTYPE *ttype = tuple->type.typeUnion.tuple;
		arity = ttype->arity;
		fieldTypes = ttype->fieldTypes;
	}
	else if ((tuple->type.typeKind) == PRT_KIND_NMDTUP)
	{
		PRT_NMDTUPTYPE *ntype = tuple->type.typeUnion.nmTuple;
		arity = ntype->arity;
		fieldTypes = ntype->fieldTypes;
	}
	else 
	{
		PRT_DBG_ASSERT(PRT_FALSE, "impossible");
		return;
	}

	PrtAssert(index < arity, "Invalid tuple index");
	PrtAssert(PrtIsSubtype(value->type, fieldTypes[index]), "Invalid type on tuple set");

	clone = PrtCloneValue(value);
	PrtFreeValue(tuple->valueUnion.tuple->values[index]);
	tuple->valueUnion.tuple->values[index] = clone;
}

PRT_VALUE* PrtTupleGet(_In_ PRT_VALUE *tuple, _In_ PRT_UINT32 index)
{
	//// Eager dereferencing of inputs to check pointer validity
	PrtAssert((tuple->type.typeKind) == PRT_KIND_TUPLE || (tuple->type.typeKind) == PRT_KIND_NMDTUP, "Cannot perform tuple get on this value");

	PRT_UINT32 arity;
	if ((tuple->type.typeKind) == PRT_KIND_TUPLE)
	{
		PRT_TUPTYPE *ttype = tuple->type.typeUnion.tuple;
		arity = ttype->arity;
	}
	else if ((tuple->type.typeKind) == PRT_KIND_NMDTUP)
	{
		PRT_NMDTUPTYPE *ntype = tuple->type.typeUnion.nmTuple;
		arity = ntype->arity;
	}
	else
	{
		PRT_DBG_ASSERT(PRT_FALSE, "impossible");
		return NULL;
	}

	PrtAssert(index < arity, "Invalid tuple index");
	return PrtCloneValue(tuple->valueUnion.tuple->values[index]);
}

void PrtNmdTupleSet(_Inout_ PRT_VALUE *tuple, _In_ PRT_STRING name, _In_ PRT_VALUE *value)
{
	//// Eager dereferencing of inputs to check pointer validity
	PrtAssert((tuple->type.typeKind) == PRT_KIND_NMDTUP, "Cannot perform tuple set on this value");
	PrtAssert(name != NULL && name[0] != '\0', "Invalid field name");
	PrtAssert(value->type.typeKind >= 0 && value->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid value");

	PRT_NMDTUPTYPE *type = tuple->type.typeUnion.nmTuple;
	PRT_UINT32 arity = type->arity;
	PRT_TYPE *fieldTypes = type->fieldTypes;
	PRT_STRING *fieldNames = type->fieldNames;
	PRT_VALUE *clone;

	PRT_UINT32 index;
	for (index = 0; index < arity; ++index)
	{
		if (strncmp(fieldNames[index], name, PRT_MAXFLDNAME_LENGTH) == 0)
		{
			break;
		}
	}

	PrtAssert(index < arity, "Invalid tuple field name");
	PrtAssert(PrtIsSubtype(value->type, fieldTypes[index]), "Invalid type on tuple set");
	clone = PrtCloneValue(value);
	PrtFreeValue(tuple->valueUnion.tuple->values[index]);
	tuple->valueUnion.tuple->values[index] = clone;
}

PRT_VALUE* PrtNmdTupleGet(_In_ PRT_VALUE *tuple, _In_ PRT_STRING name)
{
	//// Eager dereferencing of inputs to check pointer validity
	PrtAssert(tuple->type.typeKind == PRT_KIND_NMDTUP, "Cannot perform tuple set on this value");
	PrtAssert(name != NULL && name[0] != '\0', "Invalid field name");
	PRT_NMDTUPTYPE *type = tuple->type.typeUnion.nmTuple;
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
	return PrtCloneValue(tuple->valueUnion.tuple->values[index]);
}

void PrtSeqUpdate(_Inout_ PRT_VALUE *seq, _In_ PRT_UINT32 index, _In_ PRT_VALUE *value)
{
	PRT_SEQTYPE *seqType;
	PRT_VALUE *clone;
	PrtAssert(seq->type.typeKind == PRT_KIND_SEQ, "Invalid value");
	PrtAssert(index >= 0 && index < seq->valueUnion.seq->size, "Invalid index");
	PrtAssert(value->type.typeKind >= 0 && value->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid value");
	seqType = seq->type.typeUnion.seq;
	PrtAssert(PrtIsSubtype(value->type, seqType->innerType), "Invalid type on sequence update");
	clone = PrtCloneValue(value);
	PrtFreeValue(seq->valueUnion.seq->values[index]);
	seq->valueUnion.seq->values[index] = clone;
}

void PrtSeqInsert(_Inout_ PRT_VALUE *seq, _In_ PRT_UINT32 index, _In_ PRT_VALUE* value)
{
	PRT_VALUE *clone;
	PRT_SEQTYPE *seqType;
	PrtAssert(seq->type.typeKind == PRT_KIND_SEQ, "Invalid value");
	PrtAssert(index >= 0 && index <= seq->valueUnion.seq->size, "Invalid index");
	PrtAssert(value->type.typeKind >= 0 && value->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid value");
	seqType = seq->type.typeUnion.seq;
	PrtAssert(PrtIsSubtype(value->type, seqType->innerType), "Invalid type on sequence update");

	clone = PrtCloneValue(value);
	if (seq->valueUnion.seq->capacity == 0)
	{
		seq->valueUnion.seq->values = (PRT_VALUE **)PrtCalloc(1, sizeof(PRT_VALUE*));
		seq->valueUnion.seq->values[0] = clone;
		seq->valueUnion.seq->capacity = 1;
	}
	else if (seq->valueUnion.seq->size < seq->valueUnion.seq->capacity)
	{
		PRT_UINT32 i;
		PRT_VALUE **values = seq->valueUnion.seq->values;
		if (seq->valueUnion.seq->size > 0)
		{
			for (i = seq->valueUnion.seq->size - 1; i >= index; --i)
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
		PRT_VALUE **values;
		seq->valueUnion.seq->capacity = 2 * seq->valueUnion.seq->capacity;
		values = (PRT_VALUE **)PrtCalloc(seq->valueUnion.seq->capacity, sizeof(PRT_VALUE*));
		for (i = 0; i < seq->valueUnion.seq->size; ++i)
		{
			if (i < index)
			{
				values[i] = seq->valueUnion.seq->values[i];
			}
			else 
			{
				values[i + 1] = seq->valueUnion.seq->values[i];
			}
		}

		values[index] = clone;
		PrtFree(seq->valueUnion.seq->values);
		seq->valueUnion.seq->values = values;
	}

	seq->valueUnion.seq->size = seq->valueUnion.seq->size + 1;
}

void PrtSeqRemove(_Inout_ PRT_VALUE *seq, _In_ PRT_UINT32 index)
{
	PRT_UINT32 i;
	PRT_VALUE **values;
	PRT_UINT32 seqSize = seq->valueUnion.seq->size;
	PrtAssert(seq->type.typeKind == PRT_KIND_SEQ, "Invalid value");
	PrtAssert(index >= 0 && index < seqSize, "Invalid index");
	values = seq->valueUnion.seq->values;
	PrtFreeValue(values[index]);
	for (i = index; i < seqSize - 1; ++i)
	{
		values[i] = values[i + 1];
	}

	seq->valueUnion.seq->size = seq->valueUnion.seq->size - 1;
}

PRT_VALUE* PrtSeqGet(_In_ PRT_VALUE *seq, _In_ PRT_UINT32 index)
{
	PrtAssert(seq->type.typeKind == PRT_KIND_SEQ, "Invalid value");
	PrtAssert(index >= 0 && index < seq->valueUnion.seq->size, "Invalid index");
	return PrtCloneValue(seq->valueUnion.seq->values[index]);
}

PRT_UINT32 PrtSeqSizeOf(_In_ PRT_VALUE *seq)
{
	PrtAssert(seq->type.typeKind == PRT_KIND_SEQ, "Invalid value");
	return seq->valueUnion.seq->size;
}

/** Expands the map and rehashes its key-value pairs */
void PrtMapExpand(_Inout_ PRT_VALUE *map)
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

void PrtMapUpdateEx(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key, _In_ PRT_VALUE *value, _In_ PRT_BOOLEAN cloneKeyVals)
{
	PRT_MAPTYPE *mapType;
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	PRT_MAPNODE *node = NULL;
	PRT_BOOLEAN isNewKey = PRT_FALSE;

	PrtAssert(map->type.typeKind == PRT_KIND_MAP, "Invalid map value");
	PrtAssert(key->type.typeKind >= 0 && key->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid key");
	PrtAssert(value->type.typeKind >= 0 && value->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid value");

	mapType = map->type.typeUnion.map;
	PrtAssert(PrtIsSubtype(key->type, mapType->domType), "Invalid map update; key has bad type");
	PrtAssert(PrtIsSubtype(value->type, mapType->codType), "Invalid map update; value has bad type");

	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];

	if (bucket == NULL)
	{
		isNewKey = PRT_TRUE;
		node = (PRT_MAPNODE *)PrtCalloc(1, sizeof(PRT_MAPNODE));
		node->key = cloneKeyVals == PRT_TRUE ? PrtCloneValue(key) : key;
		node->value = cloneKeyVals == PRT_TRUE ? PrtCloneValue(value) : value;
		node->bucketNext = NULL;
		node->insertNext = NULL;
		map->valueUnion.map->buckets[bucketNum] = node;
	}
	else
	{
		PRT_VALUE *valueClone = cloneKeyVals == PRT_TRUE ? PrtCloneValue(value) : value;
		PRT_MAPNODE *next = bucket;
		isNewKey = PRT_TRUE;
		while (next != NULL)
		{
			if (PrtIsEqualValue(next->key, key))
			{
				PrtFreeValue(next->value);
				next->value = valueClone;
				isNewKey = PRT_FALSE;
				break;
			}

			next = next->bucketNext;
		}

		if (isNewKey == PRT_TRUE)
		{
			node = (PRT_MAPNODE *)PrtCalloc(1, sizeof(PRT_MAPNODE));
			node->key = cloneKeyVals == PRT_TRUE ? PrtCloneValue(key) : key;
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

void PrtMapUpdate(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key, _In_ PRT_VALUE *value)
{
	PrtMapUpdateEx(map, key, value, PRT_TRUE);
}

void PrtMapRemove(_Inout_ PRT_VALUE *map, _In_ PRT_VALUE *key)
{
	PRT_MAPTYPE *mapType;
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;

	PrtAssert(map->type.typeKind == PRT_KIND_MAP, "Invalid map value");
	PrtAssert(key->type.typeKind >= 0 && key->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid key");
	mapType = map->type.typeUnion.map;
	PrtAssert(PrtIsSubtype(key->type, mapType->domType), "Invalid map remove; key has bad type");

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

PRT_VALUE* PrtMapGet(_In_ PRT_VALUE *map, _In_ PRT_VALUE* key)
{
	PRT_MAPTYPE *mapType;
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;

	PrtAssert(map->type.typeKind == PRT_KIND_MAP, "Invalid map value");
	PrtAssert(key->type.typeKind >= 0 && key->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid key");
	mapType = map->type.typeUnion.map;
	PrtAssert(PrtIsSubtype(key->type, mapType->domType), "Invalid map get; key has bad type");

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

PRT_VALUE *PrtMapGetKeys(_In_ PRT_VALUE *map)
{
	PrtAssert(map->type.typeKind == PRT_KIND_MAP, "Invalid map value");
	PRT_MAPTYPE *mapType = map->type.typeUnion.map;
	PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
	PRT_SEQVALUE *seqVal = (PRT_SEQVALUE *)PrtCalloc(1, sizeof(PRT_SEQVALUE));
	retVal->type = PrtMkSeqType(mapType->domType);
	retVal->valueUnion.seq = seqVal;
	retVal->discriminator = retVal->type.typeKind;

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

PRT_VALUE *PrtMapGetValues(_In_ PRT_VALUE *map)
{
	PrtAssert(map->type.typeKind == PRT_KIND_MAP, "Invalid map value");
	PRT_MAPTYPE *mapType = map->type.typeUnion.map;
	PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
	PRT_SEQVALUE *seqVal = (PRT_SEQVALUE *)PrtCalloc(1, sizeof(PRT_SEQVALUE));
	retVal->type = PrtMkSeqType(mapType->domType);
	retVal->valueUnion.seq = seqVal;
	retVal->discriminator = retVal->type.typeKind;

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

PRT_BOOLEAN PrtMapExists(_In_ PRT_VALUE *map, _In_ PRT_VALUE *key)
{
	PRT_MAPTYPE *mapType;
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;

	PrtAssert(map->type.typeKind == PRT_KIND_MAP, "Invalid map value");
	PrtAssert(key->type.typeKind >= 0 && key->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid key");
	mapType = map->type.typeUnion.map;
	PrtAssert(PrtIsSubtype(key->type, mapType->domType), "Invalid map get; key has bad type");

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

PRT_BOOLEAN PrtMapIsSameMapping(_In_ PRT_VALUE *map, _In_ PRT_VALUE* key, _In_ PRT_VALUE* value)
{
	PRT_MAPTYPE *mapType;
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;

	PrtAssert((map->type.typeKind) == PRT_KIND_MAP, "Invalid map value");
	PrtAssert(key->type.typeKind >= 0 && key->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid key");
	mapType = map->type.typeUnion.map;
	PrtAssert(PrtIsSubtype(key->type, mapType->domType), "Invalid map get; key has bad type");

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

PRT_UINT32 PrtMapSizeOf(_In_ PRT_VALUE *map)
{
	PrtAssert(map->type.typeKind == PRT_KIND_MAP, "Invalid map value");
	return map->valueUnion.map->size;
}

PRT_UINT32 PrtMapCapacity(_In_ PRT_VALUE *map)
{
	PrtAssert(map->type.typeKind == PRT_KIND_MAP, "Invalid map value");
	return PrtHashtableCapacities[map->valueUnion.map->capNum];
}

PRT_UINT32 PrtGetHashCodeValue(_In_ PRT_VALUE* inputValue)
{
	PRT_TYPE_KIND kind = inputValue->type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return 0;
	case PRT_KIND_BOOL:
		return PrtGetHashCodeUInt32(0x00400000 ^ ((PRT_UINT32)((PRT_PRIMVALUE *)inputValue->valueUnion.primValue)->value.bl));
	case PRT_KIND_EVENT:
		return PrtGetHashCodeUInt32(0x00800000 ^ ((PRT_UINT32)((PRT_PRIMVALUE *)inputValue->valueUnion.primValue)->value.ev));
	case PRT_KIND_MACHINE:
		return PrtGetHashCodeUInt32(0x01000000 ^ ((PRT_UINT32)((PRT_PRIMVALUE *)inputValue->valueUnion.primValue)->value.mach));
	case PRT_KIND_INT:
		return PrtGetHashCodeUInt32(0x02000000 ^ ((PRT_UINT32)((PRT_PRIMVALUE *)inputValue->valueUnion.primValue)->value.nt));
	case PRT_KIND_MODEL:
		return PrtGetHashCodeUInt32(0x04000000 ^ ((PRT_UINT32)((PRT_PRIMVALUE *)inputValue->valueUnion.primValue)->value.model));
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		PRT_FORGNTYPE* fType = inputValue->type.typeUnion.forgn;
		return 0x08000000 ^ fType->hasher(fType->typeTag, ((PRT_FORGNVALUE*)inputValue)->value);
#endif
	}
	case PRT_KIND_MAP:
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
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_UINT32 j;
		PRT_UINT32 code = 0;
		PRT_UINT32 pointCode;
		PRT_TUPVALUE *tVal = inputValue->valueUnion.tuple;
		PRT_UINT32 arity = (inputValue->type.typeUnion.nmTuple)->arity;
		PRT_STRING *fnames = (inputValue->type.typeUnion.nmTuple)->fieldNames;
		for (i = 0; i < arity; ++i)
		{
			pointCode = PrtGetHashCodeFieldName(fnames[i]);
			for (j = 0; j < 4; ++j)
			{
				code += (pointCode & 0x000000FF);
				code += (code << 10);
				code ^= (code >> 6);
				pointCode = (pointCode >> 8);
			}

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
	case PRT_KIND_SEQ:
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
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_UINT32 j;
		PRT_UINT32 code = 0;
		PRT_UINT32 pointCode;
		PRT_TUPVALUE *tVal = inputValue->valueUnion.tuple;
		PRT_UINT32 arity = (inputValue->type.typeUnion.tuple)->arity;
		for (i = 0; i < arity; ++i)
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
		PrtAssert(PRT_FALSE, "Invalid type");
		return 0;
	}
}

PRT_BOOLEAN PrtIsEqualValue(_In_ PRT_VALUE* value1, _In_ PRT_VALUE* value2)
{
	PRT_TYPE_KIND kind1 = value1->type.typeKind;
	PRT_TYPE_KIND kind2 = value2->type.typeKind;
	PRT_DBG_ASSERT(kind1 != PRT_KIND_ANY, "Value must have a more concrete type");
	PRT_DBG_ASSERT(kind2 != PRT_KIND_ANY, "Value must have a more concrete type");

	if (kind1 != kind2)
	{
		return PRT_FALSE;
	}
	else if (value1 == value2)
	{
		return PRT_TRUE;
	}

	switch (kind1)
	{
	case PRT_KIND_BOOL:
		return
			value1->valueUnion.primValue->value.bl == value2->valueUnion.primValue->value.bl ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_EVENT:
		return
			value1->valueUnion.primValue->value.ev == value2->valueUnion.primValue->value.ev ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MACHINE:
		return
			value1->valueUnion.primValue->value.mach == value2->valueUnion.primValue->value.mach ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_INT:
		return
			value1->valueUnion.primValue->value.nt == value2->valueUnion.primValue->value.nt ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MODEL:
		return
			value1->valueUnion.primValue->value.model == value2->valueUnion.primValue->value.model ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		PRT_FORGNVALUE *fVal1 = value1->valueUnion.frgn;
		PRT_FORGNVALUE *fVal2 = value2->valueUnion.frgn;
		PRT_FORGNTYPE *fType1 = value1->type.typeUnion.forgn;
		PRT_FORGNTYPE *fType2 = value2->type.typeUnion.forgn;
		return fType1->eqTester(fType1->typeTag, fVal1->value, fType2->typeTag, fVal2->value);
#endif
	}
	case PRT_KIND_MAP:
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
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal1 = value1->valueUnion.tuple;
		PRT_TUPVALUE *tVal2 = value2->valueUnion.tuple;
		PRT_NMDTUPTYPE *tType1 = value1->type.typeUnion.nmTuple;
		PRT_NMDTUPTYPE *tType2 = value2->type.typeUnion.nmTuple;

		if (tType1->arity != tType2->arity)
		{
			return PRT_FALSE;
		}

		for (i = 0; i < tType1->arity; ++i)
		{
			if (strncmp(tType1->fieldNames[i], tType2->fieldNames[i], PRT_MAXFLDNAME_LENGTH) != 0)
			{
				return PRT_FALSE;
			}

			if (!PrtIsEqualValue(tVal1->values[i], tVal2->values[i]))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	case PRT_KIND_SEQ:
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
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal1 = value1->valueUnion.tuple;
		PRT_TUPVALUE *tVal2 = value2->valueUnion.tuple;
		PRT_TUPTYPE *tType1 = value1->type.typeUnion.tuple;
		PRT_TUPTYPE *tType2 = value2->type.typeUnion.tuple;

		if (tType1->arity != tType2->arity)
		{
			return PRT_FALSE;
		}

		for (i = 0; i < tType1->arity; ++i)
		{
			if (!PrtIsEqualValue(tVal1->values[i], tVal2->values[i]))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return PRT_FALSE;
	}
}

PRT_VALUE* PrtCloneValue(_In_ PRT_VALUE* value)
{
	PRT_TYPE_KIND kind = value->type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return NULL;
	case PRT_KIND_BOOL:
	{
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkBoolValue(pVal->value.bl);
	}
	case PRT_KIND_EVENT:
	{
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkEventValue(pVal->value.ev);
	}
	case PRT_KIND_MACHINE:
	{
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkMachineValue(pVal->value.mach);
	}
	case PRT_KIND_INT:
	{
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkIntValue(pVal->value.nt);
	}
	case PRT_KIND_MODEL:
	{
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkModelValue(pVal->value.model);
	}
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		PRT_FORGNVALUE *fVal = value->valueUnion.frgn;
		PRT_FORGNTYPE *fType = value->type.typeUnion.forgn;
		PRT_FORGNVALUE *cVal = (PRT_FORGNVALUE *)PrtCalloc(1, sizeof(PRT_FORGNVALUE));
		retVal->type = PrtCloneType(value->type);
		retVal->discriminator = retVal->type.typeKind;
		cVal->value = fType->cloner(fType->typeTag, fVal->value);
		retVal->valueUnion.frgn = cVal;
		return retVal;
#endif
	}
	case PRT_KIND_MAP:
	{
		PRT_VALUE *retVal = PrtMkDefaultValue(value->type);
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
			PrtMapUpdate(retVal, next->key, next->value);
			next = next->insertNext;
		}
		
		return retVal;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(value->type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.nmTuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtCloneValue(tVal->values[i]);
		}

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(value->type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.tuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtCloneValue(tVal->values[i]);
		}

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_SEQ:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(value->type);
		retVal->discriminator = retVal->type.typeKind;
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
			cVal->values = (PRT_VALUE **)PrtCalloc(sVal->capacity, sizeof(PRT_VALUE*));
			for (i = 0; i < sVal->size; ++i)
			{
				cVal->values[i] = PrtCloneValue(sVal->values[i]);
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

PRT_BOOLEAN PrtIsNullValue(_In_ PRT_VALUE *value)
{
	PRT_TYPE_KIND kind = value->type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return PRT_FALSE;
	case PRT_KIND_EVENT:
		return (value)->type.typeKind == PRT_KIND_NULL ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MACHINE:
		return (value)->type.typeKind == PRT_KIND_NULL ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MODEL:
		return (value)->type.typeKind == PRT_KIND_NULL ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_NULL:
		return PRT_TRUE;
	case PRT_KIND_BOOL:
	case PRT_KIND_INT:
	case PRT_KIND_FORGN:
	case PRT_KIND_MAP:
	case PRT_KIND_NMDTUP:
	case PRT_KIND_TUPLE:
	case PRT_KIND_SEQ:
		return PRT_FALSE;
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return PRT_FALSE;
	}
}

PRT_VALUE *PrtCastValue(_In_ PRT_VALUE *value, _In_ PRT_TYPE type)
{
	PrtAssert(value->type.typeKind >= 0 && value->type.typeKind  < PRT_TYPE_KIND_COUNT, "Invalid value");
	PrtAssert(type.typeKind >= 0 && type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid type");

	PRT_TYPE_KIND tkind = type.typeKind;
	PRT_TYPE_KIND vkind = value->type.typeKind;

	if (tkind == PRT_KIND_ANY)
	{
		return PrtCloneValue(value);
	}

	switch (tkind)
	{
	case PRT_KIND_BOOL:
	{
		PrtAssert(vkind == PRT_KIND_BOOL, "Invalid type cast");
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkBoolValue(pVal->value.bl);
	}
	case PRT_KIND_EVENT:
	{
		//// Assumes event, id, model are stored in a union with the same type 
		PrtAssert(vkind == PRT_KIND_EVENT || PrtIsNullValue(value), "Invalid type cast");
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkEventValue(pVal->value.ev);
	}
	case PRT_KIND_MACHINE:
	{
		//// Assumes event, id, model are stored in a union with the same type 
		PrtAssert(vkind == PRT_KIND_MACHINE || PrtIsNullValue(value), "Invalid type cast");
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkMachineValue(pVal->value.mach);
	}
	case PRT_KIND_INT:
	{
		PrtAssert(vkind == PRT_KIND_INT, "Invalid type cast");
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkIntValue(pVal->value.nt);
	}
	case PRT_KIND_MODEL:
	{
		//// Assumes event, id, model are stored in a union with the same type 
		PrtAssert(vkind == PRT_KIND_MODEL || PrtIsNullValue(value), "Invalid type cast");
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		return PrtMkModelValue(pVal->value.model);
	}
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		
		PrtAssert(vkind == PRT_KIND_FORGN, "Invalid type cast");
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_FORGNVALUE *fVal = value->valueUnion.frgn;
		PRT_FORGNVALUE *cVal = (PRT_FORGNVALUE *)PrtCalloc(1, sizeof(PRT_FORGNVALUE));
		cVal->value = value->type.typeUnion.forgn->cloner(value->type.typeUnion.forgn->typeTag, fVal->value);
		retVal->valueUnion.frgn = cVal;
		return retVal;
#endif
	}
	case PRT_KIND_MAP:
	{
		PrtAssert(vkind == PRT_KIND_MAP, "Invalid type cast");
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_MAPVALUE *mVal = value->valueUnion.map;
		PRT_MAPTYPE *mType = (PRT_MAPTYPE *)type.typeUnion.map;
		PRT_MAPVALUE *cVal = (PRT_MAPVALUE *)PrtMkDefaultValue(type);
		if (mVal->capNum > 0)
		{
			//// Eagerly allocate capacity in the clone to avoid intermediate rehashings.
			PrtFree(cVal->buckets);
			cVal->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[mVal->capNum], sizeof(PRT_MAPNODE *));
			cVal->capNum = mVal->capNum;
		}

		retVal->valueUnion.map = cVal;
		PRT_MAPNODE *next = mVal->first;
		while (next != NULL)
		{
			PrtMapUpdateEx(retVal, PrtCastValue(next->key, mType->domType), PrtCastValue(next->value, mType->codType), PRT_FALSE);
			next = next->insertNext;
		}

		return retVal;
	}
	case PRT_KIND_NMDTUP:
	{
		PrtAssert(vkind == PRT_KIND_NMDTUP, "Invalid type cast");
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_NMDTUPTYPE *tType = type.typeUnion.nmTuple;
		PRT_NMDTUPTYPE *srcType = value->type.typeUnion.nmTuple;
		PRT_UINT32 arity = tType->arity;
		PrtAssert(arity == srcType->arity, "Invalid type cast");

		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE *));
		for (i = 0; i < arity; ++i)
		{
			PrtAssert(strncmp(tType->fieldNames[i], srcType->fieldNames[i], PRT_MAXFLDNAME_LENGTH) == 0, "Invalid type cast");
			cVal->values[i] = PrtCastValue(tVal->values[i], tType->fieldTypes[i]);
		}

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_TUPLE:
	{
		PrtAssert(vkind == PRT_KIND_TUPLE, "Invalid type cast");
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_TUPTYPE *tType = type.typeUnion.tuple;
		PRT_TUPTYPE *srcType = value->type.typeUnion.tuple;
		PRT_UINT32 arity = tType->arity;
		PrtAssert(arity == srcType->arity, "Invalid type cast");

		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtCastValue(tVal->values[i], tType->fieldTypes[i]);
		}

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_SEQ:
	{
		PrtAssert(vkind == PRT_KIND_SEQ, "Invalid type cast");
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		PRT_SEQTYPE *sType = type.typeUnion.seq;
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
			cVal->values = (PRT_VALUE **)PrtCalloc(sVal->capacity, sizeof(PRT_VALUE*));
			for (i = 0; i < sVal->size; ++i)
			{
				cVal->values[i] = PrtCastValue(sVal->values[i], sType->innerType);
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

PRT_BOOLEAN PrtInhabitsType(_In_ PRT_VALUE *value, _In_ PRT_TYPE type)
{
	PrtAssert(value->type.typeKind >= 0 && value->type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid value");
	PrtAssert(type.typeKind >= 0 && type.typeKind < PRT_TYPE_KIND_COUNT, "Invalid type");

	PRT_TYPE_KIND tkind = type.typeKind;
	PRT_TYPE_KIND vkind = value->type.typeKind;

	PrtAssert(vkind != PRT_KIND_ANY, "Value must have a more concrete type");
	if (tkind == PRT_KIND_ANY)
	{
		return PRT_TRUE;	
	}

	switch (tkind)
	{
	case PRT_KIND_BOOL:
		return vkind == PRT_KIND_BOOL ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_EVENT:
		return (vkind == PRT_KIND_EVENT || PrtIsNullValue(value)) ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MACHINE:
		return (vkind == PRT_KIND_MACHINE || PrtIsNullValue(value)) ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_INT:
		return vkind == PRT_KIND_INT ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MODEL:
		return (vkind == PRT_KIND_MODEL || PrtIsNullValue(value)) ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_FORGN:
		return vkind == PRT_KIND_FORGN ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MAP:
	{
		if (vkind != PRT_KIND_MAP)
		{
			return PRT_FALSE;
		}
	
		PRT_MAPVALUE *mVal = value->valueUnion.map;
		PRT_MAPTYPE *mType = type.typeUnion.map;
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
		if (vkind != PRT_KIND_NMDTUP)
		{
			return PRT_FALSE;
		}

		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_NMDTUPTYPE *tType = type.typeUnion.nmTuple;
		PRT_NMDTUPTYPE *srcType = value->type.typeUnion.nmTuple;
		PRT_UINT32 arity = tType->arity;
		if (arity != srcType->arity)
		{
			return PRT_FALSE;
		}

		for (i = 0; i < arity; ++i)
		{
			if (strncmp(tType->fieldNames[i], srcType->fieldNames[i], PRT_MAXFLDNAME_LENGTH) != 0)
			{
				return PRT_FALSE;
			}
			else if (!PrtInhabitsType(tVal->values[i], tType->fieldTypes[i]))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	case PRT_KIND_TUPLE:
	{
		if (vkind != PRT_KIND_TUPLE)
		{
			return PRT_FALSE;
		}

		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_TUPTYPE *tType = type.typeUnion.tuple;
		PRT_TUPTYPE *srcType = value->type.typeUnion.tuple;
		PRT_UINT32 arity = tType->arity;
		if (arity != srcType->arity)
		{
			return PRT_FALSE;
		}

		for (i = 0; i < arity; ++i)
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
		if (vkind != PRT_KIND_SEQ)
		{
			return PRT_FALSE;
		}

		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		PRT_SEQTYPE *sType = type.typeUnion.seq;
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
		PrtAssert(PRT_FALSE, "Invalid type");
		return PRT_FALSE;
	}
}

void PrtFreeValue(_Inout_ PRT_VALUE* value)
{
	PRT_TYPE_KIND kind = value->type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		break;
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_NULL:
	case PRT_KIND_MODEL:
	{
		PRT_PRIMVALUE *pVal = value->valueUnion.primValue;
		PrtFreeType(value->type);
		PrtFree(pVal);
		PrtFree(value);
		break;
	}
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		PRT_FORGNVALUE *fVal = value->valueUnion.frgn;
		PRT_FORGNTYPE *fType = (PRT_FORGNTYPE *)value->type.typeUnion.forgn;
		fType->freer(fType->typeTag, fVal->value);
		PrtFreeType(value->type);
		PrtFree(fVal);
		PrtFree(value);
		break;
#endif
	}
	case PRT_KIND_MAP:
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

		PrtFreeType(value->type);
		PrtFree(mVal->buckets);
		PrtFree(mVal);
		PrtFree(value);
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = ((PRT_NMDTUPTYPE *)value->type.typeUnion.nmTuple)->arity;
		for (i = 0; i < arity; ++i)
		{
			PrtFreeValue(tVal->values[i]);
		}

		PrtFreeType(value->type);
		PrtFree(tVal->values);
		PrtFree(tVal);
		PrtFree(value);
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = (value->type.typeUnion.tuple)->arity;
		for (i = 0; i < arity; ++i)
		{
			PrtFreeValue(tVal->values[i]);
		}

		PrtFreeType(value->type);
		PrtFree(tVal->values);
		PrtFree(tVal);
		PrtFree(value);
		break;
	}
	case PRT_KIND_SEQ:
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

		PrtFreeType(value->type);
		PrtFree(sVal);
		PrtFree(value);
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		break;
	}
}