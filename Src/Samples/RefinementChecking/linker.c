
  #include "linker.h"
  PRT_UINT32 P_EVENT_eProcessReq = 2;
  PRT_UINT32 P_EVENT_eReqFailed = 3;
  PRT_UINT32 P_EVENT_eReqSuccessful = 4;
  PRT_UINT32 P_EVENT_eRequest = 5;
  PRT_UINT32 P_EVENT_eResponse = 6;
  PRT_UINT32 P_IORM_ClientInterface = 0;
  PRT_UINT32 P_IORM_HelperInterface = 1;
  PRT_UINT32 P_IORM_ServerClientInterface = 2;
  PRT_UINT32 P_MACHINE_ClientMachine = 0;
  PRT_UINT32 P_MACHINE_HelperMachine = 1;
  PRT_UINT32 P_MACHINE_ReqIdsAreMonotonicallyIncreasing = 2;
  PRT_UINT32 P_MACHINE_ServerAbstractionMachine = 3;
  PRT_UINT32 P_MACHINE_ServerMachine = 4;
  PRT_UINT32 P_MACHINE_TestDriver_1Client1Server = 5;
  PRT_UINT32 P_MACHINE_TestDriver_Refinement = 6;
  PRT_UINT32 P_RENAME_ClientMachine = 0;
  PRT_UINT32 P_RENAME_HelperMachine = 1;
  PRT_UINT32 P_RENAME_ServerMachine = 2;
  PRT_UINT32 _P_EVENTS_COUNT = 9;
  PRT_UINT32 _P_EVENT_HALT = 1;
  PRT_UINT32 _P_EVENT_NULL = 0;
  PRT_UINT32 _P_FOREIGN_TYPES_COUNT = 0;
  PRT_UINT32 _P_FUNS_COUNT = 1;
  PRT_UINT32 _P_FUN_PUSH_OR_IGN = 0;
  PRT_UINT32 _P_IORM_COUNT = 3;
  PRT_UINT32 _P_MACHINES_COUNT = 7;
  PRT_UINT32 _P_RENAME_COUNT = 3;
  PRT_VALUE P_GEND_VALUE_EVENT_HALT = 
  {
    PRT_VALUE_KIND_EVENT,
    1U
  };
  PRT_VALUE P_GEND_VALUE_EVENT_NULL = 
  {
    PRT_VALUE_KIND_NULL,
    0U
  };
  PRT_VALUE P_GEND_VALUE_EVENT_eProcessReq = 
  {
    PRT_VALUE_KIND_EVENT,
    2U
  };
  PRT_VALUE P_GEND_VALUE_EVENT_eReqFailed = 
  {
    PRT_VALUE_KIND_EVENT,
    3U
  };
  PRT_VALUE P_GEND_VALUE_EVENT_eReqSuccessful = 
  {
    PRT_VALUE_KIND_EVENT,
    4U
  };
  PRT_VALUE P_GEND_VALUE_EVENT_eRequest = 
  {
    PRT_VALUE_KIND_EVENT,
    5U
  };
  PRT_VALUE P_GEND_VALUE_EVENT_eResponse = 
  {
    PRT_VALUE_KIND_EVENT,
    6U
  };
  PRT_UINT32 P_GEND_EVENTSET_0[] = 
  {
    0x0U
  };
  PRT_UINT32 P_GEND_EVENTSET_1[] = 
  {
    0x4U
  };
  PRT_UINT32 P_GEND_EVENTSET_2[] = 
  {
    0x18U
  };
  PRT_UINT32 P_GEND_EVENTSET_3[] = 
  {
    0x20U
  };
  PRT_UINT32 P_GEND_EVENTSET_4[] = 
  {
    0x40U
  };
  PRT_UINT32 P_GEND_EVENTSET_5[] = 
  {
    0x41U
  };
  PRT_EVENTSETDECL P_GEND_EVENTSETS[] = 
  {
    
    {
        P_GEND_EVENTSET_0
    },
    
    {
        P_GEND_EVENTSET_1
    },
    
    {
        P_GEND_EVENTSET_2
    },
    
    {
        P_GEND_EVENTSET_3
    },
    
    {
        P_GEND_EVENTSET_4
    },
    
    {
        P_GEND_EVENTSET_5
    }
  };
  PRT_EVENTSETDECL P_GEND_EVENTSET = 
  {
    P_GEND_EVENTSET_0
  };
  PRT_EVENTSETDECL P_GEND_EVENTSET_eProcessReq = 
  {
    P_GEND_EVENTSET_1
  };
  PRT_EVENTSETDECL P_GEND_EVENTSET_eReqSuccessful_eReqFailed = 
  {
    P_GEND_EVENTSET_2
  };
  PRT_EVENTSETDECL P_GEND_EVENTSET_eRequest = 
  {
    P_GEND_EVENTSET_3
  };
  PRT_EVENTSETDECL P_GEND_EVENTSET_eResponse = 
  {
    P_GEND_EVENTSET_4
  };
  PRT_EVENTSETDECL P_GEND_EVENTSET_null_eResponse = 
  {
    P_GEND_EVENTSET_5
  };
  PRT_EVENTSETDECL __AnonEventSet_ClientInterface = 
  {
    P_GEND_EVENTSET_4
  };
  PRT_EVENTSETDECL __AnonEventSet_HelperInterface = 
  {
    P_GEND_EVENTSET_1
  };
  PRT_EVENTSETDECL __AnonEventSet_ServerClientInterface = 
  {
    P_GEND_EVENTSET_3
  };
  PRT_EVENTSETDECL __AnonEventSet_ServerHelperInterface = 
  {
    P_GEND_EVENTSET_2
  };
  PRT_FUNDECL P_FUN_IGNORE_PUSH_STRUCT = 
  {
    0U,
    NULL,
    NULL,
    1U,
    0U,
    0U,
    NULL,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_UINT32 P_GEND_LINKMAP_ClientMachine[] = 
  {
    0U,
    1U,
    2U
  };
  PRT_UINT32 P_GEND_LINKMAP_HelperMachine[] = 
  {
    0U,
    1U,
    2U
  };
  PRT_UINT32 P_GEND_LINKMAP_ServerMachine[] = 
  {
    0U,
    1U,
    2U
  };
  PRT_UINT32 P_GEND_RENAMEMAP[] = 
  {
    0U,
    1U,
    4U
  };
  PRT_EVENTDECL *P_GEND_EVENTS[] = 
  {
    &_P_EVENT_NULL_STRUCT,
    &_P_EVENT_HALT_STRUCT,
    &P_EVENT_eProcessReq_STRUCT,
    &P_EVENT_eReqFailed_STRUCT,
    &P_EVENT_eReqSuccessful_STRUCT,
    &P_EVENT_eRequest_STRUCT,
    &P_EVENT_eResponse_STRUCT
  };
  PRT_FUNDECL *P_GEND_FUNS[] = 
  {
    &P_FUN_IGNORE_PUSH_STRUCT
  };
  PRT_MACHINEDECL *P_GEND_MACHINES[] = 
  {
    &P_MACHINE_ClientMachine_STRUCT,
    &P_MACHINE_HelperMachine_STRUCT,
    &P_MACHINE_ReqIdsAreMonotonicallyIncreasing_STRUCT,
    &P_MACHINE_ServerAbstractionMachine_STRUCT,
    &P_MACHINE_ServerMachine_STRUCT,
    &P_MACHINE_TestDriver_1Client1Server_STRUCT,
    &P_MACHINE_TestDriver_Refinement_STRUCT
  };
  PRT_UINT32 *P_GEND_LINKMAP[] = 
  {
    P_GEND_LINKMAP_ClientMachine,
    P_GEND_LINKMAP_HelperMachine,
    P_GEND_LINKMAP_ServerMachine
  };
  PRT_PROGRAMDECL P_GEND_PROGRAM = 
  {
    7U,
    6U,
    7U,
    1U,
    0U,
    P_GEND_EVENTS,
    P_GEND_EVENTSETS,
    P_GEND_MACHINES,
    P_GEND_FUNS,
    NULL,
    P_GEND_LINKMAP,
    P_GEND_RENAMEMAP,
    0U,
    NULL
  };
  