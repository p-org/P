#include "PrtValues.h"
#include "PrtProgram.h"

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
const PRT_MACHINEID PrtNullMachineId = { {0, 0, 0, 0}, PRT_SPECIAL_EVENT_NULL };

// this function is not used.
//static PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeFieldName(_In_ PRT_STRING name)
//{
//	PRT_UINT32 i;
//	PRT_UINT32 code = 0;
//	PRT_UINT64 len = strnlen(name, PRT_MAXFLDNAME_LENGTH);
//	for (i = 0; i < len; ++i)
//	{
//		code += name[i];
//		code += (code << 10);
//		code ^= (code >> 6);
//	}
//
//	code += (code << 3);
//	code ^= (code >> 11);
//	code += (code << 15);
//	return code;
//}

static PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeUInt32(_In_ PRT_UINT32 value)
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

static PRT_UINT32 PRT_CALL_CONV PrtGetHashCodePrtInt(_In_ PRT_INT value)
{
	if (sizeof(PRT_INT) == 4)
	{
		return PrtGetHashCodeUInt32((PRT_UINT32)value);
	}
	return PrtGetHashCodeUInt32((PRT_UINT32)value) ^ PrtGetHashCodeUInt32((PRT_UINT64)value >> 32);
}

static PRT_UINT32 PRT_CALL_CONV PrtGetHashCodePrtFloat(_In_ PRT_FLOAT value)
{
	if (value == 0)
	{
		// Ensure that 0 and -0 have the same hash code
		return 0;
	}
	if (sizeof(PRT_FLOAT) == 4)
	{
		return PrtGetHashCodeUInt32((PRT_UINT32)value);
	}
	return PrtGetHashCodeUInt32((PRT_UINT32)value) ^ PrtGetHashCodeUInt32((PRT_UINT64)value >> 32);
}

static PRT_UINT32 PRT_CALL_CONV PrtGetHashCodePrtString(_In_ PRT_STRING value)
{
	if (value == NULL)
	{
		return 0;
	}
	PRT_UINT32 code = 5381;
	int c;
	while (c = *value++) 
	{
		code = ((code << 5) + code) + c; /* code * 33 + c */	
	}
	return code;
}

static PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeTwoUInt32(_In_ PRT_UINT32 value1, _In_ PRT_UINT32 value2)
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

static PRT_UINT32 PRT_CALL_CONV PrtGetHashCodeMachineId(_In_ PRT_MACHINEID id)
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

PRT_VALUE* PRT_CALL_CONV PrtMkBoolValue(_In_ PRT_BOOLEAN value)
{
	PrtAssert(value == PRT_TRUE || value == PRT_FALSE, "Expected a bool value");

	PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALUE_KIND_BOOL;
	retVal->valueUnion.bl = value;
	return retVal;
}

PRT_VALUE* PRT_CALL_CONV PrtMkEventValue(_In_ PRT_UINT32 value)
{
	PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALUE_KIND_EVENT;
	retVal->valueUnion.ev = value;
	return retVal;
}

PRT_VALUE* PRT_CALL_CONV PrtMkIntValue(_In_ PRT_INT value)
{
	PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALUE_KIND_INT;
	retVal->valueUnion.nt = value;
	return retVal;
}

PRT_VALUE* PRT_CALL_CONV PrtMkFloatValue(_In_ PRT_FLOAT value)
{
	PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALUE_KIND_FLOAT;
	retVal->valueUnion.ft = value;
	return retVal;
}

PRT_VALUE* PRT_CALL_CONV PrtMkStringValue(_In_ PRT_STRING value)
{
	PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALUE_KIND_STRING;
	retVal->valueUnion.str = value;
	return retVal;
}

PRT_VALUE* PRT_CALL_CONV PrtMkNullValue()
{
	PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	retVal->discriminator = PRT_VALUE_KIND_NULL;
	retVal->valueUnion.ev = PRT_SPECIAL_EVENT_NULL;
	return retVal;
}

PRT_VALUE* PRT_CALL_CONV PrtMkMachineValue(_In_ PRT_MACHINEID value)
{
	PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	PRT_MACHINEID* id = (PRT_MACHINEID *)PrtMalloc(sizeof(PRT_MACHINEID));
	retVal->discriminator = PRT_VALUE_KIND_MID;
	retVal->valueUnion.mid = id;
	id->machineId = value.machineId;
	id->processId.data1 = value.processId.data1;
	id->processId.data2 = value.processId.data2;
	id->processId.data3 = value.processId.data3;
	id->processId.data4 = value.processId.data4;
	return retVal;
}

PRT_VALUE* PRT_CALL_CONV PrtMkForeignValue(
	_In_	     PRT_UINT64 value,
	_In_	     PRT_TYPE* type)
{
	PrtAssert(type->typeKind == PRT_KIND_FOREIGN, "Bad type");
	PRT_UINT32 typeTag = type->typeUnion.foreignType->declIndex;
	PrtAssert(typeTag < program->nForeignTypes && program->foreignTypes[typeTag]->declIndex == typeTag, "Bad type tag");

	PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
	PRT_FOREIGNVALUE* frgn = (PRT_FOREIGNVALUE *)PrtMalloc(sizeof(PRT_FOREIGNVALUE));
	retVal->discriminator = PRT_VALUE_KIND_FOREIGN;
	retVal->valueUnion.frgn = frgn;
	frgn->typeTag = typeTag;
	frgn->value = program->foreignTypes[typeTag]->cloneFun(value);
	return retVal;
}

PRT_UINT64 PRT_CALL_CONV PrtGetForeignValue(PRT_VALUE* v)
{
	PrtAssert(v->discriminator == PRT_VALUE_KIND_FOREIGN, "Input value is not a foreign value");
	return v->valueUnion.frgn->value;
}

PRT_VALUE* PRT_CALL_CONV PrtMkDefaultValue(_In_ PRT_TYPE* type)
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
	case PRT_KIND_FLOAT:
		return PrtMkFloatValue(0);
	case PRT_KIND_STRING:
		return PrtMkStringValue((PRT_STRING) PrtCalloc(1, sizeof(PRT_CHAR)));
	case PRT_KIND_NULL:
		return PrtMkNullValue();
	case PRT_KIND_FOREIGN:
	{
		PRT_UINT32 declIndex = type->typeUnion.foreignType->declIndex;
		PrtAssert(declIndex < program->nForeignTypes && program->foreignTypes[declIndex]->declIndex == declIndex,
			"Invalid type expression.");
		PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_FOREIGNVALUE* frgn = (PRT_FOREIGNVALUE *)PrtMalloc(sizeof(PRT_FOREIGNVALUE));
		retVal->discriminator = PRT_VALUE_KIND_FOREIGN;
		retVal->valueUnion.frgn = frgn;
		frgn->typeTag = declIndex;
		frgn->value = program->foreignTypes[declIndex]->mkDefValueFun();
		return retVal;
	}
	case PRT_KIND_SET:
	{
		PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_SETVALUE* set = (PRT_SETVALUE *)PrtMalloc(sizeof(PRT_SETVALUE));
		retVal->discriminator = PRT_VALUE_KIND_SET;
		retVal->valueUnion.set = set;

		set->size = 0;
		set->capNum = 0;
		set->buckets = (PRT_SETNODE **)PrtCalloc(PrtHashtableCapacities[0], sizeof(PRT_SETNODE *));
		set->first = NULL;
		set->last = NULL;
		return retVal;
	}
	case PRT_KIND_MAP:
	{
		PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_MAPVALUE* map = (PRT_MAPVALUE *)PrtMalloc(sizeof(PRT_MAPVALUE));
		retVal->discriminator = PRT_VALUE_KIND_MAP;
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
		PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_TUPVALUE* tup = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		retVal->discriminator = PRT_VALUE_KIND_TUPLE;
		retVal->valueUnion.tuple = tup;
		PRT_UINT32 i;
		PRT_NMDTUPTYPE* ntype = type->typeUnion.nmTuple;
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
		PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_SEQVALUE* seq = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
		retVal->discriminator = PRT_VALUE_KIND_SEQ;
		retVal->valueUnion.seq = seq;

		seq->size = 0;
		seq->capacity = 0;
		seq->values = NULL;
		return retVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_TUPVALUE* tup = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		retVal->discriminator = PRT_VALUE_KIND_TUPLE;
		retVal->valueUnion.tuple = tup;
		PRT_UINT32 i;
		PRT_TUPTYPE* ttype = type->typeUnion.tuple;
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

void PRT_CALL_CONV PrtPrimSetBool(_Inout_ PRT_VALUE* prmVal, _In_ PRT_BOOLEAN value)
{
	PrtAssert(value == PRT_TRUE || value == PRT_FALSE, "Expected a bool value");
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_BOOL, "Invalid type on primitive set");
	prmVal->valueUnion.bl = value;
}

