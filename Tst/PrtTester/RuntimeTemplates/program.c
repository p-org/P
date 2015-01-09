
  #include "program.h"
  #define P_SEQ
  #define P_STMT_0(s, x0) P_SEQ(p_tmp_stmt_0 = (x0), (s), PrtFreeValue(p_tmp_stmt_0))
  #define P_STMT_1(s, x1, x0) P_SEQ(p_tmp_stmt_0 = (x0), p_tmp_stmt_1 = (x1), (s), PrtFreeValue(p_tmp_stmt_0), PrtFreeValue(p_tmp_stmt_1))
  #define P_BOOL_EXPR(x0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_bool = PrtPrimGetBool(p_tmp_expr_0), PrtFreeValue(p_tmp_expr_0), p_tmp_bool)
  #define P_EXPR_0(x0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_0)
  #define P_EXPR_2(x2, x1, x0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), PrtFreeValue(p_tmp_expr_0), PrtFreeValue(p_tmp_expr_1), p_tmp_expr_2)
  PRT_TYPE P_GEND_TYPE_0 = 
  {
    PRT_KIND_ANY,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_1 = 
  {
    PRT_KIND_INT,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_2 = 
  {
    PRT_KIND_NULL,
    NULL
  };
  PRT_EVENTDECL P_GEND_EVENTS[] = 
  {
    
    {
        _P_EVENT_DEFAULT,
        "default",
        0U,
        &P_GEND_TYPE_2,
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
        P_EVENT_E,
        "E",
        4294967295U,
        &P_GEND_TYPE_2,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_Program[] = 
  {
    
    {
        P_VAR_Program_i,
        P_MACHINE_Program,
        "i",
        &P_GEND_TYPE_1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_Program_Init[] = 
  {
    
    {
        0,
        P_STATE_Program_Init,
        P_MACHINE_Program,
        P_EVENT_E,
        P_STATE_Program_Call,
        P_FUN_Program_ANON0,
        0U,
        NULL
    }
  };
  PRT_VALUE *P_FUN_Program_ANON0_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_Program_ANON1_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_stmt_1;
    PRT_VALUE *p_tmp_expr_0;
    P_STMT_0(PrtSetGlobalVar(p_tmp_mach_priv, P_VAR_Program_i, p_tmp_stmt_0), P_EXPR_0(PrtMkIntValue(0U)));
    P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), PrtMkNullValue(), P_EXPR_0(PrtMkEventValue(P_EVENT_E)));
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_Program_ANON2_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_stmt_1;
    PRT_VALUE *p_tmp_expr_0;
    PRT_VALUE *p_tmp_expr_1;
    PRT_VALUE *p_tmp_expr_2;
    if (P_BOOL_EXPR(P_EXPR_2(PrtMkBoolValue(PrtIsEqualValue(p_tmp_expr_0, p_tmp_expr_1)), PrtMkIntValue(3U), PrtCloneValue(p_tmp_mach_priv->varValues[P_VAR_Program_i]))))
    {
      PrtPop(p_tmp_mach_priv);
      goto P_EXIT_FUN;
    }
    else
    {
      P_STMT_0(PrtSetGlobalVar(p_tmp_mach_priv, P_VAR_Program_i, p_tmp_stmt_0), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PrtMkIntValue(1U), PrtCloneValue(p_tmp_mach_priv->varValues[P_VAR_Program_i])));
    }
    P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), PrtMkNullValue(), P_EXPR_0(PrtMkEventValue(P_EVENT_E)));
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_Program_ANON3_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    if (P_BOOL_EXPR(P_EXPR_0(PrtMkBoolValue(PRT_FALSE))))
    {
    }
    else
    {
      PrtHandleError(PRT_STATUS_ASSERT, p_tmp_mach_priv);
    }
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_FUNDECL P_GEND_FUNS_Program[] = 
  {
    
    {
        _P_FUN_Program_PUSH_OR_IGN,
        P_MACHINE_Program,
        NULL,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Program_ANON0,
        P_MACHINE_Program,
        NULL,
        &P_FUN_Program_ANON0_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Program_ANON1,
        P_MACHINE_Program,
        NULL,
        &P_FUN_Program_ANON1_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Program_ANON2,
        P_MACHINE_Program,
        NULL,
        &P_FUN_Program_ANON2_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_Program_ANON3,
        P_MACHINE_Program,
        NULL,
        &P_FUN_Program_ANON3_IMPL,
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
    }
  };
  PRT_STATEDECL P_GEND_STATES_Program[] = 
  {
    
    {
        P_STATE_Program_Call,
        P_MACHINE_Program,
        "Call",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_Program_ANON2,
        P_FUN_Program_ANON0,
        0U,
        NULL
    },
    
    {
        P_STATE_Program_Init,
        P_MACHINE_Program,
        "Init",
        1,
        0,
        0,
        1,
        0,
        P_GEND_TRANS_Program_Init,
        NULL,
        P_FUN_Program_ANON1,
        P_FUN_Program_ANON3,
        0U,
        NULL
    }
  };
  PRT_MACHINEDECL P_GEND_MACHINES[] = 
  {
    
    {
        P_MACHINE_Program,
        "Program",
        1,
        2,
        5,
        4294967295,
        P_STATE_Program_Init,
        P_GEND_VARS_Program,
        P_GEND_STATES_Program,
        P_GEND_FUNS_Program,
        &P_CTOR_Program_IMPL,
        &P_DTOR_Program_IMPL,
        0U,
        NULL
    }
  };
  PRT_PROGRAMDECL P_GEND_PROGRAM = 
  {
    3U,
    2U,
    1U,
    0U,
    P_GEND_EVENTS,
    P_GEND_EVENTSETS,
    P_GEND_MACHINES,
    NULL,
    0U,
    NULL
  };
  