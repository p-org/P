
  #ifndef P_PROGRAM_H
    #define P_PROGRAM_H
    #include "PrtWinUser.h"
    #include "PrtExecution.h"
    enum P_EVENTS
    {
      _P_EVENT_NULL = 0,
      _P_EVENT_HALT = 1,
      P_EVENT_Ping = 2,
      P_EVENT_Pong = 3,
      P_EVENT_Req_CreateMachine = 4,
      P_EVENT_Resp_CreateMachine = 5,
      P_EVENT_Success = 6,
      _P_EVENTS_COUNT = 7
    };

    enum P_FUNS_GodMachine
    {
      _P_FUN_GodMachine_PUSH_OR_IGN = 0,
      P_FUN_GodMachine_ANON0 = 1,
      P_FUN_GodMachine_ANON1 = 2,
      P_FUN_GodMachine_ANON2 = 3,
      P_FUN_GodMachine_PopState = 4,
      _P_FUNS_GodMachine_COUNT = 5
    };

    enum P_FUNS_NodeManager
    {
      _P_FUN_NodeManager_PUSH_OR_IGN = 0,
      P_FUN_NodeManager_ANON0 = 1,
      P_FUN_NodeManager_ANON1 = 2,
      P_FUN_NodeManager_ANON2 = 3,
      P_FUN_NodeManager_PopState = 4,
      P_FUN_NodeManager__CREATELOCALMACHINE = 5,
      _P_FUNS_NodeManager_COUNT = 6
    };

    enum P_FUNS_PING
    {
      _P_FUN_PING_PUSH_OR_IGN = 0,
      P_FUN_PING_ANON0 = 1,
      P_FUN_PING_ANON1 = 2,
      P_FUN_PING_ANON2 = 3,
      P_FUN_PING_ANON3 = 4,
      P_FUN_PING_PopState = 5,
      _P_FUNS_PING_COUNT = 6
    };

    enum P_FUNS_PONG
    {
      _P_FUN_PONG_PUSH_OR_IGN = 0,
      P_FUN_PONG_ANON0 = 1,
      P_FUN_PONG_ANON1 = 2,
      P_FUN_PONG_ANON2 = 3,
      P_FUN_PONG_ANON3 = 4,
      P_FUN_PONG_PopState = 5,
      _P_FUNS_PONG_COUNT = 6
    };

    enum P_MACHINES
    {
      P_MACHINE_GodMachine = 0,
      _P_MACHINE_MAIN = 0,
      P_MACHINE_NodeManager = 1,
      P_MACHINE_PING = 2,
      P_MACHINE_PONG = 3,
      _P_MACHINES_COUNT = 4
    };

    enum P_MODELS
    {
      _P_MODELS_COUNT = 0
    };

    enum P_STATES_GodMachine
    {
      P_STATE_GodMachine_Init = 0,
      P_STATE_GodMachine__CREATEMACHINE = 1,
      _P_STATES_GodMachine_COUNT = 2
    };

    enum P_STATES_M
    {
      _P_STATES_M_COUNT = 3
    };

    enum P_STATES_NodeManager
    {
      P_STATE_NodeManager_CreateNewMachine = 0,
      P_STATE_NodeManager_Init = 1,
      P_STATE_NodeManager__CREATEMACHINE = 2,
      _P_STATES_NodeManager_COUNT = 3
    };

    enum P_STATES_PING
    {
      P_STATE_PING_Done = 0,
      P_STATE_PING_Init = 1,
      P_STATE_PING_Ping_SendPing = 2,
      P_STATE_PING_Ping_WaitPong_1 = 3,
      P_STATE_PING_Ping_WaitPong_2 = 4,
      P_STATE_PING__CREATEMACHINE = 5,
      _P_STATES_PING_COUNT = 6
    };

    enum P_STATES_PONG
    {
      P_STATE_PONG_End = 0,
      P_STATE_PONG_Init = 1,
      P_STATE_PONG_Pong_SendPong = 2,
      P_STATE_PONG__CREATEMACHINE = 3,
      _P_STATES_PONG_COUNT = 4
    };

    enum P_VARS_GodMachine
    {
      P_VAR_GodMachine_PongMachine_1 = 0,
      P_VAR_GodMachine_PongMachine_2 = 1,
      P_VAR_GodMachine_createmachine_param = 2,
      P_VAR_GodMachine_createmachine_return = 3,
      P_VAR_GodMachine_temp_NM = 4,
      _P_VARS_GodMachine_COUNT = 5
    };

    enum P_VARS_M
    {
      _P_VARS_M_COUNT = 0
    };

    enum P_VARS_NodeManager
    {
      P_VAR_NodeManager_createmachine_param = 0,
      P_VAR_NodeManager_createmachine_return = 1,
      P_VAR_NodeManager_newMachine = 2,
      _P_VARS_NodeManager_COUNT = 3
    };

    enum P_VARS_PING
    {
      P_VAR_PING_createmachine_param = 0,
      P_VAR_PING_createmachine_return = 1,
      P_VAR_PING_pongmachine = 2,
      _P_VARS_PING_COUNT = 3
    };

    enum P_VARS_PONG
    {
      P_VAR_PONG_createmachine_param = 0,
      P_VAR_PONG_createmachine_return = 1,
      _P_VARS_PONG_COUNT = 2
    };

    PRT_VALUE *P_FUN__CREATENODE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

    PRT_VALUE *P_FUN__SENDRELIABLE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

    PRT_VALUE *P_FUN__SEND_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

    void P_DTOR_GodMachine_IMPL(PRT_MACHINEINST *context);

    void P_DTOR_NodeManager_IMPL(PRT_MACHINEINST *context);

    void P_DTOR_PING_IMPL(PRT_MACHINEINST *context);

    void P_DTOR_PONG_IMPL(PRT_MACHINEINST *context);

    void P_CTOR_GodMachine_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_CTOR_NodeManager_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_CTOR_PING_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_CTOR_PONG_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    PRT_PROGRAMDECL P_GEND_PROGRAM;
  #endif
  