#include "ServerClient.h"
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

PRT_VALUE *P_FUN_PrintTime_FOREIGN(PRT_MACHINEINST *context)
{
	time_t t;
	time(&t);
	char buff[100];
	printf("\n In client machine time is :%s", ctime(&t));
	return NULL;
}

PRT_VALUE *P_FUN_Server_GetRandomVal_FOREIGN(PRT_MACHINEINST *context)
{
	PRT_VALUE *p_tmp_ret;
	//create a tuple value
	p_tmp_ret = PrtMkDefaultValue(&P_GEND_TYPE_TUP);
	PrtTupleSet(p_tmp_ret, 0, PrtMkIntValue(rand() % 1000));
	PrtTupleSet(p_tmp_ret, 1, PrtMkIntValue(rand() % 1000));
	return p_tmp_ret;
}

PRT_VALUE *P_FUN_Client_UpdateTuple_FOREIGN(PRT_MACHINEINST *context, PRT_VALUE *tup)
{
	ClientExternalContext* ex;
	ex = (ClientExternalContext*)context->extContext;
	printf("In updateTuple function\n");
	printf("old value: (%d, %d)\n", ex->first, ex->second);
	//update the external context
	ex->first = PrtTupleGetNC(tup, 0)->valueUnion.nt;
	ex->second = PrtTupleGetNC(tup, 1)->valueUnion.nt;
	printf("new value: (%d, %d)\n", ex->first, ex->second);
	return NULL;
}

//empty functions
void P_DTOR_Server_IMPL(PRT_MACHINEINST *context)
{
}

void P_CTOR_Server_IMPL(PRT_MACHINEINST *context, PRT_VALUE *value)
{
}