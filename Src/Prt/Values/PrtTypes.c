#include "PrtTypes.h"

PRT_TYPEEXPR PrtMkPrimitiveType(_In_ PRT_TYPE_KIND primType)
{
	switch (primType)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_ID:
	case PRT_KIND_INT:
	case PRT_KIND_MID:
	{
		PRT_TYPEEXPR type = (PRT_TYPEEXPR)PrtMalloc(sizeof(PRT_TYPE_KIND));
		*type = primType;
		return type;
	}
	default:
		PrtAssert(PRT_FALSE, "Expected a primitive type.");
		return NULL;
	}
}

PRT_FORGNTYPE *PrtMkForgnType(
	_In_ PRT_GUID              typeTag,
	_In_ PRT_FORGN_CLONE       cloner,
	_In_ PRT_FORGN_FREE        freer,
	_In_ PRT_FORGN_GETHASHCODE hasher,
	_In_ PRT_FORGN_ISEQUAL     eqTester)
{
	PRT_FORGNTYPE *type;
	PrtAssert(typeTag.data1 != 0 || typeTag.data2 != 0 || typeTag.data3 != 0 || typeTag.data4 != 0, "Bad type tag");
	PrtAssert(cloner != NULL, "Bad cloner");
	PrtAssert(freer != NULL, "Bad freer");
	PrtAssert(hasher != NULL, "Bad hasher");
	PrtAssert(eqTester != NULL, "Bad equality tester");

	type = (PRT_FORGNTYPE *)PrtMalloc(sizeof(PRT_FORGNTYPE));
	type->typeKind = PRT_KIND_FORGN;
	type->typeTag = typeTag;
	type->cloner = cloner;
	type->freer = freer;
	type->hasher = hasher;
	type->eqTester = eqTester;

	return type;
}

PRT_MAPTYPE *PrtMkMapType(_In_ PRT_TYPEEXPR domType, _In_ PRT_TYPEEXPR codType)
{
	PRT_MAPTYPE *type;
	PrtAssert(*domType >= 0 && *domType < PRT_TYPE_KIND_COUNT, "Invalid type expression");
	PrtAssert(*codType >= 0 && *codType < PRT_TYPE_KIND_COUNT, "Invalid type expression");
	type = (PRT_MAPTYPE *)PrtMalloc(sizeof(PRT_MAPTYPE));
	type->typeKind = PRT_KIND_MAP;
	type->domType = PrtCloneType(domType);
	type->codType = PrtCloneType(codType);
	return type;
}

PRT_NMDTUPTYPE *PrtMkNmdTupType(_In_ PRT_UINT32 arity)
{
	PRT_NMDTUPTYPE *type;
	PrtAssert(arity > 0, "Invalid tuple arity");
	type = (PRT_NMDTUPTYPE *)PrtMalloc(sizeof(PRT_NMDTUPTYPE));
	type->typeKind = PRT_KIND_NMDTUP;
	type->arity = arity;
	type->fieldNames = (PRT_STRING *)PrtCalloc((size_t)arity, sizeof(PRT_STRING));
	type->fieldTypes = (PRT_TYPEEXPR *)PrtCalloc((size_t)arity, sizeof(PRT_TYPEEXPR));
	return type;
}

PRT_TUPTYPE *PrtMkTupType(_In_ PRT_UINT32 arity)
{
	PRT_TUPTYPE *type;
	PrtAssert(arity > 0, "Invalid tuple arity");
	type = (PRT_TUPTYPE *)PrtMalloc(sizeof(PRT_TUPTYPE));
	type->typeKind = PRT_KIND_TUPLE;
	type->arity = arity;
	type->fieldTypes = (PRT_TYPEEXPR *)PrtCalloc((size_t)arity, sizeof(PRT_TYPEEXPR));
	return type;
}

PRT_SEQTYPE *PrtMkSeqType(_In_ PRT_TYPEEXPR innerType)
{
	PRT_SEQTYPE *type;
	PrtAssert(*innerType >= 0 && *innerType < PRT_TYPE_KIND_COUNT, "Invalid type expression");
	type = (PRT_SEQTYPE *)PrtMalloc(sizeof(PRT_SEQTYPE));
	type->typeKind = PRT_KIND_SEQ;
	type->innerType = PrtCloneType(innerType);
	return type;
}

void PrtSetFieldType(_Inout_ PRT_TYPEEXPR tupleType, _In_ PRT_UINT32 index, _In_ PRT_TYPEEXPR fieldType)
{
	PrtAssert(*tupleType == PRT_KIND_TUPLE || *tupleType == PRT_KIND_NMDTUP, "Invalid type expression");
	PrtAssert(*fieldType >= 0 && *fieldType < PRT_TYPE_KIND_COUNT, "Invalid type expression");

	if (*tupleType == PRT_KIND_TUPLE)
	{
		PRT_TUPTYPE *type = (PRT_TUPTYPE *)tupleType;
		PrtAssert(index < type->arity, "Invalid tuple index");
		type->fieldTypes[index] = PrtCloneType(fieldType);
	}
	else if (*tupleType == PRT_KIND_NMDTUP)
	{
		PRT_NMDTUPTYPE *type = (PRT_NMDTUPTYPE *)tupleType;
		PrtAssert(index < type->arity, "Invalid tuple index");
		type->fieldTypes[index] = PrtCloneType(fieldType);
	}
}

