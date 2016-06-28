
  #include "Driver.h"
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
  #define P_EXPR_7(x7, f7, x6, f6, x5, f5, x4, f4, x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), p_tmp_expr_4 = (x4), p_tmp_expr_5 = (x5), p_tmp_expr_6 = (x6), p_tmp_expr_7 = (x7), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), ((f3) ? PrtFreeValue(p_tmp_expr_3) : 0U), ((f4) ? PrtFreeValue(p_tmp_expr_4) : 0U), ((f5) ? PrtFreeValue(p_tmp_expr_5) : 0U), ((f6) ? PrtFreeValue(p_tmp_expr_6) : 0U), p_tmp_expr_7)
  #define P_EXPR_8(x8, f8, x7, f7, x6, f6, x5, f5, x4, f4, x3, f3, x2, f2, x1, f1, x0, f0) P_SEQ(p_tmp_expr_0 = (x0), p_tmp_expr_1 = (x1), p_tmp_expr_2 = (x2), p_tmp_expr_3 = (x3), p_tmp_expr_4 = (x4), p_tmp_expr_5 = (x5), p_tmp_expr_6 = (x6), p_tmp_expr_7 = (x7), p_tmp_expr_8 = (x8), ((f0) ? PrtFreeValue(p_tmp_expr_0) : 0U), ((f1) ? PrtFreeValue(p_tmp_expr_1) : 0U), ((f2) ? PrtFreeValue(p_tmp_expr_2) : 0U), ((f3) ? PrtFreeValue(p_tmp_expr_3) : 0U), ((f4) ? PrtFreeValue(p_tmp_expr_4) : 0U), ((f5) ? PrtFreeValue(p_tmp_expr_5) : 0U), ((f6) ? PrtFreeValue(p_tmp_expr_6) : 0U), ((f7) ? PrtFreeValue(p_tmp_expr_7) : 0U), p_tmp_expr_8)
  #define P_TUPLE_0(t, x0) P_SEQ(p_tmp_tuple = PrtMkDefaultValue(t), PrtTupleSet(p_tmp_tuple, 0U, (x0)), p_tmp_tuple)
  #define P_TUPLE_1(t, x0, x1) P_SEQ(p_tmp_tuple = PrtMkDefaultValue(t), PrtTupleSet(p_tmp_tuple, 0U, (x0)), PrtTupleSet(p_tmp_tuple, 1U, (x1)), p_tmp_tuple)
  #define P_TUPLE_2(t, x0, x1, x2) P_SEQ(p_tmp_tuple = PrtMkDefaultValue(t), PrtTupleSet(p_tmp_tuple, 0U, (x0)), PrtTupleSet(p_tmp_tuple, 1U, (x1)), PrtTupleSet(p_tmp_tuple, 2U, (x2)), p_tmp_tuple)
  PRT_TYPE P_GEND_TYPE_0 = 
  {
    PRT_KIND_ANY,
    
    {
        NULL
    }
  };
  PRT_TYPE P_GEND_TYPE_1 = 
  {
    PRT_KIND_BOOL,
    
    {
        NULL
    }
  };
  PRT_TYPE P_GEND_TYPE_2 = 
  {
    PRT_KIND_EVENT,
    
    {
        NULL
    }
  };
  PRT_TYPE P_GEND_TYPE_3 = 
  {
    PRT_KIND_INT,
    
    {
        NULL
    }
  };
  PRT_TYPE P_GEND_TYPE_4 = 
  {
    PRT_KIND_NULL,
    
    {
        NULL
    }
  };
  PRT_TYPE P_GEND_TYPE_5 = 
  {
    PRT_KIND_MACHINE,
    
    {
        NULL
    }
  };
  PRT_MAPTYPE P_GEND_TYPE_MAP_6 = 
  {
    &P_GEND_TYPE_5,
    &P_GEND_TYPE_1
  };
  PRT_TYPE P_GEND_TYPE_6 = 
  {
    PRT_KIND_MAP,
    
    {
        &P_GEND_TYPE_MAP_6
    }
  };
  PRT_MAPTYPE P_GEND_TYPE_MAP_7 = 
  {
    &P_GEND_TYPE_5,
    &P_GEND_TYPE_3
  };
  PRT_TYPE P_GEND_TYPE_7 = 
  {
    PRT_KIND_MAP,
    
    {
        &P_GEND_TYPE_MAP_7
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_8[] = 
  {
    "_payload_0"
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
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_8
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_9[] = 
  {
    "_payload_1"
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
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_9
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_10[] = 
  {
    "_payload_2"
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
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_10
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_11[] = 
  {
    "_payload_3"
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
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_11
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_12[] = 
  {
    "_payload_4"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_12[] = 
  {
    &P_GEND_TYPE_0
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
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_12
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_13[] = 
  {
    "_payload_skip"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_13[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_13 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_13,
    P_GEND_TYPE_NMDTUP_TARR_13
  };
  PRT_TYPE P_GEND_TYPE_13 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_13
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_14[] = 
  {
    "container"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_14[] = 
  {
    &P_GEND_TYPE_5
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_14 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_14,
    P_GEND_TYPE_NMDTUP_TARR_14
  };
  PRT_TYPE P_GEND_TYPE_14 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_14
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_15[] = 
  {
    "i"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_15[] = 
  {
    &P_GEND_TYPE_3
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_15 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_15,
    P_GEND_TYPE_NMDTUP_TARR_15
  };
  PRT_TYPE P_GEND_TYPE_15 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_15
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_16[] = 
  {
    "j"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_16[] = 
  {
    &P_GEND_TYPE_3
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_16 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_16,
    P_GEND_TYPE_NMDTUP_TARR_16
  };
  PRT_TYPE P_GEND_TYPE_16 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_16
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_17[] = 
  {
    "newMachine"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_17[] = 
  {
    &P_GEND_TYPE_5
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_17 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_17,
    P_GEND_TYPE_NMDTUP_TARR_17
  };
  PRT_TYPE P_GEND_TYPE_17 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_17
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_18[] = 
  {
    "p"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_18[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_18 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_18,
    P_GEND_TYPE_NMDTUP_TARR_18
  };
  PRT_TYPE P_GEND_TYPE_18 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_18
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_19[] = 
  {
    "payload"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_19[] = 
  {
    &P_GEND_TYPE_5
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
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_19
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_20[] = 
  {
    "retVal"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_20[] = 
  {
    &P_GEND_TYPE_5
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_20 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_20,
    P_GEND_TYPE_NMDTUP_TARR_20
  };
  PRT_TYPE P_GEND_TYPE_20 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_20
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_21[] = 
  {
    "timerCanceled"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_21[] = 
  {
    &P_GEND_TYPE_1
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_21 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_21,
    P_GEND_TYPE_NMDTUP_TARR_21
  };
  PRT_TYPE P_GEND_TYPE_21 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_21
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_22[] = 
  {
    "_payload_0"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_22[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_22 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_22,
    P_GEND_TYPE_NMDTUP_TARR_22
  };
  PRT_TYPE P_GEND_TYPE_22 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_22
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_23[] = 
  {
    "_payload_1"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_23[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_23 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_23,
    P_GEND_TYPE_NMDTUP_TARR_23
  };
  PRT_TYPE P_GEND_TYPE_23 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_23
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_24[] = 
  {
    "_payload_2"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_24[] = 
  {
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_24 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_24,
    P_GEND_TYPE_NMDTUP_TARR_24
  };
  PRT_TYPE P_GEND_TYPE_24 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_24
    }
  };
  PRT_SEQTYPE P_GEND_TYPE_SEQ_25 = 
  {
    &P_GEND_TYPE_5
  };
  PRT_TYPE P_GEND_TYPE_25 = 
  {
    PRT_KIND_SEQ,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_SEQ_25
    }
  };
  PRT_TYPE *P_GEND_TYPE_TUP_ARR_26[] = 
  {
    &P_GEND_TYPE_1
  };
  PRT_TUPTYPE P_GEND_TYPE_TUP_26 = 
  {
    1,
    P_GEND_TYPE_TUP_ARR_26
  };
  PRT_TYPE P_GEND_TYPE_26 = 
  {
    PRT_KIND_TUPLE,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_TUP_26
    }
  };
  PRT_TYPE *P_GEND_TYPE_TUP_ARR_27[] = 
  {
    &P_GEND_TYPE_5
  };
  PRT_TUPTYPE P_GEND_TYPE_TUP_27 = 
  {
    1,
    P_GEND_TYPE_TUP_ARR_27
  };
  PRT_TYPE P_GEND_TYPE_27 = 
  {
    PRT_KIND_TUPLE,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_TUP_27
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_28[] = 
  {
    "e",
    "p"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_28[] = 
  {
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_28 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_28,
    P_GEND_TYPE_NMDTUP_TARR_28
  };
  PRT_TYPE P_GEND_TYPE_28 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_28
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_29[] = 
  {
    "i",
    "j"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_29[] = 
  {
    &P_GEND_TYPE_3,
    &P_GEND_TYPE_3
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_29 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_29,
    P_GEND_TYPE_NMDTUP_TARR_29
  };
  PRT_TYPE P_GEND_TYPE_29 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_29
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_30[] = 
  {
    "payload"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_30[] = 
  {
    &P_GEND_TYPE_6
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_30 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_30,
    P_GEND_TYPE_NMDTUP_TARR_30
  };
  PRT_TYPE P_GEND_TYPE_30 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_30
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_31[] = 
  {
    "payload"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_31[] = 
  {
    &P_GEND_TYPE_25
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_31 = 
  {
    1,
    P_GEND_TYPE_NMDTUP_NARR_31,
    P_GEND_TYPE_NMDTUP_TARR_31
  };
  PRT_TYPE P_GEND_TYPE_31 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_31
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_32[] = 
  {
    "timerCanceled",
    "_payload_3"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_32[] = 
  {
    &P_GEND_TYPE_1,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_32 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_32,
    P_GEND_TYPE_NMDTUP_TARR_32
  };
  PRT_TYPE P_GEND_TYPE_32 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_32
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_33[] = 
  {
    "timerCanceled",
    "_payload_4"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_33[] = 
  {
    &P_GEND_TYPE_1,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_33 = 
  {
    2,
    P_GEND_TYPE_NMDTUP_NARR_33,
    P_GEND_TYPE_NMDTUP_TARR_33
  };
  PRT_TYPE P_GEND_TYPE_33 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_33
    }
  };
  PRT_TYPE *P_GEND_TYPE_TUP_ARR_34[] = 
  {
    &P_GEND_TYPE_3,
    &P_GEND_TYPE_5
  };
  PRT_TUPTYPE P_GEND_TYPE_TUP_34 = 
  {
    2,
    P_GEND_TYPE_TUP_ARR_34
  };
  PRT_TYPE P_GEND_TYPE_34 = 
  {
    PRT_KIND_TUPLE,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_TUP_34
    }
  };
  PRT_TYPE *P_GEND_TYPE_TUP_ARR_35[] = 
  {
    &P_GEND_TYPE_5,
    &P_GEND_TYPE_1
  };
  PRT_TUPTYPE P_GEND_TYPE_TUP_35 = 
  {
    2,
    P_GEND_TYPE_TUP_ARR_35
  };
  PRT_TYPE P_GEND_TYPE_35 = 
  {
    PRT_KIND_TUPLE,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_TUP_35
    }
  };
  PRT_STRING P_GEND_TYPE_NMDTUP_NARR_36[] = 
  {
    "target",
    "e",
    "p"
  };
  PRT_TYPE *P_GEND_TYPE_NMDTUP_TARR_36[] = 
  {
    &P_GEND_TYPE_5,
    &P_GEND_TYPE_2,
    &P_GEND_TYPE_0
  };
  PRT_NMDTUPTYPE P_GEND_TYPE_NMDTUP_36 = 
  {
    3,
    P_GEND_TYPE_NMDTUP_NARR_36,
    P_GEND_TYPE_NMDTUP_TARR_36
  };
  PRT_TYPE P_GEND_TYPE_36 = 
  {
    PRT_KIND_NMDTUP,
    
    {
        (PRT_MAPTYPE *)&P_GEND_TYPE_NMDTUP_36
    }
  };
  PRT_VALUE P_GEND_VALUE_0 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_CANCEL
    }
  };
  PRT_VALUE P_GEND_VALUE_1 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_NODE_DOWN
    }
  };
  PRT_VALUE P_GEND_VALUE_2 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_PING
    }
  };
  PRT_VALUE P_GEND_VALUE_3 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_PONG
    }
  };
  PRT_VALUE P_GEND_VALUE_4 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_REGISTER_CLIENT
    }
  };
  PRT_VALUE P_GEND_VALUE_5 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_ROUND_DONE
    }
  };
  PRT_VALUE P_GEND_VALUE_6 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_START
    }
  };
  PRT_VALUE P_GEND_VALUE_7 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        P_EVENT_UNIT
    }
  };
  PRT_VALUE P_GEND_VALUE_8 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        0U
    }
  };
  PRT_VALUE P_GEND_VALUE_9 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        1U
    }
  };
  PRT_VALUE P_GEND_VALUE_10 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        2U
    }
  };
  PRT_VALUE P_GEND_VALUE_11 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        3U
    }
  };
  PRT_VALUE P_GEND_VALUE_12 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        100U
    }
  };
  PRT_VALUE P_GEND_VALUE_13 = 
  {
    PRT_VALUE_KIND_INT,
    
    {
        1000U
    }
  };
  PRT_VALUE P_GEND_VALUE_14 = 
  {
    PRT_VALUE_KIND_BOOL,
    
    {
        PRT_FALSE
    }
  };
  PRT_VALUE P_GEND_VALUE_15 = 
  {
    PRT_VALUE_KIND_EVENT,
    
    {
        PRT_SPECIAL_EVENT_HALT
    }
  };
  PRT_VALUE P_GEND_VALUE_16 = 
  {
    PRT_VALUE_KIND_NULL,
    
    {
        PRT_SPECIAL_EVENT_NULL
    }
  };
  PRT_VALUE P_GEND_VALUE_17 = 
  {
    PRT_VALUE_KIND_BOOL,
    
    {
        PRT_TRUE
    }
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
        P_EVENT_L_INIT,
        "L_INIT",
        4294967295U,
        &P_GEND_TYPE_6,
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
        P_EVENT_NODE_DOWN,
        "NODE_DOWN",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_PING,
        "PING",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_PONG,
        "PONG",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_REGISTER_CLIENT,
        "REGISTER_CLIENT",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_EVENT_ROUND_DONE,
        "ROUND_DONE",
        4294967295U,
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
    },
    
    {
        P_EVENT_UNREGISTER_CLIENT,
        "UNREGISTER_CLIENT",
        4294967295U,
        &P_GEND_TYPE_5,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_Driver[] = 
  {
    
    {
        P_VAR_Driver_container,
        P_MACHINE_Driver,
        "container",
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_VAR_Driver_fd,
        P_MACHINE_Driver,
        "fd",
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_VAR_Driver_i,
        P_MACHINE_Driver,
        "i",
        &P_GEND_TYPE_3,
        0U,
        NULL
    },
    
    {
        P_VAR_Driver_n,
        P_MACHINE_Driver,
        "n",
        &P_GEND_TYPE_5,
        0U,
        NULL
    },
    
    {
        P_VAR_Driver_nodemap,
        P_MACHINE_Driver,
        "nodemap",
        &P_GEND_TYPE_6,
        0U,
        NULL
    },
    
    {
        P_VAR_Driver_nodeseq,
        P_MACHINE_Driver,
        "nodeseq",
        &P_GEND_TYPE_25,
        0U,
        NULL
    }
  };
  PRT_VARDECL P_GEND_VARS_FailureDetector[] = 
  {
    
    {
        P_VAR_FailureDetector_alive,
        P_MACHINE_FailureDetector,
        "alive",
        &P_GEND_TYPE_6,
        0U,
        NULL
    },
    
    {
        P_VAR_FailureDetector_attempts,
        P_MACHINE_FailureDetector,
        "attempts",
        &P_GEND_TYPE_3,
        0U,
        NULL
    },
    
    {
        P_VAR_FailureDetector_clients,
        P_MACHINE_FailureDetector,
        "clients",
        &P_GEND_TYPE_6,
        0U,
        NULL
    },
    
    {
        P_VAR_FailureDetector_nodes,
        P_MACHINE_FailureDetector,
        "nodes",
        &P_GEND_TYPE_25,
        0U,
        NULL
    },
    
    {
        P_VAR_FailureDetector_responses,
        P_MACHINE_FailureDetector,
        "responses",
        &P_GEND_TYPE_6,
        0U,
        NULL
    },
    
    {
        P_VAR_FailureDetector_timer,
        P_MACHINE_FailureDetector,
        "timer",
        &P_GEND_TYPE_5,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_FailureDetector_Init[] = 
  {
    
    {
        0,
        P_STATE_FailureDetector_Init,
        P_MACHINE_FailureDetector,
        P_EVENT_UNIT,
        P_STATE_FailureDetector_SendPing,
        _P_FUN_PUSH_OR_IGN,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_FailureDetector_Reset[] = 
  {
    
    {
        0,
        P_STATE_FailureDetector_Reset,
        P_MACHINE_FailureDetector,
        P_EVENT_TIMEOUT,
        P_STATE_FailureDetector_SendPing,
        P_FUN_FailureDetector_ANON4,
        0U,
        NULL
    }
  };
  PRT_TRANSDECL P_GEND_TRANS_FailureDetector_SendPing[] = 
  {
    
    {
        0,
        P_STATE_FailureDetector_SendPing,
        P_MACHINE_FailureDetector,
        P_EVENT_ROUND_DONE,
        P_STATE_FailureDetector_Reset,
        P_FUN_FailureDetector_ANON4,
        0U,
        NULL
    },
    
    {
        1,
        P_STATE_FailureDetector_SendPing,
        P_MACHINE_FailureDetector,
        P_EVENT_UNIT,
        P_STATE_FailureDetector_SendPing,
        P_FUN_FailureDetector_ANON4,
        0U,
        NULL
    }
  };
  PRT_VALUE *P_FUN_CreateNode_IMPL(PRT_MACHINEINST *context);

  PRT_VALUE *P_FUN_FailureDetector_CancelTimer_IMPL(PRT_MACHINEINST *context);

  PRT_VALUE *P_FUN_FailureDetector_InitializeAliveSet_IMPL(PRT_MACHINEINST *context);

  PRT_VALUE *P_FUN_FailureDetector_Notify_IMPL(PRT_MACHINEINST *context);

  PRT_VALUE *P_FUN_FailureDetector_SendPingsToAliveSet_IMPL(PRT_MACHINEINST *context);

  PRT_DODECL P_GEND_DOS_Driver_Init[] = 
  {
    
    {
        0,
        P_STATE_Driver_Init,
        P_MACHINE_Driver,
        P_EVENT_NODE_DOWN,
        _P_FUN_PUSH_OR_IGN,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_FailureDetector_Init[] = 
  {
    
    {
        0,
        P_STATE_FailureDetector_Init,
        P_MACHINE_FailureDetector,
        P_EVENT_REGISTER_CLIENT,
        P_FUN_FailureDetector_ANON2,
        0U,
        NULL
    },
    
    {
        1,
        P_STATE_FailureDetector_Init,
        P_MACHINE_FailureDetector,
        P_EVENT_UNREGISTER_CLIENT,
        P_FUN_FailureDetector_ANON3,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_FailureDetector_Reset[] = 
  {
    
    {
        0,
        P_STATE_FailureDetector_Reset,
        P_MACHINE_FailureDetector,
        P_EVENT_PONG,
        _P_FUN_PUSH_OR_IGN,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_FailureDetector_SendPing[] = 
  {
    
    {
        0,
        P_STATE_FailureDetector_SendPing,
        P_MACHINE_FailureDetector,
        P_EVENT_PONG,
        P_FUN_FailureDetector_ANON9,
        0U,
        NULL
    },
    
    {
        1,
        P_STATE_FailureDetector_SendPing,
        P_MACHINE_FailureDetector,
        P_EVENT_TIMEOUT,
        P_FUN_FailureDetector_ANON5,
        0U,
        NULL
    }
  };
  PRT_DODECL P_GEND_DOS_Node_WaitPing[] = 
  {
    
    {
        0,
        P_STATE_Node_WaitPing,
        P_MACHINE_Node,
        P_EVENT_PING,
        P_FUN_Node_ANON1,
        0U,
        NULL
    }
  };
  PRT_VALUE *P_FUN_CreateNode_IMPL(PRT_MACHINEINST *context)
  {
    #line 6 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/driver.p"
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      #line 6
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 6
      p_tmp_ret = NULL;
      #line 6
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 8
      P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 1U, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(PrtCloneValue(PrtMkMachineRemote(context->process, P_MACHINE_Node, p_tmp_expr_0, p_tmp_frame.locals[0U])->id), PRT_TRUE, &P_GEND_VALUE_16, PRT_FALSE), PRT_FALSE);
      #line 9
      p_tmp_ret = PrtCloneValue(P_EXPR_0(p_tmp_frame.locals[1U], PRT_FALSE));
      goto P_EXIT_FUN;
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 10
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 10
      if (p_tmp_ret == NULL)
      {
        return PrtMkDefaultValue(&P_GEND_TYPE_14);
      }
      else
      {
        return p_tmp_ret;
      }
    }
  }

  PRT_VALUE *P_FUN_FailureDetector_CancelTimer_IMPL(PRT_MACHINEINST *context)
  {
    #line 74 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      PRT_VALUE *p_tmp_stmt_2;
      #line 74
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 74
      p_tmp_ret = NULL;
      #line 74
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 74
      if (p_tmp_frame.returnTo == 4U)
      {
        #line 74
        goto L4;
      }
      #line 76
      P_STMT_2(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_stmt_0), PrtSend(PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, p_tmp_stmt_2, PRT_FALSE)), &P_GEND_VALUE_16, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_0, PRT_FALSE), PRT_FALSE, P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_FailureDetector_timer], PRT_FALSE), PRT_FALSE);
      L4:
      #line 77
      if (p_tmp_frame.rcase == NULL && !PrtReceive(p_tmp_mach_priv, &p_tmp_frame, 4U))
      {
        #line 77
        return NULL;
      }
      #line 77
      PrtGetFunction(p_tmp_mach_priv, p_tmp_frame.rcase->funIndex)(context);
      #line 77
      if (p_tmp_mach_priv->receive != NULL)
      {
        #line 77
        PrtPushFrame(p_tmp_mach_priv, &p_tmp_frame);
        #line 77
        return NULL;
      }
      #line 77
      if (p_tmp_mach_priv->lastOperation != ReturnStatement)
      {
        #line 77
        goto P_EXIT_FUN;
      }
      #line 77
      p_tmp_frame.rcase = NULL;
      #line 77
      p_tmp_frame.returnTo = 0x0FFFFU;
      #line 81
      p_tmp_ret = PrtCloneValue(P_EXPR_0(p_tmp_frame.locals[0U], PRT_FALSE));
      goto P_EXIT_FUN;
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 82
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 82
      if (p_tmp_ret == NULL)
      {
        return PrtMkDefaultValue(NULL);
      }
      else
      {
        return p_tmp_ret;
      }
    }
  }

  PRT_VALUE *P_FUN_FailureDetector_InitializeAliveSet_IMPL(PRT_MACHINEINST *context)
  {
    #line 84 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_tuple;
      #line 84
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 84
      p_tmp_ret = NULL;
      #line 84
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 86
      P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 0U, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_8, PRT_FALSE), PRT_FALSE);
      #line 87
      while (P_BOOL_EXPR(P_EXPR_3(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_0) < PrtPrimGetInt(p_tmp_expr_2)), PRT_TRUE, PrtMkIntValue(PrtSeqSizeOf(p_tmp_expr_1)), PRT_TRUE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_nodes], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_TRUE))
      {
        #line 88
        P_STMT_0(PrtMapInsertEx(p_tmp_mach_priv->varValues[P_VAR_FailureDetector_alive], PrtTupleGetNC(p_tmp_stmt_0, 0U), PRT_FALSE, PrtTupleGetNC(p_tmp_stmt_0, 1U), PRT_FALSE), P_EXPR_4(P_TUPLE_1(&P_GEND_TYPE_35, p_tmp_expr_3, p_tmp_expr_2), PRT_TRUE, PrtSeqGetNC(p_tmp_expr_1, p_tmp_expr_0), PRT_FALSE, &P_GEND_VALUE_17, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_nodes], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
        #line 88
        PrtFree(p_tmp_stmt_0->valueUnion.tuple->values);
        #line 88
        PrtFree(p_tmp_stmt_0->valueUnion.tuple);
        #line 88
        PrtFree(p_tmp_stmt_0);
        #line 89
        P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 0U, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_9, PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
        #line 87
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 91
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 91
      return p_tmp_ret;
    }
  }

  PRT_VALUE *P_FUN_FailureDetector_Notify_IMPL(PRT_MACHINEINST *context)
  {
    #line 105 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_expr_5;
      PRT_VALUE *p_tmp_expr_6;
      PRT_VALUE *p_tmp_expr_7;
      PRT_VALUE *p_tmp_expr_8;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      #line 105
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 105
      p_tmp_ret = NULL;
      #line 105
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 105
      if (p_tmp_frame.returnTo == 6U)
      {
        #line 105
        goto L6;
      }
      #line 107
      P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 0U, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_8, PRT_FALSE), PRT_FALSE);
      #line 108
      while (P_BOOL_EXPR(P_EXPR_3(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_0) < PrtPrimGetInt(p_tmp_expr_2)), PRT_TRUE, PrtMkIntValue(PrtSeqSizeOf(p_tmp_expr_1)), PRT_TRUE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_nodes], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_TRUE))
      {
        #line 109
        if (P_BOOL_EXPR(P_EXPR_8(PrtMkBoolValue(PrtPrimGetBool(p_tmp_expr_5) && PrtPrimGetBool(p_tmp_expr_7)), PRT_TRUE, PrtMkBoolValue(!PrtPrimGetBool(p_tmp_expr_6)), PRT_TRUE, PrtMkBoolValue(PrtMapExists(p_tmp_expr_3, p_tmp_expr_4)), PRT_TRUE, PrtMkBoolValue(PrtMapExists(p_tmp_expr_0, p_tmp_expr_4)), PRT_TRUE, PrtSeqGetNC(p_tmp_expr_2, p_tmp_expr_1), PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_responses], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_nodes], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_alive], PRT_FALSE), PRT_TRUE))
        {
          #line 110
          P_STMT_0(PrtMapRemove(p_tmp_mach_priv->varValues[P_VAR_FailureDetector_alive], p_tmp_stmt_0), P_EXPR_2(PrtSeqGetNC(p_tmp_expr_1, p_tmp_expr_0), PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_nodes], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
          #line 111
          P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 1U, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_8, PRT_FALSE), PRT_FALSE);
          #line 112
          while (P_BOOL_EXPR(P_EXPR_3(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_1) < PrtPrimGetInt(p_tmp_expr_2)), PRT_TRUE, PrtMkIntValue(PrtMapSizeOf(p_tmp_expr_0)), PRT_TRUE, p_tmp_frame.locals[1U], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_clients], PRT_FALSE), PRT_TRUE))
          {
            #line 113
            P_EXPR_8(PrtPushNewFrame(p_tmp_mach_priv, 3U, PRT_FUN_PARAM_CLONE, p_tmp_expr_7, PRT_FUN_PARAM_CLONE, p_tmp_expr_0, PRT_FUN_PARAM_CLONE, p_tmp_expr_5), PRT_FALSE, PrtSeqGet(p_tmp_expr_6, p_tmp_expr_3), PRT_TRUE, PrtMapGetKeys(p_tmp_expr_1), PRT_TRUE, PrtSeqGetNC(p_tmp_expr_4, p_tmp_expr_2), PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_nodes], PRT_FALSE, p_tmp_frame.locals[1U], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_clients], PRT_FALSE, &P_GEND_VALUE_1, PRT_FALSE);
            L6:
            #line 113
            p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 6U, p_tmp_mach_priv, 3U);
            #line 113
            if (p_tmp_mach_priv->receive != NULL)
            {
              #line 113
              return p_tmp_funstmt_ret;
            }
            #line 113
            if (p_tmp_mach_priv->lastOperation != ReturnStatement)
            {
              #line 113
              goto P_EXIT_FUN;
            }
            #line 113
            if (p_tmp_funstmt_ret != NULL)
            {
              #line 113
              PrtFreeValue(p_tmp_funstmt_ret);
            }
            #line 114
            P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 1U, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_9, PRT_FALSE, p_tmp_frame.locals[1U], PRT_FALSE), PRT_FALSE);
            #line 112
          }
        }
        #line 117
        P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 0U, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_9, PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
        #line 108
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 119
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 119
      return p_tmp_ret;
    }
  }

  PRT_VALUE *P_FUN_FailureDetector_SendPingsToAliveSet_IMPL(PRT_MACHINEINST *context)
  {
    #line 93 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_expr_5;
      PRT_VALUE *p_tmp_expr_6;
      PRT_VALUE *p_tmp_expr_7;
      PRT_VALUE *p_tmp_expr_8;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      #line 93
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 93
      p_tmp_ret = NULL;
      #line 93
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 93
      if (p_tmp_frame.returnTo == 5U)
      {
        #line 93
        goto L5;
      }
      #line 95
      P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 0U, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_8, PRT_FALSE), PRT_FALSE);
      #line 96
      while (P_BOOL_EXPR(P_EXPR_3(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_0) < PrtPrimGetInt(p_tmp_expr_2)), PRT_TRUE, PrtMkIntValue(PrtSeqSizeOf(p_tmp_expr_1)), PRT_TRUE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_nodes], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_TRUE))
      {
        #line 97
        if (P_BOOL_EXPR(P_EXPR_8(PrtMkBoolValue(PrtPrimGetBool(p_tmp_expr_5) && PrtPrimGetBool(p_tmp_expr_7)), PRT_TRUE, PrtMkBoolValue(!PrtPrimGetBool(p_tmp_expr_6)), PRT_TRUE, PrtMkBoolValue(PrtMapExists(p_tmp_expr_3, p_tmp_expr_4)), PRT_TRUE, PrtMkBoolValue(PrtMapExists(p_tmp_expr_0, p_tmp_expr_4)), PRT_TRUE, PrtSeqGetNC(p_tmp_expr_2, p_tmp_expr_1), PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_responses], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_nodes], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_alive], PRT_FALSE), PRT_TRUE))
        {
          #line 99
          P_EXPR_5(PrtPushNewFrame(p_tmp_mach_priv, 3U, PRT_FUN_PARAM_CLONE, p_tmp_expr_4, PRT_FUN_PARAM_CLONE, p_tmp_expr_0, PRT_FUN_PARAM_CLONE, p_tmp_expr_3), PRT_FALSE, PrtSeqGetNC(p_tmp_expr_2, p_tmp_expr_1), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_nodes], PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE, &P_GEND_VALUE_2, PRT_FALSE);
          L5:
          #line 99
          p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 5U, p_tmp_mach_priv, 3U);
          #line 99
          if (p_tmp_mach_priv->receive != NULL)
          {
            #line 99
            return p_tmp_funstmt_ret;
          }
          #line 99
          if (p_tmp_mach_priv->lastOperation != ReturnStatement)
          {
            #line 99
            goto P_EXIT_FUN;
          }
          #line 99
          if (p_tmp_funstmt_ret != NULL)
          {
            #line 99
            PrtFreeValue(p_tmp_funstmt_ret);
          }
        }
        #line 101
        P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 0U, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_9, PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
        #line 96
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 103
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 103
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_Container_ANON0_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Driver_ANON0_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Driver_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    #line 20 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/driver.p"
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_expr_5;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      PRT_VALUE *p_tmp_stmt_2;
      PRT_VALUE *p_tmp_tuple;
      #line 20
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 20
      p_tmp_ret = NULL;
      #line 20
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 20
      if (p_tmp_frame.returnTo == 0U)
      {
        #line 20
        goto L0;
      }
      #line 20
      if (p_tmp_frame.returnTo == 1U)
      {
        #line 20
        goto L1;
      }
      #line 20
      if (p_tmp_frame.returnTo == 2U)
      {
        #line 20
        goto L2;
      }
      #line 21
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Driver_i, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_8, PRT_FALSE), PRT_FALSE);
      #line 22
      while (P_BOOL_EXPR(P_EXPR_2(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_0) < PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_10, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_Driver_i], PRT_FALSE), PRT_TRUE))
      {
        PrtPushNewFrame(p_tmp_mach_priv, 2U);
        L0:
        #line 23
        p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 0U, p_tmp_mach_priv, 2U);
        #line 23
        if (p_tmp_mach_priv->receive != NULL)
        {
          #line 23
          return p_tmp_funstmt_ret;
        }
        #line 23
        if (p_tmp_mach_priv->lastOperation != ReturnStatement)
        {
          #line 23
          goto P_EXIT_FUN;
        }
        #line 23
        PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Driver_container, p_tmp_funstmt_ret, PRT_FALSE);
        #line 24
        P_EXPR_1(PrtPushNewFrame(p_tmp_mach_priv, 1U, PRT_FUN_PARAM_CLONE, p_tmp_expr_0), PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_Driver_container], PRT_FALSE);
        L1:
        #line 24
        p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 1U, p_tmp_mach_priv, 1U);
        #line 24
        if (p_tmp_mach_priv->receive != NULL)
        {
          #line 24
          return p_tmp_funstmt_ret;
        }
        #line 24
        if (p_tmp_mach_priv->lastOperation != ReturnStatement)
        {
          #line 24
          goto P_EXIT_FUN;
        }
        #line 24
        PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Driver_n, p_tmp_funstmt_ret, PRT_FALSE);
        #line 25
        P_STMT_0(PrtSeqInsertEx(p_tmp_mach_priv->varValues[P_VAR_Driver_nodeseq], PrtTupleGetNC(p_tmp_stmt_0, 0U), PrtTupleGetNC(p_tmp_stmt_0, 1U), PRT_FALSE), P_EXPR_2(P_TUPLE_1(&P_GEND_TYPE_34, p_tmp_expr_0, p_tmp_expr_1), PRT_TRUE, p_tmp_mach_priv->varValues[P_VAR_Driver_n], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_Driver_i], PRT_FALSE), PRT_FALSE);
        #line 25
        PrtFree(PrtTupleGetNC(p_tmp_stmt_0, 0U));
        #line 25
        PrtFree(p_tmp_stmt_0->valueUnion.tuple->values);
        #line 25
        PrtFree(p_tmp_stmt_0->valueUnion.tuple);
        #line 25
        PrtFree(p_tmp_stmt_0);
        #line 26
        P_STMT_0(PrtMapInsertEx(p_tmp_mach_priv->varValues[P_VAR_Driver_nodemap], PrtTupleGetNC(p_tmp_stmt_0, 0U), PRT_FALSE, PrtTupleGetNC(p_tmp_stmt_0, 1U), PRT_FALSE), P_EXPR_2(P_TUPLE_1(&P_GEND_TYPE_35, p_tmp_expr_0, p_tmp_expr_1), PRT_TRUE, &P_GEND_VALUE_17, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_Driver_n], PRT_FALSE), PRT_FALSE);
        #line 26
        PrtFree(p_tmp_stmt_0->valueUnion.tuple->values);
        #line 26
        PrtFree(p_tmp_stmt_0->valueUnion.tuple);
        #line 26
        PrtFree(p_tmp_stmt_0);
        #line 27
        P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Driver_i, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_9, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_Driver_i], PRT_FALSE), PRT_FALSE);
        #line 22
      }
      #line 30
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Driver_fd, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(PrtCloneValue(PrtMkMachine(context->process, P_MACHINE_FailureDetector, p_tmp_expr_0)->id), PRT_TRUE, p_tmp_mach_priv->varValues[P_VAR_Driver_nodeseq], PRT_FALSE), PRT_FALSE);
      #line 31
      P_STMT_2(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_stmt_0), PrtSend(PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, p_tmp_stmt_2, PRT_FALSE)), P_EXPR_0(p_tmp_mach_priv->id, PRT_FALSE), PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_4, PRT_FALSE), PRT_FALSE, P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_Driver_fd], PRT_FALSE), PRT_FALSE);
      #line 32
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Driver_i, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_8, PRT_FALSE), PRT_FALSE);
      #line 33
      while (P_BOOL_EXPR(P_EXPR_2(PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_0) < PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_10, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_Driver_i], PRT_FALSE), PRT_TRUE))
      {
        #line 34
        P_EXPR_5(PrtPushNewFrame(p_tmp_mach_priv, 3U, PRT_FUN_PARAM_CLONE, p_tmp_expr_4, PRT_FUN_PARAM_CLONE, p_tmp_expr_2, PRT_FUN_PARAM_CLONE, p_tmp_expr_3), PRT_FALSE, PrtSeqGetNC(p_tmp_expr_1, p_tmp_expr_0), PRT_FALSE, &P_GEND_VALUE_16, PRT_FALSE, &P_GEND_VALUE_15, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_Driver_nodeseq], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_Driver_i], PRT_FALSE);
        L2:
        #line 34
        p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 2U, p_tmp_mach_priv, 3U);
        #line 34
        if (p_tmp_mach_priv->receive != NULL)
        {
          #line 34
          return p_tmp_funstmt_ret;
        }
        #line 34
        if (p_tmp_mach_priv->lastOperation != ReturnStatement)
        {
          #line 34
          goto P_EXIT_FUN;
        }
        #line 34
        if (p_tmp_funstmt_ret != NULL)
        {
          #line 34
          PrtFreeValue(p_tmp_funstmt_ret);
        }
        #line 35
        P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_Driver_i, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_9, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_Driver_i], PRT_FALSE), PRT_FALSE);
        #line 33
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 37
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 37
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_FailureDetector_ANON0_IMPL(PRT_MACHINEINST *context)
  {
    #line 79 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      #line 79
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 79
      p_tmp_ret = NULL;
      #line 79
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 79
      P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 0U, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_14, PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 79
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 79
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_FailureDetector_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    #line 78 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      #line 78
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 78
      p_tmp_ret = NULL;
      #line 78
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 78
      P_STMT_0(PrtSetLocalVarEx(p_tmp_frame.locals, 0U, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_17, PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 78
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 78
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_FailureDetector_ANON2_IMPL(PRT_MACHINEINST *context)
  {
    #line 26 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      #line 26
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 26
      p_tmp_ret = NULL;
      #line 26
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 26
      P_STMT_1(PrtMapUpdateEx(p_tmp_mach_priv->varValues[P_VAR_FailureDetector_clients], p_tmp_stmt_1, PRT_TRUE, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_17, PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 26
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 26
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_FailureDetector_ANON3_IMPL(PRT_MACHINEINST *context)
  {
    #line 27 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      #line 27
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 27
      p_tmp_ret = NULL;
      #line 27
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 27
      if (P_BOOL_EXPR(P_EXPR_2(PrtMkBoolValue(PrtMapExists(p_tmp_expr_0, p_tmp_expr_1)), PRT_TRUE, p_tmp_frame.locals[0U], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_clients], PRT_FALSE), PRT_TRUE))
      {
        #line 27
        P_STMT_0(PrtMapRemove(p_tmp_mach_priv->varValues[P_VAR_FailureDetector_clients], p_tmp_stmt_0), P_EXPR_0(p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 27
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 27
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_FailureDetector_ANON4_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_FailureDetector_ANON5_IMPL(PRT_MACHINEINST *context)
  {
    #line 49 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_expr_5;
      PRT_VALUE *p_tmp_expr_6;
      PRT_VALUE *p_tmp_expr_7;
      PRT_VALUE *p_tmp_expr_8;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      #line 49
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 49
      p_tmp_ret = NULL;
      #line 49
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 49
      if (p_tmp_frame.returnTo == 3U)
      {
        #line 49
        goto L3;
      }
      #line 50
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_FailureDetector_attempts, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_2(PrtMkIntValue(PrtPrimGetInt(p_tmp_expr_0) + PrtPrimGetInt(p_tmp_expr_1)), PRT_TRUE, &P_GEND_VALUE_9, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_attempts], PRT_FALSE), PRT_FALSE);
      #line 51
      if (P_BOOL_EXPR(P_EXPR_8(PrtMkBoolValue(PrtPrimGetBool(p_tmp_expr_7) && PrtPrimGetBool(p_tmp_expr_4)), PRT_TRUE, PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_6) < PrtPrimGetInt(p_tmp_expr_5)), PRT_TRUE, PrtMkIntValue(PrtMapSizeOf(p_tmp_expr_2)), PRT_TRUE, PrtMkIntValue(PrtMapSizeOf(p_tmp_expr_0)), PRT_TRUE, PrtMkBoolValue(PrtPrimGetInt(p_tmp_expr_1) < PrtPrimGetInt(p_tmp_expr_3)), PRT_TRUE, &P_GEND_VALUE_10, PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_responses], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_attempts], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_alive], PRT_FALSE), PRT_TRUE))
      {
        #line 53
        P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_16, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_7, PRT_FALSE), PRT_FALSE);
        goto P_EXIT_FUN;
      }
      else
      {
        PrtPushNewFrame(p_tmp_mach_priv, 16U);
        L3:
        #line 55
        p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 3U, p_tmp_mach_priv, 16U);
        #line 55
        if (p_tmp_mach_priv->receive != NULL)
        {
          #line 55
          return p_tmp_funstmt_ret;
        }
        #line 55
        if (p_tmp_mach_priv->lastOperation != ReturnStatement)
        {
          #line 55
          goto P_EXIT_FUN;
        }
        #line 55
        if (p_tmp_funstmt_ret != NULL)
        {
          #line 55
          PrtFreeValue(p_tmp_funstmt_ret);
        }
        #line 57
        P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_16, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_5, PRT_FALSE), PRT_FALSE);
        goto P_EXIT_FUN;
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 59
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 59
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_FailureDetector_ANON6_IMPL(PRT_MACHINEINST *context)
  {
    #line 65 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      PRT_VALUE *p_tmp_stmt_2;
      #line 65
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 65
      p_tmp_ret = NULL;
      #line 65
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 66
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_FailureDetector_attempts, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(&P_GEND_VALUE_8, PRT_FALSE), PRT_FALSE);
      #line 67
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_FailureDetector_responses, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_0(PrtMkDefaultValue(&P_GEND_TYPE_6), PRT_TRUE), PRT_FALSE);
      #line 68
      P_STMT_2(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_stmt_0), PrtSend(PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, p_tmp_stmt_2, PRT_FALSE)), P_EXPR_0(&P_GEND_VALUE_13, PRT_FALSE), PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_6, PRT_FALSE), PRT_FALSE, P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_FailureDetector_timer], PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 69
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 69
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_FailureDetector_ANON7_IMPL(PRT_MACHINEINST *context)
  {
    #line 20 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      #line 20
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 20
      p_tmp_ret = NULL;
      #line 20
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 20
      if (p_tmp_frame.returnTo == 0U)
      {
        #line 20
        goto L0;
      }
      #line 21
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_FailureDetector_nodes, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE);
      PrtPushNewFrame(p_tmp_mach_priv, 15U);
      L0:
      #line 22
      p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 0U, p_tmp_mach_priv, 15U);
      #line 22
      if (p_tmp_mach_priv->receive != NULL)
      {
        #line 22
        return p_tmp_funstmt_ret;
      }
      #line 22
      if (p_tmp_mach_priv->lastOperation != ReturnStatement)
      {
        #line 22
        goto P_EXIT_FUN;
      }
      #line 22
      if (p_tmp_funstmt_ret != NULL)
      {
        #line 22
        PrtFreeValue(p_tmp_funstmt_ret);
      }
      #line 23
      P_STMT_0(PrtSetGlobalVarEx(p_tmp_mach_priv, P_VAR_FailureDetector_timer, p_tmp_stmt_0, !PRT_TRUE), P_EXPR_1(PrtCloneValue(PrtMkModel(context->process, P_MODEL_Timer, p_tmp_expr_0)->id), PRT_TRUE, p_tmp_mach_priv->id, PRT_FALSE), PRT_FALSE);
      #line 24
      P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_16, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_7, PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 25
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 25
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_FailureDetector_ANON8_IMPL(PRT_MACHINEINST *context)
  {
    #line 32 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      PRT_VALUE *p_tmp_stmt_2;
      #line 32
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 32
      p_tmp_ret = NULL;
      #line 32
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 32
      if (p_tmp_frame.returnTo == 1U)
      {
        #line 32
        goto L1;
      }
      PrtPushNewFrame(p_tmp_mach_priv, 17U);
      L1:
      #line 33
      p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 1U, p_tmp_mach_priv, 17U);
      #line 33
      if (p_tmp_mach_priv->receive != NULL)
      {
        #line 33
        return p_tmp_funstmt_ret;
      }
      #line 33
      if (p_tmp_mach_priv->lastOperation != ReturnStatement)
      {
        #line 33
        goto P_EXIT_FUN;
      }
      #line 33
      if (p_tmp_funstmt_ret != NULL)
      {
        #line 33
        PrtFreeValue(p_tmp_funstmt_ret);
      }
      #line 34
      P_STMT_2(P_SEQ(PrtCheckIsLocalMachineId(context, p_tmp_stmt_0), PrtSend(PrtGetMachine(context->process, p_tmp_stmt_0), p_tmp_stmt_1, p_tmp_stmt_2, PRT_FALSE)), P_EXPR_0(&P_GEND_VALUE_12, PRT_FALSE), PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_6, PRT_FALSE), PRT_FALSE, P_EXPR_0(p_tmp_mach_priv->varValues[P_VAR_FailureDetector_timer], PRT_FALSE), PRT_FALSE);
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 35
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 35
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_FailureDetector_ANON9_IMPL(PRT_MACHINEINST *context)
  {
    #line 36 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_BOOLEAN p_tmp_bool;
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      PRT_VALUE *p_tmp_stmt_0;
      PRT_VALUE *p_tmp_stmt_1;
      #line 36
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 36
      p_tmp_ret = NULL;
      #line 36
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 36
      if (p_tmp_frame.returnTo == 2U)
      {
        #line 36
        goto L2;
      }
      #line 38
      if (P_BOOL_EXPR(P_EXPR_2(PrtMkBoolValue(PrtMapExists(p_tmp_expr_0, p_tmp_expr_1)), PRT_TRUE, p_tmp_frame.locals[0U], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_alive], PRT_FALSE), PRT_TRUE))
      {
        #line 39
        P_STMT_1(PrtMapUpdateEx(p_tmp_mach_priv->varValues[P_VAR_FailureDetector_responses], p_tmp_stmt_1, PRT_TRUE, p_tmp_stmt_0, !PRT_FALSE), P_EXPR_0(p_tmp_frame.locals[0U], PRT_FALSE), PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_17, PRT_FALSE), PRT_FALSE);
        #line 40
        if (P_BOOL_EXPR(P_EXPR_4(PrtMkBoolValue(PrtIsEqualValue(p_tmp_expr_3, p_tmp_expr_2)), PRT_TRUE, PrtMkIntValue(PrtMapSizeOf(p_tmp_expr_1)), PRT_TRUE, PrtMkIntValue(PrtMapSizeOf(p_tmp_expr_0)), PRT_TRUE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_responses], PRT_FALSE, p_tmp_mach_priv->varValues[P_VAR_FailureDetector_alive], PRT_FALSE), PRT_TRUE))
        {
          PrtPushNewFrame(p_tmp_mach_priv, 14U);
          L2:
          #line 41
          p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 2U, p_tmp_mach_priv, 14U);
          #line 41
          if (p_tmp_mach_priv->receive != NULL)
          {
            #line 41
            return p_tmp_funstmt_ret;
          }
          #line 41
          if (p_tmp_mach_priv->lastOperation != ReturnStatement)
          {
            #line 41
            goto P_EXIT_FUN;
          }
          #line 41
          PrtSetLocalVarEx(p_tmp_frame.locals, 1U, p_tmp_funstmt_ret, PRT_FALSE);
          #line 42
          if (P_BOOL_EXPR(P_EXPR_0(p_tmp_frame.locals[1U], PRT_FALSE), PRT_FALSE))
          {
            #line 44
            P_STMT_1(PrtRaise(p_tmp_mach_priv, p_tmp_stmt_0, p_tmp_stmt_1), &P_GEND_VALUE_16, PRT_FALSE, P_EXPR_0(&P_GEND_VALUE_7, PRT_FALSE), PRT_FALSE);
            goto P_EXIT_FUN;
          }
        }
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 48
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 48
      return p_tmp_ret;
    }
  }

  static PRT_VALUE *P_FUN_Node_ANON0_IMPL(PRT_MACHINEINST *context)
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

  static PRT_VALUE *P_FUN_Node_ANON1_IMPL(PRT_MACHINEINST *context)
  {
    #line 124 "file:///d:/git/p-org/p/src/prtdist/samples/techfest2015-demo/failuredetector.p"
    {
      PRT_FUNSTACK_INFO p_tmp_frame;
      PRT_MACHINEINST_PRIV *p_tmp_mach_priv;
      PRT_VALUE *p_tmp_expr_0;
      PRT_VALUE *p_tmp_expr_1;
      PRT_VALUE *p_tmp_expr_2;
      PRT_VALUE *p_tmp_expr_3;
      PRT_VALUE *p_tmp_expr_4;
      PRT_VALUE *p_tmp_funstmt_ret;
      PRT_VALUE *p_tmp_ret;
      #line 124
      p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
      #line 124
      p_tmp_ret = NULL;
      #line 124
      PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
      #line 124
      if (p_tmp_frame.returnTo == 7U)
      {
        #line 124
        goto L7;
      }
      #line 126
      P_EXPR_4(PrtPushNewFrame(p_tmp_mach_priv, 3U, PRT_FUN_PARAM_CLONE, p_tmp_expr_3, PRT_FUN_PARAM_CLONE, p_tmp_expr_0, PRT_FUN_PARAM_CLONE, p_tmp_expr_2), PRT_FALSE, PrtCastValue(p_tmp_expr_1, &P_GEND_TYPE_5), PRT_FALSE, p_tmp_mach_priv->id, PRT_FALSE, p_tmp_frame.locals[0U], PRT_FALSE, &P_GEND_VALUE_3, PRT_FALSE);
      L7:
      #line 126
      p_tmp_funstmt_ret = PrtWrapFunStmt(&p_tmp_frame, 7U, p_tmp_mach_priv, 3U);
      #line 126
      if (p_tmp_mach_priv->receive != NULL)
      {
        #line 126
        return p_tmp_funstmt_ret;
      }
      #line 126
      if (p_tmp_mach_priv->lastOperation != ReturnStatement)
      {
        #line 126
        goto P_EXIT_FUN;
      }
      #line 126
      if (p_tmp_funstmt_ret != NULL)
      {
        #line 126
        PrtFreeValue(p_tmp_funstmt_ret);
      }
      goto P_EXIT_FUN;
      P_EXIT_FUN:
      #line 127
      PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
      #line 127
      return p_tmp_ret;
    }
  }

  PRT_CASEDECL P_GEND_CASES_4[] = 
  {
    
    {
        P_EVENT_CANCEL_FAILURE,
        4U
    },
    
    {
        P_EVENT_CANCEL_SUCCESS,
        5U
    }
  };
  PRT_UINT32 P_GEND_EVENTSET_0[] = 
  {
    0x0U
  };
  PRT_UINT32 P_GEND_EVENTSET_1[] = 
  {
    0x18U
  };
  PRT_UINT32 P_GEND_EVENTSET_2[] = 
  {
    0x100U
  };
  PRT_UINT32 P_GEND_EVENTSET_3[] = 
  {
    0x200U
  };
  PRT_UINT32 P_GEND_EVENTSET_4[] = 
  {
    0x400U
  };
  PRT_UINT32 P_GEND_EVENTSET_5[] = 
  {
    0x4000U
  };
  PRT_UINT32 P_GEND_EVENTSET_6[] = 
  {
    0x4400U
  };
  PRT_UINT32 P_GEND_EVENTSET_7[] = 
  {
    0x08000U
  };
  PRT_UINT32 P_GEND_EVENTSET_8[] = 
  {
    0x09000U
  };
  PRT_UINT32 P_GEND_EVENTSET_9[] = 
  {
    0x10800U
  };
  PRT_EVENTSETDECL P_GEND_EVENTSETS[] = 
  {
    
    {
        0,
        P_GEND_EVENTSET_0
    },
    
    {
        1,
        P_GEND_EVENTSET_1
    },
    
    {
        2,
        P_GEND_EVENTSET_2
    },
    
    {
        3,
        P_GEND_EVENTSET_3
    },
    
    {
        4,
        P_GEND_EVENTSET_4
    },
    
    {
        5,
        P_GEND_EVENTSET_5
    },
    
    {
        6,
        P_GEND_EVENTSET_6
    },
    
    {
        7,
        P_GEND_EVENTSET_7
    },
    
    {
        8,
        P_GEND_EVENTSET_8
    },
    
    {
        9,
        P_GEND_EVENTSET_9
    }
  };
  PRT_RECEIVEDECL P_GEND_RECEIVE_P_FUN_FailureDetector_CancelTimer[] = 
  {
    
    {
        4U,
        1U,
        2U,
        P_GEND_CASES_4
    }
  };
  PRT_FUNDECL P_GEND_FUNS_Container[] = 
  {
    
    {
        _P_FUN_PUSH_OR_IGN,
        P_MACHINE_Container,
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
        P_FUN_CreateNode,
        P_MACHINE_Container,
        "CreateNode",
        &P_FUN_CreateNode_IMPL,
        2U,
        0U,
        &P_GEND_TYPE_17,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN__CREATECONTAINER,
        P_MACHINE_Container,
        "_CREATECONTAINER",
        &P_FUN__CREATECONTAINER_IMPL,
        1U,
        0U,
        &P_GEND_TYPE_20,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN__SEND,
        P_MACHINE_Container,
        "_SEND",
        &P_FUN__SEND_IMPL,
        3U,
        0U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Container_ANON0,
        P_MACHINE_Container,
        NULL,
        &P_FUN_Container_ANON0_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    }
  };
  PRT_FUNDECL P_GEND_FUNS_Driver[] = 
  {
    
    {
        _P_FUN_PUSH_OR_IGN,
        P_MACHINE_Driver,
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
        P_FUN_CreateNode,
        P_MACHINE_Driver,
        "CreateNode",
        &P_FUN_CreateNode_IMPL,
        2U,
        0U,
        &P_GEND_TYPE_17,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN__CREATECONTAINER,
        P_MACHINE_Driver,
        "_CREATECONTAINER",
        &P_FUN__CREATECONTAINER_IMPL,
        1U,
        0U,
        &P_GEND_TYPE_20,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN__SEND,
        P_MACHINE_Driver,
        "_SEND",
        &P_FUN__SEND_IMPL,
        3U,
        0U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Driver_ANON0,
        P_MACHINE_Driver,
        NULL,
        &P_FUN_Driver_ANON0_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Driver_ANON1,
        P_MACHINE_Driver,
        NULL,
        &P_FUN_Driver_ANON1_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    }
  };
  PRT_FUNDECL P_GEND_FUNS_FailureDetector[] = 
  {
    
    {
        _P_FUN_PUSH_OR_IGN,
        P_MACHINE_FailureDetector,
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
        P_FUN_CreateNode,
        P_MACHINE_FailureDetector,
        "CreateNode",
        &P_FUN_CreateNode_IMPL,
        2U,
        0U,
        &P_GEND_TYPE_17,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN__CREATECONTAINER,
        P_MACHINE_FailureDetector,
        "_CREATECONTAINER",
        &P_FUN__CREATECONTAINER_IMPL,
        1U,
        0U,
        &P_GEND_TYPE_20,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN__SEND,
        P_MACHINE_FailureDetector,
        "_SEND",
        &P_FUN__SEND_IMPL,
        3U,
        0U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON0,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON0_IMPL,
        1U,
        2U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON1,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON1_IMPL,
        1U,
        2U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON2,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON2_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON3,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON3_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON4,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON4_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON5,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON5_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON6,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON6_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON7,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON7_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON8,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON8_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_ANON9,
        P_MACHINE_FailureDetector,
        NULL,
        &P_FUN_FailureDetector_ANON9_IMPL,
        2U,
        1U,
        &P_GEND_TYPE_21,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_CancelTimer,
        P_MACHINE_FailureDetector,
        "CancelTimer",
        &P_FUN_FailureDetector_CancelTimer_IMPL,
        2U,
        0U,
        &P_GEND_TYPE_21,
        1U,
        P_GEND_RECEIVE_P_FUN_FailureDetector_CancelTimer,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_InitializeAliveSet,
        P_MACHINE_FailureDetector,
        "InitializeAliveSet",
        &P_FUN_FailureDetector_InitializeAliveSet_IMPL,
        1U,
        0U,
        &P_GEND_TYPE_15,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_Notify,
        P_MACHINE_FailureDetector,
        "Notify",
        &P_FUN_FailureDetector_Notify_IMPL,
        2U,
        0U,
        &P_GEND_TYPE_29,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_FailureDetector_SendPingsToAliveSet,
        P_MACHINE_FailureDetector,
        "SendPingsToAliveSet",
        &P_FUN_FailureDetector_SendPingsToAliveSet_IMPL,
        1U,
        0U,
        &P_GEND_TYPE_15,
        0U,
        NULL,
        0U,
        NULL
    }
  };
  PRT_FUNDECL P_GEND_FUNS_Node[] = 
  {
    
    {
        _P_FUN_PUSH_OR_IGN,
        P_MACHINE_Node,
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
        P_FUN_CreateNode,
        P_MACHINE_Node,
        "CreateNode",
        &P_FUN_CreateNode_IMPL,
        2U,
        0U,
        &P_GEND_TYPE_17,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN__CREATECONTAINER,
        P_MACHINE_Node,
        "_CREATECONTAINER",
        &P_FUN__CREATECONTAINER_IMPL,
        1U,
        0U,
        &P_GEND_TYPE_20,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN__SEND,
        P_MACHINE_Node,
        "_SEND",
        &P_FUN__SEND_IMPL,
        3U,
        0U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Node_ANON0,
        P_MACHINE_Node,
        NULL,
        &P_FUN_Node_ANON0_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    },
    
    {
        P_FUN_Node_ANON1,
        P_MACHINE_Node,
        NULL,
        &P_FUN_Node_ANON1_IMPL,
        1U,
        1U,
        NULL,
        0U,
        NULL,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_Container[] = 
  {
    
    {
        P_STATE_Container_Init,
        P_MACHINE_Container,
        "Init",
        0,
        0,
        0,
        0,
        0,
        NULL,
        NULL,
        P_FUN_Container_ANON0,
        P_FUN_Container_ANON0,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_Driver[] = 
  {
    
    {
        P_STATE_Driver_Init,
        P_MACHINE_Driver,
        "Init",
        0,
        1,
        0,
        0,
        2,
        NULL,
        P_GEND_DOS_Driver_Init,
        P_FUN_Driver_ANON1,
        P_FUN_Driver_ANON0,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_FailureDetector[] = 
  {
    
    {
        P_STATE_FailureDetector_Init,
        P_MACHINE_FailureDetector,
        "Init",
        1,
        2,
        0,
        7,
        9,
        P_GEND_TRANS_FailureDetector_Init,
        P_GEND_DOS_FailureDetector_Init,
        P_FUN_FailureDetector_ANON7,
        P_FUN_FailureDetector_ANON4,
        0U,
        NULL
    },
    
    {
        P_STATE_FailureDetector_Reset,
        P_MACHINE_FailureDetector,
        "Reset",
        1,
        1,
        0,
        5,
        4,
        P_GEND_TRANS_FailureDetector_Reset,
        P_GEND_DOS_FailureDetector_Reset,
        P_FUN_FailureDetector_ANON6,
        P_FUN_FailureDetector_ANON4,
        0U,
        NULL
    },
    
    {
        P_STATE_FailureDetector_SendPing,
        P_MACHINE_FailureDetector,
        "SendPing",
        2,
        2,
        0,
        8,
        6,
        P_GEND_TRANS_FailureDetector_SendPing,
        P_GEND_DOS_FailureDetector_SendPing,
        P_FUN_FailureDetector_ANON8,
        P_FUN_FailureDetector_ANON4,
        0U,
        NULL
    }
  };
  PRT_STATEDECL P_GEND_STATES_Node[] = 
  {
    
    {
        P_STATE_Node_WaitPing,
        P_MACHINE_Node,
        "WaitPing",
        0,
        1,
        0,
        0,
        3,
        NULL,
        P_GEND_DOS_Node_WaitPing,
        P_FUN_Node_ANON0,
        P_FUN_Node_ANON0,
        0U,
        NULL
    }
  };
  PRT_MACHINEDECL P_GEND_MACHINES[] = 
  {
    
    {
        P_MACHINE_Container,
        "Container",
        0,
        1,
        2,
        4294967295,
        P_STATE_Container_Init,
        NULL,
        P_GEND_STATES_Container,
        P_GEND_FUNS_Container,
        &P_CTOR_Container_IMPL,
        &P_DTOR_Container_IMPL,
        0U,
        NULL
    },
    
    {
        P_MACHINE_Driver,
        "Driver",
        6,
        1,
        3,
        4294967295,
        P_STATE_Driver_Init,
        P_GEND_VARS_Driver,
        P_GEND_STATES_Driver,
        P_GEND_FUNS_Driver,
        &P_CTOR_Driver_IMPL,
        &P_DTOR_Driver_IMPL,
        0U,
        NULL
    },
    
    {
        P_MACHINE_FailureDetector,
        "FailureDetector",
        6,
        3,
        15,
        4294967295,
        P_STATE_FailureDetector_Init,
        P_GEND_VARS_FailureDetector,
        P_GEND_STATES_FailureDetector,
        P_GEND_FUNS_FailureDetector,
        &P_CTOR_FailureDetector_IMPL,
        &P_DTOR_FailureDetector_IMPL,
        0U,
        NULL
    },
    
    {
        P_MACHINE_Node,
        "Node",
        0,
        1,
        3,
        4294967295,
        P_STATE_Node_WaitPing,
        NULL,
        P_GEND_STATES_Node,
        P_GEND_FUNS_Node,
        &P_CTOR_Node_IMPL,
        &P_DTOR_Node_IMPL,
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
    17U,
    10U,
    4U,
    1U,
    0U,
    P_GEND_EVENTS,
    P_GEND_EVENTSETS,
    P_GEND_MACHINES,
    P_GEND_MODELS,
    NULL,
    0U,
    NULL
  };
  