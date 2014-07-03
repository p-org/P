/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

SmfLogger.h

Abstract:
This header file contains declarations for functions used for logging information
by P Runtime.

Environment:

Kernel mode only.

***********************************************************************************/


#pragma once
#include "SmfDepends.h"
#include "SmfPublicTypes.h"


//
// Trace P Step
//
VOID
SmfTraceStep(
__in PSMF_SMCONTEXT Context,
__in SMF_TRACE_STEP TStep,
...
);

//
// Report P Exception
//

VOID
SmfReportException(
__in SMF_EXCEPTIONS		ExC,
__in PSMF_SMCONTEXT		Machine
);

//
// Log an Assertion Failure
//
VOID SmfLogAssertionFailure(
	const char* File,
	ULONG Line,
	const char* Msg
	);