void PrtSetFieldName(_Inout_ PRT_NMDTUPTYPE *tupleType, _In_ PRT_UINT32 index, _In_ PRT_STRING fieldName)
{
	size_t nameLen;
	PRT_STRING fieldNameClone;
	PrtAssert(tupleType->typeKind == PRT_KIND_NMDTUP, "Invalid type expression");
	PrtAssert(fieldName != NULL && *fieldName != '\0', "Invalid field name");
	PrtAssert(index < tupleType->arity, "Invalid tuple index");
	nameLen = strnlen(fieldName, PRT_MAXFLDNAME_LENGTH);
	PrtAssert(nameLen > 0 && nameLen < PRT_MAXFLDNAME_LENGTH, "Invalid field name");
	fieldNameClone = (PRT_STRING)PrtCalloc(nameLen + 1, sizeof(PRT_CHAR));
	strncpy(fieldNameClone, fieldName, nameLen);
	fieldNameClone[nameLen] = '\0';
	tupleType->fieldNames[index] = fieldNameClone;
}

PRT_TYPEEXPR PrtCloneType(_In_ PRT_TYPEEXPR type)
{
	PRT_TYPE_KIND kind = *type;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_ID:
	case PRT_KIND_INT:
	case PRT_KIND_MID:
		return PrtMkPrimitiveType(kind);
	case PRT_KIND_FORGN:
	{
		PRT_FORGNTYPE *ftype = (PRT_FORGNTYPE *)type;
		return (PRT_TYPEEXPR)PrtMkForgnType(ftype->typeTag, ftype->cloner, ftype->freer, ftype->hasher, ftype->eqTester);
	}
	case PRT_KIND_MAP:
	{		
		PRT_MAPTYPE *mtype = (PRT_MAPTYPE *)type;
		return (PRT_TYPEEXPR)PrtMkMapType(mtype->domType, mtype->codType);
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = (PRT_NMDTUPTYPE *)type;
		PRT_NMDTUPTYPE *clone = PrtMkNmdTupType(ntype->arity);
		for (i = 0; i < ntype->arity; ++i)
		{
			PrtSetFieldName(clone, i, ntype->fieldNames[i]);
			PrtSetFieldType((PRT_TYPEEXPR)clone, i, ntype->fieldTypes[i]);
		}

		return (PRT_TYPEEXPR)clone;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = (PRT_SEQTYPE *)type;
		return (PRT_TYPEEXPR)PrtMkSeqType(stype->innerType);
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = (PRT_TUPTYPE *)type;
		PRT_TUPTYPE *clone = PrtMkTupType(ttype->arity);
		for (i = 0; i < ttype->arity; ++i)
		{
			PrtSetFieldType((PRT_TYPEEXPR)clone, i, ttype->fieldTypes[i]);
		}

		return (PRT_TYPEEXPR)clone;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
}

void PrtFreeType(_Inout_ PRT_TYPEEXPR type)
{
	PRT_TYPE_KIND kind = *type;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_ID:
	case PRT_KIND_INT:
	case PRT_KIND_MID:
		*type = PRT_TYPE_KIND_CANARY;
		PrtFree(type);
		break;
	case PRT_KIND_FORGN:
	{
		PRT_FORGNTYPE *ftype = (PRT_FORGNTYPE *)type;
		ftype->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(ftype);
		break;
	}
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = (PRT_MAPTYPE *)type;
		PrtFreeType(mtype->domType);
		PrtFreeType(mtype->codType);
		mtype->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(mtype);
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = (PRT_NMDTUPTYPE *)type;
		for (i = 0; i < ntype->arity; ++i)
		{
			PrtFree(ntype->fieldNames[i]);
			PrtFreeType(ntype->fieldTypes[i]);
		}

		PrtFree(ntype->fieldNames);
		PrtFree(ntype->fieldTypes);
		ntype->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(ntype);
		break;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = (PRT_SEQTYPE *)type;
		PrtFreeType(stype->innerType);
		stype->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(stype);
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = (PRT_TUPTYPE *)type;
		for (i = 0; i < ttype->arity; ++i)
		{
			PrtFreeType(ttype->fieldTypes[i]);
		}

		PrtFree(ttype->fieldTypes);
		ttype->typeKind = PRT_TYPE_KIND_CANARY;
		PrtFree(ttype);
		break;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		break;
	}
}
