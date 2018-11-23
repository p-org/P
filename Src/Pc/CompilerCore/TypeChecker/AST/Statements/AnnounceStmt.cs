using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class AnnounceStmt : IPStmt
    {
        public AnnounceStmt(ParserRuleContext sourceLocation, IPExpr pEvent, IPExpr payload)
        {
            SourceLocation = sourceLocation;
            PEvent = pEvent;
            Payload = payload;
        }

        public IPExpr PEvent { get; }
        public IPExpr Payload { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}