using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class NondetExpr : IPExpr
    {
        public NondetExpr(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }
}