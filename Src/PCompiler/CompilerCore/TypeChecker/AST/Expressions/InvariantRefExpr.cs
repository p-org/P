using System.Collections.Generic;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class InvariantRefExpr : IPExpr
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

    public class InvariantGroupRefExpr : IPExpr {
        public InvariantGroupRefExpr(InvariantGroup invGroup, ParserRuleContext sourceLocation)
        {
            InvariantGroup = invGroup;
            SourceLocation = sourceLocation;
        }
        public InvariantGroup InvariantGroup { get; set; }
        public List<Invariant> Invariants => InvariantGroup.Invariants;

        public PLanguageType Type => PrimitiveType.Bool;

        public ParserRuleContext SourceLocation { get; set; }
    }
}