using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;

namespace Plang.Compiler.Backend.Symbolic
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
