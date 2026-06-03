using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler
{
    public class DefaultTranslationErrorHandler : ITranslationErrorHandler
    {
        private readonly ILocationResolver locationResolver;

        public DefaultTranslationErrorHandler(ILocationResolver locationResolver)
            : this(locationResolver, new DefaultDiagnosticCollector())
        {
        }

        // Throws on null so we never silently break the invariant that
        // ICompilerConfiguration.Diagnostics and Handler.Diagnostics are the
        // SAME instance. Callers that want a default collector should use the
        // 1-arg overload above.
        public DefaultTranslationErrorHandler(ILocationResolver locationResolver, IDiagnosticCollector diagnostics)
        {
            this.locationResolver = locationResolver;
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IDiagnosticCollector Diagnostics { get; }

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
        
        public Exception RedeclareGlobalParam(ParserRuleContext location, IPDecl duplicate, IPDecl existing)
        {
            return IssueError(location,
                $"'{duplicate.Name}' redeclares global param '{existing.Name}' (originally declared at {locationResolver.GetLocation(existing.SourceLocation)})");
        }

        public Exception UndeclaredGlobalParam(ParserRuleContext location, string name)
        {
            return IssueError(location,
                $"undeclared global param '{name}'");
        }

        public Exception ModifyGlobalParam(ParserRuleContext location, IPDecl existing)
        {
            return IssueError(location,
                $"cannot modify global param '{existing.Name}' (declared at {locationResolver.GetLocation(existing.SourceLocation)})");
        }

        public Exception InvalidTwise(ParserRuleContext location, IPDecl testDecl, string errMsg)
        {
            return IssueError(location,
                $"invalid twise number at {locationResolver.GetLocation(testDecl.SourceLocation)}: {errMsg}");
        }

        public Exception CyclicProof(ParserRuleContext location, ProofCommand cmd)
        {
            return IssueError(location,
                $"proof commands form a cycle (starting at {locationResolver.GetLocation(cmd.SourceLocation)})");
        }
        public Exception IncorrectArgumentCount(ParserRuleContext location, int actualCount, int expectedCount)
        {
            return IssueError(location,
                $"goto, function or constructor call expected {expectedCount} arguments, got {actualCount}");
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
            return IssueError(location, "coerce expects an interface, int, or float type");
        }

        public Exception NoMainOrTestCase(string message)
        {
            return new TranslationException(message);
        }

        public Exception IllegalInterfaceCoerce(ParserRuleContext context, PLanguageType oldType, PLanguageType newType)
        {
            var outlierEvent =
                newType.AllowedPermissions.Value.First(x => !oldType.AllowedPermissions.Value.Contains(x));
            return IssueError(context,
                $"illegal coerce: permissions of '{oldType.OriginalRepresentation}' are not a superset of '{newType.OriginalRepresentation}' (e.g., event '{outlierEvent.Name}')");
        }

        public Exception StaticFunctionNotAllowedAsHandler(ParserRuleContext funName, string name)
        {
            return IssueError(funName,
                $"function '{name}' cannot be used as a handler: global and foreign functions are not allowed as handlers");
        }

        public Exception TypeMismatch(ParserRuleContext location, PLanguageType actual, params PLanguageType[] expected)
        {
            return IssueError(location,
                $"got type: {FormatType(actual)}, expected: {string.Join("; ", expected.Select(FormatType))}");
        }

        public Exception TypeMismatch(IPExpr expr, params TypeKind[] expected)
        {
            return IssueError(expr.SourceLocation,
                $"got type: {FormatType(expr.Type)}, expected: {string.Join(", ", expected.Select(e => e.Name))}");
        }

        // PrimitiveType.Null's OriginalRepresentation is the literal "null", which
        // shows up in diagnostics like `expected: null` when an event has no
        // payload or a function returns no value. Readers commonly misread that
        // as the null literal; "(no value)" describes the actual semantics.
        private static string FormatType(PLanguageType t)
        {
            var rep = t.OriginalRepresentation;
            return rep == "null" ? "(no value)" : rep;
        }

        public Exception MissingNamedTupleEntry(PParser.IdenContext location,
            NamedTupleType namedTuple)
        {
            return IssueError(location,
                $"named tuple type {namedTuple.OriginalRepresentation} has no '{location.GetText()}' field");
        }

        public Exception MissingMachineField(PParser.IdenContext location, Machine machine)
        {
            return IssueError(location,
                $"machine '{machine.Name}' has no field '{location.GetText()}'");
        }
        public Exception MissingEventField(PParser.IdenContext location, Event pevent)
        {
            return IssueError(location,
                $"payload of event '{pevent.Name}' has no field '{location.GetText()}'");
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
            return IssueError(location, location.THIS().Symbol, "keyword 'this' used outside a machine");
        }

        public Exception BinOpTypeMismatch(
            PParser.BinExprContext location,
            PLanguageType lhsType,
            PLanguageType rhsType)
        {
            return IssueError(location,
                location.op,
                $"operator '{location.op.Text}' requires both operands to be int or both float; got {lhsType.OriginalRepresentation} and {rhsType.OriginalRepresentation}");
        }

        public Exception MoreThanOneParameterForHandlers(ParserRuleContext sourceLocation, int count)
        {
            return IssueError(sourceLocation,
                $"entry, exit, do, and goto handlers take at most 1 parameter; function expects {count}");
        }

        public Exception ParseFailure(FileInfo file, string message)
        {
            return new TranslationException($"[{file.Name}] parse error: {message}");
        }

        public Exception IllegalChooseSubExprType(PParser.ChooseExprContext context, PLanguageType subExprType)
        {
            return IssueError(context,
                $"'choose' expects int (max value) or a collection (seq, set, or map); got type {subExprType}");
        }

        public Exception IllegalChooseSubExprValue(PParser.ChooseExprContext context, int numChoices)
        {
            return IssueError(context,
                $"'choose' expects at most 10000 choices; got {numChoices}");
        }

        public Exception IllegalFunctionUsedInSpecMachine(Function function, Machine callerOwner)
        {
            return IssueError(function.SourceLocation,
                $"function '{DeclarationName(function)}' is non-deterministic or has side effects (new/send/receive/announce); cannot be called from spec monitor '{DeclarationName(callerOwner)}'");
        }

        public Exception EmittedNullEvent(IPExpr evtExpr)
        {
            return IssueError(evtExpr.SourceLocation, "cannot send null events");
        }

        public Exception MissingStartState(Machine machine)
        {
            return IssueError(machine.SourceLocation, $"machine '{machine.Name}' has no start state");
        }

        public Exception ChangedStateMidTransition(ParserRuleContext location, Function method)
        {
            return IssueError(location,
                $"function '{DeclarationName(method)}' is used as a transition handler but might change state here");
        }

        public Exception InvalidPrintFormat(PParser.PrintStmtContext context, IToken symbol)
        {
            return IssueError(context,
                symbol,
                "print format placeholders must contain only digits; escape braces by doubling them");
        }

        public Exception InvalidStringExprFormat(PParser.FormatedStringContext context, IToken symbol)
        {
            return IssueError(context,
                symbol,
                "string format placeholders must contain only digits; escape braces by doubling them");
        }

        public Exception InvalidBindExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid bind: {message}");
        }

        public Exception NotClosed(ParserRuleContext sourceLocation, string message)
        {
            return IssueError(sourceLocation, $"module not closed: {message}");
        }

        public Exception NoMain(ParserRuleContext sourceLocation, string message)
        {
            return IssueError(sourceLocation, $"illegal main machine: {message}");
        }

        public Exception InvalidAssertExpr(ParserRuleContext location, Machine monitor, Event illegalEvent)
        {
            return IssueError(location,
                $"invalid assert: event '{illegalEvent.Name}' in observes set of '{monitor.Name}' is not in the sends set of the module");
        }

        public Exception InvalidAssertExpr(ParserRuleContext location, Machine monitor)
        {
            return IssueError(location,
                $"invalid assert: spec monitor '{monitor.Name}' is already attached in the module");
        }

        public Exception InvalidHideEventExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid hide event: {message}");
        }

        public Exception InvalidHideInterfaceExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid hide interface: {message}");
        }

        public Exception InvalidRenameExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid rename: {message}");
        }

        public Exception InvalidCompositionExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid composition: {message}");
        }

        public Exception InternalError(ParserRuleContext location, Exception inner)
        {
            return IssueError(location, "internal compiler error (please report): " + inner.Message);
        }

        public Exception TwoStartStates(Machine machine, State state)
        {
            return IssueError(machine.SourceLocation,
                $"machine '{machine.Name}' has two start states: '{machine.StartState.Name}' and '{state.Name}'");
        }

        public Exception ValueOutOfRange(ParserRuleContext location, string type)
        {
            return IssueError(location, $"value is out of range for type '{type}'");
        }

        public Exception NullTransitionInMonitor(ParserRuleContext location, Machine monitor)
        {
            return IssueError(location, $"cannot transition on null event in spec monitor '{monitor.Name}'");
        }

        public Exception IllegalMonitorOperation(ParserRuleContext location, IToken operation, Machine monitor)
        {
            return IssueError(location, operation,
                $"'$', '$$', 'this', 'new', 'send', 'announce', 'receive', and 'pop' are not allowed in spec monitor '{monitor.Name}'");
        }

        public Exception DeferredEventInMonitor(ParserRuleContext location, Machine monitor)
        {
            return IssueError(location, $"events cannot be deferred in spec monitor '{monitor.Name}'");
        }

        public Exception NotAllPathsReturn(Function function)
        {
            return IssueError(function.Body.SourceLocation,
                $"not all paths in function '{DeclarationName(function)}' return a value");
        }

        public Exception ExpectedMonitor(ParserRuleContext location, Machine machine)
        {
            return IssueError(location, $"expected a spec monitor; got '{machine.Name}'");
        }

        public Exception RaiseEventInNonVoidFunction(ParserRuleContext context)
        {
            return IssueError(context, "'raise' is only allowed in functions that do not return a value");
        }

        public Exception ChangeStateInNonVoidFunction(ParserRuleContext context)
        {
            return IssueError(context, "state changes are only allowed in functions that do not return a value");
        }

        public Exception PrintStmtLinearArgument(ParserRuleContext argSourceLocation)
        {
            return IssueError(argSourceLocation, "'print' is a pure statement and does not accept linear arguments");
        }

        public Exception StringAssignStmtLinearArgument(ParserRuleContext argSourceLocation)
        {
            return IssueError(argSourceLocation, "string interpolation does not support linear arguments");
        }

        public Exception DuplicateReceiveCase(ParserRuleContext location, Event pEvent)
        {
            return IssueError(location, $"event '{pEvent.Name}' appears twice in receive statement");
        }

        public Exception BareLoopControlFlow(string stmtName, ParserRuleContext context)
        {
            return IssueError(context, $"'{stmtName}' cannot appear outside a loop body");
        }

        public Exception ExitFunctionCannotTakeParameters(ParserRuleContext sourceLocation, int count)
        {
            return IssueError(sourceLocation,
                $"exit handlers cannot take parameters; function expects {count}");
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

        public string SpecObservesSetIncompleteWarning(ParserRuleContext ctx, Event ev, Machine machine)
        {
            return
                $"[Warning:] [{locationResolver.GetLocation(ctx, ctx.start)}] event '{ev.Name}' is not in the observes list of spec monitor '{machine.Name}'; the handler will never be triggered";
        }

        public Exception DuplicateBindings(ParserRuleContext ctx, Interface @interface)
        {
            return IssueError(ctx, $"interface or machine '{@interface.Name}' is mentioned or bound multiple times in the module");
        }
    }
}