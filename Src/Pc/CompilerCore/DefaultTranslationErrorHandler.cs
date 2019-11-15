using Antlr4.Runtime;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using System;
using System.IO;
using System.Linq;

namespace Plang.Compiler
{

    public class DefaultTranslationErrorHandler : ITranslationErrorHandler
    {
        private readonly ILocationResolver locationResolver;

        public DefaultTranslationErrorHandler(ILocationResolver locationResolver)
        {
            this.locationResolver = locationResolver;
        }

        public Exception DuplicateStartState(
            ParserRuleContext location,
            State duplicateStart,
            State originalStart,
            Machine machine)
        {
            return IssueError(location,
                $"state '{duplicateStart.Name}' tries to replace start state '{originalStart.Name}' in machine '{machine.Name}'");
        }

        public Exception DuplicateEventAction(ParserRuleContext location, IStateAction existingAction, State state)
        {
            return IssueError(location,
                $"event handler in state '{state.Name}' duplicates handler at {locationResolver.GetLocation(existingAction.SourceLocation)}");
        }

        public Exception DuplicateStateExitHandler(ParserRuleContext location, Function existingHandler, State state)
        {
            return IssueError(location,
                $"exit handler in state '{state.Name}' duplicates exit handler at {locationResolver.GetLocation(existingHandler.SourceLocation)}");
        }

        public Exception DuplicateStateEntry(ParserRuleContext location, Function existingHandler, State state)
        {
            return IssueError(location,
                $"entry handler in state '{state.Name}' duplicates entry handler at {locationResolver.GetLocation(existingHandler.SourceLocation)}");
        }

        public Exception DuplicateDeclaration(ParserRuleContext location, IPDecl duplicate, IPDecl existing)
        {
            return IssueError(location,
                $"'{duplicate.Name}' duplicates declaration '{existing.Name}' at {locationResolver.GetLocation(existing.SourceLocation)}");
        }

        public Exception IncorrectArgumentCount(ParserRuleContext location, int actualCount, int expectedCount)
        {
            return IssueError(location,
                $"function or constructor call expected {expectedCount} arguments, got {actualCount}");
        }

        public Exception MissingDeclaration(ParserRuleContext location, string declarationKind, string missingName)
        {
            return IssueError(location, $"could not find {declarationKind} '{missingName}'");
        }

        public Exception CircularTypeDef(ParserRuleContext location, TypeDef typeDef)
        {
            return IssueError(location, $"typedef '{typeDef.Name}' has a circular dependency");
        }

        public Exception DuplicateNamedTupleEntry(ParserRuleContext location, string duplicateName)
        {
            return IssueError(location, $"named tuple has duplicate field '{duplicateName}'");
        }

        public Exception IllegalTypeInCoerceExpr(ParserRuleContext location)
        {
            return IssueError(location, "expected an interface or int or float type");
        }

        public Exception IllegalInterfaceCoerce(ParserRuleContext context, PLanguageType oldType, PLanguageType newType)
        {
            PEvent outlierEvent =
                newType.AllowedPermissions.Value.First(x => !oldType.AllowedPermissions.Value.Contains(x));
            return IssueError(context,
                $"illegal Coerce, {oldType.OriginalRepresentation} permissions is not a superset of {newType.OriginalRepresentation} (e.g., event {outlierEvent.Name})");
        }

        public Exception StaticFunctionNotAllowedAsHandler(ParserRuleContext funName, string name)
        {
            return IssueError(funName,
                $"global functions or foreign functions are not directly allowed as handlers, {name}");
        }

        public Exception TypeMismatch(ParserRuleContext location, PLanguageType actual, params PLanguageType[] expected)
        {
            return IssueError(location,
                $"got type: {actual.OriginalRepresentation}, expected: {string.Join("; ", expected.Select(t => t.OriginalRepresentation))}");
        }

        public Exception TypeMismatch(IPExpr expr, params TypeKind[] expected)
        {
            return IssueError(expr.SourceLocation,
                $"got type: {expr.Type.OriginalRepresentation}, expected: {string.Join(", ", expected.Select(e => e.Name))}");
        }

