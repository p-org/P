using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class EventRefExpr : IStaticTerm<Event>
    {
        public EventRefExpr(ParserRuleContext sourceLocation, Event value)
        {
            Value = value;
            SourceLocation = sourceLocation;
        }

        public Event Value { get; }

        public PLanguageType Type { get; } = PrimitiveType.Event;
        public ParserRuleContext SourceLocation { get; }
    }
}