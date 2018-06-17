using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class VariableAccessExpr : IVariableRef
    {
        public VariableAccessExpr(ParserRuleContext sourceLocation, Variable variable)
        {
            SourceLocation = sourceLocation;
            Variable = variable;
            Type = variable.Type;
        }

        public Variable Variable { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}
