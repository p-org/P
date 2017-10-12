using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class SwapAssignStmt : IPStmt
    {
        public SwapAssignStmt(IPExpr newLocation, Variable oldLocation)
        {
            NewLocation = newLocation;
            OldLocation = oldLocation;
        }

        public IPExpr NewLocation { get; }
        public Variable OldLocation { get; }
    }
}