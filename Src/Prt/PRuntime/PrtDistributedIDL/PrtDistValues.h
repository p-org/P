

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Mon Jul 07 20:32:42 2014
 */
/* Compiler settings for PrtDistValues.idl:
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


#ifndef __PrtDistValues_h__
#define __PrtDistValues_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

/* header files for imported files */
#include "PrtDistTypes.h"

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __PrtDistValues_INTERFACE_DEFINED__
#define __PrtDistValues_INTERFACE_DEFINED__

/* interface PrtDistValues */
/* [full][version][uuid] */ 

typedef struct _PRT_VALUE PRT_VALUE;

typedef struct _PRT_FORGNVALUE PRT_FORGNVALUE;

typedef struct _PRT_TUPVALUE PRT_TUPVALUE;

typedef struct _PRT_SEQVALUE PRT_SEQVALUE;

typedef struct _PRT_MAPVALUE PRT_MAPVALUE;

typedef struct _PRT_MAPNODE PRT_MAPNODE;

typedef struct _PRT_VALUE_NODE PRT_VALUE_NODE;

typedef /* [public][public][public][public][public] */ struct __MIDL_PrtDistValues_0001
    {
    PRT_TYPE_KIND discriminator;
    /* [switch_is] */ /* [switch_type] */ union __MIDL_PrtDistValues_0002
        {
        /* [case()] */ PRT_BOOLEAN bl;
        /* [case()] */ PRT_UINT32 ev;
        /* [case()] */ PRT_UINT32 mach;
        /* [case()] */ PRT_UINT32 model;
        /* [case()] */ PRT_INT32 nt;
        } 	value;
    } 	PRT_PRIMVALUE;

struct _PRT_VALUE
    {
    PRT_TYPE type;
    struct 
        {
        PRT_TYPE_KIND discriminator;
        /* [switch_is] */ /* [switch_type] */ union __MIDL_PrtDistValues_0004
            {
            /* [case()][unique] */ PRT_PRIMVALUE *primValue;
            /* [case()][unique] */ PRT_FORGNVALUE *frgn;
            /* [case()][unique] */ PRT_MAPVALUE *map;
            /* [case()][unique] */ PRT_SEQVALUE *seq;
            /* [case()][unique] */ PRT_TUPVALUE *tuple;
            } 	valueUnion;
        } 	;
    } ;
struct _PRT_FORGNVALUE
    {
    int *value;
    } ;
struct _PRT_TUPVALUE
    {
    PRT_VALUE **values;
    PRT_VALUE_NODE *valuesSerialized;
    } ;
struct _PRT_SEQVALUE
    {
    PRT_UINT32 size;
    PRT_UINT32 capacity;
    PRT_VALUE **values;
    PRT_VALUE_NODE *valuesSerialized;
    } ;
struct _PRT_VALUE_NODE
    {
    PRT_VALUE *value;
    PRT_VALUE_NODE *nextNode;
    } ;
struct _PRT_MAPVALUE
    {
    PRT_UINT32 size;
    PRT_UINT32 capNum;
    PRT_MAPNODE *first;
    PRT_MAPNODE *last;
    PRT_MAPNODE **buckets;
    } ;
struct _PRT_MAPNODE
    {
    PRT_VALUE *key;
    PRT_VALUE *value;
    PRT_MAPNODE *bucketNext;
    PRT_MAPNODE *insertNext;
    PRT_MAPNODE *insertPrev;
    } ;


extern RPC_IF_HANDLE c_PrtDistValues_v1_0_c_ifspec;
extern RPC_IF_HANDLE PrtDistValues_v1_0_c_ifspec;
extern RPC_IF_HANDLE s_PrtDistValues_v1_0_s_ifspec;
#endif /* __PrtDistValues_INTERFACE_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


