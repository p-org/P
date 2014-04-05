/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

    SmfPublic.h

Abstract:
    This header file contains declarations of all the public functions which can be 
	used inside driver code.

Environment:

    Kernel mode only.		

***********************************************************************************/
#pragma once

#include "SmfPublicTypes.h"

/*********************************************************************************

		Macro Functions For Enqueue

*********************************************************************************/

//
// Macro to enqueue an Event with Payload
//
#define SMF_ENQUEUEEVENT_WITH_PAYLOAD(Machine, EventIndex, Payload, UseWorkItem) SmfEnqueueEvent(Machine, EventIndex, Payload, UseWorkItem)

//
//Macro to enqueue an Event without Payload
//
#define SMF_ENQUEUEEVENT(Machine, EventIndex, UseWorkItem) SmfEnqueueEvent(Machine, EventIndex, 0, UseWorkItem)


/*********************************************************************************

		Functions - Machine Creation

*********************************************************************************/
#ifdef KERNEL_MODE
//
// Initializes StateMachine attributes used for creating a machine of type InstanceOf
//
VOID 
SmfInitAttributes(
__inout PSMF_MACHINE_ATTRIBUTES Attributes, 
__in PDEVICE_OBJECT				PDeviceObj, 
__in PSMF_DRIVERDECL			Driver,
__in SMF_MACHINEDECL_INDEX		InstanceOf,
__in PSMF_PACKED_VALUE			Arg,
__in PVOID						PFrgnMem
);
#else
VOID 
SmfInitAttributes(
__inout PSMF_MACHINE_ATTRIBUTES Attributes, 
__in PSMF_DRIVERDECL			Driver,
__in SMF_MACHINEDECL_INDEX		InstanceOf,
__in PSMF_PACKED_VALUE			Arg,
__in PVOID						PFrgnMem
);
#endif
//
//Creates a new State Machine of using Machine_Attributes and initializes PSmHandle to new Machine handle
//
NTSTATUS 
SmfCreate(
__in PSMF_MACHINE_ATTRIBUTES	InitAttributes, 
__out PSMF_MACHINE_HANDLE		PSmHandle
);

/*********************************************************************************

		Functions - Machine Interaction

*********************************************************************************/
//
// Enqueue Event on to the State Machine
//

VOID 
SmfEnqueueEvent(
__in SMF_MACHINE_HANDLE			Machine, 
__in SMF_EVENTDECL_INDEX		EventIndex, 
__in PSMF_PACKED_VALUE			Arg,
__in BOOLEAN					UseWorkItem
); 

//
// Get Foreign Memory Context for the State Machine
//
PSMF_EXCONTEXT
SmfGetForeignContext(
__in SMF_MACHINE_HANDLE SmHandle
);

#ifdef DISTRIBUTED_RUNTIME
int StartMachine(long InstanceOf, int argc, wchar_t* argv[]);
int NewMachine(PSMF_DRIVERDECL decl, int argc, wchar_t* argv[]);
#endif