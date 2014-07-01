
#include "../PrtCmd/PrtCmdPrinting.h"
#include"PrtTypes.h"
#include"PrtValues.h"
#include<pthread.h>
#include"PrtTypes.h"
#include"PrtDistributed_TypesAndValues_s.c"

PRT_VALUE *ReceivedValue;

/** Array of prime hash table capacities. */
const PRT_UINT32 PrtHashtableCapacities1[] =
{
	3, 13, 31, 61, 127,
	251, 509, 1021, 2039, 4093,
	8191, 16381, 32749, 65521, 131071,
	262139, 524287, 1048573, 2097143, 4194301,
	8388593, 16777213, 33554393, 67108859, 134217689,
	268435399, 536870909, 1073741789, 2147483647, 4294967291
};

PRT_TYPE DeSerializeType(_In_ PRT_TYPE type)
{
	PRT_TYPE_KIND kind = type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
	case PRT_KIND_BOOL:
	case PRT_KIND_EVENT:
	case PRT_KIND_MACHINE:
	case PRT_KIND_INT:
	case PRT_KIND_MODEL:
	case PRT_KIND_NULL:
		return PrtMkPrimitiveType(kind);
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		PRT_FORGNTYPE *ftype = type.typeUnion.forgn;
		return PrtMkForgnType(ftype->typeTag, ftype->cloner, ftype->freer, ftype->hasher, ftype->eqTester);
#endif
	}
	case PRT_KIND_MAP:
	{
		PRT_MAPTYPE *mtype = type.typeUnion.map;
		return PrtMkMapType(DeSerializeType(mtype->domType), DeSerializeType(mtype->codType));
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i = 0;
		PRT_NMDTUPTYPE *ntype = type.typeUnion.nmTuple;
		PRT_TYPE clone = PrtMkNmdTupType(ntype->arity);

		PRT_STRING_NODE *currN = ntype->fieldNamesSerialized;
		PRT_TYPE_NODE * currT = ntype->fieldTypesSerialized;

		while (currN != NULL)
		{
			PrtSetFieldName(clone, i, currN->name);
			PrtSetFieldType(clone, i, DeSerializeType(currT->type));
			i = i + 1;
			currN = currN->nextNode;
			currT = currT->nextNode;
		}
		clone.typeUnion.nmTuple->fieldNamesSerialized = NULL;
		clone.typeUnion.nmTuple->fieldTypesSerialized = NULL;

		return clone;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = type.typeUnion.seq;
		return PrtMkSeqType(DeSerializeType(stype->innerType));
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i= 0;
		PRT_TUPTYPE *ttype = type.typeUnion.tuple;
		PRT_TYPE clone = PrtMkTupType(ttype->arity);
		PRT_TYPE_NODE * currT = ttype->fieldTypesSerialized;

		while (currT != NULL)
		{
			PrtSetFieldType(clone, i, DeSerializeType(currT->type));
			i = i + 1;
			currT = currT->nextNode;
		}

		return clone;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		PRT_TYPE rettype;
		rettype.typeKind = PRT_TYPE_KIND_CANARY;
		return rettype;
	}

}

PRT_VALUE* DeSerializeValue(_In_ PRT_VALUE* value)
{
	PRT_TYPE_KIND kind = value->type.typeKind;
	switch (kind)
	{
	case PRT_KIND_ANY:
		PRT_DBG_ASSERT(PRT_FALSE, "Value must have a more concrete type");
		return NULL;
	case PRT_KIND_BOOL:

	case PRT_KIND_EVENT:

	case PRT_KIND_MACHINE:

	case PRT_KIND_INT:

	case PRT_KIND_MODEL:
		return PrtCloneValue(value);
	case PRT_KIND_FORGN:
	{
#if !defined(IGNORE_FRG)
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		PRT_FORGNVALUE *fVal = value->valueUnion.frgn;
		PRT_FORGNTYPE *fType = value->type.typeUnion.forgn;
		PRT_FORGNVALUE *cVal = (PRT_FORGNVALUE *)PrtMalloc(sizeof(PRT_FORGNVALUE));
		retVal->type = PrtCloneType(value->type);
		retVal->discriminator = retVal->type.typeKind;
		cVal->value = fType->cloner(fType->typeTag, fVal->value);
		retVal->valueUnion.frgn = cVal;
		return retVal;
#endif
	}
	case PRT_KIND_MAP:
	{
		PRT_TYPE tt = DeSerializeType(value->type);
		PRT_VALUE *retVal = PrtMkDefaultValue(tt);
		PRT_MAPVALUE *mVal = value->valueUnion.map;
		PRT_MAPVALUE *cVal = retVal->valueUnion.map;
		if (mVal->capNum > 0)
		{
			//// Eagerly allocate capacity in the clone to avoid intermediate rehashings.
			PrtFree(cVal->buckets);
			cVal->buckets = (PRT_MAPNODE **)PrtCalloc(PrtHashtableCapacities1[mVal->capNum], sizeof(PRT_MAPNODE *));
			cVal->capNum = mVal->capNum;

		}
		PRT_MAPNODE *next = mVal->first;
		while (next != NULL)
		{
			PrtMapUpdate(retVal, DeSerializeValue(next->key), DeSerializeValue(next->value));
			next = next->insertNext;
		}

		return retVal;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_TYPE tt = DeSerializeType(value->type);
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(tt);
		retVal->discriminator = retVal->type.typeKind;
		
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.nmTuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));
		
		PRT_VALUE_NODE *curr = tVal->valuesSerialized;
		PRT_UINT32 i = 0;
		while (curr != NULL)
		{
			cVal->values[i] = DeSerializeValue(curr->value);
			curr = curr->nextNode;
			i = i + 1;
		}
		cVal->valuesSerialized = NULL;

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_TYPE tt = DeSerializeType(value->type);
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(tt);
		retVal->discriminator = retVal->type.typeKind;
		
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.tuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));

		PRT_VALUE_NODE *curr = tVal->valuesSerialized;
		PRT_UINT32 i = 0;
		while (curr != NULL)
		{
			cVal->values[i] = DeSerializeValue(curr->value);
			curr = curr->nextNode;
			i = i + 1;
		}
		cVal->valuesSerialized = NULL;
		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_SEQ:
	{
		PRT_TYPE tt = DeSerializeType(value->type);
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		retVal->type = PrtCloneType(tt);
		retVal->discriminator = retVal->type.typeKind;
		PRT_SEQVALUE *sVal = value->valueUnion.seq;
		PRT_SEQVALUE *cVal = (PRT_SEQVALUE *)PrtMalloc(sizeof(PRT_SEQVALUE));
		cVal->capacity = sVal->capacity;
		cVal->size = sVal->size;
		if (sVal->capacity == 0)
		{
			cVal->values = NULL;
		}
		else
		{
			PRT_UINT32 i = 0;
			cVal->values = (PRT_VALUE **)PrtCalloc(sVal->capacity, sizeof(PRT_VALUE*));
			PRT_VALUE_NODE *curr = sVal->valuesSerialized;
			while (curr != NULL)
			{
				cVal->values[i] = DeSerializeValue(curr->value);
				curr = curr->nextNode;
				i = i + 1;
			}
			
		}
		cVal->valuesSerialized = NULL;
		retVal->valueUnion.seq = cVal;
		return retVal;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
}


