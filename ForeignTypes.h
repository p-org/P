#pragma once
#ifndef P_FOREIGNTYPES_H_
#define P_FOREIGNTYPES_H_
#include "Prt.h"

#ifdef __cplusplus
extern "C" {
#endif
    
    extern PRT_EVENTDECL P_FOREIGNTYPES_EVENT_sendback;
    extern PRT_EVENTDECL P_FOREIGNTYPES_EVENT_getback;
    extern PRT_INTERFACEDECL P_FOREIGNTYPES_I_TestMachine;
    extern PRT_INTERFACEDECL P_FOREIGNTYPES_I_ForwardingMachine;
    extern PRT_MACHINEDECL P_FOREIGNTYPES_MACHINE_TestMachine;
    extern PRT_MACHINEDECL P_FOREIGNTYPES_MACHINE_ForwardingMachine;
    extern PRT_TYPE* P_FOREIGNTYPES_TYPEDEF_StringType;
    extern PRT_PROGRAMDECL P_GEND_IMPL_DefaultImpl;
    
#ifdef __cplusplus
}
#endif

#endif // P_FOREIGNTYPES_H_
