using System;

namespace Microsoft.Pc
{
    public interface IProfiler
    {
        IDisposable Start(string operation, string message);
    }
}