using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class FunCallStmt : IPStmt
    {
        public FunCallStmt(ParserRuleContext sourceLocation, Function fun, List<IPExpr> argsList)
        {
            SourceLocation = sourceLocation;
            Fun = fun;
            ArgsList = argsList;
        }

        public ParserRuleContext SourceLocation { get; }
        public Function Fun { get; }
        public List<IPExpr> ArgsList { get; }
    }
}