#pragma once
#ifndef P_PINGPONG_H_
#define P_PINGPONG_H_
#include "Prt.h"

#ifdef __cplusplus
extern "C" {
#endif
    
    extern PRT_EVENTDECL P_EVENT_START;
    extern PRT_EVENTDECL P_EVENT_TIMEOUT;
    extern PRT_EVENTDECL P_EVENT_PING;
    extern PRT_EVENTDECL P_EVENT_PONG;
    extern PRT_EVENTDECL P_EVENT_SUCCESS;
    extern PRT_FUNDECL P_FUNCTION_CreateTimer;
    extern PRT_FUNDECL P_FUNCTION_StartTimer;
    extern PRT_FUNDECL P_FUNCTION_StopProgram;
    extern PRT_INTERFACEDECL P_I_Timer;
    extern PRT_INTERFACEDECL P_I_Test_1_Machine;
    extern PRT_INTERFACEDECL P_I_Test_2_Machine;
    extern PRT_INTERFACEDECL P_I_ClientMachine;
    extern PRT_INTERFACEDECL P_I_ServerMachine;
    extern PRT_MACHINEDECL P_MACHINE_Timer;
    extern PRT_MACHINEDECL P_MACHINE_Test_1_Machine;
    extern PRT_MACHINEDECL P_MACHINE_Test_2_Machine;
    extern PRT_MACHINEDECL P_MACHINE_ClientMachine;
    extern PRT_MACHINEDECL P_MACHINE_ServerMachine;
    extern PRT_MACHINEDECL P_MACHINE_Safety;
    extern PRT_MACHINEDECL P_MACHINE_Liveness;
    extern PRT_TYPE* P_TYPEDEF_TimerPtr;
    // DECL(SafetyTest, Test0) => P_SAFETY_TEST_Test0
    // DECL(SafetyTest, Test1) => P_SAFETY_TEST_Test1
    // DECL(NamedModule, System) => P_MODULE_System
    
#ifdef __cplusplus
}
#endif

#endif // P_PINGPONG_H_
