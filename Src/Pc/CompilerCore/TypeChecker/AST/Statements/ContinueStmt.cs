using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class ContinueStmt : IPStmt
    {
        public ContinueStmt(ParserRuleContext sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public ParserRuleContext SourceLocation { get; }
    }
}
