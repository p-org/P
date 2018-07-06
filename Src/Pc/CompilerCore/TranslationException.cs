using System;

namespace Microsoft.Pc
{
    public class TranslationException : Exception
    {
        public TranslationException(string message) : base(message) { }
    }
}
