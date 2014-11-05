
  #include "program.h"
  PRT_TYPE P_GEND_TYPE_0 = 
  {
    PRT_KIND_ANY,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_1 = 
  {
    PRT_KIND_NULL,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_2 = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  PRT_EVENTDECL P_GEND_EVENTS[] = 
  {
    
    {
        _P_EVENT_DEFAULT,
        "default",
        0U,
        &P_GEND_TYPE_1,
        0U,
        NULL
    },
    
    {
        _P_EVENT_HALT,
        "halt",
        4294967295U,
        &P_GEND_TYPE_0,
        0U,
        NULL
    },
    
    {
        P_EVENT_Ping,
        "Ping",
        1U,
        &P_GEND_TYPE_2,
        0U,
        NULL
    },
    
    {
        P_EVENT_Pong,
        "Pong",
        1U,
        &P_GEND_TYPE_1,
        0U,
        NULL
    },
    
    {
        P_EVENT_Success,
        "Success",
        4294967295U,
        &P_GEND_TYPE_1,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_PING[] = 
  {
    
    {
        P_VAR_PING_pongId,
        P_MACHINE_PING,
        "pongId",
        &P_GEND_TYPE_2,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PING_Ping_Init[] = 
  {
    
    {
        0,
        P_STATE_PING_Ping_Init,
        P_MACHINE_PING,
        P_EVENT_Success,
        P_STATE_PING_Ping_SendPing,
        P_FUN_PING_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PING_Ping_SendPing[] = 
  {
    
    {
        0,
        P_STATE_PING_Ping_SendPing,
        P_MACHINE_PING,
        P_EVENT_Success,
        P_STATE_PING_Ping_WaitPong,
        P_FUN_PING_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PING_Ping_WaitPong[] = 
  {
    
    {
        0,
        P_STATE_PING_Ping_WaitPong,
        P_MACHINE_PING,
        P_EVENT_Pong,
        P_STATE_PING_Ping_SendPing,
        P_FUN_PING_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PONG_Pong_SendPong[] = 
  {
    
    {
        0,
        P_STATE_PONG_Pong_SendPong,
        P_MACHINE_PONG,
        P_EVENT_Success,
        P_STATE_PONG_Pong_WaitPing,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PONG_Pong_WaitPing[] = 
  {
    
    {
        0,
        P_STATE_PONG_Pong_WaitPing,
        P_MACHINE_PONG,
        P_EVENT_Ping,
        P_STATE_PONG_Pong_SendPong,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    }
  };
  void P_FUN_PING_ANON0_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
  }

  void P_FUN_PING_ANON1_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function in state Ping_Init
    PRT_SM_CONTEXT_PRIV *privContext = (PRT_SM_CONTEXT_PRIV *)context;
    privContext->varValues[P_VAR_PING_pongId] = PrtCloneValue(PrtMkMachine(privContext->process, P_MACHINE_PONG, PrtMkNullValue())->id);
    PrtRaise(privContext, PrtMkEventValue(P_EVENT_Success), PrtMkNullValue());
  }

  void P_FUN_PING_ANON2_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function in state Ping_SendPing
    PRT_SM_CONTEXT_PRIV *privContext = (PRT_SM_CONTEXT_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->varValues[P_VAR_PING_pongId]), PrtMkEventValue(P_EVENT_Ping), context->id);
    PrtRaise(privContext, PrtMkEventValue(P_EVENT_Success), PrtMkNullValue());
  }

  void P_FUN_PONG_ANON0_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
  }

  void P_FUN_PONG_ANON1_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function in state Pong_SendPong
    PRT_SM_CONTEXT_PRIV *privContext = (PRT_SM_CONTEXT_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->currentEvent.payload), PrtMkEventValue(P_EVENT_Pong), PrtMkNullValue());
    PrtRaise(privContext, PrtMkEventValue(P_EVENT_Success), PrtMkNullValue());
  }

  PRT_FUNDECL P_GEND_FUNS_PING[] = 
  {
    
    {
        _P_FUN_PING_PUSH_OR_IGN,
        P_MACHINE_PING,
        NULL,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_PING_ANON0,
        P_MACHINE_PING,
        NULL,
        &P_FUN_PING_ANON0_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_PING_ANON1,
        P_MACHINE_PING,
        NULL,
        &P_FUN_PING_ANON1_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_PING_ANON2,
        P_MACHINE_PING,
        NULL,
        &P_FUN_PING_ANON2_IMPL,
        0U,
        NULL
    }
  };
  PRT_FUNDECL P_GEND_FUNS_PONG[] = 
  {
    
    {
        _P_FUN_PONG_PUSH_OR_IGN,
        P_MACHINE_PONG,
        NULL,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_PONG_ANON0,
        P_MACHINE_PONG,
        NULL,
        &P_FUN_PONG_ANON0_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_PONG_ANON1,
        P_MACHINE_PONG,
        NULL,
        &P_FUN_PONG_ANON1_IMPL,
        0U,
        NULL
    }
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
    0x08U
  };
  PRT_UINT32 P_GEND_EVENTSET_3[] = 
  {
    0x10U
  };
  PRT_EVENTSETDECL P_GEND_EVENTSETS[] = 
  {
    
    {
        0,
        1,
        P_GEND_EVENTSET_0
    },
    
    {
        1,
        1,
        P_GEND_EVENTSET_1
    },
    
    {
        2,
        1,
        P_GEND_EVENTSET_2
    },
    
    {
        3,
        1,
        P_GEND_EVENTSET_3
    }
  };
  PRT_STATEDECL P_GEND_STATES_PING[] = 
  {
    
    {
        P_STATE_PING_Done,
        P_MACHINE_PING,
        "Done",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_PING_ANON0,
        P_FUN_PING_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PING_Ping_Init,
        P_MACHINE_PING,
        "Ping_Init",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_PING_Ping_Init,
        NULL,
        P_FUN_PING_ANON1,
        P_FUN_PING_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PING_Ping_SendPing,
        P_MACHINE_PING,
        "Ping_SendPing",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_PING_Ping_SendPing,
        NULL,
        P_FUN_PING_ANON2,
        P_FUN_PING_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PING_Ping_WaitPong,
        P_MACHINE_PING,
        "Ping_WaitPong",
        1,
        0,
        0,
        2,
        0,
        P_GEND_TRANS_PING_Ping_WaitPong,
        NULL,
        P_FUN_PING_ANON0,
        P_FUN_PING_ANON0,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_PONG[] = 
  {
    
    {
        P_STATE_PONG_Pong_SendPong,
        P_MACHINE_PONG,
        "Pong_SendPong",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_PONG_Pong_SendPong,
        NULL,
        P_FUN_PONG_ANON1,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PONG_Pong_WaitPing,
        P_MACHINE_PONG,
        "Pong_WaitPing",
        1,
        0,
        0,
        1,
        0,
        P_GEND_TRANS_PONG_Pong_WaitPing,
        NULL,
        P_FUN_PONG_ANON0,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    }
  };
  PRT_MACHINEDECL P_GEND_MACHINES[] = 
  {
    
    {
        P_MACHINE_PING,
        "PING",
        1,
        4,
        4,
        4294967295,
        P_STATE_PING_Ping_Init,
        P_GEND_VARS_PING,
        P_GEND_STATES_PING,
        P_GEND_FUNS_PING,
        &P_CTOR_PING_IMPL,
        &P_DTOR_PING_IMPL,
        0U,
        NULL
    },
    
    {
        P_MACHINE_PONG,
        "PONG",
        0,
        2,
        3,
        4294967295,
        P_STATE_PONG_Pong_WaitPing,
        NULL,
        P_GEND_STATES_PONG,
        P_GEND_FUNS_PONG,
        &P_CTOR_PONG_IMPL,
        &P_DTOR_PONG_IMPL,
        0U,
        NULL
    }
  };
  PRT_PROGRAMDECL P_GEND_PROGRAM = 
  {
    5U,
    4U,
    2U,
    0U,
    P_GEND_EVENTS,
    P_GEND_EVENTSETS,
    P_GEND_MACHINES,
    NULL,
    0U,
    NULL
  };
  
