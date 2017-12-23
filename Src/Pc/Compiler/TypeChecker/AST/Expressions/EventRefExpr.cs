using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class EventRefExpr : IPExpr
    {
        public EventRefExpr(ParserRuleContext sourceLocation, PEvent pEvent)
        {
            PEvent = pEvent;
            SourceLocation = sourceLocation;
        }

        public PEvent PEvent { get; }

        public PLanguageType Type { get; } = PrimitiveType.Event;
        public ParserRuleContext SourceLocation { get; }
    }
}
