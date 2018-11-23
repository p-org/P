using System.Collections.Generic;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class ReceiveStmt : IPStmt
    {
        public ReceiveStmt(ParserRuleContext sourceLocation, IReadOnlyDictionary<PEvent, Function> cases)
        {
            SourceLocation = sourceLocation;
            Cases = cases;
        }

        public IReadOnlyDictionary<PEvent, Function> Cases { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}