PRT_BOOLEAN PRT_CALL_CONV PrtPrimGetBool(_In_ PRT_VALUE* prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_BOOL, "Invalid type on primitive get");
	return prmVal->valueUnion.bl;
}

void PRT_CALL_CONV PrtPrimSetEvent(_Inout_ PRT_VALUE* prmVal, _In_ PRT_UINT32 value)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_EVENT, "Invalid type on primitive set");
	prmVal->valueUnion.ev = value;
}

PRT_UINT32 PRT_CALL_CONV PrtPrimGetEvent(_In_ PRT_VALUE* prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_EVENT, "Invalid type on primitive get");
	return prmVal->valueUnion.ev;
}

void PRT_CALL_CONV PrtPrimSetInt(_Inout_ PRT_VALUE* prmVal, _In_ PRT_INT value)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_INT, "Invalid type on primitive set");
	prmVal->valueUnion.nt = value;
}

PRT_INT PRT_CALL_CONV PrtPrimGetInt(_In_ PRT_VALUE* prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_INT, "Invalid type on primitive get");
	return prmVal->valueUnion.nt;
}

void PRT_CALL_CONV PrtPrimSetFloat(_Inout_ PRT_VALUE* prmVal, _In_ PRT_FLOAT value)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_FLOAT, "Invalid type on primitive set");
	prmVal->valueUnion.ft = value;
}

PRT_FLOAT PRT_CALL_CONV PrtPrimGetFloat(_In_ PRT_VALUE* prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_FLOAT, "Invalid type on primitive get");
	return prmVal->valueUnion.ft;
}

void PRT_CALL_CONV PrtPrimSetString(_Inout_ PRT_VALUE* prmVal, _In_ PRT_STRING value)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_STRING, "Invalid type on primitive set");
	prmVal->valueUnion.str = value;
}

PRT_STRING PRT_CALL_CONV PrtPrimGetString(_In_ PRT_VALUE* prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_STRING, "Invalid type on primitive get");
	return prmVal->valueUnion.str;
}

void PRT_CALL_CONV PrtPrimSetMachine(_Inout_ PRT_VALUE* prmVal, _In_ PRT_MACHINEID value)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_MID, "Invalid type on primitive set");
	PRT_MACHINEID* id = prmVal->valueUnion.mid;
	id->machineId = value.machineId;
	id->processId.data1 = value.processId.data1;
	id->processId.data2 = value.processId.data2;
	id->processId.data3 = value.processId.data3;
	id->processId.data4 = value.processId.data4;
}

PRT_MACHINEID PRT_CALL_CONV PrtPrimGetMachine(_In_ PRT_VALUE* prmVal)
{
	PrtAssert(PrtIsValidValue(prmVal), "Invalid value expression.");
	PrtAssert(prmVal->discriminator == PRT_VALUE_KIND_MID, "Invalid type on primitive get");
	return *prmVal->valueUnion.mid;
}

PRT_VALUE* PRT_CALL_CONV PrtStringConcat(_In_ PRT_VALUE* str1, _In_ PRT_VALUE* str2)
{
	printf("hi0");
	PrtAssert(PrtIsValidValue(str1), "Invalid value expression.");
	printf("hi1");
	PrtAssert(PrtIsValidValue(str2), "Invalid value expression.");
	printf("hi2");
	PrtAssert(str1->discriminator == PRT_VALUE_KIND_STRING, "Cannot perform string concatenation on this value");
	PrtAssert(str2->discriminator == PRT_VALUE_KIND_STRING, "Cannot perform concatenation on this value");
	PRT_STRING ret = PrtCalloc(1, sizeof(PRT_CHAR) * (strlen(str1->valueUnion.str) + strlen(str2->valueUnion.str) + 1));
	strcat(ret, str1->valueUnion.str);
	strcat(ret, str2->valueUnion.str);
	return PrtMkStringValue(ret);
}


void PRT_CALL_CONV PrtTupleSetEx(_Inout_ PRT_VALUE* tuple, _In_ PRT_UINT32 index, _In_ PRT_VALUE* value,
	PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(tuple), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(tuple->discriminator == PRT_VALUE_KIND_TUPLE, "Cannot perform tuple set on this value");
	PrtAssert(index < tuple->valueUnion.tuple->size, "Invalid tuple index");

	PRT_VALUE* oldValue = tuple->valueUnion.tuple->values[index];
	tuple->valueUnion.tuple->values[index] = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
	PrtFreeValue(oldValue);
}

void PRT_CALL_CONV PrtTupleSet(_Inout_ PRT_VALUE* tuple, _In_ PRT_UINT32 index, _In_ PRT_VALUE* value)
{
	PrtTupleSetEx(tuple, index, value, PRT_TRUE);
}

PRT_VALUE* PRT_CALL_CONV PrtTupleGet(_In_ PRT_VALUE* tuple, _In_ PRT_UINT32 index)
{
	PrtAssert(PrtIsValidValue(tuple), "Invalid value expression.");
	PrtAssert(tuple->discriminator == PRT_VALUE_KIND_TUPLE, "Cannot perform tuple get on this value");
	PrtAssert(index < tuple->valueUnion.tuple->size, "Invalid tuple index");

	return PrtCloneValue(tuple->valueUnion.tuple->values[index]);
}

PRT_VALUE** PRT_CALL_CONV PrtTupleGetLValue(_In_ PRT_VALUE* tuple, _In_ PRT_UINT32 index)
{
	PrtAssert(PrtIsValidValue(tuple), "Invalid value expression.");
	PrtAssert(tuple->discriminator == PRT_VALUE_KIND_TUPLE, "Cannot perform tuple get on this value");
	PrtAssert(index < tuple->valueUnion.tuple->size, "Invalid tuple index");

	return &tuple->valueUnion.tuple->values[index];
}

PRT_VALUE* PRT_CALL_CONV PrtTupleGetNC(_In_ PRT_VALUE* tuple, _In_ PRT_UINT32 index)
{
	PrtAssert(PrtIsValidValue(tuple), "Invalid value expression.");
	PrtAssert(tuple->discriminator == PRT_VALUE_KIND_TUPLE, "Cannot perform tuple get on this value");
	PrtAssert(index < tuple->valueUnion.tuple->size, "Invalid tuple index");

	return tuple->valueUnion.tuple->values[index];
}

