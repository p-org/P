using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class UnaryOpExpr : IPExpr
    {
        public UnaryOpExpr(UnaryOpType operation, IPExpr subExpr)
        {
            Operation = operation;
            SubExpr = subExpr;
            Type = subExpr.Type;
        }

        public UnaryOpType Operation { get; }
        public IPExpr SubExpr { get; }

        public PLanguageType Type { get; }
    }
}