        public Exception MissingNamedTupleEntry(PParser.IdenContext location,
            NamedTupleType namedTuple)
        {
            return IssueError(location,
                $"named tuple type {namedTuple.OriginalRepresentation} has no '{location.GetText()}' field");
        }

        public Exception OutOfBoundsTupleAccess(PParser.IntContext location, TupleType tuple)
        {
            return IssueError(
                location, $"tuple type {tuple.OriginalRepresentation} has no '{location.GetText()}' field");
        }

        public Exception IncomparableTypes(ParserRuleContext location, PLanguageType lhsType, PLanguageType rhsType)
        {
            return IssueError(location,
                $"types {lhsType.OriginalRepresentation} and {rhsType.OriginalRepresentation} are incomparable");
        }

        public Exception MisplacedThis(PParser.PrimitiveContext location)
        {
            return IssueError(location, location.THIS().Symbol, "keyword THIS used outside machine");
        }

        public Exception BinOpTypeMismatch(
            PParser.BinExprContext location,
            PLanguageType lhsType,
            PLanguageType rhsType)
        {
            return IssueError(location,
                location.op,
                $"expected either both float or both int; got {lhsType.OriginalRepresentation} and {rhsType.OriginalRepresentation}");
        }

        public Exception ParseFailure(FileInfo file, string message)
        {
            return new TranslationException($"[{file.Name}] parse error: {message}");
        }

        public Exception EmittedNullEvent(IPExpr evtExpr)
        {
            return IssueError(evtExpr.SourceLocation, "cannot send null events");
        }

        public Exception MissingStartState(Machine machine)
        {
            return IssueError(machine.SourceLocation, $"Value {machine.Name} has no start state");
        }

        public Exception ChangedStateMidTransition(ParserRuleContext location, Function method)
        {
            return IssueError(location,
                $"Method {DeclarationName(method)} is used as a transition function, but might change state here.");
        }

        public Exception NonDeterministicFunctionInSpecMachine(Function machineFunction)
        {
            return IssueError(machineFunction.SourceLocation,
                $"Method {DeclarationName(machineFunction)} is non-deterministic, but used in spec machine.");
        }

        public Exception RelinquishedWithoutOwnership(ILinearRef linearRef)
        {
            return IssueError(linearRef.SourceLocation,
                $"cannot give up ownership of variable {linearRef.Variable.Name} twice");
        }

        public Exception InvalidSwap(ILinearRef linearRef, string message)
        {
            return IssueError(linearRef.SourceLocation,
                $"invalid swap of {linearRef.Variable.Name}. Reason: {message}");
        }

        public Exception UseWithoutOwnership(VariableAccessExpr variable)
        {
            return IssueError(variable.SourceLocation,
                $"used variable {variable.Variable.Name} after a move or during a swap");
        }

        public Exception MovedField(MoveAssignStmt moveAssignStmt)
        {
            return IssueError(moveAssignStmt.SourceLocation, $"attempted to move field {moveAssignStmt.FromVariable}");
        }

        public Exception SwapAssignUnavailable(SwapAssignStmt swapAssignStmt, Variable variable)
        {
            return IssueError(swapAssignStmt.SourceLocation, $"variable {variable.Name} unavailable during swap");
        }

        public Exception InvalidPrintFormat(PParser.PrintStmtContext context, IToken symbol)
        {
            return IssueError(context,
                symbol,
                "Print format placeholders must contain only digits. Escape braces by doubling them.");
        }

        public Exception InvalidStringAssignFormat(PParser.StringAssignStmtContext context, IToken symbol)
        {
            return IssueError(context,
                symbol,
                "String assign format placeholders must contain only digits. Escape braces by doubling them.");
        }

        public Exception InvalidBindExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid bind operation. {message}");
        }

        public Exception NotClosed(ParserRuleContext sourceLocation, string message)
        {
            return IssueError(sourceLocation, $"Module not closed. {message}");
        }

