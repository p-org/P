using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class CtorStmt : IPStmt
    {
        public CtorStmt(Machine machine, List<IPExpr> arguments)
        {
            Machine = machine;
            Arguments = arguments;
        }

        public Machine Machine { get; }
        public List<IPExpr> Arguments { get; }
    }
}