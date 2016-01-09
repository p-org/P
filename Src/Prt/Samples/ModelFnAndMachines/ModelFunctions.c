#include "program.h"
#include <time.h>

typedef struct ClientExternalContext {
	PRT_INT32 first;
	PRT_INT32 second;
} ClientExternalContext;

void P_DTOR_Client_IMPL(PRT_MACHINEINST *context)
{
	//free external context
	printf("Free external context\n");
	PrtFree(context->extContext);
}

void P_CTOR_Client_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value)
{
	//allocate external context
	printf("Initialize external context\n");
	context->extContext = PrtMalloc(sizeof(ClientExternalContext));
}

PRT_VALUE *P_FUN_PrintTime_IMPL(PRT_MACHINEINST *context)
{
	PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
	PRT_VALUE *p_tmp_ret = NULL;
	PRT_FUNSTACK_INFO p_tmp_frame;
	PRT_VALUE *p_tmp_params;
	p_tmp_params = NULL;
	//remm to pop frame
	PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
	time_t t;
	time(&t);
	char buff[100];
	printf("\n In client machine time is :%s", ctime(&t));
	//remm to free the frame
	PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
	return p_tmp_ret;
}

PRT_VALUE *P_FUN_Server_GetRandomVal_IMPL(PRT_MACHINEINST *context)
{
	PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
	PRT_VALUE *p_tmp_ret = NULL;
	PRT_FUNSTACK_INFO p_tmp_frame;
	PRT_VALUE *p_tmp_params;
	p_tmp_params = NULL;
	//remm to pop frame
	PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
	//create a tuple value
	p_tmp_ret = PrtMkDefaultValue(P_GEND_TYPE_TUP);
	PrtTupleSet(p_tmp_ret, 0, PrtMkIntValue(rand() % 1000));
	PrtTupleSet(p_tmp_ret, 1, PrtMkIntValue(rand() % 1000));
	//remm to free the frame
	PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
	return p_tmp_ret;
}

PRT_VALUE *P_FUN_Client_UpdateTuple_IMPL(PRT_MACHINEINST *context)
{
	PRT_MACHINEINST_PRIV *p_tmp_mach_priv = (PRT_MACHINEINST_PRIV *)context;
	PRT_VALUE *p_tmp_ret = NULL;
	PRT_FUNSTACK_INFO p_tmp_frame;
	PRT_VALUE *p_tmp_params;
	ClientExternalContext* ex;
	p_tmp_params = NULL;
	//remm to pop frame
	PrtPopFrame(p_tmp_mach_priv, &p_tmp_frame);
	ex = (ClientExternalContext*)p_tmp_mach_priv->extContext;
	printf("In updateTuple function\n");
	printf("old value: (%d, %d)\n", ex->first, ex->second);
	//update the external context
	ex->first = PrtTupleGet(p_tmp_frame.locals[0], 0)->valueUnion.nt;
	ex->second = PrtTupleGet(p_tmp_frame.locals[0], 1)->valueUnion.nt;
	printf("new value: (%d, %d)\n", ex->first, ex->second);
	//remm to free the frame
	PrtFreeLocals(p_tmp_mach_priv, &p_tmp_frame);
	return p_tmp_ret;
}

//empty functions
void P_DTOR_Server_IMPL(PRT_MACHINEINST *context)
{
}

void P_CTOR_Server_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value)
{
}