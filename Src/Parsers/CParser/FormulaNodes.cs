using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Formula.API.Nodes;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Compiler;
using Microsoft.Formula.API;

namespace CParser
{
    internal class FormulaNodes
    {
        public AST<Domain> DataModel
        {
            get;
            private set;
        }
        
        #region constants
        public static AST<Id> Section_Iden = Factory.Instance.MkId("Section");
        public static AST<Id> PpEscape_Iden = Factory.Instance.MkId("PpEscape");
        public static AST<Id> PpDefine_Iden = Factory.Instance.MkId("PpDefine");
        public static AST<Id> PpInclude_Iden = Factory.Instance.MkId("PpInclude");
        public static AST<Id> PpUndef_Iden = Factory.Instance.MkId("PpUndef");
        public static AST<Id> PpPragma_Iden = Factory.Instance.MkId("PpPragma");
        public static AST<Id> PpITE_Iden = Factory.Instance.MkId("PpITE");
        public static AST<Id> PpElIf_Iden = Factory.Instance.MkId("PpElIf");
        public static AST<Id> PpIf_Iden = Factory.Instance.MkId("IF");
        public static AST<Id> PpIfdef_Iden = Factory.Instance.MkId("IFDEF");
        public static AST<Id> PpIfndef_Iden = Factory.Instance.MkId("IFNDEF");

        public static AST<Id> VarDef_Iden = Factory.Instance.MkId("VarDef");
        public static AST<Id> FunDef_Iden = Factory.Instance.MkId("FunDef");
        public static AST<Id> DatDef_Iden = Factory.Instance.MkId("DatDef");
        public static AST<Id> EnmDef_Iden = Factory.Instance.MkId("EnmDef");
        public static AST<Id> TypDef_Iden = Factory.Instance.MkId("TypDef");
        public static AST<Id> Defs_Iden = Factory.Instance.MkId("Defs");
        public static AST<Id> Unknown_Iden = Factory.Instance.MkId("UNKNOWN");

        public static AST<Id> Fields_Iden = Factory.Instance.MkId("Fields");
        public static AST<Id> Params_Iden = Factory.Instance.MkId("Params");
        public static AST<Id> Param_Iden = Factory.Instance.MkId("Param");
        public static AST<Id> Elements_Iden = Factory.Instance.MkId("Elements");
        public static AST<Id> Element_Iden = Factory.Instance.MkId("Element");

        public static AST<Id> Nil_Iden = Factory.Instance.MkId("NIL");
        public static AST<Id> True_Iden = Factory.Instance.MkId("TRUE");
        public static AST<Id> False_Iden = Factory.Instance.MkId("FALSE");
        public static AST<Id> Ellipse_Iden = Factory.Instance.MkId("ELLIPSE");
        public static AST<Id> FormatChar_Iden = Factory.Instance.MkId("CHAR");
        public static AST<Id> FormatOct_Iden = Factory.Instance.MkId("OCT");
        public static AST<Id> FormatDec_Iden = Factory.Instance.MkId("DEC");
        public static AST<Id> FormatHex_Iden = Factory.Instance.MkId("HEX");
        public static AST<Id> FormatExp_Iden = Factory.Instance.MkId("EXP");
        public static AST<Id> L_Iden = Factory.Instance.MkId("L");
        public static AST<Id> U_Iden = Factory.Instance.MkId("U");
        public static AST<Id> F_Iden = Factory.Instance.MkId("F");
        public static AST<Id> UL_Iden = Factory.Instance.MkId("UL");
        public static AST<Id> Default_Iden = Factory.Instance.MkId("DEFAULT");
        #endregion

        #region Modifiers & Storage Types
        public static AST<Id> static_Iden = Factory.Instance.MkId("STATIC");
        public static AST<Id> volatile_Iden = Factory.Instance.MkId("VOLATILE");
        public static AST<Id> Const_Iden = Factory.Instance.MkId("CONST");
        public static AST<Id> Extern_Iden = Factory.Instance.MkId("EXTERN");
        public static AST<Id> Auto_Iden = Factory.Instance.MkId("AUTO");
        public static AST<Id> Register_Iden = Factory.Instance.MkId("REGISTER");
        #endregion

