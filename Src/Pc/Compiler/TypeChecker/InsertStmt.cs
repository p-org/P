using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker
{
    public class InsertStmt : IPStmt
    {
        public IPExpr Variable { get; }
        public IPExpr Index { get; }
        public IPExpr Value { get; }


        public InsertStmt(IPExpr variable, IPExpr index, IPExpr value)
        {
            Variable = variable;
            Index = index;
            Value = value;
        }
    }
}