

/* this ALWAYS GENERATED file contains the RPC server stubs */


 /* File created by MIDL compiler version 8.00.0595 */
/* at Wed Apr 02 17:29:29 2014
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
#include "StartMachine.h"

#define TYPE_FORMAT_STRING_SIZE   3                                 
#define PROC_FORMAT_STRING_SIZE   125                               
#define EXPR_FORMAT_STRING_SIZE   1                                 
#define TRANSMIT_AS_TABLE_SIZE    0            
#define WIRE_MARSHAL_TABLE_SIZE   0            

typedef struct _StartMachine_MIDL_TYPE_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ TYPE_FORMAT_STRING_SIZE ];
    } StartMachine_MIDL_TYPE_FORMAT_STRING;

typedef struct _StartMachine_MIDL_PROC_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ PROC_FORMAT_STRING_SIZE ];
    } StartMachine_MIDL_PROC_FORMAT_STRING;

typedef struct _StartMachine_MIDL_EXPR_FORMAT_STRING
    {
    long          Pad;
    unsigned char  Format[ EXPR_FORMAT_STRING_SIZE ];
    } StartMachine_MIDL_EXPR_FORMAT_STRING;


static const RPC_SYNTAX_IDENTIFIER  _RpcTransferSyntax = 
{{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}};

extern const StartMachine_MIDL_TYPE_FORMAT_STRING StartMachine__MIDL_TypeFormatString;
extern const StartMachine_MIDL_PROC_FORMAT_STRING StartMachine__MIDL_ProcFormatString;
extern const StartMachine_MIDL_EXPR_FORMAT_STRING StartMachine__MIDL_ExprFormatString;

/* Standard interface: StartMachine, ver. 1.0,
   GUID={0x1de89548,0xcb84,0x450f,{0xb9,0x29,0x40,0x77,0x93,0x16,0xe7,0x15}} */


extern const MIDL_SERVER_INFO StartMachine_ServerInfo;

extern const RPC_DISPATCH_TABLE StartMachine_v1_0_DispatchTable;

static const RPC_SERVER_INTERFACE StartMachine___RpcServerInterface =
    {
    sizeof(RPC_SERVER_INTERFACE),
    {{0x1de89548,0xcb84,0x450f,{0xb9,0x29,0x40,0x77,0x93,0x16,0xe7,0x15}},{1,0}},
    {{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}},
    (RPC_DISPATCH_TABLE*)&StartMachine_v1_0_DispatchTable,
    0,
    0,
    0,
    &StartMachine_ServerInfo,
    0x04000000
    };
RPC_IF_HANDLE StartMachine_v1_0_s_ifspec = (RPC_IF_HANDLE)& StartMachine___RpcServerInterface;

extern const MIDL_STUB_DESC StartMachine_StubDesc;


#if !defined(__RPC_WIN32__)
#error  Invalid build platform for this stub.
#endif

#if !(TARGET_IS_NT50_OR_LATER)
#error You need Windows 2000 or later to run this stub because it uses these features:
#error   /robust command line switch.
#error However, your C/C++ compilation flags indicate you intend to run this app on earlier systems.
#error This app will fail with the RPC_X_WRONG_STUB_VERSION error.
#endif


