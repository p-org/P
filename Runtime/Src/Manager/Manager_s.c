

/* this ALWAYS GENERATED file contains the RPC server stubs */


 /* File created by MIDL compiler version 8.00.0595 */
/* at Sat Mar 29 21:39:26 2014
 */
/* Compiler settings for Manager.idl:
    Oicf, W1, Zp8, env=Win32 (32b run), target_arch=X86 8.00.0595 
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */

#if !defined(_M_IA64) && !defined(_M_AMD64) && !defined(_ARM_)


#pragma warning( disable: 4049 )  /* more than 64k source lines */
#if _MSC_VER >= 1200
#pragma warning(push)
#endif

#pragma warning( disable: 4211 )  /* redefine extern to static */
#pragma warning( disable: 4232 )  /* dllimport identity*/
#pragma warning( disable: 4024 )  /* array to pointer mapping*/
#pragma warning( disable: 4100 ) /* unreferenced arguments in x86 call */

#pragma optimize("", off ) 

#include <string.h>
#include "Manager.h"

#define TYPE_FORMAT_STRING_SIZE   7                                 
#define PROC_FORMAT_STRING_SIZE   55                                
#define EXPR_FORMAT_STRING_SIZE   1                                 
#define TRANSMIT_AS_TABLE_SIZE    0            
#define WIRE_MARSHAL_TABLE_SIZE   0            

typedef struct _Manager_MIDL_TYPE_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ TYPE_FORMAT_STRING_SIZE ];
    } Manager_MIDL_TYPE_FORMAT_STRING;

typedef struct _Manager_MIDL_PROC_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ PROC_FORMAT_STRING_SIZE ];
    } Manager_MIDL_PROC_FORMAT_STRING;

typedef struct _Manager_MIDL_EXPR_FORMAT_STRING
    {
    long          Pad;
    unsigned char  Format[ EXPR_FORMAT_STRING_SIZE ];
    } Manager_MIDL_EXPR_FORMAT_STRING;


static const RPC_SYNTAX_IDENTIFIER  _RpcTransferSyntax = 
{{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}};

extern const Manager_MIDL_TYPE_FORMAT_STRING Manager__MIDL_TypeFormatString;
extern const Manager_MIDL_PROC_FORMAT_STRING Manager__MIDL_ProcFormatString;
extern const Manager_MIDL_EXPR_FORMAT_STRING Manager__MIDL_ExprFormatString;

/* Standard interface: Manager, ver. 1.0,
   GUID={0xec966b88,0xc0b1,0x4d9e,{0xa7,0x64,0xc1,0x4d,0x49,0x43,0x45,0xc8}} */


extern const MIDL_SERVER_INFO Manager_ServerInfo;

extern const RPC_DISPATCH_TABLE Manager_v1_0_DispatchTable;

static const RPC_SERVER_INTERFACE Manager___RpcServerInterface =
    {
    sizeof(RPC_SERVER_INTERFACE),
    {{0xec966b88,0xc0b1,0x4d9e,{0xa7,0x64,0xc1,0x4d,0x49,0x43,0x45,0xc8}},{1,0}},
    {{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}},
    (RPC_DISPATCH_TABLE*)&Manager_v1_0_DispatchTable,
    0,
    0,
    0,
    &Manager_ServerInfo,
    0x04000000
    };
RPC_IF_HANDLE Manager_v1_0_s_ifspec = (RPC_IF_HANDLE)& Manager___RpcServerInterface;

extern const MIDL_STUB_DESC Manager_StubDesc;


#if !defined(__RPC_WIN32__)
#error  Invalid build platform for this stub.
#endif

#if !(TARGET_IS_NT50_OR_LATER)
#error You need Windows 2000 or later to run this stub because it uses these features:
#error   /robust command line switch.
#error However, your C/C++ compilation flags indicate you intend to run this app on earlier systems.
#error This app will fail with the RPC_X_WRONG_STUB_VERSION error.
#endif


static const Manager_MIDL_PROC_FORMAT_STRING Manager__MIDL_ProcFormatString =
    {
        0,
        {

	/* Procedure NewMachineId */

			0x32,		/* FC_BIND_PRIMITIVE */
			0x48,		/* Old Flags:  */
/*  2 */	NdrFcLong( 0x0 ),	/* 0 */
/*  6 */	NdrFcShort( 0x0 ),	/* 0 */
/*  8 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12 */	NdrFcShort( 0x19 ),	/* 25 */
/* 14 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 16 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 24 */	NdrFcShort( 0x32 ),	/* Flags:  must free, out, return, */
/* 26 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 28 */	NdrFcShort( 0x2 ),	/* Type Offset=2 */

	/* Procedure Shutdown */

/* 30 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x48,		/* Old Flags:  */
/* 32 */	NdrFcLong( 0x0 ),	/* 0 */
/* 36 */	NdrFcShort( 0x1 ),	/* 1 */
/* 38 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 40 */	NdrFcShort( 0x0 ),	/* 0 */
/* 42 */	NdrFcShort( 0x0 ),	/* 0 */
/* 44 */	0x40,		/* Oi2 Flags:  has ext, */
			0x0,		/* 0 */
/* 46 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 48 */	NdrFcShort( 0x0 ),	/* 0 */
/* 50 */	NdrFcShort( 0x0 ),	/* 0 */
/* 52 */	NdrFcShort( 0x0 ),	/* 0 */

			0x0
        }
    };

static const Manager_MIDL_TYPE_FORMAT_STRING Manager__MIDL_TypeFormatString =
    {
        0,
        {
			NdrFcShort( 0x0 ),	/* 0 */
/*  2 */	
			0x12, 0x8,	/* FC_UP [simple_pointer] */
/*  4 */	0x2,		/* FC_CHAR */
			0x5c,		/* FC_PAD */

			0x0
        }
    };

static const unsigned short Manager_FormatStringOffsetTable[] =
    {
    0,
    30
    };


static const MIDL_STUB_DESC Manager_StubDesc = 
    {
    (void *)& Manager___RpcServerInterface,
    MIDL_user_allocate,
    MIDL_user_free,
    0,
    0,
    0,
    0,
    0,
    Manager__MIDL_TypeFormatString.Format,
    1, /* -error bounds_check flag */
    0x50002, /* Ndr library version */
    0,
    0x8000253, /* MIDL Version 8.0.595 */
    0,
    0,
    0,  /* notify & notify_flag routine table */
    0x1, /* MIDL flag */
    0, /* cs routines */
    0,   /* proxy/server info */
    0
    };

static const RPC_DISPATCH_FUNCTION Manager_table[] =
    {
    NdrServerCall2,
    NdrServerCall2,
    0
    };
static const RPC_DISPATCH_TABLE Manager_v1_0_DispatchTable = 
    {
    2,
    (RPC_DISPATCH_FUNCTION*)Manager_table
    };

static const SERVER_ROUTINE Manager_ServerRoutineTable[] = 
    {
    (SERVER_ROUTINE)NewMachineId,
    (SERVER_ROUTINE)Shutdown
    };

static const MIDL_SERVER_INFO Manager_ServerInfo = 
    {
    &Manager_StubDesc,
    Manager_ServerRoutineTable,
    Manager__MIDL_ProcFormatString.Format,
    Manager_FormatStringOffsetTable,
    0,
    0,
    0,
    0};
#pragma optimize("", on )
#if _MSC_VER >= 1200
#pragma warning(pop)
#endif


#endif /* !defined(_M_IA64) && !defined(_M_AMD64) && !defined(_ARM_) */

