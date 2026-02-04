using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class TestExpr : IPExpr
    {
        public TestExpr(ParserRuleContext sourceLocation, IPExpr instance, IPDecl kind)
        {
            SourceLocation = sourceLocation;
            Instance = instance;
            Kind = kind;
            Type = PrimitiveType.Bool;
        }
        
        public IPExpr Instance { get; }
        public IPDecl Kind { get; }

        public ParserRuleContext SourceLocation { get; }
        
        public PLanguageType Type { get; }
    }
}