using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{
    /// <summary>
    /// Visits statement parse nodes and produces typed <see cref="IPStmt"/> AST.
    ///
    /// Multi-error type checking (Phase 2): every error site reports through
    /// <c>handler.Diagnostics</c> and either continues (when the statement can
    /// still be built from what we have) or returns <see cref="NoStmt"/> as a
    /// placeholder. In strict mode (default), Report re-throws so behavior is
    /// unchanged. See ExprVisitor's class doc for the full cascade-suppression
    /// strategy and how <see cref="ErrorExpr"/> propagates from sub-expressions.
    ///
    /// Recovery conventions:
    ///   - Type mismatch on a sub-expression (e.g. non-bool condition) →
    ///     Report and continue building the statement; downstream code uses
    ///     the typed expression as-is.
    ///   - Missing declaration (variable, interface, event, function, state) →
    ///     Report and return <see cref="NoStmt"/>. Without the resolved decl
    ///     there's nothing meaningful to construct.
    ///   - Sub-expression already errored (Type is ErrorType) → skip the
    ///     compatibility check that would have produced a redundant diagnostic.
    /// </summary>
    public class StatementVisitor : PParserBaseVisitor<IPStmt>
    {
        private readonly ExprVisitor exprVisitor;
        private readonly ICompilerConfiguration config;
        private readonly ITranslationErrorHandler handler;
        private readonly Machine machine;
        private readonly Function method;
        private readonly Scope table;

        public StatementVisitor(ICompilerConfiguration config, Machine machine, Function method)
        {
            this.config = config;
            this.handler = this.config.Handler;
            this.machine = machine;
            this.method = method;
            table = method.Scope;
            exprVisitor = new ExprVisitor(method, config.Handler, config.OutputLanguages.Contains(CompilerOutput.PVerifier));
        }

        public override IPStmt VisitFunctionBody(PParser.FunctionBodyContext context)
        {
            var statements = context.statement().Select(Visit).ToList();
            return new CompoundStmt(context, statements);
        }

        public override IPStmt VisitCompoundStmt(PParser.CompoundStmtContext context)
        {
            var statements = context.statement().Select(Visit).ToList();
            return new CompoundStmt(context, statements);
        }

        public override IPStmt VisitAssertStmt(PParser.AssertStmtContext context)
        {
            var assertion = exprVisitor.Visit(context.assertion);
            if (assertion.Type is not ErrorType && !PrimitiveType.Bool.IsSameTypeAs(assertion.Type))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(context.assertion, assertion.Type, PrimitiveType.Bool));
            }
            IPExpr assertMessage = new StringExpr(context, @$"{config.LocationResolver.GetLocation(context).ToString().Replace(@"\", @"\\")}",new List<IPExpr>());
            if (context.message != null)
            {
                var message = exprVisitor.Visit(context.message);
                if (message.Type is not ErrorType && !message.Type.IsSameTypeAs(PrimitiveType.String))
                {
                    handler.Diagnostics.Report(handler.TypeMismatch(context.message, message.Type, PrimitiveType.String));
                }

                assertMessage = new StringExpr(message.SourceLocation, "{0} {1}",new List<IPExpr>() {assertMessage,
                    message});
            }

            return new AssertStmt(context, assertion, assertMessage);
        }


        public override IPStmt VisitAssumeStmt(PParser.AssumeStmtContext context)
        {
            var assumption = exprVisitor.Visit(context.assumption);
            if (assumption.Type is not ErrorType && !PrimitiveType.Bool.IsSameTypeAs(assumption.Type))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(context.assumption, assumption.Type, PrimitiveType.Bool));
            }
            IPExpr assertMessage = new StringExpr(context, @$"{config.LocationResolver.GetLocation(context).ToString().Replace(@"\", @"\\")}",new List<IPExpr>());
            if (context.message != null)
            {
                var message = exprVisitor.Visit(context.message);
                if (message.Type is not ErrorType && !message.Type.IsSameTypeAs(PrimitiveType.String))
                {
                    handler.Diagnostics.Report(handler.TypeMismatch(context.message, message.Type, PrimitiveType.String));
                }

                assertMessage = new StringExpr(message.SourceLocation, "{0} {1}",new List<IPExpr>() {assertMessage,
                    message});
            }

            return new AssumeStmt(context, assumption, assertMessage);
        }

        public override IPStmt VisitPrintStmt(PParser.PrintStmtContext context)
        {
            var message = exprVisitor.Visit(context.message);
            if (message.Type is not ErrorType && !message.Type.IsSameTypeAs(PrimitiveType.String))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(context.message, message.Type, PrimitiveType.String));
            }
            return new PrintStmt(context, message);
        }

        public override IPStmt VisitReturnStmt(PParser.ReturnStmtContext context)
        {
            var returnValue = context.expr() == null ? null : exprVisitor.Visit(context.expr());
            var returnType = returnValue?.Type ?? PrimitiveType.Null;
            // CheckAssignable suppresses if returnType is ErrorType (upstream
            // expression error already reported).
            TypeCheckingUtils.CheckAssignable(handler, context, method.Signature.ReturnType, returnType);

            return new ReturnStmt(context, returnValue);
        }

        public override IPStmt VisitBreakStmt(PParser.BreakStmtContext context)
        {
            return new BreakStmt(context);
        }

        public override IPStmt VisitContinueStmt(PParser.ContinueStmtContext context)
        {
            return new ContinueStmt(context);
        }

        public override IPStmt VisitAssignStmt(PParser.AssignStmtContext context)
        {
            var variable = exprVisitor.Visit(context.lvalue());
            var value = exprVisitor.Visit(context.rvalue());

            // If this is a value assignment, we just need subtyping. Helper
            // suppresses when either side has ErrorType (lvalue rule).
            TypeCheckingUtils.CheckAssignable(handler, context.rvalue(), variable.Type, value);

            return new AssignStmt(context, variable, value);
        }

        public override IPStmt VisitAddStmt(PParser.AddStmtContext context)
        {
            var variable = exprVisitor.Visit(context.lvalue());
            var value = exprVisitor.Visit(context.rvalue());

            // If the lvalue already errored, downstream container-shape check
            // can't say anything useful; emit one statement and bail.
            if (variable.Type is ErrorType)
            {
                return new AddStmt(context, variable, value);
            }

            PLanguageType expectedValueType;

            switch (variable.Type.Canonicalize())
            {
                case SetType setType:
                    expectedValueType = setType.ElementType;
                    break;

                default:
                    handler.Diagnostics.Report(handler.TypeMismatch(variable, TypeKind.Set));
                    return new AddStmt(context, variable, value);
            }

            TypeCheckingUtils.CheckAssignable(handler, context.rvalue(), expectedValueType, value);

            return new AddStmt(context, variable, value);
        }

        public override IPStmt VisitInsertStmt(PParser.InsertStmtContext context)
        {
            var variable = exprVisitor.Visit(context.lvalue());
            var index = exprVisitor.Visit(context.expr());
            var value = exprVisitor.Visit(context.rvalue());

            if (variable.Type is ErrorType)
            {
                return new InsertStmt(context, variable, index, value);
            }

            PLanguageType expectedKeyType;
            PLanguageType expectedValueType;

            switch (variable.Type.Canonicalize())
            {
                case SequenceType sequenceType:
                    expectedKeyType = PrimitiveType.Int;
                    expectedValueType = sequenceType.ElementType;
                    break;

                case MapType mapType:
                    expectedKeyType = mapType.KeyType;
                    expectedValueType = mapType.ValueType;
                    break;

                default:
                    handler.Diagnostics.Report(handler.TypeMismatch(variable, TypeKind.Sequence, TypeKind.Map));
                    return new InsertStmt(context, variable, index, value);
            }

            TypeCheckingUtils.CheckAssignable(handler, context.rvalue(), expectedKeyType, index);
            TypeCheckingUtils.CheckAssignable(handler, context.rvalue(), expectedValueType, value);

            return new InsertStmt(context, variable, index, value);
        }

        public override IPStmt VisitRemoveStmt(PParser.RemoveStmtContext context)
        {
            var variable = exprVisitor.Visit(context.lvalue());
            var value = exprVisitor.Visit(context.expr());

            if (variable.Type is ErrorType)
            {
                return new RemoveStmt(context, variable, value);
            }

            if (PLanguageType.TypeIsOfKind(variable.Type, TypeKind.Sequence))
            {
                TypeCheckingUtils.CheckAssignable(handler, context.expr(), PrimitiveType.Int, value);
            }
            else if (PLanguageType.TypeIsOfKind(variable.Type, TypeKind.Map))
            {
                var map = (MapType)variable.Type.Canonicalize();
                TypeCheckingUtils.CheckAssignable(handler, context.expr(), map.KeyType, value);
            }
            else if (PLanguageType.TypeIsOfKind(variable.Type, TypeKind.Set))
            {
                var set = (SetType)variable.Type.Canonicalize();
                TypeCheckingUtils.CheckAssignable(handler, context.expr(), set.ElementType, value);
            }
            else
            {
                handler.Diagnostics.Report(handler.TypeMismatch(variable, TypeKind.Sequence, TypeKind.Map));
            }

            return new RemoveStmt(context, variable, value);
        }

        public override IPStmt VisitWhileStmt(PParser.WhileStmtContext context)
        {
            var condition = exprVisitor.Visit(context.expr());
            if (condition.Type is not ErrorType && !PrimitiveType.Bool.IsSameTypeAs(condition.Type))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(context.expr(), condition.Type, PrimitiveType.Bool));
            }

            var body = Visit(context.statement());
            return new WhileStmt(context, condition, body);
        }

        public override IPStmt VisitForeachStmt(PParser.ForeachStmtContext context)
        {
            var varName = context.item.GetText();
            if (!table.Lookup(varName, out Variable var))
            {
                handler.Diagnostics.Report(handler.MissingDeclaration(context.item, "foreach iterator variable", varName));
                // Still visit children so their internal errors surface in
                // collecting mode — including the loop invariants, which can
                // hold their own type errors independent of the missing var.
                exprVisitor.Visit(context.collection);
                Visit(context.statement());
                foreach (var inv in context._invariants)
                {
                    exprVisitor.Visit(inv);
                }
                return new NoStmt(context);
            }
            var collection = exprVisitor.Visit(context.collection);

            PLanguageType expectedItemType;
            if (collection.Type is ErrorType)
            {
                expectedItemType = ErrorType.Instance;
            }
            else
            {
                switch (collection.Type.Canonicalize())
                {
                    case SetType setType:
                        expectedItemType = setType.ElementType;
                        break;
                    case SequenceType seqType:
                        expectedItemType = seqType.ElementType;
                        break;
                    default:
                        handler.Diagnostics.Report(handler.TypeMismatch(collection, TypeKind.Set, TypeKind.Sequence));
                        expectedItemType = ErrorType.Instance;
                        break;
                }
            }

            var itemType = var.Type;
            if (expectedItemType is not ErrorType && itemType is not ErrorType
                && (!expectedItemType.IsSameTypeAs(itemType) || !expectedItemType.IsAssignableFrom(itemType)))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(context.item, itemType, expectedItemType));
            }

            var body = Visit(context.statement());

            var invs = context._invariants.Select(exprVisitor.Visit).ToList();

            return new ForeachStmt(context, var, collection, body, invs);
        }

        public override IPStmt VisitIfStmt(PParser.IfStmtContext context)
        {
            var condition = exprVisitor.Visit(context.expr());
            if (condition.Type is not ErrorType && !PrimitiveType.Bool.IsSameTypeAs(condition.Type))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(context.expr(), condition.Type, PrimitiveType.Bool));
            }

            var thenBody = Visit(context.thenBranch);
            var elseBody = context.elseBranch == null ? new NoStmt(context) : Visit(context.elseBranch);
            return new IfStmt(context, condition, thenBody, elseBody);
        }

        public override IPStmt VisitCtorStmt(PParser.CtorStmtContext context)
        {
            var interfaceName = context.iden().GetText();
            // Always visit arguments so their internal errors surface.
            var args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToList();

            if (!table.Lookup(interfaceName, out Interface targetInterface))
            {
                handler.Diagnostics.Report(handler.MissingDeclaration(context.iden(), "interface", interfaceName));
                return new NoStmt(context);
            }

            TypeCheckingUtils.ValidatePayloadTypes(handler, context, targetInterface.PayloadType, args);
            method.CanCreate = true;
            return new CtorStmt(context, targetInterface, args);
        }

        public override IPStmt VisitFunCallStmt(PParser.FunCallStmtContext context)
        {
            var funName = context.fun.GetText();
            var argsList = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToList();
            if (!table.Lookup(funName, out Function fun))
            {
                handler.Diagnostics.Report(handler.MissingDeclaration(context.fun, "function or function prototype", funName));
                return new NoStmt(context);
            }

            if (fun.Signature.Parameters.Count != argsList.Count)
            {
                handler.Diagnostics.Report(handler.IncorrectArgumentCount((ParserRuleContext)context.rvalueList() ?? context,
                    argsList.Count,
                    fun.Signature.Parameters.Count));
                return new FunCallStmt(context, fun, argsList);
            }

            foreach (var pair in fun.Signature.Parameters.Zip(argsList, Tuple.Create))
            {
                TypeCheckingUtils.CheckArgument(handler, context, pair.Item1.Type, pair.Item2);
            }

            method.AddCallee(fun);
            return new FunCallStmt(context, fun, argsList);
        }

        public override IPStmt VisitRaiseStmt(PParser.RaiseStmtContext context)
        {
            if (!method.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                handler.Diagnostics.Report(handler.RaiseEventInNonVoidFunction(context));
                // Still visit children to surface their errors.
                exprVisitor.Visit(context.expr());
                TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();
                return new NoStmt(context);
            }

            var evtExpr = exprVisitor.Visit(context.expr());
            if (evtExpr.Type is ErrorType)
            {
                method.CanRaiseEvent = true;
                TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();
                return new RaiseStmt(context, evtExpr, Array.Empty<IPExpr>());
            }

            if (IsDefinitelyNullEvent(evtExpr))
            {
                handler.Diagnostics.Report(handler.EmittedNullEvent(evtExpr));
            }
            else if (!PrimitiveType.Event.IsAssignableFrom(evtExpr.Type))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(context.expr(), evtExpr.Type, PrimitiveType.Event));
            }

            method.CanRaiseEvent = true;

            var args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();
            if (evtExpr is EventRefExpr eventRef)
            {
                TypeCheckingUtils.ValidatePayloadTypes(handler, context, eventRef.Value.PayloadType, args);
            }

            return new RaiseStmt(context, evtExpr, args);
        }

        public override IPStmt VisitSendStmt(PParser.SendStmtContext context)
        {
            if (machine?.IsSpec == true)
            {
                handler.Diagnostics.Report(handler.IllegalMonitorOperation(context, context.SEND().Symbol, machine));
                // Still visit children to surface their errors.
                exprVisitor.Visit(context.machine);
                exprVisitor.Visit(context.@event);
                TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();
                return new NoStmt(context);
            }

            var machineExpr = exprVisitor.Visit(context.machine);
            if (machineExpr.Type is not ErrorType && !PrimitiveType.Machine.IsAssignableFrom(machineExpr.Type))
            {
                handler.Diagnostics.Report(handler.TypeMismatch(context.machine, machineExpr.Type, PrimitiveType.Machine));
            }

            var evtExpr = exprVisitor.Visit(context.@event);
            if (evtExpr.Type is not ErrorType)
            {
                if (IsDefinitelyNullEvent(evtExpr))
                {
                    handler.Diagnostics.Report(handler.EmittedNullEvent(evtExpr));
                }
                else if (!PrimitiveType.Event.IsAssignableFrom(evtExpr.Type))
                {
                    handler.Diagnostics.Report(handler.TypeMismatch(context.@event, evtExpr.Type, PrimitiveType.Event));
                }
            }

            var args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();

            if (evtExpr is EventRefExpr eventRef)
            {
                TypeCheckingUtils.ValidatePayloadTypes(handler, context, eventRef.Value.PayloadType, args);
            }

            method.CanSend = true;

            return new SendStmt(context, machineExpr, evtExpr, args);
        }

        private static bool IsDefinitelyNullEvent(IPExpr evtExpr)
        {
            return evtExpr is NullLiteralExpr || evtExpr is EventRefExpr evtRef && evtRef.Value.Name.Equals("null");
        }

        public override IPStmt VisitAnnounceStmt(PParser.AnnounceStmtContext context)
        {
            if (machine?.IsSpec == true)
            {
                handler.Diagnostics.Report(handler.IllegalMonitorOperation(context, context.ANNOUNCE().Symbol, machine));
                exprVisitor.Visit(context.expr());
                TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();
                return new NoStmt(context);
            }

            var evtExpr = exprVisitor.Visit(context.expr());
            if (evtExpr.Type is not ErrorType)
            {
                if (IsDefinitelyNullEvent(evtExpr))
                {
                    handler.Diagnostics.Report(handler.EmittedNullEvent(evtExpr));
                }
                else if (!PrimitiveType.Event.IsAssignableFrom(evtExpr.Type))
                {
                    handler.Diagnostics.Report(handler.TypeMismatch(context.expr(), evtExpr.Type, PrimitiveType.Event));
                }
            }

            method.CanSend = true;

            var args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToList();

            if (evtExpr is EventRefExpr eventRef)
            {
                TypeCheckingUtils.ValidatePayloadTypes(handler, context, eventRef.Value.PayloadType, args);
            }

            return new AnnounceStmt(context, evtExpr, args.Count == 0 ? null : args[0]);
        }

        public override IPStmt VisitGotoStmt(PParser.GotoStmtContext context)
        {
            if (!method.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                handler.Diagnostics.Report(handler.ChangeStateInNonVoidFunction(context));
                TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();
                return new NoStmt(context);
            }

            var stateNameContext = context.stateName();
            var stateName = stateNameContext.state.GetText();
            IStateContainer current = machine;

            var state = current?.GetState(stateName);
            if (state == null)
            {
                handler.Diagnostics.Report(handler.MissingDeclaration(stateNameContext.state, "state", stateName));
                TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();
                return new NoStmt(context);
            }

            var expectedType =
                state.Entry?.Signature.ParameterTypes.ElementAtOrDefault(0) ?? PrimitiveType.Null;
            var rvaluesList = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();

            var expectedArgs = state.Entry?.Signature.Parameters.Count() ?? 0;
            if (rvaluesList.Length != expectedArgs)
            {
                handler.Diagnostics.Report(handler.IncorrectArgumentCount(context, rvaluesList.Length, expectedArgs));
                return new GotoStmt(context, state, null);
            }

            IPExpr payload;
            if (rvaluesList.Length == 0)
            {
                payload = null;
            }
            else if (rvaluesList.Length == 1)
            {
                payload = rvaluesList[0];
            }
            else
            {
                payload = new UnnamedTupleExpr(context, rvaluesList);
            }

            var payloadType = payload?.Type ?? PrimitiveType.Null;
            TypeCheckingUtils.CheckAssignable(handler, context, expectedType, payloadType);

            method.CanChangeState = true;
            return new GotoStmt(context, state, payload);
        }

        public override IPStmt VisitReceiveStmt(PParser.ReceiveStmtContext context)
        {
            if (machine?.IsSpec == true)
            {
                handler.Diagnostics.Report(handler.IllegalMonitorOperation(context, context.RECEIVE().Symbol, machine));
                // Surface ALL nested errors even though the receive itself is
                // illegal on a spec machine: handler body errors (mirrors what
                // Send/Announce/Raise do for child expressions) AND undeclared
                // event references in the event lists (Copilot's first round
                // covered the bodies; multi-agent audit caught that event-id
                // lookups were still being skipped).
                foreach (var caseContext in context.recvCase())
                {
                    foreach (var eventIdContext in caseContext.eventList().eventId())
                    {
                        if (!table.Lookup(eventIdContext.GetText(), out Event _))
                        {
                            handler.Diagnostics.Report(handler.MissingDeclaration(eventIdContext, "event", eventIdContext.GetText()));
                        }
                    }

                    var recvHandler = new Function(caseContext.anonEventHandler())
                    {
                        Scope = table.MakeChildScope(),
                        Owner = method.Owner,
                        ParentFunction = method,
                        Role = FunctionRole.ReceiveHandler
                    };
                    var param = caseContext.anonEventHandler().funParam();
                    if (param != null)
                    {
                        var paramVar = recvHandler.Scope.Put(param.name.GetText(), param, VariableRole.Param);
                        paramVar.Type = TypeResolver.ResolveType(param.type(), recvHandler.Scope, handler);
                        recvHandler.Signature.Parameters.Add(paramVar);
                    }
                    FunctionBodyVisitor.PopulateMethod(config, recvHandler);
                }
                return new NoStmt(context);
            }

            var cases = new Dictionary<Event, Function>();
            foreach (var caseContext in context.recvCase())
            {


                foreach (var eventIdContext in caseContext.eventList().eventId())
                {
                    var recvHandler =
                        new Function(caseContext.anonEventHandler())
                        {
                            Scope = table.MakeChildScope(),
                            Owner = method.Owner,
                            ParentFunction = method,
                            Role = FunctionRole.ReceiveHandler
                        };

                    var param = caseContext.anonEventHandler().funParam();
                    if (param != null)
                    {
                        var paramVar = recvHandler.Scope.Put(param.name.GetText(), param, VariableRole.Param);
                        paramVar.Type = TypeResolver.ResolveType(param.type(), recvHandler.Scope, handler);
                        recvHandler.Signature.Parameters.Add(paramVar);
                    }

                    FunctionBodyVisitor.PopulateMethod(config, recvHandler);

                    if (!table.Lookup(eventIdContext.GetText(), out Event pEvent))
                    {
                        handler.Diagnostics.Report(handler.MissingDeclaration(eventIdContext, "event", eventIdContext.GetText()));
                        continue;
                    }

                    if (cases.ContainsKey(pEvent))
                    {
                        handler.Diagnostics.Report(handler.DuplicateReceiveCase(eventIdContext, pEvent));
                        continue;
                    }

                    var expectedType =
                        recvHandler.Signature.ParameterTypes.ElementAtOrDefault(0) ?? PrimitiveType.Null;
                    TypeCheckingUtils.CheckAssignable(handler, caseContext.anonEventHandler(), expectedType, pEvent.PayloadType);

                    if (recvHandler.CanChangeState)
                    {
                        if (!method.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
                        {
                            handler.Diagnostics.Report(handler.ChangeStateInNonVoidFunction(context));
                        }
                        else
                        {
                            method.CanChangeState = true;
                        }
                    }

                    if (recvHandler.CanRaiseEvent)
                    {
                        if (!method.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
                        {
                            handler.Diagnostics.Report(handler.RaiseEventInNonVoidFunction(context));
                        }
                        else
                        {
                            method.CanRaiseEvent = true;
                        }
                    }

                    foreach (var callee in recvHandler.Callees)
                    {
                        method.AddCallee(callee);
                    }

                    cases.Add(pEvent, recvHandler);
                }
            }

            method.CanReceive = true;
            return new ReceiveStmt(context, cases);
        }

        public override IPStmt VisitNoStmt(PParser.NoStmtContext context)
        {
            return new NoStmt(context);
        }
    }
}
