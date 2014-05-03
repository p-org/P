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
	PRT_TYPE anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_TYPE anyPairType = (PRT_TYPE)PrtMkTupType(2);
	
	PrtSetFieldType(anyPairType, 0, anyType);
	PrtSetFieldType(anyPairType, 1, anyType);

	PRT_VALUE oneVal = (PRT_VALUE)PrtMkIntValue(1);
	PRT_VALUE boolVal = (PRT_VALUE)PrtMkBoolValue(PRT_TRUE);
	PRT_VALUE anyPair = PrtMkDefaultValue(anyPairType);
	
	PrtCmdPrintValueAndType(anyPair);
	printf_s("\n");

	PrtTupleSet((PRT_TUPVALUE *)anyPair, 0, oneVal);

	PrtCmdPrintValueAndType(anyPair);
	printf_s("\n");

	PrtTupleSet((PRT_TUPVALUE *)anyPair, 1, boolVal);

	PrtCmdPrintValueAndType(anyPair);
	printf_s("\n");

	PrtCmdPrintValueAndType((PRT_VALUE)PrtTupleGet((PRT_TUPVALUE *)anyPair, 0));
	printf_s("\n");

	PrtCmdPrintValueAndType((PRT_VALUE)PrtTupleGet((PRT_TUPVALUE *)anyPair, 1));
	printf_s("\n");
}

void NamedTupleTest()
{
	PRT_TYPE anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_NMDTUPTYPE *anyPairType = PrtMkNmdTupType(2);

	PrtSetFieldName(anyPairType, 0, "foo");
	PrtSetFieldType((PRT_TYPE)anyPairType, 0, anyType);
	PrtSetFieldName(anyPairType, 1, "bar");
	PrtSetFieldType((PRT_TYPE)anyPairType, 1, anyType);

	PRT_VALUE oneVal = (PRT_VALUE)PrtMkIntValue(1);
	PRT_VALUE boolVal = (PRT_VALUE)PrtMkBoolValue(PRT_TRUE);
	PRT_VALUE anyPair = PrtMkDefaultValue((PRT_TYPE)anyPairType);

	PrtCmdPrintValueAndType(anyPair);
	printf_s("\n");

	PrtNmdTupleSet((PRT_TUPVALUE *)anyPair, "foo", oneVal);

	PrtCmdPrintValueAndType(anyPair);
	printf_s("\n");

	PrtNmdTupleSet((PRT_TUPVALUE *)anyPair, "bar", boolVal);

	PrtCmdPrintValueAndType(anyPair);
	printf_s("\n");

	PrtCmdPrintValueAndType((PRT_VALUE)PrtNmdTupleGet((PRT_TUPVALUE *)anyPair, "foo"));
	printf_s("\n");

	PrtCmdPrintValueAndType((PRT_VALUE)PrtNmdTupleGet((PRT_TUPVALUE *)anyPair, "bar"));
	printf_s("\n");
}

void SeqAppendTest()
{
	PRT_INT32 i;
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_SEQTYPE *iseqType = PrtMkSeqType(intType);
	PRT_SEQVALUE *seq = (PRT_SEQVALUE *)PrtMkDefaultValue((PRT_TYPE)iseqType);
	
	PrtCmdPrintValueAndType((PRT_VALUE)seq);
	printf_s("\n");

	for (i = 0; i <= 10; ++i)
	{
		PrtSeqInsert(seq, seq->size, (PRT_VALUE)PrtMkIntValue(i));
	}

	PrtCmdPrintValueAndType((PRT_VALUE)seq);
	printf_s("\n");

	for (i = 10; i >= 0; --i)
	{
		PrtSeqInsert(seq, seq->size, (PRT_VALUE)PrtMkIntValue(i));
	}

	PrtCmdPrintValueAndType((PRT_VALUE)seq);
	printf_s("\n");
}

void SeqPrependTest()
{
	PRT_INT32 i;
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_SEQTYPE *iseqType = PrtMkSeqType(intType);
	PRT_SEQVALUE *seq = (PRT_SEQVALUE *)PrtMkDefaultValue((PRT_TYPE)iseqType);

	PrtCmdPrintValueAndType((PRT_VALUE)seq);
	printf_s("\n");

	for (i = 0; i <= 10; ++i)
	{
		PrtSeqInsert(seq, 0, (PRT_VALUE)PrtMkIntValue(i));
	}

	PrtCmdPrintValueAndType((PRT_VALUE)seq);
	printf_s("\n");

	for (i = 10; i >= 0; --i)
	{
		PrtSeqInsert(seq, 0, (PRT_VALUE)PrtMkIntValue(i));
	}

	PrtCmdPrintValueAndType((PRT_VALUE)seq);
	printf_s("\n");
}

