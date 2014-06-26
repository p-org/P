

/* this ALWAYS GENERATED file contains the RPC client stubs */


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

#include "PrtRPC_Types_h.h"

#define TYPE_FORMAT_STRING_SIZE   343                               
#define PROC_FORMAT_STRING_SIZE   137                               
#define EXPR_FORMAT_STRING_SIZE   1                                 
#define TRANSMIT_AS_TABLE_SIZE    0            
#define WIRE_MARSHAL_TABLE_SIZE   0            

typedef struct _PrtRPC_Types_MIDL_TYPE_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ TYPE_FORMAT_STRING_SIZE ];
    } PrtRPC_Types_MIDL_TYPE_FORMAT_STRING;

typedef struct _PrtRPC_Types_MIDL_PROC_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ PROC_FORMAT_STRING_SIZE ];
    } PrtRPC_Types_MIDL_PROC_FORMAT_STRING;

typedef struct _PrtRPC_Types_MIDL_EXPR_FORMAT_STRING
    {
    long          Pad;
    unsigned char  Format[ EXPR_FORMAT_STRING_SIZE ];
    } PrtRPC_Types_MIDL_EXPR_FORMAT_STRING;


static const RPC_SYNTAX_IDENTIFIER  _RpcTransferSyntax = 
{{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}};


extern const PrtRPC_Types_MIDL_TYPE_FORMAT_STRING PrtRPC_Types__MIDL_TypeFormatString;
extern const PrtRPC_Types_MIDL_PROC_FORMAT_STRING PrtRPC_Types__MIDL_ProcFormatString;
extern const PrtRPC_Types_MIDL_EXPR_FORMAT_STRING PrtRPC_Types__MIDL_ExprFormatString;

#define GENERIC_BINDING_TABLE_SIZE   0            


/* Standard interface: PrtRPC, ver. 1.0,
   GUID={0x1397cac6,0x51ca,0x4c50,{0x94,0x84,0x1a,0xfd,0x1c,0x35,0x3d,0x84}} */



static const RPC_CLIENT_INTERFACE PrtRPC___RpcClientInterface =
    {
    sizeof(RPC_CLIENT_INTERFACE),
    {{0x1397cac6,0x51ca,0x4c50,{0x94,0x84,0x1a,0xfd,0x1c,0x35,0x3d,0x84}},{1,0}},
    {{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}},
    0,
    0,
    0,
    0,
    0,
    0x00000000
    };
RPC_IF_HANDLE c_PrtRPC_v1_0_c_ifspec = (RPC_IF_HANDLE)& PrtRPC___RpcClientInterface;

extern const MIDL_STUB_DESC PrtRPC_StubDesc;

static RPC_BINDING_HANDLE PrtRPC__MIDL_AutoBindHandle;


void c_SendValue1( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_PRIMVALUE *value)
{

    NdrClientCall2(
                  ( PMIDL_STUB_DESC  )&PrtRPC_StubDesc,
                  (PFORMAT_STRING) &PrtRPC_Types__MIDL_ProcFormatString.Format[0],
                  ( unsigned char * )&handleM);
    
}


void c_SendValue2( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_PRIMVALUE *value)
{

    NdrClientCall2(
                  ( PMIDL_STUB_DESC  )&PrtRPC_StubDesc,
                  (PFORMAT_STRING) &PrtRPC_Types__MIDL_ProcFormatString.Format[34],
                  ( unsigned char * )&handleM);
    
}


void c_SendValueMap1( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_MAPVALUE *value)
{

    NdrClientCall2(
                  ( PMIDL_STUB_DESC  )&PrtRPC_StubDesc,
                  (PFORMAT_STRING) &PrtRPC_Types__MIDL_ProcFormatString.Format[68],
                  ( unsigned char * )&handleM);
    
}


void c_SendValueMap2( 
    /* [in] */ handle_t handleM,
    /* [in] */ PRT_MAPVALUE *value)
{

    NdrClientCall2(
                  ( PMIDL_STUB_DESC  )&PrtRPC_StubDesc,
                  (PFORMAT_STRING) &PrtRPC_Types__MIDL_ProcFormatString.Format[102],
                  ( unsigned char * )&handleM);
    
}


#if !defined(__RPC_WIN32__)
#error  Invalid build platform for this stub.
#endif

#if !(TARGET_IS_NT50_OR_LATER)
#error You need Windows 2000 or later to run this stub because it uses these features:
#error   /robust command line switch.
#error However, your C/C++ compilation flags indicate you intend to run this app on earlier systems.
#error This app will fail with the RPC_X_WRONG_STUB_VERSION error.
#endif


