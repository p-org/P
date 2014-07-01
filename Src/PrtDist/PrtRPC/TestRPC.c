#include "../PrtCmd/PrtCmdPrinting.h"
#include"PrtTypes.h"
#include"PrtValues.h"
#include"PrtTypes.h"
#include"PrtDistributed_TypesAndValues_c.c"
#include"PrtDistributed_TypesAndValues_h.h"
extern const PRT_UINT32 PrtHashtableCapacities1[];
extern void CreateRPCTestServer();

void InsertValueNode(PRT_VALUE_NODE** head, PRT_VALUE *value){
	PRT_VALUE_NODE* curr = *head;
	if (curr == NULL)
	{
		PRT_VALUE_NODE* curr = (PRT_VALUE_NODE*)PrtMalloc(sizeof(PRT_VALUE_NODE));
		curr->value = value;
		curr->nextNode = NULL;
		*head = curr;
	}
	else
	{
		while (curr->nextNode != NULL)
		{
			curr = curr->nextNode;
		}
		curr->nextNode = (PRT_VALUE_NODE*)PrtMalloc(sizeof(PRT_VALUE_NODE));
		curr->nextNode->value = value;
		curr->nextNode->nextNode = NULL;
	}

}

void InsertStringNode(PRT_STRING_NODE** head, PRT_STRING value){
	PRT_STRING_NODE* curr = *head;
	if (curr == NULL)
	{
		PRT_STRING_NODE* curr = (PRT_STRING_NODE*)PrtMalloc(sizeof(PRT_STRING_NODE));
		curr->name = value;
		curr->nextNode = NULL;
		*head = curr;
	}
	else
	{
		while (curr->nextNode != NULL)
		{
			curr = curr->nextNode;
		}
		curr->nextNode = (PRT_STRING_NODE*)PrtMalloc(sizeof(PRT_STRING_NODE));
		curr->nextNode->name = value;
		curr->nextNode->nextNode = NULL;
	}

}

void InsertTypeNode(PRT_TYPE_NODE** head, PRT_TYPE value){
	PRT_TYPE_NODE* curr = *head;
	if (curr == NULL)
	{
		PRT_TYPE_NODE* curr = (PRT_TYPE_NODE*)PrtMalloc(sizeof(PRT_TYPE_NODE));
		curr->type = value;
		curr->nextNode = NULL;
		*head = curr;
	}
	else
	{
		while (curr->nextNode != NULL)
		{
			curr = curr->nextNode;
		}
		curr->nextNode = (PRT_TYPE_NODE*)PrtMalloc(sizeof(PRT_TYPE_NODE));
		curr->nextNode->type = value;
		curr->nextNode->nextNode = NULL;
	}
}

PRT_TYPE SerializeType(PRT_TYPE type)
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
		return PrtMkMapType(mtype->domType, mtype->codType);
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_UINT32 i;
		PRT_NMDTUPTYPE *ntype = type.typeUnion.nmTuple;
		PRT_TYPE clone = PrtMkNmdTupType(ntype->arity);
		for (i = 0; i < ntype->arity; ++i)
		{
			InsertStringNode(&clone.typeUnion.nmTuple->fieldNamesSerialized, ntype->fieldNames[i]);
			InsertTypeNode(&clone.typeUnion.nmTuple->fieldTypesSerialized, ntype->fieldTypes[i]);
		}


		return clone;
	}
	case PRT_KIND_SEQ:
	{
		PRT_SEQTYPE *stype = type.typeUnion.seq;
		return PrtMkSeqType(stype->innerType);
	}
	case PRT_KIND_TUPLE:
	{
		PRT_UINT32 i;
		PRT_TUPTYPE *ttype = type.typeUnion.tuple;
		PRT_TYPE clone = PrtMkTupType(ttype->arity);
		for (i = 0; i < ttype->arity; ++i)
		{
			InsertTypeNode(&clone.typeUnion.nmTuple->fieldTypesSerialized, ttype->fieldTypes[i]);
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
PRT_VALUE* SerializeValue(_In_ PRT_VALUE* value)
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
		PRT_VALUE *retVal = PrtMkDefaultValue(value->type);
		retVal->type = SerializeType(value->type);
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
			PrtMapUpdate(retVal, SerializeValue(next->key), SerializeValue(next->value));
			next = next->insertNext;
		}

		return retVal;
	}
	case PRT_KIND_NMDTUP:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		retVal->type = SerializeType(value->type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.nmTuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		
		cVal->valuesSerialized = NULL;
		cVal->values = NULL;
		for (i = 0; i < arity; ++i)
		{
			InsertValueNode(&cVal->valuesSerialized, SerializeValue(tVal->values[i]));
		}

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_TUPLE:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		retVal->type = SerializeType(value->type);
		retVal->discriminator = retVal->type.typeKind;
		PRT_UINT32 i;
		PRT_TUPVALUE *tVal = value->valueUnion.tuple;
		PRT_UINT32 arity = value->type.typeUnion.tuple->arity;
		PRT_TUPVALUE *cVal = (PRT_TUPVALUE *)PrtMalloc(sizeof(PRT_TUPVALUE));
		cVal->values = (PRT_VALUE **)PrtCalloc(arity, sizeof(PRT_VALUE*));
		cVal->valuesSerialized = NULL;
		cVal->values = NULL;
		for (i = 0; i < arity; ++i)
		{
			InsertValueNode(&cVal->valuesSerialized, SerializeValue(tVal->values[i]));
		}

		retVal->valueUnion.tuple = cVal;
		return retVal;
	}
	case PRT_KIND_SEQ:
	{
		PRT_VALUE *retVal = (PRT_VALUE *)PrtMalloc(sizeof(PRT_VALUE));
		retVal->type = SerializeType(value->type);
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
			PRT_UINT32 i;
			cVal->valuesSerialized = NULL;
			cVal->values = NULL;
			for (i = 0; i < sVal->size; ++i)
			{
				InsertValueNode(&cVal->valuesSerialized, SerializeValue(sVal->values[i]));
			}
		}
		retVal->valueUnion.seq = cVal;
		return retVal;
	}
	default:
		PrtAssert(PRT_FALSE, "Invalid type");
		return NULL;
	}
}

