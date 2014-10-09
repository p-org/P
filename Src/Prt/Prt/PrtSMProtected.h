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
void
PrtRaise(
__inout PRT_SMCONTEXT		*context,
__in PRT_VALUE	*event,
__in PRT_VALUE	*payload
);

//
// Pop Current state and return to the caller state
//

void
PrtPop(
__inout PRT_SMCONTEXT		*context
);

//
// Execute Call Statement
//
void
PrtCall(
__inout PRT_SMCONTEXT		*context,
__in PRT_UINT32				stateIndex
);

//
// Execute New Statement
//
void
PrtHaltMachine(
__inout PRT_SMCONTEXT			*context
);

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
void
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


