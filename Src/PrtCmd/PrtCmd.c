#include "PrtCmd.h"

int main(int argc, char *argv[])
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

	return 0;
}
