using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class ReceiveStmt : IPStmt
    {
        public IReadOnlyDictionary<PEvent, Function> Cases { get; }

        public ReceiveStmt(IReadOnlyDictionary<PEvent, Function> cases)
        {
            Cases = cases;
        }
    }
}