void PRT_CALL_CONV PrtSeqUpdateEx(_Inout_ PRT_VALUE* seq, _In_ PRT_VALUE* index, _In_ PRT_VALUE* value,
	PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALUE_KIND_SEQ, "Invalid value");
	PrtAssert(index->discriminator == PRT_VALUE_KIND_INT, "Invalid value");
	PrtAssert(0 <= (PRT_UINT32)index->valueUnion.nt && (PRT_UINT32)index->valueUnion.nt <= seq->valueUnion.seq->size,
		"Invalid index");

	if ((PRT_UINT32)index->valueUnion.nt == seq->valueUnion.seq->size)
	{
		PrtSeqInsertEx(seq, index, value, cloneValue);
	}
	else
	{
		PRT_VALUE* oldValue = seq->valueUnion.seq->values[index->valueUnion.nt];
		seq->valueUnion.seq->values[index->valueUnion.nt] = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
		PrtFreeValue(oldValue);
	}
}

void PRT_CALL_CONV PrtSeqUpdate(_Inout_ PRT_VALUE* seq, _In_ PRT_VALUE* index, _In_ PRT_VALUE* value)
{
	PrtSeqUpdateEx(seq, index, value, PRT_TRUE);
}

void PRT_CALL_CONV PrtSeqInsertExIntIndex(_Inout_ PRT_VALUE* seq, _In_ PRT_INT index, _In_ PRT_VALUE* value,
	PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALUE_KIND_SEQ, "Invalid value");
	PrtAssert(0 <= index && (PRT_UINT32)index <= seq->valueUnion.seq->size, "Invalid index");

	PRT_VALUE* clone;
	clone = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
	if (seq->valueUnion.seq->capacity == 0)
	{
		seq->valueUnion.seq->values = (PRT_VALUE **)PrtMalloc(sizeof(PRT_VALUE*));
		seq->valueUnion.seq->values[0] = clone;
		seq->valueUnion.seq->capacity = 1;
	}
	else if (seq->valueUnion.seq->size < seq->valueUnion.seq->capacity)
	{
		PRT_INT i;
		PRT_VALUE** values = seq->valueUnion.seq->values;
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
		PRT_INT i;
		PRT_VALUE** values;
		seq->valueUnion.seq->capacity = 2 * seq->valueUnion.seq->capacity;
		values = (PRT_VALUE **)PrtCalloc(seq->valueUnion.seq->capacity, sizeof(PRT_VALUE*));
		for (i = 0; i < (PRT_INT)seq->valueUnion.seq->size; ++i)
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

PRT_VALUE** PrtSeqGetNCIntIndex(_In_ PRT_VALUE* seq, _In_ PRT_INT index)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALUE_KIND_SEQ, "Invalid value");
	PrtAssert(0 <= index && (PRT_UINT32)index < seq->valueUnion.seq->size, "Invalid index");

	return &seq->valueUnion.seq->values[index];
}

void PRT_CALL_CONV PrtSeqInsertEx(_Inout_ PRT_VALUE* seq, _In_ PRT_VALUE* index, _In_ PRT_VALUE* value,
	PRT_BOOLEAN cloneValue)
{
	PrtAssert(index->discriminator == PRT_VALUE_KIND_INT, "Invalid value");
	PrtSeqInsertExIntIndex(seq, index->valueUnion.nt, value, cloneValue);
}

void PRT_CALL_CONV PrtSeqInsert(_Inout_ PRT_VALUE* seq, _In_ PRT_VALUE* index, _In_ PRT_VALUE* value)
{
	PrtSeqInsertEx(seq, index, value, PRT_TRUE);
}

PRT_BOOLEAN PRT_CALL_CONV PrtSeqExists(_In_ PRT_VALUE* seq, _In_ PRT_VALUE* val)
{
	PRT_INT i;
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(val), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALUE_KIND_SEQ, "Invalid value");

	const PRT_UINT32 seqSize = seq->valueUnion.seq->size;
	for (i = 0; i < (PRT_INT)seqSize; i++)
	{
		if (PrtIsEqualValue(*PrtSeqGetNCIntIndex(seq, i), val))
		{
			return PRT_TRUE;
		}
	}
	return PRT_FALSE;
}

void PRT_CALL_CONV PrtSeqRemove(_Inout_ PRT_VALUE* seq, _In_ PRT_VALUE* index)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALUE_KIND_SEQ, "Invalid value");
	PrtAssert(index->discriminator == PRT_VALUE_KIND_INT, "Invalid value");
	PrtAssert(0 <= index->valueUnion.nt && (PRT_UINT32)index->valueUnion.nt < seq->valueUnion.seq->size, "Invalid index"
	);

	PRT_INT i;
	PRT_VALUE** values = seq->valueUnion.seq->values;
	PRT_UINT32 seqSize = seq->valueUnion.seq->size;
	PrtFreeValue(values[index->valueUnion.nt]);
	for (i = index->valueUnion.nt; i < (PRT_INT)seqSize - 1; ++i)
	{
		values[i] = values[i + 1];
	}

	seq->valueUnion.seq->size = seq->valueUnion.seq->size - 1;
}

PRT_VALUE* PRT_CALL_CONV PrtSeqGet(_In_ PRT_VALUE* seq, _In_ PRT_VALUE* index)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALUE_KIND_SEQ, "Invalid value");
	PrtAssert(index->discriminator == PRT_VALUE_KIND_INT, "Invalid value");
	PrtAssert(0 <= index->valueUnion.nt && (PRT_UINT32)index->valueUnion.nt < seq->valueUnion.seq->size, "Invalid index"
	);

	return PrtCloneValue(seq->valueUnion.seq->values[index->valueUnion.nt]);
}

PRT_VALUE* PRT_CALL_CONV PrtSeqGetNC(_In_ PRT_VALUE* seq, _In_ PRT_VALUE* index)
{
	PrtAssert(index->discriminator == PRT_VALUE_KIND_INT, "Invalid value");

	return *PrtSeqGetNCIntIndex(seq, index->valueUnion.nt);
}

PRT_VALUE** PRT_CALL_CONV PrtSeqGetLValue(_In_ PRT_VALUE* seq, _In_ PRT_VALUE* index)
{
	PrtAssert(index->discriminator == PRT_VALUE_KIND_INT, "Invalid value");

	return PrtSeqGetNCIntIndex(seq, index->valueUnion.nt);
}

PRT_UINT32 PRT_CALL_CONV PrtSeqSizeOf(_In_ PRT_VALUE* seq)
{
	PrtAssert(PrtIsValidValue(seq), "Invalid value expression.");
	PrtAssert(seq->discriminator == PRT_VALUE_KIND_SEQ, "Invalid value");

	return seq->valueUnion.seq->size;
}

/** Expands the set and rehashes its items */
static void PRT_CALL_CONV PrtSetExpand(_Inout_ PRT_VALUE* set)
{
	if (set->valueUnion.set->capNum + 1 >= sizeof(PrtHashtableCapacities) / sizeof(PRT_UINT32))
	{
		//// Set has reached maximum capacity.
		return;
	}

	set->valueUnion.set->capNum = set->valueUnion.set->capNum + 1;
	//// Erase all bucket-next pointers
	PRT_SETNODE* next = set->valueUnion.set->first;
	while (next != NULL)
	{
		next->bucketNext = NULL;
		next = next->insertNext;
	}

	//// Resize buckets
	PrtFree(set->valueUnion.set->buckets);
	set->valueUnion.set->buckets = (PRT_SETNODE **)PrtCalloc(PrtHashtableCapacities[set->valueUnion.set->capNum],
		sizeof(PRT_SETNODE *));

	//// Do the rehash, updating the bucketNext pointers
	PRT_UINT32 bucketNum;
	PRT_SETNODE* bucket;
	next = set->valueUnion.set->first;
	while (next != NULL)
	{
		bucketNum = PrtGetHashCodeValue(next->item) % PrtHashtableCapacities[set->valueUnion.set->capNum];
		bucket = set->valueUnion.set->buckets[bucketNum];
		if (bucket != NULL)
		{
			next->bucketNext = bucket;
		}

		set->valueUnion.set->buckets[bucketNum] = next;
		next = next->insertNext;
	}
}

