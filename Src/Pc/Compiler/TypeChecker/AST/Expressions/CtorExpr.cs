using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class CtorExpr : IPExpr
    {
        public CtorExpr(Machine machine, IPExpr[] arguments)
        {
            Machine = machine;
            Arguments = arguments;
        }

        public Machine Machine { get; }
        public IPExpr[] Arguments { get; }

        public PLanguageType Type { get; } = PrimitiveType.Machine;
    }
}
