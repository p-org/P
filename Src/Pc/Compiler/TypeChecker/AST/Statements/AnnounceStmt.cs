using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class AnnounceStmt : IPStmt
    {
        public AnnounceStmt(IPExpr pEvent, IPExpr payload)
        {
            PEvent = pEvent;
            Payload = payload;
        }

        public IPExpr PEvent { get; }
        public IPExpr Payload { get; }
    }
}