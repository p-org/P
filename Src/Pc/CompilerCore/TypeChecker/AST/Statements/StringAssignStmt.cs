using System.Collections.Generic;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.States;

namespace Plang.Compiler.TypeChecker.AST.Statements
{
    public class StringAssignStmt : IPStmt
    {
        public StringAssignStmt(ParserRuleContext sourceLocation, IPExpr location, string baseString, List<IPExpr> args)
        {
            SourceLocation = sourceLocation;
            Location = location;
            BaseString = baseString;
            Args = args;
        }

        public string BaseString { get; }
        public List<IPExpr> Args { get; }

        public ParserRuleContext SourceLocation { get; }
        public IPExpr Location { get; }
    }
}