handle_t CreateRPCClient()
{
	//create RPC client
	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;

	// Creates a string binding handle.
	// This function is nothing more than a printf.
	// Connection is not done here.
	status = RpcStringBindingCompose(
		NULL, // UUID to bind to.
		(unsigned char*)"ncacn_ip_tcp", // Use TCP/IP
		// protocol.
		(unsigned char*)"localhost", // TCP/IP network
		// address to use.
		(unsigned char*)"4748", // TCP/IP port to use.
		NULL, // Protocol dependent network options to use.
		&szStringBinding); // String binding output.

	if (status)
		exit(status);

	handle_t testme_handle = NULL;

	// Validates the format of the string binding handle and converts
	// it to a binding handle.
	// Connection is not done here either.
	status = RpcBindingFromStringBinding(
		szStringBinding, // The string binding to validate.
		&testme_handle); // Put the result in the implicit binding
	// handle defined in the IDL file.

	if (status)
		exit(status);

	return testme_handle;
}

void TestPrimitiveType()
{
	handle_t testHandle = CreateRPCClient();

	/// Make the integer type
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);

	PRT_VALUE *intVal = PrtMkDefaultValue(intType);
	PrtPrimSetInt(intVal, 100);
	printf("Value Sent is : \n");
	PrtCmdPrintValueAndType(intVal);
	printf("\n");
	RpcTryExcept
	{
		c_SendValue1(testHandle, intVal);
	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception in SendValue1 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept
	RpcTryExcept
	{
		c_SendValue2(testHandle, intVal);
	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception in SendValue1 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept

}

void SeqAppendTest()
{
	PRT_INT32 i;
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE iseqType = PrtMkSeqType(intType);
	PRT_VALUE *seq = PrtMkDefaultValue(iseqType);

	for (i = 0; i <= 10; ++i)
	{
		PrtSeqInsert(seq, seq->valueUnion.seq->size, PrtMkIntValue(i));
	}

	for (i = 10; i >= 0; --i)
	{
		PrtSeqInsert(seq, seq->valueUnion.seq->size, PrtMkIntValue(i));
	}

	PrtCmdPrintValueAndType(seq);
	printf_s("\n");

	PRT_VALUE *seq2 = SerializeValue(seq);

	handle_t testHandle = CreateRPCClient();
	RpcTryExcept
	{
		c_SendValue1(testHandle, seq2);
	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception in SendValue1 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept

	RpcTryExcept
	{
		c_SendValue2(testHandle, seq2);
	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception in SendValue1 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept
}

void MapTest2()
{
	PRT_TYPE anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_TYPE any2anyType = PrtMkMapType(anyType, anyType);
	PRT_VALUE *a2aMap = PrtMkDefaultValue(any2anyType);

	PRT_VALUE *zeroVal = PrtMkIntValue(0);
	PRT_VALUE *falseVal = PrtMkBoolValue(PRT_FALSE);

	PrtMapUpdate(a2aMap, zeroVal, zeroVal);
	PrtMapUpdate(a2aMap, falseVal, falseVal);
	PrtCmdPrintValueAndType(a2aMap);
	printf_s("Before Call to Server \n");

	handle_t testHandle = CreateRPCClient();
	RpcTryExcept
	{
		c_SendValue1(testHandle, a2aMap);
	}
		RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception in SendValue1 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept

	RpcTryExcept
	{
		c_SendValue2(testHandle, a2aMap);
	}
	RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception in SendValue1 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept
}


int main()
{
	//Create RPC test server
	CreateRPCTestServer();
	//Test Cases
	//TestPrimitiveType();
	//MapTest2();
	//SeqAppendTest();
	//wait
	getchar();

	//close RPC server
	RpcMgmtStopServerListening(NULL);
}