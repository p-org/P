using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class PrintStmt : IPStmt
    {
        public PrintStmt(ParserRuleContext sourceLocation, IPExpr message)
        {
            SourceLocation = sourceLocation;
            Message = message;
        }

        public IPExpr Message { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}