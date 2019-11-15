using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plang.Compiler.TypeChecker
{
    public static class InferMachineCreates
    {
        public static void Populate(Machine machine, ITranslationErrorHandler handler)
        {
            InterfaceSet interfaces = new InterfaceSet();
            foreach (Function function in machine.Methods)
            {
                interfaces.AddInterfaces(InferCreates(function, handler));
            }

            machine.Creates = interfaces;
        }

        private static IEnumerable<Interface> InferCreates(IPAST tree, ITranslationErrorHandler handler)
        {
            switch (tree)
            {
                case Function function:
                    if (function.IsForeign)
                    {
                        return function.CreatesInterfaces;
                    }

                    return InferCreates(function.Body, handler);

                case AddStmt addStmt:
                    return InferCreatesForExpr(addStmt.Variable, handler)
                        .Union(InferCreatesForExpr(addStmt.Value, handler));

                case AnnounceStmt announce:
                    return InferCreatesForExpr(announce.PEvent, handler)
                        .Union(InferCreatesForExpr(announce.Payload, handler));

                case AssertStmt assertStmt:
                    return InferCreatesForExpr(assertStmt.Assertion, handler);

                case AssignStmt assignStmt:
                    return InferCreatesForExpr(assignStmt.Location, handler)
                        .Union(InferCreatesForExpr(assignStmt.Value, handler));

                case CompoundStmt compoundStmt:
                    return compoundStmt.Statements.SelectMany(tree1 => InferCreates(tree1, handler));

                case CtorStmt ctorStmt:
                    Interface[] res = new[] { ctorStmt.Interface };
                    return res.Union(ctorStmt.Arguments.SelectMany(expr => InferCreatesForExpr(expr, handler)));

                case FunCallStmt funCallStmt:
                    return InferCreates(funCallStmt.Function, handler)
                        .Union(funCallStmt.ArgsList.SelectMany(expr => InferCreatesForExpr(expr, handler)));

                case GotoStmt gotoStmt:
                    return InferCreatesForExpr(gotoStmt.Payload, handler);

                case IfStmt ifStmt:
                    return InferCreatesForExpr(ifStmt.Condition, handler)
                        .Union(InferCreates(ifStmt.ThenBranch, handler))
                        .Union(InferCreates(ifStmt.ElseBranch, handler));

                case InsertStmt insertStmt:
                    return InferCreatesForExpr(insertStmt.Variable, handler)
                        .Union(InferCreatesForExpr(insertStmt.Index, handler))
                        .Union(InferCreatesForExpr(insertStmt.Value, handler));

                case MoveAssignStmt moveAssignStmt:
                    return InferCreatesForExpr(moveAssignStmt.ToLocation, handler);

                case NoStmt _:
                    return Enumerable.Empty<Interface>();

                case PopStmt _:
                    return Enumerable.Empty<Interface>();

                case PrintStmt printStmt:
                    return printStmt.Args.SelectMany(expr => InferCreatesForExpr(expr, handler));

                case RaiseStmt raiseStmt:
                    return InferCreatesForExpr(raiseStmt.PEvent, handler)
                        .Union(raiseStmt.Payload.SelectMany(expr => InferCreatesForExpr(expr, handler)));

                case ReceiveStmt receiveStmt:
                    return receiveStmt.Cases.SelectMany(x => InferCreates(x.Value, handler));

                case RemoveStmt removeStmt:
                    return InferCreatesForExpr(removeStmt.Variable, handler)
                        .Union(InferCreatesForExpr(removeStmt.Value, handler));

                case ReturnStmt returnStmt:
                    return InferCreatesForExpr(returnStmt.ReturnValue, handler);

                case StringAssignStmt stringAssignStmt:
                    return InferCreatesForExpr(stringAssignStmt.Location, handler)
                        .Union(stringAssignStmt.Args.SelectMany(expr => InferCreatesForExpr(expr, handler)));

                case BreakStmt breakStmt:
                    return Enumerable.Empty<Interface>();

                case ContinueStmt continueStmt:
                    return Enumerable.Empty<Interface>();

                case SendStmt sendStmt:
                    return InferCreatesForExpr(sendStmt.MachineExpr, handler)
                        .Union(InferCreatesForExpr(sendStmt.Evt, handler))
                        .Union(sendStmt.Arguments.SelectMany(expr => InferCreatesForExpr(expr, handler)));

                case SwapAssignStmt swapAssignStmt:
                    return InferCreatesForExpr(swapAssignStmt.NewLocation, handler);

                case WhileStmt whileStmt:
                    return InferCreatesForExpr(whileStmt.Condition, handler)
                        .Union(InferCreates(whileStmt.Body, handler));

                default:
                    throw handler.InternalError(tree.SourceLocation, new ArgumentOutOfRangeException(nameof(tree)));
            }
        }

        private static IEnumerable<Interface> InferCreatesForExpr(IPExpr expr, ITranslationErrorHandler handler)
        {
            switch (expr)
            {
                case BinOpExpr binOpExpr:
                    return InferCreatesForExpr(binOpExpr.Lhs, handler)
                        .Union(InferCreatesForExpr(binOpExpr.Rhs, handler));

                case CastExpr castExpr:
                    return InferCreatesForExpr(castExpr.SubExpr, handler);

                case CoerceExpr coerceExpr:
                    return InferCreatesForExpr(coerceExpr.SubExpr, handler);

                case ContainsExpr containsKeyExpr:
                    return InferCreatesForExpr(containsKeyExpr.Item, handler)
                        .Union(InferCreatesForExpr(containsKeyExpr.Collection, handler));

                case CloneExpr cloneExpr:
                    return InferCreatesForExpr(cloneExpr.Term, handler);

                case CtorExpr ctorExpr:
                    Interface[] res = new[] { ctorExpr.Interface };
                    return res.Union(ctorExpr.Arguments.SelectMany(expr1 => InferCreatesForExpr(expr1, handler)));

                case FunCallExpr funCallExpr:
                    return InferCreates(funCallExpr.Function, handler)
                        .Union(funCallExpr.Arguments.SelectMany(expr1 => InferCreatesForExpr(expr1, handler)));

                case KeysExpr keysExpr:
                    return InferCreatesForExpr(keysExpr.Expr, handler);

                case MapAccessExpr mapAccessExpr:
                    return InferCreatesForExpr(mapAccessExpr.MapExpr, handler)
                        .Union(InferCreatesForExpr(mapAccessExpr.IndexExpr, handler));

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    return InferCreatesForExpr(namedTupleAccessExpr.SubExpr, handler);

                case NamedTupleExpr namedTupleExpr:
                    return namedTupleExpr.TupleFields.SelectMany(expr1 => InferCreatesForExpr(expr1, handler));

                case SeqAccessExpr seqAccessExpr:
                    return InferCreatesForExpr(seqAccessExpr.SeqExpr, handler)
                        .Union(InferCreatesForExpr(seqAccessExpr.IndexExpr, handler));

                case SizeofExpr sizeofExpr:
                    return InferCreatesForExpr(sizeofExpr.Expr, handler);

                case TupleAccessExpr tupleAccessExpr:
                    return InferCreatesForExpr(tupleAccessExpr.SubExpr, handler);

                case UnaryOpExpr unaryOpExpr:
                    return InferCreatesForExpr(unaryOpExpr.SubExpr, handler);

                case UnnamedTupleExpr unnamedTupleExpr:
                    return unnamedTupleExpr.TupleFields.SelectMany(expr1 => InferCreatesForExpr(expr1, handler));

                case ValuesExpr valuesExpr:
                    return InferCreatesForExpr(valuesExpr.Expr, handler);

                default:
                    return Enumerable.Empty<Interface>();
            }
        }
    }
}