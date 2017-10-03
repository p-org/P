using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class NamedTupleAccessExpr : IPExpr
    {
        public NamedTupleAccessExpr(IPExpr subExpr, NamedTupleEntry entry)
        {
            SubExpr = subExpr;
            Entry = entry;
        }

        public IPExpr SubExpr { get; }
        public NamedTupleEntry Entry { get; }
        public string FieldName => Entry.Name;
        public PLanguageType Type => Entry.Type;
    }
}
