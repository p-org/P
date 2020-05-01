using Plang.Compiler.Backend;

namespace Plang.Compiler
{
    public interface ICompilerOutput
    {
        void WriteMessage(string msg, SeverityKind severity);

        void WriteFile(CompiledFile file);
    }
}