#include "PrtWinUser.h"
#include "PrtExecution.h"

#define MAX_LOG_SIZE 1000
/**
We do not serialize types anymore as they are removed from the PrtValues

PRT_TYPE*
PrtDistDeserializeType(
__in PRT_TYPE* type
);

PRT_TYPE*
PrtDistSerializeType(
__in PRT_TYPE* type
);

*/

/** Deserializes a Prt Value sent over RPC.
* @param[in] value Prt Value.
* @returns A proper value after deserialization.
* @see PrtDistSerializeValue
*/
PRT_VALUE*
PrtDistDeserializeValue(
__in PRT_VALUE* value
);

/** Serializes a Prt Value to be sent over RPC.
* @param[in] value Prt Value.
* @returns A proper value with serialization information.
* @see PrtDistSerializeValue
*/
PRT_VALUE*
PrtDistSerializeValue(
__in PRT_VALUE* value
);

boolean PrtDistGetNextNodeId(int *nextNodeId);

boolean PrtDistCreateContainer(int nodeId, int* newContainerId);


handle_t
PrtDistCreateRPCClient(
PRT_VALUE* target
);