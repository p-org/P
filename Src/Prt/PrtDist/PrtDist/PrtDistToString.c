#include "PrtDistToString.h"
#include <stdio.h>


PRT_STRING PrtTypeToString(_In_ PRT_TYPE type)
{
	PRT_TYPE_KIND kind = type.typeKind;
	PRT_STRING retString = (PRT_STRING)PrtCalloc(100, sizeof(char));
	switch (kind)
	{
	case PRT_KIND_ANY:
		sprintf_s(retString, 100, "any");
		break;
	case PRT_KIND_BOOL:
		sprintf_s(retString, 100, "bool");
		break;
	case PRT_KIND_EVENT:
		sprintf_s(retString, 100, "event");
		break;
	case PRT_KIND_MACHINE:
		sprintf_s(retString, 100,"machine");
		break;
	case PRT_KIND_INT:
		sprintf_s(retString, 100,"int");
		break;
	case PRT_KIND_MODEL:
		sprintf_s(retString, 100,"model");
		break;
	case PRT_KIND_FORGN:
		sprintf_s(retString, 100,"foreign");
		break;
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = (PRT_MAPTYPE *)type.typeUnion.map;
		sprintf_s(retString, 100,"map[");
		PrtTypeToString(mtype->domType);
		sprintf_s(retString, 100,", ");
		PrtTypeToString(mtype->codType);
		sprintf_s(retString, 100,"]");
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = (PRT_NMDTUPTYPE *)type.typeUnion.nmTuple;
		sprintf_s(retString, 100,"(");
		for (i = 0; i < ntype->arity; ++i)
		{
			sprintf_s(retString, 100,"%s: ", ntype->fieldNames[i]);
			PrtTypeToString(ntype->fieldTypes[i]);
			if (i < ntype->arity - 1)
			{
				sprintf_s(retString, 100,", ");
			}
			else
			{
				sprintf_s(retString, 100,")");
			}
		}

		break;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = (PRT_SEQTYPE *)type.typeUnion.seq;
		sprintf_s(retString, 100,"seq[");
		PrtTypeToString(stype->innerType);
		sprintf_s(retString, 100,"]");
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = (PRT_TUPTYPE *)type.typeUnion.tuple;
		sprintf_s(retString, 100,"(");
		if (ttype->arity == 1)
		{
			PrtTypeToString(ttype->fieldTypes[0]);
			sprintf_s(retString, 100,",)");
		}
		else
		{
			for (i = 0; i < ttype->arity; ++i)
			{
				PrtTypeToString(ttype->fieldTypes[i]);
				if (i < ttype->arity - 1)
				{
					sprintf_s(retString, 100,", ");
				}
				else
				{
					sprintf_s(retString, 100,")");
				}
			}
		}

		break;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		break;
	}

	return retString;
}

PRT_STRING PrtValueToString(_In_ PRT_VALUE *value)
{
	PRT_STRING retString = (PRT_STRING) PrtCalloc(100, sizeof(char));
	PRT_TYPE_KIND kind = value->type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		break;
	case PRT_KIND_BOOL:
		sprintf_s(retString, 100,PrtPrimGetBool(value) == PRT_TRUE ? "true" : "false");
		break;
	case PRT_KIND_EVENT:
	{
		PRT_UINT32 event = PrtPrimGetEvent(value);
		sprintf_s(retString, 100,"<%d>", event);
		break;
	}
	case PRT_KIND_NULL:
		sprintf_s(retString, 100,"null");
		break;
	case PRT_KIND_MACHINE:
	{
		PRT_UINT32 machine = PrtPrimGetMachine(value);
		sprintf_s(retString, 100,"<%d>", machine);

		break;
	}
	case PRT_KIND_INT:
		sprintf_s(retString, 100,"%d", PrtPrimGetInt(value));
		break;
	case PRT_KIND_MODEL:
	{
		PRT_UINT32 model = PrtPrimGetModel(value);
		sprintf_s(retString, 100,"<%d>", model);

		break;
	}
	case PRT_KIND_FORGN:
		sprintf_s(retString, 100,"foreign");
		break;
	case PRT_KIND_MAP:
	{
		PRT_MAPVALUE *mval = value->valueUnion.map;
		PRT_MAPNODE *next = mval->first;
		sprintf_s(retString, 100,"{");
		while (next != NULL)
		{
			PrtValueToString(next->key);
			sprintf_s(retString, 100," --> ");
			PrtValueToString(next->value);
			if (next->bucketNext != NULL)
			{
				sprintf_s(retString, 100,"*");
			}

			if (next->insertNext != NULL)
			{
				sprintf_s(retString, 100,", ");
			}

			next = next->insertNext;
		}

		sprintf_s(retString, 100,"} (%d / %d)", mval->size, PrtMapCapacity(value));
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tval = value->valueUnion.tuple;
		PRT_NMDTUPTYPE *ntype = value->type.typeUnion.nmTuple;
		sprintf_s(retString, 100,"(");
		for (i = 0; i < ntype->arity; ++i)
		{
			sprintf_s(retString, 100,"%s = ", ntype->fieldNames[i]);
			PrtValueToString(tval->values[i]);
			if (i < ntype->arity - 1)
			{
				sprintf_s(retString, 100,", ");
			}
			else
			{
				sprintf_s(retString, 100,")");
			}
		}

		break;
	}
	case PRT_KIND_SEQ:
	{
		PRT_UINT32 i;
		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		sprintf_s(retString, 100,"[");
		for (i = 0; i < sVal->size; ++i)
		{
			PrtValueToString(sVal->values[i]);
			if (i < sVal->size - 1)
			{
				sprintf_s(retString, 100,", ");
			}
		}

		sprintf_s(retString, 100,"]");
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPVALUE *tval = value->valueUnion.tuple;
		PRT_TUPTYPE *ttype = value->type.typeUnion.tuple;
		sprintf_s(retString, 100,"(");
		if (ttype->arity == 1)
		{
			PrtValueToString(tval->values[0]);
			sprintf_s(retString, 100,",)");
		}
		else
		{
			for (i = 0; i < ttype->arity; ++i)
			{
				PrtValueToString(tval->values[i]);
				if (i < ttype->arity - 1)
				{
					sprintf_s(retString, 100,", ");
				}
				else
				{
					sprintf_s(retString, 100,")");
				}
			}
		}

		break;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		break;
	}

	return retString;
}
