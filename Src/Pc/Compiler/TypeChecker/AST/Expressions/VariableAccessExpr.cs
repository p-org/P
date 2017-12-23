using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class VariableAccessExpr : IPExpr
    {
        public VariableAccessExpr(ParserRuleContext sourceLocation, Variable variable)
        {
            SourceLocation = sourceLocation;
            Variable = variable;
            Type = variable.Type;
        }

        public ParserRuleContext SourceLocation { get; }
        public Variable Variable { get; }

        public PLanguageType Type { get; }
    }
}
