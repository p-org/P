using System.Collections.Generic;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class CompoundStmt : IPStmt
    {
        public CompoundStmt(List<IPStmt> statements) { Statements = statements; }

        public List<IPStmt> Statements { get; }
    }
}