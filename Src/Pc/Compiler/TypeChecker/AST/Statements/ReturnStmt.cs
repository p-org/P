using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class ReturnStmt : IPStmt
    {
        public ReturnStmt(IPExpr returnValue) { ReturnValue = returnValue; }
        public IPExpr ReturnValue { get; }
        public PLanguageType ReturnType => ReturnValue == null ? PrimitiveType.Null : ReturnValue.Type;
    }
}