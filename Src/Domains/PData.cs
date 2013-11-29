namespace PCompiler
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
        private static readonly Dictionary<string, Tuple<AST<Id>, int>> pOpToC = 
            new Dictionary<string, Tuple<AST<Id>, int>>();

        private static readonly Dictionary<string, Tuple<AST<Id>, int>> pOpToZing =
            new Dictionary<string, Tuple<AST<Id>, int>>();

        public static readonly AST<Id> Cnst_State = Factory.Instance.MkId("STATE");
        public static readonly AST<Id> Cnst_Event = Factory.Instance.MkId("EVENT");
        public static readonly AST<Id> Cnst_Var = Factory.Instance.MkId("VAR");
        public static readonly AST<Id> Cnst_Field = Factory.Instance.MkId("FIELD");
        public static readonly AST<Id> Cnst_This = Factory.Instance.MkId("THIS");
        public static readonly AST<Id> Cnst_Trigger = Factory.Instance.MkId("TRIGGER");
        public static readonly AST<Id> Cnst_Nondet = Factory.Instance.MkId("NONDET");
        public static readonly AST<Id> Cnst_True = Factory.Instance.MkId("TRUE");
        public static readonly AST<Id> Cnst_False = Factory.Instance.MkId("FALSE");
        public static readonly AST<Id> Cnst_Delete = Factory.Instance.MkId("DELETE");
        public static readonly AST<Id> Cnst_Leave = Factory.Instance.MkId("LEAVE");
        public static readonly AST<Id> Cnst_Default = Factory.Instance.MkId("DEFAULT");
        public static readonly AST<Id> Cnst_Null = Factory.Instance.MkId("NULL");

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
        public static readonly AST<Id> Cnst_Fld = Factory.Instance.MkId("FLD");
        public static readonly AST<Id> Cnst_Sizeof = Factory.Instance.MkId("SIZEOF");
        public static readonly AST<Id> Cnst_Keys = Factory.Instance.MkId("KEYS");

        public static readonly AST<Id> Cnst_Nil = Factory.Instance.MkId("NIL");
        public static readonly AST<Id> Cnst_Bool = Factory.Instance.MkId("BOOL");
        public static readonly AST<Id> Cnst_Int = Factory.Instance.MkId("INT");
        public static readonly AST<Id> Cnst_Id = Factory.Instance.MkId("ID");
        public static readonly AST<Id> Cnst_Mid = Factory.Instance.MkId("MID");
        public static readonly AST<Id> Cnst_Sid = Factory.Instance.MkId("SID");
        public static readonly AST<Id> Cnst_Any = Factory.Instance.MkId("ANY");

        public static readonly AST<Id> Cnst_Insert = Factory.Instance.MkId("INSERT");
        public static readonly AST<Id> Cnst_Remove = Factory.Instance.MkId("REMOVE");
        public static readonly AST<Id> Cnst_Update = Factory.Instance.MkId("UPDATE");

        /// <summary>
        /// Constructor names
        /// </summary>
        //// Expressions
        public static readonly AST<Id> Con_Payload = Factory.Instance.MkId("Payload");
        public static readonly AST<Id> Con_Use = Factory.Instance.MkId("Use");
        public static readonly AST<Id> Con_Apply = Factory.Instance.MkId("Apply");
        public static readonly AST<Id> Con_Exprs = Factory.Instance.MkId("Exprs");
        public static readonly AST<Id> Con_Strings = Factory.Instance.MkId("Strings");
        public static readonly AST<Id> Con_NamedExprs = Factory.Instance.MkId("NamedExprs");
        public static readonly AST<Id> Con_New = Factory.Instance.MkId("New");
        public static readonly AST<Id> Con_Call = Factory.Instance.MkId("Call");

        //// Types
        public static readonly AST<Id> Con_TypeTuple = Factory.Instance.MkId("TypeTuple");
        public static readonly AST<Id> Con_TypeField = Factory.Instance.MkId("TypeField");
        public static readonly AST<Id> Con_TypeNamedTuple = Factory.Instance.MkId("TypeNamedTuple");
        public static readonly AST<Id> Con_TypeSeq = Factory.Instance.MkId("TypeSeq");
        public static readonly AST<Id> Con_TypeMap = Factory.Instance.MkId("TypeMap");

        //// Statements
        public static readonly AST<Id> Con_MachType = Factory.Instance.MkId("MachType");
        public static readonly AST<Id> Con_Assert = Factory.Instance.MkId("Assert");
        public static readonly AST<Id> Con_Return = Factory.Instance.MkId("Return");
        public static readonly AST<Id> Con_Assign = Factory.Instance.MkId("Assign");
        public static readonly AST<Id> Con_Send = Factory.Instance.MkId("Send");
        public static readonly AST<Id> Con_Raise = Factory.Instance.MkId("Raise");
        public static readonly AST<Id> Con_Scall = Factory.Instance.MkId("Scall");
        public static readonly AST<Id> Con_Ecall = Factory.Instance.MkId("Ecall");
        public static readonly AST<Id> Con_Tuple = Factory.Instance.MkId("Tuple");
        public static readonly AST<Id> Con_NamedTuple = Factory.Instance.MkId("NamedTuple");
        public static readonly AST<Id> Con_ITE = Factory.Instance.MkId("ITE");
        public static readonly AST<Id> Con_DataOp = Factory.Instance.MkId("DataOp");
        public static readonly AST<Id> Con_While = Factory.Instance.MkId("While");
        public static readonly AST<Id> Con_Seq = Factory.Instance.MkId("Seq");
        public static readonly AST<Id> Con_MachineDecl = Factory.Instance.MkId("MachineDecl");
        public static readonly AST<Id> Con_EventDecl = Factory.Instance.MkId("EventDecl");
        public static readonly AST<Id> Con_StateDecl = Factory.Instance.MkId("StateDecl");
        public static readonly AST<Id> Con_ExitFun = Factory.Instance.MkId("ExitFun");
        public static readonly AST<Id> Con_VarDecl = Factory.Instance.MkId("VarDecl");
        public static readonly AST<Id> Con_TransDecl = Factory.Instance.MkId("TransDecl");
        public static readonly AST<Id> Con_ActionDecl = Factory.Instance.MkId("ActionDecl");
        public static readonly AST<Id> Con_Install = Factory.Instance.MkId("Install");
        public static readonly AST<Id> Con_FunDecl = Factory.Instance.MkId("FunDecl");
        public static readonly AST<Id> Con_Params = Factory.Instance.MkId("Params");
        public static readonly AST<Id> Con_StateSetDecl = Factory.Instance.MkId("StateSetDecl");
        public static readonly AST<Id> Con_InStateSet = Factory.Instance.MkId("InStateSet");
        public static readonly AST<Id> Con_EventSetDecl = Factory.Instance.MkId("EventSetDecl");
        public static readonly AST<Id> Con_InEventSet = Factory.Instance.MkId("InEventSet");

        public static readonly AST<Id> Con_MachStart = Factory.Instance.MkId("MachStart");
        public static readonly AST<Id> Con_CanReceive = Factory.Instance.MkId("CanReceive");
        public static readonly AST<Id> Con_CanRaise = Factory.Instance.MkId("CanRaise");

        public static readonly AST<Id> Con_Flags = Factory.Instance.MkId("Flags");
        public static readonly AST<Id> Con_File = Factory.Instance.MkId("File");
        public static readonly AST<Id> Con_Fair = Factory.Instance.MkId("Fair");
        public static readonly AST<Id> Con_Stable = Factory.Instance.MkId("Stable");

        /// <summary>
        /// Empty applications
        /// </summary>
        //// Expressions
        public static readonly AST<FuncTerm> App_Payload = Factory.Instance.MkFuncTerm(Con_Payload);
        public static readonly AST<FuncTerm> App_Use = Factory.Instance.MkFuncTerm(Con_Use);
        public static readonly AST<FuncTerm> App_Apply = Factory.Instance.MkFuncTerm(Con_Apply);
        public static readonly AST<FuncTerm> App_Strings = Factory.Instance.MkFuncTerm(Con_Strings);
        public static readonly AST<FuncTerm> App_Exprs = Factory.Instance.MkFuncTerm(Con_Exprs);
        public static readonly AST<FuncTerm> App_Inits = Factory.Instance.MkFuncTerm(Con_NamedExprs);
        public static readonly AST<FuncTerm> App_New = Factory.Instance.MkFuncTerm(Con_New);
        public static readonly AST<FuncTerm> App_Call = Factory.Instance.MkFuncTerm(Con_Call);

        //// Statements
        public static readonly AST<FuncTerm> App_Assert = Factory.Instance.MkFuncTerm(Con_Assert);
        public static readonly AST<FuncTerm> App_Return = Factory.Instance.MkFuncTerm(Con_Return);
        public static readonly AST<FuncTerm> App_Assign = Factory.Instance.MkFuncTerm(Con_Assign);
        public static readonly AST<FuncTerm> App_Send = Factory.Instance.MkFuncTerm(Con_Send);
        public static readonly AST<FuncTerm> App_Raise = Factory.Instance.MkFuncTerm(Con_Raise);
        public static readonly AST<FuncTerm> App_Scall = Factory.Instance.MkFuncTerm(Con_Scall);
        public static readonly AST<FuncTerm> App_ITE = Factory.Instance.MkFuncTerm(Con_ITE);
        public static readonly AST<FuncTerm> App_While = Factory.Instance.MkFuncTerm(Con_While);
        public static readonly AST<FuncTerm> App_Seq = Factory.Instance.MkFuncTerm(Con_Seq);
        public static readonly AST<FuncTerm> App_MachineDecl = Factory.Instance.MkFuncTerm(Con_MachineDecl);
        public static readonly AST<FuncTerm> App_EventDecl = Factory.Instance.MkFuncTerm(Con_EventDecl);
        public static readonly AST<FuncTerm> App_StateDecl = Factory.Instance.MkFuncTerm(Con_StateDecl);
        public static readonly AST<FuncTerm> App_ExitFun = Factory.Instance.MkFuncTerm(Con_ExitFun);
        public static readonly AST<FuncTerm> App_VarDecl = Factory.Instance.MkFuncTerm(Con_VarDecl);
        public static readonly AST<FuncTerm> App_TransDecl = Factory.Instance.MkFuncTerm(Con_TransDecl);
        public static readonly AST<FuncTerm> App_ActionDecl = Factory.Instance.MkFuncTerm(Con_ActionDecl);
        public static readonly AST<FuncTerm> App_Install = Factory.Instance.MkFuncTerm(Con_Install);
        public static readonly AST<FuncTerm> App_FunDecl = Factory.Instance.MkFuncTerm(Con_FunDecl);
        public static readonly AST<FuncTerm> App_Params = Factory.Instance.MkFuncTerm(Con_Params);

        public static readonly AST<FuncTerm> App_MachType = Factory.Instance.MkFuncTerm(Con_MachType);

        public static readonly AST<FuncTerm> App_StateSetDecl = Factory.Instance.MkFuncTerm(Con_StateSetDecl);
        public static readonly AST<FuncTerm> App_InStateSet = Factory.Instance.MkFuncTerm(Con_InStateSet);
        public static readonly AST<FuncTerm> App_EventSetDecl = Factory.Instance.MkFuncTerm(Con_EventSetDecl);
        public static readonly AST<FuncTerm> App_InEventSet = Factory.Instance.MkFuncTerm(Con_InEventSet);

        public static readonly AST<FuncTerm> App_MachStart = Factory.Instance.MkFuncTerm(Con_MachStart);
        public static readonly AST<FuncTerm> App_CanReceive = Factory.Instance.MkFuncTerm(Con_CanReceive);
        public static readonly AST<FuncTerm> App_CanRaise = Factory.Instance.MkFuncTerm(Con_CanRaise);

        public static readonly AST<FuncTerm> App_Flags = Factory.Instance.MkFuncTerm(Con_Flags);
        public static readonly AST<FuncTerm> App_File = Factory.Instance.MkFuncTerm(Con_File);

        static PData()
        {
            pOpToC.Add(Cnst_Not.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_LNot(), 1));
            pOpToC.Add(Cnst_Neg.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Neg(), 1));
            pOpToC.Add(Cnst_Add.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Add(), 2));
            pOpToC.Add(Cnst_Sub.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Sub(), 2));
            pOpToC.Add(Cnst_Mul.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Mul(), 2));
            pOpToC.Add(Cnst_IntDiv.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Div(), 2));
            pOpToC.Add(Cnst_And.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_LAnd(), 2));
            pOpToC.Add(Cnst_Or.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_LOr(), 2));
            pOpToC.Add(Cnst_Eq.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Eq(), 2));
            pOpToC.Add(Cnst_NEq.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_NEq(), 2));
            pOpToC.Add(Cnst_Lt.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Lt(), 2));
            pOpToC.Add(Cnst_Le.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Le(), 2));
            pOpToC.Add(Cnst_Gt.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Gt(), 2));
            pOpToC.Add(Cnst_Ge.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Ge(), 2));
            pOpToC.Add(Cnst_Idx.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_AAc(), 2));
            pOpToC.Add(Cnst_Fld.Node.Name, new Tuple<AST<Id>, int>(CData.Cnst_Fld(), 2));
            pOpToC.Add(Cnst_Sizeof.Node.Name, new Tuple<AST<Id>, int>(null, 1));
            pOpToC.Add(Cnst_In.Node.Name, new Tuple<AST<Id>, int>(null, 2));
            pOpToC.Add(Cnst_Keys.Node.Name, new Tuple<AST<Id>, int>(null, 1));

            pOpToZing.Add(Cnst_Not.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Not, 1));
            pOpToZing.Add(Cnst_Neg.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Neg, 1));
            pOpToZing.Add(Cnst_Add.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Add, 2));
            pOpToZing.Add(Cnst_Sub.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Sub, 2));
            pOpToZing.Add(Cnst_Mul.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Mul, 2));
            pOpToZing.Add(Cnst_IntDiv.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_IntDiv, 2));
            pOpToZing.Add(Cnst_And.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_And, 2));
            pOpToZing.Add(Cnst_Or.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Or, 2));
            pOpToZing.Add(Cnst_Eq.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Eq, 2));
            pOpToZing.Add(Cnst_NEq.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_NEq, 2));
            pOpToZing.Add(Cnst_Lt.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Lt, 2));
            pOpToZing.Add(Cnst_Le.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Le, 2));
            pOpToZing.Add(Cnst_Gt.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Gt, 2));
            pOpToZing.Add(Cnst_Ge.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Ge, 2));
            pOpToZing.Add(Cnst_Idx.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Index, 2));
            pOpToZing.Add(Cnst_Fld.Node.Name, new Tuple<AST<Id>, int>(ZingData.Cnst_Dot, 2));
            pOpToZing.Add(Cnst_Sizeof.Node.Name, new Tuple<AST<Id>, int>(null, 1));
            pOpToZing.Add(Cnst_In.Node.Name, new Tuple<AST<Id>, int>(null, 2));
            pOpToZing.Add(Cnst_Keys.Node.Name, new Tuple<AST<Id>, int>(null, 1));
        }

        public static AST<Id> POpToCOp(Id pOp, out int arity)
        {
            var result = pOpToC[pOp.Name];
            arity = result.Item2;
            return result.Item1;
        }

        public static AST<Id> POpToZingOp(Id pOp, out int arity)
        {
            var result = pOpToZing[pOp.Name];
            arity = result.Item2;
            return result.Item1;
        }
    }
}
