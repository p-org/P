using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend
{
    public class IRTransformer
    {
        private readonly Function function;
        private int numTemp;

        private IRTransformer(Function function) { this.function = function; }

        public static void SimplifyMethod(Function function)
        {
            var transformer = new IRTransformer(function);
            function.Body = transformer.SimplifyFunctionBody(function.Body);
        }

        private (Variable, IPStmt) SaveInTemporary(IPExpr expr)
        {
            ParserRuleContext location = expr.SourceLocation;
            var temp = function.Scope.Put($"$tmp{numTemp++}", expr.SourceLocation, VariableRole.Local);
            temp.Type = expr.Type;
            function.AddLocalVariable(temp);
            var stmt = new AssignStmt(location, new VariableAccessExpr(location, temp), expr);
            return (temp, stmt);
        }

        private (IPExpr, List<IPStmt>) SimplifyLvalue(IPExpr expr)
        {
            ParserRuleContext location = expr.SourceLocation;
            PLanguageType type = expr.Type;
            switch (expr)
            {
                case MapAccessExpr mapAccessExpr:
                    var (mapExpr, mapExprDeps) = SimplifyLvalue(mapAccessExpr.MapExpr);
                    var (mapIndex, mapIndexDeps) = SimplifyExpression(mapAccessExpr.IndexExpr);
                    return (new MapAccessExpr(location, mapExpr, mapIndex, type),
                        mapExprDeps.Concat(mapIndexDeps).ToList());
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    var (ntExpr, ntExprDeps) = SimplifyLvalue(namedTupleAccessExpr.SubExpr);
                    return (new NamedTupleAccessExpr(location, ntExpr, namedTupleAccessExpr.Entry), ntExprDeps);
                case SeqAccessExpr seqAccessExpr:
                    var (seqExpr, seqExprDeps) = SimplifyLvalue(seqAccessExpr.SeqExpr);
                    var (seqIndex, seqIndexDeps) = SimplifyExpression(seqAccessExpr.IndexExpr);
                    return (new SeqAccessExpr(location, seqExpr, seqIndex, type),
                        seqExprDeps.Concat(seqIndexDeps).ToList());
                case TupleAccessExpr tupleAccessExpr:
                    var (tupExpr, tupExprDeps) = SimplifyLvalue(tupleAccessExpr.SubExpr);
                    return (new TupleAccessExpr(location, tupExpr, tupleAccessExpr.FieldNo, type), tupExprDeps);
                case VariableAccessExpr variableAccessExpr:
                    return (variableAccessExpr, new List<IPStmt>());
                default:
                    throw new ArgumentOutOfRangeException(nameof(expr));
            }
        }

        private (IPExpr, List<IPStmt>) SimplifyExpression(IPExpr expr)
        {
            ParserRuleContext location = expr.SourceLocation;
            var deps = new List<IPStmt>();
            switch (expr)
            {
                case BinOpExpr binOpExpr:
                    var (lhsTemp, lhsDeps) = SimplifyExpression(binOpExpr.Lhs);
                    var (rhsTemp, rhsDeps) = SimplifyExpression(binOpExpr.Rhs);
                    var (binOpTemp, binOpStore) =
                        SaveInTemporary(new BinOpExpr(location, binOpExpr.Operation, lhsTemp, rhsTemp));
                    deps.AddRange(lhsDeps.Concat(rhsDeps));
                    deps.Add(binOpStore);
                    return (new VariableAccessExpr(location, binOpTemp), deps);
                case BoolLiteralExpr boolLiteralExpr:
                    return (boolLiteralExpr, deps);
                case CastExpr castExpr:
                    var (castSubExpr, castDeps) = SimplifyExpression(castExpr.SubExpr);
                    var (castTemp, castStore) = SaveInTemporary(new CastExpr(location, castSubExpr, castExpr.Type));
                    deps.AddRange(castDeps);
                    deps.Add(castStore);
                    return (new VariableAccessExpr(location, castTemp), deps);
                case CoerceExpr coerceExpr:
                    var (coerceSubExpr, coerceDeps) = SimplifyExpression(coerceExpr.SubExpr);
                    var (coerceTemp, coerceStore) =
                        SaveInTemporary(new CoerceExpr(location, coerceSubExpr, coerceExpr.NewType));
                    deps.AddRange(coerceDeps);
                    deps.Add(coerceStore);
                    return (new VariableAccessExpr(location, coerceTemp), deps);
                case ContainsKeyExpr containsKeyExpr:
                    var (contKeyExpr, contKeyDeps) = SimplifyExpression(containsKeyExpr.Key);
                    var (contMapExpr, contMapDeps) = SimplifyExpression(containsKeyExpr.Map);
                    var (contTemp, contStore) =
                        SaveInTemporary(new ContainsKeyExpr(location, contKeyExpr, contMapExpr));
                    deps.AddRange(contKeyDeps.Concat(contMapDeps));
                    deps.Add(contStore);
                    return (new VariableAccessExpr(location, contTemp), deps);
                case CtorExpr ctorExpr:
                    var ctorArgs = new IPExpr[ctorExpr.Arguments.Length];
                    for (var i = 0; i < ctorExpr.Arguments.Length; i++)
                    {
                        var (argExpr, argDeps) = SimplifyExpression(ctorExpr.Arguments[i]);
                        ctorArgs[i] = argExpr;
                        deps.AddRange(argDeps);
                    }

                    var (ctorTemp, ctorStore) = SaveInTemporary(new CtorExpr(location, ctorExpr.Machine, ctorArgs));
                    deps.Add(ctorStore);
                    return (new VariableAccessExpr(location, ctorTemp), deps);
                case DefaultExpr defaultExpr:
                    return (defaultExpr, deps);
                case EnumElemRefExpr enumElemRefExpr:
                    return (enumElemRefExpr, deps);
                case EventRefExpr eventRefExpr:
                    return (eventRefExpr, deps);
                case FairNondetExpr fairNondetExpr:
                    return (fairNondetExpr, deps);
                case FloatLiteralExpr floatLiteralExpr:
                    return (floatLiteralExpr, deps);
                case FunCallExpr funCallExpr:
                    var funArgs = new IPExpr[funCallExpr.Arguments.Length];
                    for (var i = 0; i < funCallExpr.Arguments.Length; i++)
                    {
                        var (argExpr, argDeps) = SimplifyExpression(funCallExpr.Arguments[i]);
                        funArgs[i] = argExpr;
                        deps.AddRange(argDeps);
                    }

                    var (funTemp, funStore) = SaveInTemporary(new FunCallExpr(location, funCallExpr.Function, funArgs));
                    deps.Add(funStore);
                    return (new VariableAccessExpr(location, funTemp), deps);
                case IntLiteralExpr intLiteralExpr:
                    return (intLiteralExpr, deps);
                case KeysExpr keysExpr:
                    var (keysColl, keysDeps) = SimplifyExpression(keysExpr.Expr);
                    var (keysTemp, keysStore) = SaveInTemporary(new KeysExpr(location, keysColl, keysExpr.Type));
                    deps.AddRange(keysDeps);
                    deps.Add(keysStore);
                    return (new VariableAccessExpr(location, keysTemp), deps);
                case LinearAccessRefExpr linearAccessRefExpr:
                    return (linearAccessRefExpr, deps);
                case MapAccessExpr mapAccessExpr:
                    var (mapExpr, mapDeps) = SimplifyExpression(mapAccessExpr.MapExpr);
                    var (mapIdxExpr, mapIdxDeps) = SimplifyExpression(mapAccessExpr.IndexExpr);
                    var (mapItemTemp, mapItemStore) =
                        SaveInTemporary(new MapAccessExpr(location, mapExpr, mapIdxExpr, mapAccessExpr.Type));
                    deps.AddRange(mapDeps.Concat(mapIdxDeps));
                    deps.Add(mapItemStore);
                    return (new VariableAccessExpr(location, mapItemTemp), deps);
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    var (ntSubExpr, ntSubDeps) = SimplifyExpression(namedTupleAccessExpr.SubExpr);
                    var (ntTemp, ntStore) =
                        SaveInTemporary(new NamedTupleAccessExpr(location, ntSubExpr, namedTupleAccessExpr.Entry));
                    deps.AddRange(ntSubDeps);
                    deps.Add(ntStore);
                    return (new VariableAccessExpr(location, ntTemp), deps);
                case NamedTupleExpr namedTupleExpr:
                    var ntFields = new IPExpr[namedTupleExpr.TupleFields.Length];
                    for (var i = 0; i < namedTupleExpr.TupleFields.Length; i++)
                    {
                        var (field, fieldDeps) = SimplifyExpression(namedTupleExpr.TupleFields[i]);
                        ntFields[i] = field;
                        deps.AddRange(fieldDeps);
                    }

                    var (ntVal, ntValStore) =
                        SaveInTemporary(new NamedTupleExpr(location, ntFields, namedTupleExpr.Type));
                    deps.Add(ntValStore);
                    return (new VariableAccessExpr(location, ntVal), deps);
                case NondetExpr nondetExpr:
                    return (nondetExpr, deps);
                case NullLiteralExpr nullLiteralExpr:
                    return (nullLiteralExpr, deps);
                case SeqAccessExpr seqAccessExpr:
                    var (seqExpr, seqDeps) = SimplifyExpression(seqAccessExpr.SeqExpr);
                    var (seqIdx, seqIdxDeps) = SimplifyExpression(seqAccessExpr.IndexExpr);
                    var (seqElem, seqElemStore) =
                        SaveInTemporary(new SeqAccessExpr(location, seqExpr, seqIdx, seqAccessExpr.Type));
                    deps.AddRange(seqDeps.Concat(seqIdxDeps));
                    deps.Add(seqElemStore);
                    return (new VariableAccessExpr(location, seqElem), deps);
                case SizeofExpr sizeofExpr:
                    var (sizeExpr, sizeDeps) = SimplifyExpression(sizeofExpr.Expr);
                    var (sizeTemp, sizeStore) = SaveInTemporary(new SizeofExpr(location, sizeExpr));
                    deps.AddRange(sizeDeps);
                    deps.Add(sizeStore);
                    return (new VariableAccessExpr(location, sizeTemp), deps);
                case ThisRefExpr thisRefExpr:
                    return (thisRefExpr, deps);
                case TupleAccessExpr tupleAccessExpr:
                    var (tupItemExpr, tupAccessDeps) = SimplifyExpression(tupleAccessExpr.SubExpr);
                    var (tupItemTemp, tupItemStore) =
                        SaveInTemporary(new TupleAccessExpr(location,
                                                            tupItemExpr,
                                                            tupleAccessExpr.FieldNo,
                                                            tupleAccessExpr.Type));
                    deps.AddRange(tupAccessDeps);
                    deps.Add(tupItemStore);
                    return (new VariableAccessExpr(location, tupItemTemp), deps);
                case UnaryOpExpr unaryOpExpr:
                    var (unExpr, unDeps) = SimplifyExpression(unaryOpExpr.SubExpr);
                    var (unTemp, unStore) = SaveInTemporary(new UnaryOpExpr(location, unaryOpExpr.Operation, unExpr));
                    deps.AddRange(unDeps);
                    deps.Add(unStore);
                    return (new VariableAccessExpr(location, unTemp), deps);
                case UnnamedTupleExpr unnamedTupleExpr:
                    var tupleFields = new IPExpr[unnamedTupleExpr.TupleFields.Length];
                    for (var i = 0; i < unnamedTupleExpr.TupleFields.Length; i++)
                    {
                        var (field, fieldDeps) = SimplifyExpression(unnamedTupleExpr.TupleFields[i]);
                        tupleFields[i] = field;
                        deps.AddRange(fieldDeps);
                    }

                    var (tupVal, tupStore) = SaveInTemporary(new UnnamedTupleExpr(location, tupleFields));
                    deps.Add(tupStore);
                    return (new VariableAccessExpr(location, tupVal), deps);
                case ValuesExpr valuesExpr:
                    var (valuesColl, valuesDeps) = SimplifyExpression(valuesExpr.Expr);
                    var (valuesTemp, valuesStore) =
                        SaveInTemporary(new ValuesExpr(location, valuesColl, valuesExpr.Type));
                    deps.AddRange(valuesDeps);
                    deps.Add(valuesStore);
                    return (new VariableAccessExpr(location, valuesTemp), deps);
                case VariableAccessExpr variableAccessExpr:
                    return (variableAccessExpr, deps);
                default:
                    throw new ArgumentOutOfRangeException(nameof(expr));
            }
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
                    var (annEvt, annEvtDeps) = SimplifyExpression(announceStmt.PEvent);
                    var (annPayload, annPayloadDeps) = announceStmt.Payload == null
                                                           ? (null, new List<IPStmt>())
                                                           : SimplifyExpression(announceStmt.Payload);
                    return annEvtDeps.Concat(annPayloadDeps)
                                     .Concat(new[]
                                     {
                                         new AnnounceStmt(location, annEvt, annPayload)
                                     })
                                     .ToList();
                case AssertStmt assertStmt:
                    var (assertExpr, assertDeps) = SimplifyExpression(assertStmt.Assertion);
                    return assertDeps.Concat(new[]
                                     {
                                         new AssertStmt(location, assertExpr, assertStmt.Message)
                                     })
                                     .ToList();
                case AssignStmt assignStmt:
                    var (assignLV, assignLVDeps) = SimplifyLvalue(assignStmt.Variable);
                    var (assignRV, assignRVDeps) = SimplifyExpression(assignStmt.Value);
                    return assignLVDeps.Concat(assignRVDeps)
                                       .Concat(new[]
                                       {
                                           new AssignStmt(location, assignLV, assignRV)
                                       })
                                       .ToList();
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
                        var (arg, argDeps) = SimplifyExpression(ctorStmtArgument);
                        newCtorArgs.Add(arg);
                        ctorDeps.AddRange(argDeps);
                    }

                    return ctorDeps.Concat(new[]
                                   {
                                       new CtorStmt(location, ctorStmt.Machine, newCtorArgs)
                                   })
                                   .ToList();
                case FunCallStmt funCallStmt:
                    var funDeps = new List<IPStmt>();
                    var newFunArgs = new List<IPExpr>();
                    foreach (IPExpr funStmtArg in funCallStmt.ArgsList)
                    {
                        var (arg, argDeps) = SimplifyExpression(funStmtArg);
                        newFunArgs.Add(arg);
                        funDeps.AddRange(argDeps);
                    }

                    return funDeps.Concat(new[]
                                  {
                                      new FunCallStmt(location, funCallStmt.Fun, newFunArgs)
                                  })
                                  .ToList();
                case GotoStmt gotoStmt:
                    var (gotoPayload, gotoDeps) = SimplifyExpression(gotoStmt.Payload);
                    return gotoDeps.Concat(new[]
                                   {
                                       new GotoStmt(location, gotoStmt.State, gotoPayload)
                                   })
                                   .ToList();
                case IfStmt ifStmt:
                    var (ifCond, ifCondDeps) = SimplifyExpression(ifStmt.Condition);
                    var thenBranch = SimplifyStatement(ifStmt.ThenBranch);
                    var elseBranch = SimplifyStatement(ifStmt.ElseBranch);
                    return ifCondDeps.Concat(new[]
                                     {
                                         new IfStmt(location,
                                                    ifCond,
                                                    new CompoundStmt(ifStmt.ThenBranch.SourceLocation, thenBranch),
                                                    new CompoundStmt(ifStmt.ElseBranch.SourceLocation, elseBranch))
                                     })
                                     .ToList();
                case InsertStmt insertStmt:
                    var (insVar, insVarDeps) = SimplifyLvalue(insertStmt.Variable);
                    var (insIdx, insIdxDeps) = SimplifyExpression(insertStmt.Index);
                    var (insVal, insValDeps) = SimplifyExpression(insertStmt.Value);
                    return insVarDeps.Concat(insIdxDeps)
                                     .Concat(insValDeps)
                                     .Concat(new[]
                                     {
                                         new InsertStmt(location, insVar, insIdx, insVal)
                                     })
                                     .ToList();
                case MoveAssignStmt moveAssignStmt:
                    var (moveDest, moveDestDeps) = SimplifyLvalue(moveAssignStmt.ToLocation);
                    return moveDestDeps.Concat(new[]
                                       {
                                           new MoveAssignStmt(location,
                                                              moveDest,
                                                              moveAssignStmt.FromVariable)
                                       })
                                       .ToList();
                case NoStmt _:
                    return new List<IPStmt>();
                case PopStmt popStmt:
                    return new List<IPStmt> {popStmt};
                case PrintStmt printStmt:
                    var deps = new List<IPStmt>();
                    var newArgs = new List<IPExpr>();
                    foreach (IPExpr printStmtArg in printStmt.Args)
                    {
                        var (arg, argDeps) = SimplifyExpression(printStmtArg);
                        newArgs.Add(arg);
                        deps.AddRange(argDeps);
                    }

                    return deps.Concat(new[]
                               {
                                   new PrintStmt(location, printStmt.Message, newArgs)
                               })
                               .ToList();
                case RaiseStmt raiseStmt:
                    var (raiseEvent, raiseEventDeps) = SimplifyExpression(raiseStmt.PEvent);
                    var raisePayloadArgs = new IPExpr[raiseStmt.Payload.Length];
                    var raisePayloadDeps = new List<IPStmt>();
                    for (var index = 0; index < raiseStmt.Payload.Length; index++)
                    {
                        var (arg, argDeps) = SimplifyExpression(raiseStmt.Payload[index]);
                        raisePayloadArgs[index] = arg;
                        raisePayloadDeps.AddRange(argDeps);
                    }

                    return raiseEventDeps.Concat(raisePayloadDeps)
                                         .Concat(new[]
                                         {
                                             new RaiseStmt(location, raiseEvent, raisePayloadArgs)
                                         })
                                         .ToList();
                case ReceiveStmt receiveStmt:
                    foreach (Function recvCase in receiveStmt.Cases.Values)
                    {
                        recvCase.Body = SimplifyFunctionBody(recvCase.Body);
                    }

                    return new List<IPStmt> {receiveStmt};
                case RemoveStmt removeStmt:
                    var(removeVar, removeVarDeps) = SimplifyLvalue(removeStmt.Variable);
                    var (removeKey, removeKeyDeps) = SimplifyExpression(removeStmt.Value);
                    return removeVarDeps.Concat(removeKeyDeps)
                                        .Concat(new[]
                                        {
                                            new RemoveStmt(location, removeVar, removeKey)
                                        })
                                        .ToList();
                case ReturnStmt returnStmt:
                    if (returnStmt.ReturnValue == null)
                    {
                        return new List<IPStmt> {returnStmt};
                    }

                    var(returnValue, returnValueDeps) = SimplifyExpression(returnStmt.ReturnValue);
                    return returnValueDeps.Concat(new[]
                                          {
                                              new ReturnStmt(location, returnValue)
                                          })
                                          .ToList();
                case SendStmt sendStmt:
                    var (sendMachine, sendMachineDeps) = SimplifyExpression(sendStmt.MachineExpr);
                    var (sendEvent, sendEventDeps) = SimplifyExpression(sendStmt.Evt);
                    var sendArgs = new List<IPExpr>();
                    var sendArgDeps = new List<IPStmt>();
                    foreach (IPExpr pExpr in sendStmt.ArgsList)
                    {
                        var (arg, argDeps) = SimplifyExpression(pExpr);
                        sendArgs.Add(arg);
                        sendArgDeps.AddRange(argDeps);
                    }

                    return sendMachineDeps.Concat(sendEventDeps)
                                          .Concat(sendArgDeps)
                                          .Concat(new[]
                                          {
                                              new SendStmt(location, sendMachine, sendEvent, sendArgs)
                                          })
                                          .ToList();
                case SwapAssignStmt swapAssignStmt:
                    var (swapVar, swapVarDeps) = SimplifyLvalue(swapAssignStmt.NewLocation);
                    return swapVarDeps.Concat(new[]
                                      {
                                          new SwapAssignStmt(location, swapVar, swapAssignStmt.OldLocation)
                                      })
                                      .ToList();
                case WhileStmt whileStmt:
                    var (condExpr, condDeps) = SimplifyExpression(whileStmt.Condition);
                    var (condTemp, condStore) = SaveInTemporary(condExpr);
                    var whileBody = SimplifyStatement(whileStmt.Body);
                    whileBody.AddRange(condDeps);
                    whileBody.Add(condStore);
                    return condDeps.Concat(new[]
                                   {
                                       condStore,
                                       new WhileStmt(location,
                                                     new VariableAccessExpr(condExpr.SourceLocation, condTemp),
                                                     new CompoundStmt(whileStmt.Body.SourceLocation, whileBody))
                                   })
                                   .ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }
        }
    }
}
