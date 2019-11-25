using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Statements;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Plang.Compiler.TypeChecker
{
    public class ControlFlowChecker
    {
        private readonly ITranslationErrorHandler handler;

        private ControlFlowChecker(ITranslationErrorHandler handler)
        {
            this.handler = handler;
        }

        public static void AnalyzeMethods(ITranslationErrorHandler handler, IEnumerable<Function> allFunctions)
        {
            ControlFlowChecker checker = new ControlFlowChecker(handler);
            foreach (Function function in allFunctions)
            {
                checker.CheckFunction(function);
            }
        }

        private void CheckFunction(Function function)
        {
            if (function.IsForeign)
            {
                Debug.Assert(function.Body == null);
                // Skip this function; nothing to check
            }
            else
            {
                Debug.Assert(function.Body != null);
                CheckStmt(function.Body);
            }
        }

        private void CheckStmt(IPStmt stmt)
        {
            switch (stmt)
            {
                case BreakStmt breakStmt:
                    throw handler.BareLoopControlFlow("break", breakStmt.SourceLocation);
                case ContinueStmt continueStmt:
                    throw handler.BareLoopControlFlow("continue", continueStmt.SourceLocation);

                case CompoundStmt compoundStmt:
                    foreach (IPStmt subStmt in compoundStmt.Statements)
                    {
                        CheckStmt(subStmt);
                    }

                    break;

                case IfStmt ifStmt:
                    CheckStmt(ifStmt.ThenBranch);
                    CheckStmt(ifStmt.ElseBranch);
                    break;

                // Any break or continue statements inside this while loop are necessarily safe
                case WhileStmt _:
                    break;

                // None of the following statement types can contain child statements, so we can safely skip them
                case AddStmt _:
                case AnnounceStmt _:
                case AssertStmt _:
                case AssignStmt _:
                case CtorStmt _:
                case FunCallStmt _:
                case GotoStmt _:
                case InsertStmt _:
                case MoveAssignStmt _:
                case NoStmt _:
                case PopStmt _:
                case PrintStmt _:
                case RaiseStmt _:
                case ReceiveStmt _:
                case RemoveStmt _:
                case ReturnStmt _:
                case SendStmt _:
                case StringAssignStmt _:
                case SwapAssignStmt _:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(stmt));
            }
        }
    }
}