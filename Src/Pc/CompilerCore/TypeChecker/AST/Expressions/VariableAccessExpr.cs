using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
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