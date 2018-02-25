using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class IfStmt : IPStmt
    {
        public IfStmt(ParserRuleContext sourceLocation, IPExpr condition, IPStmt thenBranch, IPStmt elseBranch)
        {
            SourceLocation = sourceLocation;
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        public IPExpr Condition { get; }
        public IPStmt ThenBranch { get; }
        public IPStmt ElseBranch { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
