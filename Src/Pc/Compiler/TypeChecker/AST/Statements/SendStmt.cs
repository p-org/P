using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class SendStmt : IPStmt
    {
        public SendStmt(IPExpr machineExpr, IPExpr evt, List<IPExpr> argsList)
        {
            MachineExpr = machineExpr;
            Evt = evt;
            ArgsList = argsList;
        }

        public IPExpr MachineExpr { get; }
        public IPExpr Evt { get; }
        public List<IPExpr> ArgsList { get; }
    }
}