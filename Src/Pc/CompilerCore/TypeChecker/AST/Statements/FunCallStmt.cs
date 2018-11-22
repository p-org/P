using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class FunCallStmt : IPStmt
    {
        public FunCallStmt(ParserRuleContext sourceLocation, Function function, IReadOnlyList<IPExpr> argsList)
        {
            SourceLocation = sourceLocation;
            Function = function;
            ArgsList = argsList;
        }

        public Function Function { get; }
        public IReadOnlyList<IPExpr> ArgsList { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}