#include "FailOver.h"

// Type universe for program:
static PRT_TYPE P_GEND_TYPE_n = { PRT_KIND_NULL, { NULL } };
static PRT_TYPE P_GEND_TYPE_m = { PRT_KIND_MACHINE, { NULL } };
static PRT_TYPE P_GEND_TYPE_E = { PRT_KIND_INT, { NULL } };
static PRT_TYPE P_GEND_TYPE_R_1 = { PRT_KIND_MACHINE, { NULL } };
static PRT_TYPE P_GEND_TYPE_R = { PRT_KIND_MACHINE, { NULL } };
static PRT_TYPE* P_TUP_T[] = { &P_GEND_TYPE_R_1, &P_GEND_TYPE_R };
static PRT_TUPTYPE P_TUP = { 2U, P_TUP_T };
static PRT_TYPE P_GEND_TYPE_T2RR = { PRT_KIND_TUPLE, { .tuple = &P_TUP } };
static PRT_TYPE P_GEND_TYPE_i = { PRT_KIND_INT, { NULL } };
static PRT_TYPE P_GEND_TYPE_b = { PRT_KIND_BOOL, { NULL } };

// Function implementation prototypes:
PRT_VALUE* P_Anon_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon;

PRT_VALUE* P_Anon_IMPL_1(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_1;

PRT_VALUE* P_Anon_IMPL_2(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_2;

PRT_VALUE* P_Anon_IMPL_3(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_Anon_3;

PRT_VALUE* P_PossiblyRaiseHalt_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FUNCTION_PossiblyRaiseHalt;

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


PRT_EVENTDECL P_EVENT_eDoOpI = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "eDoOpI",
    4294967295U,
    &P_GEND_TYPE_n
};

PRT_EVENTDECL P_EVENT_eDoOpJ = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "eDoOpJ",
    4294967295U,
    &P_GEND_TYPE_n
};

PRT_EVENTDECL P_EVENT_eQueryState = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "eQueryState",
    4294967295U,
    &P_GEND_TYPE_m
};

PRT_EVENTDECL P_EVENT_eQueryStateResponse = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "eQueryStateResponse",
    4294967295U,
    &P_GEND_TYPE_E
};

PRT_EVENTDECL P_EVENT_eUpdateToState0 = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "eUpdateToState0",
    4294967295U,
    &P_GEND_TYPE_n
};

PRT_EVENTDECL P_EVENT_eUpdateToState1 = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "eUpdateToState1",
    4294967295U,
    &P_GEND_TYPE_n
};

