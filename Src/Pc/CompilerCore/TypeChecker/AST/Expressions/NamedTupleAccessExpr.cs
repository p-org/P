using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class NamedTupleAccessExpr : IPExpr
    {
        public NamedTupleAccessExpr(ParserRuleContext sourceLocation, IPExpr subExpr, NamedTupleEntry entry)
        {
            SourceLocation = sourceLocation;
            SubExpr = subExpr;
            Entry = entry;
        }

        public IPExpr SubExpr { get; }
        public NamedTupleEntry Entry { get; }
        public string FieldName => Entry.Name;

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type => Entry.Type;
    }
}