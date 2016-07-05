
  #ifndef P_PROGRAM_H
    #define P_PROGRAM_H
    #include "PrtUser.h"
    #include "PrtExecution.h"
    #ifdef __cplusplus
      extern "C"{
    #endif
    enum P_EVENTS
    {
      _P_EVENT_NULL = 0,
      _P_EVENT_HALT = 1,
      P_EVENT_CANCEL = 2,
      P_EVENT_CANCEL_FAILURE = 3,
      P_EVENT_CANCEL_SUCCESS = 4,
      P_EVENT_L_INIT = 5,
      P_EVENT_M_PING = 6,
      P_EVENT_M_PONG = 7,
      P_EVENT_NODE_DOWN = 8,
      P_EVENT_PING = 9,
      P_EVENT_PONG = 10,
      P_EVENT_REGISTER_CLIENT = 11,
      P_EVENT_ROUND_DONE = 12,
      P_EVENT_START = 13,
      P_EVENT_TIMEOUT = 14,
      P_EVENT_UNIT = 15,
      P_EVENT_UNREGISTER_CLIENT = 16,
      _P_EVENTS_COUNT = 17
    };

    enum P_FORGN_TYPES
    {
      _P_FORGN_TYPES_COUNT = 0
    };

    enum P_FUNS_Container
    {
      P_FUN_Container_ANON0 = 4,
      _P_FUNS_Container_COUNT = 5
    };

    enum P_FUNS_Driver
    {
      P_FUN_Driver_ANON0 = 4,
      P_FUN_Driver_ANON1 = 5,
      _P_FUNS_Driver_COUNT = 6
    };

    enum P_FUNS_FailureDetector
    {
      P_FUN_FailureDetector_ANON0 = 4,
      P_FUN_FailureDetector_ANON1 = 5,
      P_FUN_FailureDetector_ANON2 = 6,
      P_FUN_FailureDetector_ANON3 = 7,
      P_FUN_FailureDetector_ANON4 = 8,
      P_FUN_FailureDetector_ANON5 = 9,
      P_FUN_FailureDetector_ANON6 = 10,
      P_FUN_FailureDetector_ANON7 = 11,
      P_FUN_FailureDetector_ANON8 = 12,
      P_FUN_FailureDetector_ANON9 = 13,
      P_FUN_FailureDetector_CancelTimer = 14,
      P_FUN_FailureDetector_InitializeAliveSet = 15,
      P_FUN_FailureDetector_Notify = 16,
      P_FUN_FailureDetector_SendPingsToAliveSet = 17,
      _P_FUNS_FailureDetector_COUNT = 18
    };

    enum P_FUNS_GLOBAL
    {
      _P_FUN_PUSH_OR_IGN = 0,
      P_FUN_CreateNode = 1,
      P_FUN__CREATECONTAINER = 2,
      P_FUN__SEND = 3
    };

    enum P_FUNS_Node
    {
      P_FUN_Node_ANON0 = 4,
      P_FUN_Node_ANON1 = 5,
      _P_FUNS_Node_COUNT = 6
    };

    enum P_MACHINES
    {
      P_MACHINE_Container = 0,
      P_MACHINE_Driver = 1,
      _P_MACHINE_MAIN = 1,
      P_MACHINE_FailureDetector = 2,
      P_MACHINE_Node = 3,
      _P_MACHINES_COUNT = 4
    };

    enum P_MODELS
    {
      P_MODEL_Timer = 0,
      _P_MODELS_COUNT = 1
    };

    enum P_STATES_Container
    {
      P_STATE_Container_Init = 0,
      _P_STATES_Container_COUNT = 1
    };

    enum P_STATES_Driver
    {
      P_STATE_Driver_Init = 0,
      _P_STATES_Driver_COUNT = 1
    };

    enum P_STATES_FailureDetector
    {
      P_STATE_FailureDetector_Init = 0,
      P_STATE_FailureDetector_Reset = 1,
      P_STATE_FailureDetector_SendPing = 2,
      _P_STATES_FailureDetector_COUNT = 3
    };

    enum P_STATES_Liveness
    {
      _P_STATES_Liveness_COUNT = 2
    };

    enum P_STATES_Node
    {
      P_STATE_Node_WaitPing = 0,
      _P_STATES_Node_COUNT = 1
    };

    enum P_STATES_Safety
    {
      _P_STATES_Safety_COUNT = 1
    };

    enum P_STATES_Timer
    {
      _P_STATES_Timer_COUNT = 3
    };

    enum P_VARS_Container
    {
      _P_VARS_Container_COUNT = 0
    };

    enum P_VARS_Driver
    {
      P_VAR_Driver_container = 0,
      P_VAR_Driver_fd = 1,
      P_VAR_Driver_i = 2,
      P_VAR_Driver_n = 3,
      P_VAR_Driver_nodemap = 4,
      P_VAR_Driver_nodeseq = 5,
      _P_VARS_Driver_COUNT = 6
    };

    enum P_VARS_FailureDetector
    {
      P_VAR_FailureDetector_alive = 0,
      P_VAR_FailureDetector_attempts = 1,
      P_VAR_FailureDetector_clients = 2,
      P_VAR_FailureDetector_nodes = 3,
      P_VAR_FailureDetector_responses = 4,
      P_VAR_FailureDetector_timer = 5,
      _P_VARS_FailureDetector_COUNT = 6
    };

    enum P_VARS_Liveness
    {
      _P_VARS_Liveness_COUNT = 1
    };

    enum P_VARS_Node
    {
      _P_VARS_Node_COUNT = 0
    };

    enum P_VARS_Safety
    {
      _P_VARS_Safety_COUNT = 1
    };

    enum P_VARS_Timer
    {
      _P_VARS_Timer_COUNT = 1
    };

    PRT_VALUE *P_FUN__CREATECONTAINER_IMPL(PRT_MACHINEINST *context);

    PRT_VALUE *P_FUN__SEND_IMPL(PRT_MACHINEINST *context);

    void P_DTOR_Container_IMPL(PRT_MACHINEINST *context);

    void P_DTOR_Driver_IMPL(PRT_MACHINEINST *context);

    void P_DTOR_FailureDetector_IMPL(PRT_MACHINEINST *context);

    void P_DTOR_Node_IMPL(PRT_MACHINEINST *context);

    void P_CTOR_Container_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_CTOR_Driver_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_CTOR_FailureDetector_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_CTOR_Node_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_DTOR_Timer_IMPL(PRT_MACHINEINST *context);

    void P_CTOR_Timer_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value);

    void P_SEND_Timer_IMPL(PRT_MACHINEINST *context, PRT_VALUE *evnt, PRT_VALUE *payload, PRT_BOOLEAN doTransfer);

    extern PRT_PROGRAMDECL P_GEND_PROGRAM;
    #ifdef __cplusplus
      }
    #endif
  #endif
  