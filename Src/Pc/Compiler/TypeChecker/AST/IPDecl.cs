using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IPDecl
    {
        string Name { get; }
        ParserRuleContext SourceNode { get; }
    }
}
