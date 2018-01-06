using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    internal class IRTransformer
    {
        private readonly Function function;
        private int numTemp;

        private IRTransformer(Function function)
        {
            this.function = function;
        }

        public static void SimplifyMethod(Function function)
        {
            var transformer = new IRTransformer(function);
            function.Body = transformer.SimplifyFunctionBody(function.Body);
        }

        private Tuple<Variable, IPStmt> SaveInTemporary(IPExpr expr)
        {
            ParserRuleContext location = expr.SourceLocation;
            var temp = new Variable($"$tmp{numTemp++}", location, VariableRole.Local) {Type = expr.Type};
            function.AddLocalVariable(temp);
            var stmt = new AssignStmt(location, new VariableAccessExpr(location, temp), expr);
            return Tuple.Create(temp, (IPStmt) stmt);
        }

        private Tuple<IPExpr, List<IPStmt>> SimplifyLvalue(IPExpr expr)
        {
            ParserRuleContext location = expr.SourceLocation;
            PLanguageType type = expr.Type;
            Tuple<IPExpr, List<IPStmt>> lvalue;
            Tuple<IPExpr, List<IPStmt>> index;
            IPExpr newAccess;
            List<IPStmt> dependencies;
            switch (expr)
            {
                case null:
                    throw new ArgumentOutOfRangeException(nameof(expr));
                case MapAccessExpr mapAccessExpr:
                    lvalue = SimplifyLvalue(mapAccessExpr.MapExpr);
                    index = SimplifyExpression(mapAccessExpr.IndexExpr);
                    newAccess = new MapAccessExpr(location, lvalue.Item1, index.Item1, type);
                    dependencies = lvalue.Item2.Concat(index.Item2).ToList();
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    lvalue = SimplifyLvalue(namedTupleAccessExpr.SubExpr);
                    newAccess = new NamedTupleAccessExpr(location, lvalue.Item1, namedTupleAccessExpr.Entry);
                    dependencies = lvalue.Item2;
                    break;
                case SeqAccessExpr seqAccessExpr:
                    lvalue = SimplifyLvalue(seqAccessExpr.SeqExpr);
                    index = SimplifyExpression(seqAccessExpr.IndexExpr);
                    newAccess = new SeqAccessExpr(location, lvalue.Item1, index.Item1, type);
                    dependencies = lvalue.Item2.Concat(index.Item2).ToList();
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    lvalue = SimplifyLvalue(tupleAccessExpr.SubExpr);
                    newAccess = new TupleAccessExpr(location, lvalue.Item1, tupleAccessExpr.FieldNo, type);
                    dependencies = lvalue.Item2;
                    break;
                case VariableAccessExpr variableAccessExpr:
                    newAccess = variableAccessExpr;
                    dependencies = new List<IPStmt>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expr));
            }

            return Tuple.Create(newAccess, dependencies);
        }

        private Tuple<IPExpr, List<IPStmt>> SimplifyExpression(IPExpr expr)
        {
            ParserRuleContext location = expr.SourceLocation;
            List<IPStmt> deps = new List<IPStmt>();
            switch (expr)
            {
                case null:
                    throw new ArgumentNullException(nameof(expr));
                case BinOpExpr binOpExpr:
                    Tuple<IPExpr, List<IPStmt>> binLhs = SimplifyExpression(binOpExpr.Lhs);
                    Tuple<IPExpr, List<IPStmt>> binRhs = SimplifyExpression(binOpExpr.Rhs);
                    var storeBinOp = SaveInTemporary(new BinOpExpr(location, binOpExpr.Operation, binLhs.Item1, binRhs.Item1));
                    IPExpr opExpr = new VariableAccessExpr(location, storeBinOp.Item1);
                    deps.AddRange(binLhs.Item2.Concat(binRhs.Item2));
                    deps.Add(storeBinOp.Item2);
                    return Tuple.Create(opExpr, deps);
                case BoolLiteralExpr boolLiteralExpr:
                    break;
                case CastExpr castExpr:
                    break;
                case CoerceExpr coerceExpr:
                    break;
                case ContainsKeyExpr containsKeyExpr:
                    break;
                case CtorExpr ctorExpr:
                    break;
                case DefaultExpr defaultExpr:
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    break;
                case EventRefExpr eventRefExpr:
                    break;
                case FairNondetExpr fairNondetExpr:
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                    break;
                case FunCallExpr funCallExpr:
                    break;
                case IntLiteralExpr intLiteralExpr:
                    break;
                case KeysExpr keysExpr:
                    break;
                case LinearAccessRefExpr linearAccessRefExpr:
                    break;
                case MapAccessExpr mapAccessExpr:
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    break;
                case NamedTupleExpr namedTupleExpr:
                    break;
                case NondetExpr nondetExpr:
                    break;
                case NullLiteralExpr nullLiteralExpr:
                    break;
                case SeqAccessExpr seqAccessExpr:
                    break;
                case SizeofExpr sizeofExpr:
                    break;
                case ThisRefExpr thisRefExpr:
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    break;
                case UnaryOpExpr unaryOpExpr:
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    break;
                case ValuesExpr valuesExpr:
                    break;
                case VariableAccessExpr variableAccessExpr:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expr));
            }

            return Tuple.Create(expr, new List<IPStmt>());
        }

        private static IPStmt Flatten(IPStmt stmt)
        {
            if (!(stmt is CompoundStmt compound))
            {
                return stmt;
            }

            var newBody = new List<IPStmt>();
            foreach (IPStmt innerStmt in compound.Statements)
            {
                if (innerStmt is CompoundStmt nested)
                {
                    newBody.AddRange(nested.Statements);
                }
                else
                {
                    newBody.Add(innerStmt);
                }
            }

            return new CompoundStmt(compound.SourceLocation, newBody);
        }

        private IPStmt SimplifyFunctionBody(IPStmt functionBody)
        {
            return Flatten(new CompoundStmt(functionBody.SourceLocation, SimplifyStatement(functionBody)));
        }

        private List<IPStmt> SimplifyStatement(IPStmt statement)
        {
            ParserRuleContext location = statement?.SourceLocation;
            switch (statement)
            {
                case null:
                    throw new ArgumentNullException(nameof(statement));
                case AnnounceStmt announceStmt:
                    Tuple<IPExpr, List<IPStmt>> annEvent = SimplifyExpression(announceStmt.PEvent);
                    Tuple<IPExpr, List<IPStmt>> annPayload = SimplifyExpression(announceStmt.Payload);
                    return annEvent.Item2.Concat(annPayload.Item2).Concat(new[]
                    {
                        new AnnounceStmt(location, annEvent.Item1, annPayload.Item1)
                    }).ToList();
                case AssertStmt assertStmt:
                    Tuple<IPExpr, List<IPStmt>> assertCondition = SimplifyExpression(assertStmt.Assertion);
                    return assertCondition.Item2.Concat(new[]
                    {
                        new AssertStmt(location, assertCondition.Item1, assertStmt.Message)
                    }).ToList();
                case AssignStmt assignStmt:
                    Tuple<IPExpr, List<IPStmt>> assign = SimplifyLvalue(assignStmt.Variable);
                    Tuple<IPExpr, List<IPStmt>> assignedValue = SimplifyExpression(assignStmt.Value);
                    return assign.Item2.Concat(assignedValue.Item2).Concat(new[]
                    {
                        new AssignStmt(location, assign.Item1, assignedValue.Item1)
                    }).ToList();
                case CompoundStmt compoundStmt:
                    var newBlock = new List<IPStmt>();
                    foreach (IPStmt step in compoundStmt.Statements)
                    {
                        newBlock.AddRange(SimplifyStatement(step));
                    }

                    return new List<IPStmt> {new CompoundStmt(location, newBlock)};
                case CtorStmt ctorStmt:
                    var ctorDeps = new List<IPStmt>();
                    var newCtorArgs = new List<IPExpr>();
                    foreach (IPExpr ctorStmtArgument in ctorStmt.Arguments)
                    {
                        Tuple<IPExpr, List<IPStmt>> ctorArg = SimplifyExpression(ctorStmtArgument);
                        newCtorArgs.Add(ctorArg.Item1);
                        ctorDeps.AddRange(ctorArg.Item2);
                    }

                    return ctorDeps.Concat(new[]
                    {
                        new CtorStmt(location, ctorStmt.Machine, newCtorArgs)
                    }).ToList();
                case FunCallStmt funCallStmt:
                    var funDeps = new List<IPStmt>();
                    var newFunArgs = new List<IPExpr>();
                    foreach (IPExpr funStmtArg in funCallStmt.ArgsList)
                    {
                        Tuple<IPExpr, List<IPStmt>> funArg = SimplifyExpression(funStmtArg);
                        newFunArgs.Add(funArg.Item1);
                        funDeps.AddRange(funArg.Item2);
                    }

                    return funDeps.Concat(new[]
                    {
                        new FunCallStmt(location, funCallStmt.Fun, newFunArgs)
                    }).ToList();
                case GotoStmt gotoStmt:
                    Tuple<IPExpr, List<IPStmt>> gotoPayload = SimplifyExpression(gotoStmt.Payload);
                    return gotoPayload.Item2.Concat(new[]
                    {
                        new GotoStmt(location, gotoStmt.State, gotoPayload.Item1)
                    }).ToList();
                case IfStmt ifStmt:
                    Tuple<IPExpr, List<IPStmt>> ifCondition = SimplifyExpression(ifStmt.Condition);
                    List<IPStmt> thenBranch = SimplifyStatement(ifStmt.ThenBranch);
                    List<IPStmt> elseBranch = SimplifyStatement(ifStmt.ElseBranch);
                    return ifCondition.Item2.Concat(new[]
                    {
                        new IfStmt(location, ifCondition.Item1,
                                   new CompoundStmt(ifStmt.ThenBranch.SourceLocation, thenBranch),
                                   new CompoundStmt(ifStmt.ElseBranch.SourceLocation, elseBranch))
                    }).ToList();
                case InsertStmt insertStmt:
                    Tuple<IPExpr, List<IPStmt>> insertLValue = SimplifyLvalue(insertStmt.Variable);
                    Tuple<IPExpr, List<IPStmt>> insertIndex = SimplifyExpression(insertStmt.Index);
                    Tuple<IPExpr, List<IPStmt>> insertValue = SimplifyExpression(insertStmt.Value);
                    return insertLValue.Item2.Concat(insertIndex.Item2).Concat(insertValue.Item2).Concat(new[]
                    {
                        new InsertStmt(location, insertLValue.Item1, insertIndex.Item1, insertValue.Item1)
                    }).ToList();
                case MoveAssignStmt moveAssignStmt:
                    Tuple<IPExpr, List<IPStmt>> moveDestLValue = SimplifyLvalue(moveAssignStmt.ToLocation);
                    return moveDestLValue.Item2.Concat(new[]
                    {
                        new MoveAssignStmt(location, moveDestLValue.Item1, moveAssignStmt.FromVariable)
                    }).ToList();
                case NoStmt _:
                    return new List<IPStmt>();
                case PopStmt popStmt:
                    return new List<IPStmt> {popStmt};
                case PrintStmt printStmt:
                    var deps = new List<IPStmt>();
                    var newArgs = new List<IPExpr>();
                    foreach (IPExpr printStmtArg in printStmt.Args)
                    {
                        Tuple<IPExpr, List<IPStmt>> printArg = SimplifyExpression(printStmtArg);
                        newArgs.Add(printArg.Item1);
                        deps.AddRange(printArg.Item2);
                    }

                    return deps.Concat(new[]
                    {
                        new PrintStmt(location, printStmt.Message, newArgs)
                    }).ToList();
                case RaiseStmt raiseStmt:
                    Tuple<IPExpr, List<IPStmt>> raiseEvent = SimplifyExpression(raiseStmt.PEvent);
                    var raisePayloadArgs = new IPExpr[raiseStmt.Payload.Length];
                    var raisePayloadDeps = new List<IPStmt>();
                    for (var index = 0; index < raiseStmt.Payload.Length; index++)
                    {
                        Tuple<IPExpr, List<IPStmt>> arg = SimplifyExpression(raiseStmt.Payload[index]);
                        raisePayloadArgs[index] = arg.Item1;
                        raisePayloadDeps.AddRange(arg.Item2);
                    }

                    return raiseEvent.Item2.Concat(raisePayloadDeps).Concat(new[]
                    {
                        new RaiseStmt(location, raiseEvent.Item1, raisePayloadArgs)
                    }).ToList();
                case ReceiveStmt receiveStmt:
                    foreach (Function recvCase in receiveStmt.Cases.Values)
                    {
                        SimplifyMethod(recvCase);
                    }

                    return new List<IPStmt> {receiveStmt};
                case RemoveStmt removeStmt:
                    Tuple<IPExpr, List<IPStmt>> removeLvalue = SimplifyLvalue(removeStmt.Variable);
                    Tuple<IPExpr, List<IPStmt>> removeKey = SimplifyExpression(removeStmt.Value);
                    return removeLvalue.Item2.Concat(removeKey.Item2).Concat(new[]
                    {
                        new RemoveStmt(location, removeLvalue.Item1, removeKey.Item1)
                    }).ToList();
                case ReturnStmt returnStmt:
                    if (returnStmt.ReturnValue == null)
                    {
                        return new List<IPStmt> {returnStmt};
                    }

                    Tuple<IPExpr, List<IPStmt>> returnValue = SimplifyExpression(returnStmt.ReturnValue);
                    return returnValue.Item2.Concat(new[]
                    {
                        new ReturnStmt(location, returnValue.Item1)
                    }).ToList();
                case SendStmt sendStmt:
                    Tuple<IPExpr, List<IPStmt>> sendMachine = SimplifyExpression(sendStmt.MachineExpr);
                    Tuple<IPExpr, List<IPStmt>> sendEvent = SimplifyExpression(sendStmt.Evt);
                    var sendArgs = new List<IPExpr>();
                    var sendArgDeps = new List<IPStmt>();
                    foreach (IPExpr pExpr in sendStmt.ArgsList)
                    {
                        Tuple<IPExpr, List<IPStmt>> sendArg = SimplifyExpression(pExpr);
                        sendArgs.Add(sendArg.Item1);
                        sendArgDeps.AddRange(sendArg.Item2);
                    }

                    return sendMachine.Item2.Concat(sendEvent.Item2).Concat(sendArgDeps).Concat(new[]
                    {
                        new SendStmt(location, sendMachine.Item1, sendEvent.Item1, sendArgs)
                    }).ToList();
                case SwapAssignStmt swapAssignStmt:
                    Tuple<IPExpr, List<IPStmt>> swapLvalue = SimplifyLvalue(swapAssignStmt.NewLocation);
                    return swapLvalue.Item2.Concat(new[]
                    {
                        new SwapAssignStmt(location, swapLvalue.Item1, swapAssignStmt.OldLocation)
                    }).ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }
        }
    }
}
