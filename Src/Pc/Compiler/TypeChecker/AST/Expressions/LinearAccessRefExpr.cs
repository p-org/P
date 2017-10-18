using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class LinearAccessRefExpr : ILinearRef
    {
        public LinearAccessRefExpr(Variable variable, LinearType linearType)
        {
            Variable = variable;
            LinearType = linearType;
            Type = variable.Type;
        }

        public Variable Variable { get; }
        public PLanguageType Type { get; }
        public LinearType LinearType { get; }
    }
}
