

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Wed Oct 29 11:21:46 2014
 */
/* Compiler settings for PrtDistTypes.idl:
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


#ifndef __PrtDistTypes_h__
#define __PrtDistTypes_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

/* header files for imported files */
#include "PrtDistPrimitiveTypes.h"

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __PrtDistTypes_INTERFACE_DEFINED__
#define __PrtDistTypes_INTERFACE_DEFINED__

/* interface PrtDistTypes */
/* [full][version][uuid] */ 

typedef enum _PRT_TYPE_KIND PRT_TYPE_KIND;

typedef struct _PRT_MAPTYPE PRT_MAPTYPE;

typedef struct _PRT_NMDTUPTYPE PRT_NMDTUPTYPE;

typedef struct _PRT_SEQTYPE PRT_SEQTYPE;

typedef struct _PRT_TUPTYPE PRT_TUPTYPE;

typedef struct _PRT_FORGNTYPE PRT_FORGNTYPE;

typedef struct _PRT_TYPE_NODE PRT_TYPE_NODE;

typedef struct _PRT_STRING_NODE PRT_STRING_NODE;


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
typedef /* [public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public][public] */ struct __MIDL_PrtDistTypes_0001
    {
    PRT_TYPE_KIND typeKind;
    /* [switch_is] */ /* [switch_type] */ union __MIDL_PrtDistTypes_0002
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


extern RPC_IF_HANDLE c_PrtDistTypes_v1_0_c_ifspec;
extern RPC_IF_HANDLE PrtDistTypes_v1_0_c_ifspec;
extern RPC_IF_HANDLE s_PrtDistTypes_v1_0_s_ifspec;
#endif /* __PrtDistTypes_INTERFACE_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


