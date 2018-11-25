using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
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