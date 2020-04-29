#include "PrtTypes.h"
#include "PrtProgram.h"

PRT_TYPE* PRT_CALL_CONV PrtMkPrimitiveType(_In_ PRT_TYPE_KIND primType)
{
	PRT_TYPE* type = (PRT_TYPE *)PrtMalloc(sizeof(PRT_TYPE));
	type->typeUnion.map = NULL;
	switch (primType)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_STRING:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_FLOAT:
	case PRT_KIND_NULL:
	{
		type->typeKind = primType;
		return type;
	}
	default:
	{
		PrtAssert(PRT_FALSE, "Expected a primitive type.");
		type->typeKind = PRT_TYPE_KIND_CANARY;
		return type;
	}
	}
}

PRT_TYPE* PRT_CALL_CONV PrtMkForeignType(_In_ PRT_FOREIGNTYPEDECL* foreignType)
{
	PRT_TYPE* type = (PRT_TYPE *)PrtMalloc(sizeof(PRT_TYPE));
	type->typeKind = PRT_KIND_FOREIGN;
	type->typeUnion.foreignType = foreignType;
	return type;
}

PRT_TYPE* PRT_CALL_CONV PrtMkMapType(_In_ PRT_TYPE* domType, _In_ PRT_TYPE* codType)
{
	PrtAssert(PrtIsValidType(domType), "Invalid type expression");
	PrtAssert(PrtIsValidType(codType), "Invalid type expression");

	PRT_TYPE* type = (PRT_TYPE *)PrtMalloc(sizeof(PRT_TYPE));
	PRT_MAPTYPE* map = (PRT_MAPTYPE *)PrtMalloc(sizeof(PRT_MAPTYPE));
	type->typeKind = PRT_KIND_MAP;
	type->typeUnion.map = map;

	map->domType = PrtCloneType(domType);
	map->codType = PrtCloneType(codType);
	return type;
}

PRT_TYPE* PRT_CALL_CONV PrtMkNmdTupType(_In_ PRT_UINT32 arity)
{
	PrtAssert(arity > 0, "Invalid tuple arity");
	PRT_TYPE* type = (PRT_TYPE *)PrtMalloc(sizeof(PRT_TYPE));
	PRT_NMDTUPTYPE* nmdTup = (PRT_NMDTUPTYPE *)PrtMalloc(sizeof(PRT_NMDTUPTYPE));
	type->typeKind = PRT_KIND_NMDTUP;
	type->typeUnion.nmTuple = nmdTup;

	nmdTup->arity = arity;
	nmdTup->fieldNames = (PRT_STRING *)PrtCalloc((size_t)arity, sizeof(PRT_STRING));
	nmdTup->fieldTypes = (PRT_TYPE **)PrtCalloc((size_t)arity, sizeof(PRT_TYPE *));
	return type;
}

PRT_TYPE* PRT_CALL_CONV PrtMkTupType(_In_ PRT_UINT32 arity)
{
	PrtAssert(arity > 0, "Invalid tuple arity");
	PRT_TYPE* type = (PRT_TYPE *)PrtMalloc(sizeof(PRT_TYPE));
	PRT_TUPTYPE* tup = (PRT_TUPTYPE *)PrtMalloc(sizeof(PRT_TUPTYPE));
	type->typeKind = PRT_KIND_TUPLE;
	type->typeUnion.tuple = tup;

	tup->arity = arity;
	tup->fieldTypes = (PRT_TYPE **)PrtCalloc((size_t)arity, sizeof(PRT_TYPE *));
	return type;
}

PRT_TYPE* PRT_CALL_CONV PrtMkSeqType(_In_ PRT_TYPE* innerType)
{
	PrtAssert(PrtIsValidType(innerType), "Invalid type expression");
	PRT_TYPE* type = (PRT_TYPE *)PrtMalloc(sizeof(PRT_TYPE));
	PRT_SEQTYPE* seq = (PRT_SEQTYPE *)PrtMalloc(sizeof(PRT_SEQTYPE));
	type->typeKind = PRT_KIND_SEQ;
	type->typeUnion.seq = seq;
	seq->innerType = PrtCloneType(innerType);
	return type;
}

