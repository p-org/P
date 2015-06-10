
  #include "program.h"
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
  #define P_EXPR_5(x5, f5, x4, f4, x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), p_tmp_expr_4 = (x4), p_tmp_expr_5 = (x5), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), ((f3) ? PrtFreeValue(p_tmp_expr_3) : 0U), ((f4) ? PrtFreeValue(p_tmp_expr_4) : 0U), p_tmp_expr_5)
  #define P_EXPR_6(x6, f6, x5, f5, x4, f4, x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), p_tmp_expr_4 = (x4), p_tmp_expr_5 = (x5), p_tmp_expr_6 = (x6), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), ((f3) ? PrtFreeValue(p_tmp_expr_3) : 0U), ((f4) ? PrtFreeValue(p_tmp_expr_4) : 0U), ((f5) ? PrtFreeValue(p_tmp_expr_5) : 0U), p_tmp_expr_6)
  #define P_EXPR_7(x7, f7, x6, f6, x5, f5, x4, f4, x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), p_tmp_expr_4 = (x4), p_tmp_expr_5 = (x5), p_tmp_expr_6 = (x6), p_tmp_expr_7 = (x7), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), ((f3) ? PrtFreeValue(p_tmp_expr_3) : 0U), ((f4) ? PrtFreeValue(p_tmp_expr_4) : 0U), ((f5) ? PrtFreeValue(p_tmp_expr_5) : 0U), ((f6) ? PrtFreeValue(p_tmp_expr_6) : 0U), p_tmp_expr_7)
  #define P_TUPLE_0(t, x0) P_SEQ(p_tmp_tuple = PrtMkDefaultValue(t), PrtTupleSet(p_tmp_tuple, 0U, (x0)), p_tmp_tuple)
  #define P_TUPLE_1(t, x0, x1) P_SEQ(p_tmp_tuple = PrtMkDefaultValue(t), PrtTupleSet(p_tmp_tuple, 0U, (x0)), PrtTupleSet(p_tmp_tuple, 1U, (x1)), p_tmp_tuple)
  #define P_TUPLE_2(t, x0, x1, x2) P_SEQ(p_tmp_tuple = PrtMkDefaultValue(t), PrtTupleSet(p_tmp_tuple, 0U, (x0)), PrtTupleSet(p_tmp_tuple, 1U, (x1)), PrtTupleSet(p_tmp_tuple, 2U, (x2)), p_tmp_tuple)
  PRT_TYPE P_GEND_TYPE_0 = 
  {
    PRT_KIND_ANY,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_1 = 
  {
    PRT_KIND_EVENT,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_2 = 
  {
    PRT_KIND_INT,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_3 = 
  {
    PRT_KIND_NULL,
    NULL
  };
  PRT_TYPE P_GEND_TYPE_4 = 
  {
    PRT_KIND_MACHINE,
    NULL
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_5[] = 
  {
    "constructorparam"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_5[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_5 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_5,
    P_GEND_TYPE_NMDTUP_TARR_5
  };
  PRT_TYPE P_GEND_TYPE_5 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_5
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_6[] = 
  {
    "model_h"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_6[] = 
  {
    &P_GEND_TYPE_4
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_6 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_6,
    P_GEND_TYPE_NMDTUP_TARR_6
  };
  PRT_TYPE P_GEND_TYPE_6 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_6
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_7[] = 
  {
    "p"
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
    "param"
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
    "param"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_9[] = 
  {
    &P_GEND_TYPE_3
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
  PRT_TYPE *P_GEND_TYPE_TUP_ARR_10[] = 
  {
    &P_GEND_TYPE_4
  };
  PRT_TUPTYPE P_GEND_TYPE_TUP_10 = 
  {
    1,
    P_GEND_TYPE_TUP_ARR_10
  };
  PRT_TYPE P_GEND_TYPE_10 = 
  {
    PRT_KIND_TUPLE,
    (PRT_MAPTYPE *)&P_GEND_TYPE_TUP_10
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_11[] = 
  {
    "e",
    "p"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_11[] = 
  {
    &P_GEND_TYPE_1,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_11 = 
  {
    2,
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
    "typeofmachine",
    "constructorparam"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_12[] = 
  {
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_12 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_12,
    P_GEND_TYPE_NMDTUP_TARR_12
  };
  PRT_TYPE P_GEND_TYPE_12 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_12
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_13[] = 
  {
    "typeofmachine",
    "param"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_13[] = 
  {
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_13 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_13,
    P_GEND_TYPE_NMDTUP_TARR_13
  };
  PRT_TYPE P_GEND_TYPE_13 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_13
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_14[] = 
  {
    "typeofmachine",
    "param"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_14[] = 
  {
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_3
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_14 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_14,
    P_GEND_TYPE_NMDTUP_TARR_14
  };
  PRT_TYPE P_GEND_TYPE_14 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_14
  };
  PRT_TYPE *P_GEND_TYPE_TUP_ARR_15[] = 
  {
    &P_GEND_TYPE_4,
    &P_GEND_TYPE_4
  };
  PRT_TUPTYPE P_GEND_TYPE_TUP_15 = 
  {
    2,
    P_GEND_TYPE_TUP_ARR_15
  };
  PRT_TYPE P_GEND_TYPE_15 = 
  {
    PRT_KIND_TUPLE,
    (PRT_MAPTYPE *)&P_GEND_TYPE_TUP_15
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_16[] = 
  {
    "creator",
    "typeofmachine",
    "constructorparam"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_16[] = 
  {
    &P_GEND_TYPE_4,
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_16 = 
  {
    3,
    P_GEND_TYPE_NMDTUP_NARR_16,
    P_GEND_TYPE_NMDTUP_TARR_16
  };
  PRT_TYPE P_GEND_TYPE_16 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_16
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_17[] = 
  {
    "nodeManager",
    "typeofmachine",
    "param"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_17[] = 
  {
    &P_GEND_TYPE_4,
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_17 = 
  {
    3,
    P_GEND_TYPE_NMDTUP_NARR_17,
    P_GEND_TYPE_NMDTUP_TARR_17
  };
  PRT_TYPE P_GEND_TYPE_17 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_17
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_18[] = 
  {
    "nodeManager",
    "typeofmachine",
    "param"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_18[] = 
  {
    &P_GEND_TYPE_4,
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_3
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_18 = 
  {
    3,
    P_GEND_TYPE_NMDTUP_NARR_18,
    P_GEND_TYPE_NMDTUP_TARR_18
  };
  PRT_TYPE P_GEND_TYPE_18 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_18
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_19[] = 
  {
    "param"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_19[] = 
  {
    &P_GEND_TYPE_15
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_19 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_19,
    P_GEND_TYPE_NMDTUP_TARR_19
  };
  PRT_TYPE P_GEND_TYPE_19 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_19
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_20[] = 
  {
    "target",
    "e",
    "p"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_20[] = 
  {
    &P_GEND_TYPE_4,
    &P_GEND_TYPE_1,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_20 = 
  {
    3,
    P_GEND_TYPE_NMDTUP_NARR_20,
    P_GEND_TYPE_NMDTUP_TARR_20
  };
  PRT_TYPE P_GEND_TYPE_20 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_20
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_21[] = 
  {
    "typeofmachine",
    "param"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_21[] = 
  {
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_15
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_21 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_21,
    P_GEND_TYPE_NMDTUP_TARR_21
  };
  PRT_TYPE P_GEND_TYPE_21 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_21
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_22[] = 
  {
    "nodeManager",
    "typeofmachine",
    "param"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_22[] = 
  {
    &P_GEND_TYPE_4,
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_15
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_22 = 
  {
    3,
    P_GEND_TYPE_NMDTUP_NARR_22,
    P_GEND_TYPE_NMDTUP_TARR_22
  };
  PRT_TYPE P_GEND_TYPE_22 = 
  {
    PRT_KIND_NMDTUP,
    (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_22
  };
  PRT_VALUE P_GEND_VALUE_0 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_Ping
  };
  PRT_VALUE P_GEND_VALUE_1 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_Pong
  };
  PRT_VALUE P_GEND_VALUE_2 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_Req_CreateMachine
  };
  PRT_VALUE P_GEND_VALUE_3 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_Resp_CreateMachine
  };
  PRT_VALUE P_GEND_VALUE_4 = 
  {
    PRT_VALKIND_EVENT,
    P_EVENT_Success
  };
  PRT_VALUE P_GEND_VALUE_5 = 
  {
    PRT_VALKIND_INT,
    1U
  };
  PRT_VALUE P_GEND_VALUE_6 = 
  {
    PRT_VALKIND_INT,
    2U
  };
  PRT_VALUE P_GEND_VALUE_7 = 
  {
    PRT_VALKIND_BOOL,
    PRT_FALSE
  };
  PRT_VALUE P_GEND_VALUE_8 = 
  {
    PRT_VALKIND_EVENT,
    PRT_SPECIAL_EVENT_HALT
  };
  PRT_VALUE P_GEND_VALUE_9 = 
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
        &P_GEND_TYPE_3,
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
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_EVENT_Pong,
        "Pong",
        2U,
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_EVENT_Req_CreateMachine,
        "Req_CreateMachine",
        4294967295U,
        &P_GEND_TYPE_16,
        0U,
        NULL
    },
    
    {
        P_EVENT_Resp_CreateMachine,
        "Resp_CreateMachine",
        4294967295U,
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_EVENT_Success,
        "Success",
        4294967295U,
        &P_GEND_TYPE_3,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_GodMachine[] = 
  {
    
    {
        P_VAR_GodMachine_PongMachine_1,
        P_MACHINE_GodMachine,
        "PongMachine_1",
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_VAR_GodMachine_PongMachine_2,
        P_MACHINE_GodMachine,
        "PongMachine_2",
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_VAR_GodMachine_createmachine_param,
        P_MACHINE_GodMachine,
        "createmachine_param",
        &P_GEND_TYPE_17,
        0U,
        NULL
    },
    
    {
        P_VAR_GodMachine_createmachine_return,
        P_MACHINE_GodMachine,
        "createmachine_return",
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_VAR_GodMachine_temp_NM,
        P_MACHINE_GodMachine,
        "temp_NM",
        &P_GEND_TYPE_4,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_NodeManager[] = 
  {
    
    {
        P_VAR_NodeManager_createmachine_param,
        P_MACHINE_NodeManager,
        "createmachine_param",
        &P_GEND_TYPE_17,
        0U,
        NULL
    },
    
    {
        P_VAR_NodeManager_createmachine_return,
        P_MACHINE_NodeManager,
        "createmachine_return",
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_VAR_NodeManager_newMachine,
        P_MACHINE_NodeManager,
        "newMachine",
        &P_GEND_TYPE_4,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_PING[] = 
  {
    
    {
        P_VAR_PING_createmachine_param,
        P_MACHINE_PING,
        "createmachine_param",
        &P_GEND_TYPE_17,
        0U,
        NULL
    },
    
    {
        P_VAR_PING_createmachine_return,
        P_MACHINE_PING,
        "createmachine_return",
        &P_GEND_TYPE_4,
        0U,
        NULL
    },
    
    {
        P_VAR_PING_pongmachine,
        P_MACHINE_PING,
        "pongmachine",
        &P_GEND_TYPE_15,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_PONG[] = 
  {
    
    {
        P_VAR_PONG_createmachine_param,
        P_MACHINE_PONG,
        "createmachine_param",
        &P_GEND_TYPE_17,
        0U,
        NULL
    },
    
    {
        P_VAR_PONG_createmachine_return,
        P_MACHINE_PONG,
        "createmachine_return",
        &P_GEND_TYPE_4,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_NodeManager_CreateNewMachine[] = 
  {
    
    {
        0,
        P_STATE_NodeManager_CreateNewMachine,
        P_MACHINE_NodeManager,
        P_EVENT_Req_CreateMachine,
        P_STATE_NodeManager_CreateNewMachine,
        P_FUN_NodeManager_ANON1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_NodeManager_Init[] = 
  {
    
    {
        0,
        P_STATE_NodeManager_Init,
        P_MACHINE_NodeManager,
        P_EVENT_Req_CreateMachine,
        P_STATE_NodeManager_CreateNewMachine,
        P_FUN_NodeManager_ANON1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PING_Init[] = 
  {
    
    {
        0,
        P_STATE_PING_Init,
        P_MACHINE_PING,
        P_EVENT_Success,
        P_STATE_PING_Ping_SendPing,
        P_FUN_PING_ANON1,
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
        P_STATE_PING_Ping_WaitPong_1,
        P_FUN_PING_ANON1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PING_Ping_WaitPong_1[] = 
  {
    
    {
        0,
        P_STATE_PING_Ping_WaitPong_1,
        P_MACHINE_PING,
        P_EVENT_Pong,
        P_STATE_PING_Ping_WaitPong_2,
        P_FUN_PING_ANON1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PING_Ping_WaitPong_2[] = 
  {
    
    {
        0,
        P_STATE_PING_Ping_WaitPong_2,
        P_MACHINE_PING,
        P_EVENT_Pong,
        P_STATE_PING_Done,
        P_FUN_PING_ANON1,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_PONG_Init[] = 
  {
    
    {
        0,
        P_STATE_PONG_Init,
        P_MACHINE_PONG,
        P_EVENT_Ping,
        P_STATE_PONG_Pong_SendPong,
        P_FUN_PONG_ANON1,
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
        P_STATE_PONG_End,
        P_FUN_PONG_ANON1,
        0U,
        NULL
    }
  };
  PRT_VALUE *P_FUN_GodMachine_PopState_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

  PRT_VALUE *P_FUN_NodeManager_PopState_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

  PRT_VALUE *P_FUN_NodeManager__CREATELOCALMACHINE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

  PRT_VALUE *P_FUN_PING_PopState_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

  PRT_VALUE *P_FUN_PONG_PopState_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

  PRT_VALUE *P_FUN__CREATENODE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

  PRT_VALUE *P_FUN__SENDRELIABLE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

  PRT_VALUE *P_FUN__SEND_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value);

  PRT_DODECL P_GEND_DOS_GodMachine__CREATEMACHINE[] = 
  {
    
    {
        0,
        P_STATE_GodMachine__CREATEMACHINE,
        P_MACHINE_GodMachine,
        P_EVENT_Resp_CreateMachine,
        P_FUN_GodMachine_PopState,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_NodeManager__CREATEMACHINE[] = 
  {
    
    {
        0,
        P_STATE_NodeManager__CREATEMACHINE,
        P_MACHINE_NodeManager,
        P_EVENT_Resp_CreateMachine,
        P_FUN_NodeManager_PopState,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_PING__CREATEMACHINE[] = 
  {
    
    {
        0,
        P_STATE_PING__CREATEMACHINE,
        P_MACHINE_PING,
        P_EVENT_Resp_CreateMachine,
        P_FUN_PING_PopState,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_PONG__CREATEMACHINE[] = 
  {
    
    {
        0,
        P_STATE_PONG__CREATEMACHINE,
        P_MACHINE_PONG,
        P_EVENT_Resp_CreateMachine,
        P_FUN_PONG_PopState,
        0U,
        NULL
    }
  };
  PRT_VALUE *P_FUN_GodMachine_ANON0_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    PRT_VALUE *p_tmp_expr_1;
    PRT_VALUE *p_tmp_expr_2;
    PRT_VALUE *p_tmp_expr_3;
    PRT_VALUE *p_tmp_expr_4;
    PRT_VALUE *p_tmp_expr_5;
    PRT_VALUE *p_tmp_expr_6;
    PRT_VALUE *p_tmp_expr_7;
    p_tmp_funstmt_ret = P_EXPR_7(P_FUN__SENDRELIABLE_IMPL(context, 3U, P_TUPLE_2(&P_GEND_TYPE_20, p_tmp_expr_3, p_tmp_expr_0, p_tmp_expr_6)), PRT_FALSE, P_TUPLE_2(&P_GEND_TYPE_16, p_tmp_expr_2, p_tmp_expr_5, p_tmp_expr_4), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 1), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 2), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 0), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_GodMachine_createmachine_param], PRT_FALSE, &P_GEND_VALUE_2, PRT_FALSE);
    if (p_tmp_funstmt_ret == NULL)
    {
    }
    else
    {
      PrtFreeValue(p_tmp_funstmt_ret);
    }
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_GodMachine_ANON1_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
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

  PRT_VALUE *P_FUN_GodMachine_ANON2_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    PRT_VALUE *p_tmp_expr_1;
    PRT_VALUE *p_tmp_expr_2;
    PRT_VALUE *p_tmp_expr_3;
    PRT_VALUE *p_tmp_expr_4;
    PRT_VALUE *p_tmp_expr_5;
    if (p_tmp_mach_priv->returnTo == 1U)
    {
      goto L1;
    }
    if (p_tmp_mach_priv->returnTo == 2U)
    {
      goto L2;
    }
    if (p_tmp_mach_priv->returnTo == 3U)
    {
      goto L3;
    }
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_GodMachine_temp_NM, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(P_FUN__CREATENODE_IMPL(context, 1U, P_TUPLE_0(&P_GEND_TYPE_6, p_tmp_expr_0)), PRT_FALSE, &P_GEND_VALUE_9, PRT_FALSE), PRT_FALSE);
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_GodMachine_createmachine_param, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_3(P_TUPLE_2(&P_GEND_TYPE_18, p_tmp_expr_0, p_tmp_expr_1, p_tmp_expr_2), PRT_FALSE, &P_GEND_VALUE_9, PRT_FALSE, &P_GEND_VALUE_5, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_GodMachine_temp_NM], PRT_FALSE), PRT_FALSE);
    p_tmp_mach_priv->returnTo = 1U;
    PrtPush(p_tmp_mach_priv, P_STATE_GodMachine__CREATEMACHINE);
    goto P_EXIT_FUN;
    L1: ;
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_GodMachine_PongMachine_1, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_GodMachine_createmachine_return], PRT_FALSE), PRT_FALSE);
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_GodMachine_createmachine_param, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_3(P_TUPLE_2(&P_GEND_TYPE_18, p_tmp_expr_0, p_tmp_expr_1, p_tmp_expr_2), PRT_FALSE, &P_GEND_VALUE_9, PRT_FALSE, &P_GEND_VALUE_5, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_GodMachine_temp_NM], PRT_FALSE), PRT_FALSE);
    p_tmp_mach_priv->returnTo = 2U;
    PrtPush(p_tmp_mach_priv, P_STATE_GodMachine__CREATEMACHINE);
    goto P_EXIT_FUN;
    L2: ;
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_GodMachine_PongMachine_2, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_GodMachine_createmachine_return], PRT_FALSE), PRT_FALSE);
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_GodMachine_temp_NM, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(P_FUN__CREATENODE_IMPL(context, 1U, P_TUPLE_0(&P_GEND_TYPE_6, p_tmp_expr_0)), PRT_FALSE, &P_GEND_VALUE_9, PRT_FALSE), PRT_FALSE);
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_GodMachine_createmachine_param, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_5(P_TUPLE_2(&P_GEND_TYPE_22, p_tmp_expr_2, p_tmp_expr_3, p_tmp_expr_4), PRT_FALSE, P_TUPLE_1(&P_GEND_TYPE_15, p_tmp_expr_0, p_tmp_expr_1), PRT_TRUE, &P_GEND_VALUE_6, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_GodMachine_temp_NM], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_GodMachine_PongMachine_2], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_GodMachine_PongMachine_1], PRT_FALSE), PRT_FALSE);
    p_tmp_mach_priv->returnTo = 3U;
    PrtPush(p_tmp_mach_priv, P_STATE_GodMachine__CREATEMACHINE);
    goto P_EXIT_FUN;
    L3: ;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_GodMachine_PopState_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_GodMachine_createmachine_return, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_mach_priv->currentEvent.payload, PRT_FALSE), PRT_FALSE);
    PrtPop(p_tmp_mach_priv);
    goto P_EXIT_FUN;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_NodeManager_ANON0_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    PRT_VALUE *p_tmp_expr_1;
    PRT_VALUE *p_tmp_expr_2;
    PRT_VALUE *p_tmp_expr_3;
    PRT_VALUE *p_tmp_expr_4;
    PRT_VALUE *p_tmp_expr_5;
    PRT_VALUE *p_tmp_expr_6;
    PRT_VALUE *p_tmp_expr_7;
    p_tmp_funstmt_ret = P_EXPR_7(P_FUN__SENDRELIABLE_IMPL(context, 3U, P_TUPLE_2(&P_GEND_TYPE_20, p_tmp_expr_3, p_tmp_expr_0, p_tmp_expr_6)), PRT_FALSE, P_TUPLE_2(&P_GEND_TYPE_16, p_tmp_expr_2, p_tmp_expr_5, p_tmp_expr_4), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 1), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 2), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 0), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_NodeManager_createmachine_param], PRT_FALSE, &P_GEND_VALUE_2, PRT_FALSE);
    if (p_tmp_funstmt_ret == NULL)
    {
    }
    else
    {
      PrtFreeValue(p_tmp_funstmt_ret);
    }
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_NodeManager_ANON1_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
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

  PRT_VALUE *P_FUN_NodeManager_ANON2_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    PRT_VALUE *p_tmp_expr_1;
    PRT_VALUE *p_tmp_expr_2;
    PRT_VALUE *p_tmp_expr_3;
    PRT_VALUE *p_tmp_expr_4;
    p_tmp_funstmt_ret = P_EXPR_3(P_FUN_NodeManager__CREATELOCALMACHINE_IMPL(context, 5U, P_TUPLE_1(&P_GEND_TYPE_13, p_tmp_expr_2, p_tmp_expr_1)), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_0, 1), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_0, 2), PRT_FALSE, p_tmp_mach_priv->currentEvent.payload, PRT_FALSE);
    if (p_tmp_funstmt_ret == NULL)
    {
    }
    else
    {
      PrtFreeValue(p_tmp_funstmt_ret);
    }
    p_tmp_funstmt_ret = P_EXPR_4(P_FUN__SENDRELIABLE_IMPL(context, 3U, P_TUPLE_2(&P_GEND_TYPE_20, p_tmp_expr_3, p_tmp_expr_0, p_tmp_expr_1)), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_2, 0), PRT_FALSE, p_tmp_mach_priv->currentEvent.payload, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_NodeManager_newMachine], PRT_FALSE, &P_GEND_VALUE_3, PRT_FALSE);
    if (p_tmp_funstmt_ret == NULL)
    {
    }
    else
    {
      PrtFreeValue(p_tmp_funstmt_ret);
    }
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_NodeManager_PopState_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_NodeManager_createmachine_return, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_mach_priv->currentEvent.payload, PRT_FALSE), PRT_FALSE);
    PrtPop(p_tmp_mach_priv);
    goto P_EXIT_FUN;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_NodeManager__CREATELOCALMACHINE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    PRT_VALUE *p_tmp_expr_1;
    PRT_VALUE *p_tmp_expr_2;
    if (P_BOOL_EXPR(P_EXPR_2(PrtMkBoolValue(PrtIsEqualValue(p_tmp_expr_0, p_tmp_expr_1)), PRT_FALSE, &P_GEND_VALUE_5, PRT_FALSE, PrtTupleGetNC(value, 0U), PRT_FALSE), PRT_TRUE))
    {
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_NodeManager_newMachine, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(PrtCloneValue(PrtMkMachine(context->process, P_MACHINE_PONG, p_tmp_expr_0)->id), PRT_FALSE, PrtTupleGetNC(value, 1U), PRT_FALSE), PRT_FALSE);
    }
    else if (P_BOOL_EXPR(P_EXPR_2(PrtMkBoolValue(PrtIsEqualValue(p_tmp_expr_0, p_tmp_expr_1)), PRT_FALSE, &P_GEND_VALUE_6, PRT_FALSE, PrtTupleGetNC(value, 0U), PRT_FALSE), PRT_TRUE))
    {
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_NodeManager_newMachine, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(PrtCloneValue(PrtMkMachine(context->process, P_MACHINE_PING, p_tmp_expr_0)->id), PRT_FALSE, PrtTupleGetNC(value, 1U), PRT_FALSE), PRT_FALSE);
    }
    else if (P_BOOL_EXPR(P_EXPR_0(&P_GEND_VALUE_7, PRT_FALSE), PRT_FALSE))
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

  PRT_VALUE *P_FUN_PING_ANON0_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    PRT_VALUE *p_tmp_expr_1;
    PRT_VALUE *p_tmp_expr_2;
    PRT_VALUE *p_tmp_expr_3;
    PRT_VALUE *p_tmp_expr_4;
    PRT_VALUE *p_tmp_expr_5;
    PRT_VALUE *p_tmp_expr_6;
    PRT_VALUE *p_tmp_expr_7;
    p_tmp_funstmt_ret = P_EXPR_7(P_FUN__SENDRELIABLE_IMPL(context, 3U, P_TUPLE_2(&P_GEND_TYPE_20, p_tmp_expr_3, p_tmp_expr_0, p_tmp_expr_6)), PRT_FALSE, P_TUPLE_2(&P_GEND_TYPE_16, p_tmp_expr_2, p_tmp_expr_5, p_tmp_expr_4), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 1), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 2), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 0), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_PING_createmachine_param], PRT_FALSE, &P_GEND_VALUE_2, PRT_FALSE);
    if (p_tmp_funstmt_ret == NULL)
    {
    }
    else
    {
      PrtFreeValue(p_tmp_funstmt_ret);
    }
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_PING_ANON1_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
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

  PRT_VALUE *P_FUN_PING_ANON2_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
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
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_PING_pongmachine, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_1(PrtCastValue(p_tmp_expr_0, &P_GEND_TYPE_15), PRT_FALSE, p_tmp_mach_priv->currentEvent.payload, PRT_FALSE), PRT_FALSE);
    P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_9, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_4, PRT_FALSE), PRT_FALSE);
    goto P_EXIT_FUN;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_PING_ANON3_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
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
    PRT_VALUE *p_tmp_expr_3;
    PRT_VALUE *p_tmp_expr_4;
    p_tmp_funstmt_ret = P_EXPR_4(P_FUN__SEND_IMPL(context, 2U, P_TUPLE_2(&P_GEND_TYPE_20, p_tmp_expr_3, p_tmp_expr_0, p_tmp_expr_2)), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 0), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_PING_pongmachine], PRT_FALSE, &P_GEND_VALUE_0, PRT_FALSE);
    if (p_tmp_funstmt_ret == NULL)
    {
    }
    else
    {
      PrtFreeValue(p_tmp_funstmt_ret);
    }
    p_tmp_funstmt_ret = P_EXPR_4(P_FUN__SEND_IMPL(context, 2U, P_TUPLE_2(&P_GEND_TYPE_20, p_tmp_expr_3, p_tmp_expr_0, p_tmp_expr_2)), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 1), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_PING_pongmachine], PRT_FALSE, &P_GEND_VALUE_0, PRT_FALSE);
    if (p_tmp_funstmt_ret == NULL)
    {
    }
    else
    {
      PrtFreeValue(p_tmp_funstmt_ret);
    }
    P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_9, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_4, PRT_FALSE), PRT_FALSE);
    goto P_EXIT_FUN;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_PING_PopState_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_PING_createmachine_return, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_mach_priv->currentEvent.payload, PRT_FALSE), PRT_FALSE);
    PrtPop(p_tmp_mach_priv);
    goto P_EXIT_FUN;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_PONG_ANON0_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    PRT_VALUE *p_tmp_expr_1;
    PRT_VALUE *p_tmp_expr_2;
    PRT_VALUE *p_tmp_expr_3;
    PRT_VALUE *p_tmp_expr_4;
    PRT_VALUE *p_tmp_expr_5;
    PRT_VALUE *p_tmp_expr_6;
    PRT_VALUE *p_tmp_expr_7;
    p_tmp_funstmt_ret = P_EXPR_7(P_FUN__SENDRELIABLE_IMPL(context, 3U, P_TUPLE_2(&P_GEND_TYPE_20, p_tmp_expr_3, p_tmp_expr_0, p_tmp_expr_6)), PRT_FALSE, P_TUPLE_2(&P_GEND_TYPE_16, p_tmp_expr_2, p_tmp_expr_5, p_tmp_expr_4), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 1), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 2), PRT_FALSE, PrtTupleGetNC(p_tmp_expr_1, 0), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_PONG_createmachine_param], PRT_FALSE, &P_GEND_VALUE_2, PRT_FALSE);
    if (p_tmp_funstmt_ret == NULL)
    {
    }
    else
    {
      PrtFreeValue(p_tmp_funstmt_ret);
    }
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_PONG_ANON1_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
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

  PRT_VALUE *P_FUN_PONG_ANON2_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_stmt_1;
    PRT_VALUE *p_tmp_expr_0;
    P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_9, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_8, PRT_FALSE), PRT_FALSE);
    goto P_EXIT_FUN;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_PONG_ANON3_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
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
    PRT_VALUE *p_tmp_expr_3;
    p_tmp_funstmt_ret = P_EXPR_3(P_FUN__SEND_IMPL(context, 2U, P_TUPLE_2(&P_GEND_TYPE_20, p_tmp_expr_1, p_tmp_expr_0, p_tmp_expr_2)), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->currentEvent.payload, PRT_FALSE, &P_GEND_VALUE_1, PRT_FALSE);
    if (p_tmp_funstmt_ret == NULL)
    {
    }
    else
    {
      PrtFreeValue(p_tmp_funstmt_ret);
    }
    P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_9, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_4, PRT_FALSE), PRT_FALSE);
    goto P_EXIT_FUN;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN_PONG_PopState_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_PONG_createmachine_return, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_mach_priv->currentEvent.payload, PRT_FALSE), PRT_FALSE);
    PrtPop(p_tmp_mach_priv);
    goto P_EXIT_FUN;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN__CREATENODE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_expr_0;
    PRT_VALUE *p_tmp_expr_1;
    P_STMT_0(PrtTupleSetEx(value, 0U, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(PrtCloneValue(PrtMkMachine(context->process, P_MACHINE_NodeManager, p_tmp_expr_0)->id), PRT_FALSE, &P_GEND_VALUE_9, PRT_FALSE), PRT_FALSE);
    p_tmp_ret = PrtCloneValue(P_EXPR_0(PrtTupleGetNC(value, 0U), PRT_FALSE));
    goto P_EXIT_FUN;
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    if (p_tmp_ret == NULL)
    {
      return PrtMkDefaultValue(&P_GEND_TYPE_4);
    }
    else
    {
      return p_tmp_ret;
    }
  }

  PRT_VALUE *P_FUN__SENDRELIABLE_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_stmt_1;
    PRT_VALUE *p_tmp_stmt_2;
    PRT_VALUE *p_tmp_expr_0;
    P_STMT_2(PrtSend(PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, p_tmp_stmt_2), P_EXPR_0(PrtTupleGetNC(value, 2U), PRT_FALSE), PRT_FALSE, P_EXPR_0(PrtTupleGetNC(value, 1U), PRT_FALSE), PRT_FALSE, P_EXPR_0(PrtTupleGetNC(value, 0U), PRT_FALSE), PRT_FALSE);
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_VALUE *P_FUN__SEND_IMPL(PRT_MACHINEINST *context, PRT_UINT32 funIndex, PRT_VALUE *value)
  {
    PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
    PRT_BOOLEAN p_tmp_bool;
    PRT_VALUE *p_tmp_ret = NULL;
    PRT_VALUE *p_tmp_tuple;
    PRT_VALUE *p_tmp_funstmt_ret;
    PRT_VALUE *p_tmp_stmt_0;
    PRT_VALUE *p_tmp_stmt_1;
    PRT_VALUE *p_tmp_stmt_2;
    PRT_VALUE *p_tmp_expr_0;
    if (P_BOOL_EXPR(P_EXPR_0(PrtMkNondetBoolValue(), PRT_FALSE), PRT_TRUE))
    {
      P_STMT_2(PrtSend(PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, p_tmp_stmt_2), P_EXPR_0(PrtTupleGetNC(value, 2U), PRT_FALSE), PRT_FALSE, P_EXPR_0(PrtTupleGetNC(value, 1U), PRT_FALSE), PRT_FALSE, P_EXPR_0(PrtTupleGetNC(value, 0U), PRT_FALSE), PRT_FALSE);
    }
    P_EXIT_FUN:
    if (value != NULL)
    {
      PrtFreeValue(value);
    }
    return p_tmp_ret;
  }

  PRT_FUNDECL P_GEND_FUNS_GodMachine[] = 
  {
    
    {
        _P_FUN_GodMachine_PUSH_OR_IGN,
        P_MACHINE_GodMachine,
        NULL,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_GodMachine_ANON0,
        P_MACHINE_GodMachine,
        NULL,
        &P_FUN_GodMachine_ANON0_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_GodMachine_ANON1,
        P_MACHINE_GodMachine,
        NULL,
        &P_FUN_GodMachine_ANON1_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_GodMachine_ANON2,
        P_MACHINE_GodMachine,
        NULL,
        &P_FUN_GodMachine_ANON2_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_GodMachine_PopState,
        P_MACHINE_GodMachine,
        "PopState",
        &P_FUN_GodMachine_PopState_IMPL,
        0U,
        NULL
    }
  };
  PRT_FUNDECL P_GEND_FUNS_NodeManager[] = 
  {
    
    {
        _P_FUN_NodeManager_PUSH_OR_IGN,
        P_MACHINE_NodeManager,
        NULL,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_NodeManager_ANON0,
        P_MACHINE_NodeManager,
        NULL,
        &P_FUN_NodeManager_ANON0_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_NodeManager_ANON1,
        P_MACHINE_NodeManager,
        NULL,
        &P_FUN_NodeManager_ANON1_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_NodeManager_ANON2,
        P_MACHINE_NodeManager,
        NULL,
        &P_FUN_NodeManager_ANON2_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_NodeManager_PopState,
        P_MACHINE_NodeManager,
        "PopState",
        &P_FUN_NodeManager_PopState_IMPL,
        0U,
        NULL
    },
    
    {
        P_FUN_NodeManager__CREATELOCALMACHINE,
        P_MACHINE_NodeManager,
        "_CREATELOCALMACHINE",
        &P_FUN_NodeManager__CREATELOCALMACHINE_IMPL,
        0U,
        NULL
    }
  };
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
    },
    
    {
        P_FUN_PING_PopState,
        P_MACHINE_PING,
        "PopState",
        &P_FUN_PING_PopState_IMPL,
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
    },
    
    {
        P_FUN_PONG_PopState,
        P_MACHINE_PONG,
        "PopState",
        &P_FUN_PONG_PopState_IMPL,
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
    0x20U
  };
  PRT_UINT32 P_GEND_EVENTSET_5[] = 
  {
    0x40U
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
  PRT_STATEDECL P_GEND_STATES_GodMachine[] = 
  {
    
    {
        P_STATE_GodMachine_Init,
        P_MACHINE_GodMachine,
        "Init",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_GodMachine_ANON2,
        P_FUN_GodMachine_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_GodMachine__CREATEMACHINE,
        P_MACHINE_GodMachine,
        "_CREATEMACHINE",
        0,
        1,
        0,
        0,
        4,
        NULL,
        P_GEND_DOS_GodMachine__CREATEMACHINE,
        P_FUN_GodMachine_ANON0,
        P_FUN_GodMachine_ANON1,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_NodeManager[] = 
  {
    
    {
        P_STATE_NodeManager_CreateNewMachine,
        P_MACHINE_NodeManager,
        "CreateNewMachine",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_NodeManager_CreateNewMachine,
        NULL,
        P_FUN_NodeManager_ANON2,
        P_FUN_NodeManager_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_NodeManager_Init,
        P_MACHINE_NodeManager,
        "Init",
        1,
        0,
        0,
        3,
        0,
        P_GEND_TRANS_NodeManager_Init,
        NULL,
        P_FUN_NodeManager_ANON1,
        P_FUN_NodeManager_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_NodeManager__CREATEMACHINE,
        P_MACHINE_NodeManager,
        "_CREATEMACHINE",
        0,
        1,
        0,
        0,
        4,
        NULL,
        P_GEND_DOS_NodeManager__CREATEMACHINE,
        P_FUN_NodeManager_ANON0,
        P_FUN_NodeManager_ANON1,
        0U,
        NULL
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
        P_FUN_PING_ANON1,
        P_FUN_PING_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_PING_Init,
        P_MACHINE_PING,
        "Init",
        1,
        0,
        0,
        5,
        0,
        P_GEND_TRANS_PING_Init,
        NULL,
        P_FUN_PING_ANON2,
        P_FUN_PING_ANON1,
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
        5,
        0,
        P_GEND_TRANS_PING_Ping_SendPing,
        NULL,
        P_FUN_PING_ANON3,
        P_FUN_PING_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_PING_Ping_WaitPong_1,
        P_MACHINE_PING,
        "Ping_WaitPong_1",
        1,
        0,
        0,
        2,
        0,
        P_GEND_TRANS_PING_Ping_WaitPong_1,
        NULL,
        P_FUN_PING_ANON1,
        P_FUN_PING_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_PING_Ping_WaitPong_2,
        P_MACHINE_PING,
        "Ping_WaitPong_2",
        1,
        0,
        0,
        2,
        0,
        P_GEND_TRANS_PING_Ping_WaitPong_2,
        NULL,
        P_FUN_PING_ANON1,
        P_FUN_PING_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_PING__CREATEMACHINE,
        P_MACHINE_PING,
        "_CREATEMACHINE",
        0,
        1,
        0,
        0,
        4,
        NULL,
        P_GEND_DOS_PING__CREATEMACHINE,
        P_FUN_PING_ANON0,
        P_FUN_PING_ANON1,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_PONG[] = 
  {
    
    {
        P_STATE_PONG_End,
        P_MACHINE_PONG,
        "End",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_PONG_ANON2,
        P_FUN_PONG_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_PONG_Init,
        P_MACHINE_PONG,
        "Init",
        1,
        0,
        0,
        1,
        0,
        P_GEND_TRANS_PONG_Init,
        NULL,
        P_FUN_PONG_ANON1,
        P_FUN_PONG_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_PONG_Pong_SendPong,
        P_MACHINE_PONG,
        "Pong_SendPong",
        1,
        0,
        0,
        5,
        0,
        P_GEND_TRANS_PONG_Pong_SendPong,
        NULL,
        P_FUN_PONG_ANON3,
        P_FUN_PONG_ANON1,
        0U,
        NULL
    },
    
    {
        P_STATE_PONG__CREATEMACHINE,
        P_MACHINE_PONG,
        "_CREATEMACHINE",
        0,
        1,
        0,
        0,
        4,
        NULL,
        P_GEND_DOS_PONG__CREATEMACHINE,
        P_FUN_PONG_ANON0,
        P_FUN_PONG_ANON1,
        0U,
        NULL
    }
  };
  PRT_MACHINEDECL P_GEND_MACHINES[] = 
  {
    
    {
        P_MACHINE_GodMachine,
        "GodMachine",
        5,
        2,
        5,
        4294967295,
        P_STATE_GodMachine_Init,
        P_GEND_VARS_GodMachine,
        P_GEND_STATES_GodMachine,
        P_GEND_FUNS_GodMachine,
        &P_CTOR_GodMachine_IMPL,
        &P_DTOR_GodMachine_IMPL,
        0U,
        NULL
    },
    
    {
        P_MACHINE_NodeManager,
        "NodeManager",
        3,
        3,
        6,
        4294967295,
        P_STATE_NodeManager_Init,
        P_GEND_VARS_NodeManager,
        P_GEND_STATES_NodeManager,
        P_GEND_FUNS_NodeManager,
        &P_CTOR_NodeManager_IMPL,
        &P_DTOR_NodeManager_IMPL,
        0U,
        NULL
    },
    
    {
        P_MACHINE_PING,
        "PING",
        3,
        6,
        6,
        4294967295,
        P_STATE_PING_Init,
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
        2,
        4,
        6,
        4294967295,
        P_STATE_PONG_Init,
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
    4U,
    0U,
    P_GEND_EVENTS,
    P_GEND_EVENTSETS,
    P_GEND_MACHINES,
    NULL,
    0U,
    NULL
  };
  