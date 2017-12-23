using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Statements
{
    public class ReturnStmt : IPStmt
    {
        public ReturnStmt(ParserRuleContext sourceLocation, IPExpr returnValue)
        {
            SourceLocation = sourceLocation;
            ReturnValue = returnValue;
        }

        public IPExpr ReturnValue { get; }
        public PLanguageType ReturnType => ReturnValue == null ? PrimitiveType.Null : ReturnValue.Type;
        public ParserRuleContext SourceLocation { get; }
    }
}