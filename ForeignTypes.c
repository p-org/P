#include "ForeignTypes.h"

// Type universe for program:
static PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_m = { PRT_KIND_MACHINE, { NULL } };
static PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_a = { PRT_KIND_ANY, { NULL } };
static PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_e = { PRT_KIND_EVENT, { NULL } };
// TODO: implement full permission types in runtime
static PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_R = { PRT_KIND_MACHINE, { NULL } };
// TODO: implement full permission types in runtime
static PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_R_1 = { PRT_KIND_MACHINE, { NULL } };
static PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_n = { PRT_KIND_NULL, { NULL } };
extern PRT_UINT64 PRT_FOREIGN_MKDEF_StringType_IMPL(void);
extern PRT_UINT64 PRT_FOREIGN_CLONE_StringType_IMPL(PRT_UINT64);
extern void PRT_FOREIGN_FREE_StringType_IMPL(PRT_UINT64);
extern PRT_UINT32 PRT_FOREIGN_GETHASHCODE_StringType_IMPL(PRT_UINT64);
extern PRT_BOOLEAN PRT_FOREIGN_ISEQUAL_StringType_IMPL(PRT_UINT64, PRT_UINT64);
extern PRT_STRING PRT_FOREIGN_MKDEF_StringType_IMPL(PRT_UINT64);
static PRT_FOREIGNTYPEDECL P_FOREIGNTYPES_FOREIGNTYPE = {
    0U,
    "StringType",
    PRT_FOREIGN_MKDEF_StringType_IMPL,
    PRT_FOREIGN_CLONE_StringType_IMPL,
    PRT_FOREIGN_FREE_StringType_IMPL,
    PRT_FOREIGN_GETHASHCODE_StringType_IMPL,
    PRT_FOREIGN_ISEQUAL_StringType_IMPL,
    PRT_FOREIGN_TOSTRING_StringType_IMPL,
};
PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_StringType = { PRT_KIND_FOREIGN, { .foreignType = &P_FOREIGNTYPES_FOREIGNTYPE } };
static PRT_TYPE* P_FOREIGNTYPES_TUP_T[] = { &P_FOREIGNTYPES_GEND_TYPE_R_1, &P_FOREIGNTYPES_GEND_TYPE_StringType };
static PRT_TUPTYPE P_FOREIGNTYPES_TUP = { 2U, P_FOREIGNTYPES_TUP_T };
static PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_T2RStringType = { PRT_KIND_TUPLE, { .tuple = &P_FOREIGNTYPES_TUP } };
static PRT_TYPE* P_FOREIGNTYPES_TUP_T_1[] = { &P_FOREIGNTYPES_GEND_TYPE_m, &P_FOREIGNTYPES_GEND_TYPE_a };
static PRT_TUPTYPE P_FOREIGNTYPES_TUP_1 = { 2U, P_FOREIGNTYPES_TUP_T_1 };
static PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_T2ma = { PRT_KIND_TUPLE, { .tuple = &P_FOREIGNTYPES_TUP_1 } };
static PRT_TYPE P_FOREIGNTYPES_GEND_TYPE_b = { PRT_KIND_BOOL, { NULL } };

// Function implementation prototypes:
PRT_VALUE* P_FUN_GetPassword_FOREIGN(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);

PRT_VALUE* P_FOREIGNTYPES_Anon_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FOREIGNTYPES_FUNCTION_Anon;

PRT_VALUE* P_FOREIGNTYPES_Anon_IMPL_1(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FOREIGNTYPES_FUNCTION_Anon_1;

PRT_VALUE* P_FOREIGNTYPES_Anon_IMPL_2(PRT_MACHINEINST* context, PRT_VALUE*** argRefs);
extern PRT_FUNDECL P_FOREIGNTYPES_FUNCTION_Anon_2;


#line 6 "foreignstringtype.p"
PRT_EVENTDECL P_FOREIGNTYPES_EVENT_sendback = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "sendback",
    4294967295U,
    &P_FOREIGNTYPES_GEND_TYPE_T2ma
};

