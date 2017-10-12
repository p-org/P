using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class PrintStmt : IPStmt
    {
        public PrintStmt(string message, List<IPExpr> args)
        {
            Message = message;
            Args = args;
        }

        public string Message { get; }
        public List<IPExpr> Args { get; }
    }
}