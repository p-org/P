using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class MoveAssignStmt : IPStmt
    {
        public MoveAssignStmt(IPExpr toLocation, Variable fromVariable)
        {
            ToLocation = toLocation;
            FromVariable = fromVariable;
        }

        public IPExpr ToLocation { get; }
        public Variable FromVariable { get; }
    }
}