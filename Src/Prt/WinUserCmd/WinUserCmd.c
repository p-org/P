#include "PrtWinUser.h"




void PrtPrintType(PRT_TYPE *type)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;
	
	PrtWinUserPrintType(type, &buffer, &bufferSize, &nChars);
	for (PRT_UINT32 i = 0; i < nChars; ++i)
	{
		putchar(buffer[i]);
	}

	if (bufferSize > 0)
	{
		PrtFree(buffer);
	}
}

void PrtPrintValue(PRT_VALUE *value)
{
	char *buffer = NULL;
	PRT_UINT32 bufferSize = 0;
	PRT_UINT32 nChars = 0;

	PrtWinUserPrintValue(value, &buffer, &bufferSize, &nChars);
	printf_s("%s", buffer);
	

	if (bufferSize > 0)
	{
		PrtFree(buffer);
	}
}

void BinaryBoolFunTest()
{
	PRT_TYPE *boolType = PrtMkPrimitiveType(PRT_KIND_BOOL);
	PRT_TYPE *intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE *boolTupType = PrtMkTupType(2);
	PrtSetFieldType(boolTupType, 0, boolType);
	PrtSetFieldType(boolTupType, 1, boolType);
	PRT_TYPE *binFunType = PrtMkMapType(boolTupType, boolType);
	PRT_TYPE *popFunType = PrtMkMapType(binFunType, intType);
	
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

	printf_s("All binary boolean functions\n");
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
	
	printf_s("Map from a binary boolean function to number of true values in image\n");
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
	printf_s("Reverse of population function \n");
	PrtPrintValue(revPopFun);
	printf_s("\n");

	PrtAssert(PrtGetHashCodeValue(popFun) == PrtGetHashCodeValue(revPopFun), "Equivalent maps should have equivalent hash codes");
	PrtAssert(PrtIsEqualValue(popFun, revPopFun), "Equivalent maps should be equal");

	PrtFreeValue(revPopFun);
	PrtFreeValue(popFun);
	PrtFreeType(intType);
	PrtFreeType(boolType);
	PrtFreeType(boolTupType);
	PrtFreeType(binFunType);
	PrtFreeType(popFunType);
}

#define P_SEQ
#define P_EXPR1(x1) P_SEQ(p_expr1 = (x1), p_expr1)
#define P_EXPR2(x2, x1) P_SEQ(p_expr1 = (x1), p_expr2 = (x2), PrtFreeValue(p_expr1), p_expr2)

#define P_TUP2(t, x2, x1) P_SEQ(p_expr1 = PrtMkDefaultValue(t), PrtTupleSet(p_expr1, 0, x1), PrtTupleSet(p_expr1, 1, x2), p_expr1)

int main(int argc, char *argv[])
{
	/*
	PRT_DBG_START_MEM_BALANCED_REGION
	{
		BinaryBoolFunTest();
	}
	PRT_DBG_END_MEM_BALANCED_REGION
	*/

	PRT_VALUE * p_expr1 = NULL, *p_expr2 = NULL;
	PRT_DBG_START_MEM_BALANCED_REGION
	{
		PRT_VALUE * result = P_EXPR2(
		PrtMkIntValue(PrtPrimGetInt(p_expr1) + PrtPrimGetInt(p_expr1)),
		PrtMkIntValue(1));

		PrtPrintValue(result);
		PrtFreeValue(result);
	}
	PRT_DBG_END_MEM_BALANCED_REGION
	return 0;
}
