using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class FunCallExpr : IPExpr
    {
        public FunCallExpr(ParserRuleContext sourceLocation, Function function, IPExpr[] arguments)
        {
            SourceLocation = sourceLocation;
            Function = function;
            Arguments = arguments;
            Type = function.Signature.ReturnType;
        }

        public ParserRuleContext SourceLocation { get; }
        public Function Function { get; }
        public IPExpr[] Arguments { get; }

        public PLanguageType Type { get; }
    }
}
