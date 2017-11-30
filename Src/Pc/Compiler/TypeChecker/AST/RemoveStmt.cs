using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class RemoveStmt : IPStmt
    {
        public IPExpr Variable { get; }
        public IPExpr Value { get; }

        public RemoveStmt(IPExpr variable, IPExpr value)
        {
            Variable = variable;
            Value = value;
        }
    }
}