#line 7 "foreignstringtype.p"
PRT_EVENTDECL P_FOREIGNTYPES_EVENT_getback = 
{
    { PRT_VALUE_KIND_EVENT, 0U },
    "getback",
    4294967295U,
    &P_FOREIGNTYPES_GEND_TYPE_a
};


#line 10 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_TestMachine_RECV_INNER[] = { &P_FOREIGNTYPES_EVENT_getback, &P_FOREIGNTYPES_EVENT_sendback };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_TestMachine_RECV =
{
    2U,
    P_FOREIGNTYPES_TestMachine_RECV_INNER,
    NULL
};

#line 10 "foreignstringtype.p"
PRT_INTERFACEDECL P_FOREIGNTYPES_I_TestMachine =
{
    0U,
    "TestMachine",
    &P_FOREIGNTYPES_GEND_TYPE_n,
    &P_FOREIGNTYPES_EVENTSET_TestMachine_RECV
};

#line 28 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_ForwardingMachine_RECV_INNER[] = { &P_FOREIGNTYPES_EVENT_getback, &P_FOREIGNTYPES_EVENT_sendback };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_ForwardingMachine_RECV =
{
    2U,
    P_FOREIGNTYPES_ForwardingMachine_RECV_INNER,
    NULL
};

#line 28 "foreignstringtype.p"
PRT_INTERFACEDECL P_FOREIGNTYPES_I_ForwardingMachine =
{
    1U,
    "ForwardingMachine",
    &P_FOREIGNTYPES_GEND_TYPE_n,
    &P_FOREIGNTYPES_EVENTSET_ForwardingMachine_RECV
};

PRT_VARDECL P_FOREIGNTYPES_TestMachine_VARS[] = {
    { "someStringV", &P_FOREIGNTYPES_GEND_TYPE_StringType }
};

#line 14 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_Init_DEFERS_INNER[] = { NULL };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_Init_DEFERS =
{
    0U,
    P_FOREIGNTYPES_Init_DEFERS_INNER,
    NULL
};

#line 14 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_Init_TRANS_INNER[] = { NULL };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_Init_TRANS =
{
    0U,
    P_FOREIGNTYPES_Init_TRANS_INNER,
    NULL
};

#line 14 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_Init_DOS_INNER[] = { &P_FOREIGNTYPES_EVENT_getback };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_Init_DOS =
{
    1U,
    P_FOREIGNTYPES_Init_DOS_INNER,
    NULL
};

PRT_DODECL P_FOREIGNTYPES_DOS[] =
{
    { 0, &P_FOREIGNTYPES_EVENT_getback, &P_FOREIGNTYPES_FUNCTION_Anon_1 }
};

#line 14 "foreignstringtype.p"
#define P_FOREIGNTYPES_STATE_TestMachine_Init \
{ \
    "TestMachine.Init", \
    0U, \
    1U, \
    &P_FOREIGNTYPES_EVENTSET_Init_DEFERS, \
    &P_FOREIGNTYPES_EVENTSET_Init_TRANS, \
    &P_FOREIGNTYPES_EVENTSET_Init_DOS, \
    NULL, \
    P_FOREIGNTYPES_DOS, \
    &P_FOREIGNTYPES_FUNCTION_Anon, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_FOREIGNTYPES_TestMachine_STATES[] = { P_FOREIGNTYPES_STATE_TestMachine_Init };

#line 15 "foreignstringtype.p"
PRT_VALUE* P_FOREIGNTYPES_Anon_IMPL(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
#line 15 "foreignstringtype.p"
    PRT_VALUE* P_FOREIGNTYPES_VAR_fMachine = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_m);
    PRT_VALUE* PTMP_tmp0 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_R);
    PRT_VALUE* PTMP_tmp1 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_StringType);
    PRT_VALUE* PTMP_tmp2 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_m);
    PRT_VALUE* PTMP_tmp3 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_e);
    PRT_VALUE* PTMP_tmp4 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_R_1);
    PRT_VALUE* PTMP_tmp5 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_StringType);
    PRT_VALUE* PTMP_tmp6 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_T2RStringType);
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
#line 15 "foreignstringtype.p"
#line 17 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE = &(PTMP_tmp0);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE);
    *P_FOREIGNTYPES_LVALUE = PrtCloneValue(PrtMkInterface(context, 1, 0)->id);
    
