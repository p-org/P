namespace PChecker.Runtime.Values
{
    public static class MutabilityHelper
    {
        public static void EnsureFrozen<T>(T value)
            where T : IPValue
        {
            if (value is IPMutableValue mutable)
            {
                mutable.Freeze();
            }
        }

        /// <summary>
        /// Returns a frozen copy of a value, for a set or map to store as a key. The value itself is
        /// not frozen: the caller may still change it, e.g. when it is a field or element of another
        /// value, without changing the stored key.
        /// </summary>
        internal static IPValue FrozenCopy(IPValue value)
        {
            var copy = PValues.Clone(value);
            EnsureFrozen(copy);
            return copy;
        }
    }
}