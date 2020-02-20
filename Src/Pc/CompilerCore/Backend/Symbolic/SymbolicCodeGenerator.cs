using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Symbolic
{
    class SymbolicCodeGenerator : ICodeGenerator
    {
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            var javaSource = GenerateSource(context, globalScope);
            return new List<CompiledFile> { javaSource };
        }

        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);

            WriteSourcePrologue(context, source.Stream);

            WriteEventDefs(context, source.Stream, globalScope.Events);

            context.WriteLine(source.Stream);

            foreach (var decl in globalScope.AllDecls)
                WriteDecl(context, source.Stream, decl);

            WriteSourceEpilogue(context, source.Stream);

            return source;
        }

        private void WriteEventDefs(CompilationContext context, StringWriter output, IEnumerable<PEvent> events)
        {
            context.Write(output, "enum EventTag { ");
            context.WriteCommaSeparated(output, events, (pEvent) =>
            {
                context.Write(output, context.GetNameForDecl(pEvent));
            });
            context.WriteLine(output, " }");

            context.Write(output, "final static EventVS.Ops<EventTag> eventOps = new EventVS.Ops<EventTag>(");
            context.WriteCommaSeparated(output, events, (pEvent) =>
            {
                context.Write(output, $"EventTag.{context.GetNameForDecl(pEvent)}, ");
                var payloadType = pEvent.PayloadType;
                if (payloadType.IsSameTypeAs(PrimitiveType.Null))
                {
                    context.Write(output, "null");
                } else
                {
                    var payloadOps = GetValueSummaryOps(context, payloadType);
                    context.Write(output, payloadOps.GetName());
                }
            });
            context.Write(output, ");");
        }

        private void WriteDecl(CompilationContext context, StringWriter output, IPDecl decl)
        {
            switch (decl)
            {
                case Function function:
                    if (function.IsForeign)
                        throw new NotImplementedException("Foreign functions are not yet supported");
                    
                    WriteFunction(context, output, function);
                    break;
                case Machine machine:
                    if (machine.IsSpec)
                        context.WriteLine(output, $"// Skipping monitor {machine.Name}");
                    else
                        WriteMachine(context, output, machine);
                    break;
                default:
                    context.WriteLine(output, $"// Skipping {decl.GetType().Name} '{decl.Name}'\n");
                    break;
            }
        }

        private void WriteMachine(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.GetNameForDecl(machine);
            context.WriteLine(output, $"private static class {declName} {{");

            context.Write(output, "private enum State { ");
            context.WriteCommaSeparated(output, machine.States, (state) =>
            {
                context.Write(output, context.GetNameForDecl(state));
            });
            context.WriteLine(output, " }");
            context.WriteLine(output, "private static final PrimVS.Ops<State> stateOps = new PrimVS.Ops<State>();");

            context.WriteLine(output);

            context.WriteLine(output, $"private PrimVS<State> state;");

            foreach (var field in machine.Fields)
                context.WriteLine(output, $"private {GetSymbolicType(field.Type)} {CompilationContext.GetVar(field.Name)};");

            context.WriteLine(output);

            // In the future, we will return the side effects of initialization, so we can't use a constructor for initialization.
            // TODO: Take an event payload as input.
            var initPcScope = context.FreshPathConstraintScope();
            context.WriteLine(output, $"void initialize(Bdd {initPcScope.PathConstraintVar}) {{");
            foreach (var field in machine.Fields)
            {
                context.WriteLine(output, $"this.{CompilationContext.GetVar(field.Name)} = {GetDefaultValue(context, initPcScope, field.Type)};");
            }
            context.WriteLine(output, $"this.state = stateOps.guard(new PrimVS(State.{context.GetNameForDecl(machine.StartState)}), {initPcScope.PathConstraintVar});");
            // TODO: Call entry function for start state here
            context.WriteLine(output, "}");

            context.WriteLine(output);

            WriteEventDispatcher(context, output, machine);

            context.WriteLine(output);

            foreach (var method in machine.Methods)
                WriteFunction(context, output, method);

            context.WriteLine(output, "}");
        }

        private void WriteEventDispatcher(CompilationContext context, StringWriter output, Machine machine)
        {
            PathConstraintScope rootPcScope = context.FreshPathConstraintScope();
            // TODO: Support payload!
            context.WriteLine(output, $"InternalOutcome<EventVS<EventTag>, State> processEvent(Bdd {rootPcScope.PathConstraintVar}, EventVS<EventTag> event) {{");
            context.WriteLine(output, $"InternalOutcome<EventVS<EventTag>, State> outcome = InternalOutcome<EventVS<EventTag>, State>.empty();");
            context.WriteLine(output);
            foreach (var state in machine.States)
            {
                var statePcScope = context.FreshPathConstraintScope();
                var stateName = context.GetNameForDecl(state);
                context.WriteLine(output, $"Bdd {statePcScope.PathConstraintVar} = this.state.guardedValues.get(State.{stateName});");
                context.WriteLine(output, $"if ({statePcScope.PathConstraintVar} != null && !{statePcScope.PathConstraintVar}.isConstFalse()) {{");
                context.WriteLine(output, $"Bdd hasHandler = Bdd.constFalse();");
                context.WriteLine(output, $"EventVS<EventTag> guardedEvent = eventOps.guard(event, {statePcScope.PathConstraintVar});");
                context.WriteLine(output);
                foreach (var handler in state.AllEventHandlers)
                {
                    var handlerPcScope = context.FreshPathConstraintScope();
                    var eventName = context.GetNameForDecl(handler.Key);
                    context.WriteLine(output, $"Bdd {handlerPcScope.PathConstraintVar} = guardedEvent.getCondForTag(EventTag.{eventName});");
                    context.WriteLine(output, $"if ({handlerPcScope.PathConstraintVar} != null && !{handlerPcScope.PathConstraintVar}.isConstFalse()) {{");
                    context.WriteLine(output, $"hasHandler = hasHandler.or({handlerPcScope.PathConstraintVar});");
                    switch (handler.Value)
                    {
                        case EventDefer defer:
                            context.WriteLine(output, "/* TODO: Defer event */");
                            break;
                        case EventDoAction action:
                            context.WriteLine(output, "/* TODO: Do action */");
                            break;
                        case EventGotoState gotoState:
                            if (gotoState.TransitionFunction != null)
                            {
                                var transitionFunc = gotoState.TransitionFunction;
                                Debug.Assert(!(transitionFunc.CanChangeState ?? false));
                                Debug.Assert(!(transitionFunc.CanRaiseEvent ?? false));
                                if (transitionFunc.Signature.Parameters.Count() == 1)
                                {
                                    Debug.Assert(!handler.Key.PayloadType.IsSameTypeAs(PrimitiveType.Null));
                                    context.WriteLine(output, $"this.{context.GetNameForDecl(transitionFunc)}({handlerPcScope.PathConstraintVar}, guardedEvent.getPayload(EventTag.{eventName}));");
                                }
                                else
                                {
                                    Debug.Assert(transitionFunc.Signature.Parameters.Count() == 0);
                                    context.WriteLine(output, $"this.{context.GetNameForDecl(transitionFunc)}({handlerPcScope.PathConstraintVar});");
                                }
                            }
                            context.WriteLine(output, $"outcome.addGoto({handlerPcScope.PathConstraintVar}, State.{context.GetNameForDecl(gotoState.Target)});");
                            break;
                        case EventIgnore ignore:
                            context.WriteLine(output, "// Ignore");
                            break;
                        case EventPushState pushState:
                            context.WriteLine(output, "/* TODO: Push state */");
                            break;
                        default:
                            throw new NotImplementedException($"Unrecognized handler type {handler.Value.GetType().Name}");
                    }
                    context.WriteLine(output, "}");
                }
                context.WriteLine(output);
                context.WriteLine(output, $"if (!{statePcScope.PathConstraintVar}.and(hasHandler.not()).isConstFalse()) {{");
                context.WriteLine(output, $"throw new BugFoundException({statePcScope.PathConstraintVar});");
                context.WriteLine(output, "}");
                context.WriteLine(output, "}");
                context.WriteLine(output);
            }
            context.WriteLine(output, "return outcome;");
            context.WriteLine(output, "}");
        }

        internal struct ControlFlowContext
        {
            internal readonly PathConstraintScope pcScope;
            internal readonly LoopScope? loopScope;
            internal readonly BranchScope? branchScope;

            public ControlFlowContext(PathConstraintScope pcScope, LoopScope? loopScope, BranchScope? branchScope)
            {
                this.pcScope = pcScope;
                this.loopScope = loopScope;
                this.branchScope = branchScope;
            }

            internal static ControlFlowContext FreshFuncContext(CompilationContext context, PathConstraintScope pathConstraintScope)
            {
                return new ControlFlowContext(pathConstraintScope, null, null);
            }

            internal static ControlFlowContext FreshLoopContext(CompilationContext context)
            {
                return new ControlFlowContext(context.FreshPathConstraintScope(), context.FreshLoopScope(), null);
            }

            internal ControlFlowContext FreshBranchSubContext(CompilationContext context)
            {
                return new ControlFlowContext(context.FreshPathConstraintScope(), loopScope, context.FreshBranchScope());
            }
        }

        private void WriteFunction(CompilationContext context, StringWriter output, Function function)
        {
            var isStatic = function.Owner == null;

            if (function.CanReceive == true)
                throw new NotImplementedException("Async functions are not supported");

            var staticKeyword = isStatic ? "static " : "";

            var rootPCScope = context.FreshPathConstraintScope();

            var returnType = GetSymbolicType(function.Signature.ReturnType);
            var functionName = context.GetNameForDecl(function);

            context.WriteLine(output, $"{staticKeyword}{returnType} ");
            context.Write(output, functionName);

            context.WriteLine(output, $"(");
            context.Write(output, $"    Bdd {rootPCScope.PathConstraintVar}");
            foreach (var param in function.Signature.Parameters)
            {
                context.WriteLine(output, ",");
                context.Write(output, $"    {GetSymbolicType(param.Type, true)} {CompilationContext.GetVar(param.Name)}");
            }
            context.WriteLine(output);
            context.Write(output, ") ");

            context.WriteLine(output, "{");
            WriteFunctionBody(context, output, rootPCScope, function);
            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteFunctionBody(CompilationContext context, StringWriter output, PathConstraintScope rootPCScope, Function function)
        {
            foreach (var local in function.LocalVariables)
            {
                context.WriteLine(output, $"{GetSymbolicType(local.Type)} {CompilationContext.GetVar(local.Name)} =");
                context.WriteLine(output, $"    {GetDefaultValue(context, rootPCScope, local.Type)};");
                context.WriteLine(output);
            }

            if (!function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                context.WriteLine(output, $"{GetSymbolicType(function.Signature.ReturnType)} {CompilationContext.ReturnValue} = {GetValueSummaryOps(context, function.Signature.ReturnType).GetName()}.empty();");
                context.WriteLine(output);
            }

            WriteStmt(function, context, output, ControlFlowContext.FreshFuncContext(context, rootPCScope), function.Body);

            if (!function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                context.WriteLine(output, $"return {CompilationContext.ReturnValue};");
            }
        }

        private bool CanEarlyReturn(IPStmt stmt)
        {
            switch (stmt)
            {
                case CompoundStmt compoundStmt:
                    return compoundStmt.Statements.Any((subStmt) => CanEarlyReturn(subStmt));
                case IfStmt ifStmt:
                    return CanEarlyReturn(ifStmt.ThenBranch) || CanEarlyReturn(ifStmt.ElseBranch);
                case WhileStmt whileStmt:
                    return CanEarlyReturn(whileStmt.Body);

                case GotoStmt _:
                case PopStmt _:
                case RaiseStmt _:
                case ReturnStmt _:
                    return true;
                
                default:
                    return false;
            }
        }

        private bool MustEarlyReturn(IPStmt stmt)
        {
            switch (stmt)
            {
                case CompoundStmt compoundStmt:
                    return compoundStmt.Statements.Any((subStmt) => MustEarlyReturn(subStmt));
                case IfStmt ifStmt:
                    return MustEarlyReturn(ifStmt.ThenBranch) && MustEarlyReturn(ifStmt.ElseBranch);
                case WhileStmt whileStmt:
                    return MustEarlyReturn(whileStmt.Body);

                case GotoStmt _:
                case PopStmt _:
                case RaiseStmt _:
                case ReturnStmt _:
                    return true;

                default:
                    return false;
            }
        }

        private bool CanJumpOut(IPStmt stmt)
        {
            switch (stmt)
            {
                case CompoundStmt compoundStmt:
                    return compoundStmt.Statements.Any((subStmt) => CanJumpOut(subStmt));
                case IfStmt ifStmt:
                    return CanJumpOut(ifStmt.ThenBranch) || CanJumpOut(ifStmt.ElseBranch);
                case WhileStmt whileStmt:
                    // Any breaks or continues inside this loop body will be "caught" by the loop,
                    // so we only want to consider statements which return from the entire function.
                    return CanEarlyReturn(whileStmt.Body);

                case GotoStmt _:
                case PopStmt _:
                case RaiseStmt _:
                case ReturnStmt _:
                case BreakStmt _:
                case ContinueStmt _:
                    return true;

                default:
                    return false;
            }
        }

        private bool MustJumpOut(IPStmt stmt)
        {
            switch (stmt)
            {
                case CompoundStmt compoundStmt:
                    return compoundStmt.Statements.Any((subStmt) => MustJumpOut(subStmt));
                case IfStmt ifStmt:
                    return MustJumpOut(ifStmt.ThenBranch) && MustJumpOut(ifStmt.ElseBranch);
                case WhileStmt whileStmt:
                    // Any breaks or continues inside this loop body will be "caught" by the loop,
                    // so we only want to consider statements which return from the entire function.
                    return MustEarlyReturn(whileStmt.Body);

                case GotoStmt _:
                case PopStmt _:
                case RaiseStmt _:
                case ReturnStmt _:
                case BreakStmt _:
                case ContinueStmt _:
                    return true;

                default:
                    return false;
            }
        }

        private void WriteStmt(Function function, CompilationContext context, StringWriter output, ControlFlowContext flowContext, IPStmt stmt)
        {
            switch (stmt)
            {
                case AssignStmt assignStmt:
                    if (!assignStmt.Value.Type.IsSameTypeAs(assignStmt.Location.Type))
                    {
                        throw new NotImplementedException($"Cannot yet handle assignment to variable of type {assignStmt.Location.Type.CanonicalRepresentation} from value of type {assignStmt.Value.Type.CanonicalRepresentation}");
                    }

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        flowContext.pcScope,
                        assignStmt.Location,
                        false,
                        locationTemp =>
                        {
                            context.Write(output, $"{locationTemp} = ");
                            WriteExpr(context, output, flowContext.pcScope, assignStmt.Value);
                            context.WriteLine(output, ";");
                        }
                    );

                    break;

                case MoveAssignStmt moveStmt:
                    if (!moveStmt.FromVariable.Type.IsSameTypeAs(moveStmt.ToLocation.Type))
                    {
                        throw new NotImplementedException($"Cannot yet handle assignment to variable of type {moveStmt.ToLocation.Type.CanonicalRepresentation} from value of type {moveStmt.FromVariable.Type.CanonicalRepresentation}");
                    }

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        flowContext.pcScope,
                        moveStmt.ToLocation,
                        false,
                        locationTemp =>
                        {
                            context.Write(output, $"{locationTemp} = ");
                            WriteExpr(context, output, flowContext.pcScope, new VariableAccessExpr(moveStmt.FromVariable.SourceLocation, moveStmt.FromVariable));
                            context.WriteLine(output, ";");
                        }
                    );

                    break;

                case ReturnStmt returnStmt:
                    if (!(returnStmt.ReturnValue is null))
                    {
                        var summaryOps = GetValueSummaryOps(context, returnStmt.ReturnValue.Type).GetName();

                        context.Write(output, $"{CompilationContext.ReturnValue} = {summaryOps}.merge2({CompilationContext.ReturnValue}, ");
                        WriteExpr(context, output, flowContext.pcScope, returnStmt.ReturnValue);
                        context.WriteLine(output, $");");
                    }

                    context.WriteLine(output, $"{flowContext.pcScope.PathConstraintVar} = Bdd.constFalse();");

                    if (!(flowContext.loopScope is null))
                    {
                        context.WriteLine(output, $"{flowContext.loopScope.Value.LoopEarlyReturnFlag} = true;");
                    }

                    if (!(flowContext.branchScope is null))
                    {
                        context.WriteLine(output, $"{flowContext.branchScope.Value.JumpedOutFlag} = true;");
                    }

                    break;

                case BreakStmt breakStmt:
                    Debug.Assert(flowContext.loopScope.HasValue);
                    context.WriteLine(output, $"{flowContext.loopScope.Value.LoopExitsList}.add({flowContext.pcScope.PathConstraintVar});");

                    if (flowContext.branchScope.HasValue)
                    {
                        context.WriteLine(output, $"{flowContext.branchScope.Value.JumpedOutFlag} = true;");
                    }

                    context.WriteLine(output, $"{flowContext.pcScope.PathConstraintVar} = Bdd.constFalse();");
                    break;

                case CompoundStmt compoundStmt:
                    // Used to deermine the number of closing braces to add at the end of the block
                    var nestedEarlyExitCheckCount = 0;

                    foreach (var subStmt in compoundStmt.Statements)
                    {
                        WriteStmt(function, context, output, flowContext, subStmt);
                        context.WriteLine(output);

                        if (MustJumpOut(subStmt))
                            break;

                        if (CanJumpOut(subStmt))
                        {
                            context.WriteLine(output, $"if (!{flowContext.pcScope.PathConstraintVar}.isConstFalse()) {{");
                            nestedEarlyExitCheckCount++;
                        }
                    }

                    for (var i = 0; i < nestedEarlyExitCheckCount; i++)
                    {
                        context.WriteLine(output, "}");
                    }

                    break;

                case WhileStmt whileStmt:
                    if (!(whileStmt.Condition is BoolLiteralExpr) && ((BoolLiteralExpr)whileStmt.Condition).Value)
                    {
                        throw new ArgumentOutOfRangeException("While statement condition should always be transformed to constant 'true' during IR simplification.");
                    }

                    ControlFlowContext loopContext = ControlFlowContext.FreshLoopContext(context);

                    /* Prologue */
                    context.WriteLine(output, $"java.util.List<Bdd> {loopContext.loopScope.Value.LoopExitsList} = new java.util.ArrayList<>();");
                    context.WriteLine(output, $"boolean {loopContext.loopScope.Value.LoopEarlyReturnFlag} = false;");
                    context.WriteLine(output, $"Bdd {loopContext.pcScope.PathConstraintVar} = {flowContext.pcScope.PathConstraintVar};");

                    /* Loop body */
                    context.WriteLine(output, $"while (!{loopContext.pcScope.PathConstraintVar}.isConstFalse()) {{");
                    WriteStmt(function, context, output, loopContext, whileStmt.Body);
                    context.WriteLine(output, "}");

                    /* Epilogue */
                    context.WriteLine(output, $"if ({loopContext.loopScope.Value.LoopEarlyReturnFlag}) {{");
                    context.WriteLine(output, $"{flowContext.pcScope.PathConstraintVar} = Bdd.orMany({loopContext.loopScope.Value.LoopExitsList});");
                    if (flowContext.branchScope.HasValue)
                    {
                        context.WriteLine(output, $"{flowContext.branchScope.Value.JumpedOutFlag} = true;");
                    }
                    context.WriteLine(output, "}");

                    break;

                case IfStmt ifStmt:
                    /* Prologue */

                    var condTemp = context.FreshTempVar();
                    Debug.Assert(ifStmt.Condition.Type.IsSameTypeAs(PrimitiveType.Bool));
                    context.Write(output, $"{GetSymbolicType(PrimitiveType.Bool)} {condTemp} = ");
                    WriteExpr(context, output, flowContext.pcScope, ifStmt.Condition);
                    context.WriteLine(output, ";");

                    ControlFlowContext thenContext = flowContext.FreshBranchSubContext(context);
                    ControlFlowContext elseContext = flowContext.FreshBranchSubContext(context);

                    context.WriteLine(output, $"Bdd {thenContext.pcScope.PathConstraintVar} = BoolUtils.trueCond({condTemp});");
                    context.WriteLine(output, $"Bdd {elseContext.pcScope.PathConstraintVar} = BoolUtils.falseCond({condTemp});");

                    context.WriteLine(output, $"boolean {thenContext.branchScope.Value.JumpedOutFlag} = false;");
                    context.WriteLine(output, $"boolean {elseContext.branchScope.Value.JumpedOutFlag} = false;");

                    /* Body */

                    context.WriteLine(output, $"if (!{thenContext.pcScope.PathConstraintVar}.isConstFalse()) {{");
                    context.WriteLine(output, "// 'then' branch");
                    WriteStmt(function, context, output, thenContext, ifStmt.ThenBranch);
                    context.WriteLine(output, "}");
                    
                    if (!(ifStmt.ElseBranch is null))
                    {
                        context.WriteLine(output, $"if (!{elseContext.pcScope.PathConstraintVar}.isConstFalse()) {{");
                        context.WriteLine(output, "// 'else' branch");
                        WriteStmt(function, context, output, elseContext, ifStmt.ElseBranch);
                        context.WriteLine(output, "}");
                    }

                    /* Epilogue */

                    context.WriteLine(output, $"if ({thenContext.branchScope.Value.JumpedOutFlag} || {elseContext.branchScope.Value.JumpedOutFlag}) {{");
                    context.WriteLine(output, $"{flowContext.pcScope.PathConstraintVar} = {thenContext.pcScope.PathConstraintVar}.or({elseContext.pcScope.PathConstraintVar});");

                    if (flowContext.branchScope.HasValue)
                    {
                        context.WriteLine(output, $"{flowContext.branchScope.Value.JumpedOutFlag} = true;");
                    }

                    context.WriteLine(output, "}");

                    break;

                case FunCallStmt funCallStmt:
                    WriteFunCall(context, output, flowContext.pcScope, funCallStmt.Function, funCallStmt.ArgsList);
                    context.WriteLine(output,";");
                    break;

                case InsertStmt insertStmt:
                    {
                        var isMap = PLanguageType.TypeIsOfKind(insertStmt.Variable.Type, TypeKind.Map);

                        WriteWithLValueMutationContext(
                            context,
                            output,
                            flowContext.pcScope,
                            insertStmt.Variable,
                            true,
                            (structureTemp) =>
                            {
                                var structureOps = GetValueSummaryOps(context, insertStmt.Variable.Type);

                                context.Write(output, $"{structureTemp} = ");

                                if (isMap)
                                    context.Write(output, $"{structureOps.GetName()}.add(");
                                else
                                    context.Write(output, $"{structureOps.GetName()}.insert(");

                                WriteExpr(context, output, flowContext.pcScope, insertStmt.Index);
                                context.Write(output, ", ");
                                WriteExpr(context, output, flowContext.pcScope, insertStmt.Value);
                                context.WriteLine(output, ");");
                            }
                        );

                        break;
                    }

                case RemoveStmt removeStmt:
                    {
                        var isMap = PLanguageType.TypeIsOfKind(removeStmt.Variable.Type, TypeKind.Map);

                        WriteWithLValueMutationContext(
                            context,
                            output,
                            flowContext.pcScope,
                            removeStmt.Variable,
                            true,
                            (structureTemp) =>
                            {
                                var structureOps = GetValueSummaryOps(context, removeStmt.Variable.Type);

                                context.Write(output, $"{structureTemp} = ");

                                if (isMap)
                                    context.Write(output, $"{structureOps.GetName()}.remove(");
                                else
                                    context.Write(output, $"{structureOps.GetName()}.removeAt(");

                                WriteExpr(context, output, flowContext.pcScope, removeStmt.Value);
                                context.WriteLine(output, ");");
                            }
                        );
                    }
                    break;

                default:
                    context.WriteLine(output, $"/* Skipping statement '{stmt.GetType().Name}' */");
                    // throw new NotImplementedException($"Statement type '{stmt.GetType().Name}' is not supported");
                    break;
            }
        }

        private void WriteFunCall(CompilationContext context, StringWriter output, PathConstraintScope pcScope, Function function, IReadOnlyList<IPExpr> args)
        {
            var isStatic = function.Owner == null;
            if (!isStatic)
            {
                throw new NotImplementedException("Calls to non-static methods not yet supported");
            }

            var isAsync = function.CanReceive == true;
            if (isAsync)
            {
                throw new NotImplementedException("Calls to async methods not yet supported");
            }

            context.Write(output, $"{context.MainClassName}.{context.GetNameForDecl(function)}({pcScope.PathConstraintVar}");
            foreach (var param in args)
            {
                context.Write(output, ", ");
                WriteExpr(context, output, pcScope, param);
            }
            context.Write(output, ")");
        }

        private void WriteWithLValueMutationContext(
            CompilationContext context,
            StringWriter output,
            PathConstraintScope pcScope,
            IPExpr lvalue,
            bool needOrigValue,
            Action<string> writeMutator)
        {
            switch (lvalue)
            {
                case MapAccessExpr mapAccessExpr:
                    PLanguageType valueType = mapAccessExpr.Type;
                    PLanguageType indexType = mapAccessExpr.IndexExpr.Type;

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        pcScope,
                        mapAccessExpr.MapExpr,
                        true,
                        mapTemp =>
                        {
                            var valueTemp = context.FreshTempVar();
                            var indexTemp = context.FreshTempVar();

                            context.Write(output, $"{GetSymbolicType(indexType)} {indexTemp} = ");
                            WriteExpr(context, output, pcScope, mapAccessExpr.IndexExpr);
                            context.WriteLine(output, ";");

                            context.Write(output, $"{GetSymbolicType(valueType)} {valueTemp}");
                            if (needOrigValue)
                            {
                                context.WriteLine(output, $" = unwrapOrThrow({GetValueSummaryOps(context, mapAccessExpr.Type).GetName()}.get({mapTemp}, {indexTemp}));");
                            }
                            else
                            {
                                context.WriteLine(output, ";");
                            }

                            writeMutator(valueTemp);

                            context.WriteLine(output, $"{mapTemp} = {GetValueSummaryOps(context, mapAccessExpr.Type).GetName()}.put({mapTemp}, {indexTemp}, {valueTemp})");
                        }
                    );
                    break;

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    WriteWithLValueMutationContext(
                        context,
                        output,
                        pcScope,
                        namedTupleAccessExpr.SubExpr,
                        true,
                        namedTupleTemp =>
                        {
                            var fieldTemp = context.FreshTempVar();
                            var fieldType = GetSymbolicType(namedTupleAccessExpr.Type);
                            context.Write(output, $"{fieldType} {fieldTemp}");
                            if (needOrigValue)
                            {
                                context.WriteLine(output, $" = ({fieldType}){namedTupleTemp}.getField(\"{namedTupleAccessExpr.FieldName}\");");
                            }
                            else
                            {
                                context.WriteLine(output, ";");
                            }

                            writeMutator(fieldTemp);

                            context.WriteLine(
                                output,
                                $"{namedTupleTemp} = {namedTupleTemp}.setField(" +
                                $"\"{namedTupleAccessExpr.FieldName}\", " +
                                $"{fieldTemp});");
                        }
                    );
                    break;

                case TupleAccessExpr tupleAccessExpr:
                    WriteWithLValueMutationContext(
                        context,
                        output,
                        pcScope,
                        tupleAccessExpr.SubExpr,
                        true,
                        tupleTemp =>
                        {
                            var fieldTemp = context.FreshTempVar();
                            var fieldType = GetSymbolicType(tupleAccessExpr.Type);
                            context.Write(output, $"{fieldType} {fieldTemp}");
                            if (needOrigValue)
                            {
                                context.Write(output, $"({fieldType}){tupleTemp}.getField({tupleAccessExpr.FieldNo});");
                            }
                            else
                            {
                                context.Write(output, ";");
                            }

                            writeMutator(fieldTemp);

                            context.WriteLine(
                                output,
                                $"{tupleTemp} = {tupleTemp}.setField(" +
                                $"{tupleAccessExpr.FieldNo}," +
                                $"{fieldTemp});");
                        }
                    );
                    break;

                case SeqAccessExpr seqAccessExpr:
                    PLanguageType elementType = seqAccessExpr.Type;

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        pcScope,
                        seqAccessExpr.SeqExpr,
                        true,
                        seqTemp =>
                        {
                            var elementTemp = context.FreshTempVar();
                            var indexTemp = context.FreshTempVar();

                            context.Write(output, $"{GetSymbolicType(PrimitiveType.Int)} {indexTemp} = ");
                            WriteExpr(context, output, pcScope, seqAccessExpr.IndexExpr);
                            context.WriteLine(output, ";");

                            context.Write(output, $"{GetSymbolicType(elementType)} {elementTemp}");
                            if (needOrigValue)
                            {
                                context.WriteLine(output, $" = unwrapOrThrow({GetValueSummaryOps(context, seqAccessExpr.Type).GetName()}.get({seqTemp}, {indexTemp}));");
                            }
                            else
                            {
                                context.WriteLine(output, ";");
                            }

                            writeMutator(elementTemp);

                            context.WriteLine(output, $"{seqTemp} = unwrapOrThrow({GetValueSummaryOps(context, seqAccessExpr.Type).GetName()}.set({seqTemp}, {indexTemp}, {elementTemp}));");
                        }
                    );
                    break;
                case VariableAccessExpr variableAccessExpr:
                    var name = variableAccessExpr.Variable.Name;
                    var type = variableAccessExpr.Variable.Type;

                    var unguarded = CompilationContext.GetVar(name);
                    var summaryOps = GetValueSummaryOps(context, type).GetName();

                    var guardedTemp = context.FreshTempVar();

                    context.Write(output, $"{GetSymbolicType(variableAccessExpr.Type)} {guardedTemp}");
                    
                    if (needOrigValue)
                    {
                        context.WriteLine(output, $" = {summaryOps}.guard({unguarded}, {pcScope.PathConstraintVar});    ");
                    }
                    else
                    {
                        context.WriteLine(output, ";");
                    }

                    writeMutator(guardedTemp);

                    context.WriteLine(output,
                        $"{unguarded} = {summaryOps}.merge2(" +
                        $"{summaryOps}.guard({unguarded}, {pcScope.PathConstraintVar}.not())," +
                        $"{guardedTemp});");

                    break;
                
                default:
                    throw new ArgumentOutOfRangeException($"Expression type '{lvalue.GetType().Name}' is not an lvalue");
            }
        }

        private void WriteExpr(CompilationContext context, StringWriter output, PathConstraintScope pcScope, IPExpr expr)
        {
            switch (expr)
            {
                case CloneExpr cloneExpr:
                    WriteExpr(context, output, pcScope, cloneExpr.Term);
                    break;
                case UnaryOpExpr unaryOpExpr:
                    var lambdaTemp = context.FreshTempVar();
                    context.Write(output, "(");
                    WriteExpr(context, output, pcScope, unaryOpExpr.SubExpr);
                    context.Write(output, $").map(({lambdaTemp}) -> {UnOpToStr(unaryOpExpr.Operation)}{lambdaTemp})");
                    break;
                case BinOpExpr binOpExpr:
                    var isPrimitive = binOpExpr.Lhs.Type is PrimitiveType && binOpExpr.Rhs.Type is PrimitiveType;
                    var isEnum = binOpExpr.Lhs.Type is EnumType && binOpExpr.Rhs.Type is EnumType;
                    var isEquality = binOpExpr.Operation == BinOpType.Eq || binOpExpr.Operation == BinOpType.Neq;

                    if (!(isPrimitive || (isEnum && isEquality)))
                    {
                        throw new NotImplementedException("Binary operations are currently only supported between primitive types and enums");
                    }

                    var lhsLambdaTemp = context.FreshTempVar();
                    var rhsLambdaTemp = context.FreshTempVar();

                    context.Write(output, "(");
                    WriteExpr(context, output, pcScope, binOpExpr.Lhs);
                    context.Write(output, ").map2(");
                    WriteExpr(context, output, pcScope, binOpExpr.Rhs);
                    context.Write(
                        output,
                        $", ({lhsLambdaTemp}, {rhsLambdaTemp}) -> " +
                        $"{lhsLambdaTemp} {BinOpToStr(binOpExpr.Operation)} {rhsLambdaTemp})"
                    );

                    break;
                case BoolLiteralExpr boolLiteralExpr:
                    {
                        var unguarded = $"new { GetSymbolicType(PrimitiveType.Bool) }({boolLiteralExpr.Value})";
                        var guarded = $"{GetValueSummaryOps(context, PrimitiveType.Bool).GetName()}.guard({unguarded}, {pcScope.PathConstraintVar})";
                        context.Write(output, guarded);
                        break;
                    }
                case DefaultExpr defaultExpr:
                    context.Write(output, GetDefaultValue(context, pcScope, defaultExpr.Type));
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                    {
                        var unguarded = $"new { GetSymbolicType(PrimitiveType.Float) }({floatLiteralExpr.Value})";
                        var guarded = $"{GetValueSummaryOps(context, PrimitiveType.Float).GetName()}.guard({unguarded}, {pcScope.PathConstraintVar})";
                        context.Write(output, guarded);
                        break;
                    }
                case IntLiteralExpr intLiteralExpr:
                    {
                        var unguarded = $"new { GetSymbolicType(PrimitiveType.Int) }({intLiteralExpr.Value})";
                        var guarded = $"{GetValueSummaryOps(context, PrimitiveType.Int).GetName()}.guard({unguarded}, {pcScope.PathConstraintVar})";
                        context.Write(output, guarded);
                        break;
                    }
                case MapAccessExpr mapAccessExpr:
                    context.Write(output, $"unwrapOrThrow({GetValueSummaryOps(context, mapAccessExpr.Type).GetName()}.get(");
                    WriteExpr(context, output, pcScope, mapAccessExpr.MapExpr);
                    context.Write(output, ", ");
                    WriteExpr(context, output, pcScope, mapAccessExpr.IndexExpr);
                    context.Write(output, "))");
                    break;
                case SeqAccessExpr seqAccessExpr:
                    context.Write(output, $"unwrapOrThrow({GetValueSummaryOps(context, seqAccessExpr.Type).GetName()}.get(");
                    WriteExpr(context, output, pcScope, seqAccessExpr.SeqExpr);
                    context.Write(output, ", ");
                    WriteExpr(context, output, pcScope, seqAccessExpr.IndexExpr);
                    context.Write(output, "))");
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    context.Write(output, $"({GetSymbolicType(namedTupleAccessExpr.Type)})(");
                    WriteExpr(context, output, pcScope, namedTupleAccessExpr.SubExpr);
                    context.Write(output, $").getNamedField(\"{namedTupleAccessExpr.FieldName}\")");
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    context.Write(output, $"({GetSymbolicType(tupleAccessExpr.Type)})(");
                    WriteExpr(context, output, pcScope, tupleAccessExpr.SubExpr);
                    context.Write(output, $").getField({tupleAccessExpr.FieldNo})");
                    break;
                case NamedTupleExpr namedTupleExpr:
                    context.Write(output, "new NamedTupleVS(");
                    var fields = (namedTupleExpr.Type.Canonicalize() as NamedTupleType).Fields;
                    for (int i = 0; i < namedTupleExpr.TupleFields.Count; i++)
                    {
                        var fieldName = fields[i].Name;
                        var field = namedTupleExpr.TupleFields[i];
                        context.Write(output, $"\"{fieldName}\", ");
                        WriteExpr(context, output, pcScope, field);
                        if (i + 1 != namedTupleExpr.TupleFields.Count)
                            context.Write(output, ", ");
                    }
                    context.Write(output, ")");
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    context.Write(output, "new TupleVS(");
                    for (int i = 0; i < unnamedTupleExpr.TupleFields.Count; i++)
                    {
                        WriteExpr(context, output, pcScope, unnamedTupleExpr.TupleFields[i]);
                        if (i + 1 != unnamedTupleExpr.TupleFields.Count)
                            context.Write(output, ", ");
                    }
                    context.Write(output, ")");
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    {
                        var unguarded = $"new { GetSymbolicType(PrimitiveType.Int) }({enumElemRefExpr.Value.Value} /* enum {enumElemRefExpr.Type.OriginalRepresentation} elem {enumElemRefExpr.Value.Name} */)";
                        var guarded = $"{GetValueSummaryOps(context, PrimitiveType.Int).GetName()}.guard({unguarded}, {pcScope.PathConstraintVar})";
                        context.Write(output, guarded);
                        break;
                    }
                case VariableAccessExpr variableAccessExpr:
                    context.Write(output,
                        $"{GetValueSummaryOps(context, variableAccessExpr.Type).GetName()}.guard(" +
                        $"{CompilationContext.GetVar(variableAccessExpr.Variable.Name)}, " +
                        $"{pcScope.PathConstraintVar})");
                    break;
                case LinearAccessRefExpr linearAccessExpr:
                    context.Write(output,
                        $"{GetValueSummaryOps(context, linearAccessExpr.Type).GetName()}.guard(" +
                        $"{CompilationContext.GetVar(linearAccessExpr.Variable.Name)}, " +
                        $"{pcScope.PathConstraintVar})");
                    break;
                case FunCallExpr funCallExpr:
                    WriteFunCall(context, output, pcScope, funCallExpr.Function, funCallExpr.Arguments);
                    break;
                case ContainsExpr containsExpr:
                    var isMap = PLanguageType.TypeIsOfKind(containsExpr.Collection.Type, TypeKind.Map);

                    var structureOps = GetValueSummaryOps(context, containsExpr.Collection.Type);

                    if (isMap)
                        context.Write(output, $"{structureOps.GetName()}.containsKey(");
                    else
                        context.Write(output, $"{structureOps.GetName()}.contains(");

                    WriteExpr(context, output, pcScope, containsExpr.Collection);
                    context.Write(output, ", ");
                    WriteExpr(context, output, pcScope, containsExpr.Item);
                    context.Write(output, ")");
                    break;
                case NondetExpr _:
                case FairNondetExpr _:
                    context.Write(output, $"{GetValueSummaryOps(context, PrimitiveType.Bool).GetName()}" +
                        $".guard(BoolUtils.fromTrueGuard(Bdd.newVar()), {pcScope.PathConstraintVar})");
                    break;
                default:
                    context.Write(output, $"/* Skipping expr '{expr.GetType().Name}' */");
                    break;
            }
        }

        // TODO: This is copied from PSharpCodeGenerator.cs.  Should we factor this out into some common location?
        private string BinOpToStr(BinOpType binOpType)
        {
            switch (binOpType)
            {
                case BinOpType.Add:
                    return "+";
                case BinOpType.Sub:
                    return "-";
                case BinOpType.Mul:
                    return "*";
                case BinOpType.Div:
                    return "/";
                case BinOpType.Lt:
                    return "<";
                case BinOpType.Le:
                    return "<=";
                case BinOpType.Gt:
                    return ">";
                case BinOpType.Ge:
                    return ">=";
                case BinOpType.And:
                    return "&&";
                case BinOpType.Or:
                    return "||";
                case BinOpType.Eq:
                    return "==";
                case BinOpType.Neq:
                    return "!=";
                default:
                    throw new ArgumentOutOfRangeException(nameof(binOpType), binOpType, null);
            }
        }

        // TODO: This is copied from PSharpCodeGenerator.cs.  Should we refactor this into some common location?
        private static string UnOpToStr(UnaryOpType operation)
        {
            switch (operation)
            {
                case UnaryOpType.Negate:
                    return "-";
                case UnaryOpType.Not:
                    return "!";
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }
        private string GetConcreteBoxedType(PLanguageType type)
        {
            switch (type)
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "Boolean";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "Integer";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "Float";
                default:
                    throw new NotImplementedException($"Concrete type '{type.OriginalRepresentation}' is not supported");
            }
        }

        private string GetSymbolicType(PLanguageType type, bool isVar = false)
        {
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "PrimVS<Boolean>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "PrimVS<Integer>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "PrimVS<Float>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    if (isVar)
                        throw new NotImplementedException("Variables of type 'null' not yet supported");
                    else
                        return "void";
                case SequenceType sequenceType:
                    return $"ListVS<{GetSymbolicType(sequenceType.ElementType, true)}>";
                case MapType mapType:
                    return $"MapVS<" +
                        $"{GetConcreteBoxedType(mapType.KeyType)}, " +
                        $"{GetSymbolicType(mapType.ValueType, true)}>";
                case NamedTupleType _:
                    return "NamedTupleVS";
                case TupleType _:
                    return "TupleVS";
                case EnumType enumType:
                    return $"PrimVS<Integer> /* enum {enumType.OriginalRepresentation} */";
                default:
                    throw new NotImplementedException($"Symbolic type '{type.OriginalRepresentation}' not supported");
            }
        }

        private string GetValueSummaryOpsType(PLanguageType type)
        {
            switch (type)
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "PrimVS.Ops<Boolean>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "PrimVS.Ops<Integer>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "PrimVS.Ops<Float>";
                case SequenceType sequenceType:
                    return $"ListVS.Ops<{GetSymbolicType(sequenceType.ElementType, true)}>";
                case MapType mapType:
                    return $"MapVS.Ops<" +
                        $"{GetConcreteBoxedType(mapType.KeyType)}, " +
                        $"{GetSymbolicType(mapType.ValueType, true)}>";
                case NamedTupleType _:
                    return "NamedTupleVs.Ops";
                case TupleType _:
                    return "TupleVS.Ops";
                case EnumType enumType:
                    return $"PrimVS.Ops<Integer> /* enum {enumType.OriginalRepresentation} */";
                default:
                    throw new NotImplementedException($"Symbolic type '{type.OriginalRepresentation}' ops type not supported");
            }
        }

        private ValueSummaryOps GetValueSummaryOps(CompilationContext context, PLanguageType type)
        {
            var opsType = GetValueSummaryOpsType(type);
            string defBody;
            switch (type)
            {
                case PrimitiveType primitiveType when
                    primitiveType.IsSameTypeAs(PrimitiveType.Bool) ||
                    primitiveType.IsSameTypeAs(PrimitiveType.Int) ||
                    primitiveType.IsSameTypeAs(PrimitiveType.Float):

                    defBody = $"new {opsType}()";
                    break;

                case SequenceType sequenceType:
                    var elemOps = GetValueSummaryOps(context, sequenceType.ElementType);
                    defBody = $"new {opsType}({elemOps.GetName()})";
                    break;
                case MapType mapType:
                    var valOps = GetValueSummaryOps(context, mapType.ValueType);
                    defBody = $"new {opsType}({valOps.GetName()})";
                    break;

                case NamedTupleType namedTupleType:
                    {
                        var allFieldOps = new List<string>();
                        foreach (var field in namedTupleType.Fields)
                        {
                            var fieldOps = GetValueSummaryOps(context, field.Type);
                            allFieldOps.Add($"\"{field.Name}\", {fieldOps.GetName()}");
                        }
                        defBody = $"new {opsType}({string.Join(", ", allFieldOps)})";
                    }
                    break;

                case TupleType tupleType:
                    {
                        var allFieldOps = new List<string>();
                        foreach (var field in tupleType.Types)
                        {
                            var fieldOps = GetValueSummaryOps(context, field);
                            allFieldOps.Add(fieldOps.GetName());
                        }
                        defBody = $"new {opsType}({string.Join(", ", allFieldOps)})";
                    }
                    break;

                case EnumType enumType:
                    defBody = $"new {opsType} ()";
                    break;

                default:
                    throw new NotImplementedException($"Symbolic type '{type.OriginalRepresentation}' ops not supported");
            }

            return context.ValueSummaryOpsForDef(new ValueSummaryOpsDef(opsType, defBody));
        }

        private string GetDefaultValue(CompilationContext context, PathConstraintScope pcScope, PLanguageType type)
        {
            string unguarded;
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    unguarded = $"new {GetSymbolicType(type)}(false)";
                    break;
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    unguarded = $"new {GetSymbolicType(type)}(0)";
                    break;
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    unguarded = $"new {GetSymbolicType(type)}(0.0f)";
                    break;
                case SequenceType _:
                    unguarded = $"new {GetSymbolicType(type)}()";
                    break;
                case MapType _:
                    unguarded = $"new {GetSymbolicType(type)}()";
                    break;
                case NamedTupleType namedTupleType:
                    {
                        var allFieldDefaults = new List<string>();
                        foreach (var field in namedTupleType.Fields)
                        {
                            var fieldDefault = GetDefaultValue(context, pcScope, field.Type);
                            allFieldDefaults.Add($"\"{field.Name}\", {fieldDefault}");
                        }
                        return $"new {GetSymbolicType(type)}({string.Join(", ", allFieldDefaults)})";
                    }
                case TupleType tupleType:
                    {
                        var allFieldDefaults = new List<string>();
                        foreach (var field in tupleType.Types)
                        {
                            var fieldDefault = GetDefaultValue(context, pcScope, field);
                            allFieldDefaults.Add(fieldDefault);
                        }
                        return $"new {GetSymbolicType(type)}({string.Join(", ", allFieldDefaults)})";
                    }
                default:
                    throw new NotImplementedException($"Default value for symbolic type '{type.OriginalRepresentation}' not supported");
            }

            var guarded = $"{GetValueSummaryOps(context, type).GetName()}.guard({unguarded}, {pcScope.PathConstraintVar})";
            return guarded;
        }

        private void WriteSourcePrologue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "import symbolicp.*;");
            context.WriteLine(output);
            context.WriteLine(output, $"public class {context.MainClassName} {{");
        }

        private void WriteSourceEpilogue(CompilationContext context, StringWriter output)
        {
            for (int i = 0; i < context.PendingValueSummaryOpsDefs.Count; i++)
            {
                var def = context.PendingValueSummaryOpsDefs[i];
                var name = new ValueSummaryOps(i).GetName();
                context.WriteLine(output, $"private static final {def.opsType} {name} =");
                context.WriteLine(output, $"    {def.opsDef};");
                context.WriteLine(output);
            }

            context.WriteLine(output, "}");
        }
    }
}