#line 17 "foreignstringtype.p"
    {
        PRT_VALUE** P_FOREIGNTYPES_LVALUE_1 = &(P_FOREIGNTYPES_VAR_fMachine);
        PrtFreeValue(*P_FOREIGNTYPES_LVALUE_1);
        *P_FOREIGNTYPES_LVALUE_1 = PTMP_tmp0;
        PTMP_tmp0 = NULL;
    }
    
#line 18 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_2 = &(PTMP_tmp1);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_2);
    *P_FOREIGNTYPES_LVALUE_2 = ((_P_GEN_funval = P_FUN_GetPassword_FOREIGN(context, _P_GEN_funargs)), (_P_GEN_funval));
    
#line 18 "foreignstringtype.p"
    {
        PRT_VALUE** P_FOREIGNTYPES_LVALUE_3 = &(p_this->varValues[0]);
        PrtFreeValue(*P_FOREIGNTYPES_LVALUE_3);
        *P_FOREIGNTYPES_LVALUE_3 = PTMP_tmp1;
        PTMP_tmp1 = NULL;
    }
    
#line 19 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_4 = &(PTMP_tmp2);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_4);
    *P_FOREIGNTYPES_LVALUE_4 = PrtCloneValue(P_FOREIGNTYPES_VAR_fMachine);
    
#line 19 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_5 = &(PTMP_tmp3);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_5);
    *P_FOREIGNTYPES_LVALUE_5 = PrtCloneValue((&P_FOREIGNTYPES_EVENT_sendback.value));
    
#line 19 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_6 = &(PTMP_tmp4);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_6);
    *P_FOREIGNTYPES_LVALUE_6 = PrtCloneValue((p_this->id));
    
#line 19 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_7 = &(PTMP_tmp5);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_7);
    *P_FOREIGNTYPES_LVALUE_7 = PrtCloneValue(p_this->varValues[0]);
    
#line 19 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_8 = &(PTMP_tmp6);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_8);
    *P_FOREIGNTYPES_LVALUE_8 = (PrtMkTuple(&P_FOREIGNTYPES_GEND_TYPE_T2RStringType, &(PTMP_tmp4), &(PTMP_tmp5)));
    
#line 19 "foreignstringtype.p"
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp2), PTMP_tmp3, 1, &(PTMP_tmp6));
    *(&(PTMP_tmp3)) = NULL;
    
p_return:
    PrtFreeValue(P_FOREIGNTYPES_VAR_fMachine); P_FOREIGNTYPES_VAR_fMachine = NULL;
    PrtFreeValue(PTMP_tmp0); PTMP_tmp0 = NULL;
    PrtFreeValue(PTMP_tmp1); PTMP_tmp1 = NULL;
    PrtFreeValue(PTMP_tmp2); PTMP_tmp2 = NULL;
    PrtFreeValue(PTMP_tmp3); PTMP_tmp3 = NULL;
    PrtFreeValue(PTMP_tmp4); PTMP_tmp4 = NULL;
    PrtFreeValue(PTMP_tmp5); PTMP_tmp5 = NULL;
    PrtFreeValue(PTMP_tmp6); PTMP_tmp6 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FOREIGNTYPES_FUNCTION_Anon =
{
    NULL,
    &P_FOREIGNTYPES_Anon_IMPL,
    NULL
};


#line 22 "foreignstringtype.p"
PRT_VALUE* P_FOREIGNTYPES_Anon_IMPL_1(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
#line 22 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_VAR_payload = argRefs[0];
    PRT_VALUE* PTMP_tmp0_1 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_StringType);
    PRT_VALUE* PTMP_tmp1_1 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_b);
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
#line 22 "foreignstringtype.p"
#line 23 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_9 = &(PTMP_tmp0_1);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_9);
    *P_FOREIGNTYPES_LVALUE_9 = PrtCloneValue(PrtCastValue(*P_FOREIGNTYPES_VAR_payload, &P_FOREIGNTYPES_GEND_TYPE_StringType));
    
