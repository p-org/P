using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Compiler;
using Microsoft.Formula.API;

namespace PParser
{
    public class P_FormulaNodes
    {
        //Specials
        public static readonly Dictionary<Ops, AST<Id>> OperatorToId;

        //Program Toplevel
        public static AST<Id> MachineDecl_Iden = Factory.Instance.MkId("MachineDecl");
        public static AST<Id> MainDecl_Iden = Factory.Instance.MkId("MainDecl");
        public static AST<Id> EventDecl_Iden = Factory.Instance.MkId("EventDecl");
        public static AST<Id> AssertMaxInstances_Iden = Factory.Instance.MkId("AssertMaxInstances");
        public static AST<Id> AssumeMaxInstances_Iden = Factory.Instance.MkId("AssumeMaxInstances");

        //Machine Declaration Elements
        public static AST<Id> VarDecl_Iden = Factory.Instance.MkId("VarDecl");
        public static AST<Id> ActionDecl_Iden = Factory.Instance.MkId("ActionDecl");
        public static AST<Id> Params_Iden = Factory.Instance.MkId("Params");
        public static AST<Id> FunDecl_Iden = Factory.Instance.MkId("FunDecl");
        public static AST<Id> Flags_Iden = Factory.Instance.MkId("Flags");
        public static AST<Id> StateDecl_Iden = Factory.Instance.MkId("StateDecl");
        public static AST<Id> MachStart_Iden = Factory.Instance.MkId("MachStart");
        public static AST<Id> Passive_Iden = Factory.Instance.MkId("PASSIVE");
        public static AST<Id> StateSetDecl_Iden = Factory.Instance.MkId("StateSetDecl");
        public static AST<Id> InStateSet_Iden = Factory.Instance.MkId("InStateSet");

        //State Declaration Elements
        public static AST<Id> EventSetDecl_Iden = Factory.Instance.MkId("EventSetDecl");
        public static AST<Id> InEventSet_Iden = Factory.Instance.MkId("InEventSet");
        public static AST<Id> ExitFun_Iden = Factory.Instance.MkId("ExitFun");
        public static AST<Id> Install_Iden = Factory.Instance.MkId("Install");
        public static AST<Id> TransDecl_Iden = Factory.Instance.MkId("TransDecl");

        // DSL Statements
        public static AST<Id> ITE_Iden = Factory.Instance.MkId("ITE");
        public static AST<Id> While_Iden = Factory.Instance.MkId("While");
        public static AST<Id> Assign_Iden = Factory.Instance.MkId("Assign");
        public static AST<Id> Send_Iden = Factory.Instance.MkId("Send");
        public static AST<Id> Raise_Iden = Factory.Instance.MkId("Raise");
        public static AST<Id> Scall_Iden = Factory.Instance.MkId("Scall");
        public static AST<Id> Seq_Iden = Factory.Instance.MkId("Seq");
        public static AST<Id> Assert_Iden = Factory.Instance.MkId("Assert");
        public static AST<Id> Return_Iden = Factory.Instance.MkId("Return");
        public static AST<Id> LEAVE_Iden = Factory.Instance.MkId("LEAVE");
        public static AST<Id> DELETE_Iden = Factory.Instance.MkId("DELETE");
        public static AST<Id> DataOp_Iden = Factory.Instance.MkId("DataOp");

        public static AST<Id> Insert_Iden = Factory.Instance.MkId("INSERT");
        public static AST<Id> Remove_Iden = Factory.Instance.MkId("REMOVE");

        //Types
        #region Types
        public static AST<Id> MachType_Iden = Factory.Instance.MkId("MachType");    // TODO: Not sure this belongs here...
        public static AST<Id> TypeBOOL = Factory.Instance.MkId("BOOL");
        public static AST<Id> TypeINT = Factory.Instance.MkId("INT");
        public static AST<Id> TypeEVENT = Factory.Instance.MkId("EVENT");
        public static AST<Id> TypeAny = Factory.Instance.MkId("ANY");
        public static AST<Id> TypeID = Factory.Instance.MkId("ID");
        public static AST<Id> TypeField = Factory.Instance.MkId("TypeField");
        public static AST<Id> TypeNamedTuple = Factory.Instance.MkId("TypeNamedTuple");
        public static AST<Id> TypeTuple = Factory.Instance.MkId("TypeTuple");
        public static AST<Id> TypeSeq = Factory.Instance.MkId("TypeSeq");
        #endregion

