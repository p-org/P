using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class SetAccessExpr : IPExpr
    {
        public SetAccessExpr(ParserRuleContext sourceLocation, IPExpr setExpr, IPExpr indexExpr, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            SetExpr = setExpr;
            IndexExpr = indexExpr;
            Type = type;
        }

        public IPExpr SetExpr { get; }
        public IPExpr IndexExpr { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; }
    }
}