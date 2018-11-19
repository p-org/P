#pragma once
#ifndef P_HELLO_H_
#define P_HELLO_H_
#include "Prt.h"

#ifdef __cplusplus
extern "C" {
#endif
    
    extern PRT_EVENTDECL P_EVENT_START;
    extern PRT_EVENTDECL P_EVENT_TIMEOUT;
    extern PRT_FUNDECL P_FUNCTION_CreateTimer;
    extern PRT_FUNDECL P_FUNCTION_StartTimer;
    extern PRT_FUNDECL P_FUNCTION_StopProgram;
    extern PRT_FUNDECL P_FUNCTION_Continue;
    extern PRT_INTERFACEDECL P_I_Timer;
    extern PRT_INTERFACEDECL P_I_Hello;
    extern PRT_MACHINEDECL P_MACHINE_Timer;
    extern PRT_MACHINEDECL P_MACHINE_Hello;
    extern PRT_TYPE* P_TYPEDEF_TimerPtr;
    // DECL(SafetyTest, Test0) => P_SAFETY_TEST_Test0
    
#ifdef __cplusplus
}
#endif

#endif // P_HELLO_H_
