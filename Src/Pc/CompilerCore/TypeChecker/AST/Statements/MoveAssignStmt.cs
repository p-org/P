using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class MoveAssignStmt : IPStmt
    {
        public MoveAssignStmt(ParserRuleContext sourceLocation, IPExpr toLocation, Variable fromVariable)
        {
            SourceLocation = sourceLocation;
            ToLocation = toLocation;
            FromVariable = fromVariable;
        }

        public IPExpr ToLocation { get; }
        public Variable FromVariable { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
