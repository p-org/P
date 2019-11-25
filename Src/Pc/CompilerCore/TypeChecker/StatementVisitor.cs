using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plang.Compiler.TypeChecker
{
    public class StatementVisitor : PParserBaseVisitor<IPStmt>
    {
        private readonly ExprVisitor exprVisitor;
        private readonly ITranslationErrorHandler handler;
        private readonly Machine machine;
        private readonly Function method;
        private readonly Scope table;

        public StatementVisitor(ITranslationErrorHandler handler, Machine machine, Function method)
        {
            this.handler = handler;
            this.machine = machine;
            this.method = method;
            table = method.Scope;
            exprVisitor = new ExprVisitor(method, handler);
        }

        public override IPStmt VisitFunctionBody(PParser.FunctionBodyContext context)
        {
            List<IPStmt> statements = context.statement().Select(Visit).ToList();
            return new CompoundStmt(context, statements);
        }

        public override IPStmt VisitCompoundStmt(PParser.CompoundStmtContext context)
        {
            List<IPStmt> statements = context.statement().Select(Visit).ToList();
            return new CompoundStmt(context, statements);
        }

        public override IPStmt VisitPopStmt(PParser.PopStmtContext context)
        {
            if (machine?.IsSpec == true)
            {
                throw handler.IllegalMonitorOperation(context, context.POP().Symbol, machine);
            }

            if (!method.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                throw handler.PopInNonVoidFunction(context);
            }

            method.CanChangeState = true;
            if (method.Role.HasFlag(FunctionRole.TransitionFunction))
            {
                throw handler.ChangedStateMidTransition(context, method);
            }

            return new PopStmt(context);
        }

        public override IPStmt VisitAssertStmt(PParser.AssertStmtContext context)
        {
            IPExpr assertion = exprVisitor.Visit(context.expr());
            if (!PrimitiveType.Bool.IsSameTypeAs(assertion.Type))
            {
                throw handler.TypeMismatch(context.expr(), assertion.Type, PrimitiveType.Bool);
            }

            string message = context.StringLiteral()?.GetText() ?? "";
            if (message.StartsWith("\""))
            {
                message = message.Substring(1, message.Length - 2);
            }

            return new AssertStmt(context, assertion, message);
        }

        public override IPStmt VisitPrintStmt(PParser.PrintStmtContext context)
        {
            string message = context.StringLiteral().GetText();
            message = message.Substring(1, message.Length - 2); // strip beginning / end double quote
            int numNecessaryArgs = TypeCheckingUtils.PrintStmtNumArgs(message);
            if (numNecessaryArgs == -1)
            {
                throw handler.InvalidPrintFormat(context, context.StringLiteral().Symbol);
            }

            List<IPExpr> args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToList();
            foreach (IPExpr arg in args)
            {
                if (arg is LinearAccessRefExpr)
                {
                    throw handler.PrintStmtLinearArgument(arg.SourceLocation);
                }
            }

            if (args.Count != numNecessaryArgs)
            {
                throw handler.IncorrectArgumentCount(context, args.Count, numNecessaryArgs);
            }

            return new PrintStmt(context, message, args);
        }

        public override IPStmt VisitStringAssignStmt(PParser.StringAssignStmtContext context)
        {

            IPExpr variable = exprVisitor.Visit(context.lvalue());
            string baseString = context.StringLiteral().GetText();
            baseString = baseString.Substring(1, baseString.Length - 2); // strip beginning / end double quote
            int numNecessaryArgs = TypeCheckingUtils.PrintStmtNumArgs(baseString);
            if (numNecessaryArgs == -1)
            {
                throw handler.InvalidStringAssignFormat(context, context.StringLiteral().Symbol);
            }

            List<IPExpr> args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToList();
            foreach (IPExpr arg in args)
            {
                if (arg is LinearAccessRefExpr)
                {
                    throw handler.StringAssignStmtLinearArgument(arg.SourceLocation);
                }
            }

            if (args.Count != numNecessaryArgs)
            {
                throw handler.IncorrectArgumentCount(context, args.Count, numNecessaryArgs);
            }

            if (variable.Type != PrimitiveType.String) {
                throw handler.TypeMismatch(context, variable.Type, PrimitiveType.String);
            }

            return new StringAssignStmt(context, variable, baseString, args);
        }

        public override IPStmt VisitReturnStmt(PParser.ReturnStmtContext context)
        {
            IPExpr returnValue = context.expr() == null ? null : exprVisitor.Visit(context.expr());
            PLanguageType returnType = returnValue?.Type ?? PrimitiveType.Null;
            if (!method.Signature.ReturnType.IsAssignableFrom(returnType))
            {
                throw handler.TypeMismatch(context, returnType, method.Signature.ReturnType);
            }

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
            IPExpr variable = exprVisitor.Visit(context.lvalue());
            IPExpr value = exprVisitor.Visit(context.rvalue());

            // If we're doing a move/swap assignment
            if (value is ILinearRef linearRef)
            {
                Variable refVariable = linearRef.Variable;
                switch (linearRef.LinearType)
                {
                    case LinearType.Move:
                        // Moved values must be subtypes of their destinations
                        if (!variable.Type.IsAssignableFrom(refVariable.Type))
                        {
                            throw handler.TypeMismatch(context.rvalue(), refVariable.Type, variable.Type);
                        }

                        return new MoveAssignStmt(context, variable, refVariable);

                    case LinearType.Swap:
                        // Within a function, swaps must only be subtyped in either direction
                        // the actual types are checked at runtime. This is to allow swapping
                        // with the `any` type.
                        if (!variable.Type.IsAssignableFrom(refVariable.Type) &&
                            !refVariable.Type.IsAssignableFrom(variable.Type))
                        {
                            throw handler.TypeMismatch(context.rvalue(), refVariable.Type, variable.Type);
                        }

                        return new SwapAssignStmt(context, variable, refVariable);

                    default:
                        throw handler.InternalError(linearRef.SourceLocation,
                            new ArgumentOutOfRangeException(nameof(context)));
                }
            }

            // If this is a value assignment, we just need subtyping
            if (!variable.Type.IsAssignableFrom(value.Type))
            {
                throw handler.TypeMismatch(context.rvalue(), value.Type, variable.Type);
            }

            return new AssignStmt(context, variable, value);
        }

        public override IPStmt VisitAddStmt(PParser.AddStmtContext context)
        {
            var variable = exprVisitor.Visit(context.lvalue());
            var value = exprVisitor.Visit(context.rvalue());

            // Check linear types
            var valueIsInvariant = false;
            if (value is ILinearRef linearRef) valueIsInvariant = linearRef.LinearType.Equals(LinearType.Swap);

            // Check subtyping
            var valueType = value.Type;

            PLanguageType expectedValueType;

            switch (variable.Type.Canonicalize())
            {
                case SetType setType:
                    expectedValueType = setType.ElementType;
                    break;
                default:
                    throw handler.TypeMismatch(variable, TypeKind.Set);
            }

            if (valueIsInvariant && !expectedValueType.IsSameTypeAs(valueType)
                || !valueIsInvariant && !expectedValueType.IsAssignableFrom(valueType))
                throw handler.TypeMismatch(context.rvalue(), valueType, expectedValueType);

            return new AddStmt(context, variable, value);
        }

        public override IPStmt VisitInsertStmt(PParser.InsertStmtContext context)
        {
            IPExpr variable = exprVisitor.Visit(context.lvalue());
            IPExpr index = exprVisitor.Visit(context.expr());
            IPExpr value = exprVisitor.Visit(context.rvalue());

            // Check linear types
            bool valueIsInvariant = false;
            if (value is ILinearRef linearRef)
            {
                valueIsInvariant = linearRef.LinearType.Equals(LinearType.Swap);
            }

            // Check subtyping
            PLanguageType keyType = index.Type;
            PLanguageType valueType = value.Type;

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
                    throw handler.TypeMismatch(variable, TypeKind.Sequence, TypeKind.Map);
            }

            if (!expectedKeyType.IsAssignableFrom(keyType))
            {
                throw handler.TypeMismatch(context.rvalue(), keyType, expectedKeyType);
            }

            if (valueIsInvariant && !expectedValueType.IsSameTypeAs(valueType)
                || !valueIsInvariant && !expectedValueType.IsAssignableFrom(valueType))
            {
                throw handler.TypeMismatch(context.rvalue(), valueType, expectedValueType);
            }

            return new InsertStmt(context, variable, index, value);
        }

        public override IPStmt VisitRemoveStmt(PParser.RemoveStmtContext context)
        {
            IPExpr variable = exprVisitor.Visit(context.lvalue());
            IPExpr value = exprVisitor.Visit(context.expr());

            if (PLanguageType.TypeIsOfKind(variable.Type, TypeKind.Sequence))
            {
                if (!PrimitiveType.Int.IsAssignableFrom(value.Type))
                {
                    throw handler.TypeMismatch(context.expr(), value.Type, PrimitiveType.Int);
                }
            }
            else if (PLanguageType.TypeIsOfKind(variable.Type, TypeKind.Map))
            {
                MapType map = (MapType)variable.Type.Canonicalize();
                if (!map.KeyType.IsAssignableFrom(value.Type))
                {
                    throw handler.TypeMismatch(context.expr(), value.Type, map.KeyType);
                }
            }
            else if (PLanguageType.TypeIsOfKind(variable.Type, TypeKind.Set))
            {
                var set = (SetType)variable.Type.Canonicalize();
                if (!set.ElementType.IsAssignableFrom(value.Type))
                    throw handler.TypeMismatch(context.expr(), value.Type, set.ElementType);
            }
            else
            {
                throw handler.TypeMismatch(variable, TypeKind.Sequence, TypeKind.Map);
            }

            return new RemoveStmt(context, variable, value);
        }

        public override IPStmt VisitWhileStmt(PParser.WhileStmtContext context)
        {
            IPExpr condition = exprVisitor.Visit(context.expr());
            if (!Equals(condition.Type, PrimitiveType.Bool))
            {
                throw handler.TypeMismatch(context.expr(), condition.Type, PrimitiveType.Bool);
            }

            IPStmt body = Visit(context.statement());
            return new WhileStmt(context, condition, body);
        }

        public override IPStmt VisitIfStmt(PParser.IfStmtContext context)
        {
            IPExpr condition = exprVisitor.Visit(context.expr());
            if (!Equals(condition.Type, PrimitiveType.Bool))
            {
                throw handler.TypeMismatch(context.expr(), condition.Type, PrimitiveType.Bool);
            }

            IPStmt thenBody = Visit(context.thenBranch);
            IPStmt elseBody = context.elseBranch == null ? new NoStmt(context) : Visit(context.elseBranch);
            return new IfStmt(context, condition, thenBody, elseBody);
        }

        public override IPStmt VisitCtorStmt(PParser.CtorStmtContext context)
        {
            string interfaceName = context.iden().GetText();
            if (!table.Lookup(interfaceName, out Interface targetInterface))
            {
                throw handler.MissingDeclaration(context.iden(), "interface", interfaceName);
            }

            List<IPExpr> args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToList();
            TypeCheckingUtils.ValidatePayloadTypes(handler, context, targetInterface.PayloadType, args);
            return new CtorStmt(context, targetInterface, args);
        }

        public override IPStmt VisitFunCallStmt(PParser.FunCallStmtContext context)
        {
            string funName = context.fun.GetText();
            List<IPExpr> argsList = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToList();
            if (!table.Lookup(funName, out Function fun))
            {
                throw handler.MissingDeclaration(context.fun, "function or function prototype", funName);
            }

            if (fun.Signature.Parameters.Count != argsList.Count)
            {
                throw handler.IncorrectArgumentCount((ParserRuleContext)context.rvalueList() ?? context,
                    argsList.Count,
                    fun.Signature.Parameters.Count);
            }

            foreach (Tuple<Variable, IPExpr> pair in fun.Signature.Parameters.Zip(argsList, Tuple.Create))
            {
                TypeCheckingUtils.CheckArgument(handler, context, pair.Item1.Type, pair.Item2);
            }

            method.AddCallee(fun);
            return new FunCallStmt(context, fun, argsList);
        }

        public override IPStmt VisitRaiseStmt(PParser.RaiseStmtContext context)
        {
            IPExpr evtExpr = exprVisitor.Visit(context.expr());
            if (IsDefinitelyNullEvent(evtExpr))
            {
                throw handler.EmittedNullEvent(evtExpr);
            }

            if (!PrimitiveType.Event.IsAssignableFrom(evtExpr.Type))
            {
                throw handler.TypeMismatch(context.expr(), evtExpr.Type, PrimitiveType.Event);
            }

            method.CanRaiseEvent = true;

            IPExpr[] args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();
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
                throw handler.IllegalMonitorOperation(context, context.SEND().Symbol, machine);
            }

            IPExpr machineExpr = exprVisitor.Visit(context.machine);
            if (!PrimitiveType.Machine.IsAssignableFrom(machineExpr.Type))
            {
                throw handler.TypeMismatch(context.machine, machineExpr.Type, PrimitiveType.Machine);
            }

            IPExpr evtExpr = exprVisitor.Visit(context.@event);
            if (IsDefinitelyNullEvent(evtExpr))
            {
                throw handler.EmittedNullEvent(evtExpr);
            }

            if (!PrimitiveType.Event.IsAssignableFrom(evtExpr.Type))
            {
                throw handler.TypeMismatch(context.@event, evtExpr.Type, PrimitiveType.Event);
            }

            IPExpr[] args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();

            if (evtExpr is EventRefExpr eventRef)
            {
                TypeCheckingUtils.ValidatePayloadTypes(handler, context, eventRef.Value.PayloadType, args);
            }

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
                throw handler.IllegalMonitorOperation(context, context.ANNOUNCE().Symbol, machine);
            }

            IPExpr evtExpr = exprVisitor.Visit(context.expr());
            if (IsDefinitelyNullEvent(evtExpr))
            {
                throw handler.EmittedNullEvent(evtExpr);
            }

            if (!PrimitiveType.Event.IsAssignableFrom(evtExpr.Type))
            {
                throw handler.TypeMismatch(context.expr(), evtExpr.Type, PrimitiveType.Event);
            }

            List<IPExpr> args = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToList();
            return new AnnounceStmt(context, evtExpr, args.Count == 0 ? null : args[0]);
        }

        public override IPStmt VisitGotoStmt(PParser.GotoStmtContext context)
        {
            PParser.StateNameContext stateNameContext = context.stateName();
            string stateName = stateNameContext.state.GetText();
            IStateContainer current = machine;
            foreach (PParser.IdenContext token in stateNameContext._groups)
            {
                current = current?.GetGroup(token.GetText());
                if (current == null)
                {
                    throw handler.MissingDeclaration(token, "group", token.GetText());
                }
            }

            AST.States.State state = current?.GetState(stateName);
            if (state == null)
            {
                throw handler.MissingDeclaration(stateNameContext.state, "state", stateName);
            }

            PLanguageType expectedType =
                state.Entry?.Signature.ParameterTypes.ElementAtOrDefault(0) ?? PrimitiveType.Null;
            IPExpr[] rvaluesList = TypeCheckingUtils.VisitRvalueList(context.rvalueList(), exprVisitor).ToArray();
            foreach (IPExpr arg in rvaluesList)
            {
                if (arg is LinearAccessRefExpr linearArg && linearArg.LinearType == LinearType.Swap)
                {
                    throw handler.InvalidSwap(linearArg, "swap not allowed on goto");
                }
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

            PLanguageType payloadType = payload?.Type ?? PrimitiveType.Null;
            if (!expectedType.IsAssignableFrom(payloadType))
            {
                throw handler.TypeMismatch(context, payloadType, expectedType);
            }

            method.CanChangeState = true;
            return new GotoStmt(context, state, payload);
        }

        public override IPStmt VisitReceiveStmt(PParser.ReceiveStmtContext context)
        {
            if (machine?.IsSpec == true)
            {
                throw handler.IllegalMonitorOperation(context, context.RECEIVE().Symbol, machine);
            }

            Dictionary<PEvent, Function> cases = new Dictionary<PEvent, Function>();
            foreach (PParser.RecvCaseContext caseContext in context.recvCase())
            {
                Function recvHandler =
                    new Function(caseContext.anonEventHandler())
                    {
                        Scope = table.MakeChildScope(),
                        Owner = method.Owner,
                        ParentFunction = method,
                        Role = FunctionRole.ReceiveHandler
                    };

                if (caseContext.anonEventHandler().funParam() is PParser.FunParamContext param)
                {
                    Variable paramVar = recvHandler.Scope.Put(param.name.GetText(), param, VariableRole.Param);
                    paramVar.Type = TypeResolver.ResolveType(param.type(), recvHandler.Scope, handler);
                    recvHandler.Signature.Parameters.Add(paramVar);
                }

                FunctionBodyVisitor.PopulateMethod(handler, recvHandler);

                foreach (PParser.EventIdContext eventIdContext in caseContext.eventList().eventId())
                {
                    if (!table.Lookup(eventIdContext.GetText(), out PEvent pEvent))
                    {
                        throw handler.MissingDeclaration(eventIdContext, "event", eventIdContext.GetText());
                    }

                    if (cases.ContainsKey(pEvent))
                    {
                        throw handler.DuplicateReceiveCase(eventIdContext, pEvent);
                    }

                    PLanguageType expectedType =
                        recvHandler.Signature.ParameterTypes.ElementAtOrDefault(0) ?? PrimitiveType.Null;
                    if (!expectedType.IsAssignableFrom(pEvent.PayloadType))
                    {
                        throw handler.TypeMismatch(caseContext.anonEventHandler(), expectedType,
                            pEvent.PayloadType);
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