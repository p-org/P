using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class IfStmt : IPStmt
    {
        public IfStmt(ParserRuleContext sourceLocation, IPExpr condition, IPStmt thenBranch, IPStmt elseBranch)
        {
            SourceLocation = sourceLocation;
            Condition = condition;
            ThenBranch = new CompoundStmt(thenBranch);
            ElseBranch = elseBranch == null ? null : new CompoundStmt(elseBranch);
        }

        public IPExpr Condition { get; }
        public CompoundStmt ThenBranch { get; }
        public CompoundStmt ElseBranch { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
