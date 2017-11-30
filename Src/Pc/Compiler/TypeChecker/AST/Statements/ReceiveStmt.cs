using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.Statements
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