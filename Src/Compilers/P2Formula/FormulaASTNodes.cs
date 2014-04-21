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
    public static class SpanHelpers
    {
        public static Span ToSpan(this DSLLoc loc)
        {
            if (loc == null)
            {
                return default(Span);
            }
            else
            {
                return new Span(loc.startLine, loc.startColumn, loc.endLine, loc.endColumn);
            }
        }

        public static DSLLoc ToLoc(this Span span)
        {
            return new DSLLoc(span);
        }
    }

    public class P_FormulaNodes
    {
        //Specials
        public static readonly Dictionary<Ops, Func<DSLLoc, AST<Id>>> OperatorToId;

        //Program Toplevel
        public static AST<Id> MkMachineDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("MachineDecl", loc.ToSpan());
        }

        public static AST<Id> MkMainDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("MainDecl", loc.ToSpan());
        }

        public static AST<Id> MkEventDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("EventDecl", loc.ToSpan());
        }

        public static AST<Id> MkAssertMaxInstancesId(DSLLoc loc)
        {
            return Factory.Instance.MkId("AssertMaxInstances", loc.ToSpan());
        }

        public static AST<Id> MkAssumeMaxInstancesId(DSLLoc loc)
        {
            return Factory.Instance.MkId("AssumeMaxInstances", loc.ToSpan());
        }

        //Machine Declaration Elements
        public static AST<Id> MkVarDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("VarDecl", loc.ToSpan());
        }

        public static AST<Id> MkActionDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("ActionDecl", loc.ToSpan());
        }
      
        public static AST<Id> MkParamsId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Params", loc.ToSpan());
        }

        public static AST<Id> MkFunDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("FunDecl", loc.ToSpan());
        }

        public static AST<Id> MkFlagsId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Flags", loc.ToSpan());
        }

        public static AST<Id> MkStateDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("StateDecl", loc.ToSpan());
        }

        public static AST<Id> MkMachStartId(DSLLoc loc)
        {
            return Factory.Instance.MkId("MachStart", loc.ToSpan());
        }

        public static AST<Id> MkStableId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Stable", loc.ToSpan());
        }

        public static AST<Id> MkPassiveId(DSLLoc loc)
        {
            return Factory.Instance.MkId("PASSIVE", loc.ToSpan());
        }

        public static AST<Id> MkStateSetDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("StateSetDecl", loc.ToSpan());
        }

        public static AST<Id> MkInStateSetId(DSLLoc loc)
        {
            return Factory.Instance.MkId("InStateSet", loc.ToSpan());
        }        

        //State Declaration Elements
        public static AST<Id> MkEventSetDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("EventSetDecl", loc.ToSpan());
        }

        public static AST<Id> MkInEventSetId(DSLLoc loc)
        {
            return Factory.Instance.MkId("InEventSet", loc.ToSpan());
        }

        public static AST<Id> MkExitFunId(DSLLoc loc)
        {
            return Factory.Instance.MkId("ExitFun", loc.ToSpan());
        }

        public static AST<Id> MkInstallId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Install", loc.ToSpan());
        }

        public static AST<Id> MkTransDeclId(DSLLoc loc)
        {
            return Factory.Instance.MkId("TransDecl", loc.ToSpan());
        }

        // DSL Statements
        public static AST<Id> MkITEId(DSLLoc loc)
        {
            return Factory.Instance.MkId("ITE", loc.ToSpan());
        }

        public static AST<Id> MkWhileId(DSLLoc loc)
        {
            return Factory.Instance.MkId("While", loc.ToSpan());
        }

        public static AST<Id> MkAssignId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Assign", loc.ToSpan());
        }

        public static AST<Id> MkSendId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Send", loc.ToSpan());
        }

        public static AST<Id> MkRaiseId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Raise", loc.ToSpan());
        }

        public static AST<Id> MkScallId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Scall", loc.ToSpan());
        }

        public static AST<Id> MkMcallId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Mcall", loc.ToSpan());
        }

        public static AST<Id> MkEcallId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Ecall", loc.ToSpan());
        }

        public static AST<Id> MkSeqId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Seq", loc.ToSpan());
        }

        public static AST<Id> MkAssertId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Assert", loc.ToSpan());
        }

        public static AST<Id> MkReturnId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Return", loc.ToSpan());
        }

        public static AST<Id> MkLEAVEId(DSLLoc loc)
        {
            return Factory.Instance.MkId("LEAVE", loc.ToSpan());
        }

        public static AST<Id> MkDataOpId(DSLLoc loc)
        {
            return Factory.Instance.MkId("DataOp", loc.ToSpan());
        }

        public static AST<Id> MkInsertId(DSLLoc loc)
        {
            return Factory.Instance.MkId("INSERT", loc.ToSpan());
        }

        public static AST<Id> MkRemoveId(DSLLoc loc)
        {
            return Factory.Instance.MkId("REMOVE", loc.ToSpan());
        }

        public static AST<Id> MkUpdateId(DSLLoc loc)
        {
            return Factory.Instance.MkId("UPDATE", loc.ToSpan());
        }

        //Types
        #region Types
        public static AST<Id> MkTypeBOOLId(DSLLoc loc)
        {
            return Factory.Instance.MkId("BOOL", loc.ToSpan());
        }

        public static AST<Id> MkTypeINTId(DSLLoc loc)
        {
            return Factory.Instance.MkId("INT", loc.ToSpan());
        }

        public static AST<Id> MkTypeEVENTId(DSLLoc loc)
        {
            return Factory.Instance.MkId("EVENT", loc.ToSpan());
        }

        public static AST<Id> MkTypeAnyId(DSLLoc loc)
        {
            return Factory.Instance.MkId("ANY", loc.ToSpan());
        }

        public static AST<Id> MkTypeIDId(DSLLoc loc)
        {
            return Factory.Instance.MkId("ID", loc.ToSpan());
        }

        public static AST<Id> MkTypeMIDId(DSLLoc loc)
        {
            return Factory.Instance.MkId("MID", loc.ToSpan());
        }

        public static AST<Id> MkTypeSIDId(DSLLoc loc)
        {
            return Factory.Instance.MkId("SID", loc.ToSpan());
        }

        public static AST<Id> MkTypeFieldId(DSLLoc loc)
        {
            return Factory.Instance.MkId("TypeField", loc.ToSpan());
        }

        public static AST<Id> MkTypeNamedTupleId(DSLLoc loc)
        {
            return Factory.Instance.MkId("TypeNamedTuple", loc.ToSpan());
        }

        public static AST<Id> MkTypeTupleId(DSLLoc loc)
        {
            return Factory.Instance.MkId("TypeTuple", loc.ToSpan());
        }

        public static AST<Id> MkTypeSeqId(DSLLoc loc)
        {
            return Factory.Instance.MkId("TypeSeq", loc.ToSpan());
        }

        public static AST<Id> MkTypeMapId(DSLLoc loc)
        {
            return Factory.Instance.MkId("TypeMap", loc.ToSpan());
        }
        #endregion

        // Expressions
        #region Expressions
        public static AST<Id> MkUseId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Use", loc.ToSpan());
        }

        public static AST<Id> MkIndexId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Index", loc.ToSpan());
        }
        public static AST<Id> MkApplyId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Apply", loc.ToSpan());
        }

        public static AST<Id> MkExprsId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Exprs", loc.ToSpan());
        }

        public static AST<Id> MkStringsId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Strings", loc.ToSpan());
        }

        public static AST<Id> MkCallId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Call", loc.ToSpan());
        }

        public static AST<Id> MkNamedTupleId(DSLLoc loc)
        {
            return Factory.Instance.MkId("NamedTuple", loc.ToSpan());
        }

        public static AST<Id> MkTupleId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Tuple", loc.ToSpan());
        }

        public static AST<Id> MkNamedExprId(DSLLoc loc)
        {
            return Factory.Instance.MkId("NamedExpr", loc.ToSpan());
        }

        public static AST<Id> MkNamedExprsId(DSLLoc loc)
        {
            return Factory.Instance.MkId("NamedExprs", loc.ToSpan());
        }

        public static AST<Id> MkNewId(DSLLoc loc)
        {
            return Factory.Instance.MkId("New", loc.ToSpan());
        }

        public static AST<Id> MkPayloadId(DSLLoc loc)
        {
            return Factory.Instance.MkId("Payload", loc.ToSpan());
        }

        public static AST<Id> MkStateKindId(DSLLoc loc)
        {
            return Factory.Instance.MkId("STATE", loc.ToSpan());
        }

        public static AST<Id> MkEventKindId(DSLLoc loc)
        {
            return Factory.Instance.MkId("EVENT", loc.ToSpan());
        }

        public static AST<Id> MkVarKindId(DSLLoc loc)
        {
            return Factory.Instance.MkId("VAR", loc.ToSpan());
        }

        public static AST<Id> MkFieldKindId(DSLLoc loc)
        {
            return Factory.Instance.MkId("FIELD", loc.ToSpan());
        }

        public static AST<Id> MkNilId(DSLLoc loc)
        {
            return Factory.Instance.MkId("NIL", loc.ToSpan());
        }

        public static AST<Id> MkNondetId(DSLLoc loc)
        {
            return Factory.Instance.MkId("NONDET", loc.ToSpan());
        }

        public static AST<Id> MkFairNondetId(DSLLoc loc)
        {
            return Factory.Instance.MkId("FAIRNONDET", loc.ToSpan());
        }

        public static AST<Id> MkThisId(DSLLoc loc)
        {
            return Factory.Instance.MkId("THIS", loc.ToSpan());
        }

        public static AST<Id> MkTriggerId(DSLLoc loc)
        {
            return Factory.Instance.MkId("TRIGGER", loc.ToSpan());
        }

        public static AST<Id> MkDefaultId(DSLLoc loc)
        {
            return Factory.Instance.MkId("DEFAULT", loc.ToSpan());
        }

        public static AST<Id> MkDeleteId(DSLLoc loc)
        {
            return Factory.Instance.MkId("DELETE", loc.ToSpan());
        }

        public static AST<Id> MkNullId(DSLLoc loc)
        {
            return Factory.Instance.MkId("NULL", loc.ToSpan());
        }

        #endregion

        //Operations
        #region Operations
        public static AST<Id> MkAddId(DSLLoc loc)
        {
            return Factory.Instance.MkId("ADD", loc.ToSpan());
        }

        public static AST<Id> MkSubId(DSLLoc loc)
        {
            return Factory.Instance.MkId("SUB", loc.ToSpan());
        }

        public static AST<Id> MkMulId(DSLLoc loc)
        {
            return Factory.Instance.MkId("MUL", loc.ToSpan());
        }

        public static AST<Id> MkIntDivId(DSLLoc loc)
        {
            return Factory.Instance.MkId("INTDIV", loc.ToSpan());
        }

        public static AST<Id> MkAndId(DSLLoc loc)
        {
            return Factory.Instance.MkId("AND", loc.ToSpan());
        }

        public static AST<Id> MkOrId(DSLLoc loc)
        {
            return Factory.Instance.MkId("OR", loc.ToSpan());
        }

        public static AST<Id> MkNegativeId(DSLLoc loc)
        {
            return Factory.Instance.MkId("NEG", loc.ToSpan());
        }

        public static AST<Id> MkNotId(DSLLoc loc)
        {
            return Factory.Instance.MkId("NOT", loc.ToSpan());
        }

        public static AST<Id> MkEqEqId(DSLLoc loc)
        {
            return Factory.Instance.MkId("EQ", loc.ToSpan());
        }

        public static AST<Id> MkNEqId(DSLLoc loc)
        {
            return Factory.Instance.MkId("NEQ", loc.ToSpan());
        }

        public static AST<Id> MkGtId(DSLLoc loc)
        {
            return Factory.Instance.MkId("GT", loc.ToSpan());
        }

        public static AST<Id> MkGtEqId(DSLLoc loc)
        {
            return Factory.Instance.MkId("GE", loc.ToSpan());
        }

        public static AST<Id> MkLtEqId(DSLLoc loc)
        {
            return Factory.Instance.MkId("LE", loc.ToSpan());
        }

        public static AST<Id> MkLtId(DSLLoc loc)
        {
            return Factory.Instance.MkId("LT", loc.ToSpan());
        }

        public static AST<Id> MkInId(DSLLoc loc)
        {
            return Factory.Instance.MkId("IN", loc.ToSpan());
        }

        public static AST<Id> MkChooseId(DSLLoc loc)
        {
            return Factory.Instance.MkId("CHOOSE", loc.ToSpan());
        }

        public static AST<Id> MkEnumeratorId(DSLLoc loc)
        {
            return Factory.Instance.MkId("ENUM", loc.ToSpan());
        }

        public static AST<Id> MkS_EnumeratorId(DSLLoc loc)
        {
            return Factory.Instance.MkId("SENUM", loc.ToSpan());
        }

        public static AST<Id> MkSizeofId(DSLLoc loc)
        {
            return Factory.Instance.MkId("SIZEOF", loc.ToSpan());
        }

        public static AST<Id> MkKeysId(DSLLoc loc)
        {
            return Factory.Instance.MkId("KEYS", loc.ToSpan());
        }

        public static AST<Id> MkFldId(DSLLoc loc)
        {
            return Factory.Instance.MkId("FLD", loc.ToSpan());
        }

        public static AST<Id> MkIdxId(DSLLoc loc)
        {
            return Factory.Instance.MkId("IDX", loc.ToSpan());
        }
        #endregion

        static P_FormulaNodes()
        {  
            OperatorToId = new Dictionary<Ops, Func<DSLLoc, AST<Id>>>();

            OperatorToId[Ops.U_LNOT] = MkNotId;
            OperatorToId[Ops.U_MINUS] = MkNegativeId;

            OperatorToId[Ops.B_PLUS] = MkAddId;
            OperatorToId[Ops.B_MINUS] = MkSubId;
            OperatorToId[Ops.B_MUL] = MkMulId;
            OperatorToId[Ops.B_DIV] = MkIntDivId;
            OperatorToId[Ops.B_LAND] = MkAndId;
            OperatorToId[Ops.B_LOR] = MkOrId;
            OperatorToId[Ops.B_EQ] = MkEqEqId;
            OperatorToId[Ops.B_NE] = MkNEqId;
            OperatorToId[Ops.B_GT] = MkGtId;
            OperatorToId[Ops.B_GE] = MkGtEqId;
            OperatorToId[Ops.B_LT] = MkLtId;
            OperatorToId[Ops.B_LE] = MkLtEqId;
            OperatorToId[Ops.B_IN] = MkInId;
        }
    }
}
