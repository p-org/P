/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

PrtLogger.h

Abstract:
This header file contains declarations for functions used for logging information
by P Runtime.

Environment:

Kernel mode only.

***********************************************************************************/


#pragma once
#include "Config\PrtConfig.h"
#include "PrtSMPublicTypes.h"


//
// Trace P Step
//
VOID
PrtTraceStep(
__in PPRT_SMCONTEXT context,
__in PRT_TRACE_STEP tStep,
...
);

//
// Report P Exception
//

VOID
PrtReportException(
__in PRT_EXCEPTIONS		exception,
__in PPRT_SMCONTEXT		machine
);

//
// Log an Assertion Failure
//
VOID PrtLogAssertionFailure(
	PRT_CSTRING file,
	PRT_INT32 line,
	PRT_CSTRING msg
);

