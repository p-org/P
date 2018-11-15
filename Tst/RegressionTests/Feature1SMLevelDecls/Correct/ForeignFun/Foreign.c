#pragma once
#ifndef P_MAIN_H_
#define P_MAIN_H_
#include "Prt.h"
#endif

PRT_VALUE* P_GlobalForeignFun_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs) 
{ 
    return NULL;
}

PRT_VALUE* P_ForeignFun_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs) 
{ 
    return NULL;
}

PRT_UINT64 P_MKDEF_T_IMPL() 
{
    return 0;
}

PRT_UINT64 P_CLONE_T_IMPL(PRT_UINT64 v)
{
    return v;
}

void P_FREE_T_IMPL(PRT_UINT64 v)
{

}

PRT_UINT32 P_GETHASHCODE_T_IMPL(PRT_UINT64 v)
{
    return (PRT_UINT32)v;
}

PRT_BOOLEAN P_ISEQUAL_T_IMPL(PRT_UINT64 v1, PRT_UINT64 v2)
{
    return v1 == v2;
}

PRT_STRING P_TOSTRING_T_IMPL(PRT_UINT64 v)
{
    return "";
}
