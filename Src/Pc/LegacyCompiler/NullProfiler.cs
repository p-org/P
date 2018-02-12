using System;

namespace Microsoft.Pc
{
    public class NullProfiler : IProfiler
    {
        public IDisposable Start(string operation, string message)
        {
            return null;
        }
    }
}