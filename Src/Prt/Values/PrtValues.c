#include "PrtValues.h"

/** Maximum load factor before hashtable is resized. */
#define PRT_MAXHASHLOAD 0.75

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
		mapVal->capNum = 0;
		mapVal->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[0], sizeof(PRT_MAPNODE *));
		mapVal->first = NULL;
		mapVal->last = NULL;
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

/** Expands the map and rehashes its key-value pairs */
void PrtMapExpand(_Inout_ PRT_MAPVALUE *map)
{
	if (map->capNum + 1 >= sizeof(PrtHashtableCapacities) / sizeof(PRT_UINT32))
	{
		//// Map has reached maximum capacity.
		return;
	}

	map->capNum = map->capNum + 1;
	//// Erase all bucket-next pointers
	PRT_MAPNODE *next = map->first;
	while (next != NULL)
	{
		next->bucketNext = NULL;
		next = next->insertNext;
	}

	//// Resize buckets
	PrtFree(map->buckets);
	map->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities[map->capNum], sizeof(PRT_MAPNODE *));

	//// Do the rehash, updating the bucketNext pointers
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	next = map->first;
	while (next != NULL)
	{
		bucketNum = PrtGetHashCodeValue(next->key) % PrtHashtableCapacities[map->capNum];
		bucket = map->buckets[bucketNum];
		if (bucket != NULL)
		{
			next->bucketNext = bucket;
		}

		map->buckets[bucketNum] = next;
		next = next->insertNext;
	}
}

void PrtMapUpdate(_Inout_ PRT_MAPVALUE *map, _In_ PRT_VALUE key, _In_ PRT_VALUE value)
{
	PRT_MAPTYPE *mapType;
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;
	PRT_MAPNODE *node = NULL;
	PRT_BOOLEAN isNewKey = PRT_FALSE;

	PrtAssert(*(map->type) == PRT_KIND_MAP, "Invalid map value");
	PrtAssert(*(*key) >= 0 && *(*key) < PRT_TYPE_KIND_COUNT, "Invalid key");
	PrtAssert(*(*value) >= 0 && *(*value) < PRT_TYPE_KIND_COUNT, "Invalid value");

	mapType = (PRT_MAPTYPE *)map->type;
	PrtAssert(PrtIsSubtype(*key, mapType->domType), "Invalid map update; key has bad type");
	PrtAssert(PrtIsSubtype(*value, mapType->codType), "Invalid map update; value has bad type");

	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->capNum];
	bucket = map->buckets[bucketNum];

	if (bucket == NULL)
	{
		isNewKey = PRT_TRUE;
		node = (PRT_MAPNODE *)PrtMalloc(sizeof(PRT_MAPNODE));
		node->key = PrtCloneValue(key);
		node->value = PrtCloneValue(value);
		node->bucketNext = NULL;
		node->insertNext = NULL;
		map->buckets[bucketNum] = node;
	}
	else
	{
		PRT_VALUE valueClone = PrtCloneValue(value);
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
			node = (PRT_MAPNODE *)PrtMalloc(sizeof(PRT_MAPNODE));
			node->key = PrtCloneValue(key);
			node->value = valueClone;
			node->bucketNext = bucket;
			node->insertNext = NULL;
			map->buckets[bucketNum] = node;
		}
	}

	if (isNewKey == PRT_TRUE)
	{
		if (map->last == NULL)
		{
			map->first = node;
			map->last = node;
			node->insertPrev = NULL;
		}
		else 
		{
			node->insertPrev = map->last;
			map->last->insertNext = node;
			map->last = node;
		}

		map->size = map->size + 1;

		if (((double)map->size) / ((double)PrtHashtableCapacities[map->capNum]) > ((double)PRT_MAXHASHLOAD))
		{
			PrtMapExpand(map);
		}
	}
}

