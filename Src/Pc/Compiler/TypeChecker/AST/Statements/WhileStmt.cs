using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class WhileStmt : IPStmt
    {
        public WhileStmt(ParserRuleContext sourceLocation, IPExpr condition, IPStmt body)
        {
            SourceLocation = sourceLocation;
            Condition = condition;
            Body = body;
        }

        public IPExpr Condition { get; }
        public IPStmt Body { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
