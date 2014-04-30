#include "PrtCmd.h"

void TypeCreateTest()
{
	//// Make the integer type
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);

	//// Make the bool type
	PRT_TYPE boolType = PrtMkPrimitiveType(PRT_KIND_BOOL);

	//// Make the sequence of booleans type
	PRT_SEQTYPE *seqBoolType = PrtMkSeqType(boolType);

	//// Make the int -> seq of bool type.
	PRT_MAPTYPE *int2seqType = PrtMkMapType(intType, (PRT_TYPE)seqBoolType);

	//// Clone the type
	PRT_TYPE clone = PrtCloneType((PRT_TYPE)int2seqType);

	//// Print it
	PrtCmdPrintType((PRT_TYPE)int2seqType);

	//// Free the type
	PrtFreeType((PRT_TYPE)int2seqType);

	//// Print the clone
	printf_s("\n");
	PrtCmdPrintType(clone);

	//// Free the clone
	PrtFreeType(clone);

	//// Free the remainder
	PrtFreeType(intType);
	PrtFreeType(boolType);
	PrtFreeType((PRT_TYPE)seqBoolType);
}

void TupleTest()
{
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE boolType = PrtMkPrimitiveType(PRT_KIND_BOOL);
	PRT_TYPE anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_TYPE anyPairType = (PRT_TYPE)PrtMkTupType(2);
	
	PrtSetFieldType(anyPairType, 0, anyType);
	PrtSetFieldType(anyPairType, 1, anyType);

	PRT_VALUE oneVal = (PRT_VALUE)PrtMkIntValue(intType, 10);
	PRT_VALUE boolVal = (PRT_VALUE)PrtMkBoolValue(boolType, PRT_TRUE);
	PRT_VALUE anyPair = PrtMkDefaultValue(anyPairType);
	
	PrtCmdPrintValueAndType(anyPair);
	printf_s("\n");

	PrtTupleSet((PRT_TUPVALUE *)anyPair, 0, oneVal);

	PrtCmdPrintValueAndType(anyPair);
	printf_s("\n");

	PrtTupleSet((PRT_TUPVALUE *)anyPair, 1, boolVal);

	PrtCmdPrintValueAndType(anyPair);
	printf_s("\n");
}

void TupleTest2()
{
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE boolType = PrtMkPrimitiveType(PRT_KIND_BOOL);
	PRT_TYPE intPairType = (PRT_TYPE)PrtMkTupType(2);

	PrtSetFieldType(intPairType, 0, intType);
	PrtSetFieldType(intPairType, 1, intType);

	PRT_VALUE oneVal = (PRT_VALUE)PrtMkIntValue(intType, 10);
	PRT_VALUE boolVal = (PRT_VALUE)PrtMkBoolValue(boolType, PRT_TRUE);
	PRT_VALUE intPair = PrtMkDefaultValue(intPairType);

	PrtCmdPrintValueAndType(intPair);
	printf_s("\n");

	PrtTupleSet((PRT_TUPVALUE *)intPair, 0, oneVal);

	PrtCmdPrintValueAndType(intPair);
	printf_s("\n");

	PrtTupleSet((PRT_TUPVALUE *)intPair, 1, boolVal);

	PrtCmdPrintValueAndType(intPair);
	printf_s("\n");
}

int main(int argc, char *argv[])
{
	TupleTest();
	//// TupleTest2();
	return 0;
}
