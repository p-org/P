using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System.Collections.Generic;
using Plang.Compiler.Backend.Symbolic;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class ReceiveSplitStmt : IPStmt
    {
        public ReceiveSplitStmt(ParserRuleContext location, Continuation continuation)
        {
            SourceLocation = location;
            Cont = continuation;
        }

        public Continuation Cont { get; } 
        public ParserRuleContext SourceLocation { get; }
    }
}
