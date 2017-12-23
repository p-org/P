using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Expressions;

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

        public ParserRuleContext SourceLocation { get; }
        public IPExpr Condition { get; }
        public IPStmt ThenBranch { get; }
        public IPStmt ElseBranch { get; }
    }
}