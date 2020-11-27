using System.IO;

namespace Plang.Compiler.Backend.Rvm
{
    internal class BeforeSeparator
    {
        internal delegate void SeparatorDelegate();

        private SeparatorDelegate Separator { get; }
        private bool hadElements = false;

        internal BeforeSeparator(SeparatorDelegate separator)
        {
            this.Separator = separator;
        }

        public void beforeElement()
        {
            if (hadElements)
            {
                Separator.Invoke();
            }
            hadElements = true;
        }
    }
}
