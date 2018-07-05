#include "ForeignTypes.h"

void PRT_FOREIGN_FREE_StringType_IMPL(PRT_UINT64 frgnVal)
{
	PrtFree((PRT_STRING)frgnVal);
}

PRT_BOOLEAN PRT_FOREIGN_ISEQUAL_StringType_IMPL(PRT_UINT64 frgnVal1, PRT_UINT64 frgnVal2)
{
	return strcmp((PRT_STRING)frgnVal1, (PRT_STRING)frgnVal2) == 0 ? PRT_TRUE : PRT_FALSE;
}

PRT_STRING PRT_FOREIGN_TOSTRING_StringType_IMPL(PRT_UINT64 frgnVal)
{
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	sprintf_s(str, 100, "String : %lld", frgnVal);
	return str;
}

PRT_UINT32 PRT_FOREIGN_GETHASHCODE_StringType_IMPL(PRT_UINT64 frgnVal)
{
	return (PRT_UINT32)frgnVal;
}

PRT_UINT64 PRT_FOREIGN_MKDEF_StringType_IMPL(void)
{
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	sprintf_s(str, 100, "xyx$12");
	return (PRT_UINT64)str;
}

PRT_UINT64 PRT_FOREIGN_CLONE_StringType_IMPL(PRT_UINT64 frgnVal)
{
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	sprintf_s(str, 100, (PRT_STRING)frgnVal);
	return (PRT_UINT64)str;
}

PRT_VALUE *P_FUN_GetPassword_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE*** argRefs)
{
	return PrtMkDefaultValue(P_FOREIGNTYPES_TYPEDEF_StringType);
}