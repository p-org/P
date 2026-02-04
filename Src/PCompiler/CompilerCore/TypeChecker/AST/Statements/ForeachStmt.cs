using System.Collections.Generic;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class ForeachStmt : IPStmt
    {
        public ForeachStmt(ParserRuleContext sourceLocation, Variable item, IPExpr collection, IPStmt body, List<IPExpr> invariants)
        {
            SourceLocation = sourceLocation;
            Item = item;
            IterCollection = collection;
            Body = CompoundStmt.FromStatement(body);
            Invariants = invariants;
        }

        public Variable Item { get; }
        public IPExpr IterCollection { get; }
        public CompoundStmt Body { get; }
        public List<IPExpr> Invariants { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}