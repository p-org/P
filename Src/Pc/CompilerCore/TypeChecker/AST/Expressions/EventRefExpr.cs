using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class EventRefExpr : IStaticTerm<PEvent>
    {
        public EventRefExpr(ParserRuleContext sourceLocation, PEvent value)
        {
            Value = value;
            SourceLocation = sourceLocation;
        }

        public PEvent Value { get; }

        public PLanguageType Type { get; } = PrimitiveType.Event;
        public ParserRuleContext SourceLocation { get; }
    }
}