        //Types
        #region Types
        public static AST<Id> char_Iden = Factory.Instance.MkId("CHAR");
        public static AST<Id> uchar_Iden = Factory.Instance.MkId("UCHAR");
        public static AST<Id> schar_Iden = Factory.Instance.MkId("SCHAR");
        public static AST<Id> short_Iden = Factory.Instance.MkId("SHORT");
        public static AST<Id> ushort_Iden = Factory.Instance.MkId("USHORT");
        public static AST<Id> sshort_Iden = Factory.Instance.MkId("SSHORT");
        public static AST<Id> int_Iden = Factory.Instance.MkId("INT");
        public static AST<Id> uint_Iden = Factory.Instance.MkId("UINT");
        public static AST<Id> sint_Iden = Factory.Instance.MkId("SINT");
        public static AST<Id> byte_Iden = Factory.Instance.MkId("BYTE");
        public static AST<Id> long_Iden = Factory.Instance.MkId("LONG");
        public static AST<Id> ulong_Iden = Factory.Instance.MkId("ULONG");
        public static AST<Id> slong_Iden = Factory.Instance.MkId("SLONG");
        public static AST<Id> double_Iden = Factory.Instance.MkId("DOUBLE");
        public static AST<Id> ldouble_Iden = Factory.Instance.MkId("LDOUBLE");
        public static AST<Id> string_Iden = Factory.Instance.MkId("STRING");
        public static AST<Id> void_Iden = Factory.Instance.MkId("VOID");
        public static AST<Id> float_Iden = Factory.Instance.MkId("FLOAT");
        
        //complex types
        public static AST<Id> struct_Iden = Factory.Instance.MkId("STRUCT");
        public static AST<Id> union_Iden = Factory.Instance.MkId("UNION");
        public static AST<Id> enum_Iden = Factory.Instance.MkId("ENUM");
        #endregion

        //Operations
        #region Operations
        //unary
        public static AST<Id> LNot_Iden = Factory.Instance.MkId("LNOT");
        public static AST<Id> BNot_Iden = Factory.Instance.MkId("BNOT");
        public static AST<Id> Neg_Iden = Factory.Instance.MkId("NEG");
        public static AST<Id> Pos_Iden = Factory.Instance.MkId("POS");
        public static AST<Id> Addr_Iden = Factory.Instance.MkId("ADDR");
        public static AST<Id> Drf_Iden = Factory.Instance.MkId("DRF");
        public static AST<Id> Inc_Iden = Factory.Instance.MkId("INC");
        public static AST<Id> Dec_Iden = Factory.Instance.MkId("DEC");
        public static AST<Id> IncAfter_Iden = Factory.Instance.MkId("INCAFTER");
        public static AST<Id> DecAfter_Iden = Factory.Instance.MkId("DECAFTER");

        //Binary
        public static AST<Id> ArrayAccess_Iden = Factory.Instance.MkId("AAC");
        public static AST<Id> Add_Iden = Factory.Instance.MkId("ADD");
        public static AST<Id> Sub_Iden = Factory.Instance.MkId("SUB");
        public static AST<Id> Mul_Iden = Factory.Instance.MkId("MUL");
        public static AST<Id> Div_Iden = Factory.Instance.MkId("DIV");
        public static AST<Id> Mod_Iden = Factory.Instance.MkId("MOD");
        public static AST<Id> LAnd_Iden = Factory.Instance.MkId("LAND");
        public static AST<Id> LOr_Iden = Factory.Instance.MkId("LOR");
        public static AST<Id> BAnd_Iden = Factory.Instance.MkId("BAND");
        public static AST<Id> BOr_Iden = Factory.Instance.MkId("BOR");
        public static AST<Id> BXOr_Iden = Factory.Instance.MkId("BXOR");
        public static AST<Id> EqEq_Iden = Factory.Instance.MkId("EQ");
        public static AST<Id> NEq_Iden = Factory.Instance.MkId("NEQ");
        public static AST<Id> Gt_Iden = Factory.Instance.MkId("GT");
        public static AST<Id> GtEq_Iden = Factory.Instance.MkId("GE");
        public static AST<Id> Lt_Iden = Factory.Instance.MkId("LT");
        public static AST<Id> LtEq_Iden = Factory.Instance.MkId("LE");
        public static AST<Id> FieldAccess_Iden = Factory.Instance.MkId("FLD");
        public static AST<Id> PtrFieldAccess_Iden = Factory.Instance.MkId("PFLD");
        public static AST<Id> Left_Iden = Factory.Instance.MkId("LFT");
        public static AST<Id> Right_Iden = Factory.Instance.MkId("RT");
        public static AST<Id> Comma_Iden = Factory.Instance.MkId("CMA");

