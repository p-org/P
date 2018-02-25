using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.States;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class GotoStmt : IPStmt
    {
        public GotoStmt(ParserRuleContext sourceLocation, State state, IPExpr payload)
        {
            SourceLocation = sourceLocation;
            State = state;
            Payload = payload;
        }

        public State State { get; }
        public IPExpr Payload { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
