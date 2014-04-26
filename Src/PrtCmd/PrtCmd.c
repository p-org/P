#include "PrtCmd.h"

int main(int argc, char *argv[])
{
	PRT_TYPEEXPR intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPEEXPR boolType = PrtMkPrimitiveType(PRT_KIND_BOOL);
	PRT_SEQTYPE *seqBoolType = PrtMkSeqType(boolType);
	PRT_MAPTYPE *int2seqType = PrtMkMapType(intType, (PRT_TYPEEXPR)seqBoolType);
	PRT_TYPEEXPR clone = PrtCloneType((PRT_TYPEEXPR)int2seqType);
	PrtCmdPrintType((PRT_TYPEEXPR)int2seqType);
	PrtFree((PRT_TYPEEXPR)int2seqType);
	printf_s("\n");
	PrtCmdPrintType(clone);
	PrtFree(clone);
	return 0;
}

void PrtCmdPrintType(_In_ PRT_TYPEEXPR type)
{
	PRT_TYPE_KIND kind = *type;
	switch (kind)
	{
	case PRT_KIND_ANY:
		printf_s("any");
		break;
	case PRT_KIND_BOOL:
		printf_s("bool");
		break;
	case PRT_KIND_EVENT:
		printf_s("event");
		break;
	case PRT_KIND_ID:
		printf_s("id");
		break;
	case PRT_KIND_INT:
		printf_s("int");
		break;
	case PRT_KIND_MID:
		printf_s("mid");
		break;
	case PRT_KIND_FORGN:
		printf_s("foreign");
		break;
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = (PRT_MAPTYPE *)type;
		printf_s("map[");
		PrtCmdPrintType(mtype->domType);
		printf_s(", ");
		PrtCmdPrintType(mtype->codType);
		printf_s("]");
		break;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = (PRT_NMDTUPTYPE *)type;
		printf_s("(");
		for (i = 0; i < ntype->arity; ++i)
		{
			printf_s("%s: ", ntype->fieldNames[i]);
			PrtCmdPrintType(ntype->fieldTypes[i]);
			if (i < ntype->arity - 1)
			{
				printf_s(", ");
			}
			else 
			{
				printf_s(")");
			}
		}

		break;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = (PRT_SEQTYPE *)type;
		printf_s("seq[");
		PrtCmdPrintType(stype->innerType);
		printf_s("]");
		break;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = (PRT_TUPTYPE *)type;
		printf_s("(");
		if (ttype->arity == 1)
		{
			PrtCmdPrintType(ttype->fieldTypes[0]);
			printf_s(",)");
		}
		else 
		{
			for (i = 0; i < ttype->arity; ++i)
			{
				PrtCmdPrintType(ttype->fieldTypes[i]);
				if (i < ttype->arity - 1)
				{
					printf_s(", ");
				}
				else
				{
					printf_s(")");
				}
			}
		}

		break;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		break;
	}
}
