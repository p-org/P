using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class SendStmt : IPStmt
    {
        public SendStmt(ParserRuleContext sourceLocation, IPExpr machineExpr, IPExpr evt,
                        IReadOnlyList<IPExpr> argsList)
        {
            SourceLocation = sourceLocation;
            MachineExpr = machineExpr;
            Evt = evt;
            ArgsList = argsList;
        }

        public IPExpr MachineExpr { get; }
        public IPExpr Evt { get; }
        public IReadOnlyList<IPExpr> ArgsList { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
