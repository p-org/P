/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

    SmfProtected.h

Abstract:
    This header file contains function declarations of protected nature 
	these functions should be called only from the entry functions and exit functions
	of a state.

Environment:

    Kernel mode only.		

***********************************************************************************/

#pragma once
#include "SmfPublic.h"
#include "SmfProtectedTypes.h"
#include "SmfLogger.h"
#include "SmfArrayList.h"

/*********************************************************************************

		Macros Functions

*********************************************************************************/

#ifdef KERNEL_MODE
#define SMF_ASSERTMSG(msg, cond)	NT_ASSERTMSG(msg, (cond));
#define SMF_ASSERT(cond)	NT_ASSERT((cond));
#else																							
#define SMF_ASSERTMSG(msg, cond)	if (!(cond)) { SmfLogAssertionFailure((const char *)__FILE__, (ULONG)__LINE__, (const char *)msg); exit(-1); };
#define SMF_ASSERT(cond)	if (!(cond)) { exit(-1); }
#endif

#define MAKE_OPAQUE(Fun) (PSMF_OPAQUE_FUN)(&(Fun))

#define MAKE_OPAQUE_CONSTRUCTOR(Fun) (PSMF_OPAQUE_CONST_FUN)(&(Fun))

#define MAKE_OPAQUE_CLONE(Fun) (PSMF_OPAQUE_CLONE_FUN)(&(Fun))
#define MAKE_OPAQUE_BUILDDEF(Fun) (PSMF_OPAQUE_BUILDDEF_FUN)(&(Fun))
#define MAKE_OPAQUE_DESTROY(Fun) (PSMF_OPAQUE_DESTROY_FUN)(&(Fun))
#define MAKE_OPAQUE_EQUALS(Fun) (PSMF_OPAQUE_EQUALS_FUN)(&(Fun))
#define MAKE_OPAQUE_HASHCODE(Fun) (PSMF_OPAQUE_HASHCODE_FUN)(&(Fun))

//
// Used for removing the UNREFERENCEDPARAMETER warning
//
#define DUMMYREFERENCE(Context) (Context);

extern const SMF_PACKED_VALUE g_SmfNullPayload;

/*********************************************************************************

		Raise / Pop / Call Statements in Entry Functions

*********************************************************************************/

//
//Enqueue a private event 
//
VOID 
SmfRaise(
__inout PSMF_SMCONTEXT		Context, 
__in SMF_EVENTDECL_INDEX	EventIndex,
__in PSMF_PACKED_VALUE		Arg
); 

//
// Pop Current state and return to the caller state
//
VOID 
SmfPop(
__inout PSMF_SMCONTEXT		Context
);

//
// Execute Call Statement
//
VOID 
SmfCall(
__inout PSMF_SMCONTEXT		Context, 
SMF_STATEDECL_INDEX			State
);

//
// Execute New Statement
//
SMF_MACHINE_HANDLE 
SmfNew(
__in PSMF_DRIVERDECL			PDriverDecl, 
__inout PSMF_SMCONTEXT			Context, 
__in SMF_MACHINEDECL_INDEX		InstanceOf, 
__in INT						NumInitializers, 
...
);

//
// Delete the current state-machine
//
VOID 
SmfDelete(
PSMF_SMCONTEXT				Context
);

ULONG_PTR
SmfAllocateType(
__in PSMF_DRIVERDECL			Driver,
__in SMF_TYPEDECL_INDEX			Type);

VOID
SmfFreeType(
__in PSMF_DRIVERDECL			Driver,
__in SMF_TYPEDECL_INDEX			Type,
__in PVOID						Value);

ULONG_PTR
SmfAllocateDefaultType(
__in PSMF_DRIVERDECL			Driver,
__in SMF_TYPEDECL_INDEX			Type);

/*********************************************************************************

		Memory Management Functions.

*********************************************************************************/

FORCEINLINE
PVOID 
SmfAllocateMemory(
UINT						SizeOf
);

FORCEINLINE
VOID
SmfFreeMemory(
PVOID						PointerTo
);


/*********************************************************************************

		Operations on Packed Values.

*********************************************************************************/
//
// Pack a value in a preallocated piece of memory 
//
VOID
PackValue(
__in PSMF_DRIVERDECL			Driver,
__in PSMF_PACKED_VALUE			Dst,
__in ULONG_PTR					Value,
__in SMF_TYPEDECL_INDEX			Type
);

//
// Clone a packed value
//
VOID
Clone_PackedValue(
__in PSMF_DRIVERDECL			Driver,
__in PSMF_PACKED_VALUE			Dst,
__in PSMF_PACKED_VALUE			Src
);

//
// Pack a value in a preallocated piece of memory 
//
VOID
Destroy_PackedValue(
__in PSMF_DRIVERDECL			Driver,
__in PSMF_PACKED_VALUE			Dst
);
