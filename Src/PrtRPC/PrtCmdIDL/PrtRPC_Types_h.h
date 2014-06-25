

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0595 */
/* at Tue Jun 24 17:27:05 2014
 */
/* Compiler settings for PrtRPC_Types.idl:
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


#ifndef __PrtRPC_Types_h_h__
#define __PrtRPC_Types_h_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __PrtRPC_INTERFACE_DEFINED__
#define __PrtRPC_INTERFACE_DEFINED__

/* interface PrtRPC */
/* [unique][version][uuid] */ 

typedef /* [public] */ 
enum __MIDL_PrtRPC_0001
    {
        PRT_KIND_ANY	= 0,
        PRT_KIND_BOOL	= 1,
        PRT_KIND_EVENT	= 2,
        PRT_KIND_FORGN	= 3,
        PRT_KIND_ID	= 4,
        PRT_KIND_INT	= 5,
        PRT_KIND_MAP	= 6,
        PRT_KIND_MID	= 7,
        PRT_KIND_NMDTUP	= 8,
        PRT_KIND_SEQ	= 9,
        PRT_KIND_TUPLE	= 10,
        PRT_TYPE_KIND_COUNT	= 11,
        PRT_TYPE_KIND_CANARY	= 0xff
    } 	PRT_TYPE_KIND;

typedef enum __MIDL_PrtRPC_0001 *PRT_TYPE;

typedef PRT_TYPE *PRT_VALUE;

void SendValue1( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_VALUE value);

void SendValue2( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_VALUE value);



extern RPC_IF_HANDLE PrtRPC_v1_0_c_ifspec;
extern RPC_IF_HANDLE PrtRPC_v1_0_s_ifspec;
#endif /* __PrtRPC_INTERFACE_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


