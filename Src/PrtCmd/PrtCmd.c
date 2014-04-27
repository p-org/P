#include "PrtCmd.h"

int main(int argc, char *argv[])
{
	//// Make the integer type
	PRT_TYPEEXPR intType = PrtMkPrimitiveType(PRT_KIND_INT);

	//// Make the bool type
	PRT_TYPEEXPR boolType = PrtMkPrimitiveType(PRT_KIND_BOOL);

	//// Make the sequence of booleans type
	PRT_SEQTYPE *seqBoolType = PrtMkSeqType(boolType);

	//// Make the int -> seq of bool type.
	PRT_MAPTYPE *int2seqType = PrtMkMapType(intType, (PRT_TYPEEXPR)seqBoolType);

	//// Clone the type
	PRT_TYPEEXPR clone = PrtCloneType((PRT_TYPEEXPR)int2seqType);

	//// Print it
	PrtCmdPrintType((PRT_TYPEEXPR)int2seqType);

	//// Free the type
	PrtFreeType((PRT_TYPEEXPR)int2seqType);

	//// Print the clone
	printf_s("\n");
	PrtCmdPrintType(clone);

	//// Free the clone
	PrtFreeType(clone);

	//// Free the remainder
	PrtFreeType(intType);
	PrtFreeType(boolType);
	PrtFreeType((PRT_TYPEEXPR)seqBoolType);

	return 0;
}
