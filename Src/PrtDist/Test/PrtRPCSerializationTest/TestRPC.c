#include "PrtUser.h"
#include "PrtExecution.h"
#include "PrtRPCTest_c.c"
#include "PrtRPCTest_h.h"
#include "PrtDistSerializer.h"
extern void CreateRPCTestServer();

/***************************************************************************
* Test cases 
****************************************************************************/
/*
void PrtPrintValue(PRT_VALUE * value) {
	PRT_CHAR* log = NULL;
	log = PrtToStringValue(value);
	printf(log);
	printf("\n");
}

void PrtPrintType(PRT_TYPE* type) {
	PRT_CHAR *log = NULL;
	log = PrtToStringType(type);
	printf(log);
	printf("\n");
}*/

handle_t CreateRPCClient()
{
	handle_t testme_handle = NULL;
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

void TestOverRPC(PRT_VALUE* value)
{
	handle_t testHandle = CreateRPCClient();

	PRT_VALUE *serialized = PrtDistSerializeValue(value);
	printf("Before Serialization : \n");
	PrtPrintValue(value);
	printf("\n");

	RpcTryExcept
	{
		c_PrtRPCTestSend(testHandle, serialized);
	}
	RpcExcept(1)
	{
		unsigned long ulCode;
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception in SendValue 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept
  
}

void TupleTest()
{
	PRT_TYPE* anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_TYPE* anyPairType = PrtMkTupType(2);

	PrtSetFieldType(anyPairType, 0, anyType);
	PrtSetFieldType(anyPairType, 1, anyType);

	PRT_VALUE *oneVal = PrtMkIntValue(1);
	PRT_VALUE *boolVal = PrtMkBoolValue(PRT_TRUE);
	PRT_VALUE *anyPair = PrtMkDefaultValue(anyPairType);

	PrtPrintValue(anyPair);
	printf_s("\n");

	PrtTupleSet(anyPair, 0, oneVal);

	PrtPrintValue(anyPair);
	printf_s("\n");

	PrtTupleSet(anyPair, 1, boolVal);
	printf_s("\n");

	TestOverRPC(anyPair);
}

void NamedTupleTest()
{
	PRT_TYPE* anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_TYPE* anyPairType = PrtMkNmdTupType(2);

	PrtSetFieldName(anyPairType, 0, "foo");
	PrtSetFieldType(anyPairType, 0, anyType);
	PrtSetFieldName(anyPairType, 1, "bar");
	PrtSetFieldType(anyPairType, 1, anyType);

	PRT_VALUE *oneVal = PrtMkIntValue(1);
	PRT_VALUE *boolVal = PrtMkBoolValue(PRT_TRUE);
	PRT_VALUE *anyPair = PrtMkDefaultValue(anyPairType);

	PrtPrintValue(anyPair);
	printf_s("\n");

	PrtTupleSet(anyPair, 0, oneVal);

	PrtPrintValue(anyPair);
	printf_s("\n");

	PrtTupleSet(anyPair, 1, boolVal);

	PrtPrintValue(anyPair);
	printf_s("\n");

	PrtPrintValue(PrtTupleGet(anyPair, 0));
	printf_s("\n");

	PrtPrintValue(PrtTupleGet(anyPair, 1));
	printf_s("\n");

	TestOverRPC(anyPair);
}


void TestPrimitiveType()
{
	handle_t testHandle = CreateRPCClient();

	/// Make the integer type
	PRT_TYPE* intType = PrtMkPrimitiveType(PRT_KIND_INT);

	PRT_VALUE *intVal = PrtMkDefaultValue(intType);
	PrtPrimSetInt(intVal, 100);
	printf("Value Sent is : \n");
	PrtPrintValue(intVal);
	printf("\n");
	TestOverRPC(intVal);

}

void SeqNestedTest()
{
	PRT_INT32 i;
	PRT_TYPE* anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_TYPE* aseqType = PrtMkSeqType(anyType);
	PRT_VALUE *seq = PrtMkDefaultValue(aseqType);

	for (i = 0; i < 10; ++i)
	{
		PrtSeqInsert(seq, PrtMkIntValue(seq->valueUnion.seq->size), seq);
	}
	PrtPrintValue(seq);
	printf_s("\n");

	TestOverRPC(seq);

}

void MapTest1()
{
	PRT_TYPE* anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_TYPE* any2anyType = PrtMkMapType(anyType, anyType);
	PRT_VALUE *a2aMap = PrtMkDefaultValue(any2anyType);

	PrtPrintValue(a2aMap);
	printf_s("\n");

	PRT_VALUE *falseVal = PrtMkBoolValue(PRT_FALSE);
	PrtMapUpdate(a2aMap, falseVal, falseVal);

	PRT_UINT32 i;
	for (i = 0; i < 5; ++i)
	{
		PrtMapUpdate(a2aMap, PrtMkIntValue(i), PrtMkIntValue(i));
		PrtPrintValue(a2aMap);
		printf_s("\n");
	}

	PrtMapUpdate(a2aMap, falseVal, PrtMkIntValue(10));
	PrtPrintValue(a2aMap);
	printf_s("\n");
	handle_t testHandle = CreateRPCClient();
	TestOverRPC(a2aMap);

}

void SeqAppendTest()
{
	PRT_INT32 i;
	PRT_TYPE* intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE* iseqType = PrtMkSeqType(intType);
	PRT_VALUE *seq = PrtMkDefaultValue(iseqType);

	for (i = 0; i <= 10; ++i)
	{
		PrtSeqInsert(seq, PrtMkIntValue(seq->valueUnion.seq->size), PrtMkIntValue(i));
	}

	for (i = 10; i >= 0; --i)
	{
		PrtSeqInsert(seq, PrtMkIntValue(seq->valueUnion.seq->size), PrtMkIntValue(i));
	}

	PrtPrintValue(seq);
	printf_s("\n");
	TestOverRPC(seq);

}

void SeqPrependTest()
{
	PRT_INT32 i;
	PRT_TYPE* intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE* iseqType = PrtMkSeqType(intType);
	PRT_VALUE *seq = (PRT_VALUE *)PrtMkDefaultValue(iseqType);

	PrtPrintValue(seq);
	printf_s("\n");

	for (i = 0; i <= 10; ++i)
	{
		PrtSeqInsert(seq, PrtMkIntValue(0), PrtMkIntValue(i));
	}

	PrtPrintValue(seq);
	printf_s("\n");

	for (i = 10; i >= 0; --i)
	{
		PrtSeqInsert(seq, PrtMkIntValue(0), PrtMkIntValue(i));
	}

	PrtPrintValue(seq);
	printf_s("\n");

	TestOverRPC(seq);
}

void MapTest2()
{
	PRT_TYPE* anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_TYPE* any2anyType = PrtMkMapType(anyType, anyType);
	PRT_VALUE *a2aMap = PrtMkDefaultValue(any2anyType);

	PRT_VALUE *zeroVal = PrtMkIntValue(0);
	PRT_VALUE *falseVal = PrtMkBoolValue(PRT_FALSE);

	PrtMapUpdate(a2aMap, zeroVal, zeroVal);
	PrtMapUpdate(a2aMap, falseVal, falseVal);
	PrtPrintValue(a2aMap);

	TestOverRPC(a2aMap);
}

void BinaryBoolFunTest()
{
	PRT_TYPE* boolType = PrtMkPrimitiveType(PRT_KIND_BOOL);
	PRT_TYPE* intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE* boolTupType = PrtMkTupType(2);
	PrtSetFieldType(boolTupType, 0, boolType);
	PrtSetFieldType(boolTupType, 1, boolType);
	PRT_TYPE* binFunType = PrtMkMapType(boolTupType, boolType);
	PRT_TYPE* popFunType = PrtMkMapType(binFunType, intType);

	printf_s("Bool fun type = ");
	PrtPrintType(binFunType);
	printf_s("\n");

	printf_s("Population fun type = ");
	PrtPrintType(popFunType);
	printf_s("\n");

	PRT_UINT32 funImg;
	PRT_UINT32 funRng;
	PRT_VALUE *boolVal;
	PRT_VALUE *popCntVal;
	PRT_VALUE *boolTup;
	PRT_VALUE *popFun = PrtMkDefaultValue(popFunType);
	for (funImg = 0; funImg < 16; ++funImg)
	{
		PRT_VALUE *fun = PrtMkDefaultValue(binFunType);
		for (funRng = 0; funRng < 4; ++funRng)
		{
			//// Set (funRng_1, funRng_0) -> img_0
			boolTup = PrtMkDefaultValue(boolTupType);

			boolVal = PrtMkBoolValue((funRng & 0x00000002) == 0 ? PRT_FALSE : PRT_TRUE);
			PrtTupleSet(boolTup, 0, boolVal);
			PrtFreeValue(boolVal);

			boolVal = PrtMkBoolValue((funRng & 0x00000001) == 0 ? PRT_FALSE : PRT_TRUE);
			PrtTupleSet(boolTup, 1, boolVal);
			PrtFreeValue(boolVal);

			boolVal = PrtMkBoolValue(((funImg >> funRng) & 0x00000001) == 0 ? PRT_FALSE : PRT_TRUE);
			PrtMapUpdate(fun, boolTup, boolVal);
			PrtFreeValue(boolVal);
			PrtFreeValue(boolTup);
		}

		popCntVal = PrtMkIntValue(
			(0x00000001 & funImg) +
			(0x00000001 & (funImg >> 1)) +
			(0x00000001 & (funImg >> 2)) +
			(0x00000001 & (funImg >> 3)));

		PrtPrintValue(fun);
		printf_s("\n");

		PrtMapUpdate(popFun, fun, popCntVal);

		PrtFreeValue(popCntVal);
		PrtFreeValue(fun);
	}

	PrtPrintValue(popFun);
	printf_s("\n");

	//// Build the population function in reverse.
	//// Get the keys of the population function.
	PRT_UINT32 i;
	PRT_VALUE *popKeys = PrtMapGetKeys(popFun);
	PRT_VALUE *revPopFun = PrtMkDefaultValue(popFunType);
	for (i = 1; i <= 16; ++i)
	{
		popCntVal = PrtMapGet(popFun, popKeys->valueUnion.seq->values[16 - i]);
		PrtMapUpdate(revPopFun, popKeys->valueUnion.seq->values[16 - i], popCntVal);
		PrtFreeValue(popCntVal);
	}

	PrtFreeValue(popKeys);
	PrtPrintValue(revPopFun);
	printf_s("\n");

	PrtAssert(PrtGetHashCodeValue(popFun) == PrtGetHashCodeValue(revPopFun), "Equivalent maps should have equivalent hash codes");

	printf("-----------------------------------------------\n\n");
	TestOverRPC(popFun);

	PrtFreeValue(revPopFun);
	PrtFreeValue(popFun);
	PrtFreeType(intType);
	PrtFreeType(boolType);
	PrtFreeType(boolTupType);
	PrtFreeType(binFunType);
	PrtFreeType(popFunType);
}

int main()
{
	//Create RPC test server
	CreateRPCTestServer();
	//Test Cases
	//TestPrimitiveType();
	//MapTest1();
	//MapTest2();
	//SeqAppendTest();
	SeqNestedTest();
	//TupleTest();
	//NamedTupleTest();
	//SeqPrependTest();
	//BinaryBoolFunTest();
	//wait
	getchar();

	//close RPC server
	RpcMgmtStopServerListening(NULL);
}