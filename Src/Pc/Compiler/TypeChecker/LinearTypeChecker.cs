using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker
{
    public class LinearTypeChecker
    {
        private readonly ITranslationErrorHandler handler;

        private LinearTypeChecker(ITranslationErrorHandler handler)
        {
            this.handler = handler;
        }

        public static void AnalyzeMethods(ITranslationErrorHandler handler, List<Function> allFunctions)
        {
            var checker = new LinearTypeChecker(handler);
            var needToCheckCallSites = new List<Function>();
            foreach (Function function in allFunctions)
            {
                ISet<Variable> unavailable = checker.ProcessStatement(function.Body, new HashSet<Variable>());
                if (unavailable.Any(var => var.Role.Equals(VariableRole.Param)))
                {
                    // TODO: mark parameters as available / unavailable
                    needToCheckCallSites.Add(function);
                }
            }
        }

        private ISet<Variable> ProcessStatement(IPStmt statement, ISet<Variable> unavailable)
        {
            switch (statement)
            {
                case null:
                    throw new ArgumentOutOfRangeException(nameof(statement));
                case CompoundStmt compoundStmt:
                    foreach (IPStmt stmtStatement in compoundStmt.Statements)
                    {
                        unavailable = ProcessStatement(stmtStatement, unavailable);
                    }
                    break;
                case PopStmt _:
                    // nothing to check
                    break;
                case AssertStmt assertStmt:
                    unavailable = ProcessExpr(assertStmt.Assertion, unavailable);
                    break;
                case PrintStmt printStmt:
                    unavailable = ProcessArgList(printStmt.Args, unavailable, false);
                    break;
                case ReturnStmt returnStmt:
                    if (returnStmt.ReturnValue != null)
                    {
                        unavailable = ProcessExpr(returnStmt.ReturnValue, unavailable);
                    }
                    break;
                case AssignStmt assignStmt:
                    unavailable = ProcessExpr(assignStmt.Value, unavailable);
                    if (assignStmt.Variable is VariableAccessExpr assignAccess)
                    {
                        unavailable.Remove(assignAccess.Variable);
                    }
                    else
                    {
                        unavailable = ProcessExpr(assignStmt.Variable, unavailable);
                    }
                    break;
                case MoveAssignStmt moveAssignStmt:
                    if (moveAssignStmt.FromVariable.Role.Equals(VariableRole.Field))
                    {
                        throw handler.IssueError(null, $"cannot move field {moveAssignStmt.FromVariable.Name}");
                    }
                    unavailable.Add(moveAssignStmt.FromVariable);

                    if (moveAssignStmt.ToLocation is VariableAccessExpr moveAssignAccess)
                    {
                        unavailable.Remove(moveAssignAccess.Variable);
                    }
                    else
                    {
                        unavailable = ProcessExpr(moveAssignStmt.ToLocation, unavailable);
                    }
                    break;
                case SwapAssignStmt swapAssignStmt:
                    if (swapAssignStmt.NewLocation is VariableAccessExpr swapAssignAccess)
                    {
                        if (unavailable.Contains(swapAssignAccess.Variable) ||
                            swapAssignStmt.OldLocation.Role.Equals(VariableRole.Field))
                        {
                            throw handler.IssueError(
                                null, $"cannot swap unavailable variable {swapAssignAccess.Variable.Name}");
                        }
                    }
                    else
                    {
                        unavailable = ProcessExpr(swapAssignStmt.NewLocation, unavailable);
                    }

                    if (unavailable.Contains(swapAssignStmt.OldLocation) ||
                        swapAssignStmt.OldLocation.Role.Equals(VariableRole.Field))
                    {
                        throw handler.IssueError(
                            null, $"cannot swap unavailable variable {swapAssignStmt.OldLocation.Name}");
                    }
                    break;
                case InsertStmt insertStmt:
                    unavailable = ProcessExpr(insertStmt.Variable, unavailable);
                    unavailable = ProcessExpr(insertStmt.Index, unavailable);
                    unavailable = ProcessExpr(insertStmt.Value, unavailable);
                    break;
                case RemoveStmt removeStmt:
                    unavailable = ProcessExpr(removeStmt.Variable, unavailable);
                    unavailable = ProcessExpr(removeStmt.Value, unavailable);
                    break;
                case WhileStmt whileStmt:
                    unavailable = ProcessExpr(whileStmt.Condition, unavailable);
                    ISet<Variable> bodyUnavailable =
                        ProcessStatement(whileStmt.Body, new HashSet<Variable>(unavailable));
                    bodyUnavailable = ProcessExpr(whileStmt.Condition, bodyUnavailable);
                    if (bodyUnavailable.IsProperSupersetOf(unavailable))
                    {
                        throw handler.IssueError(null, "while loops must not relinquish variables");
                    }
                    break;
                case IfStmt ifStmt:
                    unavailable = ProcessExpr(ifStmt.Condition, unavailable);
                    ISet<Variable> thenUnavailable =
                        ProcessStatement(ifStmt.ThenBranch, new HashSet<Variable>(unavailable));
                    ISet<Variable> elseUnavailable =
                        ProcessStatement(ifStmt.ElseBranch, new HashSet<Variable>(unavailable));
                    thenUnavailable.UnionWith(elseUnavailable);
                    unavailable = thenUnavailable;
                    break;
                case CtorStmt ctorStmt:
                    unavailable = ProcessArgList(ctorStmt.Arguments, unavailable);
                    break;
                case FunCallStmt funCallStmt:
                    unavailable = ProcessArgList(funCallStmt.ArgsList, unavailable);
                    break;
                case RaiseStmt raiseStmt:
                    unavailable = ProcessExpr(raiseStmt.PEvent, unavailable);
                    if (raiseStmt.Payload != null)
                    {
                        unavailable = ProcessExpr(raiseStmt.Payload, unavailable);
                    }
                    break;
                case SendStmt sendStmt:
                    unavailable = ProcessExpr(sendStmt.MachineExpr, unavailable);
                    unavailable = ProcessExpr(sendStmt.Evt, unavailable);
                    unavailable = ProcessArgList(sendStmt.ArgsList, unavailable, false);
                    break;
                case AnnounceStmt announceStmt:
                    unavailable = ProcessExpr(announceStmt.PEvent, unavailable);
                    if (announceStmt.Payload != null)
                    {
                        unavailable = ProcessExpr(announceStmt.Payload, unavailable);
                    }
                    break;
                case GotoStmt gotoStmt:
                    if (gotoStmt.Payload != null)
                    {
                        unavailable = ProcessExpr(gotoStmt.Payload, unavailable);
                    }
                    break;
                case ReceiveStmt receiveStmt:
                    var postUnavailable = new HashSet<Variable>();
                    var caseVariables = new HashSet<Variable>();
                    foreach (KeyValuePair<PEvent, Function> recvCase in receiveStmt.Cases)
                    {
                        ISet<Variable> caseUnavailable =
                            ProcessStatement(recvCase.Value.Body, new HashSet<Variable>(unavailable));
                        postUnavailable.UnionWith(caseUnavailable);
                        caseVariables.UnionWith(recvCase.Value.Signature.Parameters);
                    }
                    unavailable = postUnavailable;
                    unavailable.ExceptWith(caseVariables);
                    break;
                case NoStmt noStmt:
                    // no-op. no effect
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }
            return unavailable;
        }

        private ISet<Variable> ProcessArgList(IEnumerable<IPExpr> arguments, ISet<Variable> unavailable,
                                              bool swapAllowed = true)
        {
            var remainingLinearRefs = new List<ILinearRef>();
            foreach (IPExpr argument in arguments)
            {
                if (argument is ILinearRef linearRef)
                {
                    if (linearRef.LinearType.Equals(LinearType.Swap) && !swapAllowed)
                    {
                        throw handler.InvalidSwap(null, linearRef, "swap not allowed in this context");
                    }
                    remainingLinearRefs.Add(linearRef);
                }
                else
                {
                    unavailable = ProcessExpr(argument, unavailable);
                }
            }
            foreach (ILinearRef linearRef in remainingLinearRefs)
            {
                unavailable = ProcessExpr(linearRef, unavailable);
            }
            return unavailable;
        }

        private ISet<Variable> ProcessExpr(IPExpr expression, ISet<Variable> unavailable)
        {
            switch (expression)
            {
                case null:
                    throw new ArgumentOutOfRangeException(nameof(expression));
                case BoolLiteralExpr _:
                    // nothing to do
                    break;
                case CastExpr castExpr:
                    unavailable = ProcessExpr(castExpr.SubExpr, unavailable);
                    break;
                case CoerceExpr coerceExpr:
                    unavailable = ProcessExpr(coerceExpr.SubExpr, unavailable);
                    break;
                case ContainsKeyExpr containsKeyExpr:
                    unavailable = ProcessExpr(containsKeyExpr.Map, unavailable);
                    unavailable = ProcessExpr(containsKeyExpr.Key, unavailable);
                    break;
                case CtorExpr ctorExpr:
                    unavailable = ProcessArgList(ctorExpr.Arguments, unavailable);
                    break;
                case DefaultExpr _:
                    // nothing to do
                    break;
                case EnumElemRefExpr _:
                    // nothing to do
                    break;
                case EventRefExpr _:
                    // nothing to do
                    break;
                case FairNondetExpr _:
                    // nothing to do
                    break;
                case FloatLiteralExpr _:
                    // nothing to do
                    break;
                case FunCallExpr funCallExpr:
                    unavailable = ProcessArgList(funCallExpr.Arguments, unavailable);
                    break;
                case IntLiteralExpr _:
                    // nothing to do
                    break;
                case KeysExpr keysExpr:
                    unavailable = ProcessExpr(keysExpr.Expr, unavailable);
                    break;
                case LinearAccessRefExpr linearAccessRefExpr:
                    if (unavailable.Contains(linearAccessRefExpr.Variable))
                    {
                        throw handler.IssueError(null, $"variable {linearAccessRefExpr.Variable} is not available");
                    }
                    if (linearAccessRefExpr.LinearType.Equals(LinearType.Move))
                    {
                        unavailable.Add(linearAccessRefExpr.Variable);
                    }
                    break;
                case UnaryOpExpr unaryOp:
                    unavailable = ProcessExpr(unaryOp.SubExpr, unavailable);
                    break;
                case MapAccessExpr mapAccessExpr:
                    unavailable = ProcessExpr(mapAccessExpr.MapExpr, unavailable);
                    unavailable = ProcessExpr(mapAccessExpr.IndexExpr, unavailable);
                    break;
                case BinOpExpr binOp:
                    unavailable = ProcessExpr(binOp.Lhs, unavailable);
                    unavailable = ProcessExpr(binOp.Rhs, unavailable);
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    unavailable = ProcessExpr(namedTupleAccessExpr.SubExpr, unavailable);
                    break;
                case NamedTupleExpr namedTupleExpr:
                    unavailable = ProcessArgList(namedTupleExpr.TupleFields, unavailable, false);
                    break;
                case NondetExpr _:
                    // nothing to do
                    break;
                case NullLiteralExpr _:
                    // nothing to do
                    break;
                case SeqAccessExpr seqAccessExpr:
                    unavailable = ProcessExpr(seqAccessExpr.SeqExpr, unavailable);
                    unavailable = ProcessExpr(seqAccessExpr.IndexExpr, unavailable);
                    break;
                case SizeofExpr sizeofExpr:
                    unavailable = ProcessExpr(sizeofExpr.Expr, unavailable);
                    break;
                case ThisRefExpr _:
                    // nothing to do
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    unavailable = ProcessExpr(tupleAccessExpr.SubExpr, unavailable);
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    unavailable = ProcessArgList(unnamedTupleExpr.TupleFields, unavailable, false);
                    break;
                case ValuesExpr valuesExpr:
                    unavailable = ProcessExpr(valuesExpr.Expr, unavailable);
                    break;
                case VariableAccessExpr variableAccessExpr:
                    if (unavailable.Contains(variableAccessExpr.Variable))
                    {
                        throw handler.IssueError(null, $"variable {variableAccessExpr.Variable.Name} not available");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }
            return unavailable;
        }
    }
}