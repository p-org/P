

/* this ALWAYS GENERATED file contains the RPC client stubs */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Wed Oct 15 10:48:39 2014
 */
/* Compiler settings for PrtDistributed.idl:
    Oicf, W1, Zp8, env=Win64 (32b run), target_arch=AMD64 8.00.0603 
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */

#if defined(_M_AMD64)


#pragma warning( disable: 4049 )  /* more than 64k source lines */
#if _MSC_VER >= 1200
#pragma warning(push)
#endif

#pragma warning( disable: 4211 )  /* redefine extern to static */
#pragma warning( disable: 4232 )  /* dllimport identity*/
#pragma warning( disable: 4024 )  /* array to pointer mapping*/

#include <string.h>

#include "PrtDistributed.h"

#define TYPE_FORMAT_STRING_SIZE   517                               
#define PROC_FORMAT_STRING_SIZE   73                                
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
                  handleM,
                  value);
    
}


void c_SendValue2( 
    /* [in] */ handle_t handleM,
    /* [full][in] */ PRT_VALUE *value)
{

    NdrClientCall2(
                  ( PMIDL_STUB_DESC  )&PrtDistributed_StubDesc,
                  (PFORMAT_STRING) &PrtDistributed__MIDL_ProcFormatString.Format[36],
                  handleM,
                  value);
    
}


#if !defined(__RPC_WIN64__)
#error  Invalid build platform for this stub.
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
/*  8 */	NdrFcShort( 0x10 ),	/* X64 Stack size/offset = 16 */
/* 10 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 12 */	NdrFcShort( 0x0 ),	/* X64 Stack size/offset = 0 */
/* 14 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18 */	0x42,		/* Oi2 Flags:  clt must size, has ext, */
			0x1,		/* 1 */
/* 20 */	0xa,		/* 10 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */
/* 24 */	NdrFcShort( 0x1 ),	/* 1 */
/* 26 */	NdrFcShort( 0x0 ),	/* 0 */
/* 28 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter handleM */

/* 30 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 32 */	NdrFcShort( 0x8 ),	/* X64 Stack size/offset = 8 */
/* 34 */	NdrFcShort( 0x2 ),	/* Type Offset=2 */

	/* Procedure SendValue2 */


	/* Parameter value */

/* 36 */	0x0,		/* 0 */
			0x49,		/* Old Flags:  full ptr, */
/* 38 */	NdrFcLong( 0x0 ),	/* 0 */
/* 42 */	NdrFcShort( 0x1 ),	/* 1 */
/* 44 */	NdrFcShort( 0x10 ),	/* X64 Stack size/offset = 16 */
/* 46 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 48 */	NdrFcShort( 0x0 ),	/* X64 Stack size/offset = 0 */
/* 50 */	NdrFcShort( 0x0 ),	/* 0 */
/* 52 */	NdrFcShort( 0x0 ),	/* 0 */
/* 54 */	0x42,		/* Oi2 Flags:  clt must size, has ext, */
			0x1,		/* 1 */
/* 56 */	0xa,		/* 10 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 58 */	NdrFcShort( 0x0 ),	/* 0 */
/* 60 */	NdrFcShort( 0x1 ),	/* 1 */
/* 62 */	NdrFcShort( 0x0 ),	/* 0 */
/* 64 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter handleM */

/* 66 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 68 */	NdrFcShort( 0x8 ),	/* X64 Stack size/offset = 8 */
/* 70 */	NdrFcShort( 0x2 ),	/* Type Offset=2 */

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
/*  4 */	NdrFcShort( 0x1ee ),	/* Offset= 494 (498) */
/*  6 */	
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x8d,		/* 141 */
/*  8 */	NdrFcShort( 0x8 ),	/* 8 */
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
/* 76 */	NdrFcShort( 0x86 ),	/* Offset= 134 (210) */
/* 78 */	NdrFcLong( 0xb ),	/* 11 */
/* 82 */	NdrFcShort( 0x92 ),	/* Offset= 146 (228) */
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
/* 134 */	NdrFcShort( 0x2c ),	/* Offset= 44 (178) */
/* 136 */	
			0x14, 0x8,	/* FC_FP [simple_pointer] */
