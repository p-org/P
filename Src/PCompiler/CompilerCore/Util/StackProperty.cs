using System;

namespace Plang.Compiler.Util
{
    public class StackProperty<T>
        where T : class
    {
        public StackProperty() : this(null)
        {
        }

        public StackProperty(T initial)
        {
            Value = initial;
        }

        public T Value { get; private set; }

        public IDisposable NewContext(T newValue)
        {
            return new ContextManager(this, newValue);
        }

        private class ContextManager : IDisposable
        {
            private readonly T oldValue;
            private readonly StackProperty<T> stackProperty;

            public ContextManager(StackProperty<T> stackProperty, T newValue)
            {
                this.stackProperty = stackProperty;
                oldValue = stackProperty.Value;
                stackProperty.Value = newValue;
            }

            public void Dispose()
            {
                stackProperty.Value = oldValue;
            }
        }
    }
}