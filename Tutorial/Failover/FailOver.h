#pragma once
#ifndef P_FAILOVER_H_
#define P_FAILOVER_H_
#include "Prt.h"

#ifdef __cplusplus
extern "C" {
#endif
    
    typedef enum P_ENUM_MyState { P_ENUMELEM_MyState_State0 = 0, P_ENUMELEM_MyState_State1 = 1 } P_ENUM_MyState;
    
    extern PRT_EVENTDECL P_EVENT_eDoOpI;
    extern PRT_EVENTDECL P_EVENT_eDoOpJ;
    extern PRT_EVENTDECL P_EVENT_eQueryState;
    extern PRT_EVENTDECL P_EVENT_eQueryStateResponse;
    extern PRT_EVENTDECL P_EVENT_eUpdateToState0;
    extern PRT_EVENTDECL P_EVENT_eUpdateToState1;
    extern PRT_INTERFACEDECL P_I_TestDriver;
    extern PRT_INTERFACEDECL P_I_FaultTolerantMachine;
    extern PRT_INTERFACEDECL P_I_ServiceMachine;
    extern PRT_INTERFACEDECL P_I_ReliableStorageMachine;
    extern PRT_MACHINEDECL P_MACHINE_TestDriver;
    extern PRT_MACHINEDECL P_MACHINE_FaultTolerantMachine;
    extern PRT_MACHINEDECL P_MACHINE_ServiceMachine;
    extern PRT_MACHINEDECL P_MACHINE_ReliableStorageMachine;
    extern PRT_TYPE* P_TYPEDEF_Pair;
    // DECL(SafetyTest, Test0) => P_SAFETY_TEST_Test0
    
#ifdef __cplusplus
}
#endif

#endif // P_FAILOVER_H_
