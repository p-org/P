using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class ThisRefExpr : IStaticTerm<Machine>
    {
        public ThisRefExpr(ParserRuleContext sourceLocation, Machine value)
        {
            SourceLocation = sourceLocation;
            Value = value;
        }

        public Machine Value { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; } = PrimitiveType.Machine;
    }
}
