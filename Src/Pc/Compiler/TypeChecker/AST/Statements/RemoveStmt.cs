using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
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