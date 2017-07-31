/**
* This file has the functions that implement the serialization and deserialization of PrtValues
*  for RPC.
*/

#include "PrtDistInternals.h"

///
/// Helper functions and Data
///
/** Array of prime hash table capacities. */

const PRT_UINT32 PrtHashtableCapacitiesDist[] =
{
	3, 13, 31, 61, 127,
	251, 509, 1021, 2039, 4093,
	8191, 16381, 32749, 65521, 131071,
	262139, 524287, 1048573, 2097143, 4194301,
	8388593, 16777213, 33554393, 67108859, 134217689,
	268435399, 536870909, 1073741789, 2147483647, 4294967291
};



void InsertValueNode(PRT_VALUE_NODE** head, PRT_VALUE *value) {
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

PRT_VALUE*
PrtDistSerializeValue(
	__in PRT_VALUE* value
)
{
	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALUE_KIND_NULL:
	case PRT_VALUE_KIND_BOOL:
	case PRT_VALUE_KIND_EVENT:
	case PRT_VALUE_KIND_MID:
	case PRT_VALUE_KIND_INT:
	case PRT_VALUE_KIND_FLOAT:
		return PrtCloneValue(value);
	case PRT_VALUE_KIND_FOREIGN:
	{
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
	case PRT_VALUE_KIND_MAP:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_MAPVALUE *map = (PRT_MAPVALUE *)PrtMalloc(sizeof(PRT_MAPVALUE));
		retVal->discriminator = PRT_VALUE_KIND_MAP;
		retVal->valueUnion.map = map;
		PRT_MAPVALUE *mVal = value->valueUnion.map;
		map->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacitiesDist[mVal->capNum], sizeof(PRT_MAPNODE *));
		map->capNum = mVal->capNum;
		map->size = 0;
		map->first = NULL;
		map->last = NULL;
		PRT_MAPNODE *next = mVal->first;
		while (next != NULL)
		{
			PrtMapUpdateEx(retVal, PrtDistSerializeValue(next->key), PRT_FALSE, PrtDistSerializeValue(next->value), PRT_FALSE);
			next = next->insertNext;
		}

		return retVal;
	}
	case PRT_VALUE_KIND_TUPLE:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->discriminator = PRT_VALUE_KIND_TUPLE;

		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->valueUnion.tuple->size;

		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));
		cVal->size = arity;
		cVal->valuesSerialized = NULL;
		for (i = 0; i < arity; ++i)
		{
			cVal->values[i] = PrtDistSerializeValue(tVal->values[i]);
			InsertValueNode(&cVal->valuesSerialized, PrtDistSerializeValue(tVal->values[i]));
		}

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}

	case PRT_VALUE_KIND_SEQ:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		PRT_SEQVALUE *cVal = (PRT_SEQVALUE *)PrtCalloc(1, sizeof(PRT_SEQVALUE));
		retVal->discriminator = PRT_VALUE_KIND_SEQ;
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
			cVal->valuesSerialized = NULL;
			cVal->values = (PRT_VALUE **)PrtCalloc(sVal->capacity, sizeof(PRT_VALUE*));
			for (i = 0; i < sVal->size; ++i)
			{
				cVal->values[i] = PrtDistSerializeValue(sVal->values[i]);
				InsertValueNode(&cVal->valuesSerialized, PrtDistSerializeValue(sVal->values[i]));
			}
		}

		return retVal;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid Operation");
		return NULL;
	}
}

PRT_VALUE*
PrtDistDeserializeValue(
	__in PRT_VALUE* value
)
{
	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
	case PRT_VALUE_KIND_NULL:
	case PRT_VALUE_KIND_BOOL:
	case PRT_VALUE_KIND_EVENT:
	case PRT_VALUE_KIND_MID:
	case PRT_VALUE_KIND_INT:
	case PRT_VALUE_KIND_FLOAT:
		return PrtCloneValue(value);
	case PRT_VALUE_KIND_FOREIGN:
	{
		PrtAssert(PRT_FALSE, "Foreign Type not Expected");
		return NULL;
	}
	case PRT_VALUE_KIND_MAP:
	{
		PRT_VALUE *retVal = (PRT_VALUE*)PrtMalloc(sizeof(PRT_VALUE));
		PRT_MAPVALUE *map = (PRT_MAPVALUE *)PrtMalloc(sizeof(PRT_MAPVALUE));
		retVal->discriminator = PRT_VALUE_KIND_MAP;
		retVal->valueUnion.map = map;
		PRT_MAPVALUE *mVal = value->valueUnion.map;
		map->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacitiesDist[mVal->capNum], sizeof(PRT_MAPNODE *));
		map->capNum = mVal->capNum;
		map->size = 0;
		map->first = NULL;
		map->last = NULL;
		PRT_MAPNODE *next = mVal->first;
		while (next != NULL)
		{
			PrtMapUpdateEx(retVal, PrtDistDeserializeValue(next->key), PRT_FALSE, PrtDistDeserializeValue(next->value), PRT_FALSE);
			next = next->insertNext;
		}

		return retVal;
	}
	case PRT_VALUE_KIND_TUPLE:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->discriminator = PRT_VALUE_KIND_TUPLE;

		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->valueUnion.tuple->size;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtCalloc(1, sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));
		cVal->size = arity;

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

	case PRT_VALUE_KIND_SEQ:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->discriminator = PRT_VALUE_KIND_SEQ;
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