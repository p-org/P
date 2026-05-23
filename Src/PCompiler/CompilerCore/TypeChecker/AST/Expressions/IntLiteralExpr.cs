using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class IntLiteralExpr : PrimitiveLiteralExpr<int>
    {
        public IntLiteralExpr(ParserRuleContext sourceLocation, int value)
            : base(sourceLocation, value, PrimitiveType.Int)
        {
        }

        public IntLiteralExpr(int value)
            : this(ParserRuleContext.EmptyContext, value)
        {
        }
    }
}
