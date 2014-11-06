
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
        P_EVENT_E1,
        "E1",
        1U,
        &P_GEND_TYPE_1,
        0U,
        NULL
    },
    
    {
        P_EVENT_E2,
        "E2",
        1U,
        &P_GEND_TYPE_1,
        0U,
        NULL
    },
    
    {
        P_EVENT_E3,
        "E3",
        1U,
        &P_GEND_TYPE_1,
        0U,
        NULL
    },
    
    {
        P_EVENT_E4,
        "E4",
        1U,
        &P_GEND_TYPE_1,
        0U,
        NULL
    },
    
    {
        P_EVENT_unit,
        "unit",
        1U,
        &P_GEND_TYPE_1,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_Ghost[] = 
  {
    
    {
        P_VAR_Ghost_real_machine,
        P_MACHINE_Ghost,
        "real_machine",
        &P_GEND_TYPE_2,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_Real[] = 
  {
    
    {
        P_VAR_Real_ghost_machine,
        P_MACHINE_Real,
        "ghost_machine",
        &P_GEND_TYPE_2,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Ghost_Ghost_Init[] = 
  {
    
    {
        0,
        P_STATE_Ghost_Ghost_Init,
        P_MACHINE_Ghost,
        P_EVENT_E1,
        P_STATE_Ghost_Ghost_S1,
        P_FUN_Ghost_ANON1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Ghost_Ghost_S1[] = 
  {
    
    {
        0,
        P_STATE_Ghost_Ghost_S1,
        P_MACHINE_Ghost,
        P_EVENT_E3,
        P_STATE_Ghost_Ghost_S2,
        P_FUN_Ghost_ANON1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Real_Real_Init[] = 
  {
    
    {
        0,
        P_STATE_Real_Real_Init,
        P_MACHINE_Real,
        P_EVENT_E4,
        P_STATE_Real_Real_S2,
        P_FUN_Real_ANON0,
        0U,
        NULL
    },
    
    {
        1,
        P_STATE_Real_Real_Init,
        P_MACHINE_Real,
        P_EVENT_unit,
        P_STATE_Real_Real_S1,
        _P_FUN_Real_PUSH_OR_IGN,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_Real_Real_Init[] = 
  {
    
    {
        0,
        P_STATE_Real_Real_Init,
        P_MACHINE_Real,
        P_EVENT_E2,
        P_FUN_Real_Action1,
        0U,
        NULL
    }
  };
  void P_FUN_Ghost_ANON0_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function of Ghost_Init
    PRT_MACHINEINST_PRIV *privContext = (PRT_MACHINEINST_PRIV *)context;
    privContext->varValues[P_VAR_Ghost_real_machine] = PrtCloneValue(privContext->currentEvent.payload);
  }

  void P_FUN_Ghost_ANON1_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // exit function of Ghost_Init/Ghost_S1/Ghost_S2
  }

  void P_FUN_Ghost_ANON2_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function of Ghost_S1
    PRT_MACHINEINST_PRIV *privContext = (PRT_MACHINEINST_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->varValues[P_VAR_Ghost_real_machine]), PrtMkEventValue(P_EVENT_E2), PrtMkNullValue());
  }

  void P_FUN_Ghost_ANON3_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function of Ghost_S2
    PRT_MACHINEINST_PRIV *privContext = (PRT_MACHINEINST_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->varValues[P_VAR_Ghost_real_machine]), PrtMkEventValue(P_EVENT_E4), PrtMkNullValue());
  }

  void P_FUN_Real_ANON0_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // exit function of Real_Init/Real_S1/Real_S2
  }

  void P_FUN_Real_ANON1_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function of Real_S1
    PRT_MACHINEINST_PRIV *privContext = (PRT_MACHINEINST_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->varValues[P_VAR_Real_ghost_machine]), PrtMkEventValue(P_EVENT_E1), PrtMkNullValue());
  }

  void P_FUN_Real_ANON2_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function of Real_Init
    PRT_MACHINEINST_PRIV *privContext = (PRT_MACHINEINST_PRIV *)context;
    privContext->varValues[P_VAR_Real_ghost_machine] = PrtCloneValue(PrtMkMachine(privContext->process, P_MACHINE_Ghost, context->id)->id);
    PrtRaise(privContext, PrtMkEventValue(P_EVENT_unit), PrtMkNullValue());
  }

  void P_FUN_Real_ANON3_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    // entry function of Real_S2
    PRT_MACHINEINST_PRIV *privContext = (PRT_MACHINEINST_PRIV *)context;
    PrtAssert(PRT_FALSE, "Test succeeded");
  }

  void P_FUN_Real_Action1_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *privContext = (PRT_MACHINEINST_PRIV *)context;
    PrtSend(PrtGetMachine(privContext->process, privContext->varValues[P_VAR_Real_ghost_machine]), PrtMkEventValue(P_EVENT_E3), PrtMkNullValue());
  }

  PRT_FUNDECL P_GEND_FUNS_Ghost[] = 
  {
    
    {
        _P_FUN_Ghost_PUSH_OR_IGN,
        P_MACHINE_Ghost,
        NULL,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Ghost_ANON0,
        P_MACHINE_Ghost,
        NULL,
        &P_FUN_Ghost_ANON0_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Ghost_ANON1,
        P_MACHINE_Ghost,
        NULL,
        &P_FUN_Ghost_ANON1_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Ghost_ANON2,
        P_MACHINE_Ghost,
        NULL,
        &P_FUN_Ghost_ANON2_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Ghost_ANON3,
        P_MACHINE_Ghost,
        NULL,
        &P_FUN_Ghost_ANON3_IMPL,
        0U,
        NULL
    }
  };
  PRT_FUNDECL P_GEND_FUNS_Real[] = 
  {
    
    {
        _P_FUN_Real_PUSH_OR_IGN,
        P_MACHINE_Real,
        NULL,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Real_ANON0,
        P_MACHINE_Real,
        NULL,
        &P_FUN_Real_ANON0_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Real_ANON1,
        P_MACHINE_Real,
        NULL,
        &P_FUN_Real_ANON1_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Real_ANON2,
        P_MACHINE_Real,
        NULL,
        &P_FUN_Real_ANON2_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Real_ANON3,
        P_MACHINE_Real,
        NULL,
        &P_FUN_Real_ANON3_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Real_Action1,
        P_MACHINE_Real,
        "Action1",
        &P_FUN_Real_Action1_IMPL,
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
  PRT_UINT32 P_GEND_EVENTSET_4[] = 
  {
    0x60U
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
    }
  };
  PRT_STATEDECL P_GEND_STATES_Ghost[] = 
  {
    
    {
        P_STATE_Ghost_Ghost_Init,
        P_MACHINE_Ghost,
        "Ghost_Init",
        1,
        0,
        0,
        1,
        0,
        P_GEND_TRANS_Ghost_Ghost_Init,
        NULL,
        P_FUN_Ghost_ANON0,
        P_FUN_Ghost_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_Ghost_Ghost_S1,
        P_MACHINE_Ghost,
        "Ghost_S1",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_Ghost_Ghost_S1,
        NULL,
        P_FUN_Ghost_ANON2,
        P_FUN_Ghost_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_Ghost_Ghost_S2,
        P_MACHINE_Ghost,
        "Ghost_S2",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_Ghost_ANON3,
        P_FUN_Ghost_ANON1,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_Real[] = 
  {
    
    {
        P_STATE_Real_Real_Init,
        P_MACHINE_Real,
        "Real_Init",
        2,
        1,
        0,
        4,
        2,
        P_GEND_TRANS_Real_Real_Init,
        P_GEND_DOS_Real_Real_Init,
        P_FUN_Real_ANON2,
        P_FUN_Real_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_Real_Real_S1,
        P_MACHINE_Real,
        "Real_S1",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_Real_ANON1,
        P_FUN_Real_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_Real_Real_S2,
        P_MACHINE_Real,
        "Real_S2",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_Real_ANON3,
        P_FUN_Real_ANON0,
        0U,
        NULL
    }
  };
  PRT_MACHINEDECL P_GEND_MACHINES[] = 
  {
    
    {
        P_MACHINE_Ghost,
        "Ghost",
        1,
        3,
        5,
        4294967295,
        P_STATE_Ghost_Ghost_Init,
        P_GEND_VARS_Ghost,
        P_GEND_STATES_Ghost,
        P_GEND_FUNS_Ghost,
        &P_CTOR_Ghost_IMPL,
        &P_DTOR_Ghost_IMPL,
        0U,
        NULL
    },
    
    {
        P_MACHINE_Real,
        "Real",
        1,
        3,
        6,
        4294967295,
        P_STATE_Real_Real_Init,
        P_GEND_VARS_Real,
        P_GEND_STATES_Real,
        P_GEND_FUNS_Real,
        &P_CTOR_Real_IMPL,
        &P_DTOR_Real_IMPL,
        0U,
        NULL
    }
  };
  PRT_PROGRAMDECL P_GEND_PROGRAM = 
  {
    7U,
    5U,
    2U,
    0U,
    P_GEND_EVENTS,
    P_GEND_EVENTSETS,
    P_GEND_MACHINES,
    NULL,
    0U,
    NULL
  };
  
