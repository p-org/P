
  #include "test.h"
  #define P_SEQ
  #define P_STMT_0(s, x0, f0) P_SEQ(p_tmp_stmt_0 = (x0), (s), ((f0) ? PrtFreeValue(p_tmp_stmt_0) : 0U))
  #define P_STMT_1(s, x1, f1, x0, f0) P_SEQ(p_tmp_stmt_0 = (x0), p_tmp_stmt_1 = (x1), (s), ((f0) ? PrtFreeValue(p_tmp_stmt_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_stmt_1) : 0U))
  #define P_STMT_2(s, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_stmt_0 = (x0), p_tmp_stmt_1 = (x1), p_tmp_stmt_2 = (x2), (s), ((f0) ? PrtFreeValue(p_tmp_stmt_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_stmt_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_stmt_2) : 0U))
  #define P_BOOL_EXPR(x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_bool = PrtPrimGetBool(p_tmp_expr_0), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), p_tmp_bool)
  #define P_EXPR_0(x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_0)
  #define P_EXPR_1(x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), p_tmp_expr_1)
  #define P_EXPR_2(x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), p_tmp_expr_2)
  #define P_EXPR_3(x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), p_tmp_expr_3)
  #define P_EXPR_4(x4, f4, x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), p_tmp_expr_4 = (x4), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), ((f3) ? PrtFreeValue(p_tmp_expr_3) : 0U), p_tmp_expr_4)
  PRT_TYPE P_GEND_TYPE_0 = 
  {
    PRT_KIND_ANY,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_1 = 
  {
    PRT_KIND_BOOL,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_2 = 
  {
    PRT_KIND_EVENT,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_3 = 
  {
    PRT_KIND_INT,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_4 = 
  {
    PRT_KIND_NULL,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_5 = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  PRT_MAPTYPE P_GEND_TYPE_MAP_6 = 
  {
    &P_GEND_TYPE_5,
    &P_GEND_TYPE_3
  };
  PRT_TYPE P_GEND_TYPE_6 = 
  {
    PRT_KIND_MAP,
    &P_GEND_TYPE_MAP_6
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_7[] = 
  {
    "_payload_0"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_7[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_7 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_7,
    P_GEND_TYPE_NMDTUP_TARR_7
  };
  PRT_TYPE P_GEND_TYPE_7 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_7
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_8[] = 
  {
    "_payload_1"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_8[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_8 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_8,
    P_GEND_TYPE_NMDTUP_TARR_8
  };
  PRT_TYPE P_GEND_TYPE_8 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_8
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_9[] = 
  {
    "_payload_2"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_9[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_9 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_9,
    P_GEND_TYPE_NMDTUP_TARR_9
  };
  PRT_TYPE P_GEND_TYPE_9 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_9
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_10[] = 
  {
    "_payload_3"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_10[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_10 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_10,
    P_GEND_TYPE_NMDTUP_TARR_10
  };
  PRT_TYPE P_GEND_TYPE_10 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_10
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_11[] = 
  {
    "_payload_skip"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_11[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_11 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_11,
    P_GEND_TYPE_NMDTUP_TARR_11
  };
  PRT_TYPE P_GEND_TYPE_11 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_11
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_12[] = 
  {
    "payload"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_12[] = 
  {
    &P_GEND_TYPE_5
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_12 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_12,
    P_GEND_TYPE_NMDTUP_TARR_12
  };
  PRT_TYPE P_GEND_TYPE_12 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_12
  };
  PRT_VALUE P_GEND_VALUE_0 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_CANCEL_FAILURE
  };
  PRT_VALUE P_GEND_VALUE_1 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_CANCEL_SUCCESS
  };
  PRT_VALUE P_GEND_VALUE_2 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_M_PING
  };
  PRT_VALUE P_GEND_VALUE_3 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_M_PONG
  };
  PRT_VALUE P_GEND_VALUE_4 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_PING
  };
  PRT_VALUE P_GEND_VALUE_5 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_PONG
  };
  PRT_VALUE P_GEND_VALUE_6 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_START
  };
  PRT_VALUE P_GEND_VALUE_7 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_SUCCESS
  };
  PRT_VALUE P_GEND_VALUE_8 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_TIMEOUT
  };
  PRT_VALUE P_GEND_VALUE_9 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_UNIT
  };
  PRT_VALUE P_GEND_VALUE_10 = 
  {
    PRT_VALKIND_INT,
    0U
  };
  PRT_VALUE P_GEND_VALUE_11 = 
  {
    PRT_VALKIND_INT,
    1U
  };
  PRT_VALUE P_GEND_VALUE_12 = 
  {
    PRT_VALKIND_INT,
    3U
  };
  PRT_VALUE P_GEND_VALUE_13 = 
  {
    PRT_VALKIND_INT,
    100U
  };
  PRT_VALUE P_GEND_VALUE_14 = 
  {
    PRT_VALKIND_EVENT,
    PRT_SPECIAL_EVENT_HALT
  };
  PRT_VALUE P_GEND_VALUE_15 = 
  {
    PRT_VALKIND_NULL,
    PRT_SPECIAL_EVENT_NULL
  };
  PRT_EVENTDECL P_GEND_EVENTS[] = 
  {
    
    {
        _P_EVENT_NULL,
        "null",
        0U,
        &P_GEND_TYPE_4,
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
        P_EVENT_CANCEL,
        "CANCEL",
        4294967295U,
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_EVENT_CANCEL_FAILURE,
        "CANCEL_FAILURE",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_CANCEL_SUCCESS,
        "CANCEL_SUCCESS",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_M_PING,
        "M_PING",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_M_PONG,
        "M_PONG",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_PING,
        "PING",
        1U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_PONG,
        "PONG",
        1U,
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_EVENT_START,
        "START",
        4294967295U,
        &P_GEND_TYPE_3,
        0U,
        NULL
    },
    
    {
        P_EVENT_SUCCESS,
        "SUCCESS",
        4294967295U,
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_EVENT_TIMEOUT,
        "TIMEOUT",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_UNIT,
        "UNIT",
        4294967295U,
        &P_GEND_TYPE_4,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_Client[] = 
  {
    
    {
        P_VAR_Client_server,
        P_MACHINE_Client,
        "server",
        &P_GEND_TYPE_5,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_Server[] = 
  {
    
    {
        P_VAR_Server_client,
        P_MACHINE_Server,
        "client",
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_VAR_Server_timer,
        P_MACHINE_Server,
        "timer",
        &P_GEND_TYPE_5,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Client_Init[] = 
  {
    
    {
        0,
        P_STATE_Client_Init,
        P_MACHINE_Client,
        P_EVENT_SUCCESS,
        P_STATE_Client_SendPing,
        P_FUN_Client_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Client_SendPing[] = 
  {
    
    {
        0,
        P_STATE_Client_SendPing,
        P_MACHINE_Client,
        P_EVENT_SUCCESS,
        P_STATE_Client_WaitPong,
        P_FUN_Client_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Client_WaitPong[] = 
  {
    
    {
        0,
        P_STATE_Client_WaitPong,
        P_MACHINE_Client,
        P_EVENT_PONG,
        P_STATE_Client_SendPing,
        P_FUN_Client_ANON0,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Server_SendPong[] = 
  {
    
    {
        0,
        P_STATE_Server_SendPong,
        P_MACHINE_Server,
        P_EVENT_SUCCESS,
        P_STATE_Server_WaitPing,
        P_FUN_Server_ANON1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Server_Sleep[] = 
  {
    
    {
        0,
        P_STATE_Server_Sleep,
        P_MACHINE_Server,
        P_EVENT_TIMEOUT,
        P_STATE_Server_SendPong,
        P_FUN_Server_ANON1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Server_WaitPing[] = 
  {
    
    {
        0,
        P_STATE_Server_WaitPing,
        P_MACHINE_Server,
        P_EVENT_PING,
        P_STATE_Server_Sleep,
        P_FUN_Server_ANON1,
        0U,
        NULL
    }
  };
  #pragma warning(disable:4101)
  #pragma warning(disable:4102)
  PRT_VALUE *P_FUN_Client_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_BOOLEAN p_tmp_bool;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_tuple;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_VALUE *p_tmp_params;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      p_tmp_params = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  PRT_VALUE *P_FUN_Client_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    #line 11 "file:///d:/git/p/src/prtdist/samples/pingpongsimple/test.p"
    {
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_BOOLEAN p_tmp_bool;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_tuple;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_VALUE *p_tmp_params;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      #line 11
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 11
      p_tmp_ret = NULL;
      #line 11
      p_tmp_params = NULL;
      #line 11
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 12
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Client_server, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(PrtCloneValue(PrtMkMachine(context->process, P_MACHINE_Server, p_tmp_expr_0)->id), PRT_TRUE, &P_GEND_VALUE_15, PRT_FALSE), PRT_FALSE);
      #line 13
      P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_15, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_7, PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 14
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 14
      return p_tmp_ret;
    }
  }

  PRT_VALUE *P_FUN_Client_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    #line 18 "file:///d:/git/p/src/prtdist/samples/pingpongsimple/test.p"
    {
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_BOOLEAN p_tmp_bool;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_tuple;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_VALUE *p_tmp_params;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      PRT_VALUE *p_tmp_stmt_2;
      PRT_VALUE *p_tmp_expr_0;
      #line 18
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 18
      p_tmp_ret = NULL;
      #line 18
      p_tmp_params = NULL;
      #line 18
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 20
      P_STMT_2(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_stmt_0), PrtSend(PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, p_tmp_stmt_2)), P_EXPR_0(p_tmp_mach_priv->id, PRT_FALSE), PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_4, PRT_FALSE), PRT_FALSE, P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_Client_server], PRT_FALSE), PRT_FALSE);
      #line 21
      P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_15, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_7, PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 22
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 22
      return p_tmp_ret;
    }
  }

  PRT_VALUE *P_FUN_Server_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    #line 35 "file:///d:/git/p/src/prtdist/samples/pingpongsimple/test.p"
    {
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_BOOLEAN p_tmp_bool;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_tuple;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_VALUE *p_tmp_params;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      #line 35
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 35
      p_tmp_ret = NULL;
      #line 35
      p_tmp_params = NULL;
      #line 35
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 36
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Server_timer, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(PrtCloneValue(PrtMkModel(context->process, P_MODEL_Timer, p_tmp_expr_0)->id), PRT_TRUE, p_tmp_mach_priv->id, PRT_FALSE), PRT_FALSE);
      P_EXIT_FUN:
      #line 37
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 37
      return p_tmp_ret;
    }
  }

  PRT_VALUE *P_FUN_Server_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_BOOLEAN p_tmp_bool;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_tuple;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_VALUE *p_tmp_params;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      p_tmp_params = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  PRT_VALUE *P_FUN_Server_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    #line 42 "file:///d:/git/p/src/prtdist/samples/pingpongsimple/test.p"
    {
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_BOOLEAN p_tmp_bool;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_tuple;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_VALUE *p_tmp_params;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      PRT_VALUE *p_tmp_stmt_2;
      PRT_VALUE *p_tmp_expr_0;
      #line 42
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 42
      p_tmp_ret = NULL;
      #line 42
      p_tmp_params = NULL;
      #line 42
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 43
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Server_client, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
      #line 44
      P_STMT_2(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_stmt_0), PrtSend(PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, p_tmp_stmt_2)), P_EXPR_0(&P_GEND_VALUE_13, PRT_FALSE), PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_6, PRT_FALSE), PRT_FALSE, P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_Server_timer], PRT_FALSE), PRT_FALSE);
      P_EXIT_FUN:
      #line 45
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 45
      return p_tmp_ret;
    }
  }

  PRT_VALUE *P_FUN_Server_ANON3_IMPL(PRT_MACHINEINST *context)
  {
    #line 50 "file:///d:/git/p/src/prtdist/samples/pingpongsimple/test.p"
    {
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_BOOLEAN p_tmp_bool;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_tuple;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_VALUE *p_tmp_params;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      PRT_VALUE *p_tmp_stmt_2;
      PRT_VALUE *p_tmp_expr_0;
      #line 50
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 50
      p_tmp_ret = NULL;
      #line 50
      p_tmp_params = NULL;
      #line 50
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 52
      P_STMT_2(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_stmt_0), PrtSend(PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, p_tmp_stmt_2)), &P_GEND_VALUE_15, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_5, PRT_FALSE), PRT_FALSE, P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_Server_client], PRT_FALSE), PRT_FALSE);
      #line 53
      P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_15, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_7, PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 54
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 54
      return p_tmp_ret;
    }
  }

  #pragma warning(default:4101)
  #pragma warning(default:4102)
  PRT_UINT32 P_GEND_EVENTSET_0[] = 
  {
    0x0U
  };
  PRT_UINT32 P_GEND_EVENTSET_1[] = 
  {
    0x080U
  };
  PRT_UINT32 P_GEND_EVENTSET_2[] = 
  {
    0x100U
  };
  PRT_UINT32 P_GEND_EVENTSET_3[] = 
  {
    0x400U
  };
  PRT_UINT32 P_GEND_EVENTSET_4[] = 
  {
    0x0800U
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
  PRT_FUNDECL P_GEND_FUNS_Client[] = 
  {
    
    {
        _P_FUN_PUSH_OR_IGN,
        P_MACHINE_Client,
        NULL,
        NULL,
        0U,
        0U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Client_ANON0,
        P_MACHINE_Client,
        NULL,
        &P_FUN_Client_ANON0_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Client_ANON1,
        P_MACHINE_Client,
        NULL,
        &P_FUN_Client_ANON1_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Client_ANON2,
        P_MACHINE_Client,
        NULL,
        &P_FUN_Client_ANON2_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    }
  };
  PRT_FUNDECL P_GEND_FUNS_Server[] = 
  {
    
    {
        _P_FUN_PUSH_OR_IGN,
        P_MACHINE_Server,
        NULL,
        NULL,
        0U,
        0U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Server_ANON0,
        P_MACHINE_Server,
        NULL,
        &P_FUN_Server_ANON0_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Server_ANON1,
        P_MACHINE_Server,
        NULL,
        &P_FUN_Server_ANON1_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Server_ANON2,
        P_MACHINE_Server,
        NULL,
        &P_FUN_Server_ANON2_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Server_ANON3,
        P_MACHINE_Server,
        NULL,
        &P_FUN_Server_ANON3_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_Client[] = 
  {
    
    {
        P_STATE_Client_Init,
        P_MACHINE_Client,
        "Init",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_Client_Init,
        NULL,
        P_FUN_Client_ANON1,
        P_FUN_Client_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_Client_SendPing,
        P_MACHINE_Client,
        "SendPing",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_Client_SendPing,
        NULL,
        P_FUN_Client_ANON2,
        P_FUN_Client_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_Client_WaitPong,
        P_MACHINE_Client,
        "WaitPong",
        1,
        0,
        0,
        2,
        0,
        P_GEND_TRANS_Client_WaitPong,
        NULL,
        P_FUN_Client_ANON0,
        P_FUN_Client_ANON0,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_Server[] = 
  {
    
    {
        P_STATE_Server_SendPong,
        P_MACHINE_Server,
        "SendPong",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_Server_SendPong,
        NULL,
        P_FUN_Server_ANON3,
        P_FUN_Server_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_Server_Sleep,
        P_MACHINE_Server,
        "Sleep",
        1,
        0,
        0,
        4,
        0,
        P_GEND_TRANS_Server_Sleep,
        NULL,
        P_FUN_Server_ANON2,
        P_FUN_Server_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_Server_WaitPing,
        P_MACHINE_Server,
        "WaitPing",
        1,
        0,
        0,
        1,
        0,
        P_GEND_TRANS_Server_WaitPing,
        NULL,
        P_FUN_Server_ANON0,
        P_FUN_Server_ANON1,
        0U,
        NULL
    }
  };
  PRT_MACHINEDECL P_GEND_MACHINES[] = 
  {
    
    {
        P_MACHINE_Client,
        "Client",
        1,
        3,
        4,
        4294967295,
        P_STATE_Client_Init,
        P_GEND_VARS_Client,
        P_GEND_STATES_Client,
        P_GEND_FUNS_Client,
        &P_CTOR_Client_IMPL,
        &P_DTOR_Client_IMPL,
        0U,
        NULL
    },
    
    {
        P_MACHINE_Server,
        "Server",
        2,
        3,
        5,
        4294967295,
        P_STATE_Server_WaitPing,
        P_GEND_VARS_Server,
        P_GEND_STATES_Server,
        P_GEND_FUNS_Server,
        &P_CTOR_Server_IMPL,
        &P_DTOR_Server_IMPL,
        0U,
        NULL
    }
  };
  PRT_MODELIMPLDECL P_GEND_MODELS[] = 
  {
    
    {
        P_MODEL_Timer,
        "Timer",
        &P_CTOR_Timer_IMPL,
        &P_SEND_Timer_IMPL,
        &P_DTOR_Timer_IMPL,
        0U,
        NULL
    }
  };
  PRT_PROGRAMDECL P_GEND_PROGRAM = 
  {
    13U,
    5U,
    2U,
    1U,
    P_GEND_EVENTS,
    P_GEND_EVENTSETS,
    P_GEND_MACHINES,
    P_GEND_MODELS,
    0U,
    NULL
  };
  