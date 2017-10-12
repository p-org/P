using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class AnnounceStmt : IPStmt
    {
        public AnnounceStmt(PEvent pEvent, IPExpr payload)
        {
            PEvent = pEvent;
            Payload = payload;
        }

        public PEvent PEvent { get; }
        public IPExpr Payload { get; }
    }
}