PRT_TYPE* PRT_CALL_CONV PrtMkSetType(_In_ PRT_TYPE* innerType)
{
	PrtAssert(PrtIsValidType(innerType), "Invalid type expression");
	PRT_TYPE* type = (PRT_TYPE *)PrtMalloc(sizeof(PRT_TYPE));
	PRT_SETTYPE* set = (PRT_SETTYPE *)PrtMalloc(sizeof(PRT_SETTYPE));
	type->typeKind = PRT_KIND_SET;
	type->typeUnion.set = set;
	set->innerType = PrtCloneType(innerType);
	return type;
}

void PRT_CALL_CONV PrtSetFieldType(_Inout_ PRT_TYPE* tupleType, _In_ PRT_UINT32 index, _In_ PRT_TYPE* fieldType)
{
	PrtAssert(PrtIsValidType(tupleType), "Invalid type expression");
	PrtAssert(PrtIsValidType(fieldType), "Invalid type expression");
	PrtAssert(tupleType->typeKind == PRT_KIND_TUPLE || tupleType->typeKind == PRT_KIND_NMDTUP, "Invalid type expression"
	);

	if (tupleType->typeKind == PRT_KIND_TUPLE)
	{
		PrtAssert(index < tupleType->typeUnion.tuple->arity, "Invalid tuple index");
		tupleType->typeUnion.tuple->fieldTypes[index] = PrtCloneType(fieldType);
	}
	else if (tupleType->typeKind == PRT_KIND_NMDTUP)
	{
		PrtAssert(index < tupleType->typeUnion.nmTuple->arity, "Invalid tuple index");
		tupleType->typeUnion.nmTuple->fieldTypes[index] = PrtCloneType(fieldType);
	}
}

void PRT_CALL_CONV PrtSetFieldName(_Inout_ PRT_TYPE* tupleType, _In_ PRT_UINT32 index, _In_ PRT_STRING fieldName)
{
	PrtAssert(PrtIsValidType(tupleType), "Invalid type expression");
	PrtAssert(tupleType->typeKind == PRT_KIND_NMDTUP, "Invalid type expression");
	PrtAssert(fieldName != NULL && *fieldName != '\0', "Invalid field name");
	PrtAssert(index < tupleType->typeUnion.nmTuple->arity, "Invalid tuple index");

	size_t nameLen;
	PRT_STRING fieldNameClone;
	nameLen = strnlen(fieldName, PRT_MAXFLDNAME_LENGTH);
	PrtAssert(nameLen > 0 && nameLen < PRT_MAXFLDNAME_LENGTH, "Invalid field name");

	fieldNameClone = (PRT_STRING)PrtCalloc(nameLen + 1, sizeof(PRT_CHAR));
	strncpy(fieldNameClone, fieldName, nameLen);
	fieldNameClone[nameLen] = '\0';
	tupleType->typeUnion.nmTuple->fieldNames[index] = fieldNameClone;
}

