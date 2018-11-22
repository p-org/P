using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class CtorStmt : IPStmt
    {
        public CtorStmt(ParserRuleContext sourceLocation, Interface @interface, IReadOnlyList<IPExpr> arguments)
        {
            SourceLocation = sourceLocation;
            Interface = @interface;
            Arguments = arguments;
        }

        public Interface Interface { get; }
        public IReadOnlyList<IPExpr> Arguments { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}