        //Assign
        public static AST<Id> Asn_Iden = Factory.Instance.MkId("ASN");
        public static AST<Id> Asnadd_Iden = Factory.Instance.MkId("ASNADD");
        public static AST<Id> Asnsub_Iden = Factory.Instance.MkId("ASNSUB");
        public static AST<Id> Asnmul_Iden = Factory.Instance.MkId("ASNMUL");
        public static AST<Id> Asndiv_Iden = Factory.Instance.MkId("ASNDIV");
        public static AST<Id> Asnmod_Iden = Factory.Instance.MkId("ASNMOD");
        public static AST<Id> Asnand_Iden = Factory.Instance.MkId("ASNAND");
        public static AST<Id> Asnor_Iden = Factory.Instance.MkId("ADNOR");
        public static AST<Id> Asnxor_Iden = Factory.Instance.MkId("ASNXOR");
        public static AST<Id> Asnrt_Iden = Factory.Instance.MkId("ASNRT");
        public static AST<Id> Asnlft_Iden = Factory.Instance.MkId("ASNLFT");

        //ternary
        public static AST<Id> Tcond_Iden = Factory.Instance.MkId("TCOND");
        #endregion

        //Identifiers
        #region Identifier Nodes
        public static AST<Id> Comment_Iden = Factory.Instance.MkId("Comment");
        public static AST<Id> Locals_Iden = Factory.Instance.MkId("Locals");
        public static AST<Id> While_Iden = Factory.Instance.MkId("WHILE");
        public static AST<Id> StrJmp_Iden = Factory.Instance.MkId("StrJmp");
        public static AST<Id> Do_Iden = Factory.Instance.MkId("DO");
        public static AST<Id> Ident_Iden = Factory.Instance.MkId("Ident");
        public static AST<Id> UnApp_Iden = Factory.Instance.MkId("UnApp");
        public static AST<Id> BinApp_Iden = Factory.Instance.MkId("BinApp");
        public static AST<Id> TerApp_Iden = Factory.Instance.MkId("TerApp");
        public static AST<Id> Paren_Iden = Factory.Instance.MkId("Paren");
        public static AST<Id> Args_Iden = Factory.Instance.MkId("Args");
        public static AST<Id> ITE_Iden = Factory.Instance.MkId("ITE");
        public static AST<Id> Seq_Iden = Factory.Instance.MkId("Seq");
        public static AST<Id> Loop_Iden = Factory.Instance.MkId("Loop");
        public static AST<Id> ArrType_Iden = Factory.Instance.MkId("ArrType");
        public static AST<Id> Switch_Iden = Factory.Instance.MkId("Switch");
        public static AST<Id> Goto_Iden = Factory.Instance.MkId("Goto");
        public static AST<Id> PtrType_Iden = Factory.Instance.MkId("PtrType");
        public static AST<Id> Label_Iden = Factory.Instance.MkId("Lbl");
        public static AST<Id> OtherDecl_Iden = Factory.Instance.MkId("OtherDecl");
        public static AST<Id> Return_Iden = Factory.Instance.MkId("Return");
        public static AST<Id> Intros_Iden = Factory.Instance.MkId("Intros");
        public static AST<Id> Cast_Iden = Factory.Instance.MkId("Cast");
        public static AST<Id> SizeOf_Iden = Factory.Instance.MkId("SizeOf");
        public static AST<Id> Init_Iden = Factory.Instance.MkId("Init");
        public static AST<Id> FunApp_Iden = Factory.Instance.MkId("FunApp");
        public static AST<Id> StringLit_Iden = Factory.Instance.MkId("StringLit");
        public static AST<Id> RealLit_Iden = Factory.Instance.MkId("RealLit");
        public static AST<Id> IntLit_Iden = Factory.Instance.MkId("IntLit");
        public static AST<Id> BooleanLit_Iden = Factory.Instance.MkId("BooleanLit");
        public static AST<Id> break_Iden = Factory.Instance.MkId("BREAK");
        public static AST<Id> continue_Iden = Factory.Instance.MkId("CONTINUE");
        public static AST<Id> Cases_Iden = Factory.Instance.MkId("Cases");
        public static AST<Id> Cases_PpLine = Factory.Instance.MkId("PpLine");
        public static AST<Id> For_Iden = Factory.Instance.MkId("For");
        public static AST<Id> Block_Iden = Factory.Instance.MkId("Block");
        public static AST<Id> TypeDef_Iden = Factory.Instance.MkId("TypeDef");
        public static AST<Id> BaseType_Iden = Factory.Instance.MkId("BaseType");
        public static AST<Id> QualType_Iden = Factory.Instance.MkId("QualType");
        public static AST<Id> FunType_Iden = Factory.Instance.MkId("FunType");
        public static AST<Id> NmdType_Iden = Factory.Instance.MkId("NmdType");
        public static AST<Id> PrmTypes_Iden = Factory.Instance.MkId("PrmTypes");
        public static AST<Id> EnmType_Iden = Factory.Instance.MkId("EnmType");
        public static AST<Id> DatType_Iden = Factory.Instance.MkId("DatType");
        public static AST<Id> File_Iden = Factory.Instance.MkId("File");
        public static AST<Id> InclDir_Iden = Factory.Instance.MkId("InclDir");
        public static AST<Id> Tops_Iden = Factory.Instance.MkId("Tops");
        #endregion

