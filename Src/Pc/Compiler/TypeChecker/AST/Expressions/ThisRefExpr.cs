using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class ThisRefExpr : IPExpr
    {
        public ThisRefExpr(Machine machine) { Machine = machine; }

        public Machine Machine { get; }

        public PLanguageType Type { get; } = PrimitiveType.Machine;
    }
}
