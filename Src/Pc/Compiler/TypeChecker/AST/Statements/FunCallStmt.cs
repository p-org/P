using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class FunCallStmt : IPStmt
    {
        public FunCallStmt(Function fun, List<IPExpr> argsList)
        {
            Fun = fun;
            ArgsList = argsList;
        }

        public Function Fun { get; }
        public List<IPExpr> ArgsList { get; }
    }
}