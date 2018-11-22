using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
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