void SeqAppendRemoveTest()
{
	PRT_INT32 i;
	PRT_INT32 j;
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_SEQTYPE *iseqType = PrtMkSeqType(intType);
	PRT_SEQVALUE *seq = (PRT_SEQVALUE *)PrtMkDefaultValue((PRT_TYPE)iseqType);

	PrtCmdPrintValueAndType((PRT_VALUE)seq);
	printf_s("\n");

	for (i = 1; i < 10; ++i)
	{
		for (j = 0; j < i; ++j)
		{
			PrtSeqInsert(seq, seq->size, (PRT_VALUE)PrtMkIntValue(j));
		}

		PrtCmdPrintValueAndType((PRT_VALUE)seq);
		printf_s("\n");

		if (i % 2 == 0)
		{
			for (j = 0; j < i; ++j)
			{
				PrtSeqRemove(seq, 0);
				PrtCmdPrintValueAndType((PRT_VALUE)seq);
				printf_s("\n");
			}
		}
		else 
		{
			for (j = i - 1; j >= 0; --j)
			{
				PrtSeqRemove(seq, j);
				PrtCmdPrintValueAndType((PRT_VALUE)seq);
				printf_s("\n");

				if (j == 0)
				{
					break;
				}
			}
		}
	}
}

void SeqNestedTest()
{
	PRT_INT32 i;
	PRT_TYPE anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_SEQTYPE *aseqType = PrtMkSeqType(anyType);
	PRT_SEQVALUE *seq = (PRT_SEQVALUE *)PrtMkDefaultValue((PRT_TYPE)aseqType);

	PrtCmdPrintValueAndType((PRT_VALUE)seq);
	printf_s("\n");

	for (i = 0; i < 10; ++i)
	{
		PrtSeqInsert(seq, seq->size, (PRT_VALUE)seq);
		PrtCmdPrintValueAndType((PRT_VALUE)seq);
		printf_s("\n");
	}
}

void TupleTest2()
{
	PRT_TYPE intType = PrtMkPrimitiveType(PRT_KIND_INT);
	PRT_TYPE intPairType = (PRT_TYPE)PrtMkTupType(2);

	PrtSetFieldType(intPairType, 0, intType);
	PrtSetFieldType(intPairType, 1, intType);

	PRT_VALUE oneVal = (PRT_VALUE)PrtMkIntValue(1);
	PRT_VALUE boolVal = (PRT_VALUE)PrtMkBoolValue(PRT_TRUE);
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

void MapTest1()
{
	PRT_TYPE anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_MAPTYPE *any2anyType = PrtMkMapType(anyType, anyType);
	PRT_MAPVALUE *a2aMap = (PRT_MAPVALUE *)PrtMkDefaultValue((PRT_TYPE)any2anyType);

	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PRT_VALUE falseVal = (PRT_VALUE)PrtMkBoolValue(PRT_FALSE);
	PrtMapUpdate(a2aMap, (PRT_VALUE)falseVal, (PRT_VALUE)falseVal);

	PRT_UINT32 i;
	for (i = 0; i < 5; ++i)
	{
		PrtMapUpdate(a2aMap, (PRT_VALUE)PrtMkIntValue(i), (PRT_VALUE)PrtMkIntValue(i));
		PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
		printf_s("\n");
	}

	PrtMapUpdate(a2aMap, (PRT_VALUE)falseVal, (PRT_VALUE)PrtMkIntValue(10));
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PrtCmdPrintValueAndType((PRT_VALUE)PrtMapGetKeys(a2aMap));
	printf_s("\n");

	PrtCmdPrintValueAndType((PRT_VALUE)PrtMapGetValues(a2aMap));
	printf_s("\n");
}

void MapTest2()
{
	PRT_TYPE anyType = PrtMkPrimitiveType(PRT_KIND_ANY);
	PRT_MAPTYPE *any2anyType = PrtMkMapType(anyType, anyType);
	PRT_MAPVALUE *a2aMap = (PRT_MAPVALUE *)PrtMkDefaultValue((PRT_TYPE)any2anyType);

	PRT_VALUE zeroVal = (PRT_VALUE)PrtMkIntValue(0);
	PRT_VALUE falseVal = (PRT_VALUE)PrtMkBoolValue(PRT_FALSE);

	PrtMapUpdate(a2aMap, zeroVal, zeroVal);
	PrtMapUpdate(a2aMap, falseVal, falseVal);
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PrtMapRemove(a2aMap, zeroVal);
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PrtMapRemove(a2aMap, zeroVal);
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PrtMapRemove(a2aMap, falseVal);
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PrtMapUpdate(a2aMap, falseVal, falseVal);
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PrtMapUpdate(a2aMap, zeroVal, zeroVal);
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PrtMapRemove(a2aMap, zeroVal);
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PrtMapRemove(a2aMap, zeroVal);
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");

	PrtMapRemove(a2aMap, falseVal);
	PrtCmdPrintValueAndType((PRT_VALUE)a2aMap);
	printf_s("\n");
}

int main(int argc, char *argv[])
{
	/*
	TupleTest();
	NamedTupleTest();
	SeqAppendTest();
	SeqPrependTest();
	SeqAppendRemoveTest();
	SeqNestedTest();
	*/
	MapTest1();
	//// MapTest2();
	return 0;
}
