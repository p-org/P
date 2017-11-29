using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class RaiseStmt : IPStmt
    {
        public RaiseStmt(IPExpr pEvent, IPExpr payload)
        {
            PEvent = pEvent;
            Payload = payload;
        }

        public IPExpr PEvent { get; }
        public IPExpr Payload { get; }
    }
}