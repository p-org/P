using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class AnnounceStmt : IPStmt
    {
        public AnnounceStmt(ParserRuleContext sourceLocation, IPExpr pEvent, IPExpr payload)
        {
            SourceLocation = sourceLocation;
            PEvent = pEvent;
            Payload = payload;
        }

        public ParserRuleContext SourceLocation { get; }
        public IPExpr PEvent { get; }
        public IPExpr Payload { get; }
    }
}