PRT_VALUE** PrtSetAddHelper(_Inout_ PRT_VALUE* set, _In_ PRT_VALUE* item, _In_ PRT_BOOLEAN cloneItem)
{
	PrtAssert(PrtIsValidValue(set), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(item), "Invalid value expression.");
	PrtAssert(set->discriminator == PRT_VALUE_KIND_SET, "Invalid value");

	PRT_SETNODE* node;

	const PRT_UINT32 bucket_num = PrtGetHashCodeValue(item) % PrtHashtableCapacities[set->valueUnion.set->capNum];
	PRT_SETNODE* bucket = set->valueUnion.set->buckets[bucket_num];
	if (bucket == NULL)
	{
		node = (PRT_SETNODE *)PrtMalloc(sizeof(PRT_SETNODE));
		node->item = cloneItem == PRT_TRUE ? PrtCloneValue(item) : item;
		node->bucketNext = NULL;
		node->insertNext = NULL;
		set->valueUnion.set->buckets[bucket_num] = node;
	}
	else
	{
		PRT_SETNODE* next = bucket;
		while (next != NULL)
		{
			if (PrtIsEqualValue(next->item, item))
			{
				//// Then need to free the unused item.
				if (cloneItem != PRT_TRUE)
				{
					PrtFreeValue(item);
				}
				return &next->item;
			}
			next = next->bucketNext;
		}

		node = (PRT_SETNODE *)PrtMalloc(sizeof(PRT_SETNODE));
		node->item = cloneItem == PRT_TRUE ? PrtCloneValue(item) : item;
		node->bucketNext = bucket;
		node->insertNext = NULL;
		set->valueUnion.set->buckets[bucket_num] = node;
	}

	if (set->valueUnion.set->last == NULL)
	{
		set->valueUnion.set->first = node;
		set->valueUnion.set->last = node;
		node->insertPrev = NULL;
	}
	else
	{
		node->insertPrev = set->valueUnion.set->last;
		set->valueUnion.set->last->insertNext = node;
		set->valueUnion.set->last = node;
	}

	set->valueUnion.set->size = set->valueUnion.set->size + 1;

	if (((double)set->valueUnion.set->size) / ((double)PrtHashtableCapacities[set->valueUnion.set->capNum]) > ((double)
		PRT_MAXHASHLOAD))
	{
		PrtSetExpand(set);
	}

	return &node->item;
}


void PRT_CALL_CONV PrtSetAddEx(_Inout_ PRT_VALUE* set, _In_ PRT_VALUE* item, _In_ PRT_BOOLEAN cloneItem)
{
	PrtSetAddHelper(set, item, cloneItem);
}


void PRT_CALL_CONV PrtSetAdd(_Inout_ PRT_VALUE* set, _In_ PRT_VALUE* item)
{
	PrtSetAddEx(set, item, PRT_TRUE);
}