PRT_EVENTDECL* P_TestDriver_RECV_INNER[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_TestDriver_RECV =
{
    7U,
    P_TestDriver_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_TestDriver =
{
    0U,
    "TestDriver",
    &P_GEND_TYPE_n,
    &P_EVENTSET_TestDriver_RECV
};

PRT_EVENTDECL* P_FaultTolerantMachine_RECV_INNER[] = { &P_EVENT_eQueryStateResponse, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_FaultTolerantMachine_RECV =
{
    2U,
    P_FaultTolerantMachine_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_FaultTolerantMachine =
{
    1U,
    "FaultTolerantMachine",
    &P_GEND_TYPE_T2RR,
    &P_EVENTSET_FaultTolerantMachine_RECV
};

PRT_EVENTDECL* P_ServiceMachine_RECV_INNER[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_ServiceMachine_RECV =
{
    7U,
    P_ServiceMachine_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_ServiceMachine =
{
    2U,
    "ServiceMachine",
    &P_GEND_TYPE_n,
    &P_EVENTSET_ServiceMachine_RECV
};

PRT_EVENTDECL* P_ReliableStorageMachine_RECV_INNER[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_ReliableStorageMachine_RECV =
{
    7U,
    P_ReliableStorageMachine_RECV_INNER,
    NULL
};

PRT_INTERFACEDECL P_I_ReliableStorageMachine =
{
    3U,
    "ReliableStorageMachine",
    &P_GEND_TYPE_n,
    &P_EVENTSET_ReliableStorageMachine_RECV
};

PRT_VARDECL P_TestDriver_VARS[] = {
    { "reliableStorage", &P_GEND_TYPE_R },
    { "service", &P_GEND_TYPE_R_1 }
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

#define P_STATE_TestDriver_Init \
{ \
    "TestDriver.Init", \
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

PRT_STATEDECL P_TestDriver_STATES[] = { P_STATE_TestDriver_Init };

PRT_VALUE* P_Anon_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* P_VAR_m = PrtMkDefaultValue(&P_GEND_TYPE_m);
    PRT_VALUE* PTMP_tmp0 = NULL;
    PRT_VALUE* PTMP_tmp1 = NULL;
    PRT_VALUE* PTMP_tmp2 = NULL;
    PRT_VALUE* PTMP_tmp3 = NULL;
    PRT_VALUE* PTMP_tmp4 = NULL;
    PRT_VALUE* PTMP_tmp5 = NULL;
    PRT_VALUE* PTMP_tmp6 = NULL;
    PRT_VALUE* PTMP_tmp7 = NULL;
    PRT_VALUE* PTMP_tmp8 = NULL;
    PRT_VALUE* PTMP_tmp9 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE = &(PTMP_tmp0);
    PrtFreeValue(*P_LVALUE);
    *P_LVALUE = PrtCloneValue(PrtMkInterface(context, 3, 0)->id);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return;
    }
    
    {
        PRT_VALUE** P_LVALUE_1 = &(p_this->varValues[0]);
        PrtFreeValue(*P_LVALUE_1);
        *P_LVALUE_1 = PTMP_tmp0;
        PTMP_tmp0 = NULL;
    }
    
    PRT_VALUE** P_LVALUE_2 = &(PTMP_tmp1);
    PrtFreeValue(*P_LVALUE_2);
    *P_LVALUE_2 = PrtCloneValue(PrtMkInterface(context, 2, 0)->id);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return;
    }
    
    {
        PRT_VALUE** P_LVALUE_3 = &(p_this->varValues[1]);
        PrtFreeValue(*P_LVALUE_3);
        *P_LVALUE_3 = PTMP_tmp1;
        PTMP_tmp1 = NULL;
    }
    
    PRT_VALUE** P_LVALUE_4 = &(PTMP_tmp2);
    PrtFreeValue(*P_LVALUE_4);
    *P_LVALUE_4 = PrtCloneValue(p_this->varValues[1]);
    
    PRT_VALUE** P_LVALUE_5 = &(PTMP_tmp3);
    PrtFreeValue(*P_LVALUE_5);
    *P_LVALUE_5 = PrtCloneValue(p_this->varValues[0]);
    
    PRT_VALUE** P_LVALUE_6 = &(PTMP_tmp4);
    PrtFreeValue(*P_LVALUE_6);
    *P_LVALUE_6 = PrtCloneValue(PrtMkInterface(context, 1, 2, &(PTMP_tmp2), &(PTMP_tmp3))->id);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return;
    }
    
    {
        PRT_VALUE** P_LVALUE_7 = &(P_VAR_m);
        PrtFreeValue(*P_LVALUE_7);
        *P_LVALUE_7 = PTMP_tmp4;
        PTMP_tmp4 = NULL;
    }
    
    PRT_VALUE** P_LVALUE_8 = &(PTMP_tmp5);
    PrtFreeValue(*P_LVALUE_8);
    *P_LVALUE_8 = PrtCloneValue(P_VAR_m);
    
    PRT_VALUE** P_LVALUE_9 = &(PTMP_tmp6);
    PrtFreeValue(*P_LVALUE_9);
    *P_LVALUE_9 = PrtCloneValue((&_P_EVENT_HALT_STRUCT.value));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp5), PTMP_tmp6, 0);
    *(&(PTMP_tmp6)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return;
    }
    
    PRT_VALUE** P_LVALUE_10 = &(PTMP_tmp7);
    PrtFreeValue(*P_LVALUE_10);
    *P_LVALUE_10 = PrtCloneValue(p_this->varValues[1]);
    
    PRT_VALUE** P_LVALUE_11 = &(PTMP_tmp8);
    PrtFreeValue(*P_LVALUE_11);
    *P_LVALUE_11 = PrtCloneValue(p_this->varValues[0]);
    
    PRT_VALUE** P_LVALUE_12 = &(PTMP_tmp9);
    PrtFreeValue(*P_LVALUE_12);
    *P_LVALUE_12 = PrtCloneValue(PrtMkInterface(context, 1, 2, &(PTMP_tmp7), &(PTMP_tmp8))->id);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return;
    }
    
    {
        PRT_VALUE** P_LVALUE_13 = &(P_VAR_m);
        PrtFreeValue(*P_LVALUE_13);
        *P_LVALUE_13 = PTMP_tmp9;
        PTMP_tmp9 = NULL;
    }
    
p_return: ;
    PrtFreeValue(P_VAR_m); P_VAR_m = NULL;
    PrtFreeValue(PTMP_tmp0); PTMP_tmp0 = NULL;
    PrtFreeValue(PTMP_tmp1); PTMP_tmp1 = NULL;
    PrtFreeValue(PTMP_tmp2); PTMP_tmp2 = NULL;
    PrtFreeValue(PTMP_tmp3); PTMP_tmp3 = NULL;
    PrtFreeValue(PTMP_tmp4); PTMP_tmp4 = NULL;
    PrtFreeValue(PTMP_tmp5); PTMP_tmp5 = NULL;
    PrtFreeValue(PTMP_tmp6); PTMP_tmp6 = NULL;
    PrtFreeValue(PTMP_tmp7); PTMP_tmp7 = NULL;
    PrtFreeValue(PTMP_tmp8); PTMP_tmp8 = NULL;
    PrtFreeValue(PTMP_tmp9); PTMP_tmp9 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon =
{
    NULL,
    &P_Anon_IMPL,
    NULL
};


PRT_FUNDECL* P_TestDriver_METHODS[] = { &P_FUNCTION_Anon };

PRT_EVENTDECL* P_TestDriver_RECV_INNER_1[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_TestDriver_RECV_1 =
{
    7U,
    P_TestDriver_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_TestDriver_SEND_INNER[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_TestDriver_SEND =
{
    7U,
    P_TestDriver_SEND_INNER,
    NULL
};

PRT_UINT32 P_TestDriver_CREATES_ARR[] = { 1, 3, 2 };
PRT_INTERFACESETDECL P_TestDriver_CREATES = { 3, P_TestDriver_CREATES_ARR };
PRT_MACHINEDECL P_MACHINE_TestDriver = 
{
    0U,
    "TestDriver",
    &P_EVENTSET_TestDriver_RECV_1,
    &P_EVENTSET_TestDriver_SEND,
    &P_TestDriver_CREATES,
    2U,
    1U,
    1U,
    4294967295U,
    0U,
    P_TestDriver_VARS,
    P_TestDriver_STATES,
    P_TestDriver_METHODS
};

PRT_VARDECL P_FaultTolerantMachine_VARS[] = {
    { "service", &P_GEND_TYPE_R_1 },
    { "reliableStorage", &P_GEND_TYPE_R }
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

#define P_STATE_FaultTolerantMachine_Init \
{ \
    "FaultTolerantMachine.Init", \
    0U, \
    0U, \
    &P_EVENTSET_Init_DEFERS_1, \
    &P_EVENTSET_Init_TRANS_1, \
    &P_EVENTSET_Init_DOS_1, \
    NULL, \
    NULL, \
    &P_FUNCTION_Anon_1, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_State0_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_State0_DEFERS =
{
    0U,
    P_State0_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_State0_TRANS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_State0_TRANS =
{
    0U,
    P_State0_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_State0_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_State0_DOS =
{
    0U,
    P_State0_DOS_INNER,
    NULL
};

#define P_STATE_FaultTolerantMachine_State0 \
{ \
    "FaultTolerantMachine.State0", \
    0U, \
    0U, \
    &P_EVENTSET_State0_DEFERS, \
    &P_EVENTSET_State0_TRANS, \
    &P_EVENTSET_State0_DOS, \
    NULL, \
    NULL, \
    &P_FUNCTION_Anon_2, \
    &_P_NO_OP, \
}

PRT_EVENTDECL* P_State1_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_State1_DEFERS =
{
    0U,
    P_State1_DEFERS_INNER,
    NULL
};

PRT_EVENTDECL* P_State1_TRANS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_State1_TRANS =
{
    0U,
    P_State1_TRANS_INNER,
    NULL
};

PRT_EVENTDECL* P_State1_DOS_INNER[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_State1_DOS =
{
    0U,
    P_State1_DOS_INNER,
    NULL
};

#define P_STATE_FaultTolerantMachine_State1 \
{ \
    "FaultTolerantMachine.State1", \
    0U, \
    0U, \
    &P_EVENTSET_State1_DEFERS, \
    &P_EVENTSET_State1_TRANS, \
    &P_EVENTSET_State1_DOS, \
    NULL, \
    NULL, \
    &P_FUNCTION_Anon_3, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_FaultTolerantMachine_STATES[] = { P_STATE_FaultTolerantMachine_Init, P_STATE_FaultTolerantMachine_State0, P_STATE_FaultTolerantMachine_State1 };

PRT_VALUE* P_Anon_IMPL_1(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE** P_VAR_arg = argRefs[0];
    PRT_VALUE* PTMP_tmp0_1 = NULL;
    PRT_VALUE* PTMP_tmp1_1 = NULL;
    PRT_VALUE* PTMP_tmp2_1 = NULL;
    PRT_VALUE* PTMP_tmp3_1 = NULL;
    PRT_VALUE* PTMP_tmp4_1 = NULL;
    PRT_VALUE* PTMP_tmp5_1 = NULL;
    PRT_VALUE* P_VAR_P_payload = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_14 = &(PTMP_tmp0_1);
    PrtFreeValue(*P_LVALUE_14);
    *P_LVALUE_14 = PrtTupleGet(*P_VAR_arg, 0);
    
    {
        PRT_VALUE** P_LVALUE_15 = &(p_this->varValues[0]);
        PrtFreeValue(*P_LVALUE_15);
        *P_LVALUE_15 = PTMP_tmp0_1;
        PTMP_tmp0_1 = NULL;
    }
    
    PRT_VALUE** P_LVALUE_16 = &(PTMP_tmp1_1);
    PrtFreeValue(*P_LVALUE_16);
    *P_LVALUE_16 = PrtTupleGet(*P_VAR_arg, 1);
    
    {
        PRT_VALUE** P_LVALUE_17 = &(p_this->varValues[1]);
        PrtFreeValue(*P_LVALUE_17);
        *P_LVALUE_17 = PTMP_tmp1_1;
        PTMP_tmp1_1 = NULL;
    }
    
    PRT_VALUE** P_LVALUE_18 = &(PTMP_tmp2_1);
    PrtFreeValue(*P_LVALUE_18);
    *P_LVALUE_18 = PrtCloneValue(p_this->varValues[1]);
    
    PRT_VALUE** P_LVALUE_19 = &(PTMP_tmp3_1);
    PrtFreeValue(*P_LVALUE_19);
    *P_LVALUE_19 = PrtCloneValue((&P_EVENT_eQueryState.value));
    
    PRT_VALUE** P_LVALUE_20 = &(PTMP_tmp4_1);
    PrtFreeValue(*P_LVALUE_20);
    *P_LVALUE_20 = PrtCloneValue((p_this->id));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp2_1), PTMP_tmp3_1, 1, &(PTMP_tmp4_1));
    *(&(PTMP_tmp3_1)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_1;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_1;
    }
    
    PRT_UINT32 P_allowedEventIds[] = { 2 };
    PrtFreeValue(P_VAR_P_payload); P_VAR_P_payload = NULL;
    PRT_UINT32 P_eventId = PrtReceiveAsync(1U, P_allowedEventIds, &P_VAR_P_payload);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_1;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_1;
    }
    switch (P_eventId) {
        case 2: {
            PRT_VALUE** P_VAR_s = &P_VAR_P_payload;
            PRT_VALUE P_LIT_INT32 = { PRT_VALUE_KIND_INT, { .nt = 0 } };
                        PRT_VALUE** P_LVALUE_21 = &(PTMP_tmp5_1);
            PrtFreeValue(*P_LVALUE_21);
            *P_LVALUE_21 = PrtMkBoolValue(PrtIsEqualValue(*P_VAR_s, (&P_LIT_INT32)));
            
            if (PrtPrimGetBool(PTMP_tmp5_1))
            {
                PrtGoto(p_this, 1U, 0);
                
            }
            
            else
            {
                PrtGoto(p_this, 2U, 0);
                
            }
            
            
            p_return_2: ;
} break;
        default: {
            PrtAssert(PRT_FALSE, "receive returned unhandled event");
        } break;
    }
    PrtFreeValue(P_VAR_P_payload); P_VAR_P_payload = NULL;
    
p_return_1: ;
    PrtFreeValue(PTMP_tmp0_1); PTMP_tmp0_1 = NULL;
    PrtFreeValue(PTMP_tmp1_1); PTMP_tmp1_1 = NULL;
    PrtFreeValue(PTMP_tmp2_1); PTMP_tmp2_1 = NULL;
    PrtFreeValue(PTMP_tmp3_1); PTMP_tmp3_1 = NULL;
    PrtFreeValue(PTMP_tmp4_1); PTMP_tmp4_1 = NULL;
    PrtFreeValue(PTMP_tmp5_1); PTMP_tmp5_1 = NULL;
    PrtFreeValue(P_VAR_P_payload); P_VAR_P_payload = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_1 =
{
    NULL,
    &P_Anon_IMPL_1,
    &P_GEND_TYPE_T2RR
};


PRT_VALUE* P_Anon_IMPL_2(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_2 = NULL;
    PRT_VALUE* PTMP_tmp1_2 = NULL;
    PRT_VALUE* PTMP_tmp2_2 = NULL;
    PRT_VALUE* PTMP_tmp3_2 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_22 = &(PTMP_tmp0_2);
    PrtFreeValue(*P_LVALUE_22);
    *P_LVALUE_22 = PrtCloneValue(p_this->varValues[0]);
    
    PRT_VALUE** P_LVALUE_23 = &(PTMP_tmp1_2);
    PrtFreeValue(*P_LVALUE_23);
    *P_LVALUE_23 = PrtCloneValue((&P_EVENT_eDoOpI.value));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp0_2), PTMP_tmp1_2, 0);
    *(&(PTMP_tmp1_2)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_3;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_3;
    }
    
    PrtFreeValue(P_PossiblyRaiseHalt_IMPL(context, _P_GEN_funargs));
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_3;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_3;
    }
    
    PRT_VALUE** P_LVALUE_24 = &(PTMP_tmp2_2);
    PrtFreeValue(*P_LVALUE_24);
    *P_LVALUE_24 = PrtCloneValue(p_this->varValues[1]);
    
    PRT_VALUE** P_LVALUE_25 = &(PTMP_tmp3_2);
    PrtFreeValue(*P_LVALUE_25);
    *P_LVALUE_25 = PrtCloneValue((&P_EVENT_eUpdateToState1.value));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp2_2), PTMP_tmp3_2, 0);
    *(&(PTMP_tmp3_2)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_3;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_3;
    }
    
    PrtGoto(p_this, 2U, 0);
    
p_return_3: ;
    PrtFreeValue(PTMP_tmp0_2); PTMP_tmp0_2 = NULL;
    PrtFreeValue(PTMP_tmp1_2); PTMP_tmp1_2 = NULL;
    PrtFreeValue(PTMP_tmp2_2); PTMP_tmp2_2 = NULL;
    PrtFreeValue(PTMP_tmp3_2); PTMP_tmp3_2 = NULL;
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
    PRT_VALUE* PTMP_tmp0_3 = NULL;
    PRT_VALUE* PTMP_tmp1_3 = NULL;
    PRT_VALUE* PTMP_tmp2_3 = NULL;
    PRT_VALUE* PTMP_tmp3_3 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_26 = &(PTMP_tmp0_3);
    PrtFreeValue(*P_LVALUE_26);
    *P_LVALUE_26 = PrtCloneValue(p_this->varValues[0]);
    
    PRT_VALUE** P_LVALUE_27 = &(PTMP_tmp1_3);
    PrtFreeValue(*P_LVALUE_27);
    *P_LVALUE_27 = PrtCloneValue((&P_EVENT_eDoOpJ.value));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp0_3), PTMP_tmp1_3, 0);
    *(&(PTMP_tmp1_3)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_4;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_4;
    }
    
    PrtFreeValue(P_PossiblyRaiseHalt_IMPL(context, _P_GEN_funargs));
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_4;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_4;
    }
    
    PRT_VALUE** P_LVALUE_28 = &(PTMP_tmp2_3);
    PrtFreeValue(*P_LVALUE_28);
    *P_LVALUE_28 = PrtCloneValue(p_this->varValues[1]);
    
    PRT_VALUE** P_LVALUE_29 = &(PTMP_tmp3_3);
    PrtFreeValue(*P_LVALUE_29);
    *P_LVALUE_29 = PrtCloneValue((&P_EVENT_eUpdateToState0.value));
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp2_3), PTMP_tmp3_3, 0);
    *(&(PTMP_tmp3_3)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_4;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_4;
    }
    
    PrtGoto(p_this, 1U, 0);
    
p_return_4: ;
    PrtFreeValue(PTMP_tmp0_3); PTMP_tmp0_3 = NULL;
    PrtFreeValue(PTMP_tmp1_3); PTMP_tmp1_3 = NULL;
    PrtFreeValue(PTMP_tmp2_3); PTMP_tmp2_3 = NULL;
    PrtFreeValue(PTMP_tmp3_3); PTMP_tmp3_3 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_3 =
{
    NULL,
    &P_Anon_IMPL_3,
    NULL
};


PRT_VALUE* P_PossiblyRaiseHalt_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_4 = NULL;
    PRT_VALUE* P_VAR_P_payload_1 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_UINT32 P_allowedEventIds_1[] = { 1, 0 };
    PrtFreeValue(P_VAR_P_payload_1); P_VAR_P_payload_1 = NULL;
    PRT_UINT32 P_eventId_1 = PrtReceiveAsync(2U, P_allowedEventIds_1, &P_VAR_P_payload_1);
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_5;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_5;
    }
    switch (P_eventId_1) {
        case 1: {
                        PRT_VALUE** P_LVALUE_30 = &(PTMP_tmp0_4);
            PrtFreeValue(*P_LVALUE_30);
            *P_LVALUE_30 = PrtCloneValue((&_P_EVENT_HALT_STRUCT.value));
            
            PrtRaise(p_this, PTMP_tmp0_4, 0);
            *(&(PTMP_tmp0_4)) = NULL;
            goto p_return_5;
            
            p_return_6: ;
} break;
        case 0: {
                        p_return_7: ;
} break;
        default: {
            PrtAssert(PRT_FALSE, "receive returned unhandled event");
        } break;
    }
    PrtFreeValue(P_VAR_P_payload_1); P_VAR_P_payload_1 = NULL;
    
