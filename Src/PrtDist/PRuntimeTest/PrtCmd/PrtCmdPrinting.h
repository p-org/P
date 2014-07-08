#ifndef PRTCMDPRINTING_H
#define PRTCMDPRINTING_H

#include <stdio.h>
#include"../../PRuntime/Prt/Values/PrtDTTypes.h"
#include"../../PRuntime/Prt/Values/PrtDTValues.h"

void PrtCmdPrintType(_In_ PRT_TYPE type);

void PrtCmdPrintValue(_In_ PRT_VALUE *value);

void PrtCmdPrintValueAndType(_In_ PRT_VALUE *value);

#endif