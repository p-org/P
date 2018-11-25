using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST
{
    public interface IPAST
    {
        ParserRuleContext SourceLocation { get; }
    }
}