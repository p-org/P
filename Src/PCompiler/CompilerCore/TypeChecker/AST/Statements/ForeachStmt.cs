using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class ForeachStmt : IPStmt
    {
        public ForeachStmt(ParserRuleContext sourceLocation, Variable item, IPExpr collection, IPStmt body)
        {
            SourceLocation = sourceLocation;
            Item = item;
            IterCollection = collection;
            Body = CompoundStmt.FromStatement(body);
        }

        public Variable Item { get; }
        public IPExpr IterCollection { get; }
        public CompoundStmt Body { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}