void PrtMapRemove(_Inout_ PRT_MAPVALUE *map, _In_ PRT_VALUE key)
{
	PRT_MAPTYPE *mapType;
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;

	PrtAssert(*(map->type) == PRT_KIND_MAP, "Invalid map value");
	PrtAssert(*(*key) >= 0 && *(*key) < PRT_TYPE_KIND_COUNT, "Invalid key");
	mapType = (PRT_MAPTYPE *)map->type;
	PrtAssert(PrtIsSubtype(*key, mapType->domType), "Invalid map remove; key has bad type");

	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->capNum];
	bucket = map->buckets[bucketNum];
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
				map->buckets[bucketNum] = next->bucketNext;
			}
			else 
			{
				prev->bucketNext = next->bucketNext;
			}

			if (next->insertPrev == NULL)
			{
				//// Then this was the first key
				map->first = next->insertNext;
			}
			else 
			{
				//// Otherwise the next of the previous key is the next of this key
				next->insertPrev->insertNext = next->insertNext;
			}

			if (next->insertNext == NULL)
			{
				//// Then this was the last key
				map->last = next->insertPrev;
			}
			else 
			{
				//// Otherwise the previous of the next key is the previous of this key
				next->insertNext->insertPrev = next->insertPrev;
			}

			PrtFree(next);
			map->size = map->size - 1;
			return;
		}

		prev = next;
		next = next->bucketNext;
	}
}

PRT_VALUE PrtMapGet(_In_ PRT_MAPVALUE *map, _In_ PRT_VALUE key)
{
	PRT_MAPTYPE *mapType;
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;

	PrtAssert(*(map->type) == PRT_KIND_MAP, "Invalid map value");
	PrtAssert(*(*key) >= 0 && *(*key) < PRT_TYPE_KIND_COUNT, "Invalid key");
	mapType = (PRT_MAPTYPE *)map->type;
	PrtAssert(PrtIsSubtype(*key, mapType->domType), "Invalid map get; key has bad type");

	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->capNum];
	bucket = map->buckets[bucketNum];
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

PRT_SEQVALUE *PrtMapGetKeys(_In_ PRT_MAPVALUE *map)
{
	PrtAssert(*(map->type) == PRT_KIND_MAP, "Invalid map value");
	PRT_MAPTYPE *mapType = (PRT_MAPTYPE *)map->type;
	PRT_SEQVALUE *seqVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
	seqVal->type = (PRT_TYPE)PrtMkSeqType(mapType->domType);

	if (map->size == 0)
	{
		seqVal->size = 0;
		seqVal->capacity = 0;
		seqVal->values = NULL;
	}
	else 
	{
		seqVal->size = map->size;
		seqVal->capacity = map->size;
		seqVal->values = (PRT_VALUE *)PrtCalloc(map->size, sizeof(PRT_VALUE));
		PRT_MAPNODE* next = map->first;
		PRT_UINT32 i = 0;
		while (next != NULL)
		{
			seqVal->values[i] = PrtCloneValue(next->key);
			++i;
			next = next->insertNext;
		}
	}

	return seqVal;
}

PRT_SEQVALUE *PrtMapGetValues(_In_ PRT_MAPVALUE *map)
{
	PrtAssert(*(map->type) == PRT_KIND_MAP, "Invalid map value");
	PRT_MAPTYPE *mapType = (PRT_MAPTYPE *)map->type;
	PRT_SEQVALUE *seqVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
	seqVal->type = (PRT_TYPE)PrtMkSeqType(mapType->codType);

	if (map->size == 0)
	{
		seqVal->size = 0;
		seqVal->capacity = 0;
		seqVal->values = NULL;
	}
	else
	{
		seqVal->size = map->size;
		seqVal->capacity = map->size;
		seqVal->values = (PRT_VALUE *)PrtCalloc(map->size, sizeof(PRT_VALUE));
		PRT_MAPNODE* next = map->first;
		PRT_UINT32 i = 0;
		while (next != NULL)
		{
			seqVal->values[i] = PrtCloneValue(next->value);
			++i;
			next = next->insertNext;
		}
	}

	return seqVal;
}

