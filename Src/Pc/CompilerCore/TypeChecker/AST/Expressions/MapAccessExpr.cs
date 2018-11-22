using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class MapAccessExpr : IPExpr
    {
        public MapAccessExpr(ParserRuleContext sourceLocation, IPExpr mapExpr, IPExpr indexExpr, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            MapExpr = mapExpr;
            IndexExpr = indexExpr;
            Type = type;
        }

        public IPExpr MapExpr { get; }
        public IPExpr IndexExpr { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; }
    }
}