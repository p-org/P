using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class WhileStmt : IPStmt
    {
        public WhileStmt(IPExpr condition, IPStmt body)
        {
            Condition = condition;
            Body = body;
        }

        public IPExpr Condition { get; }
        public IPStmt Body { get; }
    }
}