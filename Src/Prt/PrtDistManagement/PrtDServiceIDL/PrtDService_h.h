

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Tue Sep 30 21:08:58 2014
 */
/* Compiler settings for PrtDService.idl:
    Oicf, W1, Zp8, env=Win32 (32b run), target_arch=X86 8.00.0603 
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


#ifndef __PrtDService_h_h__
#define __PrtDService_h_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __PrtDService_INTERFACE_DEFINED__
#define __PrtDService_INTERFACE_DEFINED__

/* interface PrtDService */
/* [explicit_handle][full][version][uuid] */ 

/* client prototype */
void c_PrtDPingService( 
    /* [in] */ handle_t myHandle,
    /* [out] */ boolean *status);
/* server prototype */
void s_PrtDPingService( 
    /* [in] */ handle_t myHandle,
    /* [out] */ boolean *status);

/* client prototype */
void c_PrtDCreateNodeManagerForJob( 
    /* [in] */ handle_t myHandle,
    /* [string][in] */ unsigned char *JobID,
    /* [out] */ boolean *status);
/* server prototype */
void s_PrtDCreateNodeManagerForJob( 
    /* [in] */ handle_t myHandle,
    /* [string][in] */ unsigned char *JobID,
    /* [out] */ boolean *status);



extern RPC_IF_HANDLE c_PrtDService_v1_0_c_ifspec;
extern RPC_IF_HANDLE PrtDService_v1_0_c_ifspec;
extern RPC_IF_HANDLE s_PrtDService_v1_0_s_ifspec;
#endif /* __PrtDService_INTERFACE_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


