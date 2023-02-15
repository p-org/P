/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */

namespace Plang.Compiler.Backend.Rvm
{
    internal class BeforeSeparator
    {
        internal delegate void SeparatorDelegate();

        private SeparatorDelegate Separator { get; }
        private bool hadElements = false;

        internal BeforeSeparator(SeparatorDelegate separator)
        {
            Separator = separator;
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
