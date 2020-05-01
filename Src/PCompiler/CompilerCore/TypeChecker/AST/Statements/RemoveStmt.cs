using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class RemoveStmt : IPStmt
    {
        public RemoveStmt(ParserRuleContext sourceLocation, IPExpr variable, IPExpr value)
        {
            SourceLocation = sourceLocation;
            Variable = variable;
            Value = value;
        }

        public IPExpr Variable { get; }
        public IPExpr Value { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}