/*
int main(int argc, char *argv[])
{
	PRT_DBG_START_MEM_BALANCED_REGION
	{
		BinaryBoolFunTest();
	}
	PRT_DBG_END_MEM_BALANCED_REGION

	/*
	TupleTest();
	NamedTupleTest();
	SeqAppendTest();
	SeqPrependTest();
	SeqAppendRemoveTest();
	SeqNestedTest();
	MapTest1();
	MapTest2();
	

	return 0;
}
*/
void* __cdecl WaitThreadForRPCListen(void* i)
{
	printf("Thread - WaitThreadForRPCListen listening ...\n");
	RPC_STATUS status;
	status = RpcMgmtWaitServerListen();
	if (status)
		exit(status);
	printf("In WaitThreadForRPCListen - Wait Server Listen Stopped");
	pthread_exit(NULL);
	return NULL;
}


void s_SendValue1(
	handle_t handleM,
	PRT_VALUE* value
	)
{
	printf("\n");
	PRT_VALUE* x = DeSerializeValue(value);
	PrtCmdPrintValue(x);
	printf("\n");
	ReceivedValue = PrtCloneValue(x);
	printf("In SendValue1 Value Received :");
	PrtCmdPrintValue(ReceivedValue);
	printf("\n");
}

void s_SendValue2(
	handle_t handleM,
	PRT_VALUE* value
	)
{
	//ReceivedValue = PrtCloneValue((PRT_VALUE)value);
	printf("\n");
	PRT_VALUE* curr = DeSerializeValue(value);
	PrtCmdPrintValue(curr);
	PrtAssert(PrtIsEqualValue(curr, ReceivedValue), "Values Received are not Equal");
	printf("\nReceived the same Value\n");
}


void CreateRPCTestServer()
{
	printf("Creating RPC server for PService ....\n");

	RPC_STATUS status;

	status = RpcServerUseProtseqEp(
		(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP protocol.
		RPC_C_PROTSEQ_MAX_REQS_DEFAULT, // Backlog queue length for TCP/IP.
		(unsigned char*)"4748", // TCP/IP port to use.
		NULL);

	if (status)
	{
		printf(stderr, "Runtime reported exception in RpcServerUseProtseqEp"); 
		exit(status);
	}

	status = RpcServerRegisterIf2(
		s_PrtDistributed_v1_0_s_ifspec, // Interface to register.
		NULL, // Use the MIDL generated entry-point vector.
		NULL, // Use the MIDL generated entry-point vector.
		RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, // Forces use of security callback.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Use default number of concurrent calls.
		(unsigned)-1, // Infinite max size of incoming data blocks.
		NULL); // Naive security callback.

	if (status)
	{
		printf(stderr, "Runtime reported exception in RpcServerRegisterIf2");
		exit(status);
	}

	// Start to listen for remote procedure calls for all registered interfaces.
	// This call will not return until RpcMgmtStopServerListening is called.
	status = RpcServerListen(
		1, // Recommended minimum number of threads.
		RPC_C_LISTEN_MAX_CALLS_DEFAULT, // Recommended maximum number of threads.
		1);

	pthread_t workerThread;
	int s = pthread_create(&workerThread, NULL, WaitThreadForRPCListen, NULL);
	if (s)
	{
		printf(stderr, "Runtime reported exception in pthread_create");
		exit(status);
	}

}

void* __RPC_API
MIDL_user_allocate(size_t size)
{
	unsigned char* ptr;
	ptr = (unsigned char*)malloc(size);
	return (void*)ptr;
}

void __RPC_API
MIDL_user_free(void* object)

{
	free(object);
}

