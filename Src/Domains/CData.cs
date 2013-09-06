namespace DemoCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Formula.API;
    using Microsoft.Formula.API.Nodes;

    public static class CData
    {
        public static AST<Id> Cnst_True(Span span = default(Span)) { return Factory.Instance.MkId("TRUE", span); }
        public static AST<Id> Cnst_False(Span span = default(Span)) { return Factory.Instance.MkId("FALSE", span); }
        public static AST<Id> Cnst_Nil(Span span = default(Span)) { return Factory.Instance.MkId("NIL", span); }
        public static AST<Id> Cnst_Ellipse(Span span = default(Span)) { return Factory.Instance.MkId("ELLIPSE", span); }
        public static AST<Id> Cnst_Unknown(Span span = default(Span)) { return Factory.Instance.MkId("UNKNOWN", span); }

        public static AST<Id> Cnst_Extern(Span span = default(Span)) { return Factory.Instance.MkId("EXTERN", span); }
        public static AST<Id> Cnst_Static(Span span = default(Span)) { return Factory.Instance.MkId("STATIC", span); }
        public static AST<Id> Cnst_Auto(Span span = default(Span)) { return Factory.Instance.MkId("AUTO", span); }
        public static AST<Id> Cnst_Register(Span span = default(Span)) { return Factory.Instance.MkId("REGISTER", span); }

        public static AST<Id> Cnst_Struct(Span span = default(Span)) { return Factory.Instance.MkId("STRUCT", span); }
        public static AST<Id> Cnst_Enum(Span span = default(Span)) { return Factory.Instance.MkId("ENUM", span); }
        public static AST<Id> Cnst_Char(Span span = default(Span)) { return Factory.Instance.MkId("CHAR", span); }
        public static AST<Id> Cnst_Int(Span span = default(Span)) { return Factory.Instance.MkId("INT", span); }
        public static AST<Id> Cnst_Oct(Span span = default(Span)) { return Factory.Instance.MkId("OCT", span); }
        public static AST<Id> Cnst_Dec(Span span = default(Span)) { return Factory.Instance.MkId("DEC", span); }
        public static AST<Id> Cnst_Hex(Span span = default(Span)) { return Factory.Instance.MkId("HEX", span); }

        public static AST<Id> Cnst_Unsigned(Span span = default(Span)) { return Factory.Instance.MkId("U", span); }
        public static AST<Id> Cnst_Long(Span span = default(Span)) { return Factory.Instance.MkId("L", span); }
        public static AST<Id> Cnst_UnsignedLong(Span span = default(Span)) { return Factory.Instance.MkId("UL", span); }
        public static AST<Id> Cnst_Void(Span span = default(Span)) { return Factory.Instance.MkId("VOID", span); }

        public static AST<Id> Cnst_LNot(Span span = default(Span)) { return Factory.Instance.MkId("LNOT", span); }
        public static AST<Id> Cnst_Neg(Span span = default(Span)) { return Factory.Instance.MkId("NEG", span); }
        public static AST<Id> Cnst_Add(Span span = default(Span)) { return Factory.Instance.MkId("ADD", span); }
        public static AST<Id> Cnst_Sub(Span span = default(Span)) { return Factory.Instance.MkId("SUB", span); }
        public static AST<Id> Cnst_Mul(Span span = default(Span)) { return Factory.Instance.MkId("MUL", span); }
        public static AST<Id> Cnst_Div(Span span = default(Span)) { return Factory.Instance.MkId("DIV", span); }
        public static AST<Id> Cnst_LAnd(Span span = default(Span)) { return Factory.Instance.MkId("LAND", span); }
        public static AST<Id> Cnst_LOr(Span span = default(Span)) { return Factory.Instance.MkId("LOR", span); }
        public static AST<Id> Cnst_Eq(Span span = default(Span)) { return Factory.Instance.MkId("EQ", span); }
        public static AST<Id> Cnst_NEq(Span span = default(Span)) { return Factory.Instance.MkId("NEQ", span); }
        public static AST<Id> Cnst_Lt(Span span = default(Span)) { return Factory.Instance.MkId("LT", span); }
        public static AST<Id> Cnst_Le(Span span = default(Span)) { return Factory.Instance.MkId("LE", span); }
        public static AST<Id> Cnst_Gt(Span span = default(Span)) { return Factory.Instance.MkId("GT", span); }
        public static AST<Id> Cnst_Ge(Span span = default(Span)) { return Factory.Instance.MkId("GE", span); }
        public static AST<Id> Cnst_Asn(Span span = default(Span)) { return Factory.Instance.MkId("ASN", span); }
        public static AST<Id> Cnst_PFld(Span span = default(Span)) { return Factory.Instance.MkId("PFLD", span); }
        public static AST<Id> Cnst_Fld(Span span = default(Span)) { return Factory.Instance.MkId("FLD", span); }
        public static AST<Id> Cnst_AAc(Span span = default(Span)) { return Factory.Instance.MkId("AAC", span); }
        public static AST<Id> Cnst_Addr(Span span = default(Span)) { return Factory.Instance.MkId("ADDR", span); }
        public static AST<Id> Cnst_Bor(Span span = default(Span)) { return Factory.Instance.MkId("BOR", span); }
        public static AST<Id> Cnst_Band(Span span = default(Span)) { return Factory.Instance.MkId("BAND", span); }
        public static AST<Id> Cnst_Bxor(Span span = default(Span)) { return Factory.Instance.MkId("BXOR", span); }

        public static AST<Id> Cnst_Drf(Span span = default(Span)) { return Factory.Instance.MkId("DRF", span); }
        public static AST<Id> Cnst_While(Span span = default(Span)) { return Factory.Instance.MkId("WHILE", span); }
        public static AST<Id> Cnst_Do(Span span = default(Span)) { return Factory.Instance.MkId("DO", span); }

        public static AST<Id> Cnst_If(Span span = default(Span)) { return Factory.Instance.MkId("IF", span); }
        public static AST<Id> Cnst_IfDef(Span span = default(Span)) { return Factory.Instance.MkId("IFDEF", span); }
        public static AST<Id> Cnst_IfNdef(Span span = default(Span)) { return Factory.Instance.MkId("IFNDEF", span); }
        public static AST<Id> Cnst_Default(Span span = default(Span)) { return Factory.Instance.MkId("DEFAULT", span); }
        public static AST<Id> Cnst_Break(Span span = default(Span)) { return Factory.Instance.MkId("BREAK", span); }
        public static AST<Id> Cnst_Const(Span span = default(Span)) { return Factory.Instance.MkId("CONST", span); }

        /// <summary>
        /// Constructor names
        /// </summary>
        public static AST<Id> Con_Comment(Span span = default(Span)) { return Factory.Instance.MkId("Comment", span); }
        public static AST<Id> Con_EnmDef(Span span = default(Span)) { return Factory.Instance.MkId("EnmDef", span); }
        public static AST<Id> Con_Elements(Span span = default(Span)) { return Factory.Instance.MkId("Elements", span); }
        public static AST<Id> Con_IntLit(Span span = default(Span)) { return Factory.Instance.MkId("IntLit", span); }
        public static AST<Id> Con_StringLit(Span span = default(Span)) { return Factory.Instance.MkId("StringLit", span); }
        public static AST<Id> Con_Ident(Span span = default(Span)) { return Factory.Instance.MkId("Ident", span); }
        public static AST<Id> Con_VarDef(Span span = default(Span)) { return Factory.Instance.MkId("VarDef", span); }
        public static AST<Id> Con_FunDef(Span span = default(Span)) { return Factory.Instance.MkId("FunDef", span); }
        public static AST<Id> Con_Fields(Span span = default(Span)) { return Factory.Instance.MkId("Fields", span); }
        public static AST<Id> Con_DatDef(Span span = default(Span)) { return Factory.Instance.MkId("DatDef", span); }

        public static AST<Id> Con_BaseType(Span span = default(Span)) { return Factory.Instance.MkId("BaseType", span); }
        public static AST<Id> Con_NmdType(Span span = default(Span)) { return Factory.Instance.MkId("NmdType", span); }
        public static AST<Id> Con_QualType(Span span = default(Span)) { return Factory.Instance.MkId("QualType", span); }
        public static AST<Id> Con_PtrType(Span span = default(Span)) { return Factory.Instance.MkId("PtrType", span); }
        public static AST<Id> Con_ArrType(Span span = default(Span)) { return Factory.Instance.MkId("ArrType", span); }
        public static AST<Id> Con_FunType(Span span = default(Span)) { return Factory.Instance.MkId("FunType", span); }
        public static AST<Id> Con_Params(Span span = default(Span)) { return Factory.Instance.MkId("Params", span); }
        public static AST<Id> Con_Block(Span span = default(Span)) { return Factory.Instance.MkId("Block", span); }
        public static AST<Id> Con_Lbl(Span span = default(Span)) { return Factory.Instance.MkId("Lbl", span); }
        public static AST<Id> Con_Goto(Span span = default(Span)) { return Factory.Instance.MkId("Goto", span); }
        public static AST<Id> Con_Cast(Span span = default(Span)) { return Factory.Instance.MkId("Cast", span); }
        public static AST<Id> Con_Sizeof(Span span = default(Span)) { return Factory.Instance.MkId("SizeOf", span); }

        public static AST<Id> Con_PrmTypes(Span span = default(Span)) { return Factory.Instance.MkId("PrmTypes", span); }
        public static AST<Id> Con_Init(Span span = default(Span)) { return Factory.Instance.MkId("Init", span); }
        public static AST<Id> Con_Args(Span span = default(Span)) { return Factory.Instance.MkId("Args", span); }
        public static AST<Id> Con_FunApp(Span span = default(Span)) { return Factory.Instance.MkId("FunApp", span); }

        public static AST<Id> Con_Loop(Span span = default(Span)) { return Factory.Instance.MkId("Loop", span); }
        public static AST<Id> Con_ITE(Span span = default(Span)) { return Factory.Instance.MkId("ITE", span); }
        public static AST<Id> Con_Seq(Span span = default(Span)) { return Factory.Instance.MkId("Seq", span); }
        public static AST<Id> Con_Return(Span span = default(Span)) { return Factory.Instance.MkId("Return", span); }
        public static AST<Id> Con_UnApp(Span span = default(Span)) { return Factory.Instance.MkId("UnApp", span); }
        public static AST<Id> Con_BinApp(Span span = default(Span)) { return Factory.Instance.MkId("BinApp", span); }
        public static AST<Id> Con_Switch(Span span = default(Span)) { return Factory.Instance.MkId("Switch", span); }
        public static AST<Id> Con_Cases(Span span = default(Span)) { return Factory.Instance.MkId("Cases", span); }
        public static AST<Id> Con_StrJmp(Span span = default(Span)) { return Factory.Instance.MkId("StrJmp", span); }
        
        public static AST<Id> Con_Defs(Span span = default(Span)) { return Factory.Instance.MkId("Defs", span); }
        public static AST<Id> Con_PpInclude(Span span = default(Span)) { return Factory.Instance.MkId("PpInclude", span); }
        public static AST<Id> Con_PpPragma(Span span = default(Span)) { return Factory.Instance.MkId("PpPragma", span); }
        public static AST<Id> Con_PpDefine(Span span = default(Span)) { return Factory.Instance.MkId("PpDefine", span); }
        public static AST<Id> Con_PpITE(Span span = default(Span)) { return Factory.Instance.MkId("PpITE", span); }
        public static AST<Id> Con_Section(Span span = default(Span)) { return Factory.Instance.MkId("Section", span); }
        public static AST<Id> Con_File(Span span = default(Span)) { return Factory.Instance.MkId("File", span); }

        /// <summary>
        /// Empty applications
        /// </summary>
        public static AST<FuncTerm> App_Comment(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Comment(span), span); }
        public static AST<FuncTerm> App_EnmDef(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_EnmDef(span), span); }
        public static AST<FuncTerm> App_Elements(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Elements(span), span); }
        public static AST<FuncTerm> App_IntLit(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_IntLit(span), span); }
        public static AST<FuncTerm> App_StringLit(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_StringLit(span), span); }
        public static AST<FuncTerm> App_Ident(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Ident(span), span); }
        public static AST<FuncTerm> App_VarDef(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_VarDef(span), span); }
        public static AST<FuncTerm> App_Fields(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Fields(span), span); }
        public static AST<FuncTerm> App_FunDef(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_FunDef(span), span); }
        public static AST<FuncTerm> App_DataDef(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_DatDef(span), span); }
        public static AST<FuncTerm> App_Params(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Params(span), span); }
        public static AST<FuncTerm> App_BaseType(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_BaseType(span), span); }
        public static AST<FuncTerm> App_NmdType(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_NmdType(span), span); }
        public static AST<FuncTerm> App_QualType(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_QualType(span), span); }
        public static AST<FuncTerm> App_PtrType(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_PtrType(span), span); }
        public static AST<FuncTerm> App_ArrType(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_ArrType(span), span); }
        public static AST<FuncTerm> App_FunType(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_FunType(span), span); }
        public static AST<FuncTerm> App_PrmTypes(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_PrmTypes(span), span); }
        public static AST<FuncTerm> App_Init(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Init(span), span); }
        public static AST<FuncTerm> App_Args(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Args(span), span); }
        public static AST<FuncTerm> App_FunApp(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_FunApp(span), span); }
        public static AST<FuncTerm> App_Block(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Block(span), span); }
        public static AST<FuncTerm> App_Lbl(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Lbl(span), span); }
        public static AST<FuncTerm> App_Goto(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Goto(span), span); }
        public static AST<FuncTerm> App_Cast(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Cast(span), span); }
        public static AST<FuncTerm> App_Sizeof(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Sizeof(span), span); }

        public static AST<FuncTerm> App_Loop(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Loop(span), span); }
        public static AST<FuncTerm> App_ITE(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_ITE(span), span); }
        public static AST<FuncTerm> App_Return(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Return(span), span); }
        public static AST<FuncTerm> App_Seq(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Seq(span), span); }
        public static AST<FuncTerm> App_UnApp(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_UnApp(span), span); }
        public static AST<FuncTerm> App_BinApp(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_BinApp(span), span); }
        public static AST<FuncTerm> App_Switch(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Switch(span), span); }
        public static AST<FuncTerm> App_Cases(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Cases(span), span); }
        public static AST<FuncTerm> App_StrJmp(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_StrJmp(span), span); }

        public static AST<FuncTerm> App_Defs(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Defs(span), span); }
        public static AST<FuncTerm> App_Section(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_Section(span), span); }
        public static AST<FuncTerm> App_File(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_File(span), span); }
        public static AST<FuncTerm> App_PpPragma(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_PpPragma(span), span); }
        public static AST<FuncTerm> App_PpInclude(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_PpInclude(span), span); }
        public static AST<FuncTerm> App_PpDefine(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_PpDefine(span), span); }
        public static AST<FuncTerm> App_PpITE(Span span = default(Span)) { return Factory.Instance.MkFuncTerm(Con_PpITE(span), span); }

        public static AST<FuncTerm> Trm_PragmaOnce(Span span = default(Span))
        {
            return Factory.Instance.AddArg(
                    App_PpPragma(span),
                    Factory.Instance.AddArg(App_Ident(span), Factory.Instance.MkCnst("once")));
        }

        public static AST<FuncTerm> Trm_This(Span span = default(Span))
        {
            return Factory.Instance.AddArg(
               Factory.Instance.AddArg(
                   Factory.Instance.AddArg(CData.App_BinApp(span), CData.Cnst_PFld(span)),
                   Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst("Context"))),
               Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst("This")));
        }

        public static AST<FuncTerm> Trm_Trigger(Span span = default(Span))
        {
            var ctxt = 
                Factory.Instance.AddArg(
                    Factory.Instance.AddArg(
                        Factory.Instance.AddArg(CData.App_BinApp(span), CData.Cnst_PFld(span)),
                        Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst("Context"))),
                    Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst("Trigger")));
            return
                Factory.Instance.AddArg(
                    Factory.Instance.AddArg(
                        Factory.Instance.AddArg(CData.App_BinApp(span), CData.Cnst_Fld(span)),
                        ctxt),
                    Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst("Event")));
        }

        public static AST<FuncTerm> Trm_Arg(Span span = default(Span))
        {
            var ctxt =
                Factory.Instance.AddArg(
                    Factory.Instance.AddArg(
                        Factory.Instance.AddArg(CData.App_BinApp(span), CData.Cnst_PFld(span)),
                        Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst("Context"))),
                    Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst("Trigger")));
            return
                Factory.Instance.AddArg(
                    Factory.Instance.AddArg(
                        Factory.Instance.AddArg(CData.App_BinApp(span), CData.Cnst_Fld(span)),
                        ctxt),
                    Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst("Arg")));       
        }
    }
}
