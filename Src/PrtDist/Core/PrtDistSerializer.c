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
		PRT_TYPE* clone = PrtCloneType(type);
		PrtFreeType(clone->typeUnion.map->codType);
		PrtFreeType(clone->typeUnion.map->domType);
		clone->typeUnion.map->codType = PrtDistSerializeType(type->typeUnion.map->codType);
		clone->typeUnion.map->domType = PrtDistSerializeType(type->typeUnion.map->domType);
		return clone;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_TYPE* clone = PrtCloneType(type);
		clone->typeUnion.nmTuple->fieldTypesSerialized = NULL;
		clone->typeUnion.nmTuple->fieldNamesSerialized = NULL;
		for (i = 0; i < clone->typeUnion.nmTuple->arity; ++i)
		{
			InsertStringNode(&clone->typeUnion.nmTuple->fieldNamesSerialized, type->typeUnion.nmTuple->fieldNames[i]);
			InsertTypeNode(&clone->typeUnion.nmTuple->fieldTypesSerialized, PrtDistSerializeType(type->typeUnion.nmTuple->fieldTypes[i]));
		}


		return clone;
	}
	case PRT_KIND_SEQ:
	{
		PRT_TYPE* clone = PrtCloneType(type);
		clone->typeUnion.seq->innerType = PrtDistSerializeType(type->typeUnion.seq->innerType);
		return clone;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TYPE* clone = PrtCloneType(type);
		clone->typeUnion.tuple->fieldTypesSerialized = NULL;
		for (i = 0; i < type->typeUnion.tuple->arity; ++i)
		{
			InsertTypeNode(&clone->typeUnion.tuple->fieldTypesSerialized, PrtDistSerializeType(type->typeUnion.tuple->fieldTypes[i]));
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
	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
		case PRT_VALUE_KIND_NULL:
		case PRT_VALUE_KIND_BOOL:
		case PRT_VALUE_KIND_EVENT:
		case PRT_VALUE_KIND_MID:
		case PRT_VALUE_KIND_INT:
			return PrtCloneValue(value);
		case PRT_VALUE_KIND_FORGN:
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


///
/// Deserialization function for Types and Values
///
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
			PRT_TYPE* temp = PrtCloneType(PrtDistDeserializeType(currT->type));
			PrtSetFieldType(clone, i, temp);
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
	PRT_VALUE_KIND kind = value->discriminator;
	switch (kind)
	{
		case PRT_VALUE_KIND_NULL:
		case PRT_VALUE_KIND_BOOL:
		case PRT_VALUE_KIND_EVENT:
		case PRT_VALUE_KIND_MID:
		case PRT_VALUE_KIND_INT:
			return PrtCloneValue(value);
		case PRT_VALUE_KIND_FORGN:
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