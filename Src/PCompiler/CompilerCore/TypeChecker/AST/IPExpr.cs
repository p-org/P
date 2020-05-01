using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST
{
    public interface IPExpr : IPAST
    {
        PLanguageType Type { get; }
    }
}