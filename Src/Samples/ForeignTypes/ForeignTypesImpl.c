#include "ForeignStringType.h"

void P_FREE_StringForeignType_IMPL(PRT_UINT64 frgnVal)
{
	PrtFree((PRT_STRING)frgnVal);
}

PRT_BOOLEAN P_ISEQUAL_StringForeignType_IMPL(PRT_UINT64 frgnVal1, PRT_UINT64 frgnVal2)
{
	return strcmp((PRT_STRING)frgnVal1, (PRT_STRING)frgnVal2) == 0 ? PRT_TRUE : PRT_FALSE;
}

PRT_STRING P_TOSTRING_StringForeignType_IMPL(PRT_UINT64 frgnVal)
{
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	sprintf_s(str, 100, "String : %lld", frgnVal);
	return str;
}

PRT_UINT32 P_GETHASHCODE_StringForeignType_IMPL(PRT_UINT64 frgnVal)
{
	return (PRT_UINT32)frgnVal;
}

PRT_UINT64 P_MKDEF_StringForeignType_IMPL(void)
{
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	sprintf_s(str, 100, "xyx$12");
	return (PRT_UINT64)str;
}

PRT_UINT64 P_CLONE_StringForeignType_IMPL(PRT_UINT64 frgnVal)
{
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	sprintf_s(str, 100, (PRT_STRING)frgnVal);
	return (PRT_UINT64)str;
}

PRT_VALUE* P_GetPassword_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
	return PrtMkDefaultValue(P_TYPEDEF_StringForeignType);
}