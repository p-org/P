using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class FloatLiteralExpr : PrimitiveLiteralExpr<double>
    {
        public FloatLiteralExpr(ParserRuleContext sourceLocation, double value)
            : base(sourceLocation, value, PrimitiveType.Float)
        {
        }
    }
}
