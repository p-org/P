using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.Backend.ASTExt;
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

        private (VariableAccessExpr, IPStmt) SaveInTemporary(IPExpr expr)
        {
            return SaveInTemporary(expr, expr.Type);
        }

        private (VariableAccessExpr, IPStmt) SaveInTemporary(IPExpr expr, PLanguageType tempType)
        {
            ParserRuleContext location = expr.SourceLocation;
            var temp = function.Scope.Put($"$tmp{numTemp++}", location, VariableRole.Local | VariableRole.Temp);
            Debug.Assert(tempType.IsAssignableFrom(expr.Type));
            temp.Type = tempType;
            function.AddLocalVariable(temp);
            var stmt = new AssignStmt(location, new VariableAccessExpr(location, temp), expr);
            return (new VariableAccessExpr(location, temp), stmt);
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
                    return (binOpTemp, deps);
                case BoolLiteralExpr boolLiteralExpr:
                    return (boolLiteralExpr, deps);
                case CastExpr castExpr:
                    var (castSubExpr, castDeps) = SimplifyExpression(castExpr.SubExpr);
                    var (castTemp, castStore) = SaveInTemporary(new CastExpr(location, castSubExpr, castExpr.Type));
                    deps.AddRange(castDeps);
                    deps.Add(castStore);
                    return (castTemp, deps);
                case CoerceExpr coerceExpr:
                    var (coerceSubExpr, coerceDeps) = SimplifyExpression(coerceExpr.SubExpr);
                    var (coerceTemp, coerceStore) =
                        SaveInTemporary(new CoerceExpr(location, coerceSubExpr, coerceExpr.NewType));
                    deps.AddRange(coerceDeps);
                    deps.Add(coerceStore);
                    return (coerceTemp, deps);
                case ContainsKeyExpr containsKeyExpr:
                    var (contKeyExpr, contKeyDeps) = SimplifyExpression(containsKeyExpr.Key);
                    var (contMapExpr, contMapDeps) = SimplifyExpression(containsKeyExpr.Map);
                    var (contTemp, contStore) =
                        SaveInTemporary(new ContainsKeyExpr(location, contKeyExpr, contMapExpr));
                    deps.AddRange(contKeyDeps.Concat(contMapDeps));
                    deps.Add(contStore);
                    return (contTemp, deps);
                case CtorExpr ctorExpr:
                    var ctorArgs = new IPExpr[ctorExpr.Arguments.Length];
                    for (var i = 0; i < ctorExpr.Arguments.Length; i++)
                    {
                        var (argExpr, argDeps) = SimplifyExpression(ctorExpr.Arguments[i]);
                        ctorArgs[i] = argExpr;
                        deps.AddRange(argDeps);
                    }

                    var (ctorTemp, ctorStore) = SaveInTemporary(new CtorExpr(location, ctorExpr.Interface, ctorArgs));
                    deps.Add(ctorStore);
                    return (ctorTemp, deps);
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
                        deps.AddRange(argDeps);
                        if (!(argExpr is VariableAccessExpr) && !(argExpr is LinearAccessRefExpr))
                        {
                            var (argExpr2, newDep) = SaveInTemporary(argExpr);
                            deps.Add(newDep);
                            argExpr = argExpr2;
                        }
                        funArgs[i] = argExpr;
                    }

                    var (funTemp, funStore) = SaveInTemporary(new FunCallExpr(location, funCallExpr.Function, funArgs));
                    deps.Add(funStore);
                    return (funTemp, deps);
                case IntLiteralExpr intLiteralExpr:
                    return (intLiteralExpr, deps);
                case KeysExpr keysExpr:
                    var (keysColl, keysDeps) = SimplifyExpression(keysExpr.Expr);
                    var (keysTemp, keysStore) = SaveInTemporary(new KeysExpr(location, keysColl, keysExpr.Type));
                    deps.AddRange(keysDeps);
                    deps.Add(keysStore);
                    return (keysTemp, deps);
                case LinearAccessRefExpr linearAccessRefExpr:
                    return (linearAccessRefExpr, deps);
                case MapAccessExpr mapAccessExpr:
                    var (mapExpr, mapDeps) = SimplifyExpression(mapAccessExpr.MapExpr);
                    var (mapIdxExpr, mapIdxDeps) = SimplifyExpression(mapAccessExpr.IndexExpr);
                    var (mapItemTemp, mapItemStore) =
                        SaveInTemporary(new MapAccessExpr(location, mapExpr, mapIdxExpr, mapAccessExpr.Type));
                    deps.AddRange(mapDeps.Concat(mapIdxDeps));
                    deps.Add(mapItemStore);
                    return (mapItemTemp, deps);
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    var (ntSubExpr, ntSubDeps) = SimplifyExpression(namedTupleAccessExpr.SubExpr);
                    var (ntTemp, ntStore) =
                        SaveInTemporary(new NamedTupleAccessExpr(location, ntSubExpr, namedTupleAccessExpr.Entry));
                    deps.AddRange(ntSubDeps);
                    deps.Add(ntStore);
                    return (ntTemp, deps);
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
                    return (ntVal, deps);
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
                    return (seqElem, deps);
                case SizeofExpr sizeofExpr:
                    var (sizeExpr, sizeDeps) = SimplifyExpression(sizeofExpr.Expr);
                    var (sizeTemp, sizeStore) = SaveInTemporary(new SizeofExpr(location, sizeExpr));
                    deps.AddRange(sizeDeps);
                    deps.Add(sizeStore);
                    return (sizeTemp, deps);
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
                    return (tupItemTemp, deps);
                case UnaryOpExpr unaryOpExpr:
                    var (unExpr, unDeps) = SimplifyExpression(unaryOpExpr.SubExpr);
                    var (unTemp, unStore) = SaveInTemporary(new UnaryOpExpr(location, unaryOpExpr.Operation, unExpr));
                    deps.AddRange(unDeps);
                    deps.Add(unStore);
                    return (unTemp, deps);
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
                    return (tupVal, deps);
                case ValuesExpr valuesExpr:
                    var (valuesColl, valuesDeps) = SimplifyExpression(valuesExpr.Expr);
                    var (valuesTemp, valuesStore) =
                        SaveInTemporary(new ValuesExpr(location, valuesColl, valuesExpr.Type));
                    deps.AddRange(valuesDeps);
                    deps.Add(valuesStore);
                    return (valuesTemp, deps);
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

        private static CloneExpr MakeClone(IPExpr expr)
        {
            if (expr is CloneExpr cloned)
            {
                return cloned;
            }
            return new CloneExpr(expr);
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
                                           new AssignStmt(location, assignLV, MakeClone(assignRV))
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
                                       new CtorStmt(location, ctorStmt.Interface, newCtorArgs)
                                   })
                                   .ToList();
                case FunCallStmt funCallStmt:
                    var funDeps = new List<IPStmt>();
                    var newFunArgs = new List<IPExpr>();
                    foreach (IPExpr funStmtArg in funCallStmt.ArgsList)
                    {
                        var (arg, argDeps) = SimplifyExpression(funStmtArg);
                        funDeps.AddRange(argDeps);
                        if (!(arg is VariableAccessExpr) && !(arg is LinearAccessRefExpr))
                        {
                            var (arg2, argDep) = SaveInTemporary(arg);
                            funDeps.Add(argDep);
                            arg = arg2;
                        }
                        newFunArgs.Add(arg);
                    }

                    return funDeps.Concat(new[]
                                  {
                                      new FunCallStmt(location, funCallStmt.Fun, newFunArgs)
                                  })
                                  .ToList();
                case GotoStmt gotoStmt:
                    var (gotoPayload, gotoDeps) = SimplifyExpression(gotoStmt.Payload);
                    var (gotoArgTmp, gotoArgDep) = SaveInTemporary(gotoPayload);
                    return gotoDeps.Concat(new[]
                                   {
                                       gotoArgDep,
                                       new GotoStmt(location, gotoStmt.State, gotoArgTmp)
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
                                           new AssignStmt(location,
                                                              moveDest,
                                                              new VariableAccessExpr(moveAssignStmt.SourceLocation,
                                                                                     moveAssignStmt.FromVariable))
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
                    var (raiseEventTmp, raiseEventTempDep) = SaveInTemporary(MakeClone(raiseEvent));

                    var (raiseArgs, raiseArgDeps) = SimplifyArgPack(raiseStmt.Payload);

                    return raiseEventDeps.Concat(raiseEventDeps)
                                         .Concat(raiseArgDeps)
                                         .Concat(new [] {raiseEventTempDep})
                                         .Concat(new[]
                                         {
                                             new RaiseStmt(location, raiseEventTmp, raiseArgs.Cast<IPExpr>().ToArray())
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

                    var (returnValue, returnValueDeps) = SimplifyExpression(returnStmt.ReturnValue);
                    return returnValueDeps.Concat(new[]
                                          {
                                              new ReturnStmt(location, MakeClone(returnValue))
                                          })
                                          .ToList();
                case SendStmt sendStmt:
                    var (sendMachine, sendMachineDeps) = SimplifyExpression(sendStmt.MachineExpr);
                    var (sendMachineAccessExpr, sendMachineAssn) = SaveInTemporary(sendMachine);

                    var (sendEvent, sendEventDeps) = SimplifyExpression(sendStmt.Evt);
                    var (sendEventAccessExpr, sendEventAssn) = SaveInTemporary(MakeClone(sendEvent));

                    var (sendArgs, sendArgDeps) = SimplifyArgPack(sendStmt.ArgsList);

                    return sendMachineDeps
                           .Concat(new[] {sendMachineAssn})
                           .Concat(sendEventDeps)
                           .Concat(new[] {sendEventAssn})
                           .Concat(sendArgDeps)
                           .Concat(new[]
                           {
                               new SendStmt(location, sendMachineAccessExpr, sendEventAccessExpr, sendArgs)
                           })
                           .ToList();
                case SwapAssignStmt swapAssignStmt:
                    var (swapVar, swapVarDeps) = SimplifyLvalue(swapAssignStmt.NewLocation);
                    var (swapTmp, tmpAssn) = SaveInTemporary(swapVar, PrimitiveType.Any);
                    Variable rhs = swapAssignStmt.OldLocation;
                    return swapVarDeps.Concat(new[]
                                      {
                                          tmpAssn,
                                          new AssignStmt(location,
                                                         swapVar,
                                                         MakeCast(new VariableAccessExpr(location, rhs), swapVar.Type)),
                                          new AssignStmt(location,
                                                         new VariableAccessExpr(location, rhs),
                                                         MakeCast(swapTmp, rhs.Type))
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
                                                     condTemp,
                                                     new CompoundStmt(whileStmt.Body.SourceLocation, whileBody))
                                   })
                                   .ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }
        }

        private (List<VariableAccessExpr> args, List<IPStmt> deps) SimplifyArgPack(IEnumerable<IPExpr> argsPack)
        {
            var argumentVars = new List<VariableAccessExpr>();
            var argumentDeps = new List<IPStmt>();
            foreach (IPExpr pExpr in argsPack)
            {
                switch (pExpr)
                {
                    case LinearAccessRefExpr moveExpr:
                        Debug.Assert(moveExpr.LinearType == LinearType.Move);
                        argumentVars.Add(new VariableAccessExpr(moveExpr.SourceLocation, moveExpr.Variable));
                        break;
                    case VariableAccessExpr readExpr:
                        var (arg, clonedDep) = SaveInTemporary(MakeClone(readExpr));
                        argumentDeps.Add(clonedDep);
                        argumentVars.Add(arg);
                        break;
                    default:
                        var (simpleArg, argDeps) = SimplifyExpression(pExpr);
                        var (cloned, clonedDep2) = SaveInTemporary(MakeClone(simpleArg));
                        argumentDeps.AddRange(argDeps);
                        argumentDeps.Add(clonedDep2);
                        argumentVars.Add(cloned);
                        break;
                }
            }
            return (argumentVars, argumentDeps);
        }

        private IPExpr MakeCast(IPExpr expr, PLanguageType newType)
        {
            return newType.IsAssignableFrom(expr.Type) ? expr : new CastExpr(expr.SourceLocation, expr, newType);
        }
    }
}
