using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class ReceiveStmt : IPStmt
    {
        public ParserRuleContext SourceLocation { get; }
        public IReadOnlyDictionary<PEvent, Function> Cases { get; }

        public ReceiveStmt(ParserRuleContext sourceLocation, IReadOnlyDictionary<PEvent, Function> cases)
        {
            SourceLocation = sourceLocation;
            Cases = cases;
        }
    }
}