p_return_5: ;
    PrtFreeValue(PTMP_tmp0_4); PTMP_tmp0_4 = NULL;
    PrtFreeValue(P_VAR_P_payload_1); P_VAR_P_payload_1 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_PossiblyRaiseHalt =
{
    "PossiblyRaiseHalt",
    &P_PossiblyRaiseHalt_IMPL,
    NULL
};


PRT_FUNDECL* P_FaultTolerantMachine_METHODS[] = { &P_FUNCTION_Anon_1, &P_FUNCTION_Anon_2, &P_FUNCTION_Anon_3, &P_FUNCTION_PossiblyRaiseHalt };

PRT_EVENTDECL* P_FaultTolerantMachine_RECV_INNER_1[] = { &P_EVENT_eQueryStateResponse, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_FaultTolerantMachine_RECV_1 =
{
    2U,
    P_FaultTolerantMachine_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_FaultTolerantMachine_SEND_INNER[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_FaultTolerantMachine_SEND =
{
    7U,
    P_FaultTolerantMachine_SEND_INNER,
    NULL
};

PRT_MACHINEDECL P_MACHINE_FaultTolerantMachine = 
{
    1U,
    "FaultTolerantMachine",
    &P_EVENTSET_FaultTolerantMachine_RECV_1,
    &P_EVENTSET_FaultTolerantMachine_SEND,
    NULL,
    2U,
    3U,
    4U,
    4294967295U,
    0U,
    P_FaultTolerantMachine_VARS,
    P_FaultTolerantMachine_STATES,
    P_FaultTolerantMachine_METHODS
};

PRT_VARDECL P_ServiceMachine_VARS[] = {
    { "i", &P_GEND_TYPE_i },
    { "j", &P_GEND_TYPE_i },
    { "donei", &P_GEND_TYPE_b },
    { "donej", &P_GEND_TYPE_b }
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

PRT_EVENTDECL* P_Init_DOS_INNER_2[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ };
PRT_EVENTSETDECL P_EVENTSET_Init_DOS_2 =
{
    2U,
    P_Init_DOS_INNER_2,
    NULL
};

PRT_DODECL P_DOS[] =
{
    { 0, &P_EVENT_eDoOpI, &P_FUNCTION_Anon_4 },
    { 0, &P_EVENT_eDoOpJ, &P_FUNCTION_Anon_5 }
};

#define P_STATE_ServiceMachine_Init \
{ \
    "ServiceMachine.Init", \
    0U, \
    2U, \
    &P_EVENTSET_Init_DEFERS_2, \
    &P_EVENTSET_Init_TRANS_2, \
    &P_EVENTSET_Init_DOS_2, \
    NULL, \
    P_DOS, \
    &_P_NO_OP, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_ServiceMachine_STATES[] = { P_STATE_ServiceMachine_Init };

PRT_VALUE* P_Anon_IMPL_4(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE* PTMP_tmp0_5 = NULL;
    PRT_VALUE* PTMP_tmp1_4 = NULL;
    PRT_VALUE* PTMP_tmp2_4 = NULL;
    PRT_VALUE* PTMP_tmp3_4 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_1 = { PRT_VALUE_KIND_INT, { .nt = 1 } };
    PRT_VALUE P_LIT_BOOLEAN = { PRT_VALUE_KIND_BOOL, { .bl = PRT_TRUE } };
    PRT_VALUE P_LIT_BOOLEAN_1 = { PRT_VALUE_KIND_BOOL, { .bl = PRT_FALSE } };
    PRT_VALUE** P_LVALUE_31 = &(PTMP_tmp0_5);
    PrtFreeValue(*P_LVALUE_31);
    *P_LVALUE_31 = PrtMkBoolValue(!PrtPrimGetBool(p_this->varValues[2]));
    
    if (PrtPrimGetBool(PTMP_tmp0_5))
    {
        PRT_VALUE** P_LVALUE_32 = &(PTMP_tmp1_4);
        PrtFreeValue(*P_LVALUE_32);
        *P_LVALUE_32 = PrtMkIntValue(PrtPrimGetInt(p_this->varValues[0]) + PrtPrimGetInt((&P_LIT_INT32_1)));
        
        {
            PRT_VALUE** P_LVALUE_33 = &(p_this->varValues[0]);
            PrtFreeValue(*P_LVALUE_33);
            *P_LVALUE_33 = PTMP_tmp1_4;
            PTMP_tmp1_4 = NULL;
        }
        
        PRT_VALUE** P_LVALUE_34 = &(p_this->varValues[2]);
        PrtFreeValue(*P_LVALUE_34);
        *P_LVALUE_34 = PrtCloneValue((&P_LIT_BOOLEAN));
        
    }
    
    else
    {
    }
    
    
    PRT_VALUE** P_LVALUE_35 = &(p_this->varValues[3]);
    PrtFreeValue(*P_LVALUE_35);
    *P_LVALUE_35 = PrtCloneValue((&P_LIT_BOOLEAN_1));
    
    PRT_VALUE** P_LVALUE_36 = &(PTMP_tmp2_4);
    PrtFreeValue(*P_LVALUE_36);
    *P_LVALUE_36 = PrtMkIntValue(PrtPrimGetInt(p_this->varValues[1]) + PrtPrimGetInt((&P_LIT_INT32_1)));
    
    PRT_VALUE** P_LVALUE_37 = &(PTMP_tmp3_4);
    PrtFreeValue(*P_LVALUE_37);
    *P_LVALUE_37 = PrtMkBoolValue(PrtIsEqualValue(p_this->varValues[0], PTMP_tmp2_4));
    
    PrtAssert(PrtPrimGetBool(PTMP_tmp3_4), "");
    
p_return_8: ;
    PrtFreeValue(PTMP_tmp0_5); PTMP_tmp0_5 = NULL;
    PrtFreeValue(PTMP_tmp1_4); PTMP_tmp1_4 = NULL;
    PrtFreeValue(PTMP_tmp2_4); PTMP_tmp2_4 = NULL;
    PrtFreeValue(PTMP_tmp3_4); PTMP_tmp3_4 = NULL;
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
    PRT_VALUE* PTMP_tmp0_6 = NULL;
    PRT_VALUE* PTMP_tmp1_5 = NULL;
    PRT_VALUE* PTMP_tmp2_5 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_2 = { PRT_VALUE_KIND_INT, { .nt = 1 } };
    PRT_VALUE P_LIT_BOOLEAN_2 = { PRT_VALUE_KIND_BOOL, { .bl = PRT_TRUE } };
    PRT_VALUE P_LIT_BOOLEAN_3 = { PRT_VALUE_KIND_BOOL, { .bl = PRT_FALSE } };
    PRT_VALUE** P_LVALUE_38 = &(PTMP_tmp0_6);
    PrtFreeValue(*P_LVALUE_38);
    *P_LVALUE_38 = PrtMkBoolValue(!PrtPrimGetBool(p_this->varValues[3]));
    
    if (PrtPrimGetBool(PTMP_tmp0_6))
    {
        PRT_VALUE** P_LVALUE_39 = &(PTMP_tmp1_5);
        PrtFreeValue(*P_LVALUE_39);
        *P_LVALUE_39 = PrtMkIntValue(PrtPrimGetInt(p_this->varValues[1]) + PrtPrimGetInt((&P_LIT_INT32_2)));
        
        {
            PRT_VALUE** P_LVALUE_40 = &(p_this->varValues[1]);
            PrtFreeValue(*P_LVALUE_40);
            *P_LVALUE_40 = PTMP_tmp1_5;
            PTMP_tmp1_5 = NULL;
        }
        
        PRT_VALUE** P_LVALUE_41 = &(p_this->varValues[3]);
        PrtFreeValue(*P_LVALUE_41);
        *P_LVALUE_41 = PrtCloneValue((&P_LIT_BOOLEAN_2));
        
    }
    
    else
    {
    }
    
    
    PRT_VALUE** P_LVALUE_42 = &(p_this->varValues[2]);
    PrtFreeValue(*P_LVALUE_42);
    *P_LVALUE_42 = PrtCloneValue((&P_LIT_BOOLEAN_3));
    
    PRT_VALUE** P_LVALUE_43 = &(PTMP_tmp2_5);
    PrtFreeValue(*P_LVALUE_43);
    *P_LVALUE_43 = PrtMkBoolValue(PrtIsEqualValue(p_this->varValues[0], p_this->varValues[1]));
    
    PrtAssert(PrtPrimGetBool(PTMP_tmp2_5), "");
    
p_return_9: ;
    PrtFreeValue(PTMP_tmp0_6); PTMP_tmp0_6 = NULL;
    PrtFreeValue(PTMP_tmp1_5); PTMP_tmp1_5 = NULL;
    PrtFreeValue(PTMP_tmp2_5); PTMP_tmp2_5 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_5 =
{
    NULL,
    &P_Anon_IMPL_5,
    NULL
};


PRT_FUNDECL* P_ServiceMachine_METHODS[] = { &P_FUNCTION_Anon_4, &P_FUNCTION_Anon_5 };

PRT_EVENTDECL* P_ServiceMachine_RECV_INNER_1[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_ServiceMachine_RECV_1 =
{
    7U,
    P_ServiceMachine_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_ServiceMachine_SEND_INNER[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_ServiceMachine_SEND =
{
    7U,
    P_ServiceMachine_SEND_INNER,
    NULL
};

PRT_MACHINEDECL P_MACHINE_ServiceMachine = 
{
    2U,
    "ServiceMachine",
    &P_EVENTSET_ServiceMachine_RECV_1,
    &P_EVENTSET_ServiceMachine_SEND,
    NULL,
    4U,
    1U,
    2U,
    4294967295U,
    0U,
    P_ServiceMachine_VARS,
    P_ServiceMachine_STATES,
    P_ServiceMachine_METHODS
};

PRT_VARDECL P_ReliableStorageMachine_VARS[] = {
    { "s", &P_GEND_TYPE_E }
};

PRT_EVENTDECL* P_Init_DEFERS_INNER_3[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_DEFERS_3 =
{
    0U,
    P_Init_DEFERS_INNER_3,
    NULL
};

PRT_EVENTDECL* P_Init_TRANS_INNER_3[] = { NULL };
PRT_EVENTSETDECL P_EVENTSET_Init_TRANS_3 =
{
    0U,
    P_Init_TRANS_INNER_3,
    NULL
};

PRT_EVENTDECL* P_Init_DOS_INNER_3[] = { &P_EVENT_eQueryState, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1 };
PRT_EVENTSETDECL P_EVENTSET_Init_DOS_3 =
{
    3U,
    P_Init_DOS_INNER_3,
    NULL
};

PRT_DODECL P_DOS_1[] =
{
    { 0, &P_EVENT_eQueryState, &P_FUNCTION_Anon_7 },
    { 0, &P_EVENT_eUpdateToState0, &P_FUNCTION_Anon_8 },
    { 0, &P_EVENT_eUpdateToState1, &P_FUNCTION_Anon_9 }
};

#define P_STATE_ReliableStorageMachine_Init \
{ \
    "ReliableStorageMachine.Init", \
    0U, \
    3U, \
    &P_EVENTSET_Init_DEFERS_3, \
    &P_EVENTSET_Init_TRANS_3, \
    &P_EVENTSET_Init_DOS_3, \
    NULL, \
    P_DOS_1, \
    &P_FUNCTION_Anon_6, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_ReliableStorageMachine_STATES[] = { P_STATE_ReliableStorageMachine_Init };

PRT_VALUE* P_Anon_IMPL_6(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_3 = { PRT_VALUE_KIND_INT, { .nt = 0 } };
    PRT_VALUE** P_LVALUE_44 = &(p_this->varValues[0]);
    PrtFreeValue(*P_LVALUE_44);
    *P_LVALUE_44 = PrtCloneValue((&P_LIT_INT32_3));
    
p_return_10: ;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_6 =
{
    NULL,
    &P_Anon_IMPL_6,
    NULL
};


PRT_VALUE* P_Anon_IMPL_7(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE** P_VAR_m_1 = argRefs[0];
    PRT_VALUE* PTMP_tmp0_7 = NULL;
    PRT_VALUE* PTMP_tmp1_6 = NULL;
    PRT_VALUE* PTMP_tmp2_6 = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE** P_LVALUE_45 = &(PTMP_tmp0_7);
    PrtFreeValue(*P_LVALUE_45);
    *P_LVALUE_45 = PrtCloneValue(*P_VAR_m_1);
    
    PRT_VALUE** P_LVALUE_46 = &(PTMP_tmp1_6);
    PrtFreeValue(*P_LVALUE_46);
    *P_LVALUE_46 = PrtCloneValue((&P_EVENT_eQueryStateResponse.value));
    
    PRT_VALUE** P_LVALUE_47 = &(PTMP_tmp2_6);
    PrtFreeValue(*P_LVALUE_47);
    *P_LVALUE_47 = PrtCloneValue(p_this->varValues[0]);
    
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp0_7), PTMP_tmp1_6, 1, &(PTMP_tmp2_6));
    *(&(PTMP_tmp1_6)) = NULL;
    if (p_this->returnKind != ReturnStatement && p_this->returnKind != ReceiveStatement) {
        goto p_return_11;
    }
    if (p_this->isHalted == PRT_TRUE) {
        PrtFreeValue(_P_GEN_retval);
        _P_GEN_retval = NULL;
        goto p_return_11;
    }
    
p_return_11: ;
    PrtFreeValue(PTMP_tmp0_7); PTMP_tmp0_7 = NULL;
    PrtFreeValue(PTMP_tmp1_6); PTMP_tmp1_6 = NULL;
    PrtFreeValue(PTMP_tmp2_6); PTMP_tmp2_6 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_7 =
{
    NULL,
    &P_Anon_IMPL_7,
    &P_GEND_TYPE_m
};


PRT_VALUE* P_Anon_IMPL_8(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_4 = { PRT_VALUE_KIND_INT, { .nt = 0 } };
    PRT_VALUE** P_LVALUE_48 = &(p_this->varValues[0]);
    PrtFreeValue(*P_LVALUE_48);
    *P_LVALUE_48 = PrtCloneValue((&P_LIT_INT32_4));
    
p_return_12: ;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_8 =
{
    NULL,
    &P_Anon_IMPL_8,
    NULL
};


PRT_VALUE* P_Anon_IMPL_9(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
    PRT_VALUE P_LIT_INT32_5 = { PRT_VALUE_KIND_INT, { .nt = 1 } };
    PRT_VALUE** P_LVALUE_49 = &(p_this->varValues[0]);
    PrtFreeValue(*P_LVALUE_49);
    *P_LVALUE_49 = PrtCloneValue((&P_LIT_INT32_5));
    
p_return_13: ;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FUNCTION_Anon_9 =
{
    NULL,
    &P_Anon_IMPL_9,
    NULL
};


PRT_FUNDECL* P_ReliableStorageMachine_METHODS[] = { &P_FUNCTION_Anon_6, &P_FUNCTION_Anon_7, &P_FUNCTION_Anon_8, &P_FUNCTION_Anon_9 };

PRT_EVENTDECL* P_ReliableStorageMachine_RECV_INNER_1[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_ReliableStorageMachine_RECV_1 =
{
    7U,
    P_ReliableStorageMachine_RECV_INNER_1,
    NULL
};

PRT_EVENTDECL* P_ReliableStorageMachine_SEND_INNER[] = { &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eQueryStateResponse, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1, &_P_EVENT_HALT_STRUCT };
PRT_EVENTSETDECL P_EVENTSET_ReliableStorageMachine_SEND =
{
    7U,
    P_ReliableStorageMachine_SEND_INNER,
    NULL
};

PRT_MACHINEDECL P_MACHINE_ReliableStorageMachine = 
{
    3U,
    "ReliableStorageMachine",
    &P_EVENTSET_ReliableStorageMachine_RECV_1,
    &P_EVENTSET_ReliableStorageMachine_SEND,
    NULL,
    1U,
    1U,
    4U,
    4294967295U,
    0U,
    P_ReliableStorageMachine_VARS,
    P_ReliableStorageMachine_STATES,
    P_ReliableStorageMachine_METHODS
};

PRT_TYPE* P_TYPEDEF_Pair = &P_GEND_TYPE_T2RR;
PRT_EVENTDECL* P_ALL_EVENTS[] = { &_P_EVENT_NULL_STRUCT, &_P_EVENT_HALT_STRUCT, &P_EVENT_eQueryStateResponse, &P_EVENT_eDoOpI, &P_EVENT_eDoOpJ, &P_EVENT_eQueryState, &P_EVENT_eUpdateToState0, &P_EVENT_eUpdateToState1 };
PRT_MACHINEDECL* P_ALL_MACHINES[] = { &P_MACHINE_TestDriver, &P_MACHINE_FaultTolerantMachine, &P_MACHINE_ServiceMachine, &P_MACHINE_ReliableStorageMachine };
PRT_INTERFACEDECL* P_ALL_INTERFACES[] = { &P_I_TestDriver, &P_I_FaultTolerantMachine, &P_I_ServiceMachine, &P_I_ReliableStorageMachine };
PRT_FUNDECL* P_ALL_FUNCTIONS[] = { NULL };
PRT_FOREIGNTYPEDECL* P_ALL_FOREIGN_TYPES[] = { NULL };