static const StartMachine_MIDL_PROC_FORMAT_STRING StartMachine__MIDL_ProcFormatString =
    {
        0,
        {

	/* Procedure WrapSmfNew */

			0x0,		/* 0 */
			0x48,		/* Old Flags:  */
/*  2 */	NdrFcLong( 0x0 ),	/* 0 */
/*  6 */	NdrFcShort( 0x0 ),	/* 0 */
/*  8 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 12 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 14 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18 */	0x40,		/* Oi2 Flags:  has ext, */
			0x1,		/* 1 */
/* 20 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */
/* 24 */	NdrFcShort( 0x0 ),	/* 0 */
/* 26 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter startMachine_IfHandle */

/* 28 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 30 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 32 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WrapSmfEnqueueEvent */


	/* Parameter InstanceOf */

/* 34 */	0x0,		/* 0 */
			0x48,		/* Old Flags:  */
/* 36 */	NdrFcLong( 0x0 ),	/* 0 */
/* 40 */	NdrFcShort( 0x1 ),	/* 1 */
/* 42 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 44 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 46 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 48 */	NdrFcShort( 0x8 ),	/* 8 */
/* 50 */	NdrFcShort( 0x0 ),	/* 0 */
/* 52 */	0x40,		/* Oi2 Flags:  has ext, */
			0x1,		/* 1 */
/* 54 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 56 */	NdrFcShort( 0x0 ),	/* 0 */
/* 58 */	NdrFcShort( 0x0 ),	/* 0 */
/* 60 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter startMachine_IfHandle */

/* 62 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 64 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 66 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ChildUp */


	/* Parameter EventIndex */

/* 68 */	0x0,		/* 0 */
			0x48,		/* Old Flags:  */
/* 70 */	NdrFcLong( 0x0 ),	/* 0 */
/* 74 */	NdrFcShort( 0x2 ),	/* 2 */
/* 76 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 78 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 80 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 82 */	NdrFcShort( 0x0 ),	/* 0 */
/* 84 */	NdrFcShort( 0x0 ),	/* 0 */
/* 86 */	0x40,		/* Oi2 Flags:  has ext, */
			0x0,		/* 0 */
/* 88 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 90 */	NdrFcShort( 0x0 ),	/* 0 */
/* 92 */	NdrFcShort( 0x0 ),	/* 0 */
/* 94 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Procedure MachineShutdown */


	/* Parameter startMachine_IfHandle */

/* 96 */	0x0,		/* 0 */
			0x48,		/* Old Flags:  */
/* 98 */	NdrFcLong( 0x0 ),	/* 0 */
/* 102 */	NdrFcShort( 0x3 ),	/* 3 */
/* 104 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 106 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 108 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 110 */	NdrFcShort( 0x0 ),	/* 0 */
/* 112 */	NdrFcShort( 0x0 ),	/* 0 */
/* 114 */	0x40,		/* Oi2 Flags:  has ext, */
			0x0,		/* 0 */
/* 116 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 118 */	NdrFcShort( 0x0 ),	/* 0 */
/* 120 */	NdrFcShort( 0x0 ),	/* 0 */
/* 122 */	NdrFcShort( 0x0 ),	/* 0 */

			0x0
        }
    };

static const StartMachine_MIDL_TYPE_FORMAT_STRING StartMachine__MIDL_TypeFormatString =
    {
        0,
        {
			NdrFcShort( 0x0 ),	/* 0 */

			0x0
        }
    };

static const unsigned short StartMachine_FormatStringOffsetTable[] =
    {
    0,
    34,
    68,
    96
    };


static const MIDL_STUB_DESC StartMachine_StubDesc = 
    {
    (void *)& StartMachine___RpcServerInterface,
    MIDL_user_allocate,
    MIDL_user_free,
    0,
    0,
    0,
    0,
    0,
    StartMachine__MIDL_TypeFormatString.Format,
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

static const RPC_DISPATCH_FUNCTION StartMachine_table[] =
    {
    NdrServerCall2,
    NdrServerCall2,
    NdrServerCall2,
    NdrServerCall2,
    0
    };
static const RPC_DISPATCH_TABLE StartMachine_v1_0_DispatchTable = 
    {
    4,
    (RPC_DISPATCH_FUNCTION*)StartMachine_table
    };

static const SERVER_ROUTINE StartMachine_ServerRoutineTable[] = 
    {
    (SERVER_ROUTINE)WrapSmfNew,
    (SERVER_ROUTINE)WrapSmfEnqueueEvent,
    (SERVER_ROUTINE)ChildUp,
    (SERVER_ROUTINE)MachineShutdown
    };

static const MIDL_SERVER_INFO StartMachine_ServerInfo = 
    {
    &StartMachine_StubDesc,
    StartMachine_ServerRoutineTable,
    StartMachine__MIDL_ProcFormatString.Format,
    StartMachine_FormatStringOffsetTable,
    0,
    0,
    0,
    0};
#pragma optimize("", on )
#if _MSC_VER >= 1200
#pragma warning(pop)
#endif


#endif /* !defined(_M_IA64) && !defined(_M_AMD64) && !defined(_ARM_) */

