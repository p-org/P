using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class NullLiteralExpr : IStaticTerm
    {
        public NullLiteralExpr(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; } = PrimitiveType.Any;
    }
}