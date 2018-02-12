using System;

namespace Microsoft.Pc.TypeChecker
{
    public class TranslationException : Exception
    {
        public TranslationException(string message) : base(message) { }
    }
}
