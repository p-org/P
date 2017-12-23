using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class FairNondetExpr : IPExpr
    {
        public ParserRuleContext SourceLocation { get; }

        public FairNondetExpr(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }
}
