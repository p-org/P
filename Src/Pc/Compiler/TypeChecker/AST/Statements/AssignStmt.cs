using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class AssignStmt : IPStmt
    {
        public AssignStmt(IPExpr variable, IPExpr value)
        {
            Variable = variable;
            Value = value;
        }

        public IPExpr Variable { get; }
        public IPExpr Value { get; }
    }
}