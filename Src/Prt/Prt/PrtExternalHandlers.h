#include "PrtHeaders.h"

#include <stdio.h>

void
PrtExternalExceptionHandler(
__in PRT_EXCEPTIONS exception,
__in void* vcontext
);

void
PrtExternalLogHandler(
__in PRT_STEP step,
__in void* vcontext
);

