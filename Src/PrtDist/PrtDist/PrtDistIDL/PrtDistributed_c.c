

/* this ALWAYS GENERATED file contains the RPC client stubs */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Wed Oct 29 11:21:46 2014
 */
/* Compiler settings for PrtDistributed.idl:
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

#include "PrtDistributed.h"

#define TYPE_FORMAT_STRING_SIZE   643                               
#define PROC_FORMAT_STRING_SIZE   69                                
#define EXPR_FORMAT_STRING_SIZE   1                                 
#define TRANSMIT_AS_TABLE_SIZE    0            
#define WIRE_MARSHAL_TABLE_SIZE   0            

typedef struct _PrtDistributed_MIDL_TYPE_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ TYPE_FORMAT_STRING_SIZE ];
    } PrtDistributed_MIDL_TYPE_FORMAT_STRING;

typedef struct _PrtDistributed_MIDL_PROC_FORMAT_STRING
    {
    short          Pad;
    unsigned char  Format[ PROC_FORMAT_STRING_SIZE ];
    } PrtDistributed_MIDL_PROC_FORMAT_STRING;

typedef struct _PrtDistributed_MIDL_EXPR_FORMAT_STRING
    {
    long          Pad;
    unsigned char  Format[ EXPR_FORMAT_STRING_SIZE ];
    } PrtDistributed_MIDL_EXPR_FORMAT_STRING;


static const RPC_SYNTAX_IDENTIFIER  _RpcTransferSyntax = 
{{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}};


extern const PrtDistributed_MIDL_TYPE_FORMAT_STRING PrtDistributed__MIDL_TypeFormatString;
extern const PrtDistributed_MIDL_PROC_FORMAT_STRING PrtDistributed__MIDL_ProcFormatString;
extern const PrtDistributed_MIDL_EXPR_FORMAT_STRING PrtDistributed__MIDL_ExprFormatString;

#define GENERIC_BINDING_TABLE_SIZE   0            


/* Standard interface: PrtDistributed, ver. 1.0,
   GUID={0x6f15af3b,0x0784,0x4fe4,{0xa5,0x12,0x02,0xe9,0x00,0x6c,0xf6,0x60}} */



static const RPC_CLIENT_INTERFACE PrtDistributed___RpcClientInterface =
    {
    sizeof(RPC_CLIENT_INTERFACE),
    {{0x6f15af3b,0x0784,0x4fe4,{0xa5,0x12,0x02,0xe9,0x00,0x6c,0xf6,0x60}},{1,0}},
    {{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}},
    0,
    0,
    0,
    0,
    0,
    0x00000000
    };
RPC_IF_HANDLE c_PrtDistributed_v1_0_c_ifspec = (RPC_IF_HANDLE)& PrtDistributed___RpcClientInterface;

extern const MIDL_STUB_DESC PrtDistributed_StubDesc;

static RPC_BINDING_HANDLE PrtDistributed__MIDL_AutoBindHandle;


void c_SendValue1( 
    /* [in] */ handle_t handleM,
    /* [full][in] */ PRT_VALUE *value)
{

    NdrClientCall2(
                  ( PMIDL_STUB_DESC  )&PrtDistributed_StubDesc,
                  (PFORMAT_STRING) &PrtDistributed__MIDL_ProcFormatString.Format[0],
                  ( unsigned char * )&handleM);
    
}


void c_SendValue2( 
    /* [in] */ handle_t handleM,
    /* [full][in] */ PRT_VALUE *value)
{

    NdrClientCall2(
                  ( PMIDL_STUB_DESC  )&PrtDistributed_StubDesc,
                  (PFORMAT_STRING) &PrtDistributed__MIDL_ProcFormatString.Format[34],
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


static const PrtDistributed_MIDL_PROC_FORMAT_STRING PrtDistributed__MIDL_ProcFormatString =
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

/* 28 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 30 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 32 */	NdrFcShort( 0x2 ),	/* Type Offset=2 */

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

/* 62 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 64 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 66 */	NdrFcShort( 0x2 ),	/* Type Offset=2 */

			0x0
        }
    };

