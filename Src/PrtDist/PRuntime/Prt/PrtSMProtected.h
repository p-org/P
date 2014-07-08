/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

PrtProtected.h

Abstract:
This header file contains function declarations of protected nature
these functions should be called only from the entry functions and exit functions
of a state.

Environment:

Kernel mode only.

***********************************************************************************/

#pragma once
#include "PrtSMPublic.h"
#include "PrtSMProtectedTypes.h"

/*********************************************************************************

Macros Functions

*********************************************************************************/

#define MAKE_OPAQUE(Fun) (PPRT_OPAQUE_FUN)(&(Fun))

//
// Used for removing the UNREFERENCEDPARAMETER warning
//
#define DUMMYREFERENCE(Context) (Context);

/*********************************************************************************

Raise / Pop / Call Statements in Entry Functions

*********************************************************************************/

//
//Enqueue a private event 
//
VOID
PrtRaise(
__inout PRT_SMCONTEXT		*context,
__in PRT_VALUE	*event,
__in PRT_VALUE	*payload
);

//
// Pop Current state and return to the caller state
//

VOID
PrtPop(
__inout PRT_SMCONTEXT		*context
);

//
// Execute Call Statement
//
VOID
PrtCall(
__inout PRT_SMCONTEXT		*context,
__in PRT_UINT32				stateIndex
);

//
// Execute New Statement
//
PRT_MACHINE_HANDLE
PrtNew(
__in PRT_PROGRAMDECL			*programDecl,
__inout PRT_SMCONTEXT			*context,
__in PRT_UINT32					instanceOf,
__in PRT_VALUE					*payload
);

//
// Delete the current state-machine
//
VOID
PrtDelete(
PRT_SMCONTEXT				*context
);

