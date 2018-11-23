using System;

namespace Plang.Compiler
{
    public class TranslationException : Exception
    {
        public TranslationException(string message) : base(message)
        {
        }
    }
}