using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class CoerceExpr : IPExpr
    {
        public CoerceExpr(ParserRuleContext sourceLocation, IPExpr subExpr, PLanguageType newType)
        {
            SourceLocation = sourceLocation;
            SubExpr = subExpr;
            NewType = newType;
        }

        public IPExpr SubExpr { get; }
        public PLanguageType NewType { get; }

        public PLanguageType Type => NewType;
        public ParserRuleContext SourceLocation { get; }
    }
}