PRT_BOOLEAN PRT_CALL_CONV PrtIsSubtype(_In_ PRT_TYPE* subType, _In_ PRT_TYPE* supType)
{
	PrtAssert(PrtIsValidType(subType), "Invalid type expression");
	PrtAssert(PrtIsValidType(supType), "Invalid type expression");

	PRT_TYPE_KIND subKind = subType->typeKind;
	PRT_TYPE_KIND supKind = supType->typeKind;
	switch (supKind)
	{
	case PRT_KIND_ANY:
	{
		//// Everything is a subtype of `any`.
		return PRT_TRUE;
	}
	case PRT_KIND_NULL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	{
		return (subKind == supKind || subKind == PRT_KIND_NULL) ? PRT_TRUE : PRT_FALSE;
	}
	case PRT_KIND_BOOL:
	case PRT_KIND_INT:
	case PRT_KIND_STRING:
	case PRT_KIND_FLOAT:
	case PRT_KIND_FOREIGN:
	{
		//// These types do not have any proper subtypes.
		return (subKind == supKind && subType->typeUnion.foreignType->declIndex == supType
			->typeUnion.foreignType->
			declIndex)
			? PRT_TRUE
			: PRT_FALSE;
	}
	case PRT_KIND_MAP:
	{
		//// Both types are maps and inner types are in subtype relationship.
		PRT_MAPTYPE* subMap;
		PRT_MAPTYPE* supMap;
		if (subKind != PRT_KIND_MAP)
		{
			return PRT_FALSE;
		}

		subMap = (PRT_MAPTYPE *)subType->typeUnion.map;
		supMap = (PRT_MAPTYPE *)supType->typeUnion.map;
		return
			PrtIsSubtype(subMap->domType, supMap->domType) &&
			PrtIsSubtype(subMap->codType, supMap->codType)
			? PRT_TRUE
			: PRT_FALSE;
	}
	case PRT_KIND_NMDTUP:
	{
		//// Both types are named tuples with same field names, arity, and inner types are in subtype relationship.
		PRT_UINT32 i;
		PRT_NMDTUPTYPE* subNmdTup;
		PRT_NMDTUPTYPE* supNmdTup;
		if (subKind != PRT_KIND_NMDTUP)
		{
			return PRT_FALSE;
		}

		subNmdTup = (PRT_NMDTUPTYPE *)subType->typeUnion.nmTuple;
		supNmdTup = (PRT_NMDTUPTYPE *)supType->typeUnion.nmTuple;
		if (subNmdTup->arity != supNmdTup->arity)
		{
			return PRT_FALSE;
		}

		//// Next check field names.
		for (i = 0; i < subNmdTup->arity; ++i)
		{
			if (strncmp(subNmdTup->fieldNames[i], supNmdTup->fieldNames[i], PRT_MAXFLDNAME_LENGTH) != 0)
			{
				return PRT_FALSE;
			}
		}

		//// Finally check field types.
		for (i = 0; i < subNmdTup->arity; ++i)
		{
			if (!PrtIsSubtype(subNmdTup->fieldTypes[i], supNmdTup->fieldTypes[i]))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	case PRT_KIND_SEQ:
	{
		//// Both types are sequences and inner types are in subtype relationship.
		PRT_SEQTYPE* subSeq;
		PRT_SEQTYPE* supSeq;
		if (subKind != PRT_KIND_SEQ)
		{
			return PRT_FALSE;
		}
		subSeq = (PRT_SEQTYPE *)subType->typeUnion.seq;
		supSeq = (PRT_SEQTYPE *)supType->typeUnion.seq;
		return PrtIsSubtype(subSeq->innerType, supSeq->innerType);
	}
	case PRT_KIND_SET:
	{
		//// Both types are setss and inner types are in subtype relationship.
		PRT_SETTYPE* subSet;
		PRT_SETTYPE* supSet;
		if (subKind != PRT_KIND_SET)
		{
			return PRT_FALSE;
		}

		subSet = (PRT_SETTYPE *)subType->typeUnion.set;
		supSet = (PRT_SETTYPE *)supType->typeUnion.set;
		return PrtIsSubtype(subSet->innerType, supSet->innerType);
	}
	case PRT_KIND_TUPLE:
	{
		//// Both types are tuples with same arity, and inner types are in subtype relationship.
		PRT_UINT32 i;
		PRT_TUPTYPE* subTup;
		PRT_TUPTYPE* supTup;
		if (subKind != PRT_KIND_TUPLE)
		{
			return PRT_FALSE;
		}

		subTup = (PRT_TUPTYPE *)subType->typeUnion.tuple;
		supTup = (PRT_TUPTYPE *)supType->typeUnion.tuple;
		if (subTup->arity != supTup->arity)
		{
			return PRT_FALSE;
		}

		//// Finally check field types.
		for (i = 0; i < subTup->arity; ++i)
		{
			if (!PrtIsSubtype(subTup->fieldTypes[i], supTup->fieldTypes[i]))
			{
				return PRT_FALSE;
			}
		}

		return PRT_TRUE;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtIsSubtype: Invalid type");
		return PRT_FALSE;
	}
}

PRT_TYPE* PRT_CALL_CONV PrtCloneType(_In_ PRT_TYPE* type)
{
	PrtAssert(PrtIsValidType(type), "Invalid type expression");
	PRT_TYPE_KIND kind = type->typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_FLOAT:
	case PRT_KIND_STRING:
	case PRT_KIND_NULL:
	{
		return PrtMkPrimitiveType(kind);
	}
	case PRT_KIND_FOREIGN:
	{
		return PrtMkForeignType(type->typeUnion.foreignType);
	}
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE* mtype = type->typeUnion.map;
		return PrtMkMapType(mtype->domType, mtype->codType);
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE* ntype = type->typeUnion.nmTuple;
		PRT_TYPE* clone = PrtMkNmdTupType(ntype->arity);
		for (i = 0; i < ntype->arity; ++i)
		{
			PrtSetFieldName(clone, i, ntype->fieldNames[i]);
			PrtSetFieldType(clone, i, ntype->fieldTypes[i]);
		}

		return clone;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE* stype = type->typeUnion.seq;
		return PrtMkSeqType(stype->innerType);
	}
	case PRT_KIND_SET:
	{
		PRT_SETTYPE* stype = type->typeUnion.set;
		return PrtMkSetType(stype->innerType);
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE* ttype = type->typeUnion.tuple;
		PRT_TYPE* clone = PrtMkTupType(ttype->arity);
		for (i = 0; i < ttype->arity; ++i)
		{
			PrtSetFieldType(clone, i, ttype->fieldTypes[i]);
		}

		return clone;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtCloneType: Invalid type");
		return PrtMkPrimitiveType(PRT_KIND_NULL);
	}
}

void PRT_CALL_CONV PrtFreeType(_Inout_ PRT_TYPE* type)
{
	PRT_TYPE_KIND kind = type->typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_FLOAT:
	case PRT_KIND_STRING:
	case PRT_KIND_FOREIGN:
	case PRT_KIND_NULL:
		type->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(type);
		break;
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE* mtype = (PRT_MAPTYPE *)type->typeUnion.map;
		PrtFreeType(mtype->domType);
		PrtFreeType(mtype->codType);
		type->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(mtype);
		PrtFree(type);
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE* ntype = type->typeUnion.nmTuple;
		for (i = 0; i < ntype->arity; ++i)
		{
			PrtFree(ntype->fieldNames[i]);
			PrtFreeType(ntype->fieldTypes[i]);
		}

		PrtFree(ntype->fieldNames);
		PrtFree(ntype->fieldTypes);
		type->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(ntype);
		PrtFree(type);
		break;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE* stype = type->typeUnion.seq;
		PrtFreeType(stype->innerType);
		type->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(stype);
		PrtFree(type);
		break;
	}
	case PRT_KIND_SET:
	{
		PRT_SETTYPE* stype = type->typeUnion.set;
		PrtFreeType(stype->innerType);
		type->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(stype);
		PrtFree(type);
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE* ttype = type->typeUnion.tuple;
		for (i = 0; i < ttype->arity; ++i)
		{
			PrtFreeType(ttype->fieldTypes[i]);
		}

		PrtFree(ttype->fieldTypes);
		type->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(ttype);
		PrtFree(type);
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "PrtFreeType: Invalid type");
		break;
	}
}

