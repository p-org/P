
  #ifndef P_PROGRAM_H
    #define P_PROGRAM_H
    #include "PrtWinUser.h"
    #include "PrtExecution.h"
    enum P_EVENTS
    {
      _P_EVENT_DEFAULT = 0,
      _P_EVENT_HALT = 1,
      P_EVENT_Ding = 2,
      P_EVENT_Dong = 3,
      P_EVENT_Ping = 4,
      P_EVENT_Pong = 5,
      P_EVENT_Success = 6,
      _P_EVENTS_COUNT = 7
    };

    enum P_FUNS_PING
    {
      _P_FUN_PING_PUSH_OR_IGN = 0,
      P_FUN_PING_ANON0 = 1,
      P_FUN_PING_ANON1 = 2,
      P_FUN_PING_ANON2 = 3,
      P_FUN_PING_ANON3 = 4,
      _P_FUNS_PING_COUNT = 5
    };

    enum P_FUNS_PONG
    {
      _P_FUN_PONG_PUSH_OR_IGN = 0,
      P_FUN_PONG_ANON0 = 1,
      P_FUN_PONG_ANON1 = 2,
      P_FUN_PONG_ANON2 = 3,
      P_FUN_PONG_ANON3 = 4,
      _P_FUNS_PONG_COUNT = 5
    };

    enum P_MACHINES
    {
      P_MACHINE_PING = 0,
      _P_MACHINE_MAIN = 0,
      P_MACHINE_PONG = 1,
      _P_MACHINES_COUNT = 2
    };

    enum P_MODELS
    {
      _P_MODELS_COUNT = 0
    };

    enum P_STATES_PING
    {
      P_STATE_PING_Ping_ding1 = 0,
      P_STATE_PING_Ping_ping1 = 1,
      P_STATE_PING_Ping_ping2 = 2,
      P_STATE_PING_Ping_start = 3,
      _P_STATES_PING_COUNT = 4
    };

    enum P_STATES_PONG
    {
      P_STATE_PONG_Pong_dong1 = 0,
      P_STATE_PONG_Pong_dong2 = 1,
      P_STATE_PONG_Pong_pong1 = 2,
      P_STATE_PONG_Pong_start = 3,
      P_STATE_PONG__Init = 4,
      _P_STATES_PONG_COUNT = 5
    };

    enum P_VARS_PING
    {
      P_VAR_PING_pongId = 0,
      _P_VARS_PING_COUNT = 1
    };

    enum P_VARS_PONG
    {
      P_VAR_PONG_pingid = 0,
      _P_VARS_PONG_COUNT = 1
    };

    void P_DTOR_PING_IMPL(PRT_SM_CONTEXT *context);

    void P_DTOR_PONG_IMPL(PRT_SM_CONTEXT *context);

    void P_CTOR_PING_IMPL(PRT_SM_CONTEXT *context, PRT_VALUE *value);

    void P_CTOR_PONG_IMPL(PRT_SM_CONTEXT *context, PRT_VALUE *value);

    PRT_PROGRAMDECL P_GEND_PROGRAM;
  #endif
  