void PRT_CALL_CONV PrtSetRemove(_Inout_ PRT_VALUE* set, _In_ PRT_VALUE* item)
{
	PrtAssert(PrtIsValidValue(set), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(item), "Invalid value expression.");
	PrtAssert(set->discriminator == PRT_VALUE_KIND_SET, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_SETNODE* bucket;
	bucketNum = PrtGetHashCodeValue(item) % PrtHashtableCapacities[set->valueUnion.set->capNum];
	bucket = set->valueUnion.set->buckets[bucketNum];
	if (bucket == NULL)
	{
		return;
	}

	PRT_SETNODE* next = bucket;
	PRT_SETNODE* prev = NULL;
	while (next != NULL)
	{
		if (PrtIsEqualValue(next->item, item))
		{
			PrtFreeValue(next->item);
			if (next == bucket)
			{
				set->valueUnion.set->buckets[bucketNum] = next->bucketNext;
			}
			else
			{
				prev->bucketNext = next->bucketNext;
			}

			if (next->insertPrev == NULL)
			{
				//// Then this was the first item
				set->valueUnion.set->first = next->insertNext;
			}
			else
			{
				//// Otherwise the next of the previous item is the next of this item
				next->insertPrev->insertNext = next->insertNext;
			}

			if (next->insertNext == NULL)
			{
				//// Then this was the last item
				set->valueUnion.set->last = next->insertPrev;
			}
			else
			{
				//// Otherwise the previous of the next item is the previous of this item
				next->insertNext->insertPrev = next->insertPrev;
			}

			PrtFree(next);
			set->valueUnion.set->size = set->valueUnion.set->size - 1;
			return;
		}

		prev = next;
		next = next->bucketNext;
	}
}

PRT_BOOLEAN PRT_CALL_CONV PrtSetExists(_In_ PRT_VALUE* set, _In_ PRT_VALUE* item)
{
	PrtAssert(PrtIsValidValue(set), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(item), "Invalid value expression.");
	PrtAssert((set->discriminator == PRT_VALUE_KIND_SET), "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_SETNODE* bucket;
	bucketNum = PrtGetHashCodeValue(item) % PrtHashtableCapacities[set->valueUnion.set->capNum];
	bucket = set->valueUnion.set->buckets[bucketNum];
	if (bucket == NULL)
	{
		return PRT_FALSE;
	}

	PRT_SETNODE* next = bucket;
	while (next != NULL)
	{
		if (PrtIsEqualValue(next->item, item))
		{
			return PRT_TRUE;
		}

		next = next->bucketNext;
	}

	return PRT_FALSE;
}


PRT_UINT32 PRT_CALL_CONV PrtSetSizeOf(_In_ PRT_VALUE* set)
{
	PrtAssert(PrtIsValidValue(set), "Invalid value expression.");
	PrtAssert(set->discriminator == PRT_VALUE_KIND_SET, "Invalid value");

	return set->valueUnion.set->size;
}

PRT_UINT32 PRT_CALL_CONV PrtSetCapacity(_In_ PRT_VALUE* set)
{
	PrtAssert(PrtIsValidValue(set), "Invalid value expression.");
	PrtAssert(set->discriminator == PRT_VALUE_KIND_SET, "Invalid value");

	return PrtHashtableCapacities[set->valueUnion.set->capNum];
}


/** Expands the map and rehashes its key-value pairs */
static void PRT_CALL_CONV PrtMapExpand(_Inout_ PRT_VALUE* map)
{
	if (map->valueUnion.map->capNum + 1 >= sizeof(PrtHashtableCapacities) / sizeof(PRT_UINT32))
	{
		//// Map has reached maximum capacity.
		return;
	}

	map->valueUnion.map->capNum = map->valueUnion.map->capNum + 1;
	//// Erase all bucket-next pointers
	PRT_MAPNODE* next = map->valueUnion.map->first;
	while (next != NULL)
	{
		next->bucketNext = NULL;
		next = next->insertNext;
	}

	//// Resize buckets
	PrtFree(map->valueUnion.map->buckets);
	map->valueUnion.map->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[map->valueUnion.map->capNum],
		sizeof(PRT_MAPNODE *));

	//// Do the rehash, updating the bucketNext pointers
	PRT_UINT32 bucketNum;
	PRT_MAPNODE* bucket;
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

PRT_VALUE** PrtMapUpdateHelper(_Inout_ PRT_VALUE* map, _In_ PRT_VALUE* key, _In_ PRT_BOOLEAN cloneKey,
	_In_                                       PRT_VALUE* value,
	_In_                                       PRT_BOOLEAN cloneValue)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

	PRT_MAPNODE* node;

	const PRT_UINT32 bucket_num = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	PRT_MAPNODE* bucket = map->valueUnion.map->buckets[bucket_num];
	if (bucket == NULL)
	{
		node = (PRT_MAPNODE *)PrtMalloc(sizeof(PRT_MAPNODE));
		node->key = cloneKey == PRT_TRUE ? PrtCloneValue(key) : key;
		node->value = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
		node->bucketNext = NULL;
		node->insertNext = NULL;
		map->valueUnion.map->buckets[bucket_num] = node;
	}
	else
	{
		PRT_VALUE* valueClone = cloneValue == PRT_TRUE ? PrtCloneValue(value) : value;
		PRT_MAPNODE* next = bucket;
		while (next != NULL)
		{
			if (PrtIsEqualValue(next->key, key))
			{
				//// Then need to free the unused key.
				if (cloneKey != PRT_TRUE)
				{
					PrtFreeValue(key);
				}

				PRT_VALUE* oldValue = next->value;
				next->value = valueClone;
				PrtFreeValue(oldValue);

				return &next->value;
			}

			next = next->bucketNext;
		}

		node = (PRT_MAPNODE *)PrtMalloc(sizeof(PRT_MAPNODE));
		node->key = cloneKey == PRT_TRUE ? PrtCloneValue(key) : key;
		node->value = valueClone;
		node->bucketNext = bucket;
		node->insertNext = NULL;
		map->valueUnion.map->buckets[bucket_num] = node;
	}

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

	if (((double)map->valueUnion.map->size) / ((double)PrtHashtableCapacities[map->valueUnion.map->capNum]) > ((double)
		PRT_MAXHASHLOAD))
	{
		PrtMapExpand(map);
	}

	return &node->value;
}

void PRT_CALL_CONV PrtMapUpdateEx(_Inout_ PRT_VALUE* map, _In_ PRT_VALUE* key, _In_ PRT_BOOLEAN cloneKey,
	_In_                                          PRT_VALUE* value,
	_In_                                          PRT_BOOLEAN cloneValue)
{
	PrtMapUpdateHelper(map, key, cloneKey, value, cloneValue);
}

void PRT_CALL_CONV PrtMapUpdate(_Inout_ PRT_VALUE* map, _In_ PRT_VALUE* key, _In_ PRT_VALUE* value)
{
	PrtMapUpdateEx(map, key, PRT_TRUE, value, PRT_TRUE);
}

void PRT_CALL_CONV PrtMapRemove(_Inout_ PRT_VALUE* map, _In_ PRT_VALUE* key)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE* bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	if (bucket == NULL)
	{
		return;
	}

	PRT_MAPNODE* next = bucket;
	PRT_MAPNODE* prev = NULL;
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

static PRT_MAPNODE* PrtMapGetValueNode(_Inout_ PRT_VALUE* map, _In_ PRT_VALUE* key, _In_ PRT_BOOLEAN cloneKey)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

	const PRT_UINT32 bucket_num = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	PRT_MAPNODE* bucket = map->valueUnion.map->buckets[bucket_num];

	PRT_MAPNODE* next = bucket;
	while (next != NULL)
	{
		if (PrtIsEqualValue(next->key, key))
		{
			// We own key, therefore need to free the unused key.
			if (cloneKey != PRT_TRUE)
			{
				PrtFreeValue(key);
			}

			return next;
		}

		next = next->bucketNext;
	}

	// If we couldn't find a node matching our key in the bucket...
	PRT_MAPNODE* node = (PRT_MAPNODE *)PrtMalloc(sizeof(PRT_MAPNODE));
	PrtAssert(node != NULL, "PrtMalloc failed to allocate new map node");

	node->key = cloneKey == PRT_TRUE ? PrtCloneValue(key) : key;
	node->value = NULL;
	node->bucketNext = bucket;
	node->insertNext = NULL;
	map->valueUnion.map->buckets[bucket_num] = node;

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

	if (((double)map->valueUnion.map->size) / ((double)PrtHashtableCapacities[map->valueUnion.map->capNum]) > ((double)
		PRT_MAXHASHLOAD))
	{
		PrtMapExpand(map);
	}

	return node;
}

PRT_VALUE** PRT_CALL_CONV PrtMapGetLValue(_Inout_ PRT_VALUE* map, _In_ PRT_VALUE* key, _In_ PRT_BOOLEAN cloneKey,
	_In_                                                  PRT_TYPE*
	mapType)
{
	PrtAssert(PrtIsValidValue(map), "Invalid map in map-lvalue.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Map argument must be a map.");
	PrtAssert(mapType->typeKind == PRT_KIND_MAP, "Map type argument must be a map type.");

	PRT_MAPNODE* node = PrtMapGetValueNode(map, key, cloneKey);
	return &node->value;
}

PRT_VALUE* PRT_CALL_CONV PrtMapGet(_In_ PRT_VALUE* map, _In_ PRT_VALUE* key)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE* bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	PrtAssert(bucket != NULL, "Invalid map get; key not found");
	PRT_MAPNODE* next = bucket;
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

PRT_VALUE* PRT_CALL_CONV PrtMapGetNC(_In_ PRT_VALUE* map, _In_ PRT_VALUE* key)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE* bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	PrtAssert(bucket != NULL, "Invalid map get; key not found");
	PRT_MAPNODE* next = bucket;
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

PRT_VALUE* PRT_CALL_CONV PrtMapGetKeys(_In_ PRT_VALUE* map)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

	PRT_VALUE* retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
	PRT_SEQVALUE* seqVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
	retVal->discriminator = PRT_VALUE_KIND_SEQ;
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

PRT_VALUE* PRT_CALL_CONV PrtMapGetValues(_In_ PRT_VALUE* map)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

	PRT_VALUE* retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
	PRT_SEQVALUE* seqVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
	retVal->discriminator = PRT_VALUE_KIND_SEQ;
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

PRT_BOOLEAN PRT_CALL_CONV PrtMapExists(_In_ PRT_VALUE* map, _In_ PRT_VALUE* key)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert((map->discriminator == PRT_VALUE_KIND_MAP), "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE* bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	if (bucket == NULL)
	{
		return PRT_FALSE;
	}

	PRT_MAPNODE* next = bucket;
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

static PRT_BOOLEAN PRT_CALL_CONV PrtMapIsSameMapping(_In_ PRT_VALUE* map, _In_ PRT_VALUE* key, _In_ PRT_VALUE* value)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(key), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

	PRT_UINT32 bucketNum;
	PRT_MAPNODE* bucket;
	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->valueUnion.map->capNum];
	bucket = map->valueUnion.map->buckets[bucketNum];
	if (bucket == NULL)
	{
		return PRT_FALSE;
	}

	PRT_MAPNODE* next = bucket;
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

PRT_UINT32 PRT_CALL_CONV PrtMapSizeOf(_In_ PRT_VALUE* map)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

	return map->valueUnion.map->size;
}

