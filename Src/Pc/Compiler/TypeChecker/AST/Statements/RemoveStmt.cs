using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class RemoveStmt : IPStmt
    {
        public ParserRuleContext SourceLocation { get; }
        public IPExpr Variable { get; }
        public IPExpr Value { get; }

        public RemoveStmt(ParserRuleContext sourceLocation, IPExpr variable, IPExpr value)
        {
            SourceLocation = sourceLocation;
            Variable = variable;
            Value = value;
        }
    }
}