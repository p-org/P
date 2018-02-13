using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class CtorStmt : IPStmt
    {
        public CtorStmt(ParserRuleContext sourceLocation, Machine machine, List<IPExpr> arguments)
        {
            SourceLocation = sourceLocation;
            Machine = machine;
            Arguments = arguments;
        }

        public Machine Machine { get; }
        public List<IPExpr> Arguments { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}
