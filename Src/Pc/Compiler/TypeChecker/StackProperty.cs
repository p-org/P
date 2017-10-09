using System;

namespace Microsoft.Pc.TypeChecker
{
    public class StackProperty<T>
        where T : class
    {
        public T Value { get; private set; }

        public IDisposable NewContext(T newValue)
        {
            return new ContextManager(this, newValue);
        }

        private class ContextManager : IDisposable
        {
            private readonly StackProperty<T> stackProperty;
            private readonly T oldValue;

            public ContextManager(StackProperty<T> stackProperty, T newValue)
            {
                this.stackProperty = stackProperty;
                oldValue = stackProperty.Value;
                stackProperty.Value = newValue;
            }

            public void Dispose() { stackProperty.Value = oldValue; }
        }
    }
}