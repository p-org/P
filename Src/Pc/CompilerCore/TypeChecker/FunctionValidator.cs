using System.Linq;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public static class FunctionValidator
    {
        public static void CheckAllPathsReturn(ITranslationErrorHandler handler, Function function)
        {
            if (!SurelyReturns(function.Body) &&
                !function.Signature.ReturnType.IsSameTypeAs(PrimitiveType.Null))
            {
                throw handler.IssueError(function.Body.SourceLocation,
                                         $"function {function.Name} might not return a value.");
            }
        }

        private static bool SurelyReturns(IPStmt stmt)
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
                default:
                    return false;
            }
        }
    }
}
