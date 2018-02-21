using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class AssertStmt : IPStmt
    {
        public AssertStmt(ParserRuleContext sourceLocation, IPExpr assertion, string message)
        {
            SourceLocation = sourceLocation;
            Assertion = assertion;
            Message = message;
        }

        public IPExpr Assertion { get; }
        public string Message { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