PRT_BOOLEAN PRT_CALL_CONV PrtIsValidType(_In_ PRT_TYPE* type)
{
	if (type == NULL)
	{
		return PRT_FALSE;
	}

	PRT_TYPE_KIND kind = type->typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_STRING:
	case PRT_KIND_FLOAT:
	case PRT_KIND_NULL:
		return PRT_TRUE;
	case PRT_KIND_FOREIGN:
	{
		return type->typeUnion.foreignType->declIndex < program->nForeignTypes;
	}
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE* map = type->typeUnion.map;
		return map != NULL &&
			map->codType != NULL &&
			map->domType != NULL;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE* seq = type->typeUnion.seq;
		return seq != NULL && seq->innerType != NULL;
	}
	case PRT_KIND_SET:
	{
		PRT_SETTYPE* set = type->typeUnion.set;
		return set != NULL && set->innerType != NULL;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_TUPTYPE* tup = type->typeUnion.tuple;
		return tup != NULL && tup->arity > 0 && tup->fieldTypes != NULL;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_NMDTUPTYPE* tup = type->typeUnion.nmTuple;
		return tup != NULL && tup->arity > 0 && tup->fieldTypes != NULL && tup->fieldNames != NULL;
	}
	default:
		return PRT_FALSE;
	}
}