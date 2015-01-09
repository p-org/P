
  #ifndef P_PROGRAM_H
    #define P_PROGRAM_H
    #include "PrtWinUser.h"
    #include "PrtExecution.h"
    enum P_EVENTS
    {
      _P_EVENT_DEFAULT = 0,
      _P_EVENT_HALT = 1,
      P_EVENT_E = 2,
      _P_EVENTS_COUNT = 3
    };

    enum P_FUNS_Program
    {
      _P_FUN_Program_PUSH_OR_IGN = 0,
      P_FUN_Program_ANON0 = 1,
      P_FUN_Program_ANON1 = 2,
      P_FUN_Program_ANON2 = 3,
      P_FUN_Program_ANON3 = 4,
      _P_FUNS_Program_COUNT = 5
    };

    enum P_MACHINES
    {
      P_MACHINE_Program = 0,
      _P_MACHINE_MAIN = 0,
      _P_MACHINES_COUNT = 1
    };

    enum P_MODELS
    {
      _P_MODELS_COUNT = 0
    };

    enum P_STATES_Program
    {
      P_STATE_Program_Call = 0,
      P_STATE_Program_Init = 1,
      _P_STATES_Program_COUNT = 2
    };

    enum P_VARS_Program
    {
      P_VAR_Program_i = 0,
      _P_VARS_Program_COUNT = 1
    };

    void P_DTOR_Program_IMPL(PRT_MACHINEINST *context);

    void P_CTOR_Program_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    PRT_PROGRAMDECL P_GEND_PROGRAM;
  #endif
  