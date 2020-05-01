using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class ContinueStmt : IPStmt
    {
        public ContinueStmt(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public ParserRuleContext SourceLocation { get; }
    }
}