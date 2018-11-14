#pragma once
#ifndef P_MAIN_H_
#define P_MAIN_H_
#include "Prt.h"
#endif

PRT_VALUE* P_FUN_GlobalForeignFun_FOREIGN(PRT_MACHINEINST* context, PRT_VALUE*** argRefs) 
{ 
    return NULL;
}

PRT_VALUE* P_FUN_ForeignFun_FOREIGN(PRT_MACHINEINST* context, PRT_VALUE*** argRefs) 
{ 
    return NULL;
}

PRT_UINT64 PRT_FOREIGN_MKDEF_T_IMPL() 
{
    return 0;
}

PRT_UINT64 PRT_FOREIGN_CLONE_T_IMPL(PRT_UINT64 v)
{
    return v;
}

void PRT_FOREIGN_FREE_T_IMPL(PRT_UINT64 v)
{

}

PRT_UINT32 PRT_FOREIGN_GETHASHCODE_T_IMPL(PRT_UINT64 v)
{
    return (PRT_UINT32)v;
}

PRT_BOOLEAN PRT_FOREIGN_ISEQUAL_T_IMPL(PRT_UINT64 v1, PRT_UINT64 v2)
{
    return v1 == v2;
}

PRT_STRING PRT_FOREIGN_TOSTRING_T_IMPL(PRT_UINT64 v)
{
    return "";
}