        public Exception NoMain(ParserRuleContext sourceLocation, string message)
        {
            return IssueError(sourceLocation, $"Illegal main machine. {message}");
        }

        public Exception InvalidAssertExpr(ParserRuleContext location, Machine monitor, PEvent illegalEvent)
        {
            return IssueError(location,
                $"invalid assert operation. event {illegalEvent.Name} in observes set of {monitor.Name} is not in the sends set of the module");
        }

        public Exception InvalidAssertExpr(ParserRuleContext location, Machine monitor)
        {
            return IssueError(location,
                $"invalid assert operation. monitor {monitor.Name} already attached in the module");
        }

        public Exception InvalidHideEventExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid hide event operation. {message}");
        }

        public Exception InvalidHideInterfaceExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid hide interface operation. {message}");
        }

        public Exception InvalidRenameExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid rename operation. {message}");
        }

        public Exception InvalidCompositionExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid composition operation. {message}");
        }

        public Exception InternalError(ParserRuleContext location, Exception inner)
        {
            return IssueError(location, inner.Message);
        }

        public Exception TwoStartStates(Machine machine, State state)
        {
            return IssueError(machine.SourceLocation,
                $"machine {machine.Name} has two start states, {machine.StartState.Name} and {state.Name}.");
        }

        public Exception ValueOutOfRange(ParserRuleContext location, string type)
        {
            return IssueError(location, $"value not in range for {type}.");
        }

        public Exception NullTransitionInMonitor(ParserRuleContext location, Machine monitor)
        {
            return IssueError(location, $"cannot transition on null event in monitor {monitor.Name}");
        }

        public Exception IllegalMonitorOperation(ParserRuleContext location, IToken operation, Machine monitor)
        {
            return IssueError(location, operation,
                $"{monitor.Name}: $, $$, this, new, send, announce, receive, and pop are not allowed in monitors");
        }

        public Exception DeferredEventInMonitor(ParserRuleContext location, Machine monitor)
        {
            return IssueError(location, $"{monitor.Name}: Cannot defer events in monitors.");
        }

        public Exception NotAllPathsReturn(Function function)
        {
            return IssueError(function.Body.SourceLocation,
                $"not all paths in function {DeclarationName(function)} return a value!");
        }

        public Exception ExpectedMonitor(ParserRuleContext location, Machine machine)
        {
            return IssueError(location, $"expected a specification machine, but got {machine.Name}");
        }

        public Exception PopInNonVoidFunction(ParserRuleContext context)
        {
            return IssueError(context, "pop only allowed in functions that do not return a value.");
        }

        public Exception PrintStmtLinearArgument(ParserRuleContext argSourceLocation)
        {
            return IssueError(argSourceLocation, "Print is a pure statement and so does not require linear arguments.");
        }

        public Exception StringAssignStmtLinearArgument(ParserRuleContext argSourceLocation)
        {
            return IssueError(argSourceLocation, "String interpolation does not support linear arguments.");
        }

        public Exception DuplicateReceiveCase(ParserRuleContext location, PEvent pEvent)
        {
            return IssueError(location, $"Event {pEvent.Name} appears twice in receive statement argument list");
        }

        public Exception TupleSizeMoreThanEight(ParserRuleContext context)
        {
            return IssueError(context, "tuple or named tuple of size greater than 8 is not supported");
        }

        public Exception BareLoopControlFlow(string stmtName, ParserRuleContext context)
        {
            return IssueError(context, $"Loop control flow statement '{stmtName}' cannot appear outside a loop body");
        }

        private Exception IssueError(ParserRuleContext location, string message)
        {
            return IssueError(location, location.Start, message);
        }

        private Exception IssueError(ParserRuleContext ctx, IToken location, string message)
        {
            return new TranslationException($"[{locationResolver.GetLocation(ctx, location)}] {message}");
        }

        private string DeclarationName(IPDecl method)
        {
            return method.Name.Length > 0 ? method.Name : $"at {locationResolver.GetLocation(method.SourceLocation)}";
        }
    }
}