using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class NamedTupleAccessExpr : IPExpr
    {
        public NamedTupleAccessExpr(ParserRuleContext sourceLocation, IPExpr subExpr, NamedTupleEntry entry)
        {
            SourceLocation = sourceLocation;
            SubExpr = subExpr;
            Entry = entry;
        }

        public ParserRuleContext SourceLocation { get; }
        public IPExpr SubExpr { get; }
        public NamedTupleEntry Entry { get; }
        public string FieldName => Entry.Name;
        public PLanguageType Type => Entry.Type;
    }
}