/* 138 */	
			0x22,		/* FC_C_CSTRING */
			0x5c,		/* FC_PAD */
/* 140 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 142 */	NdrFcShort( 0x8 ),	/* 8 */
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
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 160 */	NdrFcShort( 0x10 ),	/* 16 */
/* 162 */	NdrFcShort( 0x0 ),	/* 0 */
/* 164 */	NdrFcShort( 0x6 ),	/* Offset= 6 (170) */
/* 166 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 168 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 170 */	
			0x14, 0x8,	/* FC_FP [simple_pointer] */
/* 172 */	
			0x22,		/* FC_C_CSTRING */
			0x5c,		/* FC_PAD */
/* 174 */	
			0x14, 0x0,	/* FC_FP */
/* 176 */	NdrFcShort( 0xffee ),	/* Offset= -18 (158) */
/* 178 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 180 */	NdrFcShort( 0x28 ),	/* 40 */
/* 182 */	NdrFcShort( 0x0 ),	/* 0 */
/* 184 */	NdrFcShort( 0xa ),	/* Offset= 10 (194) */
/* 186 */	0x8,		/* FC_LONG */
			0x40,		/* FC_STRUCTPAD4 */
/* 188 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 190 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 192 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 194 */	
			0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 196 */	NdrFcShort( 0xffc4 ),	/* Offset= -60 (136) */
/* 198 */	
			0x14, 0x0,	/* FC_FP */
/* 200 */	NdrFcShort( 0xff3e ),	/* Offset= -194 (6) */
/* 202 */	
			0x14, 0x0,	/* FC_FP */
/* 204 */	NdrFcShort( 0xffc0 ),	/* Offset= -64 (140) */
/* 206 */	
			0x14, 0x0,	/* FC_FP */
/* 208 */	NdrFcShort( 0xffce ),	/* Offset= -50 (158) */
/* 210 */	
			0x14, 0x0,	/* FC_FP */
/* 212 */	NdrFcShort( 0x2 ),	/* Offset= 2 (214) */
/* 214 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x0,		/* 0 */
/* 216 */	NdrFcShort( 0x0 ),	/* 0 */
/* 218 */	NdrFcShort( 0x0 ),	/* 0 */
/* 220 */	NdrFcShort( 0x0 ),	/* Offset= 0 (220) */
/* 222 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 224 */	NdrFcShort( 0xff26 ),	/* Offset= -218 (6) */
/* 226 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 228 */	
			0x14, 0x0,	/* FC_FP */
/* 230 */	NdrFcShort( 0x2 ),	/* Offset= 2 (232) */
/* 232 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 234 */	NdrFcShort( 0x18 ),	/* 24 */
/* 236 */	NdrFcShort( 0x0 ),	/* 0 */
/* 238 */	NdrFcShort( 0x8 ),	/* Offset= 8 (246) */
/* 240 */	0x8,		/* FC_LONG */
			0x40,		/* FC_STRUCTPAD4 */
/* 242 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 244 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 246 */	
			0x14, 0x0,	/* FC_FP */
/* 248 */	NdrFcShort( 0xff0e ),	/* Offset= -242 (6) */
/* 250 */	
			0x14, 0x0,	/* FC_FP */
