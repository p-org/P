using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class SendStmt : IPStmt
    {
        public SendStmt(IPExpr machineExpr, PEvent evt, List<IPExpr> argsList)
        {
            MachineExpr = machineExpr;
            Evt = evt;
            ArgsList = argsList;
        }

        public IPExpr MachineExpr { get; }
        public PEvent Evt { get; }
        public List<IPExpr> ArgsList { get; }
    }
}