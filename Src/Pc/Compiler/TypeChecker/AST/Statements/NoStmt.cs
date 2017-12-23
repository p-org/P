using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class NoStmt : IPStmt
    {
        public ParserRuleContext SourceLocation { get; }

        public NoStmt(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }
    }
}