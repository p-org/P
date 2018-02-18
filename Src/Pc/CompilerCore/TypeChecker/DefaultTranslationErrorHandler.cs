using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public class DefaultTranslationErrorHandler : ITranslationErrorHandler
    {
        public void IssueWarning(ParserRuleContext location, string message)
        {
            compilerOutput.WriteMessage($"[{GetLocation(location)}] {message}", SeverityKind.Warning);
        }

        public Exception IssueError(ParserRuleContext location, string message)
        {
            return IssueError(location, location.Start, message);
        }

        public Exception DuplicateEnumValue(PParser.NumberedEnumElemContext location, PEnum pEnum)
        {
            return IssueError(location,
                              $"enum element {location.name.GetText()} in {pEnum.Name} duplicates previous value");
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
            return IssueError(location, $"event handler in state '{state.Name}' duplicates handler at {GetLocation(existingAction.SourceLocation)}");
        }

        public Exception DuplicateStateExitHandler(ParserRuleContext location, Function existingHandler, State state)
        {
            return IssueError(location,
                              $"exit handler in state '{state.Name}' duplicates exit handler at {GetLocation(existingHandler.SourceLocation)}");
        }

        public Exception DuplicateStateEntry(ParserRuleContext location, Function existingHandler, State state)
        {
            return IssueError(location,
                              $"entry handler in state '{state.Name}' duplicates entry handler at {GetLocation(existingHandler.SourceLocation)}");
        }

        public Exception DuplicateDeclaration(ParserRuleContext location, IPDecl duplicate, IPDecl existing)
        {
            return IssueError(location,
                              $"'{duplicate.Name}' duplicates declaration '{existing.Name}' at {GetLocation(existing.SourceLocation)}");
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

        public Exception InternalError(ParserRuleContext location, string message)
        {
            return IssueError(location, $"internal error: {message}");
        }

        public Exception MissingStartState(Machine machine)
        {
            return IssueError(machine.SourceLocation, $"Machine {machine.Name} has no start state");
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

        public Exception SwappedField(SwapAssignStmt swapAssignStmt, Variable variable)
        {
            return IssueError(swapAssignStmt.SourceLocation, $"cannot swap field {variable.Name}");
        }

        public Exception InvalidPrintFormat(PParser.PrintStmtContext context, IToken symbol)
        {
            return IssueError(context,
                              symbol,
                              "Print format placeholders must contain only digits. Escape braces by doubling them.");
        }

        public Exception IssueError(ParserRuleContext ctx, IToken location, string message)
        {
            return new TranslationException($"[{GetLocation(ctx, location)}] {message}");
        }

        private string DeclarationName(IPDecl method)
        {
            return method.Name.Length > 0 ? method.Name : $"at {GetLocation(method.SourceLocation)}";
        }

        public Exception InvalidBindExpr(ParserRuleContext location, string message)
        {
            return IssueError(location, $"invalid bind operation. {message}");
        }

        Exception InvalidAssertExpr(ParserRuleContext location, Machine monitor, PEvent illegalEvent)
        {
            return IssueError(location, $"invalid assert operation. event {illegalEvent.Name} in observes set of {monitor.Name} is not in the sends set of the module");
        }
        #region Internal book keeping

        private readonly ParseTreeProperty<FileInfo> originalFiles;
        private readonly ICompilerOutput compilerOutput;

        public DefaultTranslationErrorHandler(ParseTreeProperty<FileInfo> originalFiles)
            : this(originalFiles, new StandardOutput())
        {
        }

        public DefaultTranslationErrorHandler(ParseTreeProperty<FileInfo> originalFiles, ICompilerOutput compilerOutput)
        {
            this.originalFiles = originalFiles;
            this.compilerOutput = compilerOutput;
        }

        private class Location
        {
            public int Line { get; set; }
            public int Column { get; set; }
            public FileInfo File { get; set; }

            public override string ToString()
            {
                return File == null ? throw new ArgumentException() : $"{File.Name}:{Line},{Column}";
            }
        }

        private Location GetLocation(ParserRuleContext decl)
        {
            if (decl == null)
            {
                return new Location
                {
                    Line = -1,
                    Column = -1,
                    File = null
                };
            }

            return new Location
            {
                Line = decl.Start.Line,
                Column = decl.Start.Column + 1,
                File = originalFiles.Get(GetRoot(decl))
            };
        }

        private Location GetLocation(IParseTree ctx, IToken tok)
        {
            if (ctx == null || tok == null)
            {
                return new Location
                {
                    Line = -1,
                    Column = -1,
                    File = null
                };
            }

            return new Location
            {
                Line = tok.Line,
                Column = tok.Column + 1,
                File = originalFiles.Get(GetRoot(ctx))
            };
        }

        private static IParseTree GetRoot(IParseTree node)
        {
            while (node?.Parent != null)
            {
                node = node.Parent;
            }

            return node;
        }

        #endregion
    }
}
