using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class ContainsKeyExpr : IPExpr
    {
        public ContainsKeyExpr(ParserRuleContext sourceLocation, IPExpr key, IPExpr map)
        {
            SourceLocation = sourceLocation;
            Key = key;
            Map = map;
        }

        public IPExpr Key { get; }
        public IPExpr Map { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
        public ParserRuleContext SourceLocation { get; }
    }
}