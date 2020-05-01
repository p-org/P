using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.States;

namespace Plang.Compiler.TypeChecker.AST.Statements
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