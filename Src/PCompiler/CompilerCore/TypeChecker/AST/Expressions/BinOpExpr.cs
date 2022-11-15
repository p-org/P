using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class BinOpExpr : IPExpr
    {
        public BinOpExpr(ParserRuleContext sourceLocation, BinOpType operation, IPExpr lhs, IPExpr rhs)
        {
            SourceLocation = sourceLocation;
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

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }

        private static bool IsArithmetic(BinOpType operation)
        {
            return operation == BinOpType.Add || operation == BinOpType.Sub || operation == BinOpType.Mul ||
                   operation == BinOpType.Div || operation == BinOpType.Mod;
        }
    }
}