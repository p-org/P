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

#define MAKE_OPAQUE_CONSTRUCTOR(Fun) (PPRT_OPAQUE_CONST_FUN)(&(Fun))
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
__inout PPRT_SMCONTEXT		Context,
__in PRT_EVENTDECL_INDEX	EventIndex,
__in PPRT_PACKED_VALUE		Arg
);

//
// Pop Current state and return to the caller state
//

VOID
PrtPop(
__inout PPRT_SMCONTEXT		Context
);

//
// Execute Call Statement
//
VOID
PrtCall(
__inout PPRT_SMCONTEXT		Context,
PRT_STATEDECL_INDEX			State
);

//
// Execute New Statement
//
PRT_MACHINE_HANDLE
PrtNew(
__in PPRT_DRIVERDECL			PDriverDecl,
__inout PPRT_SMCONTEXT			Context,
__in PRT_MACHINEDECL_INDEX		InstanceOf,
__in PPRT_PACKED_VALUE			Arg
);

//
// Delete the current state-machine
//
VOID
PrtDelete(
PPRT_SMCONTEXT				Context
);

ULONG_PTR
PrtAllocateType(
__in PPRT_DRIVERDECL			Driver,
__in PRT_TYPEDECL_INDEX			Type);

VOID
PrtFreeType(
__in PPRT_DRIVERDECL			Driver,
__in PRT_TYPEDECL_INDEX			Type,
__in PVOID						Value);

ULONG_PTR
PrtAllocateDefaultType(
__in PPRT_DRIVERDECL			Driver,
__in PRT_TYPEDECL_INDEX			Type);

/*********************************************************************************

Memory Management Functions.

*********************************************************************************/

FORCEINLINE
PVOID
PrtAllocateMemory(
UINT						SizeOf
);

FORCEINLINE
VOID
PrtFreeMemory(
PVOID						PointerTo
);


//
// Function prototypes for communicating with model machines
//
VOID
EnqueueEvent(
__in PRT_MACHINE_HANDLE			Machine,
__in PRT_EVENTDECL_INDEX		EventIndex,
__in PPRT_PACKED_VALUE			Arg,
__in BOOLEAN					UseWorkItem
);

PRT_MACHINE_HANDLE
New(
__in PPRT_DRIVERDECL			PDriverDecl,
__inout PPRT_SMCONTEXT			Context,
__in PRT_MACHINEDECL_INDEX		InstanceOf,
__in PPRT_PACKED_VALUE			Arg
);