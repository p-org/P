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



/*********************************************************************************

Functions - Machine Creation

*********************************************************************************/
#ifdef KERNEL_MODE
//
// Initializes StateMachine attributes used for creating a machine of type InstanceOf
//
VOID
PrtInitAttributes(
__inout PPRT_MACHINE_ATTRIBUTES Attributes,
__in PDEVICE_OBJECT				PDeviceObj,
__in PPRT_DRIVERDECL			Driver,
__in PRT_MACHINEDECL_INDEX		InstanceOf,
__in PPRT_PACKED_VALUE			Arg,
__in PVOID						PFrgnMem
);
#else
VOID
PrtInitAttributes(
__inout PPRT_MACHINE_ATTRIBUTES Attributes,
__in PPRT_DRIVERDECL			Driver,
__in PRT_MACHINEDECL_INDEX		InstanceOf,
__in PPRT_PACKED_VALUE			Arg,
__in PVOID						PFrgnMem
);
#endif
//
//Creates a new State Machine of using Machine_Attributes and initializes PSmHandle to new Machine handle
//
NTSTATUS
PrtCreate(
__in PPRT_MACHINE_ATTRIBUTES	InitAttributes,
__out PPRT_MACHINE_HANDLE		PSmHandle
);

/*********************************************************************************

Functions - Machine Interaction

*********************************************************************************/
//
// Enqueue Event on to the State Machine
//

VOID
SmfEnqueueEvent(
__in PRT_MACHINE_HANDLE			Machine,
__in PRT_EVENTDECL_INDEX		EventIndex,
__in PPRT_PACKED_VALUE			Arg,
__in PRT_BOOLEAN					UseWorkItem
);

//
// Get Foreign Memory Context for the State Machine
//
PPRT_EXCONTEXT
PrtGetForeignContext(
__in PRT_MACHINE_HANDLE SmHandle
);
