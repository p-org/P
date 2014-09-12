/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

PrtPublic.h

Abstract:
This header file contains declarations of all the public functions which can be
used inside driver code.

Environment:

Kernel mode only.

***********************************************************************************/
#pragma once

#include "PrtSMPublicTypes.h"
#include "Config\PrtConfig.h"


//
//Creates a new State Machine of using Machine_Attributes and initializes PSmHandle to new Machine handle
//
PRT_STATUS
PrtCreate(
__in  PRT_PPROCESS				*process,
__in  PRT_UINT32				instanceOf,
__in  PRT_VALUE					*payload,
__out PRT_MACHINE_HANDLE		*pSmHandle
);

/*********************************************************************************

Functions - Machine Interaction

*********************************************************************************/
VOID
PrtEnqueueEvent(
__in PRT_MACHINE_HANDLE			machine,
__in PRT_VALUE					*event,
__in PRT_VALUE					*payload
);

//
// Get Foreign Memory Context for the State Machine
//
PRT_EXCONTEXT*
PrtGetForeignContext(
__in PRT_MACHINE_HANDLE smHandle
);
