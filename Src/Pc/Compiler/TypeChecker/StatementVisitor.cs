using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public class StatementVisitor : PParserBaseVisitor<IPStmt>
    {
        private readonly ITranslationErrorHandler handler;
        private readonly Machine machine;
        private readonly Scope table;
        private readonly Function method;

        public StatementVisitor(ITranslationErrorHandler handler, Machine machine, Function method)
        {
            this.handler = handler;
            this.machine = machine;
            this.method = method;
            this.table = method.Scope;
        }

        public override IPStmt VisitCompoundStmt(PParser.CompoundStmtContext context)
        {
            var statements = context.statement().Select(Visit);
            return new CompoundStmt(statements.ToList());
        }

        public override IPStmt VisitPopStmt(PParser.PopStmtContext context) { return new PopStmt(); }

        public override IPStmt VisitAssertStmt(PParser.AssertStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr assertion = exprVisitor.Visit(context.expr());
            if (assertion.Type != PrimitiveType.Bool)
            {
                throw handler.TypeMismatch(context.expr(), assertion.Type, PrimitiveType.Bool);
            }
            string message = context.StringLiteral()?.GetText() ?? "";
            return new AssertStmt(assertion, message);
        }

        public override IPStmt VisitPrintStmt(PParser.PrintStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            string message = context.StringLiteral().GetText();
            int numNecessaryArgs = (from Match match in Regex.Matches(message, @"(?:{{|}}|{(\d+)}|[^{}]+|{|})")
                                    where match.Groups[1].Success
                                    select int.Parse(match.Groups[1].Value) + 1)
                .Concat(new[] {0})
                .Max();
            var argsExprs = context.rvalueList()?.rvalue().Select(rvalue => exprVisitor.Visit(rvalue)) ??
                            Enumerable.Empty<IPExpr>();
            var args = argsExprs.ToList();
            if (args.Count < numNecessaryArgs)
            {
                throw handler.IncorrectArgumentCount(
                                                     (ParserRuleContext) context.rvalueList() ?? context,
                                                     args.Count,
                                                     numNecessaryArgs);
            }
            if (args.Count > numNecessaryArgs)
            {
                handler.IssueWarning((ParserRuleContext) context.rvalueList() ?? context,
                                     "ignoring extra arguments to print expression");
                args = args.Take(numNecessaryArgs).ToList();
            }
            return new PrintStmt(message, args);
        }

        public override IPStmt VisitReturnStmt(PParser.ReturnStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr returnValue = context.expr() == null ? null : exprVisitor.Visit(context.expr());
            PLanguageType returnType = returnValue?.Type ?? PrimitiveType.Null;
            if (!method.Signature.ReturnType.IsAssignableFrom(returnType))
            {
                throw handler.TypeMismatch(context, returnType, method.Signature.ReturnType);
            }
            return new ReturnStmt(returnValue);
        }

        public override IPStmt VisitAssignStmt(PParser.AssignStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr variable = exprVisitor.Visit(context.lvalue());
            IPExpr value = exprVisitor.Visit(context.rvalue());
            if (!(value is ILinearRef linearRef))
            {
                return new AssignStmt(variable, value);
            }
            if (!variable.Type.IsAssignableFrom(linearRef.Variable.Type))
            {
                throw handler.TypeMismatch(context.rvalue(), linearRef.Variable.Type, variable.Type);
            }
            switch (linearRef.LinearType)
            {
                case LinearType.Move:
                    return new MoveAssignStmt(variable, linearRef.Variable);
                case LinearType.Swap:
                    return new SwapAssignStmt(variable, linearRef.Variable);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override IPStmt VisitInsertStmt(PParser.InsertStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr variable = exprVisitor.Visit(context.lvalue());
            IPExpr value = exprVisitor.Visit(context.rvalue());

            if (!(value.Type.Canonicalize() is TupleType valueTuple))
            {
                throw handler.TypeMismatch(context.rvalue(), value.Type, TypeKind.Tuple);
            }

            if (valueTuple.Types.Count != 2)
            {
                throw handler.IssueError(context.rvalue(), "Insertion tuple must be of length two: (key/index, value)");
            }

            PLanguageType keyType = valueTuple.Types[0];
            PLanguageType valueType = valueTuple.Types[1];

            if (PLanguageType.TypeIsOfKind(variable.Type, TypeKind.Sequence))
            {
                SequenceType sequenceType = (SequenceType) variable.Type.Canonicalize();
                if (!PrimitiveType.Int.IsAssignableFrom(keyType))
                {
                    throw handler.TypeMismatch(context.rvalue(), keyType, PrimitiveType.Int);
                }
                if (!sequenceType.ElementType.IsAssignableFrom(valueType))
                {
                    throw handler.TypeMismatch(context.rvalue(), valueTuple, sequenceType.ElementType);
                }
            }
            else if (PLanguageType.TypeIsOfKind(variable.Type, TypeKind.Map))
            {
                MapType mapType = (MapType) variable.Type.Canonicalize();
                if (!mapType.KeyType.IsAssignableFrom(keyType))
                {
                    throw handler.TypeMismatch(context.rvalue(), keyType, mapType.KeyType);
                }
                if (!mapType.ValueType.IsAssignableFrom(valueType))
                {
                    throw handler.TypeMismatch(context.rvalue(), valueTuple, mapType.ValueType);
                }
            }
            else
            {
                throw handler.TypeMismatch(context.lvalue(), variable.Type, TypeKind.Sequence, TypeKind.Map);
            }

            return new InsertStmt(variable, value);
        }

        public override IPStmt VisitRemoveStmt(PParser.RemoveStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
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
                MapType map = (MapType) variable.Type.Canonicalize();
                if (!map.KeyType.IsAssignableFrom(value.Type))
                {
                    throw handler.TypeMismatch(context.expr(), value.Type, map.KeyType);
                }
            }
            else
            {
                throw handler.TypeMismatch(context.lvalue(), variable.Type, TypeKind.Sequence, TypeKind.Map);
            }

            return new RemoveStmt(variable, value);
        }

        public override IPStmt VisitWhileStmt(PParser.WhileStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr condition = exprVisitor.Visit(context.expr());
            if (condition.Type != PrimitiveType.Bool)
            {
                throw handler.TypeMismatch(context.expr(), condition.Type, PrimitiveType.Bool);
            }
            IPStmt body = Visit(context.statement());
            return new WhileStmt(condition, body);
        }

        public override IPStmt VisitIfStmt(PParser.IfStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr condition = exprVisitor.Visit(context.expr());
            if (condition.Type != PrimitiveType.Bool)
            {
                throw handler.TypeMismatch(context.expr(), condition.Type, PrimitiveType.Bool);
            }
            IPStmt thenBody = Visit(context.thenBranch);
            IPStmt elseBody = context.elseBranch == null ? null : Visit(context.elseBranch);
            return new IfStmt(condition, thenBody, elseBody);
        }

        public override IPStmt VisitCtorStmt(PParser.CtorStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            string machineName = context.iden().GetText();
            if (table.Lookup(machineName, out Machine targetMachine))
            {
                bool hasArguments = targetMachine.PayloadType != PrimitiveType.Null;
                var args = context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ??
                           Enumerable.Empty<IPExpr>();
                if (hasArguments)
                {
                    var argsList = args.ToList();
                    if (argsList.Count != 1)
                    {
                        throw handler.IncorrectArgumentCount((ParserRuleContext) context.rvalueList() ?? context,
                                                             argsList.Count,
                                                             1);
                    }
                    return new CtorStmt(targetMachine, argsList);
                }
                if (args.Count() != 0)
                {
                    handler.IssueWarning((ParserRuleContext) context.rvalueList() ?? context,
                                         "ignoring extra parameters passed to machine constructor");
                }
                return new CtorStmt(targetMachine, new List<IPExpr>());
            }
            throw handler.MissingDeclaration(context.iden(), "machine", machineName);
        }

        public override IPStmt VisitFunCallStmt(PParser.FunCallStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            string funName = context.fun.GetText();
            var args = context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ?? Enumerable.Empty<IPExpr>();
            var argsList = args.ToList();
            if (table.Lookup(funName, out Function fun))
            {
                if (fun.Signature.Parameters.Count != argsList.Count)
                {
                    throw handler.IncorrectArgumentCount((ParserRuleContext) context.rvalueList() ?? context,
                                                         argsList.Count,
                                                         fun.Signature.Parameters.Count);
                }
                foreach (var pair in fun.Signature.Parameters.Zip(argsList, Tuple.Create))
                {
                    ITypedName param = pair.Item1;
                    IPExpr arg = pair.Item2;
                    if (!param.Type.IsAssignableFrom(arg.Type))
                    {
                        throw handler.TypeMismatch(context, arg.Type, param.Type);
                    }
                }
                return new FunCallStmt(fun, argsList);
            }
            throw handler.MissingDeclaration(context.fun, "function or function prototype", funName);
        }

        public override IPStmt VisitRaiseStmt(PParser.RaiseStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);

            IPExpr pExpr = exprVisitor.Visit(context.expr());
            if (IsDefinitelyNullEvent(pExpr))
            {
                throw handler.EmittedNullEvent(context.expr());
            }

            if (!PrimitiveType.Event.IsAssignableFrom(pExpr.Type))
            {
                throw handler.TypeMismatch(context.expr(), pExpr.Type, PrimitiveType.Event);
            }

            var args = (context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ??
                        Enumerable.Empty<IPExpr>()).ToList();

            // TODO: wot?
            return new RaiseStmt(pExpr, args.Count == 0 ? null : args[0]);
        }

        public override IPStmt VisitSendStmt(PParser.SendStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr machineExpr = exprVisitor.Visit(context.machine);
            if (machineExpr.Type != PrimitiveType.Machine)
            {
                throw handler.TypeMismatch(context.machine, machineExpr.Type, PrimitiveType.Machine);
            }

            IPExpr evtExpr = exprVisitor.Visit(context.@event);
            if (IsDefinitelyNullEvent(evtExpr))
            {
                throw handler.EmittedNullEvent(context.@event);
            }

            if (!PrimitiveType.Event.IsAssignableFrom(evtExpr.Type))
            {
                throw handler.TypeMismatch(context.@event, evtExpr.Type, PrimitiveType.Event);
            }

            var args = context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ?? Enumerable.Empty<IPExpr>();
            var argsList = args.ToList();
            return new SendStmt(machineExpr, evtExpr, argsList);
        }

        private static bool IsDefinitelyNullEvent(IPExpr evtExpr)
        {
            return evtExpr is NullLiteralExpr || evtExpr is EventRefExpr evtRef && evtRef.PEvent.Name.Equals("null");
        }

        public override IPStmt VisitAnnounceStmt(PParser.AnnounceStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);

            IPExpr evtExpr = exprVisitor.Visit(context.expr());
            if (IsDefinitelyNullEvent(evtExpr))
            {
                throw handler.EmittedNullEvent(context.expr());
            }

            if (!PrimitiveType.Event.IsAssignableFrom(evtExpr.Type))
            {
                throw handler.TypeMismatch(context.expr(), evtExpr.Type, PrimitiveType.Event);
            }

            var args = (context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ??
                        Enumerable.Empty<IPExpr>()).ToList();
            return new AnnounceStmt(evtExpr, args.Count == 0 ? null : args[0]);
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
            State state = current?.GetState(stateName);
            if (state == null)
            {
                throw handler.MissingDeclaration(stateNameContext.state, "state", stateName);
            }
            IPExpr payload = null;
            PLanguageType expectedType =
                state.Entry.Signature.ParameterTypes.ElementAtOrDefault(0) ?? PrimitiveType.Null;
            if (context.rvalueList()?.rvalue() is PParser.RvalueContext[] rvalues)
            {
                var exprVisitor = new ExprVisitor(table, handler);
                if (rvalues.Length == 1)
                {
                    payload = exprVisitor.Visit(rvalues[0]);
                }
                else
                {
                    IPExpr[] tupleFields = rvalues.Select(exprVisitor.Visit).ToArray();
                    payload = new UnnamedTupleExpr(tupleFields,
                        new TupleType(tupleFields.Select(f => f.Type).ToList()));
                }
            }
            PLanguageType payloadType = payload?.Type ?? PrimitiveType.Null;
            if (!expectedType.IsAssignableFrom(payloadType))
            {
                throw handler.TypeMismatch(context, payloadType, expectedType);
            }
            return new GotoStmt(state, payload);
        }

        public override IPStmt VisitReceiveStmt(PParser.ReceiveStmtContext context)
        {
            // TODO: can receive statements have event variables as their cases?
            var cases = new Dictionary<PEvent, Function>();
            foreach (PParser.RecvCaseContext caseContext in context.recvCase())
            {
                var recvHandler =
                    new Function(caseContext.anonEventHandler()) {Scope = table.MakeChildScope(), Owner = method.Owner};
                if (caseContext.anonEventHandler().funParam() is PParser.FunParamContext param)
                {
                    var paramVar = recvHandler.Scope.Put(param.name.GetText(), param);
                    paramVar.Type = TypeResolver.ResolveType(param.type(), recvHandler.Scope, handler);
                    recvHandler.Signature.Parameters.Add(paramVar);
                }

                FunctionBodyVisitor.PopulateMethod(handler, recvHandler);
                
                foreach (var eventIdContext in caseContext.eventList().eventId())
                {
                    if (!table.Lookup(eventIdContext.GetText(), out PEvent pEvent))
                    {
                        throw handler.MissingDeclaration(eventIdContext, "event", eventIdContext.GetText());
                    }
                    if (cases.ContainsKey(pEvent))
                    {
                        throw handler.IssueError(eventIdContext, $"duplicate case for event {pEvent.Name} in receive");
                    }
                    PLanguageType expectedType =
                        recvHandler.Signature.ParameterTypes.ElementAtOrDefault(0) ?? PrimitiveType.Null;
                    if (!expectedType.IsAssignableFrom(pEvent.PayloadType))
                    {
                        throw handler.TypeMismatch(caseContext.anonEventHandler().funParam(), expectedType,
                            pEvent.PayloadType);
                    }
                    cases.Add(pEvent, recvHandler);
                }
            }
            return new ReceiveStmt(cases);
        }

        public override IPStmt VisitNoStmt(PParser.NoStmtContext context) { return new NoStmt(); }
    }
}
