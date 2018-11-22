using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class UnaryOpExpr : IPExpr
    {
        public UnaryOpExpr(ParserRuleContext sourceLocation, UnaryOpType operation, IPExpr subExpr)
        {
            SourceLocation = sourceLocation;
            Operation = operation;
            SubExpr = subExpr;
            Type = subExpr.Type;
        }

        public UnaryOpType Operation { get; }
        public IPExpr SubExpr { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}