
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
        P_EVENT_Ding,
        "Ding",
        1U,
        &P_GEND_TYPE_1,
        0U,
        NULL
    },
    
    {
        P_EVENT_Dong,
        "Dong",
        1U,
        &P_GEND_TYPE_1,
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
        1U,
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
  PRT_VARDECL P_GEND_VARS_PONG[] = 
  {
    
    {
        P_VAR_PONG_pingid,
        P_MACHINE_PONG,
        "pingid",
        &P_GEND_TYPE_2,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PING_Ping_ping1[] = 
  {
    
    {
        0,
        P_STATE_PING_Ping_ping1,
        P_MACHINE_PING,
        P_EVENT_Dong,
        P_STATE_PING_Ping_ping2,
        P_FUN_PING_ANON0,
        0U,
        NULL
    },
    
    {
        1,
        P_STATE_PING_Ping_ping1,
        P_MACHINE_PING,
        P_EVENT_Success,
        P_STATE_PING_Ping_ding1,
        _P_FUN_PING_PUSH_OR_IGN,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PING_Ping_ping2[] = 
  {
    
    {
        0,
        P_STATE_PING_Ping_ping2,
        P_MACHINE_PING,
        P_EVENT_Pong,
        P_STATE_PING_Ping_ping1,
        P_FUN_PING_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PING_Ping_start[] = 
  {
    
    {
        0,
        P_STATE_PING_Ping_start,
        P_MACHINE_PING,
        P_EVENT_Success,
        P_STATE_PING_Ping_ping1,
        P_FUN_PING_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PONG_Pong_dong1[] = 
  {
    
    {
        0,
        P_STATE_PONG_Pong_dong1,
        P_MACHINE_PONG,
        P_EVENT_Ding,
        P_STATE_PONG_Pong_dong2,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PONG_Pong_pong1[] = 
  {
    
    {
        0,
        P_STATE_PONG_Pong_pong1,
        P_MACHINE_PONG,
        P_EVENT_Success,
        P_STATE_PONG_Pong_start,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PONG_Pong_start[] = 
  {
    
    {
        0,
        P_STATE_PONG_Pong_start,
        P_MACHINE_PONG,
        P_EVENT_Ping,
        P_STATE_PONG_Pong_dong1,
        _P_FUN_PONG_PUSH_OR_IGN,
        0U,
        NULL
    },
    
    {
        1,
        P_STATE_PONG_Pong_start,
        P_MACHINE_PONG,
        P_EVENT_Success,
        P_STATE_PONG_Pong_pong1,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PONG__Init[] = 
  {
    
    {
        0,
        P_STATE_PONG__Init,
        P_MACHINE_PONG,
        P_EVENT_Success,
        P_STATE_PONG_Pong_start,
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
    // entry function for Ping_ding1
    PRT_SM_CONTEXT_PRIV *privContext = (PRT_SM_CONTEXT_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->varValues[P_VAR_PING_pongId]), PrtMkEventValue(P_EVENT_Ding), PrtMkNullValue());
  }

  void P_FUN_PING_ANON2_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function for Ping_start
    PRT_SM_CONTEXT_PRIV *privContext = (PRT_SM_CONTEXT_PRIV *)context;
    privContext->varValues[P_VAR_PING_pongId] = PrtCloneValue(PrtMkMachine(privContext->process, P_MACHINE_PONG, context->id)->id);
    PrtRaise(privContext, PrtMkEventValue(P_EVENT_Success), PrtMkNullValue());
  }

  void P_FUN_PING_ANON3_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function for Ping_ping1
    PRT_SM_CONTEXT_PRIV *privContext = (PRT_SM_CONTEXT_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->varValues[P_VAR_PING_pongId]), PrtMkEventValue(P_EVENT_Ping), context->id);
    PrtRaise(privContext, PrtMkEventValue(P_EVENT_Success), PrtMkNullValue());
  }

  void P_FUN_PONG_ANON0_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
  }

  void P_FUN_PONG_ANON1_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function for _Init
    PRT_SM_CONTEXT_PRIV *privContext = (PRT_SM_CONTEXT_PRIV *)context;
    privContext->varValues[P_VAR_PONG_pingid] = PrtCloneValue(privContext->currentEvent.payload);
    PrtRaise(privContext, PrtMkEventValue(P_EVENT_Success), PrtMkNullValue());
  }

  void P_FUN_PONG_ANON2_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function for Pong_dong2
    PRT_SM_CONTEXT_PRIV *privContext = (PRT_SM_CONTEXT_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->varValues[P_VAR_PONG_pingid]), PrtMkEventValue(P_EVENT_Dong), PrtMkNullValue());
    PrtRaise(privContext, PrtMkEventValue(P_EVENT_Success), PrtMkNullValue());
  }

  void P_FUN_PONG_ANON3_IMPL(PRT_SM_CONTEXT *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function for Pong_pong1
    PRT_SM_CONTEXT_PRIV *privContext = (PRT_SM_CONTEXT_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->varValues[P_VAR_PONG_pingid]), PrtMkEventValue(P_EVENT_Pong), PrtMkNullValue());
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
    },
    
    {
        P_FUN_PING_ANON3,
        P_MACHINE_PING,
        NULL,
        &P_FUN_PING_ANON3_IMPL,
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
    },
    
    {
        P_FUN_PONG_ANON2,
        P_MACHINE_PONG,
        NULL,
        &P_FUN_PONG_ANON2_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_PONG_ANON3,
        P_MACHINE_PONG,
        NULL,
        &P_FUN_PONG_ANON3_IMPL,
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
    0x20U
  };
  PRT_UINT32 P_GEND_EVENTSET_3[] = 
  {
    0x40U
  };
  PRT_UINT32 P_GEND_EVENTSET_4[] = 
  {
    0x48U
  };
  PRT_UINT32 P_GEND_EVENTSET_5[] = 
  {
    0x50U
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
    },
    
    {
        4,
        1,
        P_GEND_EVENTSET_4
    },
    
    {
        5,
        1,
        P_GEND_EVENTSET_5
    }
  };
  PRT_STATEDECL P_GEND_STATES_PING[] = 
  {
    
    {
        P_STATE_PING_Ping_ding1,
        P_MACHINE_PING,
        "Ping_ding1",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_PING_ANON1,
        P_FUN_PING_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PING_Ping_ping1,
        P_MACHINE_PING,
        "Ping_ping1",
        2,
        0,
        0,
        4,
        0,
        P_GEND_TRANS_PING_Ping_ping1,
        NULL,
        P_FUN_PING_ANON3,
        P_FUN_PING_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PING_Ping_ping2,
        P_MACHINE_PING,
        "Ping_ping2",
        1,
        0,
        0,
        2,
        0,
        P_GEND_TRANS_PING_Ping_ping2,
        NULL,
        P_FUN_PING_ANON0,
        P_FUN_PING_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PING_Ping_start,
        P_MACHINE_PING,
        "Ping_start",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_PING_Ping_start,
        NULL,
        P_FUN_PING_ANON2,
        P_FUN_PING_ANON0,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_PONG[] = 
  {
    
    {
        P_STATE_PONG_Pong_dong1,
        P_MACHINE_PONG,
        "Pong_dong1",
        1,
        0,
        0,
        1,
        0,
        P_GEND_TRANS_PONG_Pong_dong1,
        NULL,
        P_FUN_PONG_ANON0,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PONG_Pong_dong2,
        P_MACHINE_PONG,
        "Pong_dong2",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_PONG_ANON2,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PONG_Pong_pong1,
        P_MACHINE_PONG,
        "Pong_pong1",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_PONG_Pong_pong1,
        NULL,
        P_FUN_PONG_ANON3,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PONG_Pong_start,
        P_MACHINE_PONG,
        "Pong_start",
        2,
        0,
        0,
        5,
        0,
        P_GEND_TRANS_PONG_Pong_start,
        NULL,
        P_FUN_PONG_ANON0,
        P_FUN_PONG_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_PONG__Init,
        P_MACHINE_PONG,
        "_Init",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_PONG__Init,
        NULL,
        P_FUN_PONG_ANON1,
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
        5,
        4294967295,
        P_STATE_PING_Ping_start,
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
        1,
        5,
        5,
        4294967295,
        P_STATE_PONG__Init,
        P_GEND_VARS_PONG,
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
    7U,
    6U,
    2U,
    0U,
    P_GEND_EVENTS,
    P_GEND_EVENTSETS,
    P_GEND_MACHINES,
    NULL,
    0U,
    NULL
  };
  
