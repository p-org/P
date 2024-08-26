using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

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

    public static class BinOpExprExtensions
    {
        public static IEnumerable<BinOpExpr> GetEquivalences (this BinOpExpr expr)
        {
            BinOpExpr makeComm(BinOpType op) => new(expr.SourceLocation, op, expr.Rhs, expr.Lhs);
            switch (expr.Operation)
            {
                case BinOpType.Lt:
                    return [makeComm(BinOpType.Gt)];
                case BinOpType.Gt:
                    return [makeComm(BinOpType.Lt)];
                case BinOpType.Le:
                    return [makeComm(BinOpType.Ge)];
                case BinOpType.Ge:
                    return [makeComm(BinOpType.Le)];
                default: return [];
            }
        }

        public static IEnumerable<BinOpExpr> GetContradictions(this BinOpExpr expr)
        {
            BinOpExpr make(BinOpType op) => new(expr.SourceLocation, op, expr.Lhs, expr.Rhs);
            switch (expr.Operation)
            {
                case BinOpType.Lt:
                    return  [make(BinOpType.Eq), make(BinOpType.Gt), make(BinOpType.Ge)];
                case BinOpType.Gt:
                    return [make(BinOpType.Eq), make(BinOpType.Lt), make(BinOpType.Le)];
                case BinOpType.Eq:
                    return [make(BinOpType.Neq), make(BinOpType.Lt), make(BinOpType.Gt)];
                case BinOpType.Le:
                    return [make(BinOpType.Gt)];
                case BinOpType.Ge:
                    return [make(BinOpType.Lt)];
                case BinOpType.Neq:
                    return [make(BinOpType.Eq)];
                default:
                    return [];
            }
        }
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

        public static IEnumerable<IPExpr> GetContradictions(this BinOpType op, IPExpr lhs, IPExpr rhs)
        {
            return new BinOpExpr(null, op, lhs, rhs).GetContradictions();
        }

        public static IEnumerable<IPExpr> GetEquivalences(this BinOpType op, IPExpr lhs, IPExpr rhs)
        {
            return new BinOpExpr(null, op, lhs, rhs).GetEquivalences();
        }

        public static IEnumerable<FunctionProperty> GetProperties(this BinOpType op)
        {
            return op switch
            {
                BinOpType.Mul or BinOpType.Add => [FunctionProperty.Symmetric],
                BinOpType.Le or BinOpType.Ge => [FunctionProperty.Transitive,
                                                FunctionProperty.Reflexive,
                                                FunctionProperty.AntiSymmetric],
                BinOpType.Lt or BinOpType.Gt => [FunctionProperty.Transitive, FunctionProperty.Asymmetric, FunctionProperty.AntiReflexive],
                BinOpType.Neq => [FunctionProperty.Symmetric, FunctionProperty.AntiReflexive],
                BinOpType.Eq => [FunctionProperty.Symmetric, FunctionProperty.Reflexive, FunctionProperty.Transitive],
                BinOpType.And or BinOpType.Or => [FunctionProperty.Symmetric],
                _ => [],
            };
        }
    }
}