

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Wed Oct 29 11:21:45 2014
 */
/* Compiler settings for PrtDistPrimitiveTypes.idl:
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


#ifndef __PrtDistPrimitiveTypes_h__
#define __PrtDistPrimitiveTypes_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __PrtDistPrimitiveTypes_INTERFACE_DEFINED__
#define __PrtDistPrimitiveTypes_INTERFACE_DEFINED__

/* interface PrtDistPrimitiveTypes */
/* [full][version][uuid] */ 

typedef unsigned small PRT_UINT8;

typedef unsigned short PRT_UINT16;

typedef unsigned long PRT_UINT32;

typedef unsigned long long PRT_UINT64;

typedef signed small PRT_INT8;

typedef short PRT_INT16;

typedef long PRT_INT32;

typedef long long PRT_INT64;

typedef unsigned char PRT_CHAR;

typedef /* [string] */ unsigned char *PRT_STRING;

typedef /* [string] */ const unsigned char *PRT_CSTRING;

typedef enum _PRT_BOOLEAN PRT_BOOLEAN;


enum _PRT_BOOLEAN
    {
        PRT_FALSE	= 0,
        PRT_TRUE	= 1
    } ;


extern RPC_IF_HANDLE c_PrtDistPrimitiveTypes_v1_0_c_ifspec;
extern RPC_IF_HANDLE PrtDistPrimitiveTypes_v1_0_c_ifspec;
extern RPC_IF_HANDLE s_PrtDistPrimitiveTypes_v1_0_s_ifspec;
#endif /* __PrtDistPrimitiveTypes_INTERFACE_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


