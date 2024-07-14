using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class EventAccessExpr : IPExpr
    {
        public EventAccessExpr(ParserRuleContext sourceLocation, PEvent pevent, IPExpr subExpr, NamedTupleEntry entry)
        {
            SourceLocation = sourceLocation;
            PEvent = pevent;
            SubExpr = subExpr;
            Entry = entry;
        }
        
        public PEvent PEvent { get; }
        public IPExpr SubExpr { get; }
        public NamedTupleEntry Entry { get; }
        public string FieldName => Entry.Name;

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type => Entry.Type;
    }
}