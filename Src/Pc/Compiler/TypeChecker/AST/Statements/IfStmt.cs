using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class IfStmt : IPStmt
    {
        public IfStmt(IPExpr condition, IPStmt thenBranch, IPStmt elseBranch)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        public IPExpr Condition { get; }
        public IPStmt ThenBranch { get; }
        public IPStmt ElseBranch { get; }
    }
}