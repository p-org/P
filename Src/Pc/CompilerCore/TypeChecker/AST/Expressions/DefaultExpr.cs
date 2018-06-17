using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class DefaultExpr : IStaticTerm
    {
        public DefaultExpr(ParserRuleContext sourceLocation, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            Type = type;
        }

        public PLanguageType Type { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