#line 23 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_10 = &(PTMP_tmp1_1);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_10);
    *P_FOREIGNTYPES_LVALUE_10 = PrtMkBoolValue(PrtIsEqualValue(PTMP_tmp0_1, p_this->varValues[0]));
    
#line 23 "foreignstringtype.p"
    PrtAssert(PrtPrimGetBool(PTMP_tmp1_1), "");
    
p_return:
    PrtFreeValue(PTMP_tmp0_1); PTMP_tmp0_1 = NULL;
    PrtFreeValue(PTMP_tmp1_1); PTMP_tmp1_1 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FOREIGNTYPES_FUNCTION_Anon_1 =
{
    NULL,
    &P_FOREIGNTYPES_Anon_IMPL_1,
    &P_FOREIGNTYPES_GEND_TYPE_a
};


PRT_FUNDECL* P_FOREIGNTYPES_TestMachine_METHODS[] = { &P_FOREIGNTYPES_FUNCTION_Anon, &P_FOREIGNTYPES_FUNCTION_Anon_1 };

#line 10 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_TestMachine_RECV_INNER_1[] = { &P_FOREIGNTYPES_EVENT_getback, &P_FOREIGNTYPES_EVENT_sendback };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_TestMachine_RECV_1 =
{
    2U,
    P_FOREIGNTYPES_TestMachine_RECV_INNER_1,
    NULL
};

#line 10 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_TestMachine_SEND_INNER[] = { &P_FOREIGNTYPES_EVENT_getback, &P_FOREIGNTYPES_EVENT_sendback };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_TestMachine_SEND =
{
    2U,
    P_FOREIGNTYPES_TestMachine_SEND_INNER,
    NULL
};

PRT_UINT32 P_FOREIGNTYPES_TestMachine_CREATES_ARR[] = { 1 };
PRT_INTERFACESETDECL P_FOREIGNTYPES_TestMachine_CREATES = { 1, P_FOREIGNTYPES_TestMachine_CREATES_ARR };
#line 10 "foreignstringtype.p"
PRT_MACHINEDECL P_FOREIGNTYPES_MACHINE_TestMachine = 
{
    0U,
    "TestMachine",
    &P_FOREIGNTYPES_EVENTSET_TestMachine_RECV_1,
    &P_FOREIGNTYPES_EVENTSET_TestMachine_SEND,
    &P_FOREIGNTYPES_TestMachine_CREATES,
    1U,
    1U,
    2U,
    4294967295U,
    0U,
    P_FOREIGNTYPES_TestMachine_VARS,
    P_FOREIGNTYPES_TestMachine_STATES,
    P_FOREIGNTYPES_TestMachine_METHODS
};

#line 29 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_Init_DEFERS_INNER_1[] = { NULL };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_Init_DEFERS_1 =
{
    0U,
    P_FOREIGNTYPES_Init_DEFERS_INNER_1,
    NULL
};

#line 29 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_Init_TRANS_INNER_1[] = { NULL };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_Init_TRANS_1 =
{
    0U,
    P_FOREIGNTYPES_Init_TRANS_INNER_1,
    NULL
};

#line 29 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_Init_DOS_INNER_1[] = { &P_FOREIGNTYPES_EVENT_sendback };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_Init_DOS_1 =
{
    1U,
    P_FOREIGNTYPES_Init_DOS_INNER_1,
    NULL
};

PRT_DODECL P_FOREIGNTYPES_DOS_1[] =
{
    { 0, &P_FOREIGNTYPES_EVENT_sendback, &P_FOREIGNTYPES_FUNCTION_Anon_2 }
};

