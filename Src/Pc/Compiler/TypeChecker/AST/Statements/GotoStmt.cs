using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.States;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class GotoStmt : IPStmt
    {
        public GotoStmt(State state, IPExpr payload)
        {
            State = state;
            Payload = payload;
        }

        public State State { get; }
        public IPExpr Payload { get; }
    }
}