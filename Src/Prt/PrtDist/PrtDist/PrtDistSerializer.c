#include "PrtHeaders.h"
#include "PrtDistSerializer.h"

PRT_TYPE
PrtDistDeserializeType(
__in PRT_TYPE type
)
{
	PRT_TYPE_KIND kind = type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_MODEL:
	case PRT_KIND_NULL:
		return PrtMkPrimitiveType(kind);
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		PRT_FORGNTYPE *ftype = type.typeUnion.forgn;
		return PrtMkForgnType(ftype->typeTag, ftype->cloner, ftype->freer, ftype->hasher, ftype->eqTester);
#endif
	}
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = type.typeUnion.map;
		return PrtMkMapType(PrtDistDeserializeType(mtype->domType), PrtDistDeserializeType(mtype->codType));
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i = 0;
		PRT_NMDTUPTYPE *ntype = type.typeUnion.nmTuple;
		PRT_TYPE clone = PrtMkNmdTupType(ntype->arity);

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
		clone.typeUnion.nmTuple->fieldNamesSerialized = NULL;
		clone.typeUnion.nmTuple->fieldTypesSerialized = NULL;

		return clone;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = type.typeUnion.seq;
		return PrtMkSeqType(PrtDistDeserializeType(stype->innerType));
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i = 0;
		PRT_TUPTYPE *ttype = type.typeUnion.tuple;
		PRT_TYPE clone = PrtMkTupType(ttype->arity);
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
		PRT_TYPE rettype;
		rettype.typeKind = PRT_TYPE_KIND_CANARY;
		return rettype;
	}

}

PRT_VALUE*
PrtDistDeserializeValue(
__in PRT_VALUE* value
)

{
	PRT_TYPE_KIND kind = value->type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return NULL;
	case PRT_KIND_BOOL:

	case PRT_KIND_EVENT:

	case PRT_KIND_MACHINE:

	case PRT_KIND_INT:

	case PRT_KIND_MODEL:
		return PrtCloneValue(value);
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
		PRT_TYPE tt = PrtDistDeserializeType(value->type);
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
		PRT_TYPE tt = PrtDistDeserializeType(value->type);
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(tt);
		retVal->discriminator = retVal->type.typeKind;

		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.nmTuple->arity;
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
		PRT_TYPE tt = PrtDistDeserializeType(value->type);
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(tt);
		retVal->discriminator = retVal->type.typeKind;

		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.tuple->arity;
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
		PRT_TYPE tt = PrtDistDeserializeType(value->type);
		PRT_VALUE *retVal = (PRT_VALUE *)PrtCalloc(1, sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(tt);
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

void InsertTypeNode(PRT_TYPE_NODE** head, PRT_TYPE value){
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

PRT_TYPE
PrtDistSerializeType(
__in PRT_TYPE type
)
{
	PRT_TYPE_KIND kind = type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_MODEL:
	case PRT_KIND_NULL:
		return PrtMkPrimitiveType(kind);
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		PRT_FORGNTYPE *ftype = type.typeUnion.forgn;
		return PrtMkForgnType(ftype->typeTag, ftype->cloner, ftype->freer, ftype->hasher, ftype->eqTester);
#endif
	}
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = type.typeUnion.map;
		return PrtMkMapType(PrtDistSerializeType(mtype->domType), PrtDistSerializeType(mtype->codType));
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = type.typeUnion.nmTuple;
		PRT_TYPE clone = PrtMkNmdTupType(ntype->arity);
		for (i = 0; i < ntype->arity; ++i)
		{
			InsertStringNode(&clone.typeUnion.nmTuple->fieldNamesSerialized, ntype->fieldNames[i]);
			InsertTypeNode(&clone.typeUnion.nmTuple->fieldTypesSerialized, PrtDistSerializeType(ntype->fieldTypes[i]));
		}


		return clone;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = type.typeUnion.seq;
		return PrtMkSeqType(PrtDistSerializeType(stype->innerType));
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = type.typeUnion.tuple;
		PRT_TYPE clone = PrtMkTupType(ttype->arity);
		for (i = 0; i < ttype->arity; ++i)
		{
			InsertTypeNode(&clone.typeUnion.tuple->fieldTypesSerialized, PrtDistSerializeType(ttype->fieldTypes[i]));
		}

		return clone;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		PRT_TYPE rettype;
		rettype.typeKind = PRT_TYPE_KIND_CANARY;
		return rettype;
	}
}

PRT_VALUE*
PrtDistSerializeValue(
__in PRT_VALUE* value
)
{
	PRT_TYPE_KIND kind = value->type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return NULL;
	case PRT_KIND_BOOL:

	case PRT_KIND_EVENT:

	case PRT_KIND_MACHINE:

	case PRT_KIND_INT:

	case PRT_KIND_MODEL:
		return PrtCloneValue(value);
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
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.nmTuple->arity;
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
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.tuple->arity;
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