#line 29 "foreignstringtype.p"
#define P_FOREIGNTYPES_STATE_ForwardingMachine_Init \
{ \
    "ForwardingMachine.Init", \
    0U, \
    1U, \
    &P_FOREIGNTYPES_EVENTSET_Init_DEFERS_1, \
    &P_FOREIGNTYPES_EVENTSET_Init_TRANS_1, \
    &P_FOREIGNTYPES_EVENTSET_Init_DOS_1, \
    NULL, \
    P_FOREIGNTYPES_DOS_1, \
    &_P_NO_OP, \
    &_P_NO_OP, \
}

PRT_STATEDECL P_FOREIGNTYPES_ForwardingMachine_STATES[] = { P_FOREIGNTYPES_STATE_ForwardingMachine_Init };

#line 30 "foreignstringtype.p"
PRT_VALUE* P_FOREIGNTYPES_Anon_IMPL_2(PRT_MACHINEINST* context, PRT_VALUE*** argRefs)
{
#line 30 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_VAR_payload_1 = argRefs[0];
    PRT_VALUE* PTMP_tmp0_2 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_m);
    PRT_VALUE* PTMP_tmp1_2 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_m);
    PRT_VALUE* PTMP_tmp2_1 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_e);
    PRT_VALUE* PTMP_tmp3_1 = PrtMkDefaultValue(&P_FOREIGNTYPES_GEND_TYPE_a);
    PRT_VALUE* _P_GEN_funval = NULL;
    PRT_VALUE** _P_GEN_funargs[32];
    PRT_MACHINEINST_PRIV* p_this = (PRT_MACHINEINST_PRIV*)context;
    PRT_VALUE* _P_GEN_retval = NULL;
    
    PRT_VALUE _P_GEN_null = { PRT_VALUE_KIND_NULL, { .ev = PRT_SPECIAL_EVENT_NULL } };
#line 30 "foreignstringtype.p"
#line 31 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_11 = &(PTMP_tmp0_2);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_11);
    *P_FOREIGNTYPES_LVALUE_11 = PrtTupleGet(*P_FOREIGNTYPES_VAR_payload_1, 0);
    
#line 31 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_12 = &(PTMP_tmp1_2);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_12);
    *P_FOREIGNTYPES_LVALUE_12 = PrtCloneValue(PTMP_tmp0_2);
    
#line 31 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_13 = &(PTMP_tmp2_1);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_13);
    *P_FOREIGNTYPES_LVALUE_13 = PrtCloneValue((&P_FOREIGNTYPES_EVENT_getback.value));
    
#line 31 "foreignstringtype.p"
    PRT_VALUE** P_FOREIGNTYPES_LVALUE_14 = &(PTMP_tmp3_1);
    PrtFreeValue(*P_FOREIGNTYPES_LVALUE_14);
    *P_FOREIGNTYPES_LVALUE_14 = PrtTupleGet(*P_FOREIGNTYPES_VAR_payload_1, 1);
    
#line 31 "foreignstringtype.p"
    PrtSendInternal(context, PrtGetMachine(context->process, PTMP_tmp1_2), PTMP_tmp2_1, 1, &(PTMP_tmp3_1));
    *(&(PTMP_tmp2_1)) = NULL;
    
p_return:
    PrtFreeValue(PTMP_tmp0_2); PTMP_tmp0_2 = NULL;
    PrtFreeValue(PTMP_tmp1_2); PTMP_tmp1_2 = NULL;
    PrtFreeValue(PTMP_tmp2_1); PTMP_tmp2_1 = NULL;
    PrtFreeValue(PTMP_tmp3_1); PTMP_tmp3_1 = NULL;
    return _P_GEN_retval;
}

PRT_FUNDECL P_FOREIGNTYPES_FUNCTION_Anon_2 =
{
    NULL,
    &P_FOREIGNTYPES_Anon_IMPL_2,
    &P_FOREIGNTYPES_GEND_TYPE_T2ma
};


PRT_FUNDECL* P_FOREIGNTYPES_ForwardingMachine_METHODS[] = { &P_FOREIGNTYPES_FUNCTION_Anon_2 };

