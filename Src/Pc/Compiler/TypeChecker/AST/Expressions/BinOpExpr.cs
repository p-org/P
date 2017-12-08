using System.Diagnostics;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class BinOpExpr : IPExpr
    {
        public BinOpExpr(BinOpType operation, IPExpr lhs, IPExpr rhs)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
            if (IsArithmetic(operation))
            {
                Debug.Assert(Lhs.Type.IsSameTypeAs(Rhs.Type));
                Type = Lhs.Type;
            }
            else
            {
                Type = PrimitiveType.Bool;
            }
        }

        public BinOpType Operation { get; }
        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; }

        private static bool IsArithmetic(BinOpType operation)
        {
            return operation == BinOpType.Add || operation == BinOpType.Sub || operation == BinOpType.Mul ||
                   operation == BinOpType.Div;
        }
    }
}