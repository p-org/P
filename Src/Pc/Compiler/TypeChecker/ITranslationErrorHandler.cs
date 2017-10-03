using System;
using System.IO;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public interface ITranslationErrorHandler
    {
        void IssueWarning(ParserRuleContext location, string message);
        Exception IssueError(ParserRuleContext location, string message);

        Exception DuplicateEnumValue(PParser.NumberedEnumElemContext location, PEnum pEnum);

        Exception DuplicateStartState(
            ParserRuleContext location,
            State duplicateStart,
            State originalStart,
            Machine machine);

        Exception DuplicateEventAction(ParserRuleContext location, IStateAction existingAction, State state);
        Exception DuplicateStateExitHandler(ParserRuleContext location, Function existingHandler, State state);
        Exception DuplicateStateEntry(ParserRuleContext location, Function existingHandler, State state);
        Exception DuplicateDeclaration(ParserRuleContext location, IPDecl duplicate, IPDecl existing);
        Exception IncorrectArgumentCount(ParserRuleContext location, int actualCount, int expectedCount);
        Exception MissingDeclaration(ParserRuleContext location, string declarationKind, string missingName);
        Exception CircularTypeDef(ParserRuleContext location, TypeDef typeDef);
        Exception DuplicateNamedTupleEntry(ParserRuleContext location, string duplicateName);
        Exception TypeMismatch(ParserRuleContext location, PLanguageType actual, params PLanguageType[] expected);
        Exception TypeMismatch(ParserRuleContext location, PLanguageType actual, params TypeKind[] expected);
        Exception MissingNamedTupleEntry(ParserRuleContext location, string missingField, NamedTupleType namedTuple);
        Exception OutOfBoundsTupleAccess(ParserRuleContext location, int field, TupleType tuple);
        Exception IncomparableTypes(ParserRuleContext location, PLanguageType lhsType, PLanguageType rhsType);
        Exception MisplacedThis(PParser.PrimitiveContext location);
        Exception BinOpTypeMismatch(PParser.BinExprContext location, PLanguageType lhsType, PLanguageType rhsType);
        Exception ParseFailure(FileInfo file);
        Exception EmittedNullEvent(ParserRuleContext location);
    }
}
