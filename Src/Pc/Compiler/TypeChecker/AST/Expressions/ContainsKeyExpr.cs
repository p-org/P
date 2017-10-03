using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class ContainsKeyExpr : IPExpr
    {
        public ContainsKeyExpr(IPExpr key, IPExpr map)
        {
            Key = key;
            Map = map;
        }

        public IPExpr Key { get; }
        public IPExpr Map { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }
}
