

/* this ALWAYS GENERATED file contains the RPC client stubs */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Mon Jul 07 20:32:41 2014
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

#define TYPE_FORMAT_STRING_SIZE   685                               
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
/*  4 */	NdrFcShort( 0x296 ),	/* Offset= 662 (666) */
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
/* 300 */	NdrFcShort( 0xc ),	/* 12 */
/* 302 */	NdrFcLong( 0x0 ),	/* 0 */
/* 306 */	NdrFcShort( 0x46 ),	/* Offset= 70 (376) */
/* 308 */	NdrFcLong( 0x9 ),	/* 9 */
/* 312 */	NdrFcShort( 0x40 ),	/* Offset= 64 (376) */
/* 314 */	NdrFcLong( 0x5 ),	/* 5 */
/* 318 */	NdrFcShort( 0x3a ),	/* Offset= 58 (376) */
/* 320 */	NdrFcLong( 0x7 ),	/* 7 */
/* 324 */	NdrFcShort( 0x34 ),	/* Offset= 52 (376) */
/* 326 */	NdrFcLong( 0x4 ),	/* 4 */
/* 330 */	NdrFcShort( 0x2e ),	/* Offset= 46 (376) */
/* 332 */	NdrFcLong( 0x1 ),	/* 1 */
/* 336 */	NdrFcShort( 0x28 ),	/* Offset= 40 (376) */
/* 338 */	NdrFcLong( 0x2 ),	/* 2 */
/* 342 */	NdrFcShort( 0x22 ),	/* Offset= 34 (376) */
/* 344 */	NdrFcLong( 0x3 ),	/* 3 */
/* 348 */	NdrFcShort( 0x46 ),	/* Offset= 70 (418) */
/* 350 */	NdrFcLong( 0x6 ),	/* 6 */
/* 354 */	NdrFcShort( 0x58 ),	/* Offset= 88 (442) */
/* 356 */	NdrFcLong( 0xa ),	/* 10 */
/* 360 */	NdrFcShort( 0xc6 ),	/* Offset= 198 (558) */
/* 362 */	NdrFcLong( 0x8 ),	/* 8 */
/* 366 */	NdrFcShort( 0x106 ),	/* Offset= 262 (628) */
/* 368 */	NdrFcLong( 0xb ),	/* 11 */
/* 372 */	NdrFcShort( 0x100 ),	/* Offset= 256 (628) */
/* 374 */	NdrFcShort( 0xffff ),	/* Offset= -1 (373) */
/* 376 */	
			0x12, 0x0,	/* FC_UP */
/* 378 */	NdrFcShort( 0x2 ),	/* Offset= 2 (380) */
/* 380 */	
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x4d,		/* 77 */
/* 382 */	NdrFcShort( 0x4 ),	/* 4 */
/* 384 */	NdrFcShort( 0x5 ),	/* 5 */
/* 386 */	NdrFcLong( 0x1 ),	/* 1 */
/* 390 */	NdrFcShort( 0x800d ),	/* Simple arm type: FC_ENUM16 */
/* 392 */	NdrFcLong( 0x2 ),	/* 2 */
/* 396 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 398 */	NdrFcLong( 0x4 ),	/* 4 */
/* 402 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 404 */	NdrFcLong( 0x7 ),	/* 7 */
/* 408 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 410 */	NdrFcLong( 0x5 ),	/* 5 */
/* 414 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 416 */	NdrFcShort( 0xffff ),	/* Offset= -1 (415) */
/* 418 */	
			0x12, 0x0,	/* FC_UP */
/* 420 */	NdrFcShort( 0x2 ),	/* Offset= 2 (422) */
/* 422 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 424 */	NdrFcShort( 0x4 ),	/* 4 */
/* 426 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 428 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 430 */	NdrFcShort( 0x0 ),	/* 0 */
/* 432 */	NdrFcShort( 0x0 ),	/* 0 */
/* 434 */	0x14, 0x8,	/* FC_FP [simple_pointer] */
/* 436 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/* 438 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 440 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 442 */	
			0x12, 0x0,	/* FC_UP */
/* 444 */	NdrFcShort( 0x46 ),	/* Offset= 70 (514) */
/* 446 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 448 */	NdrFcShort( 0x14 ),	/* 20 */
/* 450 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 452 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 454 */	NdrFcShort( 0x0 ),	/* 0 */
/* 456 */	NdrFcShort( 0x0 ),	/* 0 */
/* 458 */	0x14, 0x0,	/* FC_FP */
/* 460 */	NdrFcShort( 0xce ),	/* Offset= 206 (666) */
/* 462 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 464 */	NdrFcShort( 0x4 ),	/* 4 */
/* 466 */	NdrFcShort( 0x4 ),	/* 4 */
/* 468 */	0x14, 0x0,	/* FC_FP */
/* 470 */	NdrFcShort( 0xc4 ),	/* Offset= 196 (666) */
/* 472 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 474 */	NdrFcShort( 0x8 ),	/* 8 */
/* 476 */	NdrFcShort( 0x8 ),	/* 8 */
/* 478 */	0x14, 0x0,	/* FC_FP */
/* 480 */	NdrFcShort( 0xffde ),	/* Offset= -34 (446) */
/* 482 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 484 */	NdrFcShort( 0xc ),	/* 12 */
/* 486 */	NdrFcShort( 0xc ),	/* 12 */
/* 488 */	0x14, 0x0,	/* FC_FP */
/* 490 */	NdrFcShort( 0xffd4 ),	/* Offset= -44 (446) */
/* 492 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 494 */	NdrFcShort( 0x10 ),	/* 16 */
/* 496 */	NdrFcShort( 0x10 ),	/* 16 */
/* 498 */	0x14, 0x0,	/* FC_FP */
/* 500 */	NdrFcShort( 0xffca ),	/* Offset= -54 (446) */
/* 502 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 504 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 506 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 508 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 510 */	
			0x14, 0x0,	/* FC_FP */
