
  #ifndef P_PROGRAM_H
    #define P_PROGRAM_H
    #include "PrtWinUser.h"
    #include "PrtExecution.h"
    enum P_EVENTS
    {
      _P_EVENT_DEFAULT = 0,
      _P_EVENT_HALT = 1,
      P_EVENT_Ping = 2,
      P_EVENT_Pong = 3,
      P_EVENT_Success = 4,
      _P_EVENTS_COUNT = 5
    };

    enum P_FUNS_PING
    {
      _P_FUN_PING_PUSH_OR_IGN = 0,
      P_FUN_PING_ANON0 = 1,
      P_FUN_PING_ANON1 = 2,
      P_FUN_PING_ANON2 = 3,
      _P_FUNS_PING_COUNT = 4
    };

    enum P_FUNS_PONG
    {
      _P_FUN_PONG_PUSH_OR_IGN = 0,
      P_FUN_PONG_ANON0 = 1,
      P_FUN_PONG_ANON1 = 2,
      _P_FUNS_PONG_COUNT = 3
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
      P_STATE_PING_Done = 0,
      P_STATE_PING_Ping_Init = 1,
      P_STATE_PING_Ping_SendPing = 2,
      P_STATE_PING_Ping_WaitPong = 3,
      _P_STATES_PING_COUNT = 4
    };

    enum P_STATES_PONG
    {
      P_STATE_PONG_Pong_SendPong = 0,
      P_STATE_PONG_Pong_WaitPing = 1,
      _P_STATES_PONG_COUNT = 2
    };

    enum P_VARS_PING
    {
      P_VAR_PING_pongId = 0,
      _P_VARS_PING_COUNT = 1
    };

    enum P_VARS_PONG
    {
      _P_VARS_PONG_COUNT = 0
    };

    void P_DTOR_PING_IMPL(PRT_MACHINEINST *context);

    void P_DTOR_PONG_IMPL(PRT_MACHINEINST *context);

    void P_CTOR_PING_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_CTOR_PONG_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    PRT_PROGRAMDECL P_GEND_PROGRAM;
  #endif
  
