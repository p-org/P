using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class EnumElemRefExpr : IPExpr
    {
        public EnumElemRefExpr(EnumElem enumElem)
        {
            EnumElem = enumElem;
            Type = new EnumType(EnumElem.ParentEnum);
        }

        public EnumElem EnumElem { get; }
        public PLanguageType Type { get; }
    }
}
