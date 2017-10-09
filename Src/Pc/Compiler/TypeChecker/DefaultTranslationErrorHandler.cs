using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public class DefaultTranslationErrorHandler : ITranslationErrorHandler
    {
        public void IssueWarning(ParserRuleContext location, string message)
        {
            Console.Error.WriteLine($"[{GetLocation(location)}] {message}");
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
            return IssueError(location, $"event handler in state '{state.Name}' duplicates another handler");
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

        public Exception TypeMismatch(ParserRuleContext location, PLanguageType actual, params TypeKind[] expected)
        {
            return IssueError(location,
                              $"got type: {actual.OriginalRepresentation}, expected: {string.Join(", ", expected.Select(e => e.Name))}");
        }

        public Exception MissingNamedTupleEntry(
            ParserRuleContext location,
            string missingField,
            NamedTupleType namedTuple)
        {
            return IssueError(location,
                              $"named tuple type {namedTuple.OriginalRepresentation} has no '{missingField}' field");
        }

        public Exception OutOfBoundsTupleAccess(ParserRuleContext location, int field, TupleType tuple)
        {
            return IssueError(location, $"tuple type {tuple.OriginalRepresentation} has no '{field}' field");
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

        public Exception EmittedNullEvent(ParserRuleContext location)
        {
            return IssueError(location, "cannot send null events");
        }

        public Exception IssueError(ParserRuleContext ctx, IToken location, string message)
        {
            return new TranslationException($"[{GetLocation(ctx, location)}] {message}");
        }

        #region Internal book keeping

        private readonly ParseTreeProperty<FileInfo> originalFiles;

        public DefaultTranslationErrorHandler(ParseTreeProperty<FileInfo> originalFiles)
        {
            this.originalFiles = originalFiles;
        }

        private class Location
        {
            public int Line { get; set; }
            public int Column { get; set; }
            public FileInfo File { get; set; }

            public override string ToString() { return File == null ? "<built-in>" : $"{File.Name}:{Line},{Column}"; }
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

        private Location GetLocation(ParserRuleContext ctx, IToken tok)
        {
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
