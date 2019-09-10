#include "PingPong.h"

// Type universe for program:
static PRT_TYPE P_GEND_TYPE_i = { PRT_KIND_INT, { NULL } };
static PRT_TYPE P_GEND_TYPE_m = { PRT_KIND_MACHINE, { NULL } };
static PRT_TYPE P_GEND_TYPE_R = { PRT_KIND_MACHINE, { NULL } };
static PRT_TYPE P_GEND_TYPE_n = { PRT_KIND_NULL, { NULL } };
static PRT_TYPE P_GEND_TYPE_R_1 = { PRT_KIND_MACHINE, { NULL } };

// Function implementation prototypes:
PRT_VALUE* P_CreateTimer_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_CreateTimer;

PRT_VALUE* P_StartTimer_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_StartTimer;

PRT_VALUE* P_StopProgram_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_StopProgram;

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

PRT_VALUE* P_Anon_IMPL_7(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_7;

PRT_VALUE* P_Anon_IMPL_8(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_8;

PRT_VALUE* P_Anon_IMPL_9(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_9;

PRT_VALUE* P_Anon_IMPL_10(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_10;

PRT_VALUE* P_Anon_IMPL_11(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_11;


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

PRT_EVENTDECL P_EVENT_PING = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "PING",
    1U,
    &P_GEND_TYPE_R
};

PRT_EVENTDECL P_EVENT_PONG = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "PONG",
    1U,
    &P_GEND_TYPE_n
};

PRT_EVENTDECL P_EVENT_SUCCESS = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "SUCCESS",
    4294967295U,
    &P_GEND_TYPE_n
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

PRT_EVENTDECL* P_Test_1_Machine_RECV_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Test_1_Machine_RECV =
{
    0U,
    P_Test_1_Machine_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_Test_1_Machine =
{
    1U,
    "Test_1_Machine",
    &P_GEND_TYPE_n,
    &P_EVENTSET_Test_1_Machine_RECV
};

PRT_EVENTDECL* P_Test_2_Machine_RECV_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Test_2_Machine_RECV =
{
    0U,
    P_Test_2_Machine_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_Test_2_Machine =
{
    2U,
    "Test_2_Machine",
    &P_GEND_TYPE_n,
    &P_EVENTSET_Test_2_Machine_RECV
};

PRT_EVENTDECL* P_ClientMachine_RECV_INNER[] = { &P_EVENT_PONG };
PRT_EVENTSETDECL P_EVENTSET_ClientMachine_RECV =
{
    1U,
    P_ClientMachine_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_ClientMachine =
{
    3U,
    "ClientMachine",
    &P_GEND_TYPE_i,
    &P_EVENTSET_ClientMachine_RECV
};

PRT_EVENTDECL* P_ServerMachine_RECV_INNER[] = { &P_EVENT_PING, &P_EVENT_TIMEOUT };
PRT_EVENTSETDECL P_EVENTSET_ServerMachine_RECV =
{
    2U,
    P_ServerMachine_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_ServerMachine =
{
    4U,
    "ServerMachine",
    &P_GEND_TYPE_n,
    &P_EVENTSET_ServerMachine_RECV
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
    PRT_VALUE** P_LVALUE_6 = &(p_this->varValues[0]);
    PrtFreeValue(*P_LVALUE_6);
    *P_LVALUE_6 = PrtCloneValue(*P_VAR_payload);
    
    PrtGoto(p_this, 1U, 0);
    
p_return_3: ;
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
    PRT_VALUE* PTMP_tmp0_2 = NULL;
    PRT_VALUE* PTMP_tmp1_2 = NULL;
    PRT_VALUE* PTMP_tmp2_1 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_7 = &(PTMP_tmp0_2);
    PrtFreeValue(*P_LVALUE_7);
    *P_LVALUE_7 = PrtCloneValue(p_this->varValues[0]);
    
    PRT_VALUE** P_LVALUE_8 = &(PTMP_tmp1_2);
    PrtFreeValue(*P_LVALUE_8);
    *P_LVALUE_8 = PrtCloneValue((&P_EVENT_TIMEOUT.value));
    
    PRT_VALUE** P_LVALUE_9 = &(PTMP_tmp2_1);
    PrtFreeValue(*P_LVALUE_9);
    *P_LVALUE_9 = PrtCloneValue((p_this->id));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp0_2), PTMP_tmp1_2, 1, &(PTMP_tmp2_1));
    *(&(PTMP_tmp1_2)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_4;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_4;
    }
    
p_return_4: ;
    PrtFreeValue(PTMP_tmp0_2); PTMP_tmp0_2 = NULL;
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

PRT_VARDECL P_Test_1_Machine_VARS[] = {
    { "client", &P_GEND_TYPE_R }
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

#define P_STATE_Test_1_Machine_Init \
{ \
    "Test_1_Machine.Init", \
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

PRT_STATEDECL P_Test_1_Machine_STATES[] = { P_STATE_Test_1_Machine_Init };

PRT_VALUE* P_Anon_IMPL_2(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_3 = NULL;
    PRT_VALUE* PTMP_tmp1_3 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32 = { PRT_VALUE_KIND_INT, { .nt = 5 } };
    PRT_VALUE** P_LVALUE_10 = &(PTMP_tmp0_3);
    PrtFreeValue(*P_LVALUE_10);
    *P_LVALUE_10 = PrtCloneValue((&P_LIT_INT32));
    
    PRT_VALUE** P_LVALUE_11 = &(PTMP_tmp1_3);
    PrtFreeValue(*P_LVALUE_11);
    *P_LVALUE_11 = PrtCloneValue(PrtMkInterface(context, 3, 1, &(PTMP_tmp0_3))->id);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_5;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_5;
    }
    
    {
        PRT_VALUE** P_LVALUE_12 = &(p_this->varValues[0]);
        PrtFreeValue(*P_LVALUE_12);
        *P_LVALUE_12 = PTMP_tmp1_3;
        PTMP_tmp1_3 = NULL;
    }
    
p_return_5: ;
    PrtFreeValue(PTMP_tmp0_3); PTMP_tmp0_3 = NULL;
    PrtFreeValue(PTMP_tmp1_3); PTMP_tmp1_3 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_2 =
{
    NULL,
    &P_Anon_IMPL_2,
    NULL
};


PRT_FUNDECL* P_Test_1_Machine_METHODS[] = { &P_FUNCTION_Anon_2 };

PRT_EVENTDECL* P_Test_1_Machine_RECV_INNER_1[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Test_1_Machine_RECV_1 =
{
    0U,
    P_Test_1_Machine_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_Test_1_Machine_SEND_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Test_1_Machine_SEND =
{
    0U,
    P_Test_1_Machine_SEND_INNER,
    NULL
};

PRT_UINT32 P_Test_1_Machine_CREATES_ARR[] = { 3 };
PRT_INTERFACESETDECL P_Test_1_Machine_CREATES = { 1, P_Test_1_Machine_CREATES_ARR };
PRT_MACHINEDECL P_MACHINE_Test_1_Machine = 
{
    1U,
    "Test_1_Machine",
    &P_EVENTSET_Test_1_Machine_RECV_1,
    &P_EVENTSET_Test_1_Machine_SEND,
    &P_Test_1_Machine_CREATES,
    1U,
    1U,
    1U,
    4294967295U,
    0U,
    P_Test_1_Machine_VARS,
    P_Test_1_Machine_STATES,
    P_Test_1_Machine_METHODS
};

PRT_VARDECL P_Test_2_Machine_VARS[] = {
    { "client", &P_GEND_TYPE_R }
};

PRT_EVENTDECL* P_Init_DEFERS_INNER_2[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DEFERS_2 =
{
    0U,
    P_Init_DEFERS_INNER_2,
    NULL
};

PRT_EVENTDECL* P_Init_TRANS_INNER_2[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_TRANS_2 =
{
    0U,
    P_Init_TRANS_INNER_2,
    NULL
};

PRT_EVENTDECL* P_Init_DOS_INNER_2[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DOS_2 =
{
    0U,
    P_Init_DOS_INNER_2,
    NULL
};

#define P_STATE_Test_2_Machine_Init \
{ \
    "Test_2_Machine.Init", \
    0U, \
    0U, \
    &P_EVENTSET_Init_DEFERS_2, \
    &P_EVENTSET_Init_TRANS_2, \
    &P_EVENTSET_Init_DOS_2, \
    NULL, \
    NULL, \
    &P_FUNCTION_Anon_3, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_Test_2_Machine_STATES[] = { P_STATE_Test_2_Machine_Init };

PRT_VALUE* P_Anon_IMPL_3(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_4 = NULL;
    PRT_VALUE* PTMP_tmp1_4 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_1 = { PRT_VALUE_KIND_INT, { .nt = 1 } };
    PRT_VALUE** P_LVALUE_13 = &(PTMP_tmp0_4);
    PrtFreeValue(*P_LVALUE_13);
    *P_LVALUE_13 = PrtMkIntValue(-PrtPrimGetInt((&P_LIT_INT32_1)));
    
    PRT_VALUE** P_LVALUE_14 = &(PTMP_tmp1_4);
    PrtFreeValue(*P_LVALUE_14);
    *P_LVALUE_14 = PrtCloneValue(PrtMkInterface(context, 3, 1, &(PTMP_tmp0_4))->id);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_6;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_6;
    }
    
    {
        PRT_VALUE** P_LVALUE_15 = &(p_this->varValues[0]);
        PrtFreeValue(*P_LVALUE_15);
        *P_LVALUE_15 = PTMP_tmp1_4;
        PTMP_tmp1_4 = NULL;
    }
    
p_return_6: ;
    PrtFreeValue(PTMP_tmp0_4); PTMP_tmp0_4 = NULL;
    PrtFreeValue(PTMP_tmp1_4); PTMP_tmp1_4 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_3 =
{
    NULL,
    &P_Anon_IMPL_3,
    NULL
};


PRT_FUNDECL* P_Test_2_Machine_METHODS[] = { &P_FUNCTION_Anon_3 };

PRT_EVENTDECL* P_Test_2_Machine_RECV_INNER_1[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Test_2_Machine_RECV_1 =
{
    0U,
    P_Test_2_Machine_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_Test_2_Machine_SEND_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Test_2_Machine_SEND =
{
    0U,
    P_Test_2_Machine_SEND_INNER,
    NULL
};

PRT_UINT32 P_Test_2_Machine_CREATES_ARR[] = { 3 };
PRT_INTERFACESETDECL P_Test_2_Machine_CREATES = { 1, P_Test_2_Machine_CREATES_ARR };
PRT_MACHINEDECL P_MACHINE_Test_2_Machine = 
{
    2U,
    "Test_2_Machine",
    &P_EVENTSET_Test_2_Machine_RECV_1,
    &P_EVENTSET_Test_2_Machine_SEND,
    &P_Test_2_Machine_CREATES,
    1U,
    1U,
    1U,
    4294967295U,
    0U,
    P_Test_2_Machine_VARS,
    P_Test_2_Machine_STATES,
    P_Test_2_Machine_METHODS
};

PRT_VARDECL P_ClientMachine_VARS[] = {
    { "server", &P_GEND_TYPE_R_1 },
    { "numIterations", &P_GEND_TYPE_i }
};

PRT_EVENTDECL* P_Init_DEFERS_INNER_3[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DEFERS_3 =
{
    0U,
    P_Init_DEFERS_INNER_3,
    NULL
};

PRT_EVENTDECL* P_Init_TRANS_INNER_3[] = { &P_EVENT_SUCCESS };
PRT_EVENTSETDECL P_EVENTSET_Init_TRANS_3 =
{
    1U,
    P_Init_TRANS_INNER_3,
    NULL
};

PRT_EVENTDECL* P_Init_DOS_INNER_3[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DOS_3 =
{
    0U,
    P_Init_DOS_INNER_3,
    NULL
};

PRT_TRANSDECL P_TRANS_2[] =
{
    { 0, &P_EVENT_SUCCESS, 1, &_P_NO_OP }
};

#define P_STATE_ClientMachine_Init \
{ \
    "ClientMachine.Init", \
    1U, \
    0U, \
    &P_EVENTSET_Init_DEFERS_3, \
    &P_EVENTSET_Init_TRANS_3, \
    &P_EVENTSET_Init_DOS_3, \
    P_TRANS_2, \
    NULL, \
    &P_FUNCTION_Anon_4, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_SendPing_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_SendPing_DEFERS =
{
    0U,
    P_SendPing_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_SendPing_TRANS_INNER[] = { &P_EVENT_PONG };
PRT_EVENTSETDECL P_EVENTSET_SendPing_TRANS =
{
    1U,
    P_SendPing_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_SendPing_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_SendPing_DOS =
{
    0U,
    P_SendPing_DOS_INNER,
    NULL
};

PRT_TRANSDECL P_TRANS_3[] =
{
    { 1, &P_EVENT_PONG, 1, &_P_NO_OP }
};

#define P_STATE_ClientMachine_SendPing \
{ \
    "ClientMachine.SendPing", \
    1U, \
    0U, \
    &P_EVENTSET_SendPing_DEFERS, \
    &P_EVENTSET_SendPing_TRANS, \
    &P_EVENTSET_SendPing_DOS, \
    P_TRANS_3, \
    NULL, \
    &P_FUNCTION_Anon_5, \
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

#define P_STATE_ClientMachine_Stop \
{ \
    "ClientMachine.Stop", \
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

PRT_STATEDECL P_ClientMachine_STATES[] = { P_STATE_ClientMachine_Init, P_STATE_ClientMachine_SendPing, P_STATE_ClientMachine_Stop };

PRT_VALUE* P_Anon_IMPL_4(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE** P_VAR_n = argRefs[0];
    PRT_VALUE* PTMP_tmp0_5 = NULL;
    PRT_VALUE* PTMP_tmp1_5 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PrtPrintf("Client created\n");
    
    PRT_VALUE** P_LVALUE_16 = &(p_this->varValues[1]);
    PrtFreeValue(*P_LVALUE_16);
    *P_LVALUE_16 = PrtCloneValue(*P_VAR_n);
    
    PRT_VALUE** P_LVALUE_17 = &(PTMP_tmp0_5);
    PrtFreeValue(*P_LVALUE_17);
    *P_LVALUE_17 = PrtCloneValue(PrtMkInterface(context, 4, 0)->id);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_7;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_7;
    }
    
    {
        PRT_VALUE** P_LVALUE_18 = &(p_this->varValues[0]);
        PrtFreeValue(*P_LVALUE_18);
        *P_LVALUE_18 = PTMP_tmp0_5;
        PTMP_tmp0_5 = NULL;
    }
    
    PRT_VALUE** P_LVALUE_19 = &(PTMP_tmp1_5);
    PrtFreeValue(*P_LVALUE_19);
    *P_LVALUE_19 = PrtCloneValue((&P_EVENT_SUCCESS.value));
    
    PrtRaise(p_this, PTMP_tmp1_5, 0);
    *(&(PTMP_tmp1_5)) = NULL;
    goto p_return_7;
    
p_return_7: ;
    PrtFreeValue(PTMP_tmp0_5); PTMP_tmp0_5 = NULL;
    PrtFreeValue(PTMP_tmp1_5); PTMP_tmp1_5 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_4 =
{
    NULL,
    &P_Anon_IMPL_4,
    &P_GEND_TYPE_i
};


PRT_VALUE* P_Anon_IMPL_5(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_6 = NULL;
    PRT_VALUE* PTMP_tmp1_6 = NULL;
    PRT_VALUE* PTMP_tmp2_2 = NULL;
    PRT_VALUE* PTMP_tmp3 = NULL;
    PRT_VALUE* PTMP_tmp4 = NULL;
    PRT_VALUE* PTMP_tmp5 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_2 = { PRT_VALUE_KIND_INT, { .nt = 0 } };
    PRT_VALUE P_LIT_INT32_3 = { PRT_VALUE_KIND_INT, { .nt = 1 } };
    PRT_VALUE** P_LVALUE_20 = &(PTMP_tmp0_6);
    PrtFreeValue(*P_LVALUE_20);
    *P_LVALUE_20 = PrtMkBoolValue(PrtIsEqualValue(p_this->varValues[1], (&P_LIT_INT32_2)));
    
    if (PrtPrimGetBool(PTMP_tmp0_6))
    {
        PrtGoto(p_this, 2U, 0);
        
    }
    
    else
    {
        PRT_VALUE** P_LVALUE_21 = &(PTMP_tmp1_6);
        PrtFreeValue(*P_LVALUE_21);
        *P_LVALUE_21 = PrtMkBoolValue(PrtPrimGetInt(p_this->varValues[1]) > PrtPrimGetInt((&P_LIT_INT32_2)));
        
        if (PrtPrimGetBool(PTMP_tmp1_6))
        {
            PRT_VALUE** P_LVALUE_22 = &(PTMP_tmp2_2);
            PrtFreeValue(*P_LVALUE_22);
            *P_LVALUE_22 = PrtMkIntValue(PrtPrimGetInt(p_this->varValues[1]) - PrtPrimGetInt((&P_LIT_INT32_3)));
            
            {
                PRT_VALUE** P_LVALUE_23 = &(p_this->varValues[1]);
                PrtFreeValue(*P_LVALUE_23);
                *P_LVALUE_23 = PTMP_tmp2_2;
                PTMP_tmp2_2 = NULL;
            }
            
        }
        
        else
        {
        }
        
        
    }
    
    
    PrtPrintf("Client sending PING\n");
    
    PRT_VALUE** P_LVALUE_24 = &(PTMP_tmp3);
    PrtFreeValue(*P_LVALUE_24);
    *P_LVALUE_24 = PrtCloneValue(p_this->varValues[0]);
    
    PRT_VALUE** P_LVALUE_25 = &(PTMP_tmp4);
    PrtFreeValue(*P_LVALUE_25);
    *P_LVALUE_25 = PrtCloneValue((&P_EVENT_PING.value));
    
    PRT_VALUE** P_LVALUE_26 = &(PTMP_tmp5);
    PrtFreeValue(*P_LVALUE_26);
    *P_LVALUE_26 = PrtCloneValue((p_this->id));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp3), PTMP_tmp4, 1, &(PTMP_tmp5));
    *(&(PTMP_tmp4)) = NULL;
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
    PrtFreeValue(PTMP_tmp1_6); PTMP_tmp1_6 = NULL;
    PrtFreeValue(PTMP_tmp2_2); PTMP_tmp2_2 = NULL;
    PrtFreeValue(PTMP_tmp3); PTMP_tmp3 = NULL;
    PrtFreeValue(PTMP_tmp4); PTMP_tmp4 = NULL;
    PrtFreeValue(PTMP_tmp5); PTMP_tmp5 = NULL;
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
        goto p_return_9;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_9;
    }
    
p_return_9: ;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_6 =
{
    NULL,
    &P_Anon_IMPL_6,
    NULL
};


PRT_FUNDECL* P_ClientMachine_METHODS[] = { &P_FUNCTION_Anon_4, &P_FUNCTION_Anon_5, &P_FUNCTION_Anon_6 };

PRT_EVENTDECL* P_ClientMachine_RECV_INNER_1[] = { &P_EVENT_PONG };
PRT_EVENTSETDECL P_EVENTSET_ClientMachine_RECV_1 =
{
    1U,
    P_ClientMachine_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_ClientMachine_SEND_INNER[] = { &P_EVENT_PING };
PRT_EVENTSETDECL P_EVENTSET_ClientMachine_SEND =
{
    1U,
    P_ClientMachine_SEND_INNER,
    NULL
};

PRT_UINT32 P_ClientMachine_CREATES_ARR[] = { 4 };
PRT_INTERFACESETDECL P_ClientMachine_CREATES = { 1, P_ClientMachine_CREATES_ARR };
PRT_MACHINEDECL P_MACHINE_ClientMachine = 
{
    3U,
    "ClientMachine",
    &P_EVENTSET_ClientMachine_RECV_1,
    &P_EVENTSET_ClientMachine_SEND,
    &P_ClientMachine_CREATES,
    2U,
    3U,
    3U,
    4294967295U,
    0U,
    P_ClientMachine_VARS,
    P_ClientMachine_STATES,
    P_ClientMachine_METHODS
};

PRT_VARDECL P_ServerMachine_VARS[] = {
    { "timer", &P_GEND_TYPE_m },
    { "client", &P_GEND_TYPE_R }
};

PRT_EVENTDECL* P_Init_DEFERS_INNER_4[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DEFERS_4 =
{
    0U,
    P_Init_DEFERS_INNER_4,
    NULL
};

PRT_EVENTDECL* P_Init_TRANS_INNER_4[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_TRANS_4 =
{
    0U,
    P_Init_TRANS_INNER_4,
    NULL
};

PRT_EVENTDECL* P_Init_DOS_INNER_4[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DOS_4 =
{
    0U,
    P_Init_DOS_INNER_4,
    NULL
};

#define P_STATE_ServerMachine_Init \
{ \
    "ServerMachine.Init", \
    0U, \
    0U, \
    &P_EVENTSET_Init_DEFERS_4, \
    &P_EVENTSET_Init_TRANS_4, \
    &P_EVENTSET_Init_DOS_4, \
    NULL, \
    NULL, \
    &P_FUNCTION_Anon_7, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_WaitPing_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_WaitPing_DEFERS =
{
    0U,
    P_WaitPing_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_WaitPing_TRANS_INNER[] = { &P_EVENT_PING };
PRT_EVENTSETDECL P_EVENTSET_WaitPing_TRANS =
{
    1U,
    P_WaitPing_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_WaitPing_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_WaitPing_DOS =
{
    0U,
    P_WaitPing_DOS_INNER,
    NULL
};

PRT_TRANSDECL P_TRANS_4[] =
{
    { 1, &P_EVENT_PING, 2, &_P_NO_OP }
};

#define P_STATE_ServerMachine_WaitPing \
{ \
    "ServerMachine.WaitPing", \
    1U, \
    0U, \
    &P_EVENTSET_WaitPing_DEFERS, \
    &P_EVENTSET_WaitPing_TRANS, \
    &P_EVENTSET_WaitPing_DOS, \
    P_TRANS_4, \
    NULL, \
    &_P_NO_OP, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_Sleep_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Sleep_DEFERS =
{
    0U,
    P_Sleep_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_Sleep_TRANS_INNER[] = { &P_EVENT_TIMEOUT };
PRT_EVENTSETDECL P_EVENTSET_Sleep_TRANS =
{
    1U,
    P_Sleep_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_Sleep_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Sleep_DOS =
{
    0U,
    P_Sleep_DOS_INNER,
    NULL
};

PRT_TRANSDECL P_TRANS_5[] =
{
    { 2, &P_EVENT_TIMEOUT, 1, &P_FUNCTION_Anon_9 }
};

#define P_STATE_ServerMachine_Sleep \
{ \
    "ServerMachine.Sleep", \
    1U, \
    0U, \
    &P_EVENTSET_Sleep_DEFERS, \
    &P_EVENTSET_Sleep_TRANS, \
    &P_EVENTSET_Sleep_DOS, \
    P_TRANS_5, \
    NULL, \
    &P_FUNCTION_Anon_8, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_ServerMachine_STATES[] = { P_STATE_ServerMachine_Init, P_STATE_ServerMachine_WaitPing, P_STATE_ServerMachine_Sleep };

PRT_VALUE* P_Anon_IMPL_7(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_7 = NULL;
    PRT_VALUE* PTMP_tmp1_7 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PrtPrintf("Server created\n");
    
    PRT_VALUE** P_LVALUE_27 = &(PTMP_tmp0_7);
    PrtFreeValue(*P_LVALUE_27);
    *P_LVALUE_27 = PrtCloneValue((p_this->id));
    
    PRT_VALUE** P_LVALUE_28 = &(PTMP_tmp1_7);
    PrtFreeValue(*P_LVALUE_28);
    *P_LVALUE_28 = ((_P_GEN_funargs[0] = &(PTMP_tmp0_7)), (_P_GEN_funval = P_CreateTimer_IMPL(context, _P_GEN_funargs)), (PrtFreeValue(PTMP_tmp0_7), PTMP_tmp0_7 = NULL), (_P_GEN_funval));
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_10;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_10;
    }
    
    {
        PRT_VALUE** P_LVALUE_29 = &(p_this->varValues[0]);
        PrtFreeValue(*P_LVALUE_29);
        *P_LVALUE_29 = PTMP_tmp1_7;
        PTMP_tmp1_7 = NULL;
    }
    
    PrtGoto(p_this, 1U, 0);
    
p_return_10: ;
    PrtFreeValue(PTMP_tmp0_7); PTMP_tmp0_7 = NULL;
    PrtFreeValue(PTMP_tmp1_7); PTMP_tmp1_7 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_7 =
{
    NULL,
    &P_Anon_IMPL_7,
    NULL
};


PRT_VALUE* P_Anon_IMPL_8(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE** P_VAR_m_1 = argRefs[0];
    PRT_VALUE* PTMP_tmp0_8 = NULL;
    PRT_VALUE* PTMP_tmp1_8 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_4 = { PRT_VALUE_KIND_INT, { .nt = 1000 } };
    PRT_VALUE** P_LVALUE_30 = &(p_this->varValues[1]);
    PrtFreeValue(*P_LVALUE_30);
    *P_LVALUE_30 = PrtCloneValue(*P_VAR_m_1);
    
    PRT_VALUE** P_LVALUE_31 = &(PTMP_tmp0_8);
    PrtFreeValue(*P_LVALUE_31);
    *P_LVALUE_31 = PrtCloneValue(p_this->varValues[0]);
    
    PRT_VALUE** P_LVALUE_32 = &(PTMP_tmp1_8);
    PrtFreeValue(*P_LVALUE_32);
    *P_LVALUE_32 = PrtCloneValue((&P_LIT_INT32_4));
    
    _P_GEN_funargs[0] = &(PTMP_tmp0_8);
    _P_GEN_funargs[1] = &(PTMP_tmp1_8);
    PrtFreeValue(P_StartTimer_IMPL(context, _P_GEN_funargs));
    PrtFreeValue(PTMP_tmp0_8);
    PTMP_tmp0_8 = NULL;
    PrtFreeValue(PTMP_tmp1_8);
    PTMP_tmp1_8 = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_11;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_11;
    }
    
p_return_11: ;
    PrtFreeValue(PTMP_tmp0_8); PTMP_tmp0_8 = NULL;
    PrtFreeValue(PTMP_tmp1_8); PTMP_tmp1_8 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_8 =
{
    NULL,
    &P_Anon_IMPL_8,
    &P_GEND_TYPE_R
};


PRT_VALUE* P_Anon_IMPL_9(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_9 = NULL;
    PRT_VALUE* PTMP_tmp1_9 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PrtPrintf("Server sending PONG\n");
    
    PRT_VALUE** P_LVALUE_33 = &(PTMP_tmp0_9);
    PrtFreeValue(*P_LVALUE_33);
    *P_LVALUE_33 = PrtCloneValue(p_this->varValues[1]);
    
    PRT_VALUE** P_LVALUE_34 = &(PTMP_tmp1_9);
    PrtFreeValue(*P_LVALUE_34);
    *P_LVALUE_34 = PrtCloneValue((&P_EVENT_PONG.value));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp0_9), PTMP_tmp1_9, 0);
    *(&(PTMP_tmp1_9)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_12;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_12;
    }
    
p_return_12: ;
    PrtFreeValue(PTMP_tmp0_9); PTMP_tmp0_9 = NULL;
    PrtFreeValue(PTMP_tmp1_9); PTMP_tmp1_9 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_9 =
{
    NULL,
    &P_Anon_IMPL_9,
    NULL
};


PRT_FUNDECL* P_ServerMachine_METHODS[] = { &P_FUNCTION_Anon_7, &P_FUNCTION_Anon_8, &P_FUNCTION_Anon_9 };

PRT_EVENTDECL* P_ServerMachine_RECV_INNER_1[] = { &P_EVENT_PING, &P_EVENT_TIMEOUT };
PRT_EVENTSETDECL P_EVENTSET_ServerMachine_RECV_1 =
{
    2U,
    P_ServerMachine_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_ServerMachine_SEND_INNER[] = { &P_EVENT_PONG, &P_EVENT_START };
PRT_EVENTSETDECL P_EVENTSET_ServerMachine_SEND =
{
    2U,
    P_ServerMachine_SEND_INNER,
    NULL
};

PRT_UINT32 P_ServerMachine_CREATES_ARR[] = { 0 };
PRT_INTERFACESETDECL P_ServerMachine_CREATES = { 1, P_ServerMachine_CREATES_ARR };
PRT_MACHINEDECL P_MACHINE_ServerMachine = 
{
    4U,
    "ServerMachine",
    &P_EVENTSET_ServerMachine_RECV_1,
    &P_EVENTSET_ServerMachine_SEND,
    &P_ServerMachine_CREATES,
    2U,
    3U,
    3U,
    4294967295U,
    0U,
    P_ServerMachine_VARS,
    P_ServerMachine_STATES,
    P_ServerMachine_METHODS
};

PRT_VARDECL P_Safety_VARS[] = {
    { "pending", &P_GEND_TYPE_i }
};

PRT_EVENTDECL* P_Init_DEFERS_INNER_5[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DEFERS_5 =
{
    0U,
    P_Init_DEFERS_INNER_5,
    NULL
};

PRT_EVENTDECL* P_Init_TRANS_INNER_5[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_TRANS_5 =
{
    0U,
    P_Init_TRANS_INNER_5,
    NULL
};

PRT_EVENTDECL* P_Init_DOS_INNER_5[] = { &P_EVENT_PING, &P_EVENT_PONG };
PRT_EVENTSETDECL P_EVENTSET_Init_DOS_5 =
{
    2U,
    P_Init_DOS_INNER_5,
    NULL
};

PRT_DODECL P_DOS_1[] =
{
    { 0, &P_EVENT_PING, &P_FUNCTION_Anon_10 },
    { 0, &P_EVENT_PONG, &P_FUNCTION_Anon_11 }
};

#define P_STATE_Safety_Init \
{ \
    "Safety.Init", \
    0U, \
    2U, \
    &P_EVENTSET_Init_DEFERS_5, \
    &P_EVENTSET_Init_TRANS_5, \
    &P_EVENTSET_Init_DOS_5, \
    NULL, \
    P_DOS_1, \
    &_P_NO_OP, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_Safety_STATES[] = { P_STATE_Safety_Init };

PRT_VALUE* P_Anon_IMPL_10(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_10 = NULL;
    PRT_VALUE* PTMP_tmp1_10 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_5 = { PRT_VALUE_KIND_INT, { .nt = 0 } };
    PRT_VALUE P_LIT_INT32_6 = { PRT_VALUE_KIND_INT, { .nt = 1 } };
    PRT_VALUE** P_LVALUE_35 = &(PTMP_tmp0_10);
    PrtFreeValue(*P_LVALUE_35);
    *P_LVALUE_35 = PrtMkBoolValue(PrtIsEqualValue(p_this->varValues[0], (&P_LIT_INT32_5)));
    
    PrtAssert(PrtPrimGetBool(PTMP_tmp0_10), "");
    
    PRT_VALUE** P_LVALUE_36 = &(PTMP_tmp1_10);
    PrtFreeValue(*P_LVALUE_36);
    *P_LVALUE_36 = PrtMkIntValue(PrtPrimGetInt(p_this->varValues[0]) + PrtPrimGetInt((&P_LIT_INT32_6)));
    
    {
        PRT_VALUE** P_LVALUE_37 = &(p_this->varValues[0]);
        PrtFreeValue(*P_LVALUE_37);
        *P_LVALUE_37 = PTMP_tmp1_10;
        PTMP_tmp1_10 = NULL;
    }
    
p_return_13: ;
    PrtFreeValue(PTMP_tmp0_10); PTMP_tmp0_10 = NULL;
    PrtFreeValue(PTMP_tmp1_10); PTMP_tmp1_10 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_10 =
{
    NULL,
    &P_Anon_IMPL_10,
    NULL
};


PRT_VALUE* P_Anon_IMPL_11(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_11 = NULL;
    PRT_VALUE* PTMP_tmp1_11 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_7 = { PRT_VALUE_KIND_INT, { .nt = 1 } };
    PRT_VALUE** P_LVALUE_38 = &(PTMP_tmp0_11);
    PrtFreeValue(*P_LVALUE_38);
    *P_LVALUE_38 = PrtMkBoolValue(PrtIsEqualValue(p_this->varValues[0], (&P_LIT_INT32_7)));
    
    PrtAssert(PrtPrimGetBool(PTMP_tmp0_11), "");
    
    PRT_VALUE** P_LVALUE_39 = &(PTMP_tmp1_11);
    PrtFreeValue(*P_LVALUE_39);
    *P_LVALUE_39 = PrtMkIntValue(PrtPrimGetInt(p_this->varValues[0]) - PrtPrimGetInt((&P_LIT_INT32_7)));
    
    {
        PRT_VALUE** P_LVALUE_40 = &(p_this->varValues[0]);
        PrtFreeValue(*P_LVALUE_40);
        *P_LVALUE_40 = PTMP_tmp1_11;
        PTMP_tmp1_11 = NULL;
    }
    
p_return_14: ;
    PrtFreeValue(PTMP_tmp0_11); PTMP_tmp0_11 = NULL;
    PrtFreeValue(PTMP_tmp1_11); PTMP_tmp1_11 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_11 =
{
    NULL,
    &P_Anon_IMPL_11,
    NULL
};


PRT_FUNDECL* P_Safety_METHODS[] = { &P_FUNCTION_Anon_10, &P_FUNCTION_Anon_11 };

PRT_EVENTDECL* P_Safety_RECV_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Safety_RECV =
{
    0U,
    P_Safety_RECV_INNER,
    NULL
};

PRT_EVENTDECL* P_Safety_SEND_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Safety_SEND =
{
    0U,
    P_Safety_SEND_INNER,
    NULL
};

PRT_MACHINEDECL P_MACHINE_Safety = 
{
    5U,
    "Safety",
    &P_EVENTSET_Safety_RECV,
    &P_EVENTSET_Safety_SEND,
    NULL,
    1U,
    1U,
    2U,
    4294967295U,
    0U,
    P_Safety_VARS,
    P_Safety_STATES,
    P_Safety_METHODS
};

PRT_EVENTDECL* P_WaitPing_DEFERS_INNER_1[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_WaitPing_DEFERS_1 =
{
    0U,
    P_WaitPing_DEFERS_INNER_1,
    NULL
};

PRT_EVENTDECL* P_WaitPing_TRANS_INNER_1[] = { &P_EVENT_PING };
PRT_EVENTSETDECL P_EVENTSET_WaitPing_TRANS_1 =
{
    1U,
    P_WaitPing_TRANS_INNER_1,
    NULL
};

PRT_EVENTDECL* P_WaitPing_DOS_INNER_1[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_WaitPing_DOS_1 =
{
    0U,
    P_WaitPing_DOS_INNER_1,
    NULL
};

PRT_TRANSDECL P_TRANS_6[] =
{
    { 0, &P_EVENT_PING, 1, &_P_NO_OP }
};

#define P_STATE_Liveness_WaitPing \
{ \
    "Liveness.WaitPing", \
    1U, \
    0U, \
    &P_EVENTSET_WaitPing_DEFERS_1, \
    &P_EVENTSET_WaitPing_TRANS_1, \
    &P_EVENTSET_WaitPing_DOS_1, \
    P_TRANS_6, \
    NULL, \
    &_P_NO_OP, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_WaitPong_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_WaitPong_DEFERS =
{
    0U,
    P_WaitPong_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_WaitPong_TRANS_INNER[] = { &P_EVENT_PONG };
PRT_EVENTSETDECL P_EVENTSET_WaitPong_TRANS =
{
    1U,
    P_WaitPong_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_WaitPong_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_WaitPong_DOS =
{
    0U,
    P_WaitPong_DOS_INNER,
    NULL
};

PRT_TRANSDECL P_TRANS_7[] =
{
    { 1, &P_EVENT_PONG, 0, &_P_NO_OP }
};

#define P_STATE_Liveness_WaitPong \
{ \
    "Liveness.WaitPong", \
    1U, \
    0U, \
    &P_EVENTSET_WaitPong_DEFERS, \
    &P_EVENTSET_WaitPong_TRANS, \
    &P_EVENTSET_WaitPong_DOS, \
    P_TRANS_7, \
    NULL, \
    &_P_NO_OP, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_Liveness_STATES[] = { P_STATE_Liveness_WaitPing, P_STATE_Liveness_WaitPong };

PRT_EVENTDECL* P_Liveness_RECV_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Liveness_RECV =
{
    0U,
    P_Liveness_RECV_INNER,
    NULL
};

PRT_EVENTDECL* P_Liveness_SEND_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Liveness_SEND =
{
    0U,
    P_Liveness_SEND_INNER,
    NULL
};

PRT_MACHINEDECL P_MACHINE_Liveness = 
{
    6U,
    "Liveness",
    &P_EVENTSET_Liveness_RECV,
    &P_EVENTSET_Liveness_SEND,
    NULL,
    0U,
    2U,
    0U,
    4294967295U,
    0U,
    NULL,
    P_Liveness_STATES,
    NULL
};

PRT_TYPE* P_TYPEDEF_TimerPtr = &P_GEND_TYPE_m;
PRT_EVENTDECL* P_ALL_EVENTS[] = { &_P_EVENT_NULL_STRUCT, &_P_EVENT_HALT_STRUCT, &P_EVENT_START, &P_EVENT_TIMEOUT, &P_EVENT_PING, &P_EVENT_PONG, &P_EVENT_SUCCESS };
PRT_MACHINEDECL* P_ALL_MACHINES[] = { &P_MACHINE_Timer, &P_MACHINE_Test_1_Machine, &P_MACHINE_Test_2_Machine, &P_MACHINE_ClientMachine, &P_MACHINE_ServerMachine, &P_MACHINE_Safety, &P_MACHINE_Liveness };
PRT_INTERFACEDECL* P_ALL_INTERFACES[] = { &P_I_Timer, &P_I_Test_1_Machine, &P_I_Test_2_Machine, &P_I_ClientMachine, &P_I_ServerMachine };
PRT_FUNDECL* P_ALL_FUNCTIONS[] = { &P_FUNCTION_CreateTimer, &P_FUNCTION_StartTimer, &P_FUNCTION_StopProgram };
PRT_FOREIGNTYPEDECL* P_ALL_FOREIGN_TYPES[] = { NULL };
