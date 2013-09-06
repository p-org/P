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

		Private Macros

*********************************************************************************/


// 4127 -- Conditional Expression is Constant warning
#define WHILE(constant) \
    __pragma(warning(suppress: 4127)) \
    while(constant) 

#define TRY
#define LEAVE   goto __tryLabel;
#define FINALLY goto __tryLabel; __tryLabel:
#undef __try
#undef __finally


/*********************************************************************************

		Enum Types

*********************************************************************************/

//
// Signature of the State machine
//
typedef enum _SMF_SM_SIGNATURE SMF_SM_SIGNATURE;

//
// Private Runtime state flag 
//
typedef enum _SMF_STATE_RUNTIMEFLAGS SMF_STATE_RUNTIMEFLAGS;



/*********************************************************************************

Type Name : SMF_SM_SIGNATURE

Description : 
	Enum for state-machine signature its a unique value
	If statemachine->signature is set to StateMachine_Signature then the state-machine
	is valid
	Else state-machine has been freed and access to it is invalid

*********************************************************************************/
//State Machine Signature
typedef enum _SMF_SM_SIGNATURE
{
//
// Unique value selected arbitrarily
//
	SmfStateMachine_Signature = 2012
};