/* 252 */	NdrFcShort( 0xff90 ),	/* Offset= -112 (140) */
/* 254 */	
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x8d,		/* 141 */
/* 256 */	NdrFcShort( 0x8 ),	/* 8 */
/* 258 */	NdrFcShort( 0x5 ),	/* 5 */
/* 260 */	NdrFcLong( 0x0 ),	/* 0 */
/* 264 */	NdrFcShort( 0x1c ),	/* Offset= 28 (292) */
/* 266 */	NdrFcLong( 0x1 ),	/* 1 */
/* 270 */	NdrFcShort( 0x40 ),	/* Offset= 64 (334) */
/* 272 */	NdrFcLong( 0x2 ),	/* 2 */
/* 276 */	NdrFcShort( 0x4c ),	/* Offset= 76 (352) */
/* 278 */	NdrFcLong( 0x3 ),	/* 3 */
/* 282 */	NdrFcShort( 0x8a ),	/* Offset= 138 (420) */
/* 284 */	NdrFcLong( 0x4 ),	/* 4 */
/* 288 */	NdrFcShort( 0xb6 ),	/* Offset= 182 (470) */
/* 290 */	NdrFcShort( 0xffff ),	/* Offset= -1 (289) */
/* 292 */	
			0x12, 0x0,	/* FC_UP */
/* 294 */	NdrFcShort( 0x2 ),	/* Offset= 2 (296) */
/* 296 */	
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x4d,		/* 77 */
/* 298 */	NdrFcShort( 0x4 ),	/* 4 */
/* 300 */	NdrFcShort( 0x5 ),	/* 5 */
/* 302 */	NdrFcLong( 0x0 ),	/* 0 */
/* 306 */	NdrFcShort( 0x800d ),	/* Simple arm type: FC_ENUM16 */
/* 308 */	NdrFcLong( 0x1 ),	/* 1 */
/* 312 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 314 */	NdrFcLong( 0x2 ),	/* 2 */
/* 318 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 320 */	NdrFcLong( 0x3 ),	/* 3 */
/* 324 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 326 */	NdrFcLong( 0x4 ),	/* 4 */
/* 330 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 332 */	NdrFcShort( 0xffff ),	/* Offset= -1 (331) */
/* 334 */	
			0x12, 0x0,	/* FC_UP */
/* 336 */	NdrFcShort( 0x2 ),	/* Offset= 2 (338) */
/* 338 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 340 */	NdrFcShort( 0x8 ),	/* 8 */
/* 342 */	NdrFcShort( 0x0 ),	/* 0 */
/* 344 */	NdrFcShort( 0x4 ),	/* Offset= 4 (348) */
/* 346 */	0x36,		/* FC_POINTER */
			0x5b,		/* FC_END */
/* 348 */	
			0x14, 0x8,	/* FC_FP [simple_pointer] */
/* 350 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/* 352 */	
			0x12, 0x0,	/* FC_UP */
/* 354 */	NdrFcShort( 0x28 ),	/* Offset= 40 (394) */
/* 356 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 358 */	NdrFcShort( 0x28 ),	/* 40 */
/* 360 */	NdrFcShort( 0x0 ),	/* 0 */
/* 362 */	NdrFcShort( 0x8 ),	/* Offset= 8 (370) */
/* 364 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 366 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 368 */	0x36,		/* FC_POINTER */
			0x5b,		/* FC_END */
/* 370 */	
			0x14, 0x0,	/* FC_FP */
/* 372 */	NdrFcShort( 0x7e ),	/* Offset= 126 (498) */
/* 374 */	
			0x14, 0x0,	/* FC_FP */
/* 376 */	NdrFcShort( 0x7a ),	/* Offset= 122 (498) */
/* 378 */	
			0x14, 0x0,	/* FC_FP */
/* 380 */	NdrFcShort( 0xffe8 ),	/* Offset= -24 (356) */
/* 382 */	
			0x14, 0x0,	/* FC_FP */
/* 384 */	NdrFcShort( 0xffe4 ),	/* Offset= -28 (356) */
/* 386 */	
			0x14, 0x0,	/* FC_FP */
/* 388 */	NdrFcShort( 0xffe0 ),	/* Offset= -32 (356) */
/* 390 */	
			0x14, 0x0,	/* FC_FP */
/* 392 */	NdrFcShort( 0xffdc ),	/* Offset= -36 (356) */
/* 394 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 396 */	NdrFcShort( 0x20 ),	/* 32 */
/* 398 */	NdrFcShort( 0x0 ),	/* 0 */
/* 400 */	NdrFcShort( 0x8 ),	/* Offset= 8 (408) */
/* 402 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 404 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 406 */	0x36,		/* FC_POINTER */
			0x5b,		/* FC_END */
