using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class PopStmt : IPStmt
    {
        public ParserRuleContext SourceLocation { get; }

        public PopStmt(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }
    }
}