using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker
{
    public static class FunctionValidator
    {
        public static void CheckAllPathsReturn(ITranslationErrorHandler handler, Function function)
        {
            if (function.IsForeign)
            {
                return;
            }

            if (!SurelyReturns(function.Body) &&
                !function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                throw handler.NotAllPathsReturn(function);
            }
        }

        public static bool SurelyReturns(IPStmt stmt)
        {
            switch (stmt)
            {
                case CompoundStmt compoundStmt:
                    return compoundStmt.Statements.Any(SurelyReturns);

                case IfStmt ifStmt:
                    return SurelyReturns(ifStmt.ThenBranch) && SurelyReturns(ifStmt.ElseBranch);

                case ReturnStmt _:
                    return true;

                case AssertStmt assertStmt
                    when (assertStmt.Assertion as BoolLiteralExpr)?.Value == false:
                    return true;

                case GotoStmt _:
                    return true;

                case RaiseStmt _:
                    return true;

                case ReceiveStmt receive:
                    return receive.Cases.Values.All(fn => SurelyReturns(fn.Body));

                default:
                    return false;
            }
        }
    }
}