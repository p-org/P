﻿using System;
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
            Console.WriteLine("generated code");
            var context = new CompilationContext(job);
            var javaSource = GenerateSource(context, globalScope);
            Console.WriteLine("generated source");
            return new List<CompiledFile> { javaSource };
        }

        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);

            WriteSourcePrologue(context, source.Stream);

            context.WriteLine(source.Stream);

            WriteEventDefs(context, source.Stream, globalScope.Events);

            context.WriteLine(source.Stream);

            //WriteMachineTagDefs(context, source.Stream, globalScope.Machines);

            //context.WriteLine(source.Stream);

            foreach (var decl in globalScope.AllDecls)
                WriteDecl(context, source.Stream, decl);

            WriteMainDriver(context, source.Stream, globalScope);

            WriteSourceEpilogue(context, source.Stream);

            return source;
        }

        private void WriteEventDefs(CompilationContext context, StringWriter output, IEnumerable<PEvent> events)
        {
            context.WriteLine(output, $"static enum Events implements EventName {{");
            for (int i = 0; i < events.Count(); i++)
            {
                var pEvent = events.ElementAt(i);
                context.WriteLine(output, $"event_{pEvent.Name},");
            }
            context.WriteLine(output, $"}}");
        }

        private void WriteDecl(CompilationContext context, StringWriter output, IPDecl decl)
        {
            switch (decl)
            {
                case Function function:
                    if (function.IsForeign) break;
                        // throw new NotImplementedException("Foreign functions are not yet supported");

                    WriteFunction(context, output, function);
                    break;
                case Machine machine:
                    if (machine.IsSpec)
                        context.WriteLine(output, $"// Skipping monitor {machine.Name}");
                    else
                        WriteMachine(context, output, machine);
                    break;
                case PEvent _:
                    // Handled in WriteEventDefs
                    break;
                default:
                    context.WriteLine(output, $"// Skipping {decl.GetType().Name} '{decl.Name}'\n");
                    break;
            }
        }

        private void WriteMachine(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.GetNameForDecl(machine);
            context.WriteLine(output, $"public static class {declName} extends Machine {{");

            context.WriteLine(output);

            for (int i = 0; i < machine.States.Count(); i++)
            {
                var state = machine.States.ElementAt(i);
                context.Write(output, $"static State {context.GetNameForDecl(state)} = ");
                WriteState(context, output, state, i);
                context.WriteLine(output, ";");
            }

            foreach (var field in machine.Fields)
                context.WriteLine(output, $"private {GetSymbolicType(field.Type)} {CompilationContext.GetVar(field.Name)} = {GetDefaultValueNoGuard(context, field.Type)};");

            context.WriteLine(output);

            context.WriteLine(output, "@Override");
            context.WriteLine(output, "public void reset() {");
            context.WriteLine(output, "    super.reset();");
            foreach (var field in machine.Fields)
                context.WriteLine(output, $"    {CompilationContext.GetVar(field.Name)} = {GetDefaultValueNoGuard(context, field.Type)};");
            context.WriteLine(output, "}");

            context.WriteLine(output);


            WriteMachineConstructor(context, output, machine);

            context.WriteLine(output);

            foreach (var method in machine.Methods)
                WriteFunction(context, output, method);

            context.WriteLine(output, "}");
            context.WriteLine(output);
        }

        private void WriteMachineConstructor(CompilationContext context, StringWriter output, Machine machine)
        {
            var declName = context.GetNameForDecl(machine);
            context.WriteLine(output, $"public {declName}(int id) {{");
            context.Write(output, $"super(\"{context.GetMachineName(machine)}\", id, BufferSemantics.{context.GetBufferSemantics(machine)}, {context.GetNameForDecl(machine.StartState)}");

            foreach (var state in machine.States)
            {
                context.WriteLine(output, $", {context.GetNameForDecl(state)}");
            }

            context.WriteLine(output);
            context.WriteLine(output, ");");

            foreach (var state in machine.States)
            {
              WriteHandlerUpdate(context, output, state);
            }

            context.WriteLine(output, "}");
        }

        private void WriteHandlerUpdate(CompilationContext context, StringWriter output, State state)
        {
            context.Write(output, $"{context.GetNameForDecl(state)}.addHandlers(");
            bool first = true;
            foreach (var handler in state.AllEventHandlers)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    context.WriteLine(output, ",");
                }
                WriteEventHandler(context, output, handler);
            }
            context.WriteLine(output, ");");
        }

        private void WriteState(CompilationContext context, StringWriter output, State state, int i)
        {
            context.Write(output, $"new State(\"{context.GetNameForDecl(state)}\", {i}");
            /*
            foreach (var handler in state.AllEventHandlers)
            {
                context.WriteLine(output, ",");
                WriteEventHandler(context, output, handler, state);
            }
            context.WriteLine(output);
            */
            context.WriteLine(output, ") {");
            if (state.Entry != null)
            {
                var entryPcScope = context.FreshPathConstraintScope();

                context.WriteLine(output, $"@Override public void entry(Bdd {entryPcScope.PathConstraintVar}, Machine machine, Outcome outcome, UnionVS payload) {{");

                var entryFunc = state.Entry;
                context.Write(output, $"(({context.GetNameForDecl(entryFunc.Owner)})machine).{context.GetNameForDecl(entryFunc)}({entryPcScope.PathConstraintVar}, machine.sendEffects");
                if (entryFunc.CanChangeState ?? false)
                    context.Write(output, ", outcome");
                else if (entryFunc.CanRaiseEvent ?? false)
                    context.Write(output, ", outcome");
                if (entryFunc.Signature.Parameters.Count() > 0)
                {
                    Debug.Assert(entryFunc.Signature.Parameters.Count() == 1);
                    var payloadType = entryFunc.Signature.Parameters[0].Type;
                    var payloadTypeSymbolic = GetSymbolicType(payloadType);
                    var defaultPayload = GetDefaultValue(context, entryPcScope, payloadType);
                    context.Write(output, $", payload != null ? ({payloadTypeSymbolic}) ValueSummary.fromAny({entryPcScope.PathConstraintVar}, {defaultPayload}.getClass(), payload) : {defaultPayload}");
                }
                context.WriteLine(output, ");");

                context.WriteLine(output, "}");
            }
            if (state.Exit != null)
            {
                context.WriteLine(output, "@Override public void exit(Bdd pc, Machine machine) {");

                var exitFunc = state.Exit;
                Debug.Assert(!(exitFunc.CanChangeState ?? false));
                Debug.Assert(!(exitFunc.CanRaiseEvent ?? false));
                if (exitFunc.Signature.Parameters.Count() != 0)
                    throw new NotImplementedException("Exit functions with payloads are not yet supported");
                context.WriteLine(output, $"(({context.GetNameForDecl(exitFunc.Owner)})machine).{context.GetNameForDecl(exitFunc)}(pc, machine.sendEffects);");

                context.WriteLine(output, "}");
            }
            context.Write(output, "}");
        }

        private void WriteEventHandler(CompilationContext context, StringWriter output, KeyValuePair<PEvent, IStateAction> handler)
        {
            var eventTag = context.GetNameForDecl(handler.Key);
            switch (handler.Value)
            {
                case EventDefer _:
                    context.WriteLine(output, $"new DeferEventHandler({eventTag})");
                    break;
                case EventDoAction action:
                    context.WriteLine(output, $"new EventHandler({eventTag}) {{");
                    context.WriteLine(output, "@Override public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {");
                    var actionFunc = action.Target;
                    context.Write(output, $"(({context.GetNameForDecl(actionFunc.Owner)})machine).{context.GetNameForDecl(actionFunc)}(pc, machine.sendEffects");
                    if (actionFunc.CanChangeState ?? false)
                        context.Write(output, ", outcome");
                    else if (actionFunc.CanRaiseEvent ?? false)
                        context.Write(output, ", outcome");
                    if (actionFunc.Signature.Parameters.Count() == 1)
                    {
                        Debug.Assert(!handler.Key.PayloadType.IsSameTypeAs(PrimitiveType.Null));
                        var payloadVSType = GetSymbolicType(handler.Key.PayloadType);
                        var defaultPayload = GetDefaultValueNoGuard(context, handler.Key.PayloadType);
                        context.Write(output, $", ({payloadVSType}) ValueSummary.fromAny(pc, {defaultPayload}.getClass(), payload)");
                    }
                    context.WriteLine(output, ");");
                    context.WriteLine(output, "}");
                    context.Write(output, "}");
                    break;
                case EventGotoState gotoState:
                    var destTag = $"{context.GetNameForDecl(gotoState.Target)}";
                    context.Write(output, $"new GotoEventHandler({eventTag}, {destTag}");
                    context.Write(output, ")");

                    if (gotoState.TransitionFunction != null)
                    {
                        context.WriteLine(output, " {");
                        context.WriteLine(output, "@Override public void transitionAction(Bdd pc, Machine machine, UnionVS payload) {");

                        var transitionFunc = gotoState.TransitionFunction;
                        Debug.Assert(!(transitionFunc.CanChangeState ?? false));
                        Debug.Assert(!(transitionFunc.CanRaiseEvent ?? false));

                        context.Write(output, $"(({context.GetNameForDecl(transitionFunc.Owner)})machine).{context.GetNameForDecl(transitionFunc)}(pc, machine.sendEffects");
                        if (transitionFunc.Signature.Parameters.Count() == 1)
                        {
                            Debug.Assert(!handler.Key.PayloadType.IsSameTypeAs(PrimitiveType.Null));
                            var payloadVSType = GetSymbolicType(handler.Key.PayloadType);
                            var defaultPayload = GetDefaultValueNoGuard(context, handler.Key.PayloadType);
                            context.Write(output, $", ({payloadVSType}) ValueSummary.fromAny(pc, {defaultPayload}.getClass(), payload)");
                        }
                        context.WriteLine(output, ");");
                        context.WriteLine(output, "}");
                        context.Write(output, "}");
                    }
                    break;
                case EventIgnore _:
                    context.Write(output, $"new IgnoreEventHandler({eventTag})");
                    break;
                case EventPushState pushState:
                    context.Write(output, "/* Push state */");
                    var pushToTarget = $"{context.GetNameForDecl(pushState.Target)}";
                    context.Write(output, $"new PushEventHandler({eventTag}, {pushToTarget})");
                    break;
                default:
                    throw new NotImplementedException($"Unrecognized handler type {handler.Value.GetType().Name}");
            }
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

        private bool MayExitWithOutcome(Function func)
        {
            return (func.CanChangeState ?? false) || (func.CanRaiseEvent ?? false);
        }

        private enum FunctionReturnConvention
        {
            RETURN_VALUE,
            RETURN_VOID,
            RETURN_VALUE_OR_EXIT,
            // BDD indicates path constraint after the call, which may be more restricted
            // than the path constraint before the call if the function exited with an
            // outcome (i.e. a 'raise' or 'goto' statement) along some paths
            RETURN_BDD
        }

        private FunctionReturnConvention GetReturnConvention(Function function)
        {
            bool mayExit = MayExitWithOutcome(function);
            bool voidReturn = function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null);
            if (!voidReturn && !mayExit)
                return FunctionReturnConvention.RETURN_VALUE;
            if (voidReturn && !mayExit)
                return FunctionReturnConvention.RETURN_VOID;
            if (!voidReturn && mayExit)
                return FunctionReturnConvention.RETURN_VALUE_OR_EXIT;
            if (voidReturn && mayExit)
                return FunctionReturnConvention.RETURN_BDD;
            throw new InvalidOperationException();
        }

        private void WriteFunction(CompilationContext context, StringWriter output, Function function)
        {
            var isStatic = function.Owner == null;

            if (function.CanReceive == true)
                throw new NotImplementedException($"Async functions {context.GetNameForDecl(function)} are not supported");

            var staticKeyword = isStatic ? "static " : "";

            var rootPCScope = context.FreshPathConstraintScope();

            string returnType = null;
            switch (GetReturnConvention(function))
            {
                case FunctionReturnConvention.RETURN_VALUE:
                    returnType = GetSymbolicType(function.Signature.ReturnType);
                    break;
                case FunctionReturnConvention.RETURN_VOID:
                    returnType = "void";
                    break;
                case FunctionReturnConvention.RETURN_VALUE_OR_EXIT:
                    returnType = $"MaybeExited<{GetSymbolicType(function.Signature.ReturnType)}>";
                    break;
                case FunctionReturnConvention.RETURN_BDD:
                    returnType = "Bdd";
                    break;
            }

            var functionName = context.GetNameForDecl(function);

            context.WriteLine(output, $"{staticKeyword}{returnType} ");
            context.Write(output, functionName);

            context.WriteLine(output, $"(");
            context.WriteLine(output, $"Bdd {rootPCScope.PathConstraintVar},");
            context.Write(output, $"EffectCollection {CompilationContext.EffectCollectionVar}");
            if (function.CanChangeState ?? false)
            {
                Debug.Assert(function.Owner != null);
                context.WriteLine(output, ",");
                context.Write(output, "Outcome outcome");
            }
            else if (function.CanRaiseEvent ?? false)
            {
                context.WriteLine(output, ",");
                context.Write(output, "Outcome outcome");
            }
            foreach (var param in function.Signature.Parameters)
            {
                context.WriteLine(output, ",");
                context.Write(output, $"{GetSymbolicType(param.Type, true)} {CompilationContext.GetVar(param.Name)}");
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

            var returnConvention = GetReturnConvention(function);
            switch (returnConvention)
            {
                case FunctionReturnConvention.RETURN_VALUE:
                case FunctionReturnConvention.RETURN_VALUE_OR_EXIT:
                    context.WriteLine(output, $"{GetSymbolicType(function.Signature.ReturnType)} {CompilationContext.ReturnValue} = new {GetSymbolicType(function.Signature.ReturnType)}({GetDefaultValue(context, rootPCScope, function.Signature.ReturnType)});");
                    break;
                case FunctionReturnConvention.RETURN_VOID:
                case FunctionReturnConvention.RETURN_BDD:
                    break;
            }

            WriteStmt(function, context, output, ControlFlowContext.FreshFuncContext(context, rootPCScope), function.Body);

            switch (returnConvention)
            {
                case FunctionReturnConvention.RETURN_VALUE:
                    context.WriteLine(output, $"return {CompilationContext.ReturnValue};");
                    break;
                case FunctionReturnConvention.RETURN_VOID:
                    break;
                case FunctionReturnConvention.RETURN_VALUE_OR_EXIT:
                    context.WriteLine(output, $"return new MaybeExited({CompilationContext.ReturnValue}, {rootPCScope.PathConstraintVar});");
                    break;
                case FunctionReturnConvention.RETURN_BDD:
                    context.WriteLine(output, $"return {rootPCScope.PathConstraintVar};");
                    break;
            }
        }

        private FunCallExpr TryGetCallInAssignment(IPStmt stmt)
        {
            if (stmt is AssignStmt assign)
            {
                if (assign.Value is FunCallExpr call)
                {
                    return call;
                }
            }
            return null;
        }

        private bool CanEarlyReturn(IPStmt stmt)
        {
            var callExpr = TryGetCallInAssignment(stmt);
            if (callExpr != null)
                return MayExitWithOutcome(callExpr.Function);

            switch (stmt)
            {
                case CompoundStmt compoundStmt:
                    return compoundStmt.Statements.Any((subStmt) => CanEarlyReturn(subStmt));
                case IfStmt ifStmt:
                    return CanEarlyReturn(ifStmt.ThenBranch) || CanEarlyReturn(ifStmt.ElseBranch);
                case WhileStmt whileStmt:
                    return CanEarlyReturn(whileStmt.Body);
                case FunCallStmt callStmt:
                    return MayExitWithOutcome(callStmt.Function);

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
            var callExpr = TryGetCallInAssignment(stmt);
            if (callExpr != null)
                return MayExitWithOutcome(callExpr.Function);

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
                case FunCallStmt callStmt:
                    return MayExitWithOutcome(callStmt.Function);

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

        private void SetFlagsForPossibleReturn(CompilationContext context, StringWriter output, ControlFlowContext flowContext)
        {
            if (!(flowContext.loopScope is null))
            {
                context.WriteLine(output, $"{flowContext.loopScope.Value.LoopEarlyReturnFlag} = true;");
            }

            if (!(flowContext.branchScope is null))
            {
                context.WriteLine(output, $"{flowContext.branchScope.Value.JumpedOutFlag} = true;");
            }
        }

        private void WriteStmt(Function function, CompilationContext context, StringWriter output, ControlFlowContext flowContext, IPStmt stmt)
        {
            if (TryGetCallInAssignment(stmt) is FunCallExpr callExpr)
            {
                WriteFunCallStmt(context, output, flowContext, callExpr.Function, callExpr.Arguments, dest: (stmt as AssignStmt).Location);
                return;
            }

            switch (stmt)
            {
                case AssignStmt assignStmt:
                    CheckIsSupportedAssignment(assignStmt.Value.Type, assignStmt.Location.Type);
                    
                    WriteWithLValueMutationContext(
                        context,
                        output,
                        flowContext.pcScope,
                        assignStmt.Location,
                        false,
                        locationTemp =>
                        {
                            if (assignStmt.Value is NullLiteralExpr)
                            {
                                context.Write(output, $"{locationTemp} = {GetDefaultValueNoGuard(context, assignStmt.Location.Type)}.guard(Bdd.constFalse());");
                            } else
                            {
                                string inlineCastPrefix = GetInlineCastPrefix(assignStmt.Value.Type, assignStmt.Location.Type, context, flowContext.pcScope);
                                context.Write(output, $"{locationTemp} = {inlineCastPrefix}");
                                WriteExpr(context, output, flowContext.pcScope, assignStmt.Value);
                                if (inlineCastPrefix != "") context.Write(output, ")");
                                context.WriteLine(output, ";");
                            }
                        }
                    );

                    break;

                case MoveAssignStmt moveStmt:
                    CheckIsSupportedAssignment(moveStmt.FromVariable.Type, moveStmt.ToLocation.Type);

                    WriteWithLValueMutationContext(
                        context,
                        output,
                        flowContext.pcScope,
                        moveStmt.ToLocation,
                        false,
                        locationTemp =>
                        {
                            string inlineCastPrefix = GetInlineCastPrefix(moveStmt.FromVariable.Type, moveStmt.ToLocation.Type, context, flowContext.pcScope);
                            context.Write(output, $"{locationTemp} = {inlineCastPrefix}");
                            WriteExpr(context, output, flowContext.pcScope, new VariableAccessExpr(moveStmt.FromVariable.SourceLocation, moveStmt.FromVariable));
                            if (inlineCastPrefix != "") context.Write(output, ")");
                            context.WriteLine(output, ";");
                        }
                    );

                    break;

                case AssertStmt assertStmt:
                    context.Write(output, "Assert.prop(!(");
                    WriteExpr(context, output, flowContext.pcScope, assertStmt.Assertion);
                    context.Write(output, ").getValues().contains(Boolean.FALSE), ");
                    context.Write(output, $"\"{assertStmt.Message}\"");
                    context.Write(output, ", scheduler");
                    context.Write(output, $", {flowContext.pcScope.PathConstraintVar});");
                    break;

                case ReturnStmt returnStmt:
                    if (!(returnStmt.ReturnValue is null))
                    {
                        context.Write(output, $"{CompilationContext.ReturnValue} = {CompilationContext.ReturnValue}.update(");
                        context.Write(output, $"{flowContext.pcScope.PathConstraintVar}, ");
                        WriteExpr(context, output, flowContext.pcScope, returnStmt.ReturnValue);
                        context.WriteLine(output, $");");
                    }

                    context.WriteLine(output, $"{flowContext.pcScope.PathConstraintVar} = Bdd.constFalse();");
                    SetFlagsForPossibleReturn(context, output, flowContext);

                    break;

                case GotoStmt gotoStmt:
                    context.Write(output, $"outcome.addGuardedGoto({flowContext.pcScope.PathConstraintVar}, {context.GetNameForDecl(gotoStmt.State)}");
                    if (gotoStmt.Payload != null)
                    {
                        context.Write(output, $", new UnionVS(");
                        WriteExpr(context, output, flowContext.pcScope, gotoStmt.Payload);
                        context.Write(output, $")");
                    }
                    context.WriteLine(output, ");");

                    context.WriteLine(output, $"{flowContext.pcScope.PathConstraintVar} = Bdd.constFalse();");
                    SetFlagsForPossibleReturn(context, output, flowContext);

                    break;

                case RaiseStmt raiseStmt:
                    // TODO: Add type checking for the payload!
                    context.WriteLine(output, "// NOTE (TODO): We currently perform no typechecking on the payload!");

                    context.Write(output, $"outcome.addGuardedRaise({flowContext.pcScope.PathConstraintVar}, ");
                    WriteExpr(context, output, flowContext.pcScope, raiseStmt.PEvent);
                    if (raiseStmt.Payload.Count > 0)
                    {
                        // TODO: Determine how multi-payload raise statements are supposed to work
                        Debug.Assert(raiseStmt.Payload.Count == 1);
                        context.Write(output, ", new UnionVS(");
                        WriteExpr(context, output, flowContext.pcScope, raiseStmt.Payload[0]);
                        context.Write(output, ")");
                    }
                    context.WriteLine(output, ");");


                    context.WriteLine(output, $"{flowContext.pcScope.PathConstraintVar} = Bdd.constFalse();");
                    SetFlagsForPossibleReturn(context, output, flowContext);

                    break;

                case PrintStmt printStmt:
                    context.Write(output, $"RuntimeLogger.log(\"{printStmt.Message}\"");
                    foreach (var printArg in printStmt.Args)
                    {
                        context.Write(output, ", ");
                        WriteExpr(context, output, flowContext.pcScope, printArg);
                    }

                    context.WriteLine(output, ");");
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
                    // Used to determine the number of closing braces to add at the end of the block
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
                    WriteFunCallStmt(context, output, flowContext, funCallStmt.Function, funCallStmt.ArgsList);
                    break;

                case CtorStmt ctorStmt:
                    WriteCtorExpr(context, output, flowContext.pcScope, ctorStmt.Interface, ctorStmt.Arguments);
                    context.WriteLine(output, ";");
                    break;

                case SendStmt sendStmt:
                    context.Write(output, $"{CompilationContext.EffectCollectionVar}.send({flowContext.pcScope.PathConstraintVar}, ");
                    WriteExpr(context, output, flowContext.pcScope, sendStmt.MachineExpr);
                    context.Write(output, ", ");
                    WriteExpr(context, output, flowContext.pcScope, sendStmt.Evt);
                    context.Write(output, ", ");
                    if (sendStmt.Arguments.Count == 0)
                        context.Write(output, "null");
                    else if (sendStmt.Arguments.Count == 1)
                    {
                        context.Write(output, "new UnionVS(");
                        WriteExpr(context, output, flowContext.pcScope, sendStmt.Arguments[0]);
                        context.Write(output, ")");
                    }
                    else
                        throw new NotImplementedException("Send statements with more than one payload argument are not supported");
                    context.WriteLine(output, ");");
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
                                context.Write(output, $"{structureTemp} = ");
                                WriteExpr(context, output, flowContext.pcScope, insertStmt.Variable);
                                if (isMap)
                                    context.Write(output, $".add(");
                                else
                                    context.Write(output, $".insert(");
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
                                context.Write(output, $"{structureTemp} = ");
                                WriteExpr(context, output, flowContext.pcScope, removeStmt.Variable);

                                if (isMap)
                                    context.Write(output, $".remove(");
                                else
                                    context.Write(output, $".removeAt(");

                                WriteExpr(context, output, flowContext.pcScope, removeStmt.Value);
                                context.WriteLine(output, ");");
                            }
                        );
                    }
                    break;

                case PopStmt popStmt:
                    {
                        context.Write(output, $"outcome.addGuardedPop({flowContext.pcScope.PathConstraintVar});");
                    }
                    break;
                default:
                    context.WriteLine(output, $"/* Skipping statement '{stmt.GetType().Name}' */");
                    // throw new NotImplementedException($"Statement type '{stmt.GetType().Name}' is not supported");
                    break;
            }
        }

        private void CheckIsSupportedAssignment(PLanguageType valueType, PLanguageType locationType)
        {
            var valueIsMachineRef = valueType.IsSameTypeAs(PrimitiveType.Machine) || valueType is PermissionType;
            var locationIsMachineRef = locationType.IsSameTypeAs(PrimitiveType.Machine) || locationType is PermissionType;

            if (valueIsMachineRef && locationIsMachineRef)
                return;

            if (locationType.IsSameTypeAs(PrimitiveType.Any))
                return;

            if (!valueType.IsSameTypeAs(locationType))
            {
                throw new NotImplementedException(
                    $"Cannot yet handle assignment to variable of type {locationType.CanonicalRepresentation} " +
                    $"from value of type {valueType.CanonicalRepresentation}");
            }
        }

        private string GetInlineCastPrefix(PLanguageType valueType, PLanguageType locationType, CompilationContext context, PathConstraintScope pcScope) {
            var valueIsMachineRef = valueType.IsSameTypeAs(PrimitiveType.Machine) || valueType is PermissionType;
            var locationIsMachineRef = locationType.IsSameTypeAs(PrimitiveType.Machine) || locationType is PermissionType;

            if (valueIsMachineRef && locationIsMachineRef)
                return "";
            if (valueType.IsSameTypeAs(locationType)) return "";

            if (locationType.IsSameTypeAs(PrimitiveType.Any)) {
                return $"new UnionVS ({pcScope.PathConstraintVar}, {GetDefaultValueNoGuard(context, locationType)}.getClass(), ";
            } else if (valueType.IsSameTypeAs(PrimitiveType.Any))
            {
                return $"({GetSymbolicType(locationType)}) ValueSummary.fromAny({pcScope.PathConstraintVar}, {GetDefaultValueNoGuard(context, locationType)}.getClass(), ";
            }
            throw new NotImplementedException(
                    $"Cannot yet handle casting to variable of type {locationType.CanonicalRepresentation} " +
                    $"from value of type {valueType.CanonicalRepresentation}");
        }

        private void WriteFunCallStmt(CompilationContext context, StringWriter output, ControlFlowContext flowContext, Function function, IReadOnlyList<IPExpr> args, IPExpr dest=null)
        {
            var isAsync = function.CanReceive == true;
            if (isAsync)
            {
                throw new NotImplementedException("Calls to async methods not yet supported");
            }

            var returnConvention = GetReturnConvention(function);
            string returnTemp = null;
            switch (returnConvention)
            {
                case FunctionReturnConvention.RETURN_VALUE:
                    returnTemp = context.FreshTempVar();
                    context.Write(output, $"{GetSymbolicType(function.Signature.ReturnType)} {returnTemp} = ");
                    break;
                case FunctionReturnConvention.RETURN_VALUE_OR_EXIT:
                    returnTemp = context.FreshTempVar();
                    context.Write(output, $"MaybeExited<{GetSymbolicType(function.Signature.ReturnType)}> {returnTemp} = ");
                    break;
                case FunctionReturnConvention.RETURN_BDD:
                    returnTemp = context.FreshTempVar();
                    context.Write(output, $"Bdd {returnTemp} = ");
                    break;
                case FunctionReturnConvention.RETURN_VOID:
                    break;
            }

            context.Write(output, $"{context.GetNameForDecl(function)}({flowContext.pcScope.PathConstraintVar}, {CompilationContext.EffectCollectionVar}");

            if (function.CanChangeState ?? false)
                context.Write(output, ", outcome");

            else if (function.CanRaiseEvent ?? false)
                context.Write(output, ", outcome");


            for (int i = 0; i < args.Count(); i++)
            {
                var param = args.ElementAt(i);
                bool castToAny = GetSymbolicType(function.Signature.Parameters.ElementAt(i).Type, true) == "UnionVS" &&
                                                 GetSymbolicType(param.Type, true) != "UnionVS";
                context.Write(output, ", ");
                if (castToAny)
                {
                    context.Write(output, "new UnionVS(");
                }
                WriteExpr(context, output, flowContext.pcScope, param);
                if (castToAny) {
                    context.Write(output, ")");
                }
            }

            context.WriteLine(output, ");");

            switch (returnConvention)
            {
                case FunctionReturnConvention.RETURN_VALUE:
                    if (dest != null)
                        WriteWithLValueMutationContext(context, output, flowContext.pcScope, dest, false, (lhs) => context.WriteLine(output, $"{lhs} = {returnTemp};"));
                    break;
                case FunctionReturnConvention.RETURN_VOID:
                    Debug.Assert(dest == null);
                    break;
                case FunctionReturnConvention.RETURN_VALUE_OR_EXIT:
                    context.WriteLine(output, $"{flowContext.pcScope.PathConstraintVar} = {returnTemp}.getNewPc();");

                    // Conservatively set control flow flags.
                    // It is always safe to set these flags to true, because they exist only as a performance optimization.
                    // In the future, we may want to optimize this to be more precise.
                    SetFlagsForPossibleReturn(context, output, flowContext);

                    if (dest != null)
                        WriteWithLValueMutationContext(context, output, flowContext.pcScope, dest, false, (lhs) => context.WriteLine(output, $"{lhs} = {returnTemp}.getValue();"));
                    break;
                case FunctionReturnConvention.RETURN_BDD:
                    context.WriteLine(output, $"{flowContext.pcScope.PathConstraintVar} = {returnTemp};");

                    // Conservatively set control flow flags.
                    // It is always safe to set these flags to true, because they exist only as a performance optimization.
                    // In the future, we may want to optimize this to be more precise.
                    SetFlagsForPossibleReturn(context, output, flowContext);

                    Debug.Assert(dest == null);
                    break;
            }
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
                                context.WriteLine(output, $" = {mapTemp}.get({indexTemp});");
                            }
                            else
                            {
                                context.WriteLine(output, ";");
                            }

                            writeMutator(valueTemp);

                            context.WriteLine(output, $"{mapTemp} = {mapTemp}.put({indexTemp}, {valueTemp});");
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
                                context.WriteLine(output, $" = {seqTemp}.get({indexTemp});");
                            }
                            else
                            {
                                context.WriteLine(output, ";");
                            }

                            writeMutator(elementTemp);

                            context.WriteLine(output, $"{seqTemp} = {seqTemp}.set({indexTemp}, {elementTemp});");
                        }
                    );
                    break;
                case VariableAccessExpr variableAccessExpr:
                    var name = variableAccessExpr.Variable.Name;
                    var type = variableAccessExpr.Variable.Type;

                    var unguarded = CompilationContext.GetVar(name);

                    var guardedTemp = context.FreshTempVar();

                    context.Write(output, $"{GetSymbolicType(variableAccessExpr.Type)} {guardedTemp}");

                    if (needOrigValue)
                    {
                        context.WriteLine(output, $" = {unguarded}.guard({pcScope.PathConstraintVar});    ");
                    }
                    else
                    {
                        context.WriteLine(output, ";");
                    }

                    writeMutator(guardedTemp);

                    context.WriteLine(output,
                        $"{unguarded} = " +
                        $"{unguarded}.update({pcScope.PathConstraintVar}, " +
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
                    context.Write(output, $").apply(({lambdaTemp}) -> {UnOpToStr(unaryOpExpr.Operation)}{lambdaTemp})");
                    break;
                case BinOpExpr binOpExpr:
                    var isPrimitive = binOpExpr.Lhs.Type is PrimitiveType && binOpExpr.Rhs.Type is PrimitiveType;
                    var isEnum = binOpExpr.Lhs.Type is EnumType && binOpExpr.Rhs.Type is EnumType;
                    var isEquality = binOpExpr.Operation == BinOpType.Eq || binOpExpr.Operation == BinOpType.Neq;

                    if (!(isPrimitive || isEquality))
                    {
                        string str = $"lhs type: {binOpExpr.Lhs}, rhs type: {binOpExpr.Rhs}" ;
                        throw new NotImplementedException("Binary operations are currently only supported between primitive types and enums | " + str);
                    }

                    var lhsLambdaTemp = context.FreshTempVar();
                    var rhsLambdaTemp = context.FreshTempVar();

                    context.Write(output, "(");
                    WriteExpr(context, output, pcScope, binOpExpr.Lhs);
                    context.Write(output, ").apply2(");
                    if (binOpExpr.Rhs is NullLiteralExpr)
                        context.Write(output, $"{GetDefaultValueNoGuard(context, binOpExpr.Lhs.Type)}.guard(Bdd.constFalse())");
                    else WriteExpr(context, output, pcScope, binOpExpr.Rhs);
                    string lambda;
                    if (binOpExpr.Operation == BinOpType.Eq) lambda = $"{lhsLambdaTemp}.equals({rhsLambdaTemp}))";
                    else if (binOpExpr.Operation == BinOpType.Neq) lambda = $"!{lhsLambdaTemp}.equals({rhsLambdaTemp}))";
                    else lambda = $"{lhsLambdaTemp} {BinOpToStr(binOpExpr.Operation)} {rhsLambdaTemp})";
                    context.Write(
                        output,
                        $", ({lhsLambdaTemp}, {rhsLambdaTemp}) -> " +
                        lambda
                    );

                    break;
                case BoolLiteralExpr boolLiteralExpr:
                    {
                        var unguarded = $"new { GetSymbolicType(PrimitiveType.Bool) }" + $"({boolLiteralExpr.Value})".ToLower();
                        var guarded = $"{unguarded}.guard({pcScope.PathConstraintVar})";
                        context.Write(output, guarded);
                        break;
                    }
                case CastExpr castExpr:
                    if (castExpr.SubExpr is NullLiteralExpr)
                    {
                        context.Write(output,  GetDefaultValue(context, pcScope, castExpr.Type));
                    } else
                    {
                        string prefix = GetInlineCastPrefix(castExpr.SubExpr.Type, castExpr.Type, context, pcScope);
                        context.Write(output, prefix);
                        WriteExpr(context, output, pcScope, castExpr.SubExpr);
                        if (prefix != "") context.Write(output, ")");
                    }
                    break;
                case DefaultExpr defaultExpr:
                    context.Write(output, GetDefaultValue(context, pcScope, defaultExpr.Type));
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                    {
                        var unguarded = $"new { GetSymbolicType(PrimitiveType.Float) }({floatLiteralExpr.Value}f)";
                        var guarded = $"{unguarded}.guard({pcScope.PathConstraintVar})";
                        context.Write(output, guarded);
                        break;
                    }
                case IntLiteralExpr intLiteralExpr:
                    {
                        var unguarded = $"new { GetSymbolicType(PrimitiveType.Int) }({intLiteralExpr.Value})";
                        var guarded = $"{unguarded}.guard({pcScope.PathConstraintVar})";
                        context.Write(output, guarded);
                        break;
                    }
                case MapAccessExpr mapAccessExpr:
                    WriteExpr(context, output, pcScope, mapAccessExpr.MapExpr);
                    context.Write(output, ".get(");
                    WriteExpr(context, output, pcScope, mapAccessExpr.IndexExpr);
                    context.Write(output, ")");
                    break;
                case SeqAccessExpr seqAccessExpr:
                    WriteExpr(context, output, pcScope, seqAccessExpr.SeqExpr);
                    context.Write(output, ".get(");
                    WriteExpr(context, output, pcScope, seqAccessExpr.IndexExpr);
                    context.Write(output, ")");
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    context.Write(output, $"({GetSymbolicType(namedTupleAccessExpr.Type)})(");
                    WriteExpr(context, output, pcScope, namedTupleAccessExpr.SubExpr);
                    context.Write(output, $").getField(\"{namedTupleAccessExpr.FieldName}\")");
                    break;
                case ThisRefExpr thisRefExpr:
                    context.Write(
                        output,
                        $"new PrimVS<Machine>(this).guard(" +
                        $"{pcScope.PathConstraintVar})");
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
                        var guarded = $"{unguarded}.guard({pcScope.PathConstraintVar})";
                        context.Write(output, guarded);
                        break;
                    }
                case EventRefExpr eventRefExpr:
                    {
                        var unguarded = $"new { GetSymbolicType(PrimitiveType.Event) }({context.GetNameForDecl(eventRefExpr.Value)})";
                        var guarded = $"{unguarded}.guard({pcScope.PathConstraintVar})";
                        context.Write(output, guarded);
                        break;
                    }
                case VariableAccessExpr variableAccessExpr:
                    context.Write(output,
                        $"{CompilationContext.GetVar(variableAccessExpr.Variable.Name)}.guard(" +
                        $"{pcScope.PathConstraintVar})");
                    break;
                case LinearAccessRefExpr linearAccessExpr:
                    context.Write(output,
                        $"{CompilationContext.GetVar(linearAccessExpr.Variable.Name)}.guard(" +
                        $"{pcScope.PathConstraintVar})");
                    break;
                case FunCallExpr funCallExpr:
                    throw new InvalidOperationException("Compilation of call expressions should be handled as part of assignment statements");
                case ContainsExpr containsExpr:
                    var isMap = PLanguageType.TypeIsOfKind(containsExpr.Collection.Type, TypeKind.Map);

                    WriteExpr(context, output, pcScope, containsExpr.Collection);

                    if (isMap)
                        context.Write(output, ".containsKey(");
                    else
                        context.Write(output, ".contains(");

                    WriteExpr(context, output, pcScope, containsExpr.Item);
                    context.Write(output, ")");
                    break;
                case CtorExpr ctorExpr:
                    WriteCtorExpr(context, output, pcScope, ctorExpr.Interface, ctorExpr.Arguments);
                    break;
                case NondetExpr _:
                case FairNondetExpr _:
                    context.Write(output, $"{CompilationContext.SchedulerVar}.getNextBoolean({pcScope.PathConstraintVar})");
                    break;
                case SizeofExpr sizeOfExpr:
                    WriteExpr(context, output, pcScope, sizeOfExpr.Expr);
                    context.Write(output, ".size()");
                    break;
                default:
                    context.Write(output, $"/* Skipping expr '{expr.GetType().Name}' */");
                    break;
            }
        }

        private void WriteCtorExpr(CompilationContext context, StringWriter output, PathConstraintScope pcScope, Interface ctorInterface, IReadOnlyList<IPExpr> ctorArguments)
        {
            // TODO: Is it safe to take an interface's name and treat it as if it were a machine's name?
            context.Write(
                output,
                $"{CompilationContext.EffectCollectionVar}.create(" +
                $"{pcScope.PathConstraintVar}, " +
                $"{CompilationContext.SchedulerVar}, " +
                $"{context.GetNameForDecl(ctorInterface)}.class, ");

            if (ctorArguments.Count == 1)
            {
                Debug.Assert(ctorArguments.Count == 1);
                context.Write(output, "new UnionVS (");
                WriteExpr(context, output, pcScope, ctorArguments[0]);
                context.Write(output, "), ");
            }
            else if (ctorArguments.Count > 1)
            {
                context.Write(output, "new UnionVS (");
                context.Write(output, "new TupleVS (");
                for (int i = 0; i < ctorArguments.Count; i++)
                {
                    WriteExpr(context, output, pcScope, ctorArguments[i]);
                    if (i != ctorArguments.Count - 1) {
                        context.Write(output, ", ");
                    }
                }
                context.Write(output, ")), ");
            }

            context.Write(
                output,
                $"(i) -> new {context.GetNameForDecl(ctorInterface)}(i))");
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
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return "PrimVS<EventName>";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return "UnionVS";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                case PermissionType _:
                    return "PrimVS<Machine>";
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
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    unguarded = $"new {GetSymbolicType(type)}({CompilationContext.NullEventName})";
                    break;
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    unguarded = $"new {GetSymbolicType(type)}()";
                    break;
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                case PermissionType _:
                    unguarded = $"new {GetSymbolicType(type)}()";
                    break;
                case SequenceType _:
                    unguarded = $"new {GetSymbolicType(type)}(Bdd.constTrue())";
                    break;
                case MapType _:
                    unguarded = $"new {GetSymbolicType(type)}(Bdd.constTrue())";
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
            var guarded = $"{unguarded}.guard({pcScope.PathConstraintVar})";

            return guarded;
        }

        private string GetDefaultValueNoGuard(CompilationContext context, PLanguageType type) {
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return $"new {GetSymbolicType(type)}(false)";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return $"new {GetSymbolicType(type)}(0)";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return $"new {GetSymbolicType(type)}(0.0f)";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return $"new {GetSymbolicType(type)}({CompilationContext.NullEventName})";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return $"new {GetSymbolicType(type)}()";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                case PermissionType _:
                    return $"new PrimVS<Machine>()";
                case SequenceType _:
                    return $"new {GetSymbolicType(type)}(Bdd.constTrue())";
                case MapType _:
                    return $"new {GetSymbolicType(type)}(Bdd.constTrue())";
                case NamedTupleType namedTupleType:
                    {
                        var allFieldDefaults = new List<string>();
                        foreach (var field in namedTupleType.Fields)
                        {
                            var fieldDefault = GetDefaultValueNoGuard(context, field.Type);
                            allFieldDefaults.Add($"\"{field.Name}\", {fieldDefault}");
                        }
                        return $"new {GetSymbolicType(type)}({string.Join(", ", allFieldDefaults)})";
                    }
                case TupleType tupleType:
                    {
                        var allFieldDefaults = new List<string>();
                        foreach (var field in tupleType.Types)
                        {
                            var fieldDefault = GetDefaultValueNoGuard(context, field);
                            allFieldDefaults.Add(fieldDefault);
                        }
                        return $"new {GetSymbolicType(type)}({string.Join(", ", allFieldDefaults)})";
                    }
                default:
                    throw new NotImplementedException($"Default value for symbolic type '{type.OriginalRepresentation}' not supported");
            }
        }

        private void WriteSourcePrologue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "import symbolicp.*;");
            context.WriteLine(output, "import symbolicp.bdd.*;");
            context.WriteLine(output, "import symbolicp.vs.*;");
            context.WriteLine(output, "import symbolicp.runtime.*;");
            context.WriteLine(output, "import symbolicp.run.*;");
            context.WriteLine(output);
            context.WriteLine(output, $"public class {context.MainClassName} implements Program {{");
            context.WriteLine(output);
            context.WriteLine(output, $"public static Scheduler {CompilationContext.SchedulerVar};");
            context.WriteLine(output);
            context.WriteLine(output, "@Override");
            context.WriteLine(output, $"public void setScheduler (Scheduler s) {{ this.{CompilationContext.SchedulerVar} = s; }}");
            context.WriteLine(output);
        }

        private void WriteMainDriver(CompilationContext context, StringWriter output, Scope globalScope)
        {
            // TODO: Determine how main machine should be selected.  Should the 'main' method even
            // be generated from the P program, or should it be provided externally?
            Machine mainMachine = null;
            foreach (Machine machine in globalScope.Machines)
            {
                if (machine.Name == "Main")
                {
                    if (mainMachine != null)
                        throw new NotImplementedException("Cannot have multiple main machines.");

                    mainMachine = machine;
                }
            }

            if (mainMachine != null)
            {
                context.WriteLine(output, $"private static Machine start = new {context.GetNameForDecl(mainMachine)}(0);");
                context.WriteLine(output);
                context.WriteLine(output, "@Override");
                context.WriteLine(output, "public Machine getStart() { return start; }");
                context.WriteLine(output);
            }
        }

        private void WriteSourceEpilogue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "}");
        }
    }
}
