using Antlr4.Runtime;
using System.Collections.Generic;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class SendStmt : IPStmt
    {
        public SendStmt(ParserRuleContext sourceLocation, IPExpr machineExpr, IPExpr evt,
            IReadOnlyList<IPExpr> arguments)
        {
            SourceLocation = sourceLocation;
            MachineExpr = machineExpr;
            Evt = evt;
            Arguments = arguments;
        }

        public IPExpr MachineExpr { get; }
        public IPExpr Evt { get; }
        public IReadOnlyList<IPExpr> Arguments { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}