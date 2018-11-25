using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class ThisRefExpr : IStaticTerm<Machine>
    {
        public ThisRefExpr(ParserRuleContext sourceLocation, Machine value)
        {
            SourceLocation = sourceLocation;
            Value = value;
            Type = new PermissionType(value);
        }

        public Machine Value { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}