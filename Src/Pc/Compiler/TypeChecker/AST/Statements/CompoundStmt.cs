using System.Collections.Generic;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class CompoundStmt : IPStmt
    {
        public CompoundStmt(ParserRuleContext sourceLocation, List<IPStmt> statements)
        {
            SourceLocation = sourceLocation;
            Statements = statements;
        }

        public List<IPStmt> Statements { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
