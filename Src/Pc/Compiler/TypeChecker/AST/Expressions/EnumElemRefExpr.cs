using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class EnumElemRefExpr : IPExpr
    {
        public EnumElemRefExpr(ParserRuleContext sourceLocation, EnumElem enumElem)
        {
            SourceLocation = sourceLocation;
            EnumElem = enumElem;
            Type = new EnumType(EnumElem.ParentEnum);
        }

        public EnumElem EnumElem { get; }
        public PLanguageType Type { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
