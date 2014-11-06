
  #ifndef P_PROGRAM_H
    #define P_PROGRAM_H
    #include "PrtWinUser.h"
    #include "PrtExecution.h"
    enum P_EVENTS
    {
      _P_EVENT_DEFAULT = 0,
      _P_EVENT_HALT = 1,
      P_EVENT_E1 = 2,
      P_EVENT_E2 = 3,
      P_EVENT_E3 = 4,
      P_EVENT_E4 = 5,
      P_EVENT_unit = 6,
      _P_EVENTS_COUNT = 7
    };

    enum P_FUNS_Ghost
    {
      _P_FUN_Ghost_PUSH_OR_IGN = 0,
      P_FUN_Ghost_ANON0 = 1,
      P_FUN_Ghost_ANON1 = 2,
      P_FUN_Ghost_ANON2 = 3,
      P_FUN_Ghost_ANON3 = 4,
      _P_FUNS_Ghost_COUNT = 5
    };

    enum P_FUNS_Real
    {
      _P_FUN_Real_PUSH_OR_IGN = 0,
      P_FUN_Real_ANON0 = 1,
      P_FUN_Real_ANON1 = 2,
      P_FUN_Real_ANON2 = 3,
      P_FUN_Real_ANON3 = 4,
      P_FUN_Real_Action1 = 5,
      _P_FUNS_Real_COUNT = 6
    };

    enum P_MACHINES
    {
      P_MACHINE_Ghost = 0,
      P_MACHINE_Real = 1,
      _P_MACHINE_MAIN = 1,
      _P_MACHINES_COUNT = 2
    };

    enum P_MODELS
    {
      _P_MODELS_COUNT = 0
    };

    enum P_STATES_Ghost
    {
      P_STATE_Ghost_Ghost_Init = 0,
      P_STATE_Ghost_Ghost_S1 = 1,
      P_STATE_Ghost_Ghost_S2 = 2,
      _P_STATES_Ghost_COUNT = 3
    };

    enum P_STATES_Real
    {
      P_STATE_Real_Real_Init = 0,
      P_STATE_Real_Real_S1 = 1,
      P_STATE_Real_Real_S2 = 2,
      _P_STATES_Real_COUNT = 3
    };

    enum P_VARS_Ghost
    {
      P_VAR_Ghost_real_machine = 0,
      _P_VARS_Ghost_COUNT = 1
    };

    enum P_VARS_Real
    {
      P_VAR_Real_ghost_machine = 0,
      _P_VARS_Real_COUNT = 1
    };

    void P_DTOR_Ghost_IMPL(PRT_MACHINEINST *context);

    void P_DTOR_Real_IMPL(PRT_MACHINEINST *context);

    void P_CTOR_Ghost_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_CTOR_Real_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    PRT_PROGRAMDECL P_GEND_PROGRAM;
  #endif
  