        //FuncTerms
        #region Function Term nodes
        public static AST<FuncTerm> Comment_FuncTerm = Factory.Instance.MkFuncTerm(Comma_Iden);
        public static AST<FuncTerm> FunDef_FuncTerm = Factory.Instance.MkFuncTerm(FunDef_Iden);
        public static AST<FuncTerm> PrmTypes_FuncTerm = Factory.Instance.MkFuncTerm(PrmTypes_Iden);
        public static AST<FuncTerm> Params_FuncTerm = Factory.Instance.MkFuncTerm(Params_Iden);
        public static AST<FuncTerm> Param_FuncTerm = Factory.Instance.MkFuncTerm(Param_Iden);
        public static AST<FuncTerm> Defs_FuncTerm = Factory.Instance.MkFuncTerm(Defs_Iden);
        public static AST<FuncTerm> VarDef_FuncTerm = Factory.Instance.MkFuncTerm(VarDef_Iden);
        public static AST<FuncTerm> Locals_FuncTerm = Factory.Instance.MkFuncTerm(Locals_Iden, default(Span));
        public static AST<FuncTerm> Iden_FuncTerm = Factory.Instance.MkFuncTerm(Ident_Iden, default(Span));
        public static AST<FuncTerm> StrJmp_FuncTerm = Factory.Instance.MkFuncTerm(StrJmp_Iden, default(Span));
        public static AST<FuncTerm> UnApp_FuncTerm = Factory.Instance.MkFuncTerm(UnApp_Iden, default(Span));
        public static AST<FuncTerm> BinApp_FuncTerm = Factory.Instance.MkFuncTerm(BinApp_Iden, default(Span));
        public static AST<FuncTerm> TerApp_FuncTerm = Factory.Instance.MkFuncTerm(TerApp_Iden, default(Span));
        public static AST<FuncTerm> ParenthExp_FuncTerm = Factory.Instance.MkFuncTerm(Paren_Iden, default(Span));
        public static AST<FuncTerm> Args_FuncTerm = Factory.Instance.MkFuncTerm(Args_Iden, default(Span));
        public static AST<FuncTerm> ITE_FuncTerm = Factory.Instance.MkFuncTerm(ITE_Iden, default(Span));
        public static AST<FuncTerm> Seq_FuncTerm = Factory.Instance.MkFuncTerm(Seq_Iden, default(Span));
        public static AST<FuncTerm> Loop_FuncTerm = Factory.Instance.MkFuncTerm(Loop_Iden, default(Span));
        public static AST<FuncTerm> ArrType_FuncTerm = Factory.Instance.MkFuncTerm(ArrType_Iden, default(Span));
        public static AST<FuncTerm> Switch_FuncTerm = Factory.Instance.MkFuncTerm(Switch_Iden, default(Span));
        public static AST<FuncTerm> Goto_FuncTerm = Factory.Instance.MkFuncTerm(Goto_Iden, default(Span));
        public static AST<FuncTerm> PtrType_FuncTerm = Factory.Instance.MkFuncTerm(PtrType_Iden, default(Span));
        public static AST<FuncTerm> Label_FuncTerm = Factory.Instance.MkFuncTerm(Label_Iden, default(Span));
        public static AST<FuncTerm> OtherDecl_FuncTerm = Factory.Instance.MkFuncTerm(OtherDecl_Iden, default(Span));
        public static AST<FuncTerm> Return_FuncTerm = Factory.Instance.MkFuncTerm(Return_Iden, default(Span));
        public static AST<FuncTerm> Intros_FuncTerm = Factory.Instance.MkFuncTerm(Intros_Iden, default(Span));
        public static AST<FuncTerm> Cast_FuncTerm = Factory.Instance.MkFuncTerm(Cast_Iden, default(Span));
        public static AST<FuncTerm> Sizeof_FuncTerm = Factory.Instance.MkFuncTerm(SizeOf_Iden, default(Span));
        public static AST<FuncTerm> Init_FuncTerm = Factory.Instance.MkFuncTerm(Init_Iden, default(Span));
        public static AST<FuncTerm> FunApp_FuncTerm = Factory.Instance.MkFuncTerm(FunApp_Iden, default(Span));
        public static AST<FuncTerm> StringLit_FuncTerm = Factory.Instance.MkFuncTerm(StringLit_Iden, default(Span));
        public static AST<FuncTerm> BooleanLit_FuncTerm = Factory.Instance.MkFuncTerm(BooleanLit_Iden, default(Span));
        public static AST<FuncTerm> RealLit_FuncTerm = Factory.Instance.MkFuncTerm(RealLit_Iden, default(Span));
        public static AST<FuncTerm> IntLit_FuncTerm = Factory.Instance.MkFuncTerm(IntLit_Iden, default(Span));
        public static AST<FuncTerm> Cases_FuncTerm = Factory.Instance.MkFuncTerm(Cases_Iden, default(Span));
        public static AST<FuncTerm> For_FuncTerm = Factory.Instance.MkFuncTerm(For_Iden, default(Span));
        public static AST<FuncTerm> Block_FuncTerm = Factory.Instance.MkFuncTerm(Block_Iden, default(Span));
        public static AST<FuncTerm> BaseType_FuncTerm = Factory.Instance.MkFuncTerm(BaseType_Iden, default(Span));
        public static AST<FuncTerm> TypeDef_FuncTerm = Factory.Instance.MkFuncTerm(TypeDef_Iden, default(Span));
        public static AST<FuncTerm> ModType_FuncTerm = Factory.Instance.MkFuncTerm(QualType_Iden, default(Span));
        public static AST<FuncTerm> FunType_FuncTerm = Factory.Instance.MkFuncTerm(FunType_Iden, default(Span));
        public static AST<FuncTerm> NmdType_FuncTerm = Factory.Instance.MkFuncTerm(NmdType_Iden, default(Span));
        public static AST<FuncTerm> EnmType_FuncTerm = Factory.Instance.MkFuncTerm(EnmType_Iden, default(Span));
        public static AST<FuncTerm> DatType_FuncTerm = Factory.Instance.MkFuncTerm(DatType_Iden, default(Span));
        #endregion

        public static Dictionary<string, AST<Id>> baseTypes;

        static FormulaNodes()
        {
            baseTypes = new Dictionary<string, AST<Id>>();
            baseTypes["char"] = char_Iden;
            baseTypes["uchar"] = uchar_Iden;
            baseTypes["schar"] = schar_Iden;
            baseTypes["short"] = short_Iden;
            baseTypes["ushort"] = ushort_Iden;
            baseTypes["sshort"] = sshort_Iden;
            baseTypes["int"] = int_Iden;
            baseTypes["sint"] = sint_Iden;
            baseTypes["byte"] = byte_Iden;
            baseTypes["long"] = long_Iden;
            baseTypes["ulong"] = ulong_Iden;
            baseTypes["slong"] = slong_Iden;
            baseTypes["double"] = double_Iden;
            baseTypes["string"] = string_Iden;
            baseTypes["void"] = void_Iden;
            baseTypes["float"] = float_Iden;
            baseTypes["long double"] = ldouble_Iden;
        }
    }
}
