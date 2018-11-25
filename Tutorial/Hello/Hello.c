#include "Hello.h"

// Type universe for program:
static PRT_TYPE P_GEND_TYPE_i = { PRT_KIND_INT, { NULL } };
static PRT_TYPE P_GEND_TYPE_m = { PRT_KIND_MACHINE, { NULL } };
static PRT_TYPE P_GEND_TYPE_b = { PRT_KIND_BOOL, { NULL } };
static PRT_TYPE P_GEND_TYPE_n = { PRT_KIND_NULL, { NULL } };

// Function implementation prototypes:
PRT_VALUE* P_CreateTimer_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_CreateTimer;

PRT_VALUE* P_StartTimer_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_StartTimer;

PRT_VALUE* P_StopProgram_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_StopProgram;

PRT_VALUE* P_Continue_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Continue;

PRT_VALUE* P_Anon_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon;

PRT_VALUE* P_Anon_IMPL_1(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_1;

PRT_VALUE* P_Anon_IMPL_2(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_2;

PRT_VALUE* P_Anon_IMPL_3(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_3;

PRT_VALUE* P_Anon_IMPL_4(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_4;

PRT_VALUE* P_Anon_IMPL_5(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_5;

PRT_VALUE* P_Anon_IMPL_6(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_6;


PRT_EVENTDECL P_EVENT_START = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "START",
    4294967295U,
    &P_GEND_TYPE_i
};

PRT_EVENTDECL P_EVENT_TIMEOUT = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "TIMEOUT",
    4294967295U,
    &P_GEND_TYPE_m
};

PRT_VALUE* P_CreateTimer_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = PrtMkDefaultValue(&P_GEND_TYPE_m);
    PRT_VALUE** P_VAR_owner = argRefs[0];
    PRT_VALUE* P_VAR_m = PrtMkDefaultValue(&P_GEND_TYPE_m);
    PRT_VALUE* PTMP_tmp0 = NULL;
    PRT_VALUE* PTMP_tmp1 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE = &(PTMP_tmp0);
    PrtFreeValue(*P_LVALUE);
    *P_LVALUE = PrtCloneValue(*P_VAR_owner);
    
    PRT_VALUE** P_LVALUE_1 = &(PTMP_tmp1);
    PrtFreeValue(*P_LVALUE_1);
    *P_LVALUE_1 = PrtCloneValue(PrtMkInterface(context, 0, 1, &(PTMP_tmp0))->id);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return;
    }
    
    {
        PRT_VALUE** P_LVALUE_2 = &(P_VAR_m);
        PrtFreeValue(*P_LVALUE_2);
        *P_LVALUE_2 = PTMP_tmp1;
        PTMP_tmp1 = NULL;
    }
    
    PrtFreeValue(_P_GEN_retval);
    _P_GEN_retval = PrtCloneValue(P_VAR_m);
    goto p_return;
    
p_return: ;
    PrtFreeValue(P_VAR_m); P_VAR_m = NULL;
    PrtFreeValue(PTMP_tmp0); PTMP_tmp0 = NULL;
    PrtFreeValue(PTMP_tmp1); PTMP_tmp1 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_CreateTimer =
{
    "CreateTimer",
    &P_CreateTimer_IMPL,
    NULL
};


PRT_VALUE* P_StartTimer_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE** P_VAR_timer = argRefs[0];
    PRT_VALUE** P_VAR_time = argRefs[1];
    PRT_VALUE* PTMP_tmp0_1 = NULL;
    PRT_VALUE* PTMP_tmp1_1 = NULL;
    PRT_VALUE* PTMP_tmp2 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_3 = &(PTMP_tmp0_1);
    PrtFreeValue(*P_LVALUE_3);
    *P_LVALUE_3 = PrtCloneValue(*P_VAR_timer);
    
    PRT_VALUE** P_LVALUE_4 = &(PTMP_tmp1_1);
    PrtFreeValue(*P_LVALUE_4);
    *P_LVALUE_4 = PrtCloneValue((&P_EVENT_START.value));
    
    PRT_VALUE** P_LVALUE_5 = &(PTMP_tmp2);
    PrtFreeValue(*P_LVALUE_5);
    *P_LVALUE_5 = PrtCloneValue(*P_VAR_time);
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp0_1), PTMP_tmp1_1, 1, &(PTMP_tmp2));
    *(&(PTMP_tmp1_1)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_1;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_1;
    }
    
p_return_1: ;
    PrtFreeValue(PTMP_tmp0_1); PTMP_tmp0_1 = NULL;
    PrtFreeValue(PTMP_tmp1_1); PTMP_tmp1_1 = NULL;
    PrtFreeValue(PTMP_tmp2); PTMP_tmp2 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_StartTimer =
{
    "StartTimer",
    &P_StartTimer_IMPL,
    NULL
};


PRT_VALUE* P_StopProgram_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
p_return_2: ;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_StopProgram =
{
    "StopProgram",
    &P_StopProgram_IMPL,
    NULL
};


PRT_VALUE* P_Continue_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = PrtMkDefaultValue(&P_GEND_TYPE_b);
    PRT_VALUE* PTMP_tmp0_2 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_6 = &(PTMP_tmp0_2);
    PrtFreeValue(*P_LVALUE_6);
    *P_LVALUE_6 = (PrtMkNondetBoolValue());
    
    PrtFreeValue(_P_GEN_retval);
    _P_GEN_retval = PrtCloneValue(PTMP_tmp0_2);
    goto p_return_3;
    
p_return_3: ;
    PrtFreeValue(PTMP_tmp0_2); PTMP_tmp0_2 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Continue =
{
    "Continue",
    &P_Continue_IMPL,
    NULL
};


PRT_EVENTDECL* P_Timer_RECV_INNER[] = { &P_EVENT_START };
PRT_EVENTSETDECL P_EVENTSET_Timer_RECV =
{
    1U,
    P_Timer_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_Timer =
{
    0U,
    "Timer",
    &P_GEND_TYPE_m,
    &P_EVENTSET_Timer_RECV
};

PRT_EVENTDECL* P_Hello_RECV_INNER[] = { &P_EVENT_START, &P_EVENT_TIMEOUT, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_Hello_RECV =
{
    3U,
    P_Hello_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_Hello =
{
    1U,
    "Hello",
    &P_GEND_TYPE_n,
    &P_EVENTSET_Hello_RECV
};

PRT_VARDECL P_Timer_VARS[] = {
    { "client", &P_GEND_TYPE_m }
};

PRT_EVENTDECL* P_Init_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DEFERS =
{
    0U,
    P_Init_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_Init_TRANS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_TRANS =
{
    0U,
    P_Init_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_Init_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DOS =
{
    0U,
    P_Init_DOS_INNER,
    NULL
};

#define P_STATE_Timer_Init \
{ \
    "Timer.Init", \
    0U, \
    0U, \
    &P_EVENTSET_Init_DEFERS, \
    &P_EVENTSET_Init_TRANS, \
    &P_EVENTSET_Init_DOS, \
    NULL, \
    NULL, \
    &P_FUNCTION_Anon, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_WaitForReq_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_WaitForReq_DEFERS =
{
    0U,
    P_WaitForReq_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_WaitForReq_TRANS_INNER[] = { &P_EVENT_START };
PRT_EVENTSETDECL P_EVENTSET_WaitForReq_TRANS =
{
    1U,
    P_WaitForReq_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_WaitForReq_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_WaitForReq_DOS =
{
    0U,
    P_WaitForReq_DOS_INNER,
    NULL
};

PRT_TRANSDECL P_TRANS[] =
{
    { 1, &P_EVENT_START, 2, &_P_NO_OP }
};

#define P_STATE_Timer_WaitForReq \
{ \
    "Timer.WaitForReq", \
    1U, \
    0U, \
    &P_EVENTSET_WaitForReq_DEFERS, \
    &P_EVENTSET_WaitForReq_TRANS, \
    &P_EVENTSET_WaitForReq_DOS, \
    P_TRANS, \
    NULL, \
    &_P_NO_OP, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_WaitForTimeout_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_WaitForTimeout_DEFERS =
{
    0U,
    P_WaitForTimeout_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_WaitForTimeout_TRANS_INNER[] = { &_P_EVENT_NULL_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_WaitForTimeout_TRANS =
{
    1U,
    P_WaitForTimeout_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_WaitForTimeout_DOS_INNER[] = { &P_EVENT_START };
PRT_EVENTSETDECL P_EVENTSET_WaitForTimeout_DOS =
{
    1U,
    P_WaitForTimeout_DOS_INNER,
    NULL
};

PRT_TRANSDECL P_TRANS_1[] =
{
    { 2, &_P_EVENT_NULL_STRUCT, 1, &P_FUNCTION_Anon_1 }
};

PRT_DODECL P_DOS[] =
{
    { 2, &P_EVENT_START, NULL }
};

#define P_STATE_Timer_WaitForTimeout \
{ \
    "Timer.WaitForTimeout", \
    1U, \
    1U, \
    &P_EVENTSET_WaitForTimeout_DEFERS, \
    &P_EVENTSET_WaitForTimeout_TRANS, \
    &P_EVENTSET_WaitForTimeout_DOS, \
    P_TRANS_1, \
    P_DOS, \
    &_P_NO_OP, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_Timer_STATES[] = { P_STATE_Timer_Init, P_STATE_Timer_WaitForReq, P_STATE_Timer_WaitForTimeout };

PRT_VALUE* P_Anon_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE** P_VAR_payload = argRefs[0];
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_7 = &(p_this->varValues[0]);
    PrtFreeValue(*P_LVALUE_7);
    *P_LVALUE_7 = PrtCloneValue(*P_VAR_payload);
    
    PrtGoto(p_this, 1U, 0);
    
p_return_4: ;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon =
{
    NULL,
    &P_Anon_IMPL,
    &P_GEND_TYPE_m
};


PRT_VALUE* P_Anon_IMPL_1(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_3 = NULL;
    PRT_VALUE* PTMP_tmp1_2 = NULL;
    PRT_VALUE* PTMP_tmp2_1 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_8 = &(PTMP_tmp0_3);
    PrtFreeValue(*P_LVALUE_8);
    *P_LVALUE_8 = PrtCloneValue(p_this->varValues[0]);
    
    PRT_VALUE** P_LVALUE_9 = &(PTMP_tmp1_2);
    PrtFreeValue(*P_LVALUE_9);
    *P_LVALUE_9 = PrtCloneValue((&P_EVENT_TIMEOUT.value));
    
    PRT_VALUE** P_LVALUE_10 = &(PTMP_tmp2_1);
    PrtFreeValue(*P_LVALUE_10);
    *P_LVALUE_10 = PrtCloneValue((p_this->id));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp0_3), PTMP_tmp1_2, 1, &(PTMP_tmp2_1));
    *(&(PTMP_tmp1_2)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_5;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_5;
    }
    
p_return_5: ;
    PrtFreeValue(PTMP_tmp0_3); PTMP_tmp0_3 = NULL;
    PrtFreeValue(PTMP_tmp1_2); PTMP_tmp1_2 = NULL;
    PrtFreeValue(PTMP_tmp2_1); PTMP_tmp2_1 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_1 =
{
    NULL,
    &P_Anon_IMPL_1,
    NULL
};


PRT_FUNDECL* P_Timer_METHODS[] = { &P_FUNCTION_Anon, &P_FUNCTION_Anon_1 };

PRT_EVENTDECL* P_Timer_RECV_INNER_1[] = { &P_EVENT_START };
PRT_EVENTSETDECL P_EVENTSET_Timer_RECV_1 =
{
    1U,
    P_Timer_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_Timer_SEND_INNER[] = { &P_EVENT_TIMEOUT };
PRT_EVENTSETDECL P_EVENTSET_Timer_SEND =
{
    1U,
    P_Timer_SEND_INNER,
    NULL
};

PRT_MACHINEDECL P_MACHINE_Timer = 
{
    0U,
    "Timer",
    &P_EVENTSET_Timer_RECV_1,
    &P_EVENTSET_Timer_SEND,
    NULL,
    1U,
    3U,
    2U,
    4294967295U,
    0U,
    P_Timer_VARS,
    P_Timer_STATES,
    P_Timer_METHODS
};

PRT_VARDECL P_Hello_VARS[] = {
    { "timer", &P_GEND_TYPE_m }
};

PRT_EVENTDECL* P_Init_DEFERS_INNER_1[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DEFERS_1 =
{
    0U,
    P_Init_DEFERS_INNER_1,
    NULL
};

PRT_EVENTDECL* P_Init_TRANS_INNER_1[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_TRANS_1 =
{
    0U,
    P_Init_TRANS_INNER_1,
    NULL
};

PRT_EVENTDECL* P_Init_DOS_INNER_1[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DOS_1 =
{
    0U,
    P_Init_DOS_INNER_1,
    NULL
};

#define P_STATE_Hello_Init \
{ \
    "Hello.Init", \
    0U, \
    0U, \
    &P_EVENTSET_Init_DEFERS_1, \
    &P_EVENTSET_Init_TRANS_1, \
    &P_EVENTSET_Init_DOS_1, \
    NULL, \
    NULL, \
    &P_FUNCTION_Anon_2, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_GetInput_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_GetInput_DEFERS =
{
    0U,
    P_GetInput_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_GetInput_TRANS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_GetInput_TRANS =
{
    0U,
    P_GetInput_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_GetInput_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_GetInput_DOS =
{
    0U,
    P_GetInput_DOS_INNER,
    NULL
};

#define P_STATE_Hello_GetInput \
{ \
    "Hello.GetInput", \
    0U, \
    0U, \
    &P_EVENTSET_GetInput_DEFERS, \
    &P_EVENTSET_GetInput_TRANS, \
    &P_EVENTSET_GetInput_DOS, \
    NULL, \
    NULL, \
    &P_FUNCTION_Anon_3, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_PrintHello_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_PrintHello_DEFERS =
{
    0U,
    P_PrintHello_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_PrintHello_TRANS_INNER[] = { &P_EVENT_TIMEOUT };
PRT_EVENTSETDECL P_EVENTSET_PrintHello_TRANS =
{
    1U,
    P_PrintHello_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_PrintHello_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_PrintHello_DOS =
{
    0U,
    P_PrintHello_DOS_INNER,
    NULL
};

PRT_TRANSDECL P_TRANS_2[] =
{
    { 2, &P_EVENT_TIMEOUT, 1, &P_FUNCTION_Anon_5 }
};

#define P_STATE_Hello_PrintHello \
{ \
    "Hello.PrintHello", \
    1U, \
    0U, \
    &P_EVENTSET_PrintHello_DEFERS, \
    &P_EVENTSET_PrintHello_TRANS, \
    &P_EVENTSET_PrintHello_DOS, \
    P_TRANS_2, \
    NULL, \
    &P_FUNCTION_Anon_4, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_Stop_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Stop_DEFERS =
{
    0U,
    P_Stop_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_Stop_TRANS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Stop_TRANS =
{
    0U,
    P_Stop_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_Stop_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Stop_DOS =
{
    0U,
    P_Stop_DOS_INNER,
    NULL
};

#define P_STATE_Hello_Stop \
{ \
    "Hello.Stop", \
    0U, \
    0U, \
    &P_EVENTSET_Stop_DEFERS, \
    &P_EVENTSET_Stop_TRANS, \
    &P_EVENTSET_Stop_DOS, \
    NULL, \
    NULL, \
    &P_FUNCTION_Anon_6, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_Hello_STATES[] = { P_STATE_Hello_Init, P_STATE_Hello_GetInput, P_STATE_Hello_PrintHello, P_STATE_Hello_Stop };

PRT_VALUE* P_Anon_IMPL_2(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_4 = NULL;
    PRT_VALUE* PTMP_tmp1_3 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_11 = &(PTMP_tmp0_4);
    PrtFreeValue(*P_LVALUE_11);
    *P_LVALUE_11 = PrtCloneValue((p_this->id));
    
    PRT_VALUE** P_LVALUE_12 = &(PTMP_tmp1_3);
    PrtFreeValue(*P_LVALUE_12);
    *P_LVALUE_12 = ((_P_GEN_funargs[0] = &(PTMP_tmp0_4)), (_P_GEN_funval = P_CreateTimer_IMPL(context, _P_GEN_funargs)), (PrtFreeValue(PTMP_tmp0_4), PTMP_tmp0_4 = NULL), (_P_GEN_funval));
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_6;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_6;
    }
    
    {
        PRT_VALUE** P_LVALUE_13 = &(p_this->varValues[0]);
        PrtFreeValue(*P_LVALUE_13);
        *P_LVALUE_13 = PTMP_tmp1_3;
        PTMP_tmp1_3 = NULL;
    }
    
    PrtGoto(p_this, 1U, 0);
    
p_return_6: ;
    PrtFreeValue(PTMP_tmp0_4); PTMP_tmp0_4 = NULL;
    PrtFreeValue(PTMP_tmp1_3); PTMP_tmp1_3 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_2 =
{
    NULL,
    &P_Anon_IMPL_2,
    NULL
};


PRT_VALUE* P_Anon_IMPL_3(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* P_VAR_b = PrtMkDefaultValue(&P_GEND_TYPE_b);
    PRT_VALUE* PTMP_tmp0_5 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_14 = &(PTMP_tmp0_5);
    PrtFreeValue(*P_LVALUE_14);
    *P_LVALUE_14 = ((_P_GEN_funval = P_Continue_IMPL(context, _P_GEN_funargs)), (_P_GEN_funval));
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_7;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_7;
    }
    
    {
        PRT_VALUE** P_LVALUE_15 = &(P_VAR_b);
        PrtFreeValue(*P_LVALUE_15);
        *P_LVALUE_15 = PTMP_tmp0_5;
        PTMP_tmp0_5 = NULL;
    }
    
    if (PrtPrimGetBool(P_VAR_b))
    {
        PrtGoto(p_this, 2U, 0);
        
    }
    
    else
    {
        PrtGoto(p_this, 3U, 0);
        
    }
    
    
p_return_7: ;
    PrtFreeValue(P_VAR_b); P_VAR_b = NULL;
    PrtFreeValue(PTMP_tmp0_5); PTMP_tmp0_5 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_3 =
{
    NULL,
    &P_Anon_IMPL_3,
    NULL
};


PRT_VALUE* P_Anon_IMPL_4(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_6 = NULL;
    PRT_VALUE* PTMP_tmp1_4 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32 = { PRT_VALUE_KIND_INT, { .nt = 100 } };
    PRT_VALUE** P_LVALUE_16 = &(PTMP_tmp0_6);
    PrtFreeValue(*P_LVALUE_16);
    *P_LVALUE_16 = PrtCloneValue(p_this->varValues[0]);
    
    PRT_VALUE** P_LVALUE_17 = &(PTMP_tmp1_4);
    PrtFreeValue(*P_LVALUE_17);
    *P_LVALUE_17 = PrtCloneValue((&P_LIT_INT32));
    
    _P_GEN_funargs[0] = &(PTMP_tmp0_6);
    _P_GEN_funargs[1] = &(PTMP_tmp1_4);
    PrtFreeValue(P_StartTimer_IMPL(context, _P_GEN_funargs));
    PrtFreeValue(PTMP_tmp0_6);
    PTMP_tmp0_6 = NULL;
    PrtFreeValue(PTMP_tmp1_4);
    PTMP_tmp1_4 = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_8;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_8;
    }
    
p_return_8: ;
    PrtFreeValue(PTMP_tmp0_6); PTMP_tmp0_6 = NULL;
    PrtFreeValue(PTMP_tmp1_4); PTMP_tmp1_4 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_4 =
{
    NULL,
    &P_Anon_IMPL_4,
    NULL
};


PRT_VALUE* P_Anon_IMPL_5(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PrtPrintf("Hello\n");
    
p_return_9: ;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_5 =
{
    NULL,
    &P_Anon_IMPL_5,
    NULL
};


PRT_VALUE* P_Anon_IMPL_6(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PrtFreeValue(P_StopProgram_IMPL(context, _P_GEN_funargs));
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_10;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_10;
    }
    
p_return_10: ;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_6 =
{
    NULL,
    &P_Anon_IMPL_6,
    NULL
};


PRT_FUNDECL* P_Hello_METHODS[] = { &P_FUNCTION_Anon_2, &P_FUNCTION_Anon_3, &P_FUNCTION_Anon_4, &P_FUNCTION_Anon_5, &P_FUNCTION_Anon_6 };

PRT_EVENTDECL* P_Hello_RECV_INNER_1[] = { &P_EVENT_START, &P_EVENT_TIMEOUT, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_Hello_RECV_1 =
{
    3U,
    P_Hello_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_Hello_SEND_INNER[] = { &P_EVENT_START, &P_EVENT_TIMEOUT, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_Hello_SEND =
{
    3U,
    P_Hello_SEND_INNER,
    NULL
};

PRT_UINT32 P_Hello_CREATES_ARR[] = { 0 };
PRT_INTERFACESETDECL P_Hello_CREATES = { 1, P_Hello_CREATES_ARR };
PRT_MACHINEDECL P_MACHINE_Hello = 
{
    1U,
    "Hello",
    &P_EVENTSET_Hello_RECV_1,
    &P_EVENTSET_Hello_SEND,
    &P_Hello_CREATES,
    1U,
    4U,
    5U,
    4294967295U,
    0U,
    P_Hello_VARS,
    P_Hello_STATES,
    P_Hello_METHODS
};

PRT_TYPE* P_TYPEDEF_TimerPtr = &P_GEND_TYPE_m;
PRT_EVENTDECL* P_ALL_EVENTS[] = { &_P_EVENT_NULL_STRUCT, &_P_EVENT_HALT_STRUCT, &P_EVENT_START, &P_EVENT_TIMEOUT };
PRT_MACHINEDECL* P_ALL_MACHINES[] = { &P_MACHINE_Timer, &P_MACHINE_Hello };
PRT_INTERFACEDECL* P_ALL_INTERFACES[] = { &P_I_Timer, &P_I_Hello };
PRT_FUNDECL* P_ALL_FUNCTIONS[] = { &P_FUNCTION_CreateTimer, &P_FUNCTION_StartTimer, &P_FUNCTION_StopProgram, &P_FUNCTION_Continue };
PRT_FOREIGNTYPEDECL* P_ALL_FOREIGN_TYPES[] = { NULL };
