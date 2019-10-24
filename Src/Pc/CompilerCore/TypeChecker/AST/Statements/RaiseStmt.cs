using Antlr4.Runtime;
using System.Collections.Generic;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class RaiseStmt : IPStmt
    {
        public RaiseStmt(ParserRuleContext sourceLocation, IPExpr pEvent, IReadOnlyList<IPExpr> payload)
        {
            SourceLocation = sourceLocation;
            PEvent = pEvent;
            Payload = payload;
        }

        public IPExpr PEvent { get; }
        public IReadOnlyList<IPExpr> Payload { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}