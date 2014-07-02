

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0595 */
/* at Tue Jul 01 20:51:56 2014
 */
/* Compiler settings for PrtDistributed_TypesAndValues.idl:
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


#ifndef __PrtDistributed_TypesAndValues_h_h__
#define __PrtDistributed_TypesAndValues_h_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __PrtDistributed_INTERFACE_DEFINED__
#define __PrtDistributed_INTERFACE_DEFINED__

/* interface PrtDistributed */
/* [full][version][uuid] */ 

typedef 
enum PRT_BOOLEAN
    {
        PRT_FALSE	= 0,
        PRT_TRUE	= 1
    } 	PRT_BOOLEAN;

typedef enum _PRT_TYPE_KIND PRT_TYPE_KIND;

typedef struct _PRT_MAPTYPE PRT_MAPTYPE;

typedef struct _PRT_NMDTUPTYPE PRT_NMDTUPTYPE;

typedef struct _PRT_SEQTYPE PRT_SEQTYPE;

typedef struct _PRT_TUPTYPE PRT_TUPTYPE;

typedef struct _PRT_FORGNTYPE PRT_FORGNTYPE;

typedef struct _PRT_TYPE_NODE PRT_TYPE_NODE;

typedef struct _PRT_STRING_NODE PRT_STRING_NODE;

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

typedef const unsigned char *PRT_CSTRING;


enum _PRT_TYPE_KIND
    {
        PRT_KIND_ANY	= 0,
        PRT_KIND_BOOL	= 1,
        PRT_KIND_EVENT	= 2,
        PRT_KIND_FORGN	= 3,
        PRT_KIND_MACHINE	= 4,
        PRT_KIND_INT	= 5,
        PRT_KIND_MAP	= 6,
        PRT_KIND_MODEL	= 7,
        PRT_KIND_NMDTUP	= 8,
        PRT_KIND_NULL	= 9,
        PRT_KIND_SEQ	= 10,
        PRT_KIND_TUPLE	= 11,
        PRT_TYPE_KIND_COUNT	= 12,
        PRT_TYPE_KIND_CANARY	= 0xff
    } ;
typedef /* [public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public] */ struct __MIDL_PrtDistributed_0001
    {
    PRT_TYPE_KIND typeKind;
    /* [switch_is] */ /* [switch_type] */ union __MIDL_PrtDistributed_0002
        {
        /* [case()] */ PRT_UINT32 dummy_1;
        /* [case()] */ PRT_UINT32 dummy_2;
        /* [case()] */ PRT_UINT32 dummy_3;
        /* [case()] */ PRT_FORGNTYPE *forgn;
        /* [case()] */ PRT_UINT32 dummy_4;
        /* [case()] */ PRT_UINT32 dummy_5;
        /* [case()] */ PRT_MAPTYPE *map;
        /* [case()] */ PRT_UINT32 dummy_6;
        /* [case()] */ PRT_NMDTUPTYPE *nmTuple;
        /* [case()] */ PRT_UINT32 dummy_7;
        /* [case()] */ PRT_SEQTYPE *seq;
        /* [case()] */ PRT_TUPTYPE *tuple;
        } 	typeUnion;
    } 	PRT_TYPE;

struct _PRT_MAPTYPE
    {
    PRT_TYPE domType;
    PRT_TYPE codType;
    } ;
struct _PRT_NMDTUPTYPE
    {
    PRT_UINT32 arity;
    PRT_STRING *fieldNames;
    PRT_TYPE *fieldTypes;
    PRT_TYPE_NODE *fieldTypesSerialized;
    PRT_STRING_NODE *fieldNamesSerialized;
    } ;
struct _PRT_TYPE_NODE
    {
    PRT_TYPE type;
    PRT_TYPE_NODE *nextNode;
    } ;
struct _PRT_STRING_NODE
    {
    PRT_STRING name;
    PRT_STRING_NODE *nextNode;
    } ;
struct _PRT_SEQTYPE
    {
    PRT_TYPE innerType;
    } ;
struct _PRT_TUPTYPE
    {
    PRT_UINT32 arity;
    PRT_TYPE *fieldTypes;
    PRT_TYPE_NODE *fieldTypesSerialized;
    } ;
typedef struct PRT_GUID
    {
    PRT_UINT32 data1;
    PRT_UINT16 data2;
    PRT_UINT16 data3;
    PRT_UINT64 data4;
    } 	PRT_GUID;

typedef void *( *PRT_FORGN_CLONE )( 
    PRT_GUID typeTag,
    void *frgnVal);

typedef void ( *PRT_FORGN_FREE )( 
    PRT_GUID typeTag,
    void *frgnVal);

typedef PRT_UINT32 ( *PRT_FORGN_GETHASHCODE )( 
    PRT_GUID typeTag,
    void *frgnVal);

typedef PRT_BOOLEAN ( *PRT_FORGN_ISEQUAL )( 
    PRT_GUID typeTag1,
    void *frgnVal1,
    PRT_GUID typeTag2,
    void *frgnVal2);

struct _PRT_FORGNTYPE
    {
    PRT_GUID typeTag;
    } ;
typedef struct _PRT_VALUE PRT_VALUE;

typedef struct _PRT_FORGNVALUE PRT_FORGNVALUE;

typedef struct _PRT_TUPVALUE PRT_TUPVALUE;

typedef struct _PRT_SEQVALUE PRT_SEQVALUE;

typedef struct _PRT_MAPVALUE PRT_MAPVALUE;

typedef struct _PRT_MAPNODE PRT_MAPNODE;

typedef struct _PRT_VALUE_NODE PRT_VALUE_NODE;

typedef /* [public][public][public][public][public][public] */ struct __MIDL_PrtDistributed_0003
    {
    PRT_TYPE_KIND discriminator;
    /* [switch_is] */ /* [switch_type] */ union __MIDL_PrtDistributed_0004
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
        /* [switch_is] */ /* [switch_type] */ union __MIDL_PrtDistributed_0006
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


