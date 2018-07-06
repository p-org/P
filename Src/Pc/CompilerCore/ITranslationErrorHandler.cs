using System;
using System.IO;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc
{
    public interface ITranslationErrorHandler
    {
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
        Exception TypeMismatch(IPExpr expr, params TypeKind[] expected);
        Exception MissingNamedTupleEntry(PParser.IdenContext location, NamedTupleType namedTuple);
        Exception OutOfBoundsTupleAccess(PParser.IntContext location, TupleType tuple);
        Exception IncomparableTypes(ParserRuleContext location, PLanguageType lhsType, PLanguageType rhsType);
        Exception MisplacedThis(PParser.PrimitiveContext location);
        Exception BinOpTypeMismatch(PParser.BinExprContext location, PLanguageType lhsType, PLanguageType rhsType);
        Exception ParseFailure(FileInfo file, string message);
        Exception EmittedNullEvent(IPExpr evtExpr);
        Exception MissingStartState(Machine machine);
        Exception ChangedStateMidTransition(ParserRuleContext location, Function method);
        Exception NonDeterministicFunctionInSpecMachine(Function machineFunction);
        Exception RelinquishedWithoutOwnership(ILinearRef linearRef);
        Exception InvalidSwap(ILinearRef linearRef, string message);
        Exception UseWithoutOwnership(VariableAccessExpr variable);
        Exception MovedField(MoveAssignStmt moveAssignStmt);
        Exception SwapAssignUnavailable(SwapAssignStmt swapAssignStmt, Variable variable);
        Exception InvalidPrintFormat(PParser.PrintStmtContext context, IToken symbol);

        // module system related
        Exception InvalidBindExpr(ParserRuleContext location, string message);
        Exception InvalidAssertExpr(ParserRuleContext location, Machine monitor, PEvent illegalEvent);
        Exception InvalidAssertExpr(ParserRuleContext location, Machine monitor);
        Exception InvalidHideEventExpr(ParserRuleContext location, string message);
        Exception InvalidHideInterfaceExpr(ParserRuleContext location, string message);
        Exception InvalidRenameExpr(ParserRuleContext location, string message);
        Exception InvalidCompositionExpr(ParserRuleContext location, string message);

        // General errors
        Exception InternalError(ParserRuleContext location, Exception inner);
        Exception TwoStartStates(Machine machine, State state);
        Exception ValueOutOfRange(ParserRuleContext location, string type);
        Exception NullTransitionInMonitor(ParserRuleContext location, Machine monitor);
        Exception IllegalMonitorOperation(ParserRuleContext location, IToken operation, Machine monitor);
        Exception DeferredEventInMonitor(ParserRuleContext location, Machine monitor);
        Exception NotAllPathsReturn(Function function);
        Exception ExpectedMonitor(ParserRuleContext location, Machine machine);
        Exception PopInNonVoidFunction(ParserRuleContext context);
        Exception PrintStmtLinearArgument(ParserRuleContext argSourceLocation);
        Exception DuplicateReceiveCase(ParserRuleContext location, PEvent pEvent);
    }
}