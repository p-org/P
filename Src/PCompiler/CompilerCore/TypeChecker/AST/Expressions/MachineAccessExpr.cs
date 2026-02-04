using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class MachineAccessExpr : IPExpr
    {
        public MachineAccessExpr(ParserRuleContext sourceLocation, Machine machine, IPExpr subExpr, Variable entry)
        {
            SourceLocation = sourceLocation;
            Machine = machine;
            SubExpr = subExpr;
            Entry = entry;
        }
        
        public Machine Machine { get; }
        public IPExpr SubExpr { get; }
        public Variable Entry { get; }
        public string FieldName => Entry.Name;

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type => Entry.Type;
    }
}