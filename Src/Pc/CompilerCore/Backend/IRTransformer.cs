using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend
{
    public class IRTransformer
    {
        private readonly Function function;
        private int numTemp;

        private IRTransformer(Function function)
        {
            this.function = function;
        }

        public static void SimplifyMethod(Function function)
        {
            if (function.IsForeign) return;

            var transformer = new IRTransformer(function);
            IPStmt functionBody = function.Body;
            function.Body = new CompoundStmt(functionBody.SourceLocation, transformer.SimplifyStatement(functionBody));
        }

        private (VariableAccessExpr, IPStmt) SaveInTemporary(IPExpr expr)
        {
            return SaveInTemporary(expr, expr.Type);
        }

        private (VariableAccessExpr, IPStmt) SaveInTemporary(IPExpr expr, PLanguageType tempType)
        {
            var location = expr.SourceLocation;
            var temp = function.Scope.Put($"$tmp{numTemp++}", location, VariableRole.Local | VariableRole.Temp);
            Debug.Assert(tempType.IsAssignableFrom(expr.Type));
            temp.Type = tempType;
            function.AddLocalVariable(temp);
            var stmt = new AssignStmt(location, new VariableAccessExpr(location, temp), expr);
            return (new VariableAccessExpr(location, temp), stmt);
        }

        private (IPExpr, List<IPStmt>) SimplifyLvalue(IPExpr expr)
        {
            // TODO: I am suspicious.
            var location = expr.SourceLocation;
            var type = expr.Type;
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

        private (IExprTerm, List<IPStmt>) SimplifyExpression(IPExpr expr)
        {
            var location = expr.SourceLocation;
            var deps = new List<IPStmt>();
            switch (expr)
            {
                case IExprTerm term:
                    return (term, deps);
                case BinOpExpr binOpExpr:
                    var (lhsTemp, lhsDeps) = SimplifyExpression(binOpExpr.Lhs);
                    var (rhsTemp, rhsDeps) = SimplifyExpression(binOpExpr.Rhs);
                    var (binOpTemp, binOpStore) =
                        SaveInTemporary(new BinOpExpr(location, binOpExpr.Operation, lhsTemp, rhsTemp));
                    deps.AddRange(lhsDeps.Concat(rhsDeps));
                    deps.Add(binOpStore);
                    return (binOpTemp, deps);
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
                case ContainsExpr containsKeyExpr:
                    var (contKeyExpr, contKeyDeps) = SimplifyExpression(containsKeyExpr.Item);
                    var (contMapExpr, contMapDeps) = SimplifyExpression(containsKeyExpr.Collection);
                    var (contTemp, contStore) =
                        SaveInTemporary(new ContainsExpr(location, contKeyExpr, contMapExpr));
                    deps.AddRange(contKeyDeps.Concat(contMapDeps));
                    deps.Add(contStore);
                    return (contTemp, deps);
                case CtorExpr ctorExpr:
                    var (ctorArgs, ctorArgDeps) = SimplifyArgPack(ctorExpr.Arguments);
                    deps.AddRange(ctorArgDeps);
                    var (ctorTemp, ctorStore) = SaveInTemporary(new CtorExpr(location, ctorExpr.Interface, ctorArgs));
                    deps.Add(ctorStore);
                    return (ctorTemp, deps);
                case DefaultExpr defaultExpr:
                    var (defTemp, defStore) = SaveInTemporary(defaultExpr);
                    deps.Add(defStore);
                    return (defTemp, deps);
                case FairNondetExpr fairNondetExpr:
                    var (fndTemp, fndStore) = SaveInTemporary(fairNondetExpr);
                    deps.Add(fndStore);
                    return (fndTemp, deps);
                case FunCallExpr funCallExpr:
                    var (funArgs, funArgsDeps) = SimplifyFunArgs(funCallExpr.Arguments);
                    deps.AddRange(funArgsDeps);
                    var (funTemp, funStore) = SaveInTemporary(new FunCallExpr(location, funCallExpr.Function, funArgs));
                    deps.Add(funStore);
                    return (funTemp, deps);
                case KeysExpr keysExpr:
                    var (keysColl, keysDeps) = SimplifyExpression(keysExpr.Expr);
                    var (keysTemp, keysStore) = SaveInTemporary(new KeysExpr(location, keysColl, keysExpr.Type));
                    deps.AddRange(keysDeps);
                    deps.Add(keysStore);
                    return (keysTemp, deps);
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
                    var (args, argDeps) = SimplifyArgPack(namedTupleExpr.TupleFields);
                    deps.AddRange(argDeps);

                    var (ntVal, ntValStore) =
                        SaveInTemporary(new NamedTupleExpr(location, args, namedTupleExpr.Type));
                    deps.Add(ntValStore);
                    return (ntVal, deps);
                case NondetExpr nondetExpr:
                    var (ndTemp, ndStore) = SaveInTemporary(nondetExpr);
                    deps.Add(ndStore);
                    return (ndTemp, deps);
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
                    var (tupFields, tupFieldDeps) = SimplifyArgPack(unnamedTupleExpr.TupleFields);
                    deps.AddRange(tupFieldDeps);
                    var (tupVal, tupStore) = SaveInTemporary(new UnnamedTupleExpr(location, tupFields));
                    deps.Add(tupStore);
                    return (tupVal, deps);
                case ValuesExpr valuesExpr:
                    var (valuesColl, valuesDeps) = SimplifyExpression(valuesExpr.Expr);
                    var (valuesTemp, valuesStore) =
                        SaveInTemporary(new ValuesExpr(location, valuesColl, valuesExpr.Type));
                    deps.AddRange(valuesDeps);
                    deps.Add(valuesStore);
                    return (valuesTemp, deps);
                default:
                    throw new ArgumentOutOfRangeException(nameof(expr));
            }
        }

        private List<IPStmt> SimplifyStatement(IPStmt statement)
        {
            var location = statement?.SourceLocation;
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
                    var (assignLV, assignLVDeps) = SimplifyLvalue(assignStmt.Location);
                    var (assignRV, assignRVDeps) = SimplifyExpression(assignStmt.Value);
                    IPStmt assignment;
                    // If temporary returned, then automatically move.
                    if (assignRV is VariableAccessExpr variableRef &&
                        variableRef.Variable.Role.HasFlag(VariableRole.Temp))
                        assignment = new MoveAssignStmt(location, assignLV, variableRef.Variable);
                    else
                        assignment = new AssignStmt(location, assignLV, new CloneExpr(assignRV));
                    return assignLVDeps.Concat(assignRVDeps).Concat(new[] {assignment}).ToList();
                case CompoundStmt compoundStmt:
                    var newBlock = new List<IPStmt>();
                    foreach (var step in compoundStmt.Statements) newBlock.AddRange(SimplifyStatement(step));
                    // TODO: why not return the list? because of source location info?
                    return new List<IPStmt> {new CompoundStmt(location, newBlock)};
                case CtorStmt ctorStmt:
                    var (ctorArgs, ctorArgDeps) = SimplifyArgPack(ctorStmt.Arguments);
                    return ctorArgDeps.Concat(new[]
                        {
                            new CtorStmt(location, ctorStmt.Interface, ctorArgs)
                        })
                        .ToList();
                case FunCallStmt funCallStmt:
                    var (funCallArgs, funCallArgDeps) = SimplifyFunArgs(funCallStmt.ArgsList);
                    return funCallArgDeps.Concat(new[]
                        {
                            new FunCallStmt(location, funCallStmt.Function, funCallArgs)
                        })
                        .ToList();
                case GotoStmt gotoStmt:
                    if (gotoStmt.Payload == null) return new List<IPStmt> {gotoStmt};

                    var (gotoPayload, gotoDeps) = SimplifyExpression(gotoStmt.Payload);
                    var (gotoArgTmp, gotoArgDep) = SaveInTemporary(new CloneExpr(gotoPayload));
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
                    var (insVal, insValDeps) = SimplifyArgPack(new[] {insertStmt.Value});
                    Debug.Assert(insVal.Count == 1);
                    return insVarDeps.Concat(insIdxDeps)
                        .Concat(insValDeps)
                        .Concat(new[]
                        {
                            new InsertStmt(location, insVar, insIdx, insVal[0])
                        })
                        .ToList();
                case MoveAssignStmt moveAssignStmt:
                    var (moveDest, moveDestDeps) = SimplifyLvalue(moveAssignStmt.ToLocation);
                    return moveDestDeps.Concat(new[]
                        {
                            new MoveAssignStmt(moveAssignStmt.SourceLocation, moveDest, moveAssignStmt.FromVariable)
                        })
                        .ToList();
                case NoStmt _:
                    return new List<IPStmt>();
                case PopStmt popStmt:
                    return new List<IPStmt> {popStmt};
                case PrintStmt printStmt:
                    var deps = new List<IPStmt>();
                    var newArgs = new List<IPExpr>();
                    foreach (var printStmtArg in printStmt.Args)
                    {
                        var (arg, argDeps) = SimplifyExpression(printStmtArg);
                        newArgs.Add(arg);
                        deps.AddRange(argDeps);
                    }

                    return deps.Concat(new[] {new PrintStmt(location, printStmt.Message, newArgs)}).ToList();
                case RaiseStmt raiseStmt:
                    var (raiseEvent, raiseEventDeps) = SimplifyExpression(raiseStmt.PEvent);
                    var (raiseEventTmp, raiseEventTempDep) = SaveInTemporary(new CloneExpr(raiseEvent));

                    var (raiseArgs, raiseArgDeps) = SimplifyArgPack(raiseStmt.Payload);

                    return raiseEventDeps.Concat(raiseEventDeps)
                        .Concat(raiseArgDeps)
                        .Concat(new[] {raiseEventTempDep})
                        .Concat(new[]
                        {
                            new RaiseStmt(location, raiseEventTmp, raiseArgs)
                        })
                        .ToList();
                case ReceiveStmt receiveStmt:
                    foreach (var recvCase in receiveStmt.Cases.Values)
                    {
                        IPStmt functionBody = recvCase.Body;
                        recvCase.Body = new CompoundStmt(functionBody.SourceLocation, SimplifyStatement(functionBody));
                    }

                    return new List<IPStmt> {receiveStmt};
                case RemoveStmt removeStmt:
                    var (removeVar, removeVarDeps) = SimplifyLvalue(removeStmt.Variable);
                    var (removeKey, removeKeyDeps) = SimplifyExpression(removeStmt.Value);
                    return removeVarDeps.Concat(removeKeyDeps)
                        .Concat(new[]
                        {
                            new RemoveStmt(location, removeVar, removeKey)
                        })
                        .ToList();
                case ReturnStmt returnStmt:
                    if (returnStmt.ReturnValue == null) return new List<IPStmt> {returnStmt};

                    var (returnValue, returnValueDeps) = SimplifyExpression(returnStmt.ReturnValue);
                    return returnValueDeps.Concat(new[]
                        {
                            new ReturnStmt(location, new CloneExpr(returnValue))
                        })
                        .ToList();
                case BreakStmt breakStmt:
                    return new List<IPStmt> {breakStmt};
                case ContinueStmt continueStmt:
                    return new List<IPStmt> {continueStmt};
                case SendStmt sendStmt:
                    var (sendMachine, sendMachineDeps) = SimplifyExpression(sendStmt.MachineExpr);
                    var (sendMachineAccessExpr, sendMachineAssn) = SaveInTemporary(new CloneExpr(sendMachine));

                    var (sendEvent, sendEventDeps) = SimplifyExpression(sendStmt.Evt);
                    var (sendEventAccessExpr, sendEventAssn) = SaveInTemporary(new CloneExpr(sendEvent));

                    var (sendArgs, sendArgDeps) = SimplifyArgPack(sendStmt.Arguments);

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
                    var rhs = swapAssignStmt.OldLocation;
                    return swapVarDeps.Concat(new[]
                        {
                            new SwapAssignStmt(swapAssignStmt.SourceLocation, swapVar, rhs)
                        })
                        .ToList();
                case WhileStmt whileStmt:
                    var (condExpr, condDeps) = SimplifyExpression(whileStmt.Condition);
                    var (condTemp, condStore) = SaveInTemporary(new CloneExpr(condExpr));
                    var condLocation = whileStmt.Condition.SourceLocation;
                    var condCheck =
                        condDeps
                        .Append(condStore)
                        .Append(new IfStmt(condLocation, condTemp, new NoStmt(condLocation), new BreakStmt(condLocation)))
                        .ToList();

                    var loopBody = new CompoundStmt(
                        whileStmt.Body.SourceLocation,
                        condCheck.Concat(SimplifyStatement(whileStmt.Body)));

                    return new List<IPStmt> { new WhileStmt(location, new BoolLiteralExpr(location, true), loopBody) };
                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }
        }

        private (IReadOnlyList<IVariableRef> args, List<IPStmt> deps) SimplifyArgPack(IEnumerable<IPExpr> argsPack)
        {
            var args = argsPack.ToList();
            Debug.Assert(!args.Any(arg => arg is LinearAccessRefExpr lin && lin.LinearType.Equals(LinearType.Swap)));
            var (refArgs, deps) = SimplifyFunArgs(args);
            return (refArgs, deps);
        }

        private (ILinearRef[], List<IPStmt>) SimplifyFunArgs(IReadOnlyList<IPExpr> funCallArgs)
        {
            var funArgs = new ILinearRef[funCallArgs.Count];
            var deps = new List<IPStmt>();
            for (var i = 0; i < funCallArgs.Count; i++)
            {
                var (argExpr, argDeps) = SimplifyExpression(funCallArgs[i]);
                deps.AddRange(argDeps);
                // My thoughts: if SimplifyExpression returns a temporary, there's no way that came from user code, thus
                // it must be either an intermediate expression, or an explicit clone of another value. In either case, the
                // memory there is invisible to the rest of the program and so it can be moved safely. The first case below
                // is therefore an optimization that assumes this holds. If things go wrong, comment it out and take the extra
                // allocations as a hit.
                switch (argExpr)
                {
                    // Move temporaries...
                    case VariableAccessExpr variableAccessExpr
                        when variableAccessExpr.Variable.Role.HasFlag(VariableRole.Temp):
                        funArgs[i] = new LinearAccessRefExpr(variableAccessExpr.SourceLocation,
                            variableAccessExpr.Variable, LinearType.Move);
                        break;
                    // ...and leave linear accesses alone...
                    case LinearAccessRefExpr linearAccessRefExpr:
                        funArgs[i] = linearAccessRefExpr;
                        break;
                    // ...but clone literals and visible variables/fields.
                    default:
                        var (temp, tempDep) = SaveInTemporary(new CloneExpr(argExpr));
                        deps.Add(tempDep);
                        funArgs[i] = new LinearAccessRefExpr(temp.SourceLocation, temp.Variable, LinearType.Move);
                        break;
                }
            }

            return (funArgs, deps);
        }
    }
}
