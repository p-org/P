namespace Microsoft.Pc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Formula.API;
    using Microsoft.Formula.API.Nodes;

    public static class PData
    {
        public static readonly AST<Id> Cnst_True = Factory.Instance.MkId("TRUE");
        public static readonly AST<Id> Cnst_False = Factory.Instance.MkId("FALSE");
        public static readonly AST<Id> Cnst_This = Factory.Instance.MkId("THIS");
        public static readonly AST<Id> Cnst_Trigger = Factory.Instance.MkId("TRIGGER");
        public static readonly AST<Id> Cnst_Payload = Factory.Instance.MkId("PAYLOAD");
        public static readonly AST<Id> Cnst_Nondet = Factory.Instance.MkId("NONDET");
        public static readonly AST<Id> Cnst_FairNondet = Factory.Instance.MkId("FAIRNONDET");
        public static readonly AST<Id> Cnst_Null = Factory.Instance.MkId("NULL");
        public static readonly AST<Id> Cnst_Halt = Factory.Instance.MkId("HALT");
        public static readonly AST<Id> Cnst_Pop = Factory.Instance.MkId("POP");
        public static readonly AST<Id> Cnst_Skip = Factory.Instance.MkId("SKIP");

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
        public static readonly AST<Id> Cnst_In = Factory.Instance.MkId("IN");
        public static readonly AST<Id> Cnst_Idx = Factory.Instance.MkId("IDX");
        public static readonly AST<Id> Cnst_Sizeof = Factory.Instance.MkId("SIZEOF");
        public static readonly AST<Id> Cnst_Keys = Factory.Instance.MkId("KEYS");
        public static readonly AST<Id> Cnst_Values = Factory.Instance.MkId("VALUES");
        public static readonly AST<Id> Cnst_Insert = Factory.Instance.MkId("INSERT");
        public static readonly AST<Id> Cnst_Remove = Factory.Instance.MkId("REMOVE");
        public static readonly AST<Id> Cnst_Update = Factory.Instance.MkId("UPDATE");
        public static readonly AST<Id> Cnst_Assign = Factory.Instance.MkId("ASSIGN");

        /// <summary>
        /// Expressions
        /// </summary>
        public static readonly AST<Id> Cnst_Nil = Factory.Instance.MkId("NIL");        
        public static readonly AST<Id> Con_Name = Factory.Instance.MkId("Name");
        public static readonly AST<Id> Con_Exprs = Factory.Instance.MkId("Exprs");
        public static readonly AST<Id> Con_NamedExprs = Factory.Instance.MkId("NamedExprs");
        public static readonly AST<Id> Con_New = Factory.Instance.MkId("New");
        public static readonly AST<Id> Con_FunApp = Factory.Instance.MkId("FunApp");
        public static readonly AST<Id> Con_NulApp = Factory.Instance.MkId("NulApp");
        public static readonly AST<Id> Con_UnApp = Factory.Instance.MkId("UnApp");
        public static readonly AST<Id> Con_BinApp = Factory.Instance.MkId("BinApp");
        public static readonly AST<Id> Con_Field = Factory.Instance.MkId("Field");
        public static readonly AST<Id> Con_Default = Factory.Instance.MkId("Default");
        public static readonly AST<Id> Con_Cast = Factory.Instance.MkId("Cast");
        public static readonly AST<Id> Con_Tuple = Factory.Instance.MkId("Tuple");
        public static readonly AST<Id> Con_NamedTuple = Factory.Instance.MkId("NamedTuple");

        /// <summary>
        /// Statements
        /// </summary>
        public static readonly AST<Id> Con_NewStmt = Factory.Instance.MkId("NewStmt");
        public static readonly AST<Id> Con_Raise = Factory.Instance.MkId("Raise");
        public static readonly AST<Id> Con_Send = Factory.Instance.MkId("Send");
        public static readonly AST<Id> Con_Monitor = Factory.Instance.MkId("Monitor");
        public static readonly AST<Id> Con_FunStmt = Factory.Instance.MkId("FunStmt");
        public static readonly AST<Id> Con_NulStmt = Factory.Instance.MkId("NulStmt");
        public static readonly AST<Id> Con_UnStmt = Factory.Instance.MkId("UnStmt");
        public static readonly AST<Id> Con_BinStmt = Factory.Instance.MkId("BinStmt");
        public static readonly AST<Id> Con_Return = Factory.Instance.MkId("Return");
        public static readonly AST<Id> Con_While = Factory.Instance.MkId("While");
        public static readonly AST<Id> Con_Ite = Factory.Instance.MkId("Ite");
        public static readonly AST<Id> Con_Seq = Factory.Instance.MkId("Seq");
        public static readonly AST<Id> Con_Receive = Factory.Instance.MkId("Receive");

        //// Types
        public static readonly AST<Id> Cnst_Bool = Factory.Instance.MkId("BOOL");
        public static readonly AST<Id> Cnst_Int = Factory.Instance.MkId("INT");
        public static readonly AST<Id> Cnst_Any = Factory.Instance.MkId("ANY");
        public static readonly AST<Id> Con_BaseType = Factory.Instance.MkId("BaseType");
        public static readonly AST<Id> Con_TupType = Factory.Instance.MkId("TupType");
        public static readonly AST<Id> Con_NamedTupType = Factory.Instance.MkId("NamedTupType");
        public static readonly AST<Id> Con_SeqType = Factory.Instance.MkId("SeqType");
        public static readonly AST<Id> Con_MapType = Factory.Instance.MkId("MapType");
        public static readonly AST<Id> Con_NmdTupTypeField = Factory.Instance.MkId("NmdTupTypeField");

        //// Machine declarations
        public static readonly AST<Id> Con_MachType = Factory.Instance.MkId("MachType");
        public static readonly AST<Id> Con_MachineDecl = Factory.Instance.MkId("MachineDecl");
        public static readonly AST<Id> Con_EventDecl = Factory.Instance.MkId("EventDecl");
        public static readonly AST<Id> Con_StateDecl = Factory.Instance.MkId("StateDecl");
        public static readonly AST<Id> Con_ExitFun = Factory.Instance.MkId("ExitFun");
        public static readonly AST<Id> Con_VarDecl = Factory.Instance.MkId("VarDecl");
        public static readonly AST<Id> Con_TransDecl = Factory.Instance.MkId("TransDecl");
        public static readonly AST<Id> Con_Install = Factory.Instance.MkId("Install");
        public static readonly AST<Id> Con_FunDecl = Factory.Instance.MkId("FunDecl");
        public static readonly AST<Id> Con_Params = Factory.Instance.MkId("Params");
        public static readonly AST<Id> Con_StateSetDecl = Factory.Instance.MkId("StateSetDecl");
        public static readonly AST<Id> Con_InStateSet = Factory.Instance.MkId("InStateSet");
        public static readonly AST<Id> Con_EventSetDecl = Factory.Instance.MkId("EventSetDecl");
        public static readonly AST<Id> Con_InEventSet = Factory.Instance.MkId("InEventSet");
    }
}
