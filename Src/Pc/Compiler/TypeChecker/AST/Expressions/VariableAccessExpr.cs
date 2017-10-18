using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class VariableAccessExpr : IVarRef
    {
        public VariableAccessExpr(Variable variable)
        {
            Variable = variable;
            Type = variable.Type;
        }

        public Variable Variable { get; }

        public PLanguageType Type { get; }
    }
}
