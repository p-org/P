using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class AnnounceStmt : IPStmt
    {
        public AnnounceStmt(ParserRuleContext sourceLocation, IPExpr pEvent, IPExpr payload)
        {
            SourceLocation = sourceLocation;
            Event = pEvent;
            Payload = payload;
        }

        public IPExpr Event { get; }
        public IPExpr Payload { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}