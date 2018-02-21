using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.Backend.ASTExt;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker
{
    public static class InferMachineCreates
    {
        public static void Populate(Machine machine)
        {
            foreach (Function function in machine.Methods)
            {
                machine.Creates = new InterfaceSet();
                machine.Creates.AddInterfaces(InferCreates(function));
            }
        }

        public static IEnumerable<Interface> InferCreates(IPAST tree)
        {
            switch (tree)
            {
                case null:
                    throw new ArgumentNullException(nameof(tree));
                case Function function:
                    return InferCreates(function.Body);
                case AnnounceStmt announce:
                    return InferCreatesForExpr(announce.PEvent).Union(InferCreatesForExpr(announce.Payload));
                case AssertStmt assertStmt:
                    return InferCreatesForExpr(assertStmt.Assertion);
                case AssignStmt assignStmt:
                    return InferCreatesForExpr(assignStmt.Variable)
                        .Union(InferCreatesForExpr(assignStmt.Value));
                case CompoundStmt compoundStmt:
                    return compoundStmt.Statements.SelectMany(InferCreates);
                case CtorStmt ctorStmt:
                    var res = new List<Interface>();
                    res.Add(ctorStmt.Interface);
                    return res.Union(ctorStmt.Arguments.SelectMany(InferCreatesForExpr));
                case FunCallStmt funCallStmt:
                    return InferCreates(funCallStmt.Fun)
                        .Union(funCallStmt.ArgsList.SelectMany(InferCreatesForExpr));
                case GotoStmt gotoStmt:
                    return InferCreatesForExpr(gotoStmt.Payload);
                case IfStmt ifStmt:
                    return InferCreatesForExpr(ifStmt.Condition)
                           .Union(InferCreates(ifStmt.ThenBranch))
                           .Union(InferCreates(ifStmt.ElseBranch));
                case InsertStmt insertStmt:
                    return InferCreatesForExpr(insertStmt.Variable)
                           .Union(InferCreatesForExpr(insertStmt.Index))
                           .Union(InferCreatesForExpr(insertStmt.Value));
                case MoveAssignStmt moveAssignStmt:
                    return InferCreatesForExpr(moveAssignStmt.ToLocation);
                case NoStmt _:
                    return Enumerable.Empty<Interface>();
                case PopStmt _:
                    return Enumerable.Empty<Interface>();
                case PrintStmt printStmt:
                    return printStmt.Args.SelectMany(InferCreatesForExpr);
                case RaiseStmt raiseStmt:
                    return InferCreatesForExpr(raiseStmt.PEvent)
                        .Union(raiseStmt.Payload.SelectMany(InferCreatesForExpr));
                case ReceiveStmt receiveStmt:
                    return receiveStmt.Cases.SelectMany(x => InferCreates(x.Value));
                case RemoveStmt removeStmt:
                    return InferCreatesForExpr(removeStmt.Variable)
                        .Union(InferCreatesForExpr(removeStmt.Value));
                case ReturnStmt returnStmt:
                    return InferCreatesForExpr(returnStmt.ReturnValue);
                case SendStmt sendStmt:
                    return InferCreatesForExpr(sendStmt.MachineExpr)
                           .Union(InferCreatesForExpr(sendStmt.Evt))
                           .Union(sendStmt.ArgsList.SelectMany(InferCreatesForExpr));
                case SwapAssignStmt swapAssignStmt:
                    return InferCreatesForExpr(swapAssignStmt.NewLocation);
                case WhileStmt whileStmt:
                    return InferCreatesForExpr(whileStmt.Condition).Union(InferCreates(whileStmt.Body));
                default:
                    throw new ArgumentOutOfRangeException(nameof(tree));
            }
        }

        public static IEnumerable<Interface> InferCreatesForExpr(IPExpr expr)
        {
            switch (expr)
            {
                case BinOpExpr binOpExpr:
                    return InferCreatesForExpr(binOpExpr.Lhs)
                        .Union(InferCreatesForExpr(binOpExpr.Rhs));
                case CastExpr castExpr:
                    return InferCreatesForExpr(castExpr.SubExpr);
                case CoerceExpr coerceExpr:
                    return InferCreatesForExpr(coerceExpr.SubExpr);
                case ContainsKeyExpr containsKeyExpr:
                    return InferCreatesForExpr(containsKeyExpr.Key)
                        .Union(InferCreatesForExpr(containsKeyExpr.Map));
                case CloneExpr cloneExpr:
                    return InferCreatesForExpr(cloneExpr.SubExpr);
                case CtorExpr ctorExpr:
                    var res = new List<Interface>
                    {
                        ctorExpr.Interface
                    };
                    return res.Union(ctorExpr.Arguments.SelectMany(InferCreatesForExpr));
                case FunCallExpr funCallExpr:
                    return InferCreates(funCallExpr.Function)
                        .Union(funCallExpr.Arguments.SelectMany(InferCreatesForExpr));
                case KeysExpr keysExpr:
                    return InferCreatesForExpr(keysExpr.Expr);
                case MapAccessExpr mapAccessExpr:
                    return InferCreatesForExpr(mapAccessExpr.MapExpr)
                        .Union(InferCreatesForExpr(mapAccessExpr.IndexExpr));
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    return InferCreatesForExpr(namedTupleAccessExpr.SubExpr);
                case NamedTupleExpr namedTupleExpr:
                    return namedTupleExpr.TupleFields.SelectMany(InferCreatesForExpr);
                case SeqAccessExpr seqAccessExpr:
                    return InferCreatesForExpr(seqAccessExpr.SeqExpr)
                        .Union(InferCreatesForExpr(seqAccessExpr.IndexExpr));
                case SizeofExpr sizeofExpr:
                    return InferCreatesForExpr(sizeofExpr.Expr);
                case TupleAccessExpr tupleAccessExpr:
                    return InferCreatesForExpr(tupleAccessExpr.SubExpr);
                case UnaryOpExpr unaryOpExpr:
                    return InferCreatesForExpr(unaryOpExpr.SubExpr);
                case UnnamedTupleExpr unnamedTupleExpr:
                    return unnamedTupleExpr.TupleFields.SelectMany(InferCreatesForExpr);
                case ValuesExpr valuesExpr:
                    return InferCreatesForExpr(valuesExpr.Expr);
                default:
                    return Enumerable.Empty<Interface>();
            }
        }
    }
}
