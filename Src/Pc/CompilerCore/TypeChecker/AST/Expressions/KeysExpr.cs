using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class KeysExpr : IPExpr
    {
        public KeysExpr(ParserRuleContext sourceLocation, IPExpr expr, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            Expr = expr;
            Type = type;
        }

        public IPExpr Expr { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; }
    }
}