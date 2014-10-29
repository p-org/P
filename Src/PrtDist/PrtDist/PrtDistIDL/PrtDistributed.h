

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Wed Oct 15 10:48:39 2014
 */
/* Compiler settings for PrtDistributed.idl:
    Oicf, W1, Zp8, env=Win64 (32b run), target_arch=AMD64 8.00.0603 
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


#ifndef __PrtDistributed_h__
#define __PrtDistributed_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

/* header files for imported files */
#include "PrtDistValues.h"

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __PrtDistributed_INTERFACE_DEFINED__
#define __PrtDistributed_INTERFACE_DEFINED__

/* interface PrtDistributed */
/* [full][version][uuid] */ 

/* client prototype */
void c_SendValue1( 
    /* [in] */ handle_t handleM,
    /* [full][in] */ PRT_VALUE *value);
/* server prototype */
void s_SendValue1( 
    /* [in] */ handle_t handleM,
    /* [full][in] */ PRT_VALUE *value);

/* client prototype */
void c_SendValue2( 
    /* [in] */ handle_t handleM,
    /* [full][in] */ PRT_VALUE *value);
/* server prototype */
void s_SendValue2( 
    /* [in] */ handle_t handleM,
    /* [full][in] */ PRT_VALUE *value);



extern RPC_IF_HANDLE c_PrtDistributed_v1_0_c_ifspec;
extern RPC_IF_HANDLE PrtDistributed_v1_0_c_ifspec;
extern RPC_IF_HANDLE s_PrtDistributed_v1_0_s_ifspec;
#endif /* __PrtDistributed_INTERFACE_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


