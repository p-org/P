using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public enum UnaryOpType
    {
        Negate,
        Not
    }

    public static class UnaryOpExprExtensions
    {
        public static IEnumerable<IPExpr> GetContradictions(this UnaryOpExpr expr)
        {
            switch (expr.Operation)
            {
                case UnaryOpType.Not:
                    return [expr.SubExpr];
                default: return [];
            }
        }

        public static IEnumerable<IPExpr> GetNegation(this UnaryOpExpr expr)
        {
            return GetContradictions(expr);
        }
    }
}