using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class SwapAssignStmt : IPStmt
    {
        public SwapAssignStmt(ParserRuleContext sourceLocation, IPExpr newLocation, Variable oldLocation)
        {
            SourceLocation = sourceLocation;
            NewLocation = newLocation;
            OldLocation = oldLocation;
        }

        public IPExpr NewLocation { get; }
        public Variable OldLocation { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}