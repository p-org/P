#include "program.h"

PRT_UINT64 UserMkDefaultForeignValue(_In_ PRT_UINT16 typeTag)
{
	PRT_STRING str = NULL;
	switch (typeTag)
	{
	case P_FORGN_TYPE_StringType:
		str = PrtMalloc(sizeof(PRT_CHAR) * 100);
		sprintf_s(str, 100, "xyx$12");
		return (PRT_UINT64)str;
	default:
		return (PRT_UINT64)NULL;
	};
}

PRT_UINT64 UserCloneForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal)
{
	PRT_STRING str = NULL;
	switch (typeTag)
	{
	case P_FORGN_TYPE_StringType:
		str = PrtMalloc(sizeof(PRT_CHAR) * 100);
		sprintf_s(str, 100, (PRT_STRING)frgnVal);
		return (PRT_UINT64)str;
	default:
		return (PRT_UINT64)NULL;
	};
}

void UserFreeForeignValue(_In_ PRT_UINT16 typeTag, _Inout_ PRT_UINT64 frgnVal)
{
	switch (typeTag)
	{
	case P_FORGN_TYPE_StringType:
		PrtFree((PRT_STRING)frgnVal);;
	default:
		break;
	};
}

PRT_UINT32 UserHashForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal)
{
	return (PRT_UINT32)frgnVal;
}

PRT_STRING UserToStringForeignValue(_In_ PRT_UINT16 typeTag, _In_ PRT_UINT64 frgnVal)
{
	PRT_STRING str = PrtMalloc(sizeof(PRT_CHAR) * 100);
	switch (typeTag)
	{
	case P_FORGN_TYPE_StringType:
		sprintf_s(str, 100, "String : %s", frgnVal);
		break;
	default:
		break;
	};
	return str;
}

PRT_BOOLEAN UserIsEqualForeignValue(_In_ PRT_UINT16 typeTag1, _In_ PRT_UINT64 frgnVal1, _In_ PRT_UINT16 typeTag2, _In_ PRT_UINT64 frgnVal2)
{
	if (typeTag1 != typeTag2)
		return PRT_FALSE;
	else
	{
		switch (typeTag1)
		{
		case P_FORGN_TYPE_StringType:
			return (strcmp((PRT_STRING)frgnVal1, (PRT_STRING)frgnVal2) == 0);
		default:
			return PRT_FALSE;
			break;
		};
	}
}

PRT_VALUE *P_FUN_TestMachine_GetPassword_IMPL(PRT_MACHINEINST *context)
{
	PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
	PRT_VALUE *p_tmp_ret = NULL;
	PRT_FUNSTACK_INFO p_tmp_frame;
	//remm to pop frame
	PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
	//remm to free the frame
	PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);

	return PrtMkDefaultValue(P_GEND_TYPE_StringType);
}