#line 28 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_ForwardingMachine_RECV_INNER_1[] = { &P_FOREIGNTYPES_EVENT_getback, &P_FOREIGNTYPES_EVENT_sendback };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_ForwardingMachine_RECV_1 =
{
    2U,
    P_FOREIGNTYPES_ForwardingMachine_RECV_INNER_1,
    NULL
};

#line 28 "foreignstringtype.p"
PRT_EVENTDECL* P_FOREIGNTYPES_ForwardingMachine_SEND_INNER[] = { &P_FOREIGNTYPES_EVENT_getback, &P_FOREIGNTYPES_EVENT_sendback };
PRT_EVENTSETDECL P_FOREIGNTYPES_EVENTSET_ForwardingMachine_SEND =
{
    2U,
    P_FOREIGNTYPES_ForwardingMachine_SEND_INNER,
    NULL
};

#line 28 "foreignstringtype.p"
PRT_MACHINEDECL P_FOREIGNTYPES_MACHINE_ForwardingMachine = 
{
    1U,
    "ForwardingMachine",
    &P_FOREIGNTYPES_EVENTSET_ForwardingMachine_RECV_1,
    &P_FOREIGNTYPES_EVENTSET_ForwardingMachine_SEND,
    NULL,
    0U,
    1U,
    1U,
    4294967295U,
    0U,
    NULL,
    P_FOREIGNTYPES_ForwardingMachine_STATES,
    P_FOREIGNTYPES_ForwardingMachine_METHODS
};

#line 2 "foreignstringtype.p"
PRT_TYPE* P_FOREIGNTYPES_TYPEDEF_StringType = &P_FOREIGNTYPES_GEND_TYPE_StringType;
PRT_EVENTDECL* P_FOREIGNTYPES_ALL_EVENTS[] = { &_P_EVENT_NULL_STRUCT, &_P_EVENT_HALT_STRUCT, &P_FOREIGNTYPES_EVENT_sendback, &P_FOREIGNTYPES_EVENT_getback };
PRT_MACHINEDECL* P_FOREIGNTYPES_ALL_MACHINES[] = { &P_FOREIGNTYPES_MACHINE_TestMachine, &P_FOREIGNTYPES_MACHINE_ForwardingMachine };
PRT_INTERFACEDECL* P_FOREIGNTYPES_ALL_INTERFACES[] = { &P_FOREIGNTYPES_I_TestMachine, &P_FOREIGNTYPES_I_ForwardingMachine };
PRT_FUNDECL* P_FOREIGNTYPES_ALL_FUNCTIONS[] = { &P_FOREIGNTYPES_FUNCTION_GetPassword };
PRT_FOREIGNTYPEDECL* P_FOREIGNTYPES_ALL_FOREIGN_TYPES[] = { P_FOREIGNTYPES_GEND_TYPE_StringType.typeUnion.foreignType };
int P_FOREIGNTYPES_DefaultImpl_LME_0[] = { -1,1 };
int P_FOREIGNTYPES_DefaultImpl_LME_1[] = { -1,-1 };
int* P_FOREIGNTYPES_DefaultImpl_LINKMAP[] = { P_FOREIGNTYPES_DefaultImpl_LME_0, P_FOREIGNTYPES_DefaultImpl_LME_1 };
int P_FOREIGNTYPES_DefaultImpl_DEFMAP[] = { 0,1 };
PRT_PROGRAMDECL P_GEND_IMPL_DefaultImpl = {
    4U,
    2U,
    2U,
    1U,
    1U,
    P_FOREIGNTYPES_ALL_EVENTS,
    P_FOREIGNTYPES_ALL_MACHINES,
    P_FOREIGNTYPES_ALL_INTERFACES,
    P_FOREIGNTYPES_ALL_FUNCTIONS,
    P_FOREIGNTYPES_ALL_FOREIGN_TYPES,
    P_FOREIGNTYPES_DefaultImpl_LINKMAP,
    P_FOREIGNTYPES_DefaultImpl_DEFMAP
};
