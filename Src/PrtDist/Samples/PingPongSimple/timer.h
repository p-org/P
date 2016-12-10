
  #ifndef P_PROGRAM_H
    #define P_PROGRAM_H
    #include "linker.h"
    #ifdef __cplusplus
      extern "C"{
    #endif
    enum P_FUNS_Timer
    {
      P_FUN_Timer_ANON0 = 1,
      P_FUN_Timer_ANON1 = 3,
      P_FUN_Timer_ANON2 = 5,
      P_FUN_Timer_ANON3 = 7,
      P_FUN_Timer_ANON4 = 9,
      P_FUN_Timer_ANON5 = 11,
      _P_FUNS_Timer_COUNT = 12,
      P_FUN_Timer_ANON6 = 13,
      P_FUN_Timer_ANON7 = 15,
      P_FUN_Timer_ANON8 = 17,
      P_FUN_Timer_ANON9 = 19,
      P_FUN_Timer_ANON10 = 21,
      P_FUN_Timer_ANON11 = 23
    };

    enum P_STATES_Timer
    {
      P_STATE_Timer_Init = 0,
      P_STATE_Timer_WaitForCancel = 1,
      P_STATE_Timer_WaitForReq = 2,
      _P_STATES_Timer_COUNT = 3
    };

    enum P_VARS_Timer
    {
      P_VAR_Timer_client = 0,
      _P_VARS_Timer_COUNT = 1
    };

    void P_DTOR_Timer_IMPL(PRT_MACHINEINST *context);

    void P_CTOR_Timer_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    PRT_VALUE *P_FUN_CancelTimer_IMPL(PRT_MACHINEINST *context);

    PRT_VALUE *P_FUN_StartTimer_IMPL(PRT_MACHINEINST *context);

    #ifdef __cplusplus
      }
    #endif
  #endif
  