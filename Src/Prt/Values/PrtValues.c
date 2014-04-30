#include "PrtValues.h"

PRT_PRIMVALUE *PrtMkBoolValue(_In_ PRT_TYPE type, _In_ PRT_BOOLEAN value)
{
	PRT_PRIMVALUE *primVal;
	PrtAssert(*type == PRT_KIND_BOOL, "Did not receive bool type.");
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtCloneType(type);
	primVal->value.bl = value;
	return primVal;
}

PRT_PRIMVALUE *PrtMkEventValue(_In_ PRT_TYPE type, _In_ PRT_UINT32 value)
{
	PRT_PRIMVALUE *primVal;
	PrtAssert(*type == PRT_KIND_EVENT, "Did not receive event type.");
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtCloneType(type);
	primVal->value.ev = value;
	return primVal;
}

PRT_PRIMVALUE *PrtMkIntValue(_In_ PRT_TYPE type, _In_ PRT_INT32 value)
{
	PRT_PRIMVALUE *primVal;
	PrtAssert(*type == PRT_KIND_INT, "Did not receive int type.");
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtCloneType(type);
	primVal->value.nt = value;
	return primVal;
}

PRT_PRIMVALUE *PrtMkIdValue(_In_ PRT_TYPE type, _In_ PRT_UINT32 value)
{
	PRT_PRIMVALUE *primVal;
	PrtAssert(*type == PRT_KIND_ID, "Did not receive id type.");
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtCloneType(type);
	primVal->value.id = value;
	return primVal;
}

PRT_PRIMVALUE *PrtMkMIdValue(_In_ PRT_TYPE type, _In_ PRT_UINT32 value)
{
	PRT_PRIMVALUE *primVal;
	PrtAssert(*type == PRT_KIND_MID, "Did not receive mid type.");
	primVal = (PRT_PRIMVALUE *)PrtMalloc(sizeof(PRT_PRIMVALUE));
	primVal->type = PrtCloneType(type);
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
		kind = PRT_KIND_ID;
		return (PRT_VALUE)PrtMkIdValue(&kind, PRT_NULL_ID);
	case PRT_KIND_BOOL:
		return (PRT_VALUE)PrtMkBoolValue(&kind, PRT_FALSE);
	case PRT_KIND_EVENT:
		return (PRT_VALUE)PrtMkEventValue(&kind, PRT_NULL_ID);
	case PRT_KIND_ID:
		return (PRT_VALUE)PrtMkIdValue(&kind, PRT_NULL_ID);
	case PRT_KIND_INT:
		return (PRT_VALUE)PrtMkIntValue(&kind, 0);
	case PRT_KIND_MID:
		return (PRT_VALUE)PrtMkMIdValue(&kind, PRT_NULL_ID);
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

	tuple->values[index] = PrtCloneValue(value);
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
		return (PRT_VALUE)PrtMkBoolValue(pVal->type, pVal->value.bl);
	}
	case PRT_KIND_EVENT:
	{
		PRT_PRIMVALUE *pVal = (PRT_PRIMVALUE *)value;
		return (PRT_VALUE)PrtMkEventValue(pVal->type, pVal->value.ev);
	}
	case PRT_KIND_ID:
	{
		PRT_PRIMVALUE *pVal = (PRT_PRIMVALUE *)value;
		return (PRT_VALUE)PrtMkIdValue(pVal->type, pVal->value.id);
	}
	case PRT_KIND_INT:
	{
		PRT_PRIMVALUE *pVal = (PRT_PRIMVALUE *)value;
		return (PRT_VALUE)PrtMkIntValue(pVal->type, pVal->value.nt);
	}
	case PRT_KIND_MID:
	{
		PRT_PRIMVALUE *pVal = (PRT_PRIMVALUE *)value;
		return (PRT_VALUE)PrtMkMIdValue(pVal->type, pVal->value.md);
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
		PrtAssert(PRT_FALSE, "Not implemented");
		return NULL;
	}
	case PRT_KIND_SEQ:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return NULL;
	}
	case PRT_KIND_TUPLE:
	{
		PrtAssert(PRT_FALSE, "Not implemented");
		return NULL;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
}
