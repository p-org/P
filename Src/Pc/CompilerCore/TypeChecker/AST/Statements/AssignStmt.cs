using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class AssignStmt : IPStmt
    {
        public AssignStmt(ParserRuleContext sourceLocation, IPExpr location, IPExpr value)
        {
            SourceLocation = sourceLocation;
            Location = location;
            Value = value;
        }

        public IPExpr Location { get; }
        public IPExpr Value { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}