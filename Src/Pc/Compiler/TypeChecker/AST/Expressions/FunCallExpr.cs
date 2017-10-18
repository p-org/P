using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class FunCallExpr : IPExpr
    {
        public FunCallExpr(Function function, IPExpr[] arguments)
        {
            Function = function;
            Arguments = arguments;
            Type = function.Signature.ReturnType;
        }

        public Function Function { get; }
        public IPExpr[] Arguments { get; }

        public PLanguageType Type { get; }
    }
}
