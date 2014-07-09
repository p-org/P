#include "../../Prt/PrtHeaders.h"

#include <stdio.h>

VOID
PrtExternalExceptionHandler(
__in PRT_EXCEPTIONS exception,
__in PVOID vcontext
);

VOID
PrtExternalLogHandler(
__in PRT_STEP step,
__in PVOID vcontext
);

