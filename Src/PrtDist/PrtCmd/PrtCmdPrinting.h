#ifndef PRTCMDPRINTING_H
#define PRTCMDPRINTING_H

#include <stdio.h>
#include "..\Prt\Prt.h"

void PrtCmdPrintType(_In_ PRT_TYPE type);

void PrtCmdPrintValue(_In_ PRT_VALUE *value);

void PrtCmdPrintValueAndType(_In_ PRT_VALUE *value);

#endif