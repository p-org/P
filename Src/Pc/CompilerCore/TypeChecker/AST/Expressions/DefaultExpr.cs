using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class DefaultExpr : IPExpr
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