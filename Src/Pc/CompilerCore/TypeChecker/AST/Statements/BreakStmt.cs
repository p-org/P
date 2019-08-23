using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class BreakStmt : IPStmt
    {
        public BreakStmt(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public ParserRuleContext SourceLocation { get; }
    }
}
