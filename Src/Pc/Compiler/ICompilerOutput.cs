using Microsoft.Formula.API;

namespace Microsoft.Pc
{
    public interface ICompilerOutput
    {
        void WriteMessage(string msg, SeverityKind severity);
    }
}
