using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class SpecRefExpr : IStaticTerm<Machine>
    {
        public SpecRefExpr(ParserRuleContext sourceLocation, Machine value)
        {
            Value = value;
            SourceLocation = sourceLocation;
        }

        public Machine Value { get; }

        public PLanguageType Type { get; } = PrimitiveType.Machine;
        public ParserRuleContext SourceLocation { get; }
    }
}