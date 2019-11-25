using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Plang.Compiler.TypeChecker
{
    public class LinearTypeChecker
    {
        private readonly HashSet<Variable> allUnavailableParams = new HashSet<Variable>();
        private readonly List<FunCallExpr> funCallExprs = new List<FunCallExpr>();
        private readonly List<FunCallStmt> funCallStmts = new List<FunCallStmt>();
        private readonly ITranslationErrorHandler handler;

        private LinearTypeChecker(ITranslationErrorHandler handler)
        {
            this.handler = handler;
        }

        public static void AnalyzeMethods(ITranslationErrorHandler handler, IEnumerable<Function> allFunctions)
        {
            LinearTypeChecker checker = new LinearTypeChecker(handler);
            foreach (Function function in allFunctions)
            {
                checker.CheckFunction(function);
            }

            checker.CheckInterproceduralCalls();
        }

        private void CheckFunction(Function method)
        {
            if (method.IsForeign)
            {
                return;
            }

            ISet<Variable> unavailable = ProcessStatement(new HashSet<Variable>(), method.Body);
            allUnavailableParams.UnionWith(unavailable.Where(var => var.Role.Equals(VariableRole.Param)));
        }

        private void CheckInterproceduralCalls()
        {
            foreach (FunCallExpr funCallExpr in funCallExprs)
            {
                CheckFunctionCall(funCallExpr.Function, funCallExpr.Arguments);
            }

            foreach (FunCallStmt funCallStmt in funCallStmts)
            {
                CheckFunctionCall(funCallStmt.Function, funCallStmt.ArgsList);
            }
        }

        private void CheckFunctionCall(Function function, IEnumerable<IPExpr> arguments)
        {
            int i = 0;
            foreach (Tuple<Variable, IPExpr> pair in function.Signature.Parameters.Zip(arguments, Tuple.Create))
            {
                if (pair.Item2 is ILinearRef linearRef)
                {
                    if (linearRef.LinearType.Equals(LinearType.Swap) && allUnavailableParams.Contains(pair.Item1))
                    {
                        throw handler.InvalidSwap(linearRef,
                            $"function {function.Name} relinquishes argument #{i} and therefore cannot be swapped.");
                    }
                }

                i++;
            }
        }

        private ISet<Variable> ProcessStatement(ISet<Variable> unavailable, IPStmt statement)
        {
            Contract.Requires(statement != null);
            switch (statement)
            {
                case CompoundStmt compoundStmt:
                    unavailable = compoundStmt.Statements.Aggregate(unavailable, ProcessStatement);
                    break;

                case AssertStmt assertStmt:
                    unavailable = ProcessExpr(unavailable, assertStmt.Assertion);
                    break;

                case PrintStmt printStmt:
                    unavailable = ProcessArgList(printStmt.Args, unavailable, ArgOptions.SwapNotAllowed);
                    break;

                case ReturnStmt returnStmt:
                    if (returnStmt.ReturnValue != null)
                    {
                        unavailable = ProcessExpr(unavailable, returnStmt.ReturnValue);
                    }

                    break;

                case BreakStmt breakStmt:
                    break;

                case ContinueStmt continueStmt:
                    break;

                case AssignStmt assignStmt:
                    unavailable = ProcessExpr(unavailable, assignStmt.Value);
                    if (assignStmt.Location is VariableAccessExpr assignAccess)
                    {
                        unavailable.Remove(assignAccess.Variable);
                    }
                    else
                    {
                        unavailable = ProcessExpr(unavailable, assignStmt.Location);
                    }

                    break;

                case MoveAssignStmt moveAssignStmt:
                    if (moveAssignStmt.FromVariable.Role.Equals(VariableRole.Field))
                    {
                        throw handler.MovedField(moveAssignStmt);
                    }

                    unavailable.Add(moveAssignStmt.FromVariable);

                    if (moveAssignStmt.ToLocation is VariableAccessExpr moveAssignAccess)
                    {
                        unavailable.Remove(moveAssignAccess.Variable);
                    }
                    else
                    {
                        unavailable = ProcessExpr(unavailable, moveAssignStmt.ToLocation);
                    }

                    break;

                case SwapAssignStmt swapAssignStmt:
                    if (swapAssignStmt.NewLocation is VariableAccessExpr swapAssignAccess)
                    {
                        if (unavailable.Contains(swapAssignAccess.Variable))
                        {
                            throw handler.SwapAssignUnavailable(swapAssignStmt, swapAssignAccess.Variable);
                        }
                    }
                    else
                    {
                        unavailable = ProcessExpr(unavailable, swapAssignStmt.NewLocation);
                    }

                    if (unavailable.Contains(swapAssignStmt.OldLocation))
                    {
                        throw handler.SwapAssignUnavailable(swapAssignStmt, swapAssignStmt.OldLocation);
                    }

                    break;

                case StringAssignStmt stringAssignStmt:
                    if (stringAssignStmt.Location is VariableAccessExpr stringAssignAccess)
                    {
                        unavailable.Remove(stringAssignAccess.Variable);
                    }
                    else
                    {
                        unavailable = ProcessExpr(unavailable, stringAssignStmt.Location);
                    }
                    unavailable = ProcessArgList(stringAssignStmt.Args, unavailable, ArgOptions.SwapNotAllowed);
                    break;


                case AddStmt addStmt:
                    unavailable = ProcessExpr(unavailable, addStmt.Variable);
                    unavailable = ProcessExpr(unavailable, addStmt.Value);
                    break;

                case InsertStmt insertStmt:
                    unavailable = ProcessExpr(unavailable, insertStmt.Variable);
                    unavailable = ProcessExpr(unavailable, insertStmt.Index);
                    unavailable = ProcessExpr(unavailable, insertStmt.Value);
                    break;

                case RemoveStmt removeStmt:
                    unavailable = ProcessExpr(unavailable, removeStmt.Variable);
                    unavailable = ProcessExpr(unavailable, removeStmt.Value);
                    break;

                case WhileStmt whileStmt:
                    unavailable = ProcessExpr(unavailable, whileStmt.Condition);
                    // process running the body twice. on the first go, the loop can potentially
                    // relinquish additional variables on the second go, either the body will use
                    // one of these variables and throw or reach a fixed point since all paths are
                    // considered simultaneously. Then, we continue our overapproximation by taking
                    // the union of no runs and one or more runs.
                    ISet<Variable> bodyUnavailable =
                        ProcessStatement(new HashSet<Variable>(unavailable), whileStmt.Body);
                    bodyUnavailable = ProcessExpr(bodyUnavailable, whileStmt.Condition);
                    // TODO: more efficient way of doing this?
                    bodyUnavailable = ProcessStatement(bodyUnavailable, whileStmt.Body);
                    bodyUnavailable = ProcessExpr(bodyUnavailable, whileStmt.Condition);
                    unavailable.UnionWith(bodyUnavailable);
                    break;

                case IfStmt ifStmt:
                    unavailable = ProcessExpr(unavailable, ifStmt.Condition);
                    ISet<Variable> thenUnavailable =
                        ProcessStatement(new HashSet<Variable>(unavailable), ifStmt.ThenBranch);
                    ISet<Variable> elseUnavailable =
                        ProcessStatement(new HashSet<Variable>(unavailable), ifStmt.ElseBranch);
                    thenUnavailable.UnionWith(elseUnavailable);
                    unavailable = thenUnavailable;
                    break;

                case CtorStmt ctorStmt:
                    unavailable = ProcessArgList(ctorStmt.Arguments, unavailable, ArgOptions.SwapNotAllowed);
                    break;

                case FunCallStmt funCallStmt:
                    unavailable = ProcessArgList(funCallStmt.ArgsList, unavailable);
                    funCallStmts.Add(funCallStmt);
                    break;

                case RaiseStmt raiseStmt:
                    unavailable = ProcessExpr(unavailable, raiseStmt.PEvent);
                    unavailable = ProcessArgList(raiseStmt.Payload, unavailable, ArgOptions.SwapNotAllowed);
                    break;

                case SendStmt sendStmt:
                    unavailable = ProcessExpr(unavailable, sendStmt.MachineExpr);
                    unavailable = ProcessExpr(unavailable, sendStmt.Evt);
                    unavailable = ProcessArgList(sendStmt.Arguments, unavailable, ArgOptions.SwapNotAllowed);
                    break;

                case AnnounceStmt announceStmt:
                    unavailable = ProcessExpr(unavailable, announceStmt.PEvent);
                    if (announceStmt.Payload != null)
                    {
                        unavailable = ProcessExpr(unavailable, announceStmt.Payload);
                    }

                    break;

                case GotoStmt gotoStmt:
                    if (gotoStmt.Payload != null)
                    {
                        unavailable = ProcessExpr(unavailable, gotoStmt.Payload);
                    }

                    break;

                case ReceiveStmt receiveStmt:
                    HashSet<Variable> postUnavailable = new HashSet<Variable>();
                    HashSet<Variable> caseVariables = new HashSet<Variable>();
                    foreach (KeyValuePair<PEvent, Function> recvCase in receiveStmt.Cases)
                    {
                        ISet<Variable> caseUnavailable =
                            ProcessStatement(new HashSet<Variable>(unavailable), recvCase.Value.Body);
                        postUnavailable.UnionWith(caseUnavailable);
                        caseVariables.UnionWith(recvCase.Value.Signature.Parameters);
                    }

                    unavailable = postUnavailable;
                    unavailable.ExceptWith(caseVariables);
                    break;

                case PopStmt _:
                case NoStmt _:
                    // nothing to check
                    break;

                default:
                    throw handler.InternalError(statement.SourceLocation,
                        new ArgumentOutOfRangeException(nameof(statement)));
            }

            return unavailable;
        }

        private ISet<Variable> ProcessArgList(IEnumerable<IPExpr> arguments, ISet<Variable> unavailable,
            ArgOptions perm = ArgOptions.SwapAllowed)
        {
            bool swapAllowed = perm == ArgOptions.SwapAllowed;
            List<ILinearRef> remainingLinearRefs = new List<ILinearRef>();
            foreach (IPExpr argument in arguments)
            {
                if (argument is ILinearRef linearRef)
                {
                    if (linearRef.LinearType.Equals(LinearType.Swap) && !swapAllowed)
                    {
                        throw handler.InvalidSwap(linearRef, "swap not allowed in this context");
                    }

                    remainingLinearRefs.Add(linearRef);
                }
                else
                {
                    unavailable = ProcessExpr(unavailable, argument);
                }
            }

            return remainingLinearRefs.Aggregate(unavailable, ProcessExpr);
        }

        private ISet<Variable> ProcessExpr(ISet<Variable> unavailable, IPExpr expression)
        {
            Contract.Requires(expression != null);
            switch (expression)
            {
                case CloneExpr cloneExpr:
                    unavailable = ProcessExpr(unavailable, cloneExpr.Term);
                    break;

                case CastExpr castExpr:
                    unavailable = ProcessExpr(unavailable, castExpr.SubExpr);
                    break;

                case CoerceExpr coerceExpr:
                    unavailable = ProcessExpr(unavailable, coerceExpr.SubExpr);
                    break;

                case ContainsExpr containsKeyExpr:
                    unavailable = ProcessExpr(unavailable, containsKeyExpr.Collection);
                    unavailable = ProcessExpr(unavailable, containsKeyExpr.Item);
                    break;

                case CtorExpr ctorExpr:
                    unavailable = ProcessArgList(ctorExpr.Arguments, unavailable, ArgOptions.SwapNotAllowed);
                    break;

                case FunCallExpr funCallExpr:
                    unavailable = ProcessArgList(funCallExpr.Arguments, unavailable);
                    funCallExprs.Add(funCallExpr);
                    break;

                case KeysExpr keysExpr:
                    unavailable = ProcessExpr(unavailable, keysExpr.Expr);
                    break;

                case LinearAccessRefExpr linearAccessRefExpr:
                    if (unavailable.Contains(linearAccessRefExpr.Variable) ||
                        linearAccessRefExpr.Variable.Role.Equals(VariableRole.Field))
                    {
                        throw handler.RelinquishedWithoutOwnership(linearAccessRefExpr);
                    }

                    if (linearAccessRefExpr.LinearType.Equals(LinearType.Move))
                    {
                        unavailable.Add(linearAccessRefExpr.Variable);
                    }

                    break;

                case UnaryOpExpr unaryOp:
                    unavailable = ProcessExpr(unavailable, unaryOp.SubExpr);
                    break;

                case MapAccessExpr mapAccessExpr:
                    unavailable = ProcessExpr(unavailable, mapAccessExpr.MapExpr);
                    unavailable = ProcessExpr(unavailable, mapAccessExpr.IndexExpr);
                    break;

                case BinOpExpr binOp:
                    unavailable = ProcessExpr(unavailable, binOp.Lhs);
                    unavailable = ProcessExpr(unavailable, binOp.Rhs);
                    break;

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    unavailable = ProcessExpr(unavailable, namedTupleAccessExpr.SubExpr);
                    break;

                case NamedTupleExpr namedTupleExpr:
                    unavailable = ProcessArgList(namedTupleExpr.TupleFields, unavailable, ArgOptions.SwapNotAllowed);
                    break;

                case SeqAccessExpr seqAccessExpr:
                    unavailable = ProcessExpr(unavailable, seqAccessExpr.SeqExpr);
                    unavailable = ProcessExpr(unavailable, seqAccessExpr.IndexExpr);
                    break;

                case SizeofExpr sizeofExpr:
                    unavailable = ProcessExpr(unavailable, sizeofExpr.Expr);
                    break;

                case TupleAccessExpr tupleAccessExpr:
                    unavailable = ProcessExpr(unavailable, tupleAccessExpr.SubExpr);
                    break;

                case UnnamedTupleExpr unnamedTupleExpr:
                    unavailable = ProcessArgList(unnamedTupleExpr.TupleFields, unavailable, ArgOptions.SwapNotAllowed);
                    break;

                case ValuesExpr valuesExpr:
                    unavailable = ProcessExpr(unavailable, valuesExpr.Expr);
                    break;

                case VariableAccessExpr variableAccessExpr:
                    if (unavailable.Contains(variableAccessExpr.Variable))
                    {
                        throw handler.UseWithoutOwnership(variableAccessExpr);
                    }

                    break;

                case BoolLiteralExpr _:
                case DefaultExpr _:
                case EnumElemRefExpr _:
                case EventRefExpr _:
                case FairNondetExpr _:
                case FloatLiteralExpr _:
                case IntLiteralExpr _:
                case StringLiteralExpr _:
                case NondetExpr _:
                case NullLiteralExpr _:
                case ThisRefExpr _:
                    // nothing to do
                    break;

                default:
                    throw handler.InternalError(expression.SourceLocation,
                        new ArgumentOutOfRangeException(nameof(expression)));
            }

            return unavailable;
        }

        private enum ArgOptions
        {
            SwapAllowed,
            SwapNotAllowed
        }
    }
}