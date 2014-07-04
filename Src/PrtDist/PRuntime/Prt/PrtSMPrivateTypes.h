/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

SmfPrivateTypes.h

Abstract:
This header file contains declarations of Types used internally by P Runtime,
these types are of private nature and should not be called used out side
of P runtime.

Environment:

Kernel mode only.

***********************************************************************************/

#pragma once

/*********************************************************************************

Enum Types

*********************************************************************************/

//
// Signature of the State machine
//
typedef enum _PRT_SM_SIGNATURE PRT_SM_SIGNATURE;

//
// Private Runtime state flag 
//
typedef enum _PRT_STATE_RUNTIMEFLAGS PRT_STATE_RUNTIMEFLAGS;



/*********************************************************************************

Type Name : PRT_SM_SIGNATURE

Description :
Enum for state-machine signature its a unique value
If statemachine->signature is set to StateMachine_Signature then the state-machine
is valid
Else state-machine has been freed and access to it is invalid

*********************************************************************************/
//State Machine Signature
typedef enum _PRT_SM_SIGNATURE
{
	//
	// Unique value selected arbitrarily
	//
	PrtStateMachine_Signature = 2014
};



