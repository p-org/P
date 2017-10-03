using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class MapAccessExpr : IPExpr
    {
        public MapAccessExpr(IPExpr mapExpr, IPExpr indexExpr, PLanguageType type)
        {
            MapExpr = mapExpr;
            IndexExpr = indexExpr;
            Type = type;
        }

        public IPExpr MapExpr { get; }
        public IPExpr IndexExpr { get; }
        public PLanguageType Type { get; }
    }
}
