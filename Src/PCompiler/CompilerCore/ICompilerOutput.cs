using Plang.Compiler.Backend;

namespace Plang.Compiler
{
    public interface ICompilerOutput
    {
        void WriteMessage(string msg, SeverityKind severity);

        void WriteFile(CompiledFile file);

        void WriteError(string msg);

        void WriteInfo(string msg);

        void WriteWarning(string msg);
    }
}