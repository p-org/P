using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class InsertStmt : IPStmt
    {
        public InsertStmt(ParserRuleContext sourceLocation, IPExpr variable, IPExpr index, IPExpr value)
        {
            SourceLocation = sourceLocation;
            Variable = variable;
            Index = index;
            Value = value;
        }

        public IPExpr Variable { get; }
        public IPExpr Index { get; }
        public IPExpr Value { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}