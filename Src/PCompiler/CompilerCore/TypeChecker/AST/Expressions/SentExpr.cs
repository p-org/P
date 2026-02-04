using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class SentExpr : IPExpr
    {
        public SentExpr(ParserRuleContext sourceLocation, IPExpr instance)
        {
            SourceLocation = sourceLocation;
            Instance = instance;
            Type = PrimitiveType.Bool;
        }
        
        public IPExpr Instance { get; }

        public ParserRuleContext SourceLocation { get; }
        
        public PLanguageType Type { get; }
    }
}