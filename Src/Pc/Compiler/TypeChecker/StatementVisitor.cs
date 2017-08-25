using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class StatementVisitor : PParserBaseVisitor<IEnumerable<IPStmt>>
    {
        private readonly DeclarationTable table;
        private readonly Machine machine;
        private readonly ITranslationErrorHandler handler;

        public StatementVisitor(DeclarationTable table, Machine machine, ITranslationErrorHandler handler)
        {
            this.table = table;
            this.machine = machine;
            this.handler = handler;
        }

        public override IEnumerable<IPStmt> VisitCompoundStmt(PParser.CompoundStmtContext context)
        {
            var statements = context.statement().SelectMany(Visit);
            yield return new CompoundStatement(statements.ToList());
        }

        public override IEnumerable<IPStmt> VisitPopStmt(PParser.PopStmtContext context) { yield return new PopStmt(); }

        public override IEnumerable<IPStmt> VisitAssertStmt(PParser.AssertStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr assertion = exprVisitor.Visit(context.expr());
            if (assertion.Type != PrimitiveType.Bool)
            {
                throw handler.TypeMismatch(context.expr(), assertion.Type, PrimitiveType.Bool);
            }
            string message = context.StringLiteral()?.GetText() ?? "";
            yield return new AssertStmt(assertion, message);
        }

        public override IEnumerable<IPStmt> VisitPrintStmt(PParser.PrintStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            string message = context.StringLiteral().GetText();
            int numNecessaryArgs = (from Match match in Regex.Matches(message, @"(?:{{|}}|{(\d+)}|[^{}]+|{|})")
                                    where match.Groups.Count >= 2
                                    select int.Parse(match.Groups[1].Value) + 1)
                .Concat(new[] {0})
                .Max();
            var argsExprs = context.rvalueList()?.rvalue().Select(rvalue => exprVisitor.Visit(rvalue)) ?? Enumerable.Empty<IPExpr>();
            var args = argsExprs.ToList();
            if (args.Count < numNecessaryArgs)
            {
                throw handler.IncorrectArgumentCount(
                    (ParserRuleContext)context.rvalueList() ?? context,
                    args.Count,
                    numNecessaryArgs);
            }
            if (args.Count > numNecessaryArgs)
            {
                handler.IssueWarning((ParserRuleContext)context.rvalueList() ?? context, "ignoring extra arguments to print expression");
                args = args.Take(numNecessaryArgs).ToList();
            }
            yield return new PrintStmt(message, args);
        }

        public override IEnumerable<IPStmt> VisitReturnStmt(PParser.ReturnStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            yield return new ReturnStmt(context.expr() == null ? null : exprVisitor.Visit(context.expr()));
        }

        public override IEnumerable<IPStmt> VisitAssignStmt(PParser.AssignStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr variable = exprVisitor.Visit(context.lvalue());
            IPExpr value = exprVisitor.Visit(context.rvalue());
            if (value is ILinearRef linearRef)
            {
                if (!variable.Type.IsAssignableFrom(linearRef.Variable.Type))
                {
                    throw handler.TypeMismatch(context.rvalue(), linearRef.Variable.Type, variable.Type);
                }
                switch (linearRef.LinearType)
                {
                    case LinearType.Move:
                        yield return new MoveAssignStmt(variable, linearRef.Variable);
                        break;
                    case LinearType.Swap:
                        yield return new SwapAssignStmt(variable, linearRef.Variable);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                yield return new AssignStmt(variable, value);
            }
        }

        public override IEnumerable<IPStmt> VisitInsertStmt(PParser.InsertStmtContext context)
        {
            throw new NotImplementedException("insert statements");
        }

        public override IEnumerable<IPStmt> VisitRemoveStmt(PParser.RemoveStmtContext context)
        {
            throw new NotImplementedException("remove statements");
        }

        public override IEnumerable<IPStmt> VisitWhileStmt(PParser.WhileStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr condition = exprVisitor.Visit(context.expr());
            if (condition.Type != PrimitiveType.Bool)
            {
                throw handler.TypeMismatch(context.expr(), condition.Type, PrimitiveType.Bool);
            }
            var body = Visit(context.statement()).ToList();
            Debug.Assert(body.Count == 1);
            yield return new WhileStmt(condition, body[0]);
        }

        public override IEnumerable<IPStmt> VisitIfStmt(PParser.IfStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr condition = exprVisitor.Visit(context.expr());
            if (condition.Type != PrimitiveType.Bool)
            {
                throw handler.TypeMismatch(context.expr(), condition.Type, PrimitiveType.Bool);
            }
            var thenBody = Visit(context.thenBranch).ToList();
            Debug.Assert(thenBody.Count == 1);
            var elseBody = context.elseBranch == null ? null : Visit(context.elseBranch).ToList();
            Debug.Assert(elseBody == null || elseBody.Count == 1);
            yield return new IfStmt(condition, thenBody[0], elseBody?[0]);
        }

        public override IEnumerable<IPStmt> VisitCtorStmt(PParser.CtorStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            string machineName = context.iden().GetText();
            if (table.Lookup(machineName, out Machine machine))
            {
                bool hasArguments = machine.PayloadType != PrimitiveType.Null;
                var args = context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ??
                           Enumerable.Empty<IPExpr>();
                if (hasArguments)
                {
                    var argsList = args.ToList();
                    if (argsList.Count != 1)
                    {
                        throw handler.IncorrectArgumentCount((ParserRuleContext)context.rvalueList() ?? context,
                                                             argsList.Count,
                                                             1);
                    }
                    yield return new CtorStmt(machine, argsList);
                }
                else
                {
                    if (args.Count() != 0)
                    {
                        handler.IssueWarning(
                            (ParserRuleContext)context.rvalueList() ?? context,
                            "ignoring extra parameters passed to machine constructor");
                    }
                    yield return new CtorStmt(machine, new List<IPExpr>());
                }
            }
            else if (table.Lookup(machineName, out MachineProto machineProto))
            {
                throw new NotImplementedException("machine prototype constructor statements");
            }
            else
            {
                throw handler.MissingDeclaration(context.iden(), "machine or machine prototype", machineName);
            }
        }

        public override IEnumerable<IPStmt> VisitFunCallStmt(PParser.FunCallStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            string funName = context.fun.GetText();
            var args = context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ?? Enumerable.Empty<IPExpr>();
            var argsList = args.ToList();
            if (table.Lookup(funName, out Function fun))
            {
                if (fun.Signature.Parameters.Count != argsList.Count)
                {
                    throw handler.IncorrectArgumentCount(
                        (ParserRuleContext)context.rvalueList() ?? context,
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
                yield return new FunCallStmt(fun, argsList);
            }
            else if (table.Lookup(funName, out FunctionProto proto))
            {
                throw new NotImplementedException("function prototype call statement");
            }
            else
            {
                throw handler.MissingDeclaration(context.fun, "function or function prototype", funName);
            }
        }

        public override IEnumerable<IPStmt> VisitRaiseStmt(PParser.RaiseStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr pExpr = exprVisitor.Visit(context.expr());
            if (!(pExpr is EventRefExpr eventRef))
            {
                throw new NotImplementedException("raising dynamic events");
            }

            var args = (context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ??
                        Enumerable.Empty<IPExpr>()).ToList();
            PEvent evt = eventRef.PEvent;
            if (evt.PayloadType == PrimitiveType.Null && args.Count == 0 ||
                evt.PayloadType != PrimitiveType.Null && args.Count == 1)
            {
                yield return new RaiseStmt(eventRef.PEvent, args.Count == 0 ? null : args[0]);
            }
            throw handler.IncorrectArgumentCount(
                (ParserRuleContext)context.rvalueList() ?? context,
                args.Count,
                evt.PayloadType == PrimitiveType.Null ? 0 : 1);
        }

        public override IEnumerable<IPStmt> VisitSendStmt(PParser.SendStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr machineExpr = exprVisitor.Visit(context.machine);
            if (machineExpr.Type != PrimitiveType.Machine)
            {
                throw handler.TypeMismatch(context.machine, machineExpr.Type, PrimitiveType.Machine);
            }
            IPExpr evtExpr = exprVisitor.Visit(context.@event);
            if (!(evtExpr is EventRefExpr))
            {
                throw new NotImplementedException("sending dynamic events");
            }

            PEvent evt = ((EventRefExpr) evtExpr).PEvent;
            var args = context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ?? Enumerable.Empty<IPExpr>();
            var argsList = args.ToList();
            if (evt.PayloadType != PrimitiveType.Null && argsList.Count == 1)
            {
                if (!evt.PayloadType.IsAssignableFrom(argsList[0].Type))
                {
                    throw handler.TypeMismatch(context.rvalueList().rvalue(0), argsList[0].Type, evt.PayloadType);
                }
                yield return new SendStmt(machineExpr, evt, argsList);
            }
            else if (evt.PayloadType == PrimitiveType.Null && argsList.Count == 0)
            {
                yield return new SendStmt(machineExpr, evt, argsList);
            }
            else
            {
                throw handler.IncorrectArgumentCount(
                    (ParserRuleContext)context.rvalueList() ?? context,
                    argsList.Count,
                    evt.PayloadType == PrimitiveType.Null ? 0 : 1);
            }
        }

        public override IEnumerable<IPStmt> VisitAnnounceStmt(PParser.AnnounceStmtContext context)
        {
            var exprVisitor = new ExprVisitor(table, handler);
            IPExpr pExpr = exprVisitor.Visit(context.expr());
            if (!(pExpr is EventRefExpr eventRef))
            {
                throw new NotImplementedException("announcing dynamic events");
            }

            var args = (context.rvalueList()?.rvalue().Select(rv => exprVisitor.Visit(rv)) ??
                        Enumerable.Empty<IPExpr>()).ToList();
            PEvent evt = eventRef.PEvent;
            if (evt.PayloadType == PrimitiveType.Null && args.Count == 0 ||
                evt.PayloadType != PrimitiveType.Null && args.Count == 1)
            {
                yield return new AnnounceStmt(eventRef.PEvent, args.Count == 0 ? null : args[0]);
            }
            throw handler.IncorrectArgumentCount(
                (ParserRuleContext)context.rvalueList() ?? context,
                args.Count,
                evt.PayloadType == PrimitiveType.Null ? 0 : 1);
        }

        public override IEnumerable<IPStmt> VisitGotoStmt(PParser.GotoStmtContext context)
        {
            PParser.StateNameContext stateNameContext = context.stateName();
            var stateName = stateNameContext.state.GetText();
            IStateContainer current = machine;
            foreach (var token in stateNameContext._groups)
            {
                current = current?.GetGroup(token.GetText());
                if (current == null)
                {  
                    throw handler.MissingDeclaration(token, "group", token.GetText());
                }
            }
            var state = current?.GetState(stateName);
            if (state == null)
            {
                throw handler.MissingDeclaration(stateNameContext.state, "state", stateName);
            }
            IPExpr payload = null;
            if (context.rvalueList() != null)
            {
                throw new NotImplementedException("goto statement with payload");
            }

            yield return new GotoStmt(state, payload);
        }

        public override IEnumerable<IPStmt> VisitReceiveStmt(PParser.ReceiveStmtContext context)
        {
            throw new NotImplementedException("receive statements");
        }

        public override IEnumerable<IPStmt> VisitNoStmt(PParser.NoStmtContext context)
        {
            return Enumerable.Empty<IPStmt>();
        }
    }

    public class GotoStmt : IPStmt
    {
        public State State { get; }
        public IPExpr Payload { get; }
        public GotoStmt(State state, IPExpr payload)
        {
            State = state;
            Payload = payload;
        }
    }

    public class AnnounceStmt : IPStmt
    {
        public PEvent PEvent { get; }
        public IPExpr Payload { get; }

        public AnnounceStmt(PEvent pEvent, IPExpr payload)
        {
            PEvent = pEvent;
            Payload = payload;
        }
    }

    public class SendStmt : IPStmt
    {
        public SendStmt(IPExpr machineExpr, PEvent evt, List<IPExpr> argsList)
        {
            MachineExpr = machineExpr;
            Evt = evt;
            ArgsList = argsList;
        }

        public IPExpr MachineExpr { get; }
        public PEvent Evt { get; }
        public List<IPExpr> ArgsList { get; }
    }

    public class RaiseStmt : IPStmt
    {
        public RaiseStmt(PEvent pEvent, IPExpr payload)
        {
            PEvent = pEvent;
            Payload = payload;
        }

        public PEvent PEvent { get; }
        public IPExpr Payload { get; }
    }

    public class FunCallStmt : IPStmt
    {
        public FunCallStmt(Function fun, List<IPExpr> argsList)
        {
            Fun = fun;
            ArgsList = argsList;
        }

        public Function Fun { get; }
        public List<IPExpr> ArgsList { get; }
    }

    public class CtorStmt : IPStmt
    {
        public CtorStmt(Machine machine, List<IPExpr> arguments)
        {
            Machine = machine;
            Arguments = arguments;
        }

        public Machine Machine { get; }
        public List<IPExpr> Arguments { get; }
    }

    public class IfStmt : IPStmt
    {
        public IfStmt(IPExpr condition, IPStmt thenBranch, IPStmt elseBranch)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        public IPExpr Condition { get; }
        public IPStmt ThenBranch { get; }
        public IPStmt ElseBranch { get; }
    }

    public class WhileStmt : IPStmt
    {
        public WhileStmt(IPExpr condition, IPStmt body)
        {
            Condition = condition;
            Body = body;
        }

        public IPExpr Condition { get; }
        public IPStmt Body { get; }
    }

    public class AssignStmt : IPStmt
    {
        public AssignStmt(IPExpr variable, IPExpr value)
        {
            Variable = variable;
            Value = value;
        }

        public IPExpr Variable { get; }
        public IPExpr Value { get; }
    }

    public class SwapAssignStmt : IPStmt
    {
        public SwapAssignStmt(IPExpr newLocation, Variable oldLocation)
        {
            NewLocation = newLocation;
            OldLocation = oldLocation;
        }

        public IPExpr NewLocation { get; }
        public Variable OldLocation { get; }
    }

    public class MoveAssignStmt : IPStmt
    {
        public MoveAssignStmt(IPExpr toLocation, Variable fromVariable)
        {
            ToLocation = toLocation;
            FromVariable = fromVariable;
        }

        public IPExpr ToLocation { get; }
        public Variable FromVariable { get; }
    }

    public class ReturnStmt : IPStmt
    {
        public ReturnStmt(IPExpr returnValue) { ReturnValue = returnValue; }
        public IPExpr ReturnValue { get; }
        public PLanguageType ReturnType => ReturnValue == null ? PrimitiveType.Null : ReturnValue.Type;
    }

    public class PrintStmt : IPStmt
    {
        public PrintStmt(string message, List<IPExpr> args)
        {
            Message = message;
            Args = args;
        }

        public string Message { get; }
        public List<IPExpr> Args { get; }
    }

    public class AssertStmt : IPStmt
    {
        public AssertStmt(IPExpr assertion, string message)
        {
            Assertion = assertion;
            Message = message;
        }

        public IPExpr Assertion { get; }
        public string Message { get; }
    }

    public class PopStmt : IPStmt
    {
    }

    public class CompoundStatement : IPStmt
    {
        public CompoundStatement(List<IPStmt> statements) { Statements = statements; }

        public List<IPStmt> Statements { get; }
    }

    public interface IPStmt
    {
    }
}
