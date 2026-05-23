using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class BoolLiteralExpr : PrimitiveLiteralExpr<bool>
    {
        public BoolLiteralExpr(ParserRuleContext sourceLocation, bool value)
            : base(sourceLocation, value, PrimitiveType.Bool)
        {
        }

        public BoolLiteralExpr(bool value)
            : this(ParserRuleContext.EmptyContext, value)
        {
        }
    }
}
