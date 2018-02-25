using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class RaiseStmt : IPStmt
    {
        public RaiseStmt(ParserRuleContext sourceLocation, IPExpr pEvent, IPExpr[] payload)
        {
            SourceLocation = sourceLocation;
            PEvent = pEvent;
            Payload = payload;
        }

        public IPExpr PEvent { get; }
        public IPExpr[] Payload { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