PRT_UINT32 PRT_CALL_CONV PrtMapCapacity(_In_ PRT_VALUE* map)
{
	PrtAssert(PrtIsValidValue(map), "Invalid value expression.");
	PrtAssert(map->discriminator == PRT_VALUE_KIND_MAP, "Invalid value");

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
	case PRT_VALUE_KIND_BOOL:
		return PrtGetHashCodeUInt32(0x00400000 ^ ((PRT_UINT32)inputValue->valueUnion.bl));
	case PRT_VALUE_KIND_EVENT:
		return PrtGetHashCodeUInt32(0x00800000 ^ (inputValue->valueUnion.ev));
	case PRT_VALUE_KIND_MID:
		return PrtGetHashCodeUInt32(0x01000000 ^ PrtGetHashCodeMachineId(*inputValue->valueUnion.mid));
	case PRT_VALUE_KIND_INT:
		return PrtGetHashCodePrtInt(0x02000000 ^ ((PRT_INT)inputValue->valueUnion.nt));
	case PRT_VALUE_KIND_FLOAT:
		return PrtGetHashCodePrtFloat((inputValue->valueUnion.ft));
	case PRT_VALUE_KIND_STRING:
		return PrtGetHashCodePrtString((inputValue->valueUnion.str));
	case PRT_VALUE_KIND_FOREIGN:
	{
		return 0x08000000 ^ program->foreignTypes[inputValue->valueUnion.frgn->typeTag]->hashFun(
			inputValue->valueUnion.frgn->value);
	}
	case PRT_VALUE_KIND_MAP:
	{
		//// Hash function designed so two maps with same key-value pairs are hashed equally (independently of order).
		//// Hash codes are added on the finite field Z_{PRT_HASH_AC_COMPOSEMOD}.
		PRT_MAPVALUE* mVal = inputValue->valueUnion.map;
		PRT_MAPNODE* next = mVal->first;
		PRT_UINT64 code = 1;
		PRT_UINT64 pointCode;
		while (next != NULL)
		{
			pointCode = (PRT_UINT64)PrtGetHashCodeTwoUInt32(PrtGetHashCodeValue(next->key),
				PrtGetHashCodeValue(next->value));
			if (pointCode == 0)
			{
				pointCode = 1;
			}

			code = (code + pointCode) % PRT_HASH_AC_COMPOSEMOD;
			next = next->insertNext;
		}
		return 0x10000000 ^ (PRT_UINT32)code;
	}
	case PRT_VALUE_KIND_SET:
	{
		PRT_SETVALUE* uVal = inputValue->valueUnion.set;
		PRT_SETNODE* next = uVal->first;
		PRT_UINT64 code = 1;
		PRT_UINT64 pointCode;
		while (next != NULL)
		{
			pointCode = (PRT_UINT64)PrtGetHashCodeUInt32(PrtGetHashCodeValue(next->item));
			if (pointCode == 0)
			{
				pointCode = 1;
			}

			code = (code + pointCode) % PRT_HASH_AC_COMPOSEMOD;
			next = next->insertNext;
		}

		return 0x10000000 ^ (PRT_UINT32)code;
	}

	case PRT_VALUE_KIND_SEQ:
	{
		PRT_UINT32 i;
		PRT_UINT32 j;
		PRT_UINT32 code = 0;
		PRT_UINT32 pointCode;
		PRT_SEQVALUE* sVal = inputValue->valueUnion.seq;
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
	case PRT_VALUE_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_UINT32 j;
		PRT_UINT32 code = 0;
		PRT_UINT32 pointCode;
		PRT_TUPVALUE* tVal = inputValue->valueUnion.tuple;
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

PRT_API void PRT_CALL_CONV PrtRemoveByKey(_Inout_ PRT_VALUE* collection, _In_ PRT_VALUE* key)
{
	if (collection->discriminator == PRT_VALUE_KIND_MAP)
	{
		PrtMapRemove(collection, key);
	}
	else if (collection->discriminator == PRT_VALUE_KIND_SEQ)
	{
		PrtSeqRemove(collection, key);
	}
	else if (collection->discriminator == PRT_VALUE_KIND_SET)
	{
		PrtSetRemove(collection, key);
	}
	else
	{
		PrtAssert(PRT_FALSE, "Can only remove elements from a map, set or sequence.");
	}
}

PRT_BOOLEAN PRT_CALL_CONV PrtIsEqualValue(_In_ PRT_VALUE* value1, _In_ PRT_VALUE* value2)
{
	PrtAssert(PrtIsValidValue(value1), "Invalid value expression.");
	PrtAssert(PrtIsValidValue(value2), "Invalid value expression.");

	PRT_VALUE_KIND kind1 = value1->discriminator;
	PRT_VALUE_KIND kind2 = value2->discriminator;

	if (PrtIsNullValue(value1) && PrtIsNullValue(value2))
	{
		return PRT_TRUE;
	}
	if (kind1 != kind2)
	{
		return PRT_FALSE;
	}
	if (value1 == value2)
	{
		return PRT_TRUE;
	}

	switch (kind1)
	{
	case PRT_VALUE_KIND_NULL:
		//// Checked for equality with null earlier.
		return PRT_FALSE;
	case PRT_VALUE_KIND_BOOL:
		return
			value1->valueUnion.bl == value2->valueUnion.bl ? PRT_TRUE : PRT_FALSE;
	case PRT_VALUE_KIND_EVENT:
		return
			value1->valueUnion.ev == value2->valueUnion.ev ? PRT_TRUE : PRT_FALSE;
	case PRT_VALUE_KIND_MID:
	{
		PRT_MACHINEID* id1 = value1->valueUnion.mid;
		PRT_MACHINEID* id2 = value2->valueUnion.mid;
		return
			id1->processId.data1 == id2->processId.data1 &&
			id1->processId.data2 == id2->processId.data2 &&
			id1->processId.data3 == id2->processId.data3 &&
			id1->processId.data4 == id2->processId.data4 &&
			id1->machineId == id2->machineId
			? PRT_TRUE
			: PRT_FALSE;
	}
	case PRT_VALUE_KIND_INT:
		return
			value1->valueUnion.nt == value2->valueUnion.nt ? PRT_TRUE : PRT_FALSE;
	case PRT_VALUE_KIND_FLOAT:
		return
			value1->valueUnion.ft == value2->valueUnion.ft ? PRT_TRUE : PRT_FALSE;
	case PRT_VALUE_KIND_STRING:
		return 
			strcmp(value1->valueUnion.str, value2->valueUnion.str)==0 ? PRT_TRUE : PRT_FALSE;
	case PRT_VALUE_KIND_FOREIGN:
		{
			PRT_FOREIGNVALUE* fVal1 = value1->valueUnion.frgn;
			PRT_FOREIGNVALUE* fVal2 = value2->valueUnion.frgn;
			return (fVal1->typeTag == fVal2->typeTag)
				       ? program->foreignTypes[fVal1->typeTag]->isEqualFun(fVal1->value, fVal2->value)
				       : PRT_FALSE;
		}
	case PRT_VALUE_KIND_SET: 
		{
			PRT_SETVALUE* uVal1 = value1->valueUnion.set;
			PRT_SETVALUE* uVal2 = value2->valueUnion.set;
			if (uVal1->size != uVal2->size)
			{
				return PRT_FALSE;
			}

			PRT_SETNODE* next = uVal1->first;
			while (next != NULL)
			{
				if (!PrtSetExists(value2, next->item))
				{
					return PRT_FALSE;
				}

				next = next->insertNext;
			}

			return PRT_TRUE;
		}

	case PRT_VALUE_KIND_MAP:
	{
		PRT_MAPVALUE* mVal1 = value1->valueUnion.map;
		PRT_MAPVALUE* mVal2 = value2->valueUnion.map;

		if (mVal1->size != mVal2->size)
		{
			return PRT_FALSE;
		}

		PRT_MAPNODE* next = mVal1->first;
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
	case PRT_VALUE_KIND_SEQ:
	{
		PRT_UINT32 i;
		PRT_SEQVALUE* sVal1 = value1->valueUnion.seq;
		PRT_SEQVALUE* sVal2 = value2->valueUnion.seq;

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
	case PRT_VALUE_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE* tVal1 = value1->valueUnion.tuple;
		PRT_TUPVALUE* tVal2 = value2->valueUnion.tuple;

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

PRT_VALUE* PRT_CALL_CONV PrtCloneValue(_In_ PRT_VALUE* value)
{
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");

	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALUE_KIND_NULL:
		return PrtMkNullValue();
	case PRT_VALUE_KIND_BOOL:
		return PrtMkBoolValue(value->valueUnion.bl);
	case PRT_VALUE_KIND_EVENT:
		return PrtMkEventValue(value->valueUnion.ev);
	case PRT_VALUE_KIND_MID:
		return PrtMkMachineValue(*value->valueUnion.mid);
	case PRT_VALUE_KIND_INT:
		return PrtMkIntValue(value->valueUnion.nt);
	case PRT_VALUE_KIND_FLOAT:
		return PrtMkFloatValue(value->valueUnion.ft);
	case PRT_VALUE_KIND_STRING: 
	{
		PRT_STRING copy = (PRT_STRING) PrtMalloc(strlen(value->valueUnion.str) + 1);
		strcpy(copy, value->valueUnion.str);
		return PrtMkStringValue(copy);
	}
	case PRT_VALUE_KIND_FOREIGN:
	{
		PRT_VALUE* retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		PRT_FOREIGNVALUE* fVal = value->valueUnion.frgn;
		PRT_FOREIGNVALUE* cVal = (PRT_FOREIGNVALUE *)PrtMalloc(sizeof(PRT_FOREIGNVALUE));
		retVal->discriminator = PRT_VALUE_KIND_FOREIGN;
		retVal->valueUnion.frgn = cVal;
		cVal->typeTag = fVal->typeTag;
		cVal->value = program->foreignTypes[fVal->typeTag]->cloneFun(fVal->value);
		return retVal;
	}
	case PRT_VALUE_KIND_MAP:
	{
		PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_MAPVALUE* map = (PRT_MAPVALUE *)PrtMalloc(sizeof(PRT_MAPVALUE));
		retVal->discriminator = PRT_VALUE_KIND_MAP;
		retVal->valueUnion.map = map;
		PRT_MAPVALUE* mVal = value->valueUnion.map;
		map->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[mVal->capNum], sizeof(PRT_MAPNODE *));
		map->capNum = mVal->capNum;
		map->size = 0;
		map->first = NULL;
		map->last = NULL;
		PRT_MAPNODE* next = mVal->first;
		while (next != NULL)
		{
			PrtMapUpdate(retVal, next->key, next->value);
			next = next->insertNext;
		}

		return retVal;
	}

	case PRT_VALUE_KIND_SET:
		{
			PRT_VALUE* retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
			PRT_SETVALUE* set = (PRT_SETVALUE *)PrtMalloc(sizeof(PRT_SETVALUE));
			retVal->discriminator = PRT_VALUE_KIND_SET;
			retVal->valueUnion.set = set;
			PRT_SETVALUE* uVal = value->valueUnion.set;
			set->buckets = (PRT_SETNODE **)PrtCalloc(PrtHashtableCapacities[uVal->capNum], sizeof(PRT_SETNODE *));
			set->capNum = uVal->capNum;
			set->size = 0;
			set->first = NULL;
			set->last = NULL;
			PRT_SETNODE* next = uVal->first;
			while (next != NULL)
			{
				PrtSetAdd(retVal, next->item);
				next = next->insertNext;
			}

			return retVal;
		}

	case PRT_VALUE_KIND_TUPLE:
	{
		PRT_VALUE* retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		PRT_TUPVALUE* cVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		retVal->discriminator = PRT_VALUE_KIND_TUPLE;
		retVal->valueUnion.tuple = cVal;

		PRT_UINT32 i;
		PRT_TUPVALUE* tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->valueUnion.tuple->size;
		cVal->size = arity;
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE *));
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtCloneValue(tVal->values[i]);
		}

		return retVal;
	}
	case PRT_VALUE_KIND_SEQ:
	{
		PRT_VALUE* retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		PRT_SEQVALUE* cVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
		retVal->discriminator = PRT_VALUE_KIND_SEQ;
		retVal->valueUnion.seq = cVal;

		PRT_SEQVALUE* sVal = value->valueUnion.seq;
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

PRT_BOOLEAN PRT_CALL_CONV PrtIsNullValue(_In_ PRT_VALUE* value)
{
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");

	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALUE_KIND_NULL:
		return PRT_TRUE;
	case PRT_VALUE_KIND_EVENT:
		return value->valueUnion.ev == PRT_SPECIAL_EVENT_NULL ? PRT_TRUE : PRT_FALSE;
	case PRT_VALUE_KIND_MID:
	{
		PRT_MACHINEID* id = value->valueUnion.mid;
		return
			id->processId.data1 == PrtNullMachineId.processId.data1 &&
			id->processId.data2 == PrtNullMachineId.processId.data2 &&
			id->processId.data3 == PrtNullMachineId.processId.data3 &&
			id->processId.data4 == PrtNullMachineId.processId.data4 &&
			id->machineId == PrtNullMachineId.machineId
			? PRT_TRUE
			: PRT_FALSE;
	}
	case PRT_VALUE_KIND_BOOL:
	case PRT_VALUE_KIND_INT:
	case PRT_VALUE_KIND_FLOAT:
	case PRT_VALUE_KIND_FOREIGN:
	case PRT_VALUE_KIND_SET:
	case PRT_VALUE_KIND_MAP:
	case PRT_VALUE_KIND_TUPLE:
	case PRT_VALUE_KIND_STRING:
	case PRT_VALUE_KIND_SEQ:
		return PRT_FALSE;
	default:
		PrtAssert(PRT_FALSE, "PrtIsNullValue: Invalid value");
		return PRT_FALSE;
	}
}

PRT_VALUE* PRT_CALL_CONV PrtConvertValue(_In_ PRT_VALUE* value, _In_ PRT_TYPE* type)
{
	PrtAssert(
		value->discriminator == PRT_VALUE_KIND_FLOAT
		|| value->discriminator == PRT_VALUE_KIND_INT
		|| value->discriminator == PRT_VALUE_KIND_MID, "Invalid value expression.");
	PrtAssert(
		type->typeKind == PRT_KIND_INT
		|| type->typeKind == PRT_KIND_FLOAT
		|| type->typeKind == PRT_KIND_MACHINE, "Invalid type expression.");

	switch (type->typeKind)
	{
	case PRT_KIND_MACHINE:
		return PrtCloneValue(value);
	case PRT_KIND_INT:
		return PrtMkIntValue(value->discriminator == PRT_VALUE_KIND_FLOAT
			? (PRT_INT)value->valueUnion.ft
			: value->valueUnion.nt);
	case PRT_KIND_FLOAT:
		return PrtMkFloatValue(value->discriminator == PRT_VALUE_KIND_FLOAT
			? value->valueUnion.ft
			: value->valueUnion.nt);
	default:
		PrtAssert(PRT_FALSE, "Illegal convert invocation");
		return NULL;
	}
}

PRT_VALUE* PRT_CALL_CONV PrtCastValue(_In_ PRT_VALUE* value, _In_ PRT_TYPE* type)
{
	PrtAssert(PrtIsValidValue(value), "Invalid value expression.");
	PrtAssert(PrtIsValidType(type), "Invalid type expression.");
	PrtAssert(PrtInhabitsType(value, type), "Invalid type cast");
	return value;
}

PRT_BOOLEAN PRT_CALL_CONV PrtInhabitsType(_In_ PRT_VALUE* value, _In_ PRT_TYPE* type)
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
		return vkind == PRT_VALUE_KIND_BOOL ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_EVENT:
		return (vkind == PRT_VALUE_KIND_EVENT || PrtIsNullValue(value)) ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MACHINE:
		return (vkind == PRT_VALUE_KIND_MID || PrtIsNullValue(value)) ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_INT:
		return vkind == PRT_VALUE_KIND_INT ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_FLOAT:
		return vkind == PRT_VALUE_KIND_FLOAT ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_STRING:
		return vkind == PRT_VALUE_KIND_STRING ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_FOREIGN:
		return (vkind == PRT_VALUE_KIND_FOREIGN && value->valueUnion.frgn->typeTag == type
			->typeUnion.foreignType->declIndex
			)
			? PRT_TRUE
			: PRT_FALSE;
	case PRT_KIND_MAP:
	{
		if (vkind != PRT_VALUE_KIND_MAP)
		{
			return PRT_FALSE;
		}

		PRT_MAPVALUE* mVal = value->valueUnion.map;
		PRT_MAPTYPE* mType = type->typeUnion.map;
		PRT_MAPNODE* next = mVal->first;
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

	case PRT_KIND_SET:
	{
		if (vkind != PRT_VALUE_KIND_SET)
		{
			return PRT_FALSE;
		}

		PRT_SETVALUE* uVal = value->valueUnion.set;
		PRT_SETTYPE* uType = type->typeUnion.set;
		PRT_SETNODE* next = uVal->first;
		while (next != NULL)
		{
			if (!PrtInhabitsType(next->item, uType->innerType))
			{
				return PRT_FALSE;
			}

			next = next->insertNext;
		}

		return PRT_TRUE;
	}
	case PRT_KIND_NMDTUP:
	{
		if (vkind != PRT_VALUE_KIND_TUPLE)
		{
			return PRT_FALSE;
		}

		PRT_TUPVALUE* tVal = value->valueUnion.tuple;
		PRT_NMDTUPTYPE* tType = type->typeUnion.nmTuple;
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
		if (vkind != PRT_VALUE_KIND_TUPLE)
		{
			return PRT_FALSE;
		}

		PRT_TUPVALUE* tVal = value->valueUnion.tuple;
		PRT_TUPTYPE* tType = type->typeUnion.tuple;
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
		if (vkind != PRT_VALUE_KIND_SEQ)
		{
			return PRT_FALSE;
		}

		PRT_SEQVALUE* sVal = value->valueUnion.seq;
		PRT_SEQTYPE* sType = type->typeUnion.seq;
		if (sVal->size == 0)
		{
			return PRT_TRUE;
		}
		PRT_UINT32 i;
		for (i = 0; i < sVal->size; ++i)
		{
			if (!PrtInhabitsType(sVal->values[i], sType->innerType))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtInhabitsType: Invalid type");
		return PRT_FALSE;
	}
}

void PRT_CALL_CONV PrtFreeValue(_Inout_ PRT_VALUE* value)
{
	if (value == NULL)
	{
		return;
	}

	const PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALUE_KIND_BOOL:
	case PRT_VALUE_KIND_EVENT:
	case PRT_VALUE_KIND_INT:
	case PRT_VALUE_KIND_FLOAT:
	case PRT_VALUE_KIND_NULL:
	{
		PrtFree(value);
		break;
	}
	case PRT_VALUE_KIND_STRING:
	{
		PrtFree(value->valueUnion.str);
		PrtFree(value);
		break;
	}
	case PRT_VALUE_KIND_MID:
	{
		PRT_MACHINEID* id = value->valueUnion.mid;
		PrtFree(id);
		PrtFree(value);
		break;
	}
	case PRT_VALUE_KIND_FOREIGN:
	{
		PRT_FOREIGNVALUE* fVal = value->valueUnion.frgn;
		program->foreignTypes[fVal->typeTag]->freeFun(fVal->value);
		PrtFree(fVal);
		PrtFree(value);
		break;
	}
	case PRT_VALUE_KIND_MAP:
	{
		PRT_MAPVALUE* mVal = value->valueUnion.map;
		PRT_MAPNODE* next = mVal->first;
		PRT_MAPNODE* tmp;
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
	case PRT_VALUE_KIND_SET:
	{
		PRT_SETVALUE* uVal = value->valueUnion.set;
		PRT_SETNODE* next = uVal->first;
		PRT_SETNODE* tmp;
		while (next != NULL)
		{
			tmp = next->insertNext;
			PrtFreeValue(next->item);
			PrtFree(next);
			next = tmp;
		}

		PrtFree(uVal->buckets);
		PrtFree(uVal);
		PrtFree(value);
		break;
	}
	case PRT_VALUE_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE* tVal = value->valueUnion.tuple;
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
	case PRT_VALUE_KIND_SEQ:
	{
		PRT_SEQVALUE* sVal = value->valueUnion.seq;
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
	{
		PrtAssert(PRT_FALSE, "PrtFreeValue: Invalid value");
		break;
	}
	}
}

PRT_BOOLEAN PRT_CALL_CONV PrtIsValidValue(_In_ PRT_VALUE* value)
{
	if (value == NULL)
	{
		return PRT_FALSE;
	}

	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALUE_KIND_BOOL:
		return value->discriminator == PRT_VALUE_KIND_BOOL &&
			(value->valueUnion.bl == PRT_TRUE || value->valueUnion.bl == PRT_FALSE);
	case PRT_VALUE_KIND_EVENT:
		return value->discriminator == PRT_VALUE_KIND_EVENT;
	case PRT_VALUE_KIND_MID:
		return value->discriminator == PRT_VALUE_KIND_MID;
	case PRT_VALUE_KIND_INT:
		return value->discriminator == PRT_VALUE_KIND_INT;
	case PRT_VALUE_KIND_FLOAT:
		return value->discriminator == PRT_VALUE_KIND_FLOAT;
	case PRT_VALUE_KIND_STRING:
		return value->discriminator == PRT_VALUE_KIND_STRING;
	case PRT_VALUE_KIND_NULL:
		return value->discriminator == PRT_VALUE_KIND_NULL &&
			value->valueUnion.ev == PRT_SPECIAL_EVENT_NULL;
	case PRT_VALUE_KIND_FOREIGN:
		return value->discriminator == PRT_VALUE_KIND_FOREIGN &&
			value->valueUnion.frgn != NULL &&
			value->valueUnion.frgn->typeTag < program->nForeignTypes &&
			program->foreignTypes[value->valueUnion.frgn->typeTag]->declIndex == value->valueUnion.frgn->typeTag;
	case PRT_VALUE_KIND_MAP:
		return value->discriminator == PRT_VALUE_KIND_MAP &&
			value->valueUnion.map != NULL;
	case PRT_VALUE_KIND_SET:
		return value->discriminator == PRT_VALUE_KIND_SET &&
			value->valueUnion.set != NULL;
	case PRT_VALUE_KIND_SEQ:
		return value->discriminator == PRT_VALUE_KIND_SEQ &&
			value->valueUnion.seq != NULL;
	case PRT_VALUE_KIND_TUPLE:
		return value->discriminator == PRT_VALUE_KIND_TUPLE &&
			value->valueUnion.tuple != NULL &&
			value->valueUnion.tuple->values != NULL;
	default:
		PrtAssert(PRT_FALSE, "Invalid value");
		return PRT_FALSE;
	}
}

PRT_VALUE* PRT_CALL_CONV PrtMkNondetBoolValue()
{
	return PrtMkBoolValue(PrtChoose());
}