/* 512 */	NdrFcShort( 0xffbe ),	/* Offset= -66 (446) */
/* 514 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 516 */	NdrFcShort( 0x14 ),	/* 20 */
/* 518 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 520 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 522 */	NdrFcShort( 0x8 ),	/* 8 */
/* 524 */	NdrFcShort( 0x8 ),	/* 8 */
/* 526 */	0x14, 0x0,	/* FC_FP */
/* 528 */	NdrFcShort( 0xffae ),	/* Offset= -82 (446) */
/* 530 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 532 */	NdrFcShort( 0xc ),	/* 12 */
/* 534 */	NdrFcShort( 0xc ),	/* 12 */
/* 536 */	0x14, 0x0,	/* FC_FP */
/* 538 */	NdrFcShort( 0xffa4 ),	/* Offset= -92 (446) */
/* 540 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 542 */	NdrFcShort( 0x10 ),	/* 16 */
/* 544 */	NdrFcShort( 0x10 ),	/* 16 */
/* 546 */	0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 548 */	NdrFcShort( 0xffda ),	/* Offset= -38 (510) */
/* 550 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 552 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 554 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 556 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 558 */	
			0x12, 0x0,	/* FC_UP */
/* 560 */	NdrFcShort( 0x24 ),	/* Offset= 36 (596) */
/* 562 */	
			0x14, 0x0,	/* FC_FP */
/* 564 */	NdrFcShort( 0x66 ),	/* Offset= 102 (666) */
/* 566 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 568 */	NdrFcShort( 0x8 ),	/* 8 */
/* 570 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 572 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 574 */	NdrFcShort( 0x0 ),	/* 0 */
/* 576 */	NdrFcShort( 0x0 ),	/* 0 */
/* 578 */	0x14, 0x0,	/* FC_FP */
/* 580 */	NdrFcShort( 0x56 ),	/* Offset= 86 (666) */
/* 582 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 584 */	NdrFcShort( 0x4 ),	/* 4 */
/* 586 */	NdrFcShort( 0x4 ),	/* 4 */
/* 588 */	0x14, 0x0,	/* FC_FP */
/* 590 */	NdrFcShort( 0xffe8 ),	/* Offset= -24 (566) */
/* 592 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 594 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 596 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 598 */	NdrFcShort( 0x10 ),	/* 16 */
/* 600 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 602 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 604 */	NdrFcShort( 0x8 ),	/* 8 */
/* 606 */	NdrFcShort( 0x8 ),	/* 8 */
/* 608 */	0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 610 */	NdrFcShort( 0xffd0 ),	/* Offset= -48 (562) */
/* 612 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 614 */	NdrFcShort( 0xc ),	/* 12 */
/* 616 */	NdrFcShort( 0xc ),	/* 12 */
/* 618 */	0x14, 0x0,	/* FC_FP */
/* 620 */	NdrFcShort( 0xffca ),	/* Offset= -54 (566) */
/* 622 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 624 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 626 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 628 */	
			0x12, 0x0,	/* FC_UP */
/* 630 */	NdrFcShort( 0x6 ),	/* Offset= 6 (636) */
/* 632 */	
			0x14, 0x0,	/* FC_FP */
/* 634 */	NdrFcShort( 0x20 ),	/* Offset= 32 (666) */
/* 636 */	
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 638 */	NdrFcShort( 0x8 ),	/* 8 */
/* 640 */	
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 642 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 644 */	NdrFcShort( 0x0 ),	/* 0 */
/* 646 */	NdrFcShort( 0x0 ),	/* 0 */
/* 648 */	0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 650 */	NdrFcShort( 0xffee ),	/* Offset= -18 (632) */
/* 652 */	
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 654 */	NdrFcShort( 0x4 ),	/* 4 */
/* 656 */	NdrFcShort( 0x4 ),	/* 4 */
/* 658 */	0x14, 0x0,	/* FC_FP */
/* 660 */	NdrFcShort( 0xffa2 ),	/* Offset= -94 (566) */
/* 662 */	
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 664 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 666 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 668 */	NdrFcShort( 0x10 ),	/* 16 */
/* 670 */	NdrFcShort( 0x0 ),	/* 0 */
/* 672 */	NdrFcShort( 0x0 ),	/* Offset= 0 (672) */
/* 674 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 676 */	NdrFcShort( 0xfd62 ),	/* Offset= -670 (6) */
/* 678 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 680 */	NdrFcShort( 0xfe80 ),	/* Offset= -384 (296) */
/* 682 */	0x5c,		/* FC_PAD */
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

