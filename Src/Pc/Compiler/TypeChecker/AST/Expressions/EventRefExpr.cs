using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class EventRefExpr : IPExpr
    {
        public EventRefExpr(PEvent pEvent) { PEvent = pEvent; }

        public PEvent PEvent { get; }

        public PLanguageType Type { get; } = PrimitiveType.Event;
    }
}
