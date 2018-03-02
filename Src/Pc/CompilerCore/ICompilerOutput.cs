using Microsoft.Pc.Backend;

namespace Microsoft.Pc
{
    public interface ICompilerOutput
    {
        void WriteMessage(string msg, SeverityKind severity);
        void WriteFile(CompiledFile file);
    }
}
