

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Wed Jun 25 19:21:35 2014
 */
/* Compiler settings for PrtRPC_Types.idl:
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
/* [full][version][uuid] */ 

typedef __int32 PRT_INT32;

typedef __int64 PRT_INT64;

typedef unsigned char PRT_CHAR;

typedef /* [string] */ unsigned char *PRT_STRING;

typedef const unsigned char *PRT_CSTRING;

typedef void *PRT_HANDLE;

typedef /* [public][public][public][public][public][public][public][public][public][public][public][public][public][public][public] */ 
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

typedef /* [public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public] */ struct __MIDL_PrtRPC_0002
    {
    PRT_TYPE_KIND typeKind;
    /* [switch_is] */ /* [switch_type] */ union __MIDL_PrtRPC_0003
        {
        /* [case()] */ struct PRT_MAPTYPE *mapT;
        /* [case()] */ struct PRT_NMDTUPTYPE *nmTupleT;
        /* [case()] */ struct PRT_SEQTYPE *seqT;
        /* [case()] */ struct PRT_TUPTYPE *tupleT;
        } 	type_details;
    } 	PRT_TYPE;

typedef PRT_TYPE *PRT_VALUE;

typedef /* [public][public][public][public] */ 
enum __MIDL_PrtRPC_0004
    {
        _bool	= 0,
        _event	= ( _bool + 1 ) ,
        _integer	= ( _event + 1 ) ,
        _id	= ( _integer + 1 ) ,
        _mid	= ( _id + 1 ) 
    } 	typesInP;

typedef /* [public][public][public] */ struct __MIDL_PrtRPC_0005
    {
    typesInP type;
    /* [switch_is] */ /* [switch_type] */ union __MIDL_PrtRPC_0006
        {
        /* [case()] */ boolean bl;
        /* [case()] */ PRT_INT32 ev;
        /* [case()] */ PRT_INT32 nt;
        /* [case()] */ PRT_INT64 id;
        /* [case()] */ PRT_INT64 mid;
        } 	val;
    } 	PrimVale;

typedef struct PRT_PRIMVALUE
    {
    PRT_TYPE type;
    PrimVale value;
    } 	PRT_PRIMVALUE;

typedef struct PRT_MAPTYPE
    {
    PRT_TYPE domType;
    PRT_TYPE codType;
    } 	PRT_MAPTYPE;

typedef struct PRT_NMDTUPTYPE
    {
    PRT_INT32 arity;
    PRT_STRING *fieldNames;
    PRT_TYPE *fieldTypes;
    } 	PRT_NMDTUPTYPE;

typedef struct PRT_SEQTYPE
    {
    PRT_TYPE innerType;
    } 	PRT_SEQTYPE;

typedef struct PRT_TUPTYPE
    {
    PRT_INT32 arity;
    PRT_TYPE *fieldTypes;
    } 	PRT_TUPTYPE;

typedef struct PRT_GUID
    {
    PRT_INT32 data1;
    PRT_INT32 data2;
    PRT_INT32 data3;
    PRT_INT64 data4;
    } 	PRT_GUID;

typedef struct PRT_TUPVALUE
    {
    PRT_TYPE type;
    PRT_VALUE *values;
    } 	PRT_TUPVALUE;

typedef struct PRT_SEQVALUE
    {
    PRT_TYPE type;
    PRT_INT32 size;
    PRT_INT32 capacity;
    PRT_VALUE *values;
    } 	PRT_SEQVALUE;

typedef struct PRT_MAPVALUE
    {
    PRT_TYPE type;
    PRT_INT32 size;
    PRT_INT32 capNum;
    struct PRT_MAPNODE *first;
    struct PRT_MAPNODE *last;
    struct PRT_MAPNODE **buckets;
    } 	PRT_MAPVALUE;

typedef struct PRT_MAPNODE
    {
    PRT_VALUE key;
    PRT_VALUE value;
    struct PRT_MAPNODE *bucketNext;
    struct PRT_MAPNODE *insertNext;
    struct PRT_MAPNODE *insertPrev;
    } 	PRT_MAPNODE;

/* client prototype */
void c_SendValue1( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_PRIMVALUE *value);
/* server prototype */
void s_SendValue1( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_PRIMVALUE *value);

/* client prototype */
void c_SendValue2( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_PRIMVALUE *value);
/* server prototype */
void s_SendValue2( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_PRIMVALUE *value);

/* client prototype */
void c_SendValueMap1( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_MAPVALUE *value);
/* server prototype */
void s_SendValueMap1( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_MAPVALUE *value);

/* client prototype */
void c_SendValueMap2( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_MAPVALUE *value);
/* server prototype */
void s_SendValueMap2( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_MAPVALUE *value);



extern RPC_IF_HANDLE c_PrtRPC_v1_0_c_ifspec;
extern RPC_IF_HANDLE PrtRPC_v1_0_c_ifspec;
extern RPC_IF_HANDLE s_PrtRPC_v1_0_s_ifspec;
#endif /* __PrtRPC_INTERFACE_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


