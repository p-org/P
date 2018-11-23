using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class EnumElemRefExpr : IStaticTerm<EnumElem>
    {
        public EnumElemRefExpr(ParserRuleContext sourceLocation, EnumElem value)
        {
            SourceLocation = sourceLocation;
            Value = value;
            Type = new EnumType(Value.ParentEnum);
        }

        public EnumElem Value { get; }
        public PLanguageType Type { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}