static const PrtRPC_Types_MIDL_PROC_FORMAT_STRING PrtRPC_Types__MIDL_ProcFormatString =
    {
        0,
        {

	/* Procedure SendValue1 */

			0x0,		/* 0 */
			0x49,		/* Old Flags:  full ptr, */
/*  2 */	NdrFcLong( 0x0 ),	/* 0 */
/*  6 */	NdrFcShort( 0x0 ),	/* 0 */
/*  8 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 12 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 14 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18 */	0x42,		/* Oi2 Flags:  clt must size, has ext, */
			0x1,		/* 1 */
/* 20 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */
/* 24 */	NdrFcShort( 0x1 ),	/* 1 */
/* 26 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter handleM */

/* 28 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 30 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 32 */	NdrFcShort( 0xde ),	/* Type Offset=222 */

	/* Procedure SendValue2 */


	/* Parameter value */

/* 34 */	0x0,		/* 0 */
			0x49,		/* Old Flags:  full ptr, */
/* 36 */	NdrFcLong( 0x0 ),	/* 0 */
/* 40 */	NdrFcShort( 0x1 ),	/* 1 */
/* 42 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 44 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 46 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 48 */	NdrFcShort( 0x0 ),	/* 0 */
/* 50 */	NdrFcShort( 0x0 ),	/* 0 */
/* 52 */	0x42,		/* Oi2 Flags:  clt must size, has ext, */
			0x1,		/* 1 */
/* 54 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 56 */	NdrFcShort( 0x0 ),	/* 0 */
/* 58 */	NdrFcShort( 0x1 ),	/* 1 */
/* 60 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter handleM */

/* 62 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 64 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 66 */	NdrFcShort( 0xde ),	/* Type Offset=222 */

	/* Procedure SendValueMap1 */


	/* Parameter value */

/* 68 */	0x0,		/* 0 */
			0x49,		/* Old Flags:  full ptr, */
/* 70 */	NdrFcLong( 0x0 ),	/* 0 */
/* 74 */	NdrFcShort( 0x2 ),	/* 2 */
/* 76 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 78 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 80 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 82 */	NdrFcShort( 0x0 ),	/* 0 */
/* 84 */	NdrFcShort( 0x0 ),	/* 0 */
/* 86 */	0x42,		/* Oi2 Flags:  clt must size, has ext, */
			0x1,		/* 1 */
/* 88 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 90 */	NdrFcShort( 0x0 ),	/* 0 */
/* 92 */	NdrFcShort( 0x1 ),	/* 1 */
/* 94 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter handleM */

/* 96 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 98 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 100 */	NdrFcShort( 0x138 ),	/* Type Offset=312 */

	/* Procedure SendValueMap2 */


	/* Parameter value */

/* 102 */	0x0,		/* 0 */
			0x49,		/* Old Flags:  full ptr, */
/* 104 */	NdrFcLong( 0x0 ),	/* 0 */
/* 108 */	NdrFcShort( 0x3 ),	/* 3 */
/* 110 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 112 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 114 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 116 */	NdrFcShort( 0x0 ),	/* 0 */
/* 118 */	NdrFcShort( 0x0 ),	/* 0 */
/* 120 */	0x42,		/* Oi2 Flags:  clt must size, has ext, */
			0x1,		/* 1 */
/* 122 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 124 */	NdrFcShort( 0x0 ),	/* 0 */
/* 126 */	NdrFcShort( 0x1 ),	/* 1 */
/* 128 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter handleM */

/* 130 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 132 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 134 */	NdrFcShort( 0x138 ),	/* Type Offset=312 */

			0x0
        }
    };

