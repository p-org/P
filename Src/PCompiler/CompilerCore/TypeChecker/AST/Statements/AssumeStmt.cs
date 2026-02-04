using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class AssumeStmt : IPStmt
    {
        public AssumeStmt(ParserRuleContext sourceLocation, IPExpr assumption, IPExpr message)
        {
            SourceLocation = sourceLocation;
            Assumption = assumption;
            Message = message;
        }

        public IPExpr Assumption { get; }
        public IPExpr Message { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}