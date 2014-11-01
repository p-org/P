#include "PrtWinUser.h"

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
PrtDistExceptionHandler(
__in PRT_STATUS exception,
__in void* vcontext
);

void
PrtDistLogHandler(
__in PRT_STEP step,
__in void* vcontext
);

PRT_STRING
PrtValueToString(
_In_ PRT_VALUE *value
);