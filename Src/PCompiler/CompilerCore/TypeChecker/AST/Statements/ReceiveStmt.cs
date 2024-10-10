using System.Collections.Generic;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class ReceiveStmt : IPStmt
    {
        public ReceiveStmt(ParserRuleContext sourceLocation, IReadOnlyDictionary<Event, Function> cases)
        {
            SourceLocation = sourceLocation;
            Cases = cases;
        }

        public IReadOnlyDictionary<Event, Function> Cases { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}