static const PrtRPC_Types_MIDL_TYPE_FORMAT_STRING PrtRPC_Types__MIDL_TypeFormatString =
    {
        0,
        {
			NdrFcShort( 0x0 ),	/* 0 */
/*  2 */	
			0x11, 0x0,	/* FC_RP */
/*  4 */	NdrFcShort( 0xda ),	/* Offset= 218 (222) */
/*  6 */	
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x4d,		/* 77 */
/*  8 */	NdrFcShort( 0x4 ),	/* 4 */
/* 10 */	NdrFcShort( 0xb ),	/* 11 */
/* 12 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16 */	NdrFcShort( 0x40 ),	/* Offset= 64 (80) */
/* 18 */	NdrFcLong( 0x1 ),	/* 1 */
/* 22 */	NdrFcShort( 0x3a ),	/* Offset= 58 (80) */
/* 24 */	NdrFcLong( 0x2 ),	/* 2 */
/* 28 */	NdrFcShort( 0x34 ),	/* Offset= 52 (80) */
/* 30 */	NdrFcLong( 0x3 ),	/* 3 */
/* 34 */	NdrFcShort( 0x2e ),	/* Offset= 46 (80) */
/* 36 */	NdrFcLong( 0x4 ),	/* 4 */
/* 40 */	NdrFcShort( 0x28 ),	/* Offset= 40 (80) */
/* 42 */	NdrFcLong( 0x5 ),	/* 5 */
/* 46 */	NdrFcShort( 0x22 ),	/* Offset= 34 (80) */
/* 48 */	NdrFcLong( 0x6 ),	/* 6 */
/* 52 */	NdrFcShort( 0x1c ),	/* Offset= 28 (80) */
/* 54 */	NdrFcLong( 0x7 ),	/* 7 */
/* 58 */	NdrFcShort( 0x2c ),	/* Offset= 44 (102) */
/* 60 */	NdrFcLong( 0x8 ),	/* 8 */
/* 64 */	NdrFcShort( 0x26 ),	/* Offset= 38 (102) */
/* 66 */	NdrFcLong( 0x9 ),	/* 9 */
/* 70 */	NdrFcShort( 0x48 ),	/* Offset= 72 (142) */
/* 72 */	NdrFcLong( 0xa ),	/* 10 */
/* 76 */	NdrFcShort( 0x54 ),	/* Offset= 84 (160) */
/* 78 */	NdrFcShort( 0xffff ),	/* Offset= -1 (77) */
/* 80 */	
			0x14, 0x0,	/* FC_FP */
/* 82 */	NdrFcShort( 0x2 ),	/* Offset= 2 (84) */
/* 84 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x0,		/* 0 */
/* 86 */	NdrFcShort( 0x0 ),	/* 0 */
/* 88 */	NdrFcShort( 0x0 ),	/* 0 */
/* 90 */	NdrFcShort( 0x0 ),	/* Offset= 0 (90) */
/* 92 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 94 */	NdrFcShort( 0xffa8 ),	/* Offset= -88 (6) */
/* 96 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 98 */	NdrFcShort( 0xffa4 ),	/* Offset= -92 (6) */
/* 100 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 102 */	
			0x14, 0x0,	/* FC_FP */
/* 104 */	NdrFcShort( 0x6 ),	/* Offset= 6 (110) */
/* 106 */	
			0x14, 0x8,	/* FC_FP [simple_pointer] */
/* 108 */	
			0x22,		/* FC_C_CSTRING */
			0x5c,		/* FC_PAD */
/* 110 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 112 */	NdrFcShort( 0xc ),	/* 12 */
/* 114 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 116 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 118 */	NdrFcShort( 0x4 ),	/* 4 */
/* 120 */	NdrFcShort( 0x4 ),	/* 4 */
/* 122 */	0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 124 */	NdrFcShort( 0xffee ),	/* Offset= -18 (106) */
/* 126 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 128 */	NdrFcShort( 0x8 ),	/* 8 */
/* 130 */	NdrFcShort( 0x8 ),	/* 8 */
/* 132 */	0x14, 0x0,	/* FC_FP */
/* 134 */	NdrFcShort( 0xff80 ),	/* Offset= -128 (6) */
/* 136 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 138 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 140 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 142 */	
			0x14, 0x0,	/* FC_FP */
/* 144 */	NdrFcShort( 0x2 ),	/* Offset= 2 (146) */
/* 146 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x0,		/* 0 */
/* 148 */	NdrFcShort( 0x0 ),	/* 0 */
/* 150 */	NdrFcShort( 0x0 ),	/* 0 */
/* 152 */	NdrFcShort( 0x0 ),	/* Offset= 0 (152) */
/* 154 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 156 */	NdrFcShort( 0xff6a ),	/* Offset= -150 (6) */
/* 158 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 160 */	
			0x14, 0x0,	/* FC_FP */
/* 162 */	NdrFcShort( 0x2 ),	/* Offset= 2 (164) */
/* 164 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 166 */	NdrFcShort( 0x8 ),	/* 8 */
/* 168 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 170 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 172 */	NdrFcShort( 0x4 ),	/* 4 */
/* 174 */	NdrFcShort( 0x4 ),	/* 4 */
/* 176 */	0x14, 0x0,	/* FC_FP */
/* 178 */	NdrFcShort( 0xff54 ),	/* Offset= -172 (6) */
/* 180 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 182 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 184 */	
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x8d,		/* 141 */
/* 186 */	NdrFcShort( 0x8 ),	/* 8 */
/* 188 */	NdrFcShort( 0x5 ),	/* 5 */
/* 190 */	NdrFcLong( 0x0 ),	/* 0 */
/* 194 */	NdrFcShort( 0x8003 ),	/* Simple arm type: FC_SMALL */
/* 196 */	NdrFcLong( 0x1 ),	/* 1 */
/* 200 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 202 */	NdrFcLong( 0x2 ),	/* 2 */
/* 206 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 208 */	NdrFcLong( 0x3 ),	/* 3 */
/* 212 */	NdrFcShort( 0x800b ),	/* Simple arm type: FC_HYPER */
/* 214 */	NdrFcLong( 0x4 ),	/* 4 */
/* 218 */	NdrFcShort( 0x800b ),	/* Simple arm type: FC_HYPER */
/* 220 */	NdrFcShort( 0xffff ),	/* Offset= -1 (219) */
/* 222 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x7,		/* 7 */
/* 224 */	NdrFcShort( 0x18 ),	/* 24 */
/* 226 */	NdrFcShort( 0x0 ),	/* 0 */
/* 228 */	NdrFcShort( 0x0 ),	/* Offset= 0 (228) */
/* 230 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 232 */	NdrFcShort( 0xff1e ),	/* Offset= -226 (6) */
/* 234 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 236 */	NdrFcShort( 0xffcc ),	/* Offset= -52 (184) */
/* 238 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 240 */	
			0x11, 0x0,	/* FC_RP */
/* 242 */	NdrFcShort( 0x46 ),	/* Offset= 70 (312) */
/* 244 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 246 */	NdrFcShort( 0x14 ),	/* 20 */
/* 248 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 250 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 252 */	NdrFcShort( 0x0 ),	/* 0 */
/* 254 */	NdrFcShort( 0x0 ),	/* 0 */
/* 256 */	0x14, 0x0,	/* FC_FP */
/* 258 */	NdrFcShort( 0xff04 ),	/* Offset= -252 (6) */
/* 260 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 262 */	NdrFcShort( 0x4 ),	/* 4 */
/* 264 */	NdrFcShort( 0x4 ),	/* 4 */
/* 266 */	0x14, 0x0,	/* FC_FP */
/* 268 */	NdrFcShort( 0xfefa ),	/* Offset= -262 (6) */
/* 270 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 272 */	NdrFcShort( 0x8 ),	/* 8 */
/* 274 */	NdrFcShort( 0x8 ),	/* 8 */
/* 276 */	0x14, 0x0,	/* FC_FP */
/* 278 */	NdrFcShort( 0xffde ),	/* Offset= -34 (244) */
/* 280 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 282 */	NdrFcShort( 0xc ),	/* 12 */
/* 284 */	NdrFcShort( 0xc ),	/* 12 */
/* 286 */	0x14, 0x0,	/* FC_FP */
/* 288 */	NdrFcShort( 0xffd4 ),	/* Offset= -44 (244) */
/* 290 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 292 */	NdrFcShort( 0x10 ),	/* 16 */
/* 294 */	NdrFcShort( 0x10 ),	/* 16 */
/* 296 */	0x14, 0x0,	/* FC_FP */
/* 298 */	NdrFcShort( 0xffca ),	/* Offset= -54 (244) */
/* 300 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 302 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 304 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 306 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 308 */	
			0x14, 0x0,	/* FC_FP */
/* 310 */	NdrFcShort( 0xffbe ),	/* Offset= -66 (244) */
/* 312 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 314 */	NdrFcShort( 0x1c ),	/* 28 */
/* 316 */	NdrFcShort( 0x0 ),	/* 0 */
/* 318 */	NdrFcShort( 0xc ),	/* Offset= 12 (330) */
/* 320 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 322 */	NdrFcShort( 0xfec4 ),	/* Offset= -316 (6) */
/* 324 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 326 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 328 */	0x36,		/* FC_POINTER */
			0x5b,		/* FC_END */
/* 330 */	
			0x14, 0x0,	/* FC_FP */
/* 332 */	NdrFcShort( 0xffa8 ),	/* Offset= -88 (244) */
/* 334 */	
			0x14, 0x0,	/* FC_FP */
/* 336 */	NdrFcShort( 0xffa4 ),	/* Offset= -92 (244) */
/* 338 */	
			0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 340 */	NdrFcShort( 0xffe0 ),	/* Offset= -32 (308) */

			0x0
        }
    };

static const unsigned short PrtRPC_FormatStringOffsetTable[] =
    {
    0,
    34,
    68,
    102
    };


static const MIDL_STUB_DESC PrtRPC_StubDesc = 
    {
    (void *)& PrtRPC___RpcClientInterface,
    MIDL_user_allocate,
    MIDL_user_free,
    &PrtRPC__MIDL_AutoBindHandle,
    0,
    0,
    0,
    0,
    PrtRPC_Types__MIDL_TypeFormatString.Format,
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
#pragma optimize("", on )
#if _MSC_VER >= 1200
#pragma warning(pop)
#endif


#endif /* !defined(_M_IA64) && !defined(_M_AMD64) && !defined(_ARM_) */