PRT_BOOLEAN PrtMapExists(_In_ PRT_MAPVALUE *map, _In_ PRT_VALUE key)
{
	PRT_MAPTYPE *mapType;
	PRT_UINT32 bucketNum;
	PRT_MAPNODE *bucket;

	PrtAssert(*(map->type) == PRT_KIND_MAP, "Invalid map value");
	PrtAssert(*(*key) >= 0 && *(*key) < PRT_TYPE_KIND_COUNT, "Invalid key");
	mapType = (PRT_MAPTYPE *)map->type;
	PrtAssert(PrtIsSubtype(*key, mapType->domType), "Invalid map get; key has bad type");

	bucketNum = PrtGetHashCodeValue(key) % PrtHashtableCapacities[map->capNum];
	bucket = map->buckets[bucketNum];
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

PRT_UINT32 PrtMapSizeOf(_In_ PRT_MAPVALUE *map)
{
	PrtAssert(*(map->type) == PRT_KIND_MAP, "Invalid map value");
	return map->size;
}

PRT_UINT32 PrtMapCapacity(_In_ PRT_MAPVALUE *map)
{
	PrtAssert(*(map->type) == PRT_KIND_MAP, "Invalid map value");
	return PrtHashtableCapacities[map->capNum];
}

PRT_UINT32 PrtGetHashCodeValue(_In_ PRT_VALUE value)
{
	PRT_TYPE_KIND kind = **value;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return 0;
	case PRT_KIND_BOOL:
		return (PRT_UINT32)((PRT_PRIMVALUE *)value)->value.bl;
	case PRT_KIND_EVENT:
		return (PRT_UINT32)((PRT_PRIMVALUE *)value)->value.ev;
	case PRT_KIND_ID:
		return (PRT_UINT32)((PRT_PRIMVALUE *)value)->value.id;
	case PRT_KIND_INT:
		return (PRT_UINT32)((PRT_PRIMVALUE *)value)->value.nt;
	case PRT_KIND_MID:
		return (PRT_UINT32)((PRT_PRIMVALUE *)value)->value.md;
	case PRT_KIND_FORGN:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return 0;
	}
	case PRT_KIND_MAP:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return 0;
	}
	case PRT_KIND_NMDTUP:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return 0;
	}
	case PRT_KIND_SEQ:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return 0;
	}
	case PRT_KIND_TUPLE:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return 0;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return 0;
	}
}

PRT_BOOLEAN PrtIsEqualValue(_In_ PRT_VALUE value1, _In_ PRT_VALUE value2)
{
	PRT_TYPE_KIND kind1 = **value1;
	PRT_TYPE_KIND kind2 = **value2;
	PRT_DBG_ASSERT(kind1 != PRT_KIND_ANY, "Value must have a more concrete type");
	PRT_DBG_ASSERT(kind2 != PRT_KIND_ANY, "Value must have a more concrete type");

	if (kind1 != kind2)
	{
		return PRT_FALSE;
	}

	switch (kind1)
	{
	case PRT_KIND_BOOL:
		return
			((PRT_PRIMVALUE *)value1)->value.bl == ((PRT_PRIMVALUE *)value2)->value.bl ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_EVENT:
		return
			((PRT_PRIMVALUE *)value1)->value.ev == ((PRT_PRIMVALUE *)value2)->value.ev ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_ID:
		return
			((PRT_PRIMVALUE *)value1)->value.id == ((PRT_PRIMVALUE *)value2)->value.id ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_INT:
		return
			((PRT_PRIMVALUE *)value1)->value.nt == ((PRT_PRIMVALUE *)value2)->value.nt ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_MID:
		return
			((PRT_PRIMVALUE *)value1)->value.md == ((PRT_PRIMVALUE *)value2)->value.md ? PRT_TRUE : PRT_FALSE;
	case PRT_KIND_FORGN:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return PRT_FALSE;
	}
	case PRT_KIND_MAP:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return PRT_FALSE;
	}
	case PRT_KIND_NMDTUP:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return PRT_FALSE;
	}
	case PRT_KIND_SEQ:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return PRT_FALSE;
	}
	case PRT_KIND_TUPLE:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return PRT_FALSE;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return PRT_FALSE;
	}
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