static const PrtDistributed_MIDL_TYPE_FORMAT_STRING PrtDistributed__MIDL_TypeFormatString =
    {
        0,
        {
			NdrFcShort( 0x0 ),	/* 0 */
/*  2 */	
			0x14, 0x0,	/* FC_FP */
/*  4 */	NdrFcShort( 0x26c ),	/* Offset= 620 (624) */
/*  6 */	
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x4d,		/* 77 */
/*  8 */	NdrFcShort( 0x4 ),	/* 4 */
/* 10 */	NdrFcShort( 0xc ),	/* 12 */
/* 12 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 18 */	NdrFcLong( 0x1 ),	/* 1 */
/* 22 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 24 */	NdrFcLong( 0x2 ),	/* 2 */
/* 28 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 30 */	NdrFcLong( 0x3 ),	/* 3 */
/* 34 */	NdrFcShort( 0x34 ),	/* Offset= 52 (86) */
/* 36 */	NdrFcLong( 0x4 ),	/* 4 */
/* 40 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 42 */	NdrFcLong( 0x5 ),	/* 5 */
/* 46 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 48 */	NdrFcLong( 0x6 ),	/* 6 */
/* 52 */	NdrFcShort( 0x3a ),	/* Offset= 58 (110) */
/* 54 */	NdrFcLong( 0x7 ),	/* 7 */
/* 58 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 60 */	NdrFcLong( 0x8 ),	/* 8 */
/* 64 */	NdrFcShort( 0x44 ),	/* Offset= 68 (132) */
/* 66 */	NdrFcLong( 0x9 ),	/* 9 */
/* 70 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 72 */	NdrFcLong( 0xa ),	/* 10 */
/* 76 */	NdrFcShort( 0xa6 ),	/* Offset= 166 (242) */
/* 78 */	NdrFcLong( 0xb ),	/* 11 */
/* 82 */	NdrFcShort( 0xb2 ),	/* Offset= 178 (260) */
/* 84 */	NdrFcShort( 0xffff ),	/* Offset= -1 (83) */
/* 86 */	
			0x14, 0x0,	/* FC_FP */
/* 88 */	NdrFcShort( 0xc ),	/* Offset= 12 (100) */
/* 90 */	
			0x15,		/* FC_STRUCT */
			0x7,		/* 7 */
/* 92 */	NdrFcShort( 0x10 ),	/* 16 */
/* 94 */	0x8,		/* FC_LONG */
			0x6,		/* FC_SHORT */
/* 96 */	0x6,		/* FC_SHORT */
			0xb,		/* FC_HYPER */
/* 98 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 100 */	
			0x15,		/* FC_STRUCT */
			0x7,		/* 7 */
/* 102 */	NdrFcShort( 0x10 ),	/* 16 */
/* 104 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 106 */	NdrFcShort( 0xfff0 ),	/* Offset= -16 (90) */
/* 108 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 110 */	
			0x14, 0x0,	/* FC_FP */
/* 112 */	NdrFcShort( 0x2 ),	/* Offset= 2 (114) */
/* 114 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x0,		/* 0 */
/* 116 */	NdrFcShort( 0x0 ),	/* 0 */
/* 118 */	NdrFcShort( 0x0 ),	/* 0 */
/* 120 */	NdrFcShort( 0x0 ),	/* Offset= 0 (120) */
/* 122 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 124 */	NdrFcShort( 0xff8a ),	/* Offset= -118 (6) */
/* 126 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 128 */	NdrFcShort( 0xff86 ),	/* Offset= -122 (6) */
/* 130 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 132 */	
			0x14, 0x0,	/* FC_FP */
/* 134 */	NdrFcShort( 0x36 ),	/* Offset= 54 (188) */
/* 136 */	
			0x14, 0x8,	/* FC_FP [simple_pointer] */
/* 138 */	
			0x22,		/* FC_C_CSTRING */
			0x5c,		/* FC_PAD */
/* 140 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 142 */	NdrFcShort( 0x4 ),	/* 4 */
/* 144 */	NdrFcShort( 0x0 ),	/* 0 */
/* 146 */	NdrFcShort( 0x8 ),	/* Offset= 8 (154) */
/* 148 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 150 */	NdrFcShort( 0xff70 ),	/* Offset= -144 (6) */
/* 152 */	0x36,		/* FC_POINTER */
			0x5b,		/* FC_END */
/* 154 */	
			0x14, 0x0,	/* FC_FP */
/* 156 */	NdrFcShort( 0xfff0 ),	/* Offset= -16 (140) */
/* 158 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 160 */	NdrFcShort( 0x8 ),	/* 8 */
/* 162 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 164 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 166 */	NdrFcShort( 0x0 ),	/* 0 */
/* 168 */	NdrFcShort( 0x0 ),	/* 0 */
/* 170 */	0x14, 0x8,	/* FC_FP [simple_pointer] */
/* 172 */	
			0x22,		/* FC_C_CSTRING */
			0x5c,		/* FC_PAD */
/* 174 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 176 */	NdrFcShort( 0x4 ),	/* 4 */
/* 178 */	NdrFcShort( 0x4 ),	/* 4 */
/* 180 */	0x14, 0x0,	/* FC_FP */
/* 182 */	NdrFcShort( 0xffe8 ),	/* Offset= -24 (158) */
/* 184 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 186 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 188 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 190 */	NdrFcShort( 0x14 ),	/* 20 */
/* 192 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 194 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 196 */	NdrFcShort( 0x4 ),	/* 4 */
/* 198 */	NdrFcShort( 0x4 ),	/* 4 */
/* 200 */	0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 202 */	NdrFcShort( 0xffbe ),	/* Offset= -66 (136) */
/* 204 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 206 */	NdrFcShort( 0x8 ),	/* 8 */
/* 208 */	NdrFcShort( 0x8 ),	/* 8 */
/* 210 */	0x14, 0x0,	/* FC_FP */
/* 212 */	NdrFcShort( 0xff32 ),	/* Offset= -206 (6) */
/* 214 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 216 */	NdrFcShort( 0xc ),	/* 12 */
/* 218 */	NdrFcShort( 0xc ),	/* 12 */
/* 220 */	0x14, 0x0,	/* FC_FP */
/* 222 */	NdrFcShort( 0xffae ),	/* Offset= -82 (140) */
/* 224 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 226 */	NdrFcShort( 0x10 ),	/* 16 */
/* 228 */	NdrFcShort( 0x10 ),	/* 16 */
/* 230 */	0x14, 0x0,	/* FC_FP */
/* 232 */	NdrFcShort( 0xffb6 ),	/* Offset= -74 (158) */
/* 234 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 236 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 238 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 240 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 242 */	
			0x14, 0x0,	/* FC_FP */
/* 244 */	NdrFcShort( 0x2 ),	/* Offset= 2 (246) */
/* 246 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x0,		/* 0 */
/* 248 */	NdrFcShort( 0x0 ),	/* 0 */
/* 250 */	NdrFcShort( 0x0 ),	/* 0 */
/* 252 */	NdrFcShort( 0x0 ),	/* Offset= 0 (252) */
/* 254 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 256 */	NdrFcShort( 0xff06 ),	/* Offset= -250 (6) */
/* 258 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 260 */	
			0x14, 0x0,	/* FC_FP */
/* 262 */	NdrFcShort( 0x2 ),	/* Offset= 2 (264) */
/* 264 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 266 */	NdrFcShort( 0xc ),	/* 12 */
/* 268 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 270 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 272 */	NdrFcShort( 0x4 ),	/* 4 */
/* 274 */	NdrFcShort( 0x4 ),	/* 4 */
/* 276 */	0x14, 0x0,	/* FC_FP */
/* 278 */	NdrFcShort( 0xfef0 ),	/* Offset= -272 (6) */
/* 280 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 282 */	NdrFcShort( 0x8 ),	/* 8 */
/* 284 */	NdrFcShort( 0x8 ),	/* 8 */
/* 286 */	0x14, 0x0,	/* FC_FP */
/* 288 */	NdrFcShort( 0xff6c ),	/* Offset= -148 (140) */
/* 290 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 292 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 294 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 296 */	
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x4d,		/* 77 */
/* 298 */	NdrFcShort( 0x4 ),	/* 4 */
/* 300 */	NdrFcShort( 0x5 ),	/* 5 */
/* 302 */	NdrFcLong( 0x0 ),	/* 0 */
/* 306 */	NdrFcShort( 0x1c ),	/* Offset= 28 (334) */
/* 308 */	NdrFcLong( 0x1 ),	/* 1 */
/* 312 */	NdrFcShort( 0x40 ),	/* Offset= 64 (376) */
/* 314 */	NdrFcLong( 0x2 ),	/* 2 */
/* 318 */	NdrFcShort( 0x52 ),	/* Offset= 82 (400) */
/* 320 */	NdrFcLong( 0x3 ),	/* 3 */
/* 324 */	NdrFcShort( 0xc0 ),	/* Offset= 192 (516) */
/* 326 */	NdrFcLong( 0x4 ),	/* 4 */
/* 330 */	NdrFcShort( 0x100 ),	/* Offset= 256 (586) */
/* 332 */	NdrFcShort( 0xffff ),	/* Offset= -1 (331) */
/* 334 */	
			0x12, 0x0,	/* FC_UP */
/* 336 */	NdrFcShort( 0x2 ),	/* Offset= 2 (338) */
/* 338 */	
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x4d,		/* 77 */
/* 340 */	NdrFcShort( 0x4 ),	/* 4 */
/* 342 */	NdrFcShort( 0x5 ),	/* 5 */
/* 344 */	NdrFcLong( 0x0 ),	/* 0 */
/* 348 */	NdrFcShort( 0x800d ),	/* Simple arm type: FC_ENUM16 */
/* 350 */	NdrFcLong( 0x1 ),	/* 1 */
/* 354 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 356 */	NdrFcLong( 0x2 ),	/* 2 */
/* 360 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 362 */	NdrFcLong( 0x3 ),	/* 3 */
/* 366 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 368 */	NdrFcLong( 0x4 ),	/* 4 */
/* 372 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 374 */	NdrFcShort( 0xffff ),	/* Offset= -1 (373) */
/* 376 */	
			0x12, 0x0,	/* FC_UP */
/* 378 */	NdrFcShort( 0x2 ),	/* Offset= 2 (380) */
/* 380 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 382 */	NdrFcShort( 0x4 ),	/* 4 */
/* 384 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 386 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 388 */	NdrFcShort( 0x0 ),	/* 0 */
/* 390 */	NdrFcShort( 0x0 ),	/* 0 */
/* 392 */	0x14, 0x8,	/* FC_FP [simple_pointer] */
/* 394 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/* 396 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 398 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 400 */	
			0x12, 0x0,	/* FC_UP */
/* 402 */	NdrFcShort( 0x46 ),	/* Offset= 70 (472) */
/* 404 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 406 */	NdrFcShort( 0x14 ),	/* 20 */
/* 408 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 410 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 412 */	NdrFcShort( 0x0 ),	/* 0 */
/* 414 */	NdrFcShort( 0x0 ),	/* 0 */
/* 416 */	0x14, 0x0,	/* FC_FP */
/* 418 */	NdrFcShort( 0xce ),	/* Offset= 206 (624) */
/* 420 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 422 */	NdrFcShort( 0x4 ),	/* 4 */
/* 424 */	NdrFcShort( 0x4 ),	/* 4 */
/* 426 */	0x14, 0x0,	/* FC_FP */
/* 428 */	NdrFcShort( 0xc4 ),	/* Offset= 196 (624) */
/* 430 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 432 */	NdrFcShort( 0x8 ),	/* 8 */
/* 434 */	NdrFcShort( 0x8 ),	/* 8 */
/* 436 */	0x14, 0x0,	/* FC_FP */
/* 438 */	NdrFcShort( 0xffde ),	/* Offset= -34 (404) */
/* 440 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 442 */	NdrFcShort( 0xc ),	/* 12 */
/* 444 */	NdrFcShort( 0xc ),	/* 12 */
/* 446 */	0x14, 0x0,	/* FC_FP */
/* 448 */	NdrFcShort( 0xffd4 ),	/* Offset= -44 (404) */
/* 450 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 452 */	NdrFcShort( 0x10 ),	/* 16 */
/* 454 */	NdrFcShort( 0x10 ),	/* 16 */
/* 456 */	0x14, 0x0,	/* FC_FP */
/* 458 */	NdrFcShort( 0xffca ),	/* Offset= -54 (404) */
/* 460 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 462 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 464 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 466 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 468 */	
			0x14, 0x0,	/* FC_FP */
/* 470 */	NdrFcShort( 0xffbe ),	/* Offset= -66 (404) */
/* 472 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 474 */	NdrFcShort( 0x14 ),	/* 20 */
/* 476 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 478 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 480 */	NdrFcShort( 0x8 ),	/* 8 */
/* 482 */	NdrFcShort( 0x8 ),	/* 8 */
/* 484 */	0x14, 0x0,	/* FC_FP */
/* 486 */	NdrFcShort( 0xffae ),	/* Offset= -82 (404) */
/* 488 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 490 */	NdrFcShort( 0xc ),	/* 12 */
/* 492 */	NdrFcShort( 0xc ),	/* 12 */
/* 494 */	0x14, 0x0,	/* FC_FP */
/* 496 */	NdrFcShort( 0xffa4 ),	/* Offset= -92 (404) */
/* 498 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 500 */	NdrFcShort( 0x10 ),	/* 16 */
/* 502 */	NdrFcShort( 0x10 ),	/* 16 */
/* 504 */	0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 506 */	NdrFcShort( 0xffda ),	/* Offset= -38 (468) */
/* 508 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 510 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 512 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 514 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 516 */	
			0x12, 0x0,	/* FC_UP */
/* 518 */	NdrFcShort( 0x24 ),	/* Offset= 36 (554) */
/* 520 */	
			0x14, 0x0,	/* FC_FP */
/* 522 */	NdrFcShort( 0x66 ),	/* Offset= 102 (624) */
/* 524 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 526 */	NdrFcShort( 0x8 ),	/* 8 */
/* 528 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 530 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 532 */	NdrFcShort( 0x0 ),	/* 0 */
/* 534 */	NdrFcShort( 0x0 ),	/* 0 */
/* 536 */	0x14, 0x0,	/* FC_FP */
/* 538 */	NdrFcShort( 0x56 ),	/* Offset= 86 (624) */
/* 540 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 542 */	NdrFcShort( 0x4 ),	/* 4 */
/* 544 */	NdrFcShort( 0x4 ),	/* 4 */
/* 546 */	0x14, 0x0,	/* FC_FP */
/* 548 */	NdrFcShort( 0xffe8 ),	/* Offset= -24 (524) */
/* 550 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 552 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 554 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 556 */	NdrFcShort( 0x10 ),	/* 16 */
/* 558 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 560 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 562 */	NdrFcShort( 0x8 ),	/* 8 */
/* 564 */	NdrFcShort( 0x8 ),	/* 8 */
/* 566 */	0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 568 */	NdrFcShort( 0xffd0 ),	/* Offset= -48 (520) */
/* 570 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 572 */	NdrFcShort( 0xc ),	/* 12 */
/* 574 */	NdrFcShort( 0xc ),	/* 12 */
/* 576 */	0x14, 0x0,	/* FC_FP */
/* 578 */	NdrFcShort( 0xffca ),	/* Offset= -54 (524) */
/* 580 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 582 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 584 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 586 */	
			0x12, 0x0,	/* FC_UP */
/* 588 */	NdrFcShort( 0x6 ),	/* Offset= 6 (594) */
/* 590 */	
			0x14, 0x0,	/* FC_FP */
/* 592 */	NdrFcShort( 0x20 ),	/* Offset= 32 (624) */
/* 594 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 596 */	NdrFcShort( 0x8 ),	/* 8 */
/* 598 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 600 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 602 */	NdrFcShort( 0x0 ),	/* 0 */
/* 604 */	NdrFcShort( 0x0 ),	/* 0 */
/* 606 */	0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 608 */	NdrFcShort( 0xffee ),	/* Offset= -18 (590) */
/* 610 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 612 */	NdrFcShort( 0x4 ),	/* 4 */
/* 614 */	NdrFcShort( 0x4 ),	/* 4 */
/* 616 */	0x14, 0x0,	/* FC_FP */
/* 618 */	NdrFcShort( 0xffa2 ),	/* Offset= -94 (524) */
/* 620 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 622 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 624 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 626 */	NdrFcShort( 0x10 ),	/* 16 */
/* 628 */	NdrFcShort( 0x0 ),	/* 0 */
/* 630 */	NdrFcShort( 0x0 ),	/* Offset= 0 (630) */
/* 632 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 634 */	NdrFcShort( 0xfd8c ),	/* Offset= -628 (6) */
/* 636 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 638 */	NdrFcShort( 0xfeaa ),	/* Offset= -342 (296) */
/* 640 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */

			0x0
        }
    };

static const unsigned short PrtDistributed_FormatStringOffsetTable[] =
    {
    0,
    34
    };


static const MIDL_STUB_DESC PrtDistributed_StubDesc = 
    {
    (void *)& PrtDistributed___RpcClientInterface,
    MIDL_user_allocate,
    MIDL_user_free,
    &PrtDistributed__MIDL_AutoBindHandle,
    0,
    0,
    0,
    0,
    PrtDistributed__MIDL_TypeFormatString.Format,
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

