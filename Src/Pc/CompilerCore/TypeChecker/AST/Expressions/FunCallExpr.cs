using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class FunCallExpr : IPExpr
    {
        public FunCallExpr(ParserRuleContext sourceLocation, Function function, IReadOnlyList<IPExpr> arguments)
        {
            SourceLocation = sourceLocation;
            Function = function;
            Arguments = arguments;
            Type = function.Signature.ReturnType;
        }

        public Function Function { get; }
        public IReadOnlyList<IPExpr> Arguments { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}