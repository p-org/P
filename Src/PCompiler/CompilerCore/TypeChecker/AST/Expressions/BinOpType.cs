using System;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public enum BinOpType
    {
        Add,
        Sub,
        Mul,
        Div,
        Mod,
        Eq,
        Neq,
        Lt,
        Le,
        Gt,
        Ge,
        And,
        Or
    }

    public enum BinOpKind
    {
        Boolean,
        Comparison,
        Equality,
        Numeric
    }

    public static class BinOpExtensions
    {
        public static BinOpKind GetKind(this BinOpType op)
        {
            switch (op)
            {
                // Comparison operators
                case BinOpType.Lt:
                case BinOpType.Le:
                case BinOpType.Ge:
                case BinOpType.Gt:
                    return BinOpKind.Comparison;

                // Equality operators
                case BinOpType.Neq:
                case BinOpType.Eq:
                    return BinOpKind.Equality;

                // Arithmetic operators
                case BinOpType.Add:
                case BinOpType.Sub:
                case BinOpType.Mul:
                case BinOpType.Div:
                case BinOpType.Mod:
                    return BinOpKind.Numeric;

                // Boolean operators:
                case BinOpType.And:
                case BinOpType.Or:
                    return BinOpKind.Boolean;

                // This should be dead code.
                default:
                    throw new NotImplementedException(op.ToString());
            }
        }
    }
}