using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IPAST
    {
        ParserRuleContext SourceLocation { get; }
    }
}