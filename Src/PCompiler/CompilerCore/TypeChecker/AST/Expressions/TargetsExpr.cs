using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class TargetsExpr : IPExpr
    {
        public TargetsExpr(ParserRuleContext sourceLocation, IPExpr instance, IPExpr target)
        {
            SourceLocation = sourceLocation;
            Instance = instance;
            Target = target;
            Type = PrimitiveType.Bool;
        }
        
        public IPExpr Instance { get; }
        public IPExpr Target { get; }

        public ParserRuleContext SourceLocation { get; }
        
        public PLanguageType Type { get; }
    }
}