
  #include "timer.h"
  #define P_SEQ
  #define P_STMT_0(s, x0, f0) P_SEQ(p_tmp_stmt_0 = (x0), (s), ((f0) ? PrtFreeValue(p_tmp_stmt_0) : 0U))
  #define P_BOOL_EXPR(x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_bool = PrtPrimGetBool(p_tmp_expr_0), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), p_tmp_bool)
  #define P_EXPR_0(x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_0)
  #define P_EXPR_1(x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), p_tmp_expr_1)
  #define P_EXPR_3(x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), p_tmp_expr_3)
  #define P_TUPLE_0(t, x0) P_SEQ(p_tmp_tuple = PrtMkDefaultValue(t), PrtTupleSet(p_tmp_tuple, 0U, (x0)), p_tmp_tuple)
  static PRT_TYPE P_GEND_TYPE_0 = 
  {
    PRT_KIND_ANY,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_1 = 
  {
    PRT_KIND_BOOL,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_2 = 
  {
    PRT_KIND_EVENT,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_3 = 
  {
    PRT_KIND_INT,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_4 = 
  {
    PRT_KIND_MACHINE,
    
    {
        NULL
    }
  };
  static PRT_TYPE P_GEND_TYPE_5 = 
  {
    PRT_KIND_NULL,
    
    {
        NULL
    }
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_6[] = 
  {
    "_payload_0"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_6[] = 
  {
    &P_GEND_TYPE_5
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_6 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_6,
    P_GEND_TYPE_NMDTUP_TARR_6
  };
  static PRT_TYPE P_GEND_TYPE_6 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_6
    }
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_7[] = 
  {
    "_payload_1"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_7[] = 
  {
    &P_GEND_TYPE_5
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_7 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_7,
    P_GEND_TYPE_NMDTUP_TARR_7
  };
  static PRT_TYPE P_GEND_TYPE_7 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_7
    }
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_8[] = 
  {
    "_payload_2"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_8[] = 
  {
    &P_GEND_TYPE_5
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_8 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_8,
    P_GEND_TYPE_NMDTUP_TARR_8
  };
  static PRT_TYPE P_GEND_TYPE_8 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_8
    }
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_9[] = 
  {
    "_payload_skip"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_9[] = 
  {
    &P_GEND_TYPE_5
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
    "m"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_10[] = 
  {
    &P_GEND_TYPE_4
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
    "owner"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_11[] = 
  {
    &P_GEND_TYPE_4
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
    "payload"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_12[] = 
  {
    &P_GEND_TYPE_4
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_12 = 
  {
    1,
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
    "time"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_13[] = 
  {
    &P_GEND_TYPE_3
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_13 = 
  {
    1,
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
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_14[] = 
  {
    "timer"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_14[] = 
  {
    &P_GEND_TYPE_4
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_14 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_14,
    P_GEND_TYPE_NMDTUP_TARR_14
  };
  static PRT_TYPE P_GEND_TYPE_14 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_14
    }
  };
  static PRT_STRING P_GEND_TYPE_NMDTUP_NARR_15[] = 
  {
    "timer",
    "time"
  };
  static PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_15[] = 
  {
    &P_GEND_TYPE_4,
    &P_GEND_TYPE_3
  };
  static PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_15 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_15,
    P_GEND_TYPE_NMDTUP_TARR_15
  };
  static PRT_TYPE P_GEND_TYPE_15 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_15
    }
  };
  static PRT_VALUE P_GEND_VALUE_0 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_CANCEL_FAILURE
    }
  };
  static PRT_VALUE P_GEND_VALUE_1 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_CANCEL_SUCCESS
    }
  };
  static PRT_VALUE P_GEND_VALUE_2 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_TIMEOUT
    }
  };
  static PRT_VALUE P_GEND_VALUE_3 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_UNIT
    }
  };
  static PRT_VALUE P_GEND_VALUE_4 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        PRT_SPECIAL_EVENT_HALT
    }
  };
  static PRT_VALUE P_GEND_VALUE_5 = 
  {
    PRT_VALUE_KIND_NULL,
    
    {
        PRT_SPECIAL_EVENT_NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_Timer[] = 
  {
    
    {
        P_VAR_Timer_client,
        P_MACHINE_Timer,
        "client",
        &P_GEND_TYPE_4,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Timer_Init[] = 
  {
    
    {
        0,
        P_STATE_Timer_Init,
        P_MACHINE_Timer,
        P_EVENT_UNIT,
        P_STATE_Timer_WaitForReq,
        P_FUN_Timer_ANON3,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Timer_WaitForCancel[] = 
  {
    
    {
        0,
        P_STATE_Timer_WaitForCancel,
        P_MACHINE_Timer,
        P_EVENT_CANCEL,
        P_STATE_Timer_WaitForReq,
        P_FUN_Timer_ANON0,
        0U,
        NULL
    },
    
    {
        1,
        P_STATE_Timer_WaitForCancel,
        P_MACHINE_Timer,
        _P_EVENT_NULL,
        P_STATE_Timer_WaitForReq,
        P_FUN_Timer_ANON10,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Timer_WaitForReq[] = 
  {
    
    {
        0,
        P_STATE_Timer_WaitForReq,
        P_MACHINE_Timer,
        P_EVENT_CANCEL,
        P_STATE_Timer_WaitForReq,
        P_FUN_Timer_ANON9,
        0U,
        NULL
    },
    
    {
        1,
        P_STATE_Timer_WaitForReq,
        P_MACHINE_Timer,
        P_EVENT_START,
        P_STATE_Timer_WaitForCancel,
        P_FUN_Timer_ANON6,
        0U,
        NULL
    }
  };
  PRT_VALUE *P_FUN_CreateTimer_IMPL(PRT_MACHINEINST *context);

  PRT_DODECL P_GEND_DOS_Timer_WaitForCancel[] = 
  {
    
    {
        0,
        P_STATE_Timer_WaitForCancel,
        P_MACHINE_Timer,
        P_EVENT_START,
        _P_FUN_PUSH_OR_IGN,
        0U,
        NULL
    }
  };
  PRT_VALUE *P_FUN_CreateTimer_IMPL(PRT_MACHINEINST *context)
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
      P_EXPR_1(P_SEQ(p_tmp_funstmt_ret = PrtCloneValue(PrtMkInterfaceOrMachine(context, P_IORM_Timer, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_0)->id), NULL), PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE);
      PrtSetLocalVarEx(p_tmp_frame.locals, 1U, p_tmp_funstmt_ret, PRT_FALSE);
      P_SEQ(p_tmp_ret = p_tmp_frame.locals[1U], p_tmp_frame.locals[1U] = NULL);
      goto P_EXIT_FUN;
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      if (p_tmp_ret == NULL)
      {
        return PrtMkDefaultValue(&P_GEND_TYPE_11);
      }
      else
      {
        return p_tmp_ret;
      }
    }
  }

  static PRT_VALUE *P_FUN_Timer_ANON0_IMPL(PRT_MACHINEINST *context)
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
      PRT_VALUE *p_tmp_machine;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      if (P_BOOL_EXPR(P_EXPR_0(PrtMkNondetBoolValue(), PRT_TRUE), PRT_TRUE))
      {
        P_SEQ(p_tmp_machine = P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_Timer_client], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_1, PRT_FALSE), P_EXPR_3(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_2), PrtSend(context, PrtGetMachine(context->process, p_tmp_expr_2), p_tmp_expr_1, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_0), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE));
      }
      else
      {
        P_SEQ(p_tmp_machine = P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_Timer_client], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_0, PRT_FALSE), P_EXPR_3(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_2), PrtSend(context, PrtGetMachine(context->process, p_tmp_expr_2), p_tmp_expr_1, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_0), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE));
        P_SEQ(p_tmp_machine = P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_Timer_client], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_2, PRT_FALSE), P_EXPR_3(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_2), PrtSend(context, PrtGetMachine(context->process, p_tmp_expr_2), p_tmp_expr_1, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_0), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE));
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_Timer_ANON10_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_event;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_machine;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_SEQ(p_tmp_machine = P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_Timer_client], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_2, PRT_FALSE), P_EXPR_3(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_2), PrtSend(context, PrtGetMachine(context->process, p_tmp_expr_2), p_tmp_expr_1, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_0), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE));
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_Timer_ANON11_IMPL(PRT_MACHINEINST *context)
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
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Timer_client, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
      P_STMT_0(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, 0U), P_EXPR_0(&P_GEND_VALUE_3, PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_Timer_ANON1_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Timer_ANON2_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Timer_ANON3_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Timer_ANON4_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Timer_ANON5_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Timer_ANON6_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Timer_ANON7_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Timer_ANON8_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Timer_ANON9_IMPL(PRT_MACHINEINST *context)
  {
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_event;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_machine;
      PRT_VALUE *p_tmp_ret;
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      p_tmp_ret = NULL;
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      P_SEQ(p_tmp_machine = P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_Timer_client], PRT_FALSE), p_tmp_event = P_EXPR_0(&P_GEND_VALUE_0, PRT_FALSE), P_EXPR_3(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_expr_2), PrtSend(context, PrtGetMachine(context->process, p_tmp_expr_2), p_tmp_expr_1, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_0), NULL), PRT_FALSE, p_tmp_machine, PRT_FALSE, p_tmp_event, PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE));
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      return p_tmp_ret;
    }
  }

  PRT_FUNDECL P_GEND_FUNS_Timer[] = 
  {
    
    {
        P_FUN_Timer_ANON0,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON0_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON1,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON1_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON2,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON2_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON3,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON3_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON4,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON4_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON5,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON5_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON6,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON6_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON7,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON7_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON8,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON8_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON9,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON9_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON10,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON10_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_5,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Timer_ANON11,
        P_MACHINE_Timer,
        NULL,
        &P_FUN_Timer_ANON11_IMPL,
        1U,
        1U,
        1U,
        &P_GEND_TYPE_4,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    }
  };
  PRT_FUNDECL P_FUN_CancelTimer_STRUCT = 
  {
    P_FUN_CancelTimer,
    0U,
    "CancelTimer",
    &P_FUN_CancelTimer_IMPL,
    1U,
    1U,
    0U,
    NULL,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_CreateTimer_STRUCT = 
  {
    P_FUN_CreateTimer,
    0U,
    "CreateTimer",
    &P_FUN_CreateTimer_IMPL,
    1U,
    2U,
    0U,
    NULL,
    &P_GEND_TYPE_10,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_FUNDECL P_FUN_StartTimer_STRUCT = 
  {
    P_FUN_StartTimer,
    0U,
    "StartTimer",
    &P_FUN_StartTimer_IMPL,
    2U,
    2U,
    0U,
    NULL,
    NULL,
    0U,
    NULL,
    0U,
    NULL
  };
  PRT_STATEDECL P_GEND_STATES_Timer[] = 
  {
    
    {
        P_STATE_Timer_Init,
        P_MACHINE_Timer,
        "Init",
        1,
        0,
        P_GEND_EVENTSET_,
        P_GEND_EVENTSET_UNIT,
        P_GEND_EVENTSET_,
        P_GEND_TRANS_Timer_Init,
        NULL,
        P_FUN_Timer_ANON11,
        P_FUN_Timer_ANON2,
        0U,
        NULL
    },
    
    {
        P_STATE_Timer_WaitForCancel,
        P_MACHINE_Timer,
        "WaitForCancel",
        2,
        1,
        P_GEND_EVENTSET_,
        P_GEND_EVENTSET_CANCELnull,
        P_GEND_EVENTSET_START,
        P_GEND_TRANS_Timer_WaitForCancel,
        P_GEND_DOS_Timer_WaitForCancel,
        P_FUN_Timer_ANON7,
        P_FUN_Timer_ANON8,
        0U,
        NULL
    },
    
    {
        P_STATE_Timer_WaitForReq,
        P_MACHINE_Timer,
        "WaitForReq",
        2,
        0,
        P_GEND_EVENTSET_,
        P_GEND_EVENTSET_CANCELSTART,
        P_GEND_EVENTSET_,
        P_GEND_TRANS_Timer_WaitForReq,
        NULL,
        P_FUN_Timer_ANON4,
        P_FUN_Timer_ANON5,
        0U,
        NULL
    }
  };
  PRT_MACHINEDECL P_MACHINE_Timer_STRUCT = 
  {
    P_MACHINE_Timer,
    "Timer",
    1,
    3,
    13,
    4294967295,
    P_STATE_Timer_Init,
    P_GEND_VARS_Timer,
    P_GEND_STATES_Timer,
    P_GEND_FUNS_Timer,
    &P_CTOR_Timer_IMPL,
    &P_DTOR_Timer_IMPL,
    0U,
    NULL
  };
  