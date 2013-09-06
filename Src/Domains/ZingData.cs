namespace DemoCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    using Microsoft.Formula.API;
    using Microsoft.Formula.API.Nodes;

    public static class ZingData
    {
        public static readonly AST<Cnst> Cnst_SmEvent = Factory.Instance.MkCnst("SM_EVENT");
        public static readonly AST<Cnst> Cnst_SmEventSet = Factory.Instance.MkCnst("SM_EVENT_SET");
        public static readonly AST<Cnst> Cnst_SmUnion = Factory.Instance.MkCnst("SM_ARG_UNION");
        public static readonly AST<Cnst> Cnst_SmHandle = Factory.Instance.MkCnst("SM_HANDLE");


        public static readonly AST<Id> Cnst_Bool = Factory.Instance.MkId("BOOL");
        public static readonly AST<Id> Cnst_Int = Factory.Instance.MkId("INT");
        public static readonly AST<Id> Cnst_Void = Factory.Instance.MkId("VOID");
        public static readonly AST<Id> Cnst_True = Factory.Instance.MkId("TRUE");
        public static readonly AST<Id> Cnst_False = Factory.Instance.MkId("FALSE");
        public static readonly AST<Id> Cnst_Async = Factory.Instance.MkId("ASYNC");
        public static readonly AST<Id> Cnst_Activate = Factory.Instance.MkId("ACTIVATE");
        public static readonly AST<Id> Cnst_Static = Factory.Instance.MkId("STATIC");
        public static readonly AST<Id> Cnst_Yield = Factory.Instance.MkId("YIELD");

        public static readonly AST<Id> Cnst_Not = Factory.Instance.MkId("NOT");
        public static readonly AST<Id> Cnst_Neg = Factory.Instance.MkId("NEG");
        public static readonly AST<Id> Cnst_Add = Factory.Instance.MkId("ADD");
        public static readonly AST<Id> Cnst_Sub = Factory.Instance.MkId("SUB");
        public static readonly AST<Id> Cnst_Mul = Factory.Instance.MkId("MUL");
        public static readonly AST<Id> Cnst_IntDiv = Factory.Instance.MkId("INTDIV");
        public static readonly AST<Id> Cnst_And = Factory.Instance.MkId("AND");
        public static readonly AST<Id> Cnst_Or = Factory.Instance.MkId("OR");
        public static readonly AST<Id> Cnst_Eq = Factory.Instance.MkId("EQ");
        public static readonly AST<Id> Cnst_NEq = Factory.Instance.MkId("NEQ");
        public static readonly AST<Id> Cnst_Lt = Factory.Instance.MkId("LT");
        public static readonly AST<Id> Cnst_Le = Factory.Instance.MkId("LE");
        public static readonly AST<Id> Cnst_Gt = Factory.Instance.MkId("GT");
        public static readonly AST<Id> Cnst_Ge = Factory.Instance.MkId("GE");
        public static readonly AST<Id> Cnst_Dot = Factory.Instance.MkId("DOT");
        public static readonly AST<Id> Cnst_In = Factory.Instance.MkId("IN");
        public static readonly AST<Id> Cnst_Index = Factory.Instance.MkId("INDEX");
        public static readonly AST<Id> Cnst_Nil = Factory.Instance.MkId("NIL");

        /// <summary>
        /// Constructor names
        /// </summary>
        //// Expressions
        public static readonly AST<Id> Con_Identifier = Factory.Instance.MkId("Identifier");
        public static readonly AST<Id> Con_Apply = Factory.Instance.MkId("Apply");
        public static readonly AST<Id> Con_Args = Factory.Instance.MkId("Args");
        public static readonly AST<Id> Con_Expr = Factory.Instance.MkId("Expr");
        public static readonly AST<Id> Con_Call = Factory.Instance.MkId("Call");
        public static readonly AST<Id> Con_New = Factory.Instance.MkId("New");

        /// <summary>
        /// Attributes
        /// </summary>
        public static readonly AST<Id> Con_Attrs = Factory.Instance.MkId("Attrs");

        //// Statements
        public static readonly AST<Id> Con_Return = Factory.Instance.MkId("Return");
        public static readonly AST<Id> Con_Assert = Factory.Instance.MkId("Assert");
        public static readonly AST<Id> Con_Assume = Factory.Instance.MkId("Assume");
        public static readonly AST<Id> Con_CallStmt = Factory.Instance.MkId("CallStmt");
        public static readonly AST<Id> Con_Assign = Factory.Instance.MkId("Assign");
        public static readonly AST<Id> Con_Goto = Factory.Instance.MkId("Goto");
        public static readonly AST<Id> Con_ITE = Factory.Instance.MkId("ITE");
        public static readonly AST<Id> Con_While = Factory.Instance.MkId("While");
        public static readonly AST<Id> Con_Seq = Factory.Instance.MkId("Seq");
        public static readonly AST<Id> Con_Stmt = Factory.Instance.MkId("Stmt");
        public static readonly AST<Id> Con_LabelStmt = Factory.Instance.MkId("LabelStmt");
        public static readonly AST<Id> Con_Blocks = Factory.Instance.MkId("Blocks");
        public static readonly AST<Id> Con_ClassDecl = Factory.Instance.MkId("ClassDecl");
        public static readonly AST<Id> Con_VarDecl = Factory.Instance.MkId("VarDecl");
        public static readonly AST<Id> Con_VarDecls = Factory.Instance.MkId("VarDecls");
        public static readonly AST<Id> Con_MethodDecl = Factory.Instance.MkId("MethodDecl");
        public static readonly AST<Id> Con_MethodDecls = Factory.Instance.MkId("MethodDecls");
        public static readonly AST<Id> Con_EnumDecl = Factory.Instance.MkId("EnumDecl");
        public static readonly AST<Id> Con_EnumElems = Factory.Instance.MkId("EnumElems");
        public static readonly AST<Id> Con_Decls = Factory.Instance.MkId("Decls");
        public static readonly AST<Id> Con_File = Factory.Instance.MkId("File");

        /// <summary>
        /// Empty applications
        /// </summary>
        //// Expressions
        public static readonly AST<FuncTerm> App_Identifier = Factory.Instance.MkFuncTerm(Con_Identifier);
        public static readonly AST<FuncTerm> App_Apply = Factory.Instance.MkFuncTerm(Con_Apply);
        public static readonly AST<FuncTerm> App_Args = Factory.Instance.MkFuncTerm(Con_Args);
        public static readonly AST<FuncTerm> App_New = Factory.Instance.MkFuncTerm(Con_New);
        public static readonly AST<FuncTerm> App_Call = Factory.Instance.MkFuncTerm(Con_Call);

        public static readonly AST<FuncTerm> App_Attrs = Factory.Instance.MkFuncTerm(Con_Attrs);

        //// Statements
        public static readonly AST<FuncTerm> App_Return = Factory.Instance.MkFuncTerm(Con_Return);
        public static readonly AST<FuncTerm> App_Assert = Factory.Instance.MkFuncTerm(Con_Assert);
        public static readonly AST<FuncTerm> App_Assume = Factory.Instance.MkFuncTerm(Con_Assume);
        public static readonly AST<FuncTerm> App_CallStmt = Factory.Instance.MkFuncTerm(Con_CallStmt);
        public static readonly AST<FuncTerm> App_Assign = Factory.Instance.MkFuncTerm(Con_Assign);
        public static readonly AST<FuncTerm> App_Goto = Factory.Instance.MkFuncTerm(Con_Goto);
        public static readonly AST<FuncTerm> App_ITE = Factory.Instance.MkFuncTerm(Con_ITE);
        public static readonly AST<FuncTerm> App_While = Factory.Instance.MkFuncTerm(Con_While);
        public static readonly AST<FuncTerm> App_Seq = Factory.Instance.MkFuncTerm(Con_Seq);
        public static readonly AST<FuncTerm> App_Stmt = Factory.Instance.MkFuncTerm(Con_Stmt);
        public static readonly AST<FuncTerm> App_LabelStmt = Factory.Instance.MkFuncTerm(Con_LabelStmt);
        public static readonly AST<FuncTerm> App_Blocks = Factory.Instance.MkFuncTerm(Con_Blocks);

        public static readonly AST<FuncTerm> App_EnumDecl = Factory.Instance.MkFuncTerm(Con_EnumDecl);
        public static readonly AST<FuncTerm> App_EnumElems = Factory.Instance.MkFuncTerm(Con_EnumElems);
        public static readonly AST<FuncTerm> App_VarDecl = Factory.Instance.MkFuncTerm(Con_VarDecl);
        public static readonly AST<FuncTerm> App_VarDecls = Factory.Instance.MkFuncTerm(Con_VarDecls);
        public static readonly AST<FuncTerm> App_MethodDecl = Factory.Instance.MkFuncTerm(Con_MethodDecl);
        public static readonly AST<FuncTerm> App_MethodDecls = Factory.Instance.MkFuncTerm(Con_MethodDecls);
        public static readonly AST<FuncTerm> App_ClassDecl = Factory.Instance.MkFuncTerm(Con_ClassDecl);
        public static readonly AST<FuncTerm> App_Decls = Factory.Instance.MkFuncTerm(Con_Decls);
        public static readonly AST<FuncTerm> App_File = Factory.Instance.MkFuncTerm(Con_File);

        static ZingData()
        {
            
        }

        public static AST<Node> pTypeToZingType(string pType)
        {
            if (pType == "NIL")
                return ZingData.Cnst_Void;
            if (pType == "BOOL")
                return ZingData.Cnst_Bool;
            if (pType == "INT")
                return ZingData.Cnst_Int;
            if (pType == "EVENT")
                return Factory.Instance.MkCnst("SM_EVENT");
            if (pType == "ID")
                return Factory.Instance.MkCnst("SM_HANDLE");
            throw new InvalidOperationException();
        }
    }
}