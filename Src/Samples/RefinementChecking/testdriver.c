
  #include "testdriver.h"
  #define P_SEQ
  #define P_STMT_0(s, x0, f0) P_SEQ(p_tmp_stmt_0 = (x0), (s), ((f0) ? PrtFreeValue(p_tmp_stmt_0) : 0U))
  #define P_STMT_1(s, x1, f1, x0, f0) P_SEQ(p_tmp_stmt_0 = (x0), p_tmp_stmt_1 = (x1), (s), ((f0) ? PrtFreeValue(p_tmp_stmt_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_stmt_1) : 0U))
  #define P_BOOL_EXPR(x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_bool = PrtPrimGetBool(p_tmp_expr_0), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), p_tmp_bool)
  #define P_EXPR_0(x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_0)
  #define P_EXPR_1(x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), p_tmp_expr_1)
  #define P_EXPR_2(x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), p_tmp_expr_2)
  #define P_EXPR_3(x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), p_tmp_expr_3)
  #define P_EXPR_4(x4, f4, x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), p_tmp_expr_4 = (x4), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), ((f3) ? PrtFreeValue(p_tmp_expr_3) : 0U), p_tmp_expr_4)
  #define P_EXPR_5(x5, f5, x4, f4, x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), p_tmp_expr_4 = (x4), p_tmp_expr_5 = (x5), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), ((f3) ? PrtFreeValue(p_tmp_expr_3) : 0U), ((f4) ? PrtFreeValue(p_tmp_expr_4) : 0U), p_tmp_expr_5)
  #define P_EXPR_6(x6, f6, x5, f5, x4, f4, x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), p_tmp_expr_4 = (x4), p_tmp_expr_5 = (x5), p_tmp_expr_6 = (x6), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), ((f3) ? PrtFreeValue(p_tmp_expr_3) : 0U), ((f4) ? PrtFreeValue(p_tmp_expr_4) : 0U), ((f5) ? PrtFreeValue(p_tmp_expr_5) : 0U), p_tmp_expr_6)
  #define P_TUPLE_0(t, x0) P_SEQ(p_tmp_tuple = PrtMkDefaultValue(t), PrtTupleSet(p_tmp_tuple, 0U, (x0)), p_tmp_tuple)
  #define P_TUPLE_1(t, x0, x1) P_SEQ(p_tmp_tuple = PrtMkDefaultValue(t), PrtTupleSet(p_tmp_tuple, 0U, (x0)), PrtTupleSet(p_tmp_tuple, 1U, (x1)), p_tmp_tuple)
  static PRT_TYPE P_GEND_TYPE_0 = 
  {
    PRT_KIND_BOOL,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_1 = 
  {
    PRT_KIND_EVENT,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_2 = 
  {
    PRT_KIND_INT,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_3 = 
  {
    PRT_KIND_MACHINE,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_4 = 
  {
    PRT_KIND_NULL,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_5 = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  static PRT_TYPE P_GEND_TYPE_6 = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  static PRT_TYPE P_GEND_TYPE_7 = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  static PRT_TYPE P_GEND_TYPE_8 = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_9[] = 
  {
    "index"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_9[] = 
  {
    &P_GEND_TYPE_2
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_9 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_9,
    P_GEND_TYPE_NMDTUP_TARR_9
  };
  static PRT_TYPE P_GEND_TYPE_9 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_9
    }
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_10[] = 
  {
    "server"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_10[] = 
  {
    &P_GEND_TYPE_7
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_10 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_10,
    P_GEND_TYPE_NMDTUP_TARR_10
  };
  static PRT_TYPE P_GEND_TYPE_10 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_10
    }
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_11[] = 
  {
    "successful"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_11[] = 
  {
    &P_GEND_TYPE_0
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_11 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_11,
    P_GEND_TYPE_NMDTUP_TARR_11
  };
  static PRT_TYPE P_GEND_TYPE_11 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_11
    }
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_12[] = 
  {
    "id",
    "success"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_12[] = 
  {
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_0
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_12 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_12,
    P_GEND_TYPE_NMDTUP_TARR_12
  };
  static PRT_TYPE P_GEND_TYPE_12 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_12
    }
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_13[] = 
  {
    "source",
    "id"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_13[] = 
  {
    &P_GEND_TYPE_8,
    &P_GEND_TYPE_2
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_13 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_13,
    P_GEND_TYPE_NMDTUP_TARR_13
  };
  static PRT_TYPE P_GEND_TYPE_13 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_13
    }
  };
  static PRT_VALUE P_GEND_VALUE_0 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        0U
    }
  };
  static PRT_VALUE P_GEND_VALUE_1 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        1U
    }
  };
  static PRT_VALUE P_GEND_VALUE_2 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        2U
    }
  };
  static PRT_VALUE P_GEND_VALUE_3 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        4U
    }
  };
  static PRT_VALUE P_GEND_VALUE_4 = 
  {
    PRT_VALUE_KIND_BOOL,
    
    {
        PRT_FALSE
    }
  };
  static PRT_VALUE P_GEND_VALUE_5 = 
  {
    PRT_VALUE_KIND_BOOL,
    
    {
        PRT_TRUE
    }
  };
  PRT_EVENTDECL P_EVENT_eProcessReq_STRUCT = 
  {
    0U,
    "eProcessReq",
    4294967295U,
    &P_GEND_TYPE_2,
    0U,
    NULL
  };
  PRT_EVENTDECL P_EVENT_eReqFailed_STRUCT = 
  {
    0U,
    "eReqFailed",
    4294967295U,
    &P_GEND_TYPE_4,
    0U,
    NULL
  };
  PRT_EVENTDECL P_EVENT_eReqSuccessful_STRUCT = 
  {
    0U,
    "eReqSuccessful",
    4294967295U,
    &P_GEND_TYPE_4,
    0U,
    NULL
  };
  PRT_EVENTDECL P_EVENT_eRequest_STRUCT = 
  {
    0U,
    "eRequest",
    4294967295U,
    &P_GEND_TYPE_13,
    0U,
    NULL
  };
  PRT_EVENTDECL P_EVENT_eResponse_STRUCT = 
  {
    0U,
    "eResponse",
    4294967295U,
    &P_GEND_TYPE_12,
    0U,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_ClientInterface = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_HelperInterface = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_ServerClientInterface = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_ServerHelperInterface = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_requestType = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_13
    }
  };
  PRT_TYPE P_GEND_TYPE_responseType = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_12
    }
  };
  PRT_VARDECL P_GEND_VARS_ClientMachine[] = 
  {
    
    {
        "lastRecvSuccessfulReqId",
        &P_GEND_TYPE_2,
        0U,
        NULL
    },
    
    {
        "nextReqId",
        &P_GEND_TYPE_2,
        0U,
        NULL
    },
    
    {
        "server",
        &P_GEND_TYPE_7,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_HelperMachine[] = 
  {
    
    {
        "server",
        &P_GEND_TYPE_6,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_ReqIdsAreMonotonicallyIncreasing[] = 
  {
    
    {
        "previousId",
        &P_GEND_TYPE_2,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_ServerMachine[] = 
  {
    
    {
        "helper",
        &P_GEND_TYPE_5,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_TestDriver_Refinement[] = 
  {
    
    {
        "nextReqId",
        &P_GEND_TYPE_2,
        0U,
        NULL
    },
    
    {
        "server",
        &P_GEND_TYPE_7,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_ClientMachine_StartPumpingRequests[] = 
  {
    
    {
        P_STATE_ClientMachine_StartPumpingRequests,
        &P_EVENT_eResponse_STRUCT,
        &P_FUN_ClientMachine_ANON4_STRUCT,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_HelperMachine_Init[] = 
  {
    
    {
        P_STATE_HelperMachine_Init,
        &P_EVENT_eProcessReq_STRUCT,
        &P_FUN_HelperMachine_ANON1_STRUCT,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_ReqIdsAreMonotonicallyIncreasing_Init[] = 
  {
    
    {
        P_STATE_ReqIdsAreMonotonicallyIncreasing_Init,
        &P_EVENT_eRequest_STRUCT,
        &P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON2_STRUCT,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_ServerAbstractionMachine_Init[] = 
  {
    
    {
        P_STATE_ServerAbstractionMachine_Init,
        &P_EVENT_eRequest_STRUCT,
        &P_FUN_ServerAbstractionMachine_ANON2_STRUCT,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_ServerMachine_WaitForRequests[] = 
  {
    
    {
        P_STATE_ServerMachine_WaitForRequests,
        &P_EVENT_eRequest_STRUCT,
        &P_FUN_ServerMachine_ANON7_STRUCT,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_TestDriver_Refinement_StartPumpingRequests[] = 
  {
    
    {
        P_STATE_TestDriver_Refinement_StartPumpingRequests,
        &P_EVENT_eResponse_STRUCT,
        &P_FUN_IGNORE_PUSH_STRUCT,
        0U,
        NULL
    },
    
    {
        P_STATE_TestDriver_Refinement_StartPumpingRequests,
        &_P_EVENT_NULL_STRUCT,
        &P_FUN_TestDriver_Refinement_ANON5_STRUCT,
        0U,
        NULL
    }
  };
  static PRT_VALUE *P_FUN_ClientMachine_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ClientMachine_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ClientMachine_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ClientMachine_ANON3_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ClientMachine_ANON4_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      if (!P_BOOL_EXPR(P_EXPR_3(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_2) > PrtPrimGetInt(p_tmp_expr_0)), PRT_TRUE, PrtTupleGetNC(p_tmp_expr_1, 0), PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_ClientMachine_lastRecvSuccessfulReqId], PRT_FALSE), PRT_TRUE))
      {
        PrtHandleError(PRT_STATUS_ASSERT, p_tmp_mach_priv);
      }
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_ClientMachine_lastRecvSuccessfulReqId, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_1(PrtTupleGetNC(p_tmp_expr_0, 0), PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ClientMachine_ANON5_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_ClientMachine_nextReqId, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_1, PRT_FALSE), PRT_FALSE);
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_ClientMachine_server, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
      PrtGoto(p_tmp_mach_priv, P_STATE_ClientMachine_StartPumpingRequests, 0U);
      goto P_EXIT_FUN;
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ClientMachine_ANON6_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_event;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_expr_5;
      PRT_VALUE *p_tmp_machine;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_tuple;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 1U, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_0, PRT_FALSE), PRT_FALSE);
      while (P_BOOL_EXPR(P_EXPR_2(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_0) < PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_2, PRT_FALSE, p_tmp_frame.locals[1U], PRT_FALSE), PRT_TRUE))
      {
        P_SEQ(p_tmp_machine = P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_ClientMachine_server], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_EVENT_eRequest, PRT_FALSE), P_EXPR_5(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_4), PrtSendInternal(context, PrtGetMachine(context->process, p_tmp_expr_4), p_tmp_expr_3, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_2), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, P_TUPLE_1(&P_GEND_TYPE_13, p_tmp_expr_1, p_tmp_expr_0), PRT_TRUE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_ClientMachine_nextReqId], PRT_FALSE));
        P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_ClientMachine_nextReqId, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_1, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_ClientMachine_nextReqId], PRT_FALSE), PRT_FALSE);
        P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 1U, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_1, PRT_FALSE, p_tmp_frame.locals[1U], PRT_FALSE), PRT_FALSE);
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_HelperMachine_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_HelperMachine_server, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_HelperMachine_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      if (P_BOOL_EXPR(P_EXPR_0(PrtMkNondetBoolValue(), PRT_TRUE), PRT_TRUE))
      {
        P_STMT_1(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_stmt_0), PrtSendInternal(context, PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, 0U)), P_EXPR_0(&P_GEND_VALUE_EVENT_eReqSuccessful, PRT_FALSE), PRT_FALSE, P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_HelperMachine_server], PRT_FALSE), PRT_FALSE);
      }
      else
      {
        P_STMT_1(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_stmt_0), PrtSendInternal(context, PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, 0U)), P_EXPR_0(&P_GEND_VALUE_EVENT_eReqFailed, PRT_FALSE), PRT_FALSE, P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_HelperMachine_server], PRT_FALSE), PRT_FALSE);
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_HelperMachine_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_HelperMachine_ANON3_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      if (!P_BOOL_EXPR(P_EXPR_3(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_2) > PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, PrtTupleGetNC(p_tmp_expr_0, 1), PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_ReqIdsAreMonotonicallyIncreasing_previousId], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_TRUE))
      {
        PrtHandleError(PRT_STATUS_ASSERT, p_tmp_mach_priv);
      }
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_ReqIdsAreMonotonicallyIncreasing_previousId, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_1(PrtTupleGetNC(p_tmp_expr_0, 1), PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerAbstractionMachine_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerAbstractionMachine_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerAbstractionMachine_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_event;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_expr_5;
      PRT_VALUE *p_tmp_expr_6;
      PRT_VALUE *p_tmp_machine;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_tuple;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 1U, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_0(PrtMkNondetBoolValue(), PRT_TRUE), PRT_FALSE);
      P_SEQ(p_tmp_machine = P_EXPR_1(PrtTupleGetNC(p_tmp_expr_0, 0), PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_EVENT_eResponse, PRT_FALSE), P_EXPR_6(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_5), PrtSendInternal(context, PrtGetMachine(context->process, p_tmp_expr_5), p_tmp_expr_4, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_3), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, P_TUPLE_1(&P_GEND_TYPE_12, p_tmp_expr_2, p_tmp_expr_1), PRT_TRUE, PrtTupleGetNC(p_tmp_expr_0, 1), PRT_FALSE, p_tmp_frame.locals[1U], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE));
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerMachine_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerMachine_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerMachine_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerMachine_ANON3_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerMachine_ANON4_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_event;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_expr_5;
      PRT_VALUE *p_tmp_expr_6;
      PRT_VALUE *p_tmp_machine;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_tuple;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_SEQ(p_tmp_machine = P_EXPR_1(PrtTupleGetNC(p_tmp_expr_0, 0), PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_EVENT_eResponse, PRT_FALSE), P_EXPR_6(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_5), PrtSendInternal(context, PrtGetMachine(context->process, p_tmp_expr_5), p_tmp_expr_4, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_3), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, P_TUPLE_1(&P_GEND_TYPE_12, p_tmp_expr_2, p_tmp_expr_1), PRT_TRUE, PrtTupleGetNC(p_tmp_expr_0, 1), PRT_FALSE, &P_GEND_VALUE_5, PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE));
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerMachine_ANON5_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_event;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_expr_5;
      PRT_VALUE *p_tmp_expr_6;
      PRT_VALUE *p_tmp_machine;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_tuple;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_SEQ(p_tmp_machine = P_EXPR_1(PrtTupleGetNC(p_tmp_expr_0, 0), PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_EVENT_eResponse, PRT_FALSE), P_EXPR_6(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_5), PrtSendInternal(context, PrtGetMachine(context->process, p_tmp_expr_5), p_tmp_expr_4, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_3), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, P_TUPLE_1(&P_GEND_TYPE_12, p_tmp_expr_2, p_tmp_expr_1), PRT_TRUE, PrtTupleGetNC(p_tmp_expr_0, 1), PRT_FALSE, &P_GEND_VALUE_4, PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE));
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerMachine_ANON6_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_EXPR_2(P_SEQ(p_tmp_funstmt_ret = PrtCloneValue(PrtMkInterfaceOrMachine(context, P_IORM_HelperInterface, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_1)->id), NULL), PRT_FALSE, PrtCastValue(p_tmp_expr_0, &P_GEND_TYPE_6), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE);
      PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_ServerMachine_helper, p_tmp_funstmt_ret, PRT_FALSE);
      PrtGoto(p_tmp_mach_priv, P_STATE_ServerMachine_WaitForRequests, 0U);
      goto P_EXIT_FUN;
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_ServerMachine_ANON7_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_event;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_machine;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      if (p_tmp_frame.returnTo == 0U)
      {
        goto L0;
      }
      P_SEQ(p_tmp_machine = P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_ServerMachine_helper], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_EVENT_eProcessReq, PRT_FALSE), P_EXPR_4(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_3), PrtSendInternal(context, PrtGetMachine(context->process, p_tmp_expr_3), p_tmp_expr_2, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_1), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, PrtTupleGetNC(p_tmp_expr_0, 1), PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE));
      L0:
      if (p_tmp_frame.rcase == NULL && !PrtReceive(p_tmp_mach_priv, &p_tmp_frame, 0U))
      {
        return NULL;
      }
      (p_tmp_frame.rcase->fun->implementation)(context);
      if (p_tmp_mach_priv->receive != NULL)
      {
        PrtPushFrame(p_tmp_mach_priv, &p_tmp_frame);
        return NULL;
      }
      if (p_tmp_mach_priv->lastOperation != ReturnStatement)
      {
        goto P_EXIT_FUN;
      }
      p_tmp_frame.rcase = NULL;
      p_tmp_frame.returnTo = 0x0FFFFU;
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_TestDriver_1Client1Server_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_TestDriver_1Client1Server_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_TestDriver_1Client1Server_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      p_tmp_funstmt_ret = PrtCloneValue(PrtMkInterfaceOrMachine(context, P_IORM_ServerClientInterface, 0U)->id);
      PrtSetLocalVarEx(p_tmp_frame.locals, 1U, p_tmp_funstmt_ret, PRT_FALSE);
      P_EXPR_1(P_SEQ(p_tmp_funstmt_ret = PrtCloneValue(PrtMkInterfaceOrMachine(context, P_IORM_ClientInterface, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_0)->id), NULL), PRT_FALSE, p_tmp_frame.locals[1U], PRT_FALSE);
      if (p_tmp_funstmt_ret != NULL)
      {
        PrtFreeValue(p_tmp_funstmt_ret);
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_TestDriver_Refinement_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_TestDriver_Refinement_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_TestDriver_Refinement_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_TestDriver_Refinement_ANON3_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_TestDriver_Refinement_ANON4_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_TestDriver_Refinement_nextReqId, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_1, PRT_FALSE), PRT_FALSE);
      p_tmp_funstmt_ret = PrtCloneValue(PrtMkInterfaceOrMachine(context, P_IORM_ServerClientInterface, 0U)->id);
      PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_TestDriver_Refinement_server, p_tmp_funstmt_ret, PRT_FALSE);
      PrtGoto(p_tmp_mach_priv, P_STATE_TestDriver_Refinement_StartPumpingRequests, 0U);
      goto P_EXIT_FUN;
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_TestDriver_Refinement_ANON5_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_event;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_expr_5;
      PRT_VALUE *p_tmp_machine;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_tuple;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_SEQ(p_tmp_machine = P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_TestDriver_Refinement_server], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_EVENT_eRequest, PRT_FALSE), P_EXPR_5(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_4), PrtSendInternal(context, PrtGetMachine(context->process, p_tmp_expr_4), p_tmp_expr_3, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_2), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, P_TUPLE_1(&P_GEND_TYPE_13, p_tmp_expr_1, p_tmp_expr_0), PRT_TRUE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_TestDriver_Refinement_nextReqId], PRT_FALSE));
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_TestDriver_Refinement_nextReqId, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_1, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_TestDriver_Refinement_nextReqId], PRT_FALSE), PRT_FALSE);
      if (P_BOOL_EXPR(P_EXPR_2(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_0) > PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_3, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_TestDriver_Refinement_nextReqId], PRT_FALSE), PRT_TRUE))
      {
        P_STMT_0(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, 0U), P_EXPR_0(&P_GEND_VALUE_EVENT_HALT, PRT_FALSE), PRT_FALSE);
        goto P_EXIT_FUN;
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  PRT_CASEDECL P_GEND_CASES_P_FUN_ServerMachine_ANON7_0[] = 
  {
    
    {
        &P_EVENT_eReqFailed_STRUCT,
        &P_FUN_ServerMachine_ANON5_STRUCT
    },
    
    {
        &P_EVENT_eReqSuccessful_STRUCT,
        &P_FUN_ServerMachine_ANON4_STRUCT
    }
  };
  PRT_RECEIVEDECL P_GEND_RECEIVE_P_FUN_ServerMachine_ANON7[] = 
  {
    
    {
        0U,
        &P_GEND_EVENTSET_eReqSuccessful_eReqFailed,
        2U,
        P_GEND_CASES_P_FUN_ServerMachine_ANON7_0
    }
  };
  PRT_FUNDECL *P_GEND_FUNS_ClientMachine[] = 
  {
    &P_FUN_ClientMachine_ANON0_STRUCT,
    &P_FUN_ClientMachine_ANON1_STRUCT,
    &P_FUN_ClientMachine_ANON2_STRUCT,
    &P_FUN_ClientMachine_ANON3_STRUCT,
    &P_FUN_ClientMachine_ANON4_STRUCT,
    &P_FUN_ClientMachine_ANON5_STRUCT,
    &P_FUN_ClientMachine_ANON6_STRUCT
  };
  PRT_FUNDECL *P_GEND_FUNS_HelperMachine[] = 
  {
    &P_FUN_HelperMachine_ANON0_STRUCT,
    &P_FUN_HelperMachine_ANON1_STRUCT,
    &P_FUN_HelperMachine_ANON2_STRUCT,
    &P_FUN_HelperMachine_ANON3_STRUCT
  };
  PRT_FUNDECL *P_GEND_FUNS_ReqIdsAreMonotonicallyIncreasing[] = 
  {
    &P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON0_STRUCT,
    &P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON1_STRUCT,
    &P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON2_STRUCT
  };
  PRT_FUNDECL *P_GEND_FUNS_ServerAbstractionMachine[] = 
  {
    &P_FUN_ServerAbstractionMachine_ANON0_STRUCT,
    &P_FUN_ServerAbstractionMachine_ANON1_STRUCT,
    &P_FUN_ServerAbstractionMachine_ANON2_STRUCT
  };
  PRT_FUNDECL *P_GEND_FUNS_ServerMachine[] = 
  {
    &P_FUN_ServerMachine_ANON0_STRUCT,
    &P_FUN_ServerMachine_ANON1_STRUCT,
    &P_FUN_ServerMachine_ANON2_STRUCT,
    &P_FUN_ServerMachine_ANON3_STRUCT,
    &P_FUN_ServerMachine_ANON4_STRUCT,
    &P_FUN_ServerMachine_ANON5_STRUCT,
    &P_FUN_ServerMachine_ANON6_STRUCT,
    &P_FUN_ServerMachine_ANON7_STRUCT
  };
  PRT_FUNDECL *P_GEND_FUNS_TestDriver_1Client1Server[] = 
  {
    &P_FUN_TestDriver_1Client1Server_ANON0_STRUCT,
    &P_FUN_TestDriver_1Client1Server_ANON1_STRUCT,
    &P_FUN_TestDriver_1Client1Server_ANON2_STRUCT
  };
  PRT_FUNDECL *P_GEND_FUNS_TestDriver_Refinement[] = 
  {
    &P_FUN_TestDriver_Refinement_ANON0_STRUCT,
    &P_FUN_TestDriver_Refinement_ANON1_STRUCT,
    &P_FUN_TestDriver_Refinement_ANON2_STRUCT,
    &P_FUN_TestDriver_Refinement_ANON3_STRUCT,
    &P_FUN_TestDriver_Refinement_ANON4_STRUCT,
    &P_FUN_TestDriver_Refinement_ANON5_STRUCT
  };
  PRT_FUNDECL P_FUN_ClientMachine_ANON0_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ClientMachine_ANON0_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ClientMachine_ANON1_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ClientMachine_ANON1_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ClientMachine_ANON2_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ClientMachine_ANON2_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ClientMachine_ANON3_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ClientMachine_ANON3_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ClientMachine_ANON4_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ClientMachine_ANON4_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_12,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ClientMachine_ANON5_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ClientMachine_ANON5_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_7,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ClientMachine_ANON6_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ClientMachine_ANON6_IMPL,
    1U,
    2U,
    1U,
    &P_GEND_TYPE_4,
    &P_GEND_TYPE_9,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_HelperMachine_ANON0_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_HelperMachine_ANON0_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_6,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_HelperMachine_ANON1_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_HelperMachine_ANON1_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_HelperMachine_ANON2_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_HelperMachine_ANON2_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_HelperMachine_ANON3_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_HelperMachine_ANON3_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON0_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON0_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON1_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON1_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON2_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON2_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_13,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerAbstractionMachine_ANON0_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerAbstractionMachine_ANON0_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerAbstractionMachine_ANON1_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerAbstractionMachine_ANON1_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerAbstractionMachine_ANON2_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerAbstractionMachine_ANON2_IMPL,
    1U,
    2U,
    1U,
    &P_GEND_TYPE_13,
    &P_GEND_TYPE_11,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerMachine_ANON0_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerMachine_ANON0_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerMachine_ANON1_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerMachine_ANON1_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerMachine_ANON2_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerMachine_ANON2_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerMachine_ANON3_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerMachine_ANON3_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerMachine_ANON4_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerMachine_ANON4_IMPL,
    1U,
    1U,
    2U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerMachine_ANON5_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerMachine_ANON5_IMPL,
    1U,
    1U,
    2U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerMachine_ANON6_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerMachine_ANON6_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_ServerMachine_ANON7_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_ServerMachine_ANON7_IMPL,
    1U,
    2U,
    1U,
    &P_GEND_TYPE_13,
    NULL,
    1U,
    P_GEND_RECEIVE_P_FUN_ServerMachine_ANON7,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_TestDriver_1Client1Server_ANON0_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_TestDriver_1Client1Server_ANON0_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_TestDriver_1Client1Server_ANON1_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_TestDriver_1Client1Server_ANON1_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_TestDriver_1Client1Server_ANON2_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_TestDriver_1Client1Server_ANON2_IMPL,
    1U,
    2U,
    1U,
    &P_GEND_TYPE_4,
    &P_GEND_TYPE_10,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON0_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_TestDriver_Refinement_ANON0_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON1_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_TestDriver_Refinement_ANON1_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON2_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_TestDriver_Refinement_ANON2_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON3_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_TestDriver_Refinement_ANON3_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON4_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_TestDriver_Refinement_ANON4_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_TestDriver_Refinement_ANON5_STRUCT = 
  {
    0U,
    NULL,
    &P_FUN_TestDriver_Refinement_ANON5_IMPL,
    1U,
    1U,
    1U,
    &P_GEND_TYPE_4,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_STATEDECL P_GEND_STATES_ClientMachine[] = 
  {
    
    {
        "Init",
        0,
        0,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        NULL,
        NULL,
        &P_FUN_ClientMachine_ANON5_STRUCT,
        &P_FUN_ClientMachine_ANON1_STRUCT,
        0U,
        NULL
    },
    
    {
        "StartPumpingRequests",
        0,
        1,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET_eResponse,
        NULL,
        P_GEND_DOS_ClientMachine_StartPumpingRequests,
        &P_FUN_ClientMachine_ANON6_STRUCT,
        &P_FUN_ClientMachine_ANON3_STRUCT,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_HelperMachine[] = 
  {
    
    {
        "Init",
        0,
        1,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET_eProcessReq,
        NULL,
        P_GEND_DOS_HelperMachine_Init,
        &P_FUN_HelperMachine_ANON0_STRUCT,
        &P_FUN_HelperMachine_ANON3_STRUCT,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_ReqIdsAreMonotonicallyIncreasing[] = 
  {
    
    {
        "Init",
        0,
        1,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET_eRequest,
        NULL,
        P_GEND_DOS_ReqIdsAreMonotonicallyIncreasing_Init,
        &P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON0_STRUCT,
        &P_FUN_ReqIdsAreMonotonicallyIncreasing_ANON1_STRUCT,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_ServerAbstractionMachine[] = 
  {
    
    {
        "Init",
        0,
        1,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET_eRequest,
        NULL,
        P_GEND_DOS_ServerAbstractionMachine_Init,
        &P_FUN_ServerAbstractionMachine_ANON0_STRUCT,
        &P_FUN_ServerAbstractionMachine_ANON1_STRUCT,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_ServerMachine[] = 
  {
    
    {
        "Init",
        0,
        0,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        NULL,
        NULL,
        &P_FUN_ServerMachine_ANON6_STRUCT,
        &P_FUN_ServerMachine_ANON1_STRUCT,
        0U,
        NULL
    },
    
    {
        "WaitForRequests",
        0,
        1,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET_eRequest,
        NULL,
        P_GEND_DOS_ServerMachine_WaitForRequests,
        &P_FUN_ServerMachine_ANON2_STRUCT,
        &P_FUN_ServerMachine_ANON3_STRUCT,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_TestDriver_1Client1Server[] = 
  {
    
    {
        "Init",
        0,
        0,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        NULL,
        NULL,
        &P_FUN_TestDriver_1Client1Server_ANON2_STRUCT,
        &P_FUN_TestDriver_1Client1Server_ANON1_STRUCT,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_TestDriver_Refinement[] = 
  {
    
    {
        "Init",
        0,
        0,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        NULL,
        NULL,
        &P_FUN_TestDriver_Refinement_ANON4_STRUCT,
        &P_FUN_TestDriver_Refinement_ANON1_STRUCT,
        0U,
        NULL
    },
    
    {
        "StartPumpingRequests",
        0,
        2,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET,
        &P_GEND_EVENTSET_null_eResponse,
        NULL,
        P_GEND_DOS_TestDriver_Refinement_StartPumpingRequests,
        &P_FUN_TestDriver_Refinement_ANON2_STRUCT,
        &P_FUN_TestDriver_Refinement_ANON3_STRUCT,
        0U,
        NULL
    }
  };
  PRT_MACHINEDECL P_MACHINE_ClientMachine_STRUCT = 
  {
    0U,
    "ClientMachine",
    3,
    2,
    7,
    4294967295,
    P_STATE_ClientMachine_Init,
    P_GEND_VARS_ClientMachine,
    P_GEND_STATES_ClientMachine,
    P_GEND_FUNS_ClientMachine,
    0U,
    NULL
  };
  PRT_MACHINEDECL P_MACHINE_HelperMachine_STRUCT = 
  {
    0U,
    "HelperMachine",
    1,
    1,
    4,
    4294967295,
    P_STATE_HelperMachine_Init,
    P_GEND_VARS_HelperMachine,
    P_GEND_STATES_HelperMachine,
    P_GEND_FUNS_HelperMachine,
    0U,
    NULL
  };
  PRT_MACHINEDECL P_MACHINE_ReqIdsAreMonotonicallyIncreasing_STRUCT = 
  {
    0U,
    "ReqIdsAreMonotonicallyIncreasing",
    1,
    1,
    3,
    4294967295,
    P_STATE_ReqIdsAreMonotonicallyIncreasing_Init,
    P_GEND_VARS_ReqIdsAreMonotonicallyIncreasing,
    P_GEND_STATES_ReqIdsAreMonotonicallyIncreasing,
    P_GEND_FUNS_ReqIdsAreMonotonicallyIncreasing,
    0U,
    NULL
  };
  PRT_MACHINEDECL P_MACHINE_ServerAbstractionMachine_STRUCT = 
  {
    0U,
    "ServerAbstractionMachine",
    0,
    1,
    3,
    4294967295,
    P_STATE_ServerAbstractionMachine_Init,
    NULL,
    P_GEND_STATES_ServerAbstractionMachine,
    P_GEND_FUNS_ServerAbstractionMachine,
    0U,
    NULL
  };
  PRT_MACHINEDECL P_MACHINE_ServerMachine_STRUCT = 
  {
    0U,
    "ServerMachine",
    1,
    2,
    8,
    4294967295,
    P_STATE_ServerMachine_Init,
    P_GEND_VARS_ServerMachine,
    P_GEND_STATES_ServerMachine,
    P_GEND_FUNS_ServerMachine,
    0U,
    NULL
  };
  PRT_MACHINEDECL P_MACHINE_TestDriver_1Client1Server_STRUCT = 
  {
    0U,
    "TestDriver_1Client1Server",
    0,
    1,
    3,
    4294967295,
    P_STATE_TestDriver_1Client1Server_Init,
    NULL,
    P_GEND_STATES_TestDriver_1Client1Server,
    P_GEND_FUNS_TestDriver_1Client1Server,
    0U,
    NULL
  };
  PRT_MACHINEDECL P_MACHINE_TestDriver_Refinement_STRUCT = 
  {
    0U,
    "TestDriver_Refinement",
    2,
    2,
    6,
    4294967295,
    P_STATE_TestDriver_Refinement_Init,
    P_GEND_VARS_TestDriver_Refinement,
    P_GEND_STATES_TestDriver_Refinement,
    P_GEND_FUNS_TestDriver_Refinement,
    0U,
    NULL
  };
  