        // Expressions
        #region Expressions
        public static AST<Id> Use_Iden = Factory.Instance.MkId("Use");
        public static AST<Id> Index_Iden = Factory.Instance.MkId("Index");
        public static AST<Id> Apply_Iden = Factory.Instance.MkId("Apply");
        public static AST<Id> Exprs_Iden = Factory.Instance.MkId("Exprs");
        public static AST<Id> Call_Iden = Factory.Instance.MkId("Call");
        public static AST<Id> NamedTuple_Iden = Factory.Instance.MkId("NamedTuple");
        public static AST<Id> Tuple_Iden = Factory.Instance.MkId("Tuple");
        public static AST<Id> NamedExpr_Iden = Factory.Instance.MkId("NamedExpr");
        public static AST<Id> NamedExprs_Iden = Factory.Instance.MkId("NamedExprs");
        public static AST<Id> New_Iden = Factory.Instance.MkId("New");
        public static AST<Id> Payload_Iden = Factory.Instance.MkId("Payload");

        public static AST<Id> StateKind_Iden = Factory.Instance.MkId("STATE");
        public static AST<Id> EventKind_Iden = Factory.Instance.MkId("EVENT");
        public static AST<Id> VarKind_Iden = Factory.Instance.MkId("VAR");
        public static AST<Id> FieldKind_Iden = Factory.Instance.MkId("FIELD");
        public static AST<Id> Nil_Iden = Factory.Instance.MkId("NIL");
        public static AST<Id> Nondet_Iden = Factory.Instance.MkId("NONDET");
        public static AST<Id> This_Iden = Factory.Instance.MkId("THIS");
        public static AST<Id> Trigger_Iden = Factory.Instance.MkId("TRIGGER");
        public static AST<Id> Default_Iden = Factory.Instance.MkId("DEFAULT");
        public static AST<Id> Null_Iden = Factory.Instance.MkId("NULL");

        #endregion

        //Operations
        #region Operations
        public static AST<Id> Add_Iden = Factory.Instance.MkId("ADD");
        public static AST<Id> Sub_Iden = Factory.Instance.MkId("SUB");
        public static AST<Id> Mul_Iden = Factory.Instance.MkId("MUL");
        public static AST<Id> IntDiv_Iden = Factory.Instance.MkId("INTDIV");
        public static AST<Id> And_Iden = Factory.Instance.MkId("AND");
        public static AST<Id> Or_Iden = Factory.Instance.MkId("OR");
        public static AST<Id> Negative_Iden = Factory.Instance.MkId("NEG");
        public static AST<Id> Not_Iden = Factory.Instance.MkId("NOT");
        public static AST<Id> EqEq_Iden = Factory.Instance.MkId("EQ");
        public static AST<Id> NEq_Iden = Factory.Instance.MkId("NEQ");
        public static AST<Id> Gt_Iden = Factory.Instance.MkId("GT");
        public static AST<Id> GtEq_Iden = Factory.Instance.MkId("GE");
        public static AST<Id> Lt_Iden = Factory.Instance.MkId("LT");
        public static AST<Id> LtEq_Iden = Factory.Instance.MkId("LE");
        public static AST<Id> In_Iden = Factory.Instance.MkId("IN");
        public static AST<Id> Choose_Iden = Factory.Instance.MkId("CHOOSE");
        public static AST<Id> Enumerator_Iden = Factory.Instance.MkId("ENUM");
        public static AST<Id> S_Enumerator_Iden = Factory.Instance.MkId("SENUM");
        public static AST<Id> Sizeof_Iden = Factory.Instance.MkId("SIZEOF");
        public static AST<Id> Fld_Iden = Factory.Instance.MkId("FLD");
        public static AST<Id> Idx_Iden = Factory.Instance.MkId("IDX");
        #endregion

        public static AST<Id> GetIdNode(string Id_Name, Span Span)
        {
            return Factory.Instance.MkId(Id_Name, Span);
        }

        public static AST<FuncTerm> GetFuncTermNode(string funcTerm_Name, Span Span)
        {
            var f_id = GetIdNode(funcTerm_Name, Span);
            return Factory.Instance.MkFuncTerm(f_id, Span);
        }

        static P_FormulaNodes()
        {  
            OperatorToId = new Dictionary<Ops, AST<Id>>();

            OperatorToId[Ops.U_LNOT] = Not_Iden;
            OperatorToId[Ops.U_MINUS] = Negative_Iden;

            OperatorToId[Ops.B_PLUS] = Add_Iden;
            OperatorToId[Ops.B_MINUS] = Sub_Iden;
            OperatorToId[Ops.B_MUL] = Mul_Iden;
            OperatorToId[Ops.B_DIV] = IntDiv_Iden;
            OperatorToId[Ops.B_LAND] = And_Iden;
            OperatorToId[Ops.B_LOR] = Or_Iden;
            OperatorToId[Ops.B_EQ] = EqEq_Iden;
            OperatorToId[Ops.B_NE] = NEq_Iden;
            OperatorToId[Ops.B_GT] = Gt_Iden;
            OperatorToId[Ops.B_GE] = GtEq_Iden;
            OperatorToId[Ops.B_LT] = Lt_Iden;
            OperatorToId[Ops.B_LE] = LtEq_Iden;
            
        }
    }
}
