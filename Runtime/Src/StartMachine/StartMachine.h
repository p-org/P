

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0595 */
/* at Mon Mar 31 10:33:49 2014
 */
/* Compiler settings for StartMachine.idl:
    Oicf, W1, Zp8, env=Win32 (32b run), target_arch=X86 8.00.0595 
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__


#ifndef __StartMachine_h__
#define __StartMachine_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __StartMachine_INTERFACE_DEFINED__
#define __StartMachine_INTERFACE_DEFINED__

/* interface StartMachine */
/* [explicit_handle][version][uuid] */ 

/* client prototype */
void Client_WrapSmfNew( 
    handle_t startMachine_IfHandle,
    /* [in] */ long InstanceOf);
/* server prototype */
void WrapSmfNew( 
    handle_t startMachine_IfHandle,
    /* [in] */ long InstanceOf);

/* client prototype */
void Client_WrapSmfEnqueueEvent( 
    handle_t startMachine_IfHandle,
    /* [in] */ long EventIndex);
/* server prototype */
void WrapSmfEnqueueEvent( 
    handle_t startMachine_IfHandle,
    /* [in] */ long EventIndex);

/* client prototype */
void Client_ChildUp( 
    handle_t startMachine_IfHandle);
/* server prototype */
void ChildUp( 
    handle_t startMachine_IfHandle);

/* client prototype */
void Client_Shutdown( 
    handle_t startMachine_IfHandle);
/* server prototype */
void Shutdown( 
    handle_t startMachine_IfHandle);



extern RPC_IF_HANDLE Client_StartMachine_v1_0_c_ifspec;
extern RPC_IF_HANDLE StartMachine_v1_0_c_ifspec;
extern RPC_IF_HANDLE StartMachine_v1_0_s_ifspec;
#endif /* __StartMachine_INTERFACE_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


