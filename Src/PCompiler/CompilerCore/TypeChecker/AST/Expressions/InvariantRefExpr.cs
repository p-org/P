using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    class InvariantRefExpr : IPExpr
    {
        public InvariantRefExpr(Invariant inv, ParserRuleContext sourceLocation)
        {
            Invariant = inv;
            SourceLocation = sourceLocation;
        }
        public Invariant Invariant { get; set; }

        public PLanguageType Type => PrimitiveType.Bool;

        public ParserRuleContext SourceLocation { get; set; }
    }
}