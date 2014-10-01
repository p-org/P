

/* this ALWAYS GENERATED file contains the RPC server stubs */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Tue Sep 30 22:19:20 2014
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
#include "PrtDService_h.h"

#define TYPE_FORMAT_STRING_SIZE   11                                
#define PROC_FORMAT_STRING_SIZE   75                                
#define EXPR_FORMAT_STRING_SIZE   1                                 
#define TRANSMIT_AS_TABLE_SIZE    0            
#define WIRE_MARSHAL_TABLE_SIZE   0            

typedef struct _PrtDService_MIDL_TYPE_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ TYPE_FORMAT_STRING_SIZE ];
    } PrtDService_MIDL_TYPE_FORMAT_STRING;

typedef struct _PrtDService_MIDL_PROC_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ PROC_FORMAT_STRING_SIZE ];
    } PrtDService_MIDL_PROC_FORMAT_STRING;

typedef struct _PrtDService_MIDL_EXPR_FORMAT_STRING
    {
    long          Pad;
    unsigned char  Format[ EXPR_FORMAT_STRING_SIZE ];
    } PrtDService_MIDL_EXPR_FORMAT_STRING;


static const RPC_SYNTAX_IDENTIFIER  _RpcTransferSyntax = 
{{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}};

extern const PrtDService_MIDL_TYPE_FORMAT_STRING PrtDService__MIDL_TypeFormatString;
extern const PrtDService_MIDL_PROC_FORMAT_STRING PrtDService__MIDL_ProcFormatString;
extern const PrtDService_MIDL_EXPR_FORMAT_STRING PrtDService__MIDL_ExprFormatString;

/* Standard interface: PrtDService, ver. 1.0,
   GUID={0x563ae8a2,0x1f4b,0x4ac0,{0x93,0x31,0x9a,0xe3,0x3f,0x51,0x7a,0x35}} */


extern const MIDL_SERVER_INFO PrtDService_ServerInfo;

extern const RPC_DISPATCH_TABLE PrtDService_v1_0_DispatchTable;

static const RPC_SERVER_INTERFACE PrtDService___RpcServerInterface =
    {
    sizeof(RPC_SERVER_INTERFACE),
    {{0x563ae8a2,0x1f4b,0x4ac0,{0x93,0x31,0x9a,0xe3,0x3f,0x51,0x7a,0x35}},{1,0}},
    {{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}},
    (RPC_DISPATCH_TABLE*)&PrtDService_v1_0_DispatchTable,
    0,
    0,
    0,
    &PrtDService_ServerInfo,
    0x04000000
    };
RPC_IF_HANDLE s_PrtDService_v1_0_s_ifspec = (RPC_IF_HANDLE)& PrtDService___RpcServerInterface;

extern const MIDL_STUB_DESC PrtDService_StubDesc;


#if !defined(__RPC_WIN32__)
#error  Invalid build platform for this stub.
#endif

#if !(TARGET_IS_NT50_OR_LATER)
#error You need Windows 2000 or later to run this stub because it uses these features:
#error   /robust command line switch.
#error However, your C/C++ compilation flags indicate you intend to run this app on earlier systems.
#error This app will fail with the RPC_X_WRONG_STUB_VERSION error.
#endif


static const PrtDService_MIDL_PROC_FORMAT_STRING PrtDService__MIDL_ProcFormatString =
    {
        0,
        {

	/* Procedure PrtDPingService */

			0x0,		/* 0 */
			0x48,		/* Old Flags:  */
/*  2 */	NdrFcLong( 0x0 ),	/* 0 */
/*  6 */	NdrFcShort( 0x0 ),	/* 0 */
/*  8 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 12 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 14 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16 */	NdrFcShort( 0x19 ),	/* 25 */
/* 18 */	0x40,		/* Oi2 Flags:  has ext, */
			0x1,		/* 1 */
/* 20 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */
/* 24 */	NdrFcShort( 0x0 ),	/* 0 */
/* 26 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter myHandle */

/* 28 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 30 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 32 */	0x3,		/* FC_SMALL */
			0x0,		/* 0 */

	/* Procedure PrtDCreateNodeManagerForJob */


	/* Parameter status */

/* 34 */	0x0,		/* 0 */
			0x48,		/* Old Flags:  */
/* 36 */	NdrFcLong( 0x0 ),	/* 0 */
/* 40 */	NdrFcShort( 0x1 ),	/* 1 */
/* 42 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 44 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 46 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 48 */	NdrFcShort( 0x0 ),	/* 0 */
/* 50 */	NdrFcShort( 0x19 ),	/* 25 */
/* 52 */	0x42,		/* Oi2 Flags:  clt must size, has ext, */
			0x2,		/* 2 */
/* 54 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 56 */	NdrFcShort( 0x0 ),	/* 0 */
/* 58 */	NdrFcShort( 0x0 ),	/* 0 */
/* 60 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter myHandle */

/* 62 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 64 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 66 */	NdrFcShort( 0x8 ),	/* Type Offset=8 */

	/* Parameter JobID */

/* 68 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 70 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 72 */	0x3,		/* FC_SMALL */
			0x0,		/* 0 */

			0x0
        }
    };

static const PrtDService_MIDL_TYPE_FORMAT_STRING PrtDService__MIDL_TypeFormatString =
    {
        0,
        {
			NdrFcShort( 0x0 ),	/* 0 */
/*  2 */	
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/*  4 */	0x3,		/* FC_SMALL */
			0x5c,		/* FC_PAD */
/*  6 */	
			0x11, 0x8,	/* FC_RP [simple_pointer] */
/*  8 */	
			0x22,		/* FC_C_CSTRING */
			0x5c,		/* FC_PAD */

			0x0
        }
    };

static const unsigned short PrtDService_FormatStringOffsetTable[] =
    {
    0,
    34
    };


static const MIDL_STUB_DESC PrtDService_StubDesc = 
    {
    (void *)& PrtDService___RpcServerInterface,
    MIDL_user_allocate,
    MIDL_user_free,
    0,
    0,
    0,
    0,
    0,
    PrtDService__MIDL_TypeFormatString.Format,
    1, /* -error bounds_check flag */
    0x50002, /* Ndr library version */
    0,
    0x800025b, /* MIDL Version 8.0.603 */
    0,
    0,
    0,  /* notify & notify_flag routine table */
    0x1, /* MIDL flag */
    0, /* cs routines */
    0,   /* proxy/server info */
    0
    };

static const RPC_DISPATCH_FUNCTION PrtDService_table[] =
    {
    NdrServerCall2,
    NdrServerCall2,
    0
    };
static const RPC_DISPATCH_TABLE PrtDService_v1_0_DispatchTable = 
    {
    2,
    (RPC_DISPATCH_FUNCTION*)PrtDService_table
    };

static const SERVER_ROUTINE PrtDService_ServerRoutineTable[] = 
    {
    (SERVER_ROUTINE)s_PrtDPingService,
    (SERVER_ROUTINE)s_PrtDCreateNodeManagerForJob
    };

static const MIDL_SERVER_INFO PrtDService_ServerInfo = 
    {
    &PrtDService_StubDesc,
    PrtDService_ServerRoutineTable,
    PrtDService__MIDL_ProcFormatString.Format,
    PrtDService_FormatStringOffsetTable,
    0,
    0,
    0,
    0};
#pragma optimize("", on )
#if _MSC_VER >= 1200
#pragma warning(pop)
#endif


#endif /* !defined(_M_IA64) && !defined(_M_AMD64) && !defined(_ARM_) */

