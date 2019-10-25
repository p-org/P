using Antlr4.Runtime;
using System.Collections.Generic;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class PrintStmt : IPStmt
    {
        public PrintStmt(ParserRuleContext sourceLocation, string message, List<IPExpr> args)
        {
            SourceLocation = sourceLocation;
            Message = message;
            Args = args;
        }

        public string Message { get; }
        public List<IPExpr> Args { get; }

        public ParserRuleContext SourceLocation { get; }
    }
}