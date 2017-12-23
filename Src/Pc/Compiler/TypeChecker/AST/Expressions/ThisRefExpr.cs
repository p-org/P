using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class ThisRefExpr : IPExpr
    {
        public ThisRefExpr(ParserRuleContext sourceLocation, Machine machine)
        {
            SourceLocation = sourceLocation;
            Machine = machine;
        }

        public Machine Machine { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; } = PrimitiveType.Machine;
    }
}
