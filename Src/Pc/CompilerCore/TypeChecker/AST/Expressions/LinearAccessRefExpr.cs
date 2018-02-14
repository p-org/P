using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class LinearAccessRefExpr : ILinearRef
    {
        public LinearAccessRefExpr(ParserRuleContext sourceLocation, Variable variable, LinearType linearType)
        {
            SourceLocation = sourceLocation;
            Variable = variable;
            LinearType = linearType;
            Type = variable.Type;
        }

        public ParserRuleContext SourceLocation { get; }
        public Variable Variable { get; }
        public PLanguageType Type { get; }
        public LinearType LinearType { get; }
    }
}
