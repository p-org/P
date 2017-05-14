
  #ifndef P_TESTDRIVER_H_
    #define P_TESTDRIVER_H_
    #include "PrtUser.h"
    #include "PrtExecution.h"
    #ifdef __cplusplus
      extern "C"{
    #endif
    enum P_FIELD_INDEX_requestType
    {
      P_FIELD_INDEX_requestType_source = 0,
      P_FIELD_INDEX_requestType_id = 1,
      _P_FIELD_INDEX_requestType_COUNT = 2
    };

    enum P_FIELD_INDEX_responseType
    {
      P_FIELD_INDEX_responseType_id = 0,
      P_FIELD_INDEX_responseType_success = 1,
      _P_FIELD_INDEX_responseType_COUNT = 2
    };

    enum P_FUNS_ClientMachine
    {
      P_FUN_ClientMachine_ANON0 = 1,
      P_FUN_ClientMachine_ANON1 = 3,
      P_FUN_ClientMachine_ANON2 = 5,
      P_FUN_ClientMachine_ANON3 = 7,
      _P_FUNS_ClientMachine_COUNT = 7,
      P_FUN_ClientMachine_ANON4 = 9,
      P_FUN_ClientMachine_ANON5 = 11,
      P_FUN_ClientMachine_ANON6 = 13
    };

    enum P_FUNS_HelperMachine
    {
      P_FUN_HelperMachine_ANON0 = 1,
      P_FUN_HelperMachine_ANON1 = 3,
      _P_FUNS_HelperMachine_COUNT = 4,
      P_FUN_HelperMachine_ANON2 = 5,
      P_FUN_HelperMachine_ANON3 = 7
    };

    enum P_FUNS_ReqIdsAreMonotonicallyIncreasing
    {
      P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON0 = 1,
      P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON1 = 3,
      _P_FUNS_ReqIdsAreMonotonicallyIncreasing_COUNT = 3,
      P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON2 = 5
    };

    enum P_FUNS_ServerAbstractionMachine
    {
      P_FUN_ServerAbstractionMachine_ANON0 = 1,
      P_FUN_ServerAbstractionMachine_ANON1 = 3,
      _P_FUNS_ServerAbstractionMachine_COUNT = 3,
      P_FUN_ServerAbstractionMachine_ANON2 = 5
    };

    enum P_FUNS_ServerMachine
    {
      P_FUN_ServerMachine_ANON0 = 1,
      P_FUN_ServerMachine_ANON1 = 3,
      P_FUN_ServerMachine_ANON2 = 5,
      P_FUN_ServerMachine_ANON3 = 7,
      _P_FUNS_ServerMachine_COUNT = 8,
      P_FUN_ServerMachine_ANON4 = 9,
      P_FUN_ServerMachine_ANON5 = 11,
      P_FUN_ServerMachine_ANON6 = 13,
      P_FUN_ServerMachine_ANON7 = 15
    };

    enum P_FUNS_TestDriver_1Client1Server
    {
      P_FUN_TestDriver_1Client1Server_ANON0 = 1,
      P_FUN_TestDriver_1Client1Server_ANON1 = 3,
      _P_FUNS_TestDriver_1Client1Server_COUNT = 3,
      P_FUN_TestDriver_1Client1Server_ANON2 = 5
    };

    enum P_FUNS_TestDriver_Refinement
    {
      P_FUN_TestDriver_Refinement_ANON0 = 1,
      P_FUN_TestDriver_Refinement_ANON1 = 3,
      P_FUN_TestDriver_Refinement_ANON2 = 5,
      _P_FUNS_TestDriver_Refinement_COUNT = 6,
      P_FUN_TestDriver_Refinement_ANON3 = 7,
      P_FUN_TestDriver_Refinement_ANON4 = 9,
      P_FUN_TestDriver_Refinement_ANON5 = 11
    };

    enum P_STATES_ClientMachine
    {
      P_STATE_ClientMachine_Init = 0,
      P_STATE_ClientMachine_StartPumpingRequests = 1,
      _P_STATES_ClientMachine_COUNT = 2
    };

    enum P_STATES_HelperMachine
    {
      P_STATE_HelperMachine_Init = 0,
      _P_STATES_HelperMachine_COUNT = 1
    };

    enum P_STATES_ReqIdsAreMonotonicallyIncreasing
    {
      P_STATE_ReqIdsAreMonotonicallyIncreasing_Init = 0,
      _P_STATES_ReqIdsAreMonotonicallyIncreasing_COUNT = 1
    };

    enum P_STATES_ServerAbstractionMachine
    {
      P_STATE_ServerAbstractionMachine_Init = 0,
      _P_STATES_ServerAbstractionMachine_COUNT = 1
    };

    enum P_STATES_ServerMachine
    {
      P_STATE_ServerMachine_Init = 0,
      P_STATE_ServerMachine_WaitForRequests = 1,
      _P_STATES_ServerMachine_COUNT = 2
    };

    enum P_STATES_TestDriver_1Client1Server
    {
      P_STATE_TestDriver_1Client1Server_Init = 0,
      _P_STATES_TestDriver_1Client1Server_COUNT = 1
    };

    enum P_STATES_TestDriver_Refinement
    {
      P_STATE_TestDriver_Refinement_Init = 0,
      P_STATE_TestDriver_Refinement_StartPumpingRequests = 1,
      _P_STATES_TestDriver_Refinement_COUNT = 2
    };

    enum P_VARS_ClientMachine
    {
      P_VAR_ClientMachine_lastRecvSuccessfulReqId = 0,
      P_VAR_ClientMachine_nextReqId = 1,
      P_VAR_ClientMachine_server = 2,
      _P_VARS_ClientMachine_COUNT = 3
    };

    enum P_VARS_HelperMachine
    {
      P_VAR_HelperMachine_server = 0,
      _P_VARS_HelperMachine_COUNT = 1
    };

    enum P_VARS_ReqIdsAreMonotonicallyIncreasing
    {
      P_VAR_ReqIdsAreMonotonicallyIncreasing_previousId = 0,
      _P_VARS_ReqIdsAreMonotonicallyIncreasing_COUNT = 1
    };

    enum P_VARS_ServerAbstractionMachine
    {
      _P_VARS_ServerAbstractionMachine_COUNT = 0
    };

    enum P_VARS_ServerMachine
    {
      P_VAR_ServerMachine_helper = 0,
      _P_VARS_ServerMachine_COUNT = 1
    };

    enum P_VARS_TestDriver_1Client1Server
    {
      _P_VARS_TestDriver_1Client1Server_COUNT = 0
    };

    enum P_VARS_TestDriver_Refinement
    {
      P_VAR_TestDriver_Refinement_nextReqId = 0,
      P_VAR_TestDriver_Refinement_server = 1,
      _P_VARS_TestDriver_Refinement_COUNT = 2
    };

    extern PRT_EVENTDECL P_EVENT_eProcessReq_STRUCT;
    extern PRT_EVENTDECL P_EVENT_eReqFailed_STRUCT;
    extern PRT_EVENTDECL P_EVENT_eReqSuccessful_STRUCT;
    extern PRT_EVENTDECL P_EVENT_eRequest_STRUCT;
    extern PRT_EVENTDECL P_EVENT_eResponse_STRUCT;
    extern PRT_EVENTDECL _P_EVENT_HALT_STRUCT;
    extern PRT_EVENTDECL _P_EVENT_NULL_STRUCT;
    extern PRT_EVENTSETDECL P_GEND_EVENTSET;
    extern PRT_EVENTSETDECL P_GEND_EVENTSET_eProcessReq;
    extern PRT_EVENTSETDECL P_GEND_EVENTSET_eReqSuccessful_eReqFailed;
    extern PRT_EVENTSETDECL P_GEND_EVENTSET_eRequest;
    extern PRT_EVENTSETDECL P_GEND_EVENTSET_eResponse;
    extern PRT_EVENTSETDECL P_GEND_EVENTSET_null_eResponse;
    extern PRT_FUNDECL P_FUN_ClientMachine_ANON0_STRUCT;
    extern PRT_FUNDECL P_FUN_ClientMachine_ANON1_STRUCT;
    extern PRT_FUNDECL P_FUN_ClientMachine_ANON2_STRUCT;
    extern PRT_FUNDECL P_FUN_ClientMachine_ANON3_STRUCT;
    extern PRT_FUNDECL P_FUN_ClientMachine_ANON4_STRUCT;
    extern PRT_FUNDECL P_FUN_ClientMachine_ANON5_STRUCT;
    extern PRT_FUNDECL P_FUN_ClientMachine_ANON6_STRUCT;
    extern PRT_FUNDECL P_FUN_HelperMachine_ANON0_STRUCT;
    extern PRT_FUNDECL P_FUN_HelperMachine_ANON1_STRUCT;
    extern PRT_FUNDECL P_FUN_HelperMachine_ANON2_STRUCT;
    extern PRT_FUNDECL P_FUN_HelperMachine_ANON3_STRUCT;
    extern PRT_FUNDECL P_FUN_IGNORE_PUSH_STRUCT;
    extern PRT_FUNDECL P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON0_STRUCT;
    extern PRT_FUNDECL P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON1_STRUCT;
    extern PRT_FUNDECL P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON2_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerAbstractionMachine_ANON0_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerAbstractionMachine_ANON1_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerAbstractionMachine_ANON2_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerMachine_ANON0_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerMachine_ANON1_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerMachine_ANON2_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerMachine_ANON3_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerMachine_ANON4_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerMachine_ANON5_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerMachine_ANON6_STRUCT;
    extern PRT_FUNDECL P_FUN_ServerMachine_ANON7_STRUCT;
    extern PRT_FUNDECL P_FUN_TestDriver_1Client1Server_ANON0_STRUCT;
    extern PRT_FUNDECL P_FUN_TestDriver_1Client1Server_ANON1_STRUCT;
    extern PRT_FUNDECL P_FUN_TestDriver_1Client1Server_ANON2_STRUCT;
    extern PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON0_STRUCT;
    extern PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON1_STRUCT;
    extern PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON2_STRUCT;
    extern PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON3_STRUCT;
    extern PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON4_STRUCT;
    extern PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON5_STRUCT;
    extern PRT_MACHINEDECL P_MACHINE_ClientMachine_STRUCT;
    extern PRT_MACHINEDECL P_MACHINE_HelperMachine_STRUCT;
    extern PRT_MACHINEDECL P_MACHINE_ReqIdsAreMonotonicallyIncreasing_STRUCT;
    extern PRT_MACHINEDECL P_MACHINE_ServerAbstractionMachine_STRUCT;
    extern PRT_MACHINEDECL P_MACHINE_ServerMachine_STRUCT;
    extern PRT_MACHINEDECL P_MACHINE_TestDriver_1Client1Server_STRUCT;
    extern PRT_MACHINEDECL P_MACHINE_TestDriver_Refinement_STRUCT;
    extern PRT_PROGRAMDECL P_GEND_PROGRAM;
    extern PRT_TYPE P_GEND_TYPE_ClientInterface;
    extern PRT_TYPE P_GEND_TYPE_HelperInterface;
    extern PRT_TYPE P_GEND_TYPE_ServerClientInterface;
    extern PRT_TYPE P_GEND_TYPE_ServerHelperInterface;
    extern PRT_TYPE P_GEND_TYPE_requestType;
    extern PRT_TYPE P_GEND_TYPE_responseType;
    extern PRT_UINT32 P_EVENT_eProcessReq;
    extern PRT_UINT32 P_EVENT_eReqFailed;
    extern PRT_UINT32 P_EVENT_eReqSuccessful;
    extern PRT_UINT32 P_EVENT_eRequest;
    extern PRT_UINT32 P_EVENT_eResponse;
    extern PRT_UINT32 P_IORM_ClientInterface;
    extern PRT_UINT32 P_IORM_ClientMachine;
    extern PRT_UINT32 P_IORM_HelperInterface;
    extern PRT_UINT32 P_IORM_HelperMachine;
    extern PRT_UINT32 P_IORM_ReqIdsAreMonotonicallyIncreasing;
    extern PRT_UINT32 P_IORM_ServerAbstractionMachine;
    extern PRT_UINT32 P_IORM_ServerClientInterface;
    extern PRT_UINT32 P_IORM_ServerHelperInterface;
    extern PRT_UINT32 P_IORM_ServerMachine;
    extern PRT_UINT32 P_IORM_TestDriver_1Client1Server;
    extern PRT_UINT32 P_IORM_TestDriver_Refinement;
    extern PRT_UINT32 P_MACHINE_ClientMachine;
    extern PRT_UINT32 P_MACHINE_HelperMachine;
    extern PRT_UINT32 P_MACHINE_ReqIdsAreMonotonicallyIncreasing;
    extern PRT_UINT32 P_MACHINE_ServerAbstractionMachine;
    extern PRT_UINT32 P_MACHINE_ServerMachine;
    extern PRT_UINT32 P_MACHINE_TestDriver_1Client1Server;
    extern PRT_UINT32 P_MACHINE_TestDriver_Refinement;
    extern PRT_UINT32 _P_EVENT_HALT;
    extern PRT_UINT32 _P_EVENT_NULL;
    extern PRT_VALUE P_GEND_VALUE_EVENT_HALT;
    extern PRT_VALUE P_GEND_VALUE_EVENT_NULL;
    extern PRT_VALUE P_GEND_VALUE_EVENT_eProcessReq;
    extern PRT_VALUE P_GEND_VALUE_EVENT_eReqFailed;
    extern PRT_VALUE P_GEND_VALUE_EVENT_eReqSuccessful;
    extern PRT_VALUE P_GEND_VALUE_EVENT_eRequest;
    extern PRT_VALUE P_GEND_VALUE_EVENT_eResponse;
    #ifdef __cplusplus
      }
    #endif
  #endif
  