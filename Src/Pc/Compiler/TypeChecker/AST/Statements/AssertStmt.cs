using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
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
}