#include "PrtWinUser.h"
#include "PrtExecution.h"

PRT_TYPE*
PrtDistDeserializeType(
__in PRT_TYPE* type
);

PRT_TYPE*
PrtDistSerializeType(
__in PRT_TYPE* type
);

PRT_VALUE*
PrtDistDeserializeValue(
__in PRT_VALUE* value
);

PRT_VALUE*
PrtDistSerializeValue(
__in PRT_VALUE* value
);

void
PrtDistSMExceptionHandler(
__in PRT_STATUS exception,
__in void* vcontext
);

void
PrtDistSMLogHandler(
__in PRT_STEP step,
__in void* vcontext
);