/* 408 */	
			0x14, 0x0,	/* FC_FP */
/* 410 */	NdrFcShort( 0xffca ),	/* Offset= -54 (356) */
/* 412 */	
			0x14, 0x0,	/* FC_FP */
/* 414 */	NdrFcShort( 0xffc6 ),	/* Offset= -58 (356) */
/* 416 */	
			0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 418 */	NdrFcShort( 0xffe4 ),	/* Offset= -28 (390) */
/* 420 */	
			0x12, 0x0,	/* FC_UP */
/* 422 */	NdrFcShort( 0x1a ),	/* Offset= 26 (448) */
/* 424 */	
			0x14, 0x0,	/* FC_FP */
/* 426 */	NdrFcShort( 0x48 ),	/* Offset= 72 (498) */
/* 428 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 430 */	NdrFcShort( 0x10 ),	/* 16 */
/* 432 */	NdrFcShort( 0x0 ),	/* 0 */
/* 434 */	NdrFcShort( 0x6 ),	/* Offset= 6 (440) */
/* 436 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 438 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 440 */	
			0x14, 0x0,	/* FC_FP */
/* 442 */	NdrFcShort( 0x38 ),	/* Offset= 56 (498) */
/* 444 */	
			0x14, 0x0,	/* FC_FP */
/* 446 */	NdrFcShort( 0xffee ),	/* Offset= -18 (428) */
/* 448 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 450 */	NdrFcShort( 0x18 ),	/* 24 */
/* 452 */	NdrFcShort( 0x0 ),	/* 0 */
/* 454 */	NdrFcShort( 0x8 ),	/* Offset= 8 (462) */
/* 456 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 458 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 460 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 462 */	
			0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 464 */	NdrFcShort( 0xffd8 ),	/* Offset= -40 (424) */
/* 466 */	
			0x14, 0x0,	/* FC_FP */
/* 468 */	NdrFcShort( 0xffd8 ),	/* Offset= -40 (428) */
/* 470 */	
			0x12, 0x0,	/* FC_UP */
/* 472 */	NdrFcShort( 0x6 ),	/* Offset= 6 (478) */
/* 474 */	
			0x14, 0x0,	/* FC_FP */
/* 476 */	NdrFcShort( 0x16 ),	/* Offset= 22 (498) */
/* 478 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 480 */	NdrFcShort( 0x10 ),	/* 16 */
/* 482 */	NdrFcShort( 0x0 ),	/* 0 */
/* 484 */	NdrFcShort( 0x6 ),	/* Offset= 6 (490) */
/* 486 */	0x36,		/* FC_POINTER */
			0x36,		/* FC_POINTER */
/* 488 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 490 */	
			0x14, 0x10,	/* FC_FP [pointer_deref] */
/* 492 */	NdrFcShort( 0xffee ),	/* Offset= -18 (474) */
/* 494 */	
			0x14, 0x0,	/* FC_FP */
/* 496 */	NdrFcShort( 0xffbc ),	/* Offset= -68 (428) */
/* 498 */	
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 500 */	NdrFcShort( 0x20 ),	/* 32 */
/* 502 */	NdrFcShort( 0x0 ),	/* 0 */
/* 504 */	NdrFcShort( 0x0 ),	/* Offset= 0 (504) */
/* 506 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 508 */	NdrFcShort( 0xfe0a ),	/* Offset= -502 (6) */
/* 510 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 512 */	NdrFcShort( 0xfefe ),	/* Offset= -258 (254) */
/* 514 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */

			0x0
        }
    };

static const unsigned short PrtDistributed_FormatStringOffsetTable[] =
    {
    0,
    36
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
#if _MSC_VER >= 1200
#pragma warning(pop)
#endif


